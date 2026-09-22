using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 互认统计查询仓储的内存替身：保存接收侧与来源侧汇总分组行、原因行、四类接收侧明细行、
/// 来源侧明细行与匹配记录集合视图行，按用例脚本记录筛选条件、窗口与调用次数。
/// </summary>
/// <remarks>
/// 替身只用于隔离数据库：应用层下推的筛选条件、窗口起点与页容量被原样记录，供用例断言"窗口校验发生在读取之前"、
/// "范围与筛选条件按规范下推"与"名称回填调用次数不随行数增长"；取页按用例授权的集合顺序切片，
/// 排序稳定性由语句静态守卫负责，替身不重排数据。未覆盖的查询成员一律抛出，
/// 避免用例在未覆盖的路径上静默走过。
/// </remarks>
internal sealed class FakeStatisticsQueryRepository : IMedicalRecognitionReportQueryRepository
{
  /// <summary>未使用的查询成员统一提示。</summary>
  private const string UnusedMember = "本替身不覆盖该查询成员。";

  /// <summary>接收侧汇总的当页分组行集合；计数方法返回其条数，取页方法按窗口切片。</summary>
  public List<RecognitionUsageSummaryGroupItem> SummaryGroupItems { get; } = [];

  /// <summary>接收侧汇总的不采纳原因行集合；取原因方法返回全量，应用层按分组键装配。</summary>
  public List<RecognitionUsageReasonItem> ReasonItems { get; } = [];

  /// <summary>接收侧提醒明细行集合；含未反馈项，计数与取页都基于本集合。</summary>
  public List<RecognitionReminderDetailItem> ReminderItems { get; } = [];

  /// <summary>提醒明细计数的脚本化覆盖取值；小于零时按集合条数计数，供导出行数上限用例注入超上限与边界取值。</summary>
  public long ReminderCountOverride { get; init; } = -1;

  /// <summary>接收侧采纳明细行集合；计数与取页都基于本集合。</summary>
  public List<RecognitionAdoptionDetailItem> AdoptionItems { get; } = [];

  /// <summary>接收侧不采纳明细行集合；计数与取页都基于本集合。</summary>
  public List<RecognitionNonAdoptionDetailItem> NonAdoptionItems { get; } = [];

  /// <summary>接收侧引用明细行集合；计数与取页都基于本集合。</summary>
  public List<RecognitionReferenceDetailItem> ReferenceItems { get; } = [];

  /// <summary>来源侧汇总的当页分组行集合；计数方法返回其条数，取页方法按窗口切片。</summary>
  public List<SourceRecognitionSummaryGroupItem> SourceSummaryGroupItems { get; } = [];

  /// <summary>来源侧明细行集合；计数与取页都基于本集合。</summary>
  public List<SourceRecognitionDetailItem> SourceDetailItems { get; } = [];

  /// <summary>匹配记录集合视图的组级行集合；按标识查找，未命中即视图查询返回空。</summary>
  public List<RecognitionMatchRecordView> MatchRecordViews { get; } = [];

  /// <summary>匹配记录集合视图的匹配项行集合；按标识查询时返回全量。</summary>
  public List<RecognitionMatchRecordViewItem> MatchRecordViewItems { get; } = [];

  /// <summary>汇总计数方法调用次数。</summary>
  public int SummaryCountCalls { get; private set; }

  /// <summary>汇总取页方法调用次数。</summary>
  public int SummaryPageCalls { get; private set; }

  /// <summary>原因汇总方法调用次数。</summary>
  public int ReasonCalls { get; private set; }

  /// <summary>提醒明细计数与取页调用次数之和。</summary>
  public int ReminderCalls { get; private set; }

  /// <summary>采纳明细计数与取页调用次数之和。</summary>
  public int AdoptionCalls { get; private set; }

  /// <summary>不采纳明细计数与取页调用次数之和。</summary>
  public int NonAdoptionCalls { get; private set; }

  /// <summary>引用明细计数与取页调用次数之和。</summary>
  public int ReferenceCalls { get; private set; }

  /// <summary>来源侧汇总计数调用次数。</summary>
  public int SourceSummaryCountCalls { get; private set; }

  /// <summary>来源侧汇总取页调用次数。</summary>
  public int SourceSummaryPageCalls { get; private set; }

  /// <summary>来源侧明细计数与取页调用次数之和。</summary>
  public int SourceDetailCalls { get; private set; }

  /// <summary>匹配记录组级信息查询调用次数。</summary>
  public int MatchRecordViewCalls { get; private set; }

  /// <summary>匹配记录匹配项查询调用次数。</summary>
  public int MatchRecordViewItemsCalls { get; private set; }

  /// <summary>全部已实现成员的调用总次数；非法窗口用例用它断言零仓储访问。</summary>
  public int TotalCalls =>
    SummaryCountCalls + SummaryPageCalls + ReasonCalls + ReminderCalls + AdoptionCalls + NonAdoptionCalls + ReferenceCalls
    + SourceSummaryCountCalls + SourceSummaryPageCalls + SourceDetailCalls + MatchRecordViewCalls + MatchRecordViewItemsCalls;

  /// <summary>最近一次收到的汇总筛选条件。</summary>
  public RecognitionUsageSummaryFilter? LastSummaryFilter { get; private set; }

  /// <summary>最近一次收到的明细筛选条件。</summary>
  public RecognitionUsageDetailFilter? LastDetailFilter { get; private set; }

  /// <summary>最近一次收到的来源侧汇总筛选条件。</summary>
  public SourceRecognitionSummaryFilter? LastSourceSummaryFilter { get; private set; }

  /// <summary>最近一次收到的来源侧明细筛选条件。</summary>
  public SourceRecognitionDetailFilter? LastSourceDetailFilter { get; private set; }

  /// <summary>最近一次匹配记录组级信息查询收到的标识。</summary>
  public Guid? LastMatchRecordViewId { get; private set; }

  /// <summary>最近一次匹配记录匹配项查询收到的标识。</summary>
  public Guid? LastMatchRecordViewItemsId { get; private set; }

  /// <summary>最近一次汇总取页收到的窗口起点。</summary>
  public int LastSummarySkipCount { get; private set; }

  /// <summary>最近一次汇总取页收到的页容量。</summary>
  public int LastSummaryPageSize { get; private set; }

  /// <summary>最近一次明细取页收到的窗口起点。</summary>
  public int LastDetailSkipCount { get; private set; }

  /// <summary>最近一次明细取页收到的页容量。</summary>
  public int LastDetailPageSize { get; private set; }

  /// <summary>最近一次明细取页对应的明细类型。</summary>
  public RecognitionUsageDetailType LastDetailType { get; private set; }

  /// <summary>最近一次来源侧汇总取页收到的窗口起点。</summary>
  public int LastSourceSummarySkipCount { get; private set; }

  /// <summary>最近一次来源侧汇总取页收到的页容量。</summary>
  public int LastSourceSummaryPageSize { get; private set; }

  /// <summary>最近一次来源侧明细取页收到的窗口起点。</summary>
  public int LastSourceDetailSkipCount { get; private set; }

  /// <summary>最近一次来源侧明细取页收到的页容量。</summary>
  public int LastSourceDetailPageSize { get; private set; }

  /// <inheritdoc/>
  public Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter)
  {
    SummaryCountCalls++;
    LastSummaryFilter = filter;
    return Task.FromResult((long)SummaryGroupItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize)
  {
    SummaryPageCalls++;
    LastSummaryFilter = filter;
    LastSummarySkipCount = skipCount;
    LastSummaryPageSize = pageSize;
    return Task.FromResult<IEnumerable<RecognitionUsageSummaryGroupItem>>([.. SummaryGroupItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter)
  {
    ReasonCalls++;
    LastSummaryFilter = filter;
    return Task.FromResult<IEnumerable<RecognitionUsageReasonItem>>([.. ReasonItems]);
  }

  /// <inheritdoc/>
  public Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter)
  {
    ReminderCalls++;
    LastDetailFilter = filter;
    return Task.FromResult(ReminderCountOverride >= 0 ? ReminderCountOverride : (long)ReminderItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize)
  {
    ReminderCalls++;
    LastDetailFilter = filter;
    LastDetailType = RecognitionUsageDetailType.Reminder;
    LastDetailSkipCount = skipCount;
    LastDetailPageSize = pageSize;
    return Task.FromResult<IEnumerable<RecognitionReminderDetailItem>>([.. ReminderItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter)
  {
    AdoptionCalls++;
    LastDetailFilter = filter;
    return Task.FromResult((long)AdoptionItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize)
  {
    AdoptionCalls++;
    LastDetailFilter = filter;
    LastDetailType = RecognitionUsageDetailType.Adopted;
    LastDetailSkipCount = skipCount;
    LastDetailPageSize = pageSize;
    return Task.FromResult<IEnumerable<RecognitionAdoptionDetailItem>>([.. AdoptionItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter)
  {
    NonAdoptionCalls++;
    LastDetailFilter = filter;
    return Task.FromResult((long)NonAdoptionItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize)
  {
    NonAdoptionCalls++;
    LastDetailFilter = filter;
    LastDetailType = RecognitionUsageDetailType.NotAdopted;
    LastDetailSkipCount = skipCount;
    LastDetailPageSize = pageSize;
    return Task.FromResult<IEnumerable<RecognitionNonAdoptionDetailItem>>([.. NonAdoptionItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter)
  {
    ReferenceCalls++;
    LastDetailFilter = filter;
    return Task.FromResult((long)ReferenceItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize)
  {
    ReferenceCalls++;
    LastDetailFilter = filter;
    LastDetailType = RecognitionUsageDetailType.Referenced;
    LastDetailSkipCount = skipCount;
    LastDetailPageSize = pageSize;
    return Task.FromResult<IEnumerable<RecognitionReferenceDetailItem>>([.. ReferenceItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <summary>本替身不覆盖：目录与配置类查询。</summary>
  /// <param name="itemType">项目类型筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：分组列表查询。</summary>
  /// <param name="categoryId">分类标识筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：标准项目列表查询。</summary>
  /// <param name="categoryId">分类标识筛选。</param>
  /// <param name="groupId">分组标识筛选。</param>
  /// <param name="code">编码筛选。</param>
  /// <param name="name">名称筛选。</param>
  /// <param name="isValid">启用状态筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：当前有效目录查询。</summary>
  /// <param name="itemType">项目类型筛选。</param>
  /// <param name="categoryName">分类名称筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：互认配置列表查询。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码筛选。</param>
  /// <param name="configurationStatus">配置状态筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：互认项目金额列表查询。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码筛选。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告列表计数。</summary>
  /// <param name="filter">报告列表筛选条件。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告列表取页。</summary>
  /// <param name="filter">报告列表筛选条件。</param>
  /// <param name="skipCount">窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告版本列表查询。</summary>
  /// <param name="reportId">报告标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告版本详情查询。</summary>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告来源归属查询。</summary>
  /// <param name="reportId">报告标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告版本公共信息查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：检验专项内容查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：普通检验结果查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按版本集合读取普通检验结果。</summary>
  /// <param name="reportVersionIds">报告版本标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：细菌鉴定结果查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：药敏结果查询。</summary>
  /// <param name="bacteriaResultId">细菌鉴定结果标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：检查专项内容查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：检查项目查询。</summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按版本集合读取检查项目。</summary>
  /// <param name="reportVersionIds">报告版本标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：检查部位查询。</summary>
  /// <param name="examinationItemId">检查项目标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按项目集合读取检查部位。</summary>
  /// <param name="examinationItemIds">检查项目标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：候选报告查询。</summary>
  /// <param name="organizationCode">可信接收组织编码。</param>
  /// <param name="hospitalCode">可信接收医院编码。</param>
  /// <param name="branchCode">可信接收院区编码。</param>
  /// <param name="patientId">平台患者标识。</param>
  /// <param name="identityDocumentTypeCode">证件类型代码。</param>
  /// <param name="identityDocumentNo">证件号码。</param>
  /// <param name="visitType">就诊类型。</param>
  /// <param name="visitSerialNo">就诊流水号。</param>
  /// <param name="standardProjectCodes">标准项目编码集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    Guid patientId,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo,
    IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：匹配响应的报告事实查询。</summary>
  /// <param name="reportVersionIds">报告版本标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告版本有效性读取。</summary>
  /// <param name="reportVersionIds">报告版本标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：报告版本文件信息查询。</summary>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：引用详情候选采纳记录查询。</summary>
  /// <param name="organizationCode">可信接收组织编码。</param>
  /// <param name="hospitalCode">可信接收医院编码。</param>
  /// <param name="branchCode">可信接收院区编码。</param>
  /// <param name="identityDocumentTypeCode">证件类型代码。</param>
  /// <param name="identityDocumentNo">证件号码。</param>
  /// <param name="visitType">就诊类型。</param>
  /// <param name="visitSerialNo">就诊流水号。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：引用详情报告公共上下文查询。</summary>
  /// <param name="reportVersionIds">报告版本标识集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：引用详情的标准项目名称查询。</summary>
  /// <param name="standardProjectCodes">标准项目编码集合。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter)
  {
    SourceSummaryCountCalls++;
    LastSourceSummaryFilter = filter;
    return Task.FromResult((long)SourceSummaryGroupItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize)
  {
    SourceSummaryPageCalls++;
    LastSourceSummaryFilter = filter;
    LastSourceSummarySkipCount = skipCount;
    LastSourceSummaryPageSize = pageSize;
    return Task.FromResult<IEnumerable<SourceRecognitionSummaryGroupItem>>([.. SourceSummaryGroupItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter)
  {
    SourceDetailCalls++;
    LastSourceDetailFilter = filter;
    return Task.FromResult((long)SourceDetailItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize)
  {
    SourceDetailCalls++;
    LastSourceDetailFilter = filter;
    LastSourceDetailSkipCount = skipCount;
    LastSourceDetailPageSize = pageSize;
    return Task.FromResult<IEnumerable<SourceRecognitionDetailItem>>([.. SourceDetailItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId)
  {
    MatchRecordViewCalls++;
    LastMatchRecordViewId = recognitionMatchRecordId;
    return Task.FromResult<RecognitionMatchRecordView?>(MatchRecordViews.FirstOrDefault(view => view.RecognitionMatchRecordId == recognitionMatchRecordId));
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId)
  {
    MatchRecordViewItemsCalls++;
    LastMatchRecordViewItemsId = recognitionMatchRecordId;
    return Task.FromResult<IEnumerable<RecognitionMatchRecordViewItem>>([.. MatchRecordViewItems]);
  }
}
