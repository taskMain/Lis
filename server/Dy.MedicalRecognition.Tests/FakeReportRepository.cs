using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 报告提交与作废的内存仓储替身：保存报告、版本、平台患者与七类专项明细，记录调用次数并按用例脚本注入失败。
/// </summary>
/// <remarks>
/// 替身只用于隔离数据库：写入按内存字典进行，读取按业务键或主键定位，影响行数按是否命中计算。
/// 未参与本阶段用例的目录与金额成员一律抛出 <see cref="NotSupportedException"/>，避免测试静默走过未覆盖的路径。
/// </remarks>
internal sealed class FakeReportRepository : IMedicalRecognitionReportRepository
{
  /// <summary>未使用的仓储成员统一提示。</summary>
  private const string UnusedMember = "本替身不覆盖该仓储成员。";

  /// <summary>内存平台患者，按主键保存。</summary>
  public Dictionary<Guid, PlatformPatient> Patients { get; } = [];

  /// <summary>内存报告，按主键保存。</summary>
  public Dictionary<Guid, MedicalRecognitionReport> Reports { get; } = [];

  /// <summary>内存报告版本，按主键保存。</summary>
  public Dictionary<Guid, MedicalReportVersion> Versions { get; } = [];

  /// <summary>内存检验专项内容。</summary>
  public List<LaboratoryReportContent> LaboratoryContents { get; } = [];

  /// <summary>内存普通检验结果。</summary>
  public List<LaboratoryResultItem> LaboratoryResultItems { get; } = [];

  /// <summary>内存细菌鉴定结果。</summary>
  public List<LaboratoryBacteriaResult> BacteriaResults { get; } = [];

  /// <summary>内存药敏结果。</summary>
  public List<LaboratoryAntimicrobialSusceptibility> Susceptibilities { get; } = [];

  /// <summary>内存检查专项内容。</summary>
  public List<ExaminationReportContent> ExaminationContents { get; } = [];

  /// <summary>内存检查项目。</summary>
  public List<ExaminationItem> ExaminationItems { get; } = [];

  /// <summary>内存检查部位。</summary>
  public List<ExaminationSite> ExaminationSites { get; } = [];

  /// <summary>内存互认匹配记录，按主键保存。</summary>
  public Dictionary<Guid, RecognitionMatchRecord> MatchRecords { get; } = [];

  /// <summary>内存互认匹配项，按主键保存。</summary>
  public Dictionary<Guid, RecognitionMatchItem> MatchItems { get; } = [];

  /// <summary>内存互认处理结果，按主键保存。</summary>
  public Dictionary<Guid, RecognitionProcessingResult> ProcessingResults { get; } = [];

  /// <summary>内存互认引用事实，按主键保存。</summary>
  public Dictionary<Guid, RecognitionReference> References { get; } = [];

  /// <summary>内存组织医院院区互认项目金额，按四个业务键保存。</summary>
  public Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode, string StandardProjectCode), OrganizationHospitalBranchRecognitionAmount> Amounts { get; } = [];

  /// <summary>互认处理结果按所属记录读取的调用次数；幂等命中与失败路径都不应读取金额，金额读取次数据此分开观察。</summary>
  public int ProcessingResultQueryCalls { get; private set; }

  /// <summary>互认匹配项按所属记录读取的调用次数。</summary>
  public int MatchItemQueryCalls { get; private set; }

  /// <summary>采纳项目当前金额按业务键读取的调用次数；不采纳项目不应产生任何读取。</summary>
  public int AmountReadCalls { get; private set; }

  /// <summary>处理结果保存时间写入的调用次数。</summary>
  public int DecisionSavedTimeUpdateCalls { get; private set; }

  /// <summary>最近一次写入的处理结果保存时间。</summary>
  public DateTime? LastDecisionSavedTime { get; private set; }

  /// <summary>为真时检验专项内容写入按影响行数 0 失败。</summary>
  public bool FailLaboratoryContentWrite { get; set; }

  /// <summary>为真时检查专项内容写入按影响行数 0 失败。</summary>
  public bool FailExaminationContentWrite { get; set; }

  /// <summary>为真时互认匹配记录写入按影响行数 0 失败。</summary>
  public bool FailMatchRecordWrite { get; set; }

  /// <summary>为真时互认匹配项写入按影响行数 0 失败。</summary>
  public bool FailMatchItemWrite { get; set; }

  /// <summary>为真时互认处理结果写入按影响行数 0 失败，用于构造整批保存中的行数守卫失败。</summary>
  public bool FailProcessingResultWrite { get; set; }

  /// <summary>为真时处理结果保存时间写入按影响行数 0 失败，用于构造匹配记录更新上的行数守卫失败。</summary>
  public bool FailDecisionSavedTimeWrite { get; set; }

  /// <summary>为真时引用事实写入按影响行数 0 失败，用于构造整批保存中的行数守卫失败。</summary>
  public bool FailReferenceWrite { get; set; }

  /// <summary>生成一个新的实体主键。</summary>
  /// <returns>新的主键标识。</returns>
  public Guid CreateGuid() => Guid.NewGuid();

  /// <inheritdoc/>
  public Task<PlatformPatient?> GetPlatformPatientByDocumentAsync(string identityDocumentTypeCode, string identityDocumentNo)
  {
    PlatformPatient? stored = Patients.Values.SingleOrDefault(patient =>
      patient.IdentityDocumentTypeCode == identityDocumentTypeCode && patient.IdentityDocumentNo == identityDocumentNo);
    return Task.FromResult(stored);
  }

  /// <inheritdoc/>
  public Task<int> CreatePlatformPatientAsync(PlatformPatient platformPatient)
  {
    Patients[platformPatient.Id] = platformPatient;
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByIdAsync(Guid id) =>
    Task.FromResult(Reports.TryGetValue(id, out MedicalRecognitionReport? report) ? report : null);

  /// <inheritdoc/>
  public Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo) =>
    Task.FromResult(Reports.Values.SingleOrDefault(report =>
      report.OrganizationCode == organizationCode && report.HospitalCode == hospitalCode && report.BranchCode == branchCode
      && report.ReportType == reportType && report.ReportNo == reportNo));

  /// <inheritdoc/>
  public Task<int> CreateMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport)
  {
    Reports[medicalRecognitionReport.Id] = medicalRecognitionReport;
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> UpdateMedicalRecognitionReportCurrentVersionAsync(MedicalRecognitionReport medicalRecognitionReport)
  {
    if (!Reports.TryGetValue(medicalRecognitionReport.Id, out MedicalRecognitionReport? stored)) return Task.FromResult(0);
    stored.CurrentVersionId = medicalRecognitionReport.CurrentVersionId;
    stored.ReportTime = medicalRecognitionReport.ReportTime;
    stored.PatientName = medicalRecognitionReport.PatientName;
    stored.IdentityDocumentNo = medicalRecognitionReport.IdentityDocumentNo;
    stored.PatientId = medicalRecognitionReport.PatientId;
    stored.OperId = medicalRecognitionReport.OperId;
    stored.OperTime = medicalRecognitionReport.OperTime;
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> VoidMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport)
  {
    if (!Reports.TryGetValue(medicalRecognitionReport.Id, out MedicalRecognitionReport? stored)) return Task.FromResult(0);
    // 条件更新带原有效状态：已作废时影响 0 行，调用方据此区分幂等与冲突。
    if (stored.Status != MedicalReportLifecycleStatus.Effective) return Task.FromResult(0);
    stored.Status = medicalRecognitionReport.Status;
    stored.VoidedTime = medicalRecognitionReport.VoidedTime;
    stored.VoidReason = medicalRecognitionReport.VoidReason;
    stored.OperId = medicalRecognitionReport.OperId;
    stored.OperTime = medicalRecognitionReport.OperTime;
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> GetMedicalReportMaxVersionNumberAsync(Guid reportId) =>
    Task.FromResult(Versions.Values.Where(version => version.ReportId == reportId)
      .Select(version => version.VersionNumber).DefaultIfEmpty(0).Max());

  /// <inheritdoc/>
  public Task<MedicalReportVersion?> GetMedicalReportVersionByIdAsync(Guid id) =>
    Task.FromResult(Versions.TryGetValue(id, out MedicalReportVersion? version) ? version : null);

  /// <inheritdoc/>
  public Task<int> CreateMedicalReportVersionAsync(MedicalReportVersion medicalReportVersion)
  {
    Versions[medicalReportVersion.Id] = medicalReportVersion;
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateLaboratoryReportContentAsync(LaboratoryReportContent laboratoryReportContent)
  {
    if (FailLaboratoryContentWrite) return Task.FromResult(0);
    LaboratoryContents.Add(laboratoryReportContent);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateLaboratoryResultItemAsync(LaboratoryResultItem laboratoryResultItem)
  {
    LaboratoryResultItems.Add(laboratoryResultItem);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateLaboratoryBacteriaResultAsync(LaboratoryBacteriaResult laboratoryBacteriaResult)
  {
    BacteriaResults.Add(laboratoryBacteriaResult);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateLaboratoryAntimicrobialSusceptibilityAsync(LaboratoryAntimicrobialSusceptibility laboratoryAntimicrobialSusceptibility)
  {
    Susceptibilities.Add(laboratoryAntimicrobialSusceptibility);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateExaminationReportContentAsync(ExaminationReportContent examinationReportContent)
  {
    if (FailExaminationContentWrite) return Task.FromResult(0);
    ExaminationContents.Add(examinationReportContent);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateExaminationItemAsync(ExaminationItem examinationItem)
  {
    ExaminationItems.Add(examinationItem);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> CreateExaminationSiteAsync(ExaminationSite examinationSite)
  {
    ExaminationSites.Add(examinationSite);
    return Task.FromResult(1);
  }

  /// <summary>本替身不覆盖：新增分类。</summary>
  /// <param name="medicalStandardCategory">分类实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> CreateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：修改分类。</summary>
  /// <param name="medicalStandardCategory">分类实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> UpdateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：启用分类。</summary>
  /// <param name="enableMedicalStandardCategoryCommand">启用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand enableMedicalStandardCategoryCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：停用分类。</summary>
  /// <param name="disableMedicalStandardCategoryCommand">停用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand disableMedicalStandardCategoryCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：新增分组。</summary>
  /// <param name="medicalStandardGroup">分组实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> CreateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：修改分组。</summary>
  /// <param name="medicalStandardGroup">分组实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> UpdateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：启用分组。</summary>
  /// <param name="enableMedicalStandardGroupCommand">启用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand enableMedicalStandardGroupCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：停用分组。</summary>
  /// <param name="disableMedicalStandardGroupCommand">停用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand disableMedicalStandardGroupCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：新增标准项目。</summary>
  /// <param name="medicalStandardItem">标准项目实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> CreateMedicalStandardItemAsync(MedicalStandardItem medicalStandardItem) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：修改标准项目备注。</summary>
  /// <param name="medicalStandardItem">标准项目实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> ChangeMedicalStandardItemRemarkAsync(MedicalStandardItem medicalStandardItem) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：启用标准项目。</summary>
  /// <param name="enableMedicalStandardItemCommand">启用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand enableMedicalStandardItemCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：停用标准项目。</summary>
  /// <param name="disableMedicalStandardItemCommand">停用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand disableMedicalStandardItemCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按主键读取分类。</summary>
  /// <param name="id">分类标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：分类名称查重。</summary>
  /// <param name="name">分类名称。</param>
  /// <param name="excludedId">排除标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<bool> MedicalStandardCategoryNameExistsAsync(string name, Guid? excludedId = null) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：分类下级分组判断。</summary>
  /// <param name="categoryId">分类标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<bool> MedicalStandardCategoryHasGroupsAsync(Guid categoryId) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按主键读取分组。</summary>
  /// <param name="id">分组标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：分组名称查重。</summary>
  /// <param name="categoryId">分类标识。</param>
  /// <param name="name">分组名称。</param>
  /// <param name="excludedId">排除标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<bool> MedicalStandardGroupNameExistsAsync(Guid categoryId, string name, Guid? excludedId = null) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按主键读取标准项目。</summary>
  /// <param name="id">标准项目标识。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按编码读取标准项目。</summary>
  /// <param name="code">标准项目编码。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：标准项目编码查重。</summary>
  /// <param name="code">标准项目编码。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<bool> MedicalStandardItemCodeExistsAsync(string code) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：新增互认配置。</summary>
  /// <param name="mutualRecognitionItem">互认配置实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem mutualRecognitionItem) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按标识与组织读取互认配置。</summary>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">组织编码。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：修改互认配置天数。</summary>
  /// <param name="mutualRecognitionItem">互认配置实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem mutualRecognitionItem) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：启用互认配置。</summary>
  /// <param name="enableMutualRecognitionItemCommand">启用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand enableMutualRecognitionItemCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：停用互认配置。</summary>
  /// <param name="disableMutualRecognitionItemCommand">停用命令。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand disableMutualRecognitionItemCommand) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：按组织与标准项目读取互认配置。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<MutualRecognitionItem?> GetMutualRecognitionItemByOrganizationAndProjectAsync(string organizationCode, string standardProjectCode) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：新增组织医院院区互认项目金额。</summary>
  /// <param name="organizationHospitalBranchRecognitionAmount">金额实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> CreateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount) => throw new NotSupportedException(UnusedMember);

  /// <summary>本替身不覆盖：更新组织医院院区互认项目金额。</summary>
  /// <param name="organizationHospitalBranchRecognitionAmount">金额实体。</param>
  /// <returns>不返回结果。</returns>
  /// <exception cref="NotSupportedException">本替身不覆盖该成员时抛出。</exception>
  public Task<int> UpdateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount) => throw new NotSupportedException(UnusedMember);

  /// <summary>插入一条互认匹配记录，处理结果保存时间按领域层写入的空值保存。</summary>
  /// <param name="recognitionMatchRecord">互认匹配记录实体。</param>
  /// <returns>受影响行数：为真时按 0 行失败，否则写入并返回 1。</returns>
  public Task<int> CreateRecognitionMatchRecordAsync(RecognitionMatchRecord recognitionMatchRecord)
  {
    if (FailMatchRecordWrite) return Task.FromResult(0);
    MatchRecords[recognitionMatchRecord.Id] = recognitionMatchRecord;
    return Task.FromResult(1);
  }

  /// <summary>插入一条互认匹配项。</summary>
  /// <param name="recognitionMatchItem">互认匹配项实体。</param>
  /// <returns>受影响行数：为真时按 0 行失败，否则写入并返回 1。</returns>
  public Task<int> CreateRecognitionMatchItemAsync(RecognitionMatchItem recognitionMatchItem)
  {
    if (FailMatchItemWrite) return Task.FromResult(0);
    MatchItems[recognitionMatchItem.Id] = recognitionMatchItem;
    return Task.FromResult(1);
  }

  /// <summary>按主键读取一条互认匹配记录。</summary>
  /// <param name="id">互认匹配记录标识。</param>
  /// <returns>匹配记录；记录不存在时返回 <see langword="null"/>。</returns>
  public Task<RecognitionMatchRecord?> GetRecognitionMatchRecordByIdAsync(Guid id) =>
    Task.FromResult(MatchRecords.TryGetValue(id, out RecognitionMatchRecord? record) ? record : null);

  /// <summary>按所属互认匹配记录读取该组已保存的处理结果。</summary>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
  /// <returns>该组处理结果，按匹配项标识升序；尚无处理结果时为空集合。</returns>
  public Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByRecordAsync(Guid recognitionMatchRecordId)
  {
    ProcessingResultQueryCalls++;
    IReadOnlyList<RecognitionProcessingResult> results =
    [
      .. ProcessingResults.Values
        .Where(result => result.RecognitionMatchRecordId == recognitionMatchRecordId)
        .OrderBy(result => result.RecognitionMatchItemId)
    ];
    return Task.FromResult(results);
  }

  /// <summary>按所属互认匹配记录读取该组已保存的互认匹配项。</summary>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
  /// <returns>该组匹配项，按匹配项标识升序；没有匹配项时为空集合。</returns>
  public Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByRecordAsync(Guid recognitionMatchRecordId)
  {
    MatchItemQueryCalls++;
    IReadOnlyList<RecognitionMatchItem> items =
    [
      .. MatchItems.Values
        .Where(item => item.RecognitionMatchRecordId == recognitionMatchRecordId)
        .OrderBy(item => item.Id)
    ];
    return Task.FromResult(items);
  }

  /// <summary>插入一条互认处理结果。</summary>
  /// <param name="recognitionProcessingResult">互认处理结果实体。</param>
  /// <returns>受影响行数：为真时按 0 行失败，否则写入并返回 1。</returns>
  public Task<int> CreateRecognitionProcessingResultAsync(RecognitionProcessingResult recognitionProcessingResult)
  {
    if (FailProcessingResultWrite) return Task.FromResult(0);
    ProcessingResults[recognitionProcessingResult.Id] = recognitionProcessingResult;
    return Task.FromResult(1);
  }

  /// <summary>把该组的处理结果保存时间写入互认匹配记录。</summary>
  /// <remarks>条件与原语句同一口径：主键命中且该列当前为空值时才更新一行，因此重复写入影响 0 行。</remarks>
  /// <param name="recognitionMatchRecord">携带匹配记录标识、本次处理结果保存时间与操作字段的实体。</param>
  /// <returns>受影响行数：首次写入为 1，重复写入或记录不存在为 0。</returns>
  public Task<int> UpdateRecognitionMatchRecordDecisionSavedTimeAsync(RecognitionMatchRecord recognitionMatchRecord)
  {
    DecisionSavedTimeUpdateCalls++;
    LastDecisionSavedTime = recognitionMatchRecord.DecisionSavedTime;
    if (FailDecisionSavedTimeWrite) return Task.FromResult(0);
    if (!MatchRecords.TryGetValue(recognitionMatchRecord.Id, out RecognitionMatchRecord? stored)) return Task.FromResult(0);
    if (stored.DecisionSavedTime is not null) return Task.FromResult(0);

    stored.DecisionSavedTime = recognitionMatchRecord.DecisionSavedTime;
    stored.OperId = recognitionMatchRecord.OperId;
    stored.OperTime = recognitionMatchRecord.OperTime;
    return Task.FromResult(1);
  }

  /// <summary>
  /// 按互认匹配项标识集合读取匹配项，供引用结果提交逐项反查所属匹配记录。
  /// </summary>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的匹配项，按匹配项标识升序。</returns>
  public Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds)
  {
    ReferenceMatchItemsByIdsQueryCalls++;
    IReadOnlyList<RecognitionMatchItem> items = [.. MatchItems.Values.Where(item => recognitionMatchItemIds.Contains(item.Id)).OrderBy(item => item.Id)];
    return Task.FromResult(items);
  }

  /// <summary>
  /// 按互认匹配记录标识集合读取匹配记录，供引用结果提交校验所属记录的接收三值。
  /// </summary>
  /// <param name="recognitionMatchRecordIds">本次提交涉及的全部互认匹配记录标识。</param>
  /// <returns>命中的匹配记录集合，按记录标识升序。</returns>
  public Task<IReadOnlyList<RecognitionMatchRecord>> QueryRecognitionMatchRecordsByIdsAsync(IReadOnlyList<Guid> recognitionMatchRecordIds)
  {
    ReferenceMatchRecordsByIdsQueryCalls++;
    IReadOnlyList<RecognitionMatchRecord> records =
      [.. MatchRecords.Values.Where(record => recognitionMatchRecordIds.Contains(record.Id)).OrderBy(record => record.Id)];
    return Task.FromResult(records);
  }

  /// <summary>
  /// 按互认匹配项标识集合读取处理结果，供引用结果提交逐项反查该项目的决定。
  /// </summary>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的处理结果，按匹配项标识升序。</returns>
  public Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds)
  {
    ReferenceProcessingResultQueryCalls++;
    IReadOnlyList<RecognitionProcessingResult> results =
      [.. ProcessingResults.Values.Where(result => recognitionMatchItemIds.Contains(result.RecognitionMatchItemId)).OrderBy(result => result.RecognitionMatchItemId)];
    return Task.FromResult(results);
  }

  /// <summary>
  /// 按互认匹配项标识集合读取已保存的引用事实，供引用结果提交做幂等识别与冲突判定。
  /// </summary>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的引用事实，按匹配项标识升序。</returns>
  public Task<IReadOnlyList<RecognitionReference>> QueryRecognitionReferencesByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds)
  {
    ReferenceQueryCalls++;
    IReadOnlyList<RecognitionReference> references =
      [.. References.Values.Where(reference => recognitionMatchItemIds.Contains(reference.RecognitionMatchItemId)).OrderBy(reference => reference.RecognitionMatchItemId)];
    return Task.FromResult(references);
  }

  /// <summary>
  /// 插入一条互认引用事实。
  /// </summary>
  /// <param name="recognitionReference">互认引用事实实体。</param>
  /// <returns>受影响行数：<see cref="FailReferenceWrite"/> 为真时按 0 行失败，否则写入并返回 1。</returns>
  public Task<int> CreateRecognitionReferenceAsync(RecognitionReference recognitionReference)
  {
    if (FailReferenceWrite) return Task.FromResult(0);
    References[recognitionReference.Id] = recognitionReference;
    return Task.FromResult(1);
  }

  /// <summary>互认匹配项按标识集合读取的调用次数。</summary>
  public int ReferenceMatchItemsByIdsQueryCalls { get; private set; }

  /// <summary>互认匹配记录按标识集合读取的调用次数。</summary>
  public int ReferenceMatchRecordsByIdsQueryCalls { get; private set; }

  /// <summary>互认处理结果按匹配项标识集合读取的调用次数。</summary>
  public int ReferenceProcessingResultQueryCalls { get; private set; }

  /// <summary>互认引用事实按匹配项标识集合读取的调用次数。</summary>
  public int ReferenceQueryCalls { get; private set; }

  /// <summary>按组织、医院、院区与标准项目四个业务键读取单条金额记录。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>金额记录；该业务键未配置金额时返回 <see langword="null"/>。</returns>
  public Task<OrganizationHospitalBranchRecognitionAmount?> GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, string standardProjectCode)
  {
    AmountReadCalls++;
    return Task.FromResult(Amounts.TryGetValue((organizationCode, hospitalCode, branchCode, standardProjectCode), out OrganizationHospitalBranchRecognitionAmount? amount)
      ? amount
      : null);
  }

  /// <summary>登记一条采纳项目的当前金额，供处理结果提交按业务键读取。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="currentAmount">当前金额。</param>
  public void AddAmount(string organizationCode, string hospitalCode, string branchCode, string standardProjectCode, decimal currentAmount)
  {
    Amounts[(organizationCode, hospitalCode, branchCode, standardProjectCode)] = new OrganizationHospitalBranchRecognitionAmount
    {
      Id = Guid.NewGuid(),
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode,
      CurrentAmount = currentAmount,
      OperId = Guid.Empty,
      OperTime = DateTimeOffset.UnixEpoch
    };
  }
}
