using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告采集与生命周期的读写实现：报告、报告版本、平台患者与七类专项内容明细。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态与存在性，均由领域层判定；
/// 报告业务键、报告版本序号与患者证件键的并发冲突由数据库唯一索引拒绝，数据库异常按原样向外传播。
/// 每个调用点都显式传作用域，语句标识由调用方法名推导，不使用共享映射器的上下文设置方法。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 报告语句集的作用域名；与 <c>MedicalRecognitionReport.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  private const string MedicalRecognitionReportScope = "MedicalRecognitionReport";
  /// <summary>
  /// 报告版本语句集的作用域名；与 <c>MedicalReportVersion.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  private const string MedicalReportVersionScope = "MedicalReportVersion";
  /// <summary>
  /// 平台患者语句集的作用域名；与 <c>PlatformPatient.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  private const string PlatformPatientScope = "PlatformPatient";
  /// <summary>
  /// 检验专项内容语句集的作用域名。
  /// </summary>
  private const string LaboratoryReportContentScope = "LaboratoryReportContent";
  /// <summary>
  /// 普通检验结果语句集的作用域名。
  /// </summary>
  private const string LaboratoryResultItemScope = "LaboratoryResultItem";
  /// <summary>
  /// 细菌鉴定结果语句集的作用域名。
  /// </summary>
  private const string LaboratoryBacteriaResultScope = "LaboratoryBacteriaResult";
  /// <summary>
  /// 药敏结果语句集的作用域名。
  /// </summary>
  private const string LaboratoryAntimicrobialSusceptibilityScope = "LaboratoryAntimicrobialSusceptibility";
  /// <summary>
  /// 检查专项内容语句集的作用域名。
  /// </summary>
  private const string ExaminationReportContentScope = "ExaminationReportContent";
  /// <summary>
  /// 检查项目语句集的作用域名。
  /// </summary>
  private const string ExaminationItemScope = "ExaminationItem";
  /// <summary>
  /// 检查部位语句集的作用域名。
  /// </summary>
  private const string ExaminationSiteScope = "ExaminationSite";

  /// <inheritdoc/>
  public async Task<PlatformPatient?> GetPlatformPatientByDocumentAsync(string identityDocumentTypeCode, string identityDocumentNo) =>
    await dataMapper.QuerySingleAsync<PlatformPatient>(
      new { IdentityDocumentTypeCode = identityDocumentTypeCode, IdentityDocumentNo = identityDocumentNo },
      scope: PlatformPatientScope);

  /// <summary>
  /// 插入一个平台患者。
  /// </summary>
  /// <param name="value">待保存的患者实体，携带规范形式后的证件键与核心身份字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreatePlatformPatientAsync(PlatformPatient value) =>
    await dataMapper.InsertAsync(value, scope: PlatformPatientScope);
  /// <inheritdoc/>
  public async Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByIdAsync(Guid id) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReport>(new { Id = id }, scope: MedicalRecognitionReportScope);

  /// <inheritdoc/>
  public async Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReport>(
      new { OrganizationCode = organizationCode, HospitalCode = hospitalCode, BranchCode = branchCode, ReportType = reportType, ReportNo = reportNo },
      scope: MedicalRecognitionReportScope);

  /// <summary>
  /// 插入一份当前有效的报告。
  /// </summary>
  /// <param name="value">待保存的报告实体，携带当前版本指向与三个检索列。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateMedicalRecognitionReportAsync(MedicalRecognitionReport value) =>
    await dataMapper.InsertAsync(value, scope: MedicalRecognitionReportScope);

  /// <inheritdoc/>
  public async Task<int> UpdateMedicalRecognitionReportCurrentVersionAsync(MedicalRecognitionReport value) =>
    await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope);

  /// <inheritdoc/>
  public async Task<int> VoidMedicalRecognitionReportAsync(MedicalRecognitionReport value) =>
    await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope);

  /// <inheritdoc/>
  public async Task<int> GetMedicalReportMaxVersionNumberAsync(Guid reportId) =>
    await dataMapper.QuerySingleAsync<int>(new { ReportId = reportId }, scope: MedicalReportVersionScope);

  /// <inheritdoc/>
  public async Task<MedicalReportVersion?> GetMedicalReportVersionByIdAsync(Guid id) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersion>(new { Id = id }, scope: MedicalReportVersionScope);

  /// <summary>
  /// 插入一个报告版本。
  /// </summary>
  /// <param name="value">待保存的版本实体，携带版本序号、公共信息、文件键与下载名。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateMedicalReportVersionAsync(MedicalReportVersion value) =>
    await dataMapper.InsertAsync(value, scope: MedicalReportVersionScope);

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryReportContentAsync(LaboratoryReportContent value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryReportContentScope);

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryResultItemAsync(LaboratoryResultItem value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryResultItemScope);

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryBacteriaResultAsync(LaboratoryBacteriaResult value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryBacteriaResultScope);

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryAntimicrobialSusceptibilityAsync(LaboratoryAntimicrobialSusceptibility value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryAntimicrobialSusceptibilityScope);

  /// <inheritdoc/>
  public async Task<int> CreateExaminationReportContentAsync(ExaminationReportContent value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationReportContentScope);

  /// <inheritdoc/>
  public async Task<int> CreateExaminationItemAsync(ExaminationItem value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationItemScope);

  /// <inheritdoc/>
  public async Task<int> CreateExaminationSiteAsync(ExaminationSite value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationSiteScope);
}
