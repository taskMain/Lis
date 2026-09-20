using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 互认匹配所需只读投影的内存替身：负责候选报告与报告事实读取，以及互认配置列表读取。
/// </summary>
/// <remarks>
/// 只实现互认匹配查询实际使用的读取，其余成员调用即失败，避免用例在未覆盖的路径上静默走过。
/// 候选报告按标准项目编码集合筛选后按输入顺序返回，排序由用例预置的数据顺序表达；
/// 报告事实与项目级内容按版本标识索引读取，调用次数被记录，供用例断言读取次数不随命中项目数增长。
/// </remarks>
internal sealed class FakeMatchQueryRepository : IMedicalRecognitionReportQueryRepository
{
  /// <summary>未使用的查询成员统一提示。</summary>
  private const string UnusedMember = "本替身不覆盖该查询成员。";

  /// <summary>互认配置列表读取结果；按组织编码预置，读取时按组织编码返回。</summary>
  public Dictionary<string, List<RecognitionProjectConfigurationListItem>> ConfigurationsByOrganization { get; } = [];

  /// <summary>候选报告行；按标准项目编码集合与平台患者筛选后返回。</summary>
  public List<RecognitionMatchCandidateReportItem> CandidateReports { get; } = [];

  /// <summary>各候选报告主体所属的平台患者标识；按报告标识索引，表达报告主体与平台患者的归属。</summary>
  private Dictionary<Guid, Guid> CandidateReportPatientIds { get; } = [];

  /// <summary>当前已解析到的平台患者标识；用例预置候选报告时未显式指定患者则取该值。</summary>
  public Guid CandidatePatientId { get; set; }

  /// <summary>报告事实行；按报告版本标识索引。</summary>
  public Dictionary<Guid, RecognitionMatchReportFactsItem> ReportFactsByVersion { get; } = [];

  /// <summary>普通检验结果明细；按报告版本标识索引。</summary>
  public Dictionary<Guid, List<LaboratoryResultItemView>> LaboratoryResultsByVersion { get; } = [];

  /// <summary>检查项目；按报告版本标识索引。</summary>
  public Dictionary<Guid, List<ExaminationItemView>> ExaminationItemsByVersion { get; } = [];

  /// <summary>检查部位；按检查项目标识索引。</summary>
  public Dictionary<Guid, List<ExaminationSiteView>> ExaminationSitesByItem { get; } = [];

  /// <summary>仍为当前有效版本的报告版本标识集合；未登记的版本按已失效处理，与语句只返回有效版本的语义一致。</summary>
  public HashSet<Guid> ValidReportVersionIds { get; } = [];

  /// <summary>引用详情候选采纳记录；按可信接收三值、患者证件与本次来源就诊筛选后返回。</summary>
  public List<RecognitionCitationCandidateItem> CitationCandidates { get; } = [];

  /// <summary>引用详情的报告公共上下文行；按报告版本标识索引。</summary>
  public Dictionary<Guid, RecognitionCitationReportContextItem> CitationReportContextsByVersion { get; } = [];

  /// <summary>引用详情的标准项目名称行；按标准项目编码索引，未登记的标准项目按名称缺失处理。</summary>
  public Dictionary<string, string> StandardProjectNamesByCode { get; } = [];

  /// <summary>互认配置列表读取的调用次数。</summary>
  public int ConfigurationQueryCalls { get; private set; }

  /// <summary>
  /// 取某组织下某标准项目在标准目录中的名称，供用例按预置值断言标准项目名称的取值来源。
  /// </summary>
  /// <param name="organizationCode">配置归属组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>该组织下该编码的标准项目名称。</returns>
  /// <exception cref="InvalidOperationException">该组织下不存在该编码的互认配置时抛出，避免用例在错误的预置数据上取得期望值。</exception>
  public string GetStandardItemName(string organizationCode, string standardProjectCode)
  {
    RecognitionProjectConfigurationListItem configuration = ConfigurationsByOrganization[organizationCode]
      .Single(item => string.Equals(item.StandardProjectCode, standardProjectCode, StringComparison.Ordinal));

    return configuration.StandardItemName;
  }

  /// <summary>登记一条某组织下某标准项目的互认配置，并返回该配置行。</summary>
  /// <param name="organizationCode">配置归属组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="isValid">配置自身是否启用。</param>
  /// <param name="itemType">标准项目所属分类的项目类型。</param>
  /// <param name="recognitionDurationDays">可互认时间天数。</param>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <returns>登记后的配置行，供用例继续调整取值。</returns>
  public RecognitionProjectConfigurationListItem AddConfiguration(
    string organizationCode,
    string standardProjectCode,
    bool isValid = true,
    MedicalItemType itemType = MedicalItemType.Laboratory,
    int recognitionDurationDays = 30,
    bool categoryIsValid = true,
    bool groupIsValid = true,
    bool itemIsValid = true)
  {
    RecognitionProjectConfigurationListItem configuration = new()
    {
      ConfigurationId = Guid.NewGuid(),
      StandardProjectCode = standardProjectCode,
      StandardItemName = $"{standardProjectCode} 名称",
      ItemType = itemType,
      CategoryName = "分类",
      GroupName = "分组",
      RecognitionDurationDays = recognitionDurationDays,
      IsValid = isValid,
      CategoryIsValid = categoryIsValid,
      GroupIsValid = groupIsValid,
      ItemIsValid = itemIsValid
    };

    if (!ConfigurationsByOrganization.TryGetValue(organizationCode, out List<RecognitionProjectConfigurationListItem>? configurations))
    {
      configurations = [];
      ConfigurationsByOrganization[organizationCode] = configurations;
    }

    configurations.Add(configuration);
    return configuration;
  }

  /// <summary>登记一条检验报告的候选行，并返回该行供用例继续调整取值。</summary>
  /// <param name="standardProjectCode">本次命中的标准项目编码。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <param name="sourceHospitalCode">来源医院编码。</param>
  /// <param name="matchBaselineTime">匹配基准时间；传空表示该报告版本没有可用的业务时间。</param>
  /// <param name="reportTime">报告时间。</param>
  /// <param name="patientId">该报告主体所属的平台患者标识；传空表示取当前已解析到的患者。</param>
  /// <returns>登记后的候选行。</returns>
  public RecognitionMatchCandidateReportItem AddLaboratoryCandidate(
    string standardProjectCode,
    Guid reportId,
    Guid reportVersionId,
    string sourceHospitalCode,
    DateTime? matchBaselineTime,
    DateTime reportTime,
    Guid? patientId = null)
  {
    RecognitionMatchCandidateReportItem candidate = new()
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId,
      SourceHospitalCode = sourceHospitalCode,
      ItemType = MedicalItemType.Laboratory,
      StandardProjectCode = standardProjectCode,
      MatchBaselineTime = matchBaselineTime,
      ReportTime = reportTime
    };
    CandidateReports.Add(candidate);
    CandidateReportPatientIds[reportId] = patientId ?? CandidatePatientId;
    return candidate;
  }

  /// <summary>候选报告读取的调用次数。</summary>
  public int CandidateQueryCalls { get; private set; }

  /// <summary>最近一次候选报告读取收到的平台患者标识；管理器必须把本次解析到的患者交给候选查询。</summary>
  public Guid? CandidateQueryPatientId { get; private set; }

  /// <summary>报告事实读取的调用次数。</summary>
  public int ReportFactsQueryCalls { get; private set; }

  /// <summary>报告版本有效性读取的调用次数。</summary>
  public int ValidReportVersionQueryCalls { get; private set; }

  /// <summary>普通检验结果读取的调用次数；按版本集合读取与按单版本读取都计入。</summary>
  public int LaboratoryResultQueryCalls { get; private set; }

  /// <summary>检查项目读取的调用次数；按版本集合读取与按单版本读取都计入。</summary>
  public int ExaminationItemQueryCalls { get; private set; }

  /// <summary>检查部位集合读取的调用次数。</summary>
  public int ExaminationSiteQueryCalls { get; private set; }

  /// <summary>引用详情候选采纳记录读取的调用次数。</summary>
  public int CitationQueryCalls { get; private set; }

  /// <summary>引用详情报告公共上下文读取的调用次数。</summary>
  public int CitationContextQueryCalls { get; private set; }

  /// <summary>引用详情标准项目名称读取的调用次数。</summary>
  public int CitationStandardProjectNameQueryCalls { get; private set; }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(
    string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus)
  {
    ConfigurationQueryCalls++;
    return Task.FromResult<IEnumerable<RecognitionProjectConfigurationListItem>>(
      ConfigurationsByOrganization.TryGetValue(organizationCode, out List<RecognitionProjectConfigurationListItem>? configurations)
        ? configurations
        : []);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    Guid patientId,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo,
    IReadOnlyList<string> standardProjectCodes)
  {
    CandidateQueryCalls++;
    CandidateQueryPatientId = patientId;
    return Task.FromResult<IEnumerable<RecognitionMatchCandidateReportItem>>(
    [
      .. CandidateReports
        .Where(candidate => standardProjectCodes.Contains(candidate.StandardProjectCode, StringComparer.Ordinal))
        // 候选范围限定为本次解析到的平台患者：报告主体未关联该患者时不返回该候选。
        .Where(candidate => CandidateReportPatientIds.TryGetValue(candidate.ReportId, out Guid candidatePatientId) && candidatePatientId == patientId)
    ]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    ReportFactsQueryCalls++;
    return Task.FromResult<IEnumerable<RecognitionMatchReportFactsItem>>(
      [.. reportVersionIds.Where(ReportFactsByVersion.ContainsKey).Select(reportVersionId => ReportFactsByVersion[reportVersionId])]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    ValidReportVersionQueryCalls++;
    return Task.FromResult<IEnumerable<RecognitionValidReportVersionItem>>(
      [.. reportVersionIds.Where(ValidReportVersionIds.Contains).Order().Select(reportVersionId => new RecognitionValidReportVersionItem { Id = reportVersionId })]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId)
  {
    LaboratoryResultQueryCalls++;
    return Task.FromResult<IEnumerable<LaboratoryResultItemView>>(
      [.. LaboratoryResultsByVersion.TryGetValue(reportVersionId, out List<LaboratoryResultItemView>? items) ? items : []]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    LaboratoryResultQueryCalls++;
    return Task.FromResult<IEnumerable<LaboratoryResultItemView>>(
      [.. reportVersionIds.Where(LaboratoryResultsByVersion.ContainsKey).SelectMany(reportVersionId => LaboratoryResultsByVersion[reportVersionId])]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId)
  {
    ExaminationItemQueryCalls++;
    return Task.FromResult<IEnumerable<ExaminationItemView>>(
      [.. ExaminationItemsByVersion.TryGetValue(reportVersionId, out List<ExaminationItemView>? items) ? items : []]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    ExaminationItemQueryCalls++;
    return Task.FromResult<IEnumerable<ExaminationItemView>>(
      [.. reportVersionIds.Where(ExaminationItemsByVersion.ContainsKey).SelectMany(reportVersionId => ExaminationItemsByVersion[reportVersionId])]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
    Task.FromResult<IEnumerable<ExaminationSiteView>>(
      [.. ExaminationSitesByItem.TryGetValue(examinationItemId, out List<ExaminationSiteView>? sites) ? sites : []]);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds)
  {
    ExaminationSiteQueryCalls++;
    return Task.FromResult<IEnumerable<ExaminationSiteView>>(
      [.. examinationItemIds.Where(ExaminationSitesByItem.ContainsKey).SelectMany(examinationItemId => ExaminationSitesByItem[examinationItemId])]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo)
  {
    CitationQueryCalls++;
    CitationQueryOrganizationCode = organizationCode;
    CitationQueryHospitalCode = hospitalCode;
    CitationQueryBranchCode = branchCode;
    return Task.FromResult<IEnumerable<RecognitionCitationCandidateItem>>(
    [
      .. CitationCandidates
        // 候选范围限定为本次可信接收三值与患者证件、来源就诊：任一项不一致的记录都不返回。
        .Where(candidate => string.Equals(candidate.ReceiverOrganizationCode, organizationCode, StringComparison.Ordinal)
          && string.Equals(candidate.ReceiverHospitalCode, hospitalCode, StringComparison.Ordinal)
          && string.Equals(candidate.ReceiverBranchCode, branchCode, StringComparison.Ordinal)
          && string.Equals(candidate.IdentityDocumentTypeCode, identityDocumentTypeCode, StringComparison.Ordinal)
          && string.Equals(candidate.IdentityDocumentNo, identityDocumentNo, StringComparison.Ordinal)
          && candidate.VisitType == visitType
          && string.Equals(candidate.VisitSerialNo, visitSerialNo, StringComparison.Ordinal))
        .OrderByDescending(candidate => candidate.DecisionSavedTime)
        .ThenBy(candidate => candidate.RecognitionMatchItemId)
    ]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    CitationContextQueryCalls++;
    return Task.FromResult<IEnumerable<RecognitionCitationReportContextItem>>(
    [
      .. reportVersionIds
        .Where(CitationReportContextsByVersion.ContainsKey)
        .Select(reportVersionId => CitationReportContextsByVersion[reportVersionId])
        .OrderBy(context => context.ReportVersionId)
    ]);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(
    IReadOnlyList<string> standardProjectCodes)
  {
    CitationStandardProjectNameQueryCalls++;
    return Task.FromResult<IEnumerable<CitationStandardProjectNameItem>>(
    [
      .. standardProjectCodes
        .Where(StandardProjectNamesByCode.ContainsKey)
        .Select(code => new CitationStandardProjectNameItem { StandardProjectCode = code, StandardProjectName = StandardProjectNamesByCode[code] })
        .OrderBy(item => item.StandardProjectCode, StringComparer.Ordinal)
    ]);
  }

  /// <summary>最近一次引用详情候选读取收到的可信组织编码。</summary>
  public string? CitationQueryOrganizationCode { get; private set; }

  /// <summary>最近一次引用详情候选读取收到的可信医院编码。</summary>
  public string? CitationQueryHospitalCode { get; private set; }

  /// <summary>最近一次引用详情候选读取收到的可信院区编码。</summary>
  public string? CitationQueryBranchCode { get; private set; }

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);
}
