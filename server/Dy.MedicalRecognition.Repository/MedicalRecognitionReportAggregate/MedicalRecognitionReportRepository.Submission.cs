using System.Data.Common;
using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告采集与生命周期的读写实现：报告、报告版本、平台患者与七类专项内容明细。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态与存在性，均由领域层判定；
/// 报告业务键、报告版本序号与患者证件键的唯一约束冲突由本层识别为持久化事实并翻译为对应异常，
/// 是否构成业务拒绝仍由领域层决定，本层不改变对外响应形态。
/// 每个调用点都显式传作用域与语句标识，不使用共享映射器的上下文设置方法。
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
      scope: PlatformPatientScope,
      sqlId: "GetPlatformPatientByDocument");

  /// <inheritdoc/>
  public async Task<(PlatformPatient? Patient, bool IsReadable)> TryGetPlatformPatientByDocumentAsync(string identityDocumentTypeCode, string identityDocumentNo)
  {
    try
    {
      return (await GetPlatformPatientByDocumentAsync(identityDocumentTypeCode, identityDocumentNo), true);
    }
    catch (Exception exception) when (IsAbortedTransaction(exception))
    {
      // 所在事务已被唯一约束冲突终止：本次读取不可用，交由领域层按并发冲突拒绝。
      return (null, false);
    }
  }

  /// <summary>
  /// 插入一个平台患者，并把证件键唯一约束冲突翻译为领域可识别的重复患者异常。
  /// </summary>
  /// <param name="value">待保存的患者实体，携带规范形式后的证件键与核心身份字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  /// <exception cref="DuplicatePlatformPatientException">写入违反“证件类型代码 + 证件号码”唯一约束时抛出。</exception>
  public async Task<int> CreatePlatformPatientAsync(PlatformPatient value)
  {
    try
    {
      return await dataMapper.InsertAsync(value, scope: PlatformPatientScope, sqlId: "InsertPlatformPatient");
    }
    catch (Exception exception) when (IsUniqueConstraintViolation(exception))
    {
      // 唯一约束冲突是持久化事实，这里只翻译异常类型；并发复读与身份核对由领域层完成。
      throw new DuplicatePlatformPatientException("同一证件类型与证件号码的平台患者已存在。", exception);
    }
  }
  /// <inheritdoc/>
  public async Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByIdAsync(Guid id) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReport>(new { Id = id }, scope: MedicalRecognitionReportScope, sqlId: "GetMedicalRecognitionReportById");

  /// <inheritdoc/>
  public async Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReport>(
      new { OrganizationCode = organizationCode, HospitalCode = hospitalCode, BranchCode = branchCode, ReportType = reportType, ReportNo = reportNo },
      scope: MedicalRecognitionReportScope,
      sqlId: "GetMedicalRecognitionReportByBusinessKey");

  /// <summary>
  /// 插入一份当前有效的报告，并把报告业务键唯一约束冲突翻译为领域可识别的重复报告异常。
  /// </summary>
  /// <param name="value">待保存的报告实体，携带当前版本指向与三个检索列。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  /// <exception cref="DuplicateMedicalRecognitionReportException">写入违反报告业务键唯一约束时抛出。</exception>
  public async Task<int> CreateMedicalRecognitionReportAsync(MedicalRecognitionReport value)
  {
    try
    {
      return await dataMapper.InsertAsync(value, scope: MedicalRecognitionReportScope, sqlId: "InsertMedicalRecognitionReport");
    }
    catch (Exception exception) when (IsUniqueConstraintViolation(exception))
    {
      // 唯一约束冲突是持久化事实，这里只翻译异常类型；并发首插的业务文案由领域层给出。
      throw new DuplicateMedicalRecognitionReportException("同一组织、医院、院区、报告类型与报告单号的报告已存在。", exception);
    }
  }

  /// <inheritdoc/>
  public async Task<int> UpdateMedicalRecognitionReportCurrentVersionAsync(MedicalRecognitionReport value) =>
    await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope, sqlId: "UpdateMedicalRecognitionReportCurrentVersion");

  /// <inheritdoc/>
  public async Task<int> VoidMedicalRecognitionReportAsync(MedicalRecognitionReport value) =>
    await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope, sqlId: "VoidMedicalRecognitionReport");

  /// <inheritdoc/>
  public async Task<int> GetMedicalReportMaxVersionNumberAsync(Guid reportId) =>
    await dataMapper.QuerySingleAsync<int>(new { ReportId = reportId }, scope: MedicalReportVersionScope, sqlId: "GetMedicalReportMaxVersionNumber");

  /// <inheritdoc/>
  public async Task<MedicalReportVersion?> GetMedicalReportVersionByIdAsync(Guid id) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersion>(new { Id = id }, scope: MedicalReportVersionScope, sqlId: "GetMedicalReportVersionById");

  /// <summary>
  /// 插入一个报告版本，并把报告与版本序号唯一约束冲突翻译为领域可识别的重复版本异常。
  /// </summary>
  /// <param name="value">待保存的版本实体，携带版本序号、公共信息、文件键与下载名。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  /// <exception cref="DuplicateMedicalReportVersionException">写入违反“报告标识 + 版本序号”唯一约束时抛出。</exception>
  public async Task<int> CreateMedicalReportVersionAsync(MedicalReportVersion value)
  {
    try
    {
      return await dataMapper.InsertAsync(value, scope: MedicalReportVersionScope, sqlId: "InsertMedicalReportVersion");
    }
    catch (Exception exception) when (IsUniqueConstraintViolation(exception))
    {
      // 唯一约束冲突是持久化事实，这里只翻译异常类型；并发追加的业务文案由领域层给出。
      throw new DuplicateMedicalReportVersionException("同一报告内该版本序号已被占用。", exception);
    }
  }

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryReportContentAsync(LaboratoryReportContent value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryReportContentScope, sqlId: "InsertLaboratoryReportContent");

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryResultItemAsync(LaboratoryResultItem value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryResultItemScope, sqlId: "InsertLaboratoryResultItem");

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryBacteriaResultAsync(LaboratoryBacteriaResult value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryBacteriaResultScope, sqlId: "InsertLaboratoryBacteriaResult");

  /// <inheritdoc/>
  public async Task<int> CreateLaboratoryAntimicrobialSusceptibilityAsync(LaboratoryAntimicrobialSusceptibility value) =>
    await dataMapper.InsertAsync(value, scope: LaboratoryAntimicrobialSusceptibilityScope, sqlId: "InsertLaboratoryAntimicrobialSusceptibility");

  /// <inheritdoc/>
  public async Task<int> CreateExaminationReportContentAsync(ExaminationReportContent value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationReportContentScope, sqlId: "InsertExaminationReportContent");

  /// <inheritdoc/>
  public async Task<int> CreateExaminationItemAsync(ExaminationItem value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationItemScope, sqlId: "InsertExaminationItem");

  /// <inheritdoc/>
  public async Task<int> CreateExaminationSiteAsync(ExaminationSite value) =>
    await dataMapper.InsertAsync(value, scope: ExaminationSiteScope, sqlId: "InsertExaminationSite");
}
