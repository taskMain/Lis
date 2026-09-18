using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 报告管理端查询仓储的内存替身：保存报告行、版本行、内容行与下载信息，并按用例脚本记录筛选条件与调用次数。
/// </summary>
/// <remarks>
/// 替身只用于隔离数据库：应用层下推的筛选条件、窗口起点与页容量被原样记录，供用例断言"校验发生在读取之前"、
/// "条件按规范形式下推"与"名称回填调用次数不随行数增长"。未参与本阶段用例的目录与金额查询成员一律抛出，
/// 避免测试静默走过未覆盖的路径。
/// </remarks>
internal sealed class FakeQueryRepository : IMedicalRecognitionReportQueryRepository
{
  /// <summary>未使用的查询成员统一提示。</summary>
  private const string UnusedMember = "本替身不覆盖该查询成员。";

  /// <summary>报告列表行；计数与取页都基于本集合，并把条件记录到 <see cref="LastFilter"/>。</summary>
  public List<MedicalReportListItem> ReportItems { get; } = [];

  /// <summary>报告来源归属行。</summary>
  public Dictionary<Guid, MedicalReportScopeItem> ReportScopes { get; } = [];

  /// <summary>版本列表行。</summary>
  public List<MedicalReportVersionItem> VersionItems { get; } = [];

  /// <summary>版本详情行，按报告标识与版本标识索引。</summary>
  public Dictionary<(Guid ReportId, Guid ReportVersionId), MedicalReportVersionDetailItem> VersionDetails { get; } = [];

  /// <summary>版本公共信息，按版本标识索引。</summary>
  public Dictionary<Guid, MedicalRecognitionReportDetailCommon> VersionCommons { get; } = [];

  /// <summary>检验专项内容，按版本标识索引。</summary>
  public Dictionary<Guid, LaboratoryReportContentItem> LaboratoryContents { get; } = [];

  /// <summary>普通检验结果，按版本标识索引。</summary>
  public Dictionary<Guid, List<LaboratoryResultItemView>> LaboratoryResultItems { get; } = [];

  /// <summary>细菌鉴定结果，按版本标识索引。</summary>
  public Dictionary<Guid, List<LaboratoryBacteriaResultItem>> BacteriaItems { get; } = [];

  /// <summary>药敏结果，按细菌鉴定结果标识索引。</summary>
  public Dictionary<Guid, List<LaboratorySusceptibilityItem>> SusceptibilityItems { get; } = [];

  /// <summary>检查专项内容，按版本标识索引。</summary>
  public Dictionary<Guid, ExaminationReportContentItem> ExaminationContents { get; } = [];

  /// <summary>检查项目，按版本标识索引。</summary>
  public Dictionary<Guid, List<ExaminationItemView>> ExaminationItemViews { get; } = [];

  /// <summary>检查部位，按检查项目标识索引。</summary>
  public Dictionary<Guid, List<ExaminationSiteView>> ExaminationSiteViews { get; } = [];

  /// <summary>计数方法调用次数。</summary>
  public int CountCalls { get; private set; }

  /// <summary>取当页方法调用次数。</summary>
  public int PageCalls { get; private set; }

  /// <summary>最近一次收到的筛选条件。</summary>
  public MedicalReportListFilter? LastFilter { get; private set; }

  /// <summary>最近一次收到的窗口起点。</summary>
  public int LastSkipCount { get; private set; }

  /// <summary>最近一次收到的页容量。</summary>
  public int LastPageSize { get; private set; }

  /// <inheritdoc/>
  public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter)
  {
    CountCalls++;
    LastFilter = filter;
    return Task.FromResult((long)ReportItems.Count);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize)
  {
    PageCalls++;
    LastFilter = filter;
    LastSkipCount = skipCount;
    LastPageSize = pageSize;
    return Task.FromResult<IEnumerable<MedicalReportListItem>>(
      [.. ReportItems.Skip(skipCount).Take(pageSize)]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) =>
    Task.FromResult<IEnumerable<MedicalReportVersionItem>>(
      [.. VersionItems.Where(item => item.ReportId == reportId).OrderBy(item => item.VersionSequence)]);

  /// <inheritdoc/>
  public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) =>
    Task.FromResult(VersionDetails.TryGetValue((reportId, reportVersionId), out MedicalReportVersionDetailItem? detail) ? detail : null);

  /// <inheritdoc/>
  public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) =>
    Task.FromResult(ReportScopes.TryGetValue(reportId, out MedicalReportScopeItem? scope) ? scope : null);

  /// <inheritdoc/>
  public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) =>
    Task.FromResult(VersionCommons.TryGetValue(reportVersionId, out MedicalRecognitionReportDetailCommon? common) ? common : null);

  /// <inheritdoc/>
  public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) =>
    Task.FromResult(LaboratoryContents.TryGetValue(reportVersionId, out LaboratoryReportContentItem? content) ? content : null);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) =>
    Task.FromResult<IEnumerable<LaboratoryResultItemView>>(
      [.. LaboratoryResultItems.TryGetValue(reportVersionId, out List<LaboratoryResultItemView>? items) ? items : []]);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) =>
    Task.FromResult<IEnumerable<LaboratoryBacteriaResultItem>>(
      [.. BacteriaItems.TryGetValue(reportVersionId, out List<LaboratoryBacteriaResultItem>? items) ? items : []]);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) =>
    Task.FromResult<IEnumerable<LaboratorySusceptibilityItem>>(
      [.. SusceptibilityItems.TryGetValue(bacteriaResultId, out List<LaboratorySusceptibilityItem>? items) ? items : []]);

  /// <inheritdoc/>
  public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) =>
    Task.FromResult(ExaminationContents.TryGetValue(reportVersionId, out ExaminationReportContentItem? content) ? content : null);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) =>
    Task.FromResult<IEnumerable<ExaminationItemView>>(
      [.. ExaminationItemViews.TryGetValue(reportVersionId, out List<ExaminationItemView>? items) ? items : []]);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
    Task.FromResult<IEnumerable<ExaminationSiteView>>(
      [.. ExaminationSiteViews.TryGetValue(examinationItemId, out List<ExaminationSiteView>? items) ? items : []]);

  /// <inheritdoc/>
  public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId)
  {
    if (!ReportScopes.TryGetValue(reportId, out MedicalReportScopeItem? scope)) return Task.FromResult<MedicalReportVersionFileItem?>(null);
    if (!VersionDetails.ContainsKey((reportId, reportVersionId))) return Task.FromResult<MedicalReportVersionFileItem?>(null);

    return Task.FromResult<MedicalReportVersionFileItem?>(new MedicalReportVersionFileItem
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId,
      OrganizationCode = scope.OrganizationCode,
      HospitalCode = scope.HospitalCode,
      BranchCode = scope.BranchCode,
      PdfFileId = $"key/{reportVersionId:N}.pdf",
      PdfFileName = "v1.pdf",
      ReportStatus = MedicalReportLifecycleStatus.Effective
    });
  }

  /// <summary>本替身不覆盖：分类列表查询。</summary>
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
}
