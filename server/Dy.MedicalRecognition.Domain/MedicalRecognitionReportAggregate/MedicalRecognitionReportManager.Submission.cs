using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告采集与生命周期：完整报告提交的内部步骤串行编排，以及报告作废。
/// </summary>
/// <remarks>
/// 提交按固定顺序完成：定位或创建报告、解析平台患者并核对、追加版本并更新当前版本指向与报告主体上的
/// 报告时间、患者姓名、证件号码、写入专项内容与全部明细、最后登记事件；任一步失败时整次提交不保存任何内容，
/// 也不登记后续步骤的事件。全部校验在第一次写入之前完成，因此校验失败不会留下任何业务数据。
/// 作废只改变生命周期状态、作废时间与作废原因，不生成内容版本、不物理删除文件。
/// 本类不控制事务生命周期，提交用例的事务边界声明在公开应用服务入口的方法上。
/// </remarks>
public partial class MedicalRecognitionReportManager
{
  /// <summary>
  /// 细菌鉴定结果表示培养未检出的检测结论原文；该表述下不要求来源菌种编码与名称。
  /// </summary>
  private const string NoOrganismDetectedConclusion = "未检出";

  /// <summary>
  /// 就诊类型的合法取值集合；只有这五个值可被接收，其它取值一律拒绝。
  /// </summary>
  private static readonly VisitType[] AcceptedVisitTypes =
  [
    VisitType.Outpatient, VisitType.Emergency, VisitType.Inpatient, VisitType.PhysicalExam, VisitType.Other
  ];

  /// <summary>
  /// 提交一份完整检验报告。
  /// </summary>
  /// <remarks>
  /// 提交前先完成全部请求校验与明细唯一性判定，再定位报告并解析患者；报告已作废时拒绝追加版本。
  /// </remarks>
  /// <param name="command">提交命令，携带报告业务标识、公共版本信息、检验专项内容、全部明细与可信归属。</param>
  /// <returns>提交成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 任一必填或成对字段不成立、就诊类型非法、业务时间逆序、明细集合为空、明细唯一性重复、
  /// 报告已作废、患者身份冲突、报告跨患者迁移或并发唯一约束冲突时抛出；此时不保存任何内容、不登记事件。
  /// </exception>
  public async Task<bool> SubmitCompleteLaboratoryReportAsync(SubmitCompleteLaboratoryReportCommand command)
  {
    ValidateReportVersion(command.ReportNo, command.Version, command.ReceivedTime);
    if (command.Results.Count == 0) throw new InvalidOperationException("业务拒绝：普通检验结果至少一条。");
    if (command.Content is null) throw new InvalidOperationException("业务拒绝：检验专项内容不能为空。");
    ValidateLaboratoryContent(command.Content);
    foreach (LaboratoryResultItemRequest result in command.Results) ValidateLaboratoryResultItem(result);
    foreach (LaboratoryBacteriaResultRequest bacteria in command.BacteriaResults) ValidateLaboratoryBacteriaResult(bacteria);
    ValidateLaboratoryDetailUniqueness(command.Results, command.BacteriaResults);

    MedicalRecognitionReport report = await ResolveReportAsync(command, MedicalReportType.Laboratory);
    PlatformPatient patient = await ResolvePlatformPatientAsync(report, command.Version, command.OperId, command.OperTime);
    (MedicalReportVersion version, int versionNumber) = await AppendVersionAsync(report, patient, command.Version, command.PdfFileId, command.PdfFileName, command.OperId, command.OperTime);
    AddEvent(command.CreateMedicalReportVersionAppendedAsCurrentEvent(report, version));
    await WriteLaboratoryContentAsync(version.Id, command, report);

    AddEvent(command.CreateCompleteLaboratoryReportSubmittedEvent(report.Id, version.Id, versionNumber, command.ReportNo, MedicalReportType.Laboratory));
    return true;
  }

  /// <summary>
  /// 提交一份完整检查报告。
  /// </summary>
  /// <remarks>内部步骤与失败语义与检验报告提交一致，明细替换为检查项目与检查部位。</remarks>
  /// <param name="command">提交命令，携带报告业务标识、公共版本信息、检查专项内容、全部项目与可信归属。</param>
  /// <returns>提交成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 任一必填或成对字段不成立、就诊类型非法、业务时间逆序、项目集合为空、部位名称为空、
  /// 报告已作废、患者身份冲突、报告跨患者迁移或并发唯一约束冲突时抛出；此时不保存任何内容、不登记事件。
  /// </exception>
  public async Task<bool> SubmitCompleteExaminationReportAsync(SubmitCompleteExaminationReportCommand command)
  {
    ValidateReportVersion(command.ReportNo, command.Version, command.ReceivedTime);
    if (command.Items.Count == 0) throw new InvalidOperationException("业务拒绝：检查项目至少一条。");
    if (command.Content is null) throw new InvalidOperationException("业务拒绝：检查专项内容不能为空。");
    ValidateExaminationContent(command.Content);
    foreach (ExaminationItemRequest item in command.Items) ValidateExaminationItem(item);

    MedicalRecognitionReport report = await ResolveReportAsync(command, MedicalReportType.Examination);
    PlatformPatient patient = await ResolvePlatformPatientAsync(report, command.Version, command.OperId, command.OperTime);
    (MedicalReportVersion version, int versionNumber) = await AppendVersionAsync(report, patient, command.Version, command.PdfFileId, command.PdfFileName, command.OperId, command.OperTime);
    AddEvent(command.CreateMedicalReportVersionAppendedAsCurrentEvent(report, version));
    await WriteExaminationContentAsync(version.Id, command, report);

    AddEvent(command.CreateCompleteExaminationReportSubmittedEvent(report.Id, version.Id, versionNumber, command.ReportNo, MedicalReportType.Examination));
    return true;
  }

  /// <summary>
  /// 作废一份检验报告。
  /// </summary>
  /// <param name="command">作废命令，携带报告单号、作废时间与原因、可信归属与本次请求接收时间。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">报告不存在、作废时间越界或作废信息冲突时抛出；此时不保存、不登记事件。</exception>
  public async Task<bool> VoidLaboratoryReportAsync(VoidLaboratoryReportCommand command) =>
    await VoidReportAsync(command.OrganizationCode, command.HospitalCode, command.BranchCode, MedicalReportType.Laboratory,
      command.ReportNo, command.VoidedTime, command.VoidReason, command.OperId, command.OperTime, command.ReceivedTime,
      (report, receivedTime, reason, at) => command.CreateLaboratoryReportVoidedEvent(report.Id, report.ReportNo, receivedTime, reason));

  /// <summary>
  /// 作废一份检查报告。
  /// </summary>
  /// <param name="command">作废命令，携带报告单号、作废时间与原因、可信归属与本次请求接收时间。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">报告不存在、作废时间越界或作废信息冲突时抛出；此时不保存、不登记事件。</exception>
  public async Task<bool> VoidExaminationReportAsync(VoidExaminationReportCommand command) =>
    await VoidReportAsync(command.OrganizationCode, command.HospitalCode, command.BranchCode, MedicalReportType.Examination,
      command.ReportNo, command.VoidedTime, command.VoidReason, command.OperId, command.OperTime, command.ReceivedTime,
      (report, receivedTime, reason, at) => command.CreateExaminationReportVoidedEvent(report.Id, report.ReportNo, receivedTime, reason));

  /// <summary>
  /// 两个作废用例共用的作废落地：定位报告、读取当前版本平台接收时间、按边界校验、判定幂等或冲突并写入作废事实。
  /// </summary>
  /// <remarks>
  /// 报告不存在时不创建占位记录；已作废且作废时间与原因完全一致时幂等成功且不重复登记事件；
  /// 已作废但任一事实不同时返回作废信息冲突，不覆盖首次保存的作废事实。
  /// 条件更新带未作废的原状态，影响 0 行说明并发请求已写入同一作废事实，按幂等成功处理。
  /// </remarks>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="reportType">报告类型。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="voidedTime">作废时间。</param>
  /// <param name="voidReason">作废原因。</param>
  /// <param name="operId">操作人标识。</param>
  /// <param name="operTime">操作时间。</param>
  /// <param name="receivedTime">本次请求的接收时间。</param>
  /// <param name="createEvent">按报告的最终作废事实构造作废事件的委托。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">报告不存在、作废时间越界、作废事实缺失或作废信息冲突时抛出。</exception>
  private async Task<bool> VoidReportAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    MedicalReportType reportType,
    string reportNo,
    DateTime voidedTime,
    string voidReason,
    Guid operId,
    DateTimeOffset operTime,
    DateTime receivedTime,
    Func<MedicalRecognitionReport, DateTime, string, DateTime, DomainEvent> createEvent)
  {
    if (string.IsNullOrWhiteSpace(reportNo)) throw new InvalidOperationException("业务拒绝：报告单号不能是空白。");
    if (string.IsNullOrWhiteSpace(voidReason)) throw new InvalidOperationException("业务拒绝：作废原因不能是空白。");
    if (voidedTime == default) throw new InvalidOperationException("业务拒绝：作废时间不能缺失。");

    MedicalRecognitionReport report = await repository.GetMedicalRecognitionReportByBusinessKeyAsync(
      organizationCode, hospitalCode, branchCode, reportType, reportNo)
      ?? throw new InvalidOperationException("业务拒绝：报告不存在。");

    if (report.Status == MedicalReportLifecycleStatus.Voided)
    {
      // 幂等判定只比较作废时间与原因：两者完全一致说明重复提交同一作废事实，不重复登记事件、不刷新操作字段。
      bool sameFact = report.VoidedTime == voidedTime && string.Equals(report.VoidReason ?? string.Empty, voidReason, StringComparison.Ordinal);
      if (!sameFact) throw new InvalidOperationException("业务拒绝：报告作废信息与已保存的作废事实不一致。");
      return true;
    }

    MedicalReportVersion currentVersion = await repository.GetMedicalReportVersionByIdAsync(report.CurrentVersionId)
      ?? throw new InvalidOperationException("业务拒绝：报告当前版本不存在，无法作废。");

    // 作废时间不得早于当前版本平台接收时间、不得晚于本次请求接收时间，两端含边界。
    if (voidedTime < currentVersion.ReceivedTime) throw new InvalidOperationException("业务拒绝：作废时间不得早于当前版本的平台接收时间。");
    if (voidedTime > receivedTime) throw new InvalidOperationException("业务拒绝：作废时间不得晚于本次请求的接收时间。");

    MedicalRecognitionReport voided = new()
    {
      Id = report.Id,
      OrganizationCode = report.OrganizationCode,
      HospitalCode = report.HospitalCode,
      BranchCode = report.BranchCode,
      ReportType = report.ReportType,
      ReportNo = report.ReportNo,
      PatientId = report.PatientId,
      CurrentVersionId = report.CurrentVersionId,
      ReportTime = report.ReportTime,
      PatientName = report.PatientName,
      IdentityDocumentNo = report.IdentityDocumentNo,
      Status = MedicalReportLifecycleStatus.Voided,
      VoidedTime = voidedTime,
      VoidReason = voidReason,
      OperId = operId,
      OperTime = operTime
    };

    int affectedRows = await repository.VoidMedicalRecognitionReportAsync(voided);
    if (affectedRows != 1)
    {
      // 条件更新带未作废的原状态，影响 0 行说明并发请求已把同一报告改为已作废；
      // 复读一次区分"同一事实已保存"与"作废事实已不同"，不循环重试、不自动重放意图。
      MedicalRecognitionReport? current = await repository.GetMedicalRecognitionReportByBusinessKeyAsync(
        organizationCode, hospitalCode, branchCode, reportType, reportNo);
      if (current is null) throw new InvalidOperationException("业务拒绝：报告不存在。");
      bool sameFact = current.Status == MedicalReportLifecycleStatus.Voided
        && current.VoidedTime == voidedTime
        && string.Equals(current.VoidReason ?? string.Empty, voidReason, StringComparison.Ordinal);
      if (!sameFact) throw new InvalidOperationException("业务拒绝：报告作废信息与已保存的作废事实不一致。");
      return true;
    }

    AddEvent(createEvent(voided, voidedTime, voidReason, voidedTime));
    return true;
  }

  /// <summary>
  /// 按五个业务键定位检验报告，未命中时创建当前有效报告并登记报告已创建事件；已作废报告拒绝追加版本。
  /// </summary>
  /// <param name="command">本次检验报告提交命令，提供业务键与操作字段。</param>
  /// <param name="reportType">报告类型。</param>
  /// <returns>可用于追加版本的报告实体。</returns>
  /// <exception cref="InvalidOperationException">报告已作废，或并发首插命中业务键唯一约束时抛出。</exception>
  private async Task<MedicalRecognitionReport> ResolveReportAsync(SubmitCompleteLaboratoryReportCommand command, MedicalReportType reportType)
  {
    (MedicalRecognitionReport report, bool isCreated) = await CreateReportIfAbsentAsync(
      command.OrganizationCode, command.HospitalCode, command.BranchCode, reportType, command.ReportNo, command.OperId, command.OperTime);
    if (isCreated) AddEvent(command.CreateMedicalRecognitionReportCreatedEvent(report));
    return report;
  }

  /// <summary>
  /// 按五个业务键定位检查报告，未命中时创建当前有效报告并登记报告已创建事件；已作废报告拒绝追加版本。
  /// </summary>
  /// <param name="command">本次检查报告提交命令，提供业务键与操作字段。</param>
  /// <param name="reportType">报告类型。</param>
  /// <returns>可用于追加版本的报告实体。</returns>
  /// <exception cref="InvalidOperationException">报告已作废，或并发首插命中业务键唯一约束时抛出。</exception>
  private async Task<MedicalRecognitionReport> ResolveReportAsync(SubmitCompleteExaminationReportCommand command, MedicalReportType reportType)
  {
    (MedicalRecognitionReport report, bool isCreated) = await CreateReportIfAbsentAsync(
      command.OrganizationCode, command.HospitalCode, command.BranchCode, reportType, command.ReportNo, command.OperId, command.OperTime);
    if (isCreated) AddEvent(command.CreateMedicalRecognitionReportCreatedEvent(report));
    return report;
  }

  /// <summary>
  /// 按五个业务键定位报告，未命中时创建当前有效报告；已作废报告拒绝追加版本。
  /// </summary>
  /// <remarks>
  /// 返回值第二项表达本次是否新建，调用方据此判定是否登记报告已创建事件；报告已存在时只取得、不重复登记。
  /// </remarks>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="reportType">报告类型。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="operId">操作人标识。</param>
  /// <param name="operTime">操作时间。</param>
  /// <returns>可用于追加版本的报告实体，以及本次是否为新建。</returns>
  /// <exception cref="InvalidOperationException">报告已作废，或并发首插命中业务键唯一约束时抛出。</exception>
  private async Task<(MedicalRecognitionReport Report, bool IsCreated)> CreateReportIfAbsentAsync(
    string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo, Guid operId, DateTimeOffset operTime)
  {
    MedicalRecognitionReport? existing = await repository.GetMedicalRecognitionReportByBusinessKeyAsync(
      organizationCode, hospitalCode, branchCode, reportType, reportNo);
    if (existing is not null)
    {
      if (existing.Status == MedicalReportLifecycleStatus.Voided) throw new InvalidOperationException("业务拒绝：报告已作废，不能再次提交。");
      return (existing, false);
    }

    MedicalRecognitionReport created = new()
    {
      Id = repository.CreateGuid(),
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      ReportType = reportType,
      ReportNo = reportNo,
      PatientId = Guid.Empty,
      CurrentVersionId = Guid.Empty,
      ReportTime = DateTime.MinValue,
      PatientName = string.Empty,
      IdentityDocumentNo = string.Empty,
      Status = MedicalReportLifecycleStatus.Effective,
      VoidedTime = null,
      VoidReason = null,
      OperId = operId,
      OperTime = operTime
    };

    try
    {
      if (await repository.CreateMedicalRecognitionReportAsync(created) != 1)
        throw new InvalidOperationException("业务拒绝：报告保存影响的行数异常，未完成提交。");
    }
    catch (DuplicateMedicalRecognitionReportException exception)
    {
      // 唯一约束冲突是持久化事实：并发首次提交同一业务键时由数据库兜底，翻译为业务拒绝并提示重试；
      // 报告已创建事件在此不登记，本次提交整体失败。
      throw new InvalidOperationException("业务拒绝：该报告已被并发提交，请刷新后重试。", exception);
    }

    return (created, true);
  }

  /// <summary>
  /// 按证件类型代码与证件号码解析平台患者并核对核心身份，必要时创建平台患者。
  /// </summary>
  /// <remarks>
  /// 首次版本把报告与该患者关联；后续版本必须解析到报告既有关联患者，解析到其他患者或核心身份不一致时拒绝。
  /// 并发首次解析撞证件键时复读一次：核心身份一致则继续追加版本，不一致按患者身份信息冲突拒绝；
  /// 患者与报告主体检索列按规范形式保存，报告版本表另按来源原值保存。
  /// </remarks>
  /// <param name="report">本次提交所属报告。</param>
  /// <param name="version">公共版本信息。</param>
  /// <param name="operId">操作人标识。</param>
  /// <param name="operTime">操作时间。</param>
  /// <returns>本次提交解析到的平台患者。</returns>
  /// <exception cref="InvalidOperationException">跨患者迁移、核心身份冲突，或并发复读仍无患者时抛出。</exception>
  private async Task<PlatformPatient> ResolvePlatformPatientAsync(MedicalRecognitionReport report, MedicalReportVersionRequest version, Guid operId, DateTimeOffset operTime)
  {
    string documentTypeCode = NormalizeCode(version.IdentityDocumentTypeCode);
    string documentNo = NormalizeDocumentNo(version.IdentityDocumentNo);
    string patientName = NormalizeName(version.PatientName);

    PlatformPatient? patient = await repository.GetPlatformPatientByDocumentAsync(documentTypeCode, documentNo);
    if (patient is null)
    {
      PlatformPatient created = new()
      {
        Id = repository.CreateGuid(),
        IdentityDocumentTypeCode = documentTypeCode,
        IdentityDocumentNo = documentNo,
        PatientName = patientName,
        PatientGenderCode = version.PatientGenderCode,
        PatientBirthDate = version.PatientBirthDate,
        OperId = operId,
        OperTime = operTime
      };

      try
      {
        if (await repository.CreatePlatformPatientAsync(created) != 1)
          throw new InvalidOperationException("业务拒绝：平台患者保存影响的行数异常，未完成提交。");
        patient = created;
      }
      catch (DuplicatePlatformPatientException)
      {
        // 并发首次解析同一证件：按同一证件键复读一次，只复读一次、不循环重试。
        // 复读在「唯一约束冲突已终止所在事务」时不可执行，此时仓储返回不可读物（见其实现），
        // 领域层按并发冲突拒绝并提示刷新后重试，不把持久化异常向外泄漏。
        (PlatformPatient Patient, bool IsReadable) concurrent = await repository.TryGetPlatformPatientByDocumentAsync(documentTypeCode, documentNo);
        if (!concurrent.IsReadable) throw new InvalidOperationException("业务拒绝：患者身份信息并发冲突，请刷新后重试。");
        patient = concurrent.Patient ?? throw new InvalidOperationException("业务拒绝：患者身份信息并发冲突，未完成提交。");
      }
    }

    if (!IsSamePatientIdentity(patient, patientName, version.PatientGenderCode, version.PatientBirthDate))
      throw new InvalidOperationException("业务拒绝：证件已存在但患者身份信息不一致。");
    if (report.PatientId != Guid.Empty && report.PatientId != patient.Id)
      throw new InvalidOperationException("业务拒绝：本次提交解析到的患者与报告已关联的患者不一致。");

    return patient;
  }

  /// <summary>
  /// 核对平台患者与本次提交的核心身份（姓名规范形式、性别代码、出生日期）是否一致。
  /// </summary>
  /// <param name="patient">已存在的平台患者。</param>
  /// <param name="patientName">本次提交的姓名规范形式。</param>
  /// <param name="genderCode">本次提交的性别代码。</param>
  /// <param name="birthDate">本次提交的出生日期。</param>
  /// <returns>三项全部一致时为 <see langword="true"/>。</returns>
  private static bool IsSamePatientIdentity(PlatformPatient patient, string patientName, string genderCode, DateTime birthDate) =>
    string.Equals(patient.PatientName, patientName, StringComparison.Ordinal)
    && string.Equals(patient.PatientGenderCode, genderCode, StringComparison.Ordinal)
    && patient.PatientBirthDate == birthDate;

  /// <summary>
  /// 追加一个报告版本并把当前版本指向与报告主体检索列更新为本次版本取值。
  /// </summary>
  /// <param name="report">本次提交所属报告。</param>
  /// <param name="patient">本次提交解析到的平台患者。</param>
  /// <param name="version">公共版本信息。</param>
  /// <param name="pdfFileId">PDF 文件键。</param>
  /// <param name="pdfFileName">PDF 下载名。</param>
  /// <param name="operId">操作人标识。</param>
  /// <param name="operTime">操作时间。</param>
  /// <returns>本次追加的版本实体与其版本序号。</returns>
  /// <exception cref="InvalidOperationException">版本追加或当前版本指向更新未影响恰好一行时抛出。</exception>
  private async Task<(MedicalReportVersion Version, int VersionNumber)> AppendVersionAsync(
    MedicalRecognitionReport report, PlatformPatient patient, MedicalReportVersionRequest version, string pdfFileId, string pdfFileName, Guid operId, DateTimeOffset operTime)
  {
    int versionNumber = await repository.GetMedicalReportMaxVersionNumberAsync(report.Id) + 1;
    MedicalReportVersion appended = new()
    {
      Id = repository.CreateGuid(),
      ReportId = report.Id,
      PatientId = patient.Id,
      VersionNumber = versionNumber,
      SourceReportName = version.SourceReportName,
      PatientName = version.PatientName,
      PatientGenderCode = version.PatientGenderCode,
      PatientBirthDate = version.PatientBirthDate,
      PatientPhoneNumber = version.PatientPhoneNumber,
      AgeAtReport = version.AgeAtReport,
      IdentityDocumentTypeCode = version.IdentityDocumentTypeCode,
      IdentityDocumentNo = version.IdentityDocumentNo,
      VisitType = version.VisitType,
      VisitSerialNo = version.VisitSerialNo,
      ApplicationDeptId = version.ApplicationDeptId,
      ApplicationDeptName = version.ApplicationDeptName,
      ApplicationDoctorId = version.ApplicationDoctorId,
      ApplicationDoctorName = version.ApplicationDoctorName,
      ExecutionDeptId = version.ExecutionDeptId,
      ExecutionDeptName = version.ExecutionDeptName,
      ReportDeptId = version.ReportDeptId,
      ReportDeptName = version.ReportDeptName,
      ReportDoctorId = version.ReportDoctorId,
      ReportDoctorName = version.ReportDoctorName,
      ReviewDoctorId = version.ReviewDoctorId,
      ReviewDoctorName = version.ReviewDoctorName,
      ReviewTime = version.ReviewTime,
      InpatientNo = version.InpatientNo,
      WardName = version.WardName,
      RoomName = version.RoomName,
      BedNo = version.BedNo,
      ApplicationTime = version.ApplicationTime,
      ReportTime = version.ReportTime,
      SourceModifiedTime = version.SourceModifiedTime,
      // 平台接收时间由服务端取，不采用请求值。
      ReceivedTime = DateTime.Now,
      PdfFileId = pdfFileId,
      PdfFileName = pdfFileName,
      SourceConfidentialFlag = version.SourceConfidentialFlag,
      OperId = operId,
      OperTime = operTime
    };

    try
    {
      if (await repository.CreateMedicalReportVersionAsync(appended) != 1)
        throw new InvalidOperationException("业务拒绝：报告版本保存影响的行数异常，未完成提交。");
    }
    catch (DuplicateMedicalReportVersionException exception)
    {
      // 版本序号唯一索引拦截同一报告的并发追加；统一翻译为业务拒绝并提示重试，不自动改用其它序号。
      throw new InvalidOperationException("业务拒绝：报告版本已被并发提交，请刷新后重试。", exception);
    }

    MedicalRecognitionReport updatedReport = new()
    {
      Id = report.Id,
      OrganizationCode = report.OrganizationCode,
      HospitalCode = report.HospitalCode,
      BranchCode = report.BranchCode,
      ReportType = report.ReportType,
      ReportNo = report.ReportNo,
      PatientId = patient.Id,
      CurrentVersionId = appended.Id,
      ReportTime = appended.ReportTime,
      PatientName = patient.PatientName,
      IdentityDocumentNo = patient.IdentityDocumentNo,
      Status = report.Status,
      VoidedTime = report.VoidedTime,
      VoidReason = report.VoidReason,
      OperId = operId,
      OperTime = operTime
    };
    if (await repository.UpdateMedicalRecognitionReportCurrentVersionAsync(updatedReport) != 1)
      throw new InvalidOperationException("业务拒绝：报告当前版本指向更新失败，未完成提交。");

    report.PatientId = patient.Id;
    report.CurrentVersionId = appended.Id;
    report.ReportTime = appended.ReportTime;
    report.PatientName = patient.PatientName;
    report.IdentityDocumentNo = patient.IdentityDocumentNo;

    return (appended, versionNumber);
  }

  /// <summary>
  /// 写入检验专项内容、普通结果、细菌鉴定结果与药敏结果。
  /// </summary>
  /// <param name="reportVersionId">本次提交形成的报告版本标识。</param>
  /// <param name="command">提交命令。</param>
  /// <param name="report">本次提交所属报告，用于事件的报告身份字段。</param>
  /// <returns>全部明细写入完成后的异步操作。</returns>
  /// <exception cref="InvalidOperationException">任一条写入未影响恰好一行时抛出。</exception>
  private async Task WriteLaboratoryContentAsync(Guid reportVersionId, SubmitCompleteLaboratoryReportCommand command, MedicalRecognitionReport report)
  {
    LaboratoryReportContentRequest content = command.Content;
    LaboratoryReportContent laboratoryContent = new()
    {
      Id = repository.CreateGuid(),
      ReportVersionId = reportVersionId,
      ReportCategoryCode = content.ReportCategoryCode,
      ReportCategoryName = content.ReportCategoryName,
      ReportRemark = content.ReportRemark,
      OverallAbnormalFlag = content.OverallAbnormalFlag,
      SourceOrderSerialNo = content.SourceOrderSerialNo,
      SpecimenCollectedTime = content.SpecimenCollectedTime,
      SpecimenSubmittedTime = content.SpecimenSubmittedTime,
      LaboratoryReceivedTime = content.LaboratoryReceivedTime,
      SourceSpecimenNo = content.SourceSpecimenNo,
      SpecimenTypeCode = content.SpecimenTypeCode,
      SpecimenTypeName = content.SpecimenTypeName,
      TestingCompletedTime = content.TestingCompletedTime,
      InspectorId = content.InspectorId,
      InspectorName = content.InspectorName,
      OperId = command.OperId,
      OperTime = command.OperTime
    };
    await EnsureAffectedOneRowAsync(repository.CreateLaboratoryReportContentAsync(laboratoryContent), "检验专项内容");

    foreach (LaboratoryResultItemRequest result in command.Results)
    {
      LaboratoryResultItem item = new()
      {
        Id = repository.CreateGuid(),
        ReportVersionId = reportVersionId,
        SourceDetailKey = result.SourceDetailKey,
        SourceProjectName = result.SourceProjectName,
        SourceProjectCode = result.SourceProjectCode,
        StandardProjectCode = result.StandardProjectCode,
        SourceResultText = result.SourceResultText,
        ResultType = result.ResultType,
        LoincCode = result.LoincCode,
        Unit = result.Unit,
        ReferenceRange = result.ReferenceRange,
        TestingMethod = result.TestingMethod,
        InstrumentCode = result.InstrumentCode,
        InstrumentName = result.InstrumentName,
        DisplayOrder = result.DisplayOrder,
        AbnormalFlag = result.AbnormalFlag,
        CriticalValueFlag = result.CriticalValueFlag,
        LaboratoryChargeItemCode = result.LaboratoryChargeItemCode,
        InsuranceChargeItemCode = result.MedicalInsuranceChargeItemCode,
        InspectorId = result.InspectorId,
        InspectorName = result.InspectorName,
        OperId = command.OperId,
        OperTime = command.OperTime
      };
      await EnsureAffectedOneRowAsync(repository.CreateLaboratoryResultItemAsync(item), "普通检验结果");
    }

    foreach (LaboratoryBacteriaResultRequest source in command.BacteriaResults)
    {
      LaboratoryBacteriaResult bacteria = new()
      {
        Id = repository.CreateGuid(),
        ReportVersionId = reportVersionId,
        SourceDetailKey = source.SourceDetailKey,
        SourceOrganismCode = source.SourceOrganismCode,
        SourceOrganismName = source.SourceOrganismName,
        SourceResultText = source.SourceResultText,
        DetectionConclusion = source.DetectionConclusion,
        ColonyCount = source.ColonyCount,
        CultureMedium = source.CultureMedium,
        CultureTime = source.CultureTime,
        CultureCondition = source.CultureCondition,
        DiscoveryMethod = source.DiscoveryMethod,
        DetectionMethod = source.DetectionMethod,
        Description = source.Description,
        InstrumentCode = source.InstrumentCode,
        InstrumentName = source.InstrumentName,
        TestPanelCode = source.TestPanelCode,
        TestPanelName = source.TestPanelName,
        InspectorId = source.InspectorId,
        InspectorName = source.InspectorName,
        OperId = command.OperId,
        OperTime = command.OperTime
      };
      await EnsureAffectedOneRowAsync(repository.CreateLaboratoryBacteriaResultAsync(bacteria), "细菌鉴定结果");

      // 药敏结果固定归属刚写入的细菌鉴定结果，归属字段不接受请求值，因此不会产生无法归属的记录。
      foreach (LaboratoryAntimicrobialSusceptibilityRequest susceptibility in source.Susceptibilities)
      {
        LaboratoryAntimicrobialSusceptibility item = new()
        {
          Id = repository.CreateGuid(),
          BacteriaResultId = bacteria.Id,
          SourceDetailKey = susceptibility.SourceDetailKey,
          DrugCode = susceptibility.DrugCode,
          DrugName = susceptibility.DrugName,
          SusceptibilityCode = susceptibility.SusceptibilityCode,
          SourceConclusionText = susceptibility.SourceConclusionText,
          ResistanceResultCode = susceptibility.ResistanceResultCode,
          DiskContent = susceptibility.DiskContent,
          MicValue = susceptibility.MicValue,
          InhibitionZoneDiameter = susceptibility.InhibitionZoneDiameter,
          ReferenceValue = susceptibility.ReferenceValue,
          DisplayOrder = susceptibility.DisplayOrder,
          InspectorId = susceptibility.InspectorId,
          InspectorName = susceptibility.InspectorName,
          TestingMethod = susceptibility.TestingMethod,
          TestPanelOrder = susceptibility.TestPanelOrder,
          OperId = command.OperId,
          OperTime = command.OperTime
        };
        await EnsureAffectedOneRowAsync(repository.CreateLaboratoryAntimicrobialSusceptibilityAsync(item), "药敏结果");
      }
    }
  }

  /// <summary>
  /// 写入检查专项内容、检查项目与检查部位。
  /// </summary>
  /// <param name="reportVersionId">本次提交形成的报告版本标识。</param>
  /// <param name="command">提交命令。</param>
  /// <param name="report">本次提交所属报告，用于事件的报告身份字段。</param>
  /// <returns>全部项目与部位写入完成后的异步操作。</returns>
  /// <exception cref="InvalidOperationException">任一条写入未影响恰好一行时抛出。</exception>
  private async Task WriteExaminationContentAsync(Guid reportVersionId, SubmitCompleteExaminationReportCommand command, MedicalRecognitionReport report)
  {
    ExaminationReportContentRequest content = command.Content;
    ExaminationReportContent examinationContent = new()
    {
      Id = repository.CreateGuid(),
      ReportVersionId = reportVersionId,
      SourceExaminationTypeCode = content.SourceExaminationTypeCode,
      SourceExaminationTypeName = content.SourceExaminationTypeName,
      ReportRemark = content.ReportRemark,
      OverallAbnormalFlag = content.OverallAbnormalFlag,
      Findings = content.Findings,
      Conclusion = content.Conclusion,
      ConditionDescription = content.ConditionDescription,
      ExaminationPurpose = content.ExaminationPurpose,
      SourceDiagnosisCode = content.SourceDiagnosisCode,
      SourceDiagnosisName = content.SourceDiagnosisName,
      ExaminationTime = content.ExaminationTime,
      ExaminerId = content.ExaminerId,
      ExaminerName = content.ExaminerName,
      // 来源影像状态未提供时按未知处理并保存，不拒绝整份报告。
      SourceImageStatus = content.SourceImageStatus ?? SourceImageStatus.Unknown,
      ImageAccessUrl = content.ImageAccessUrl,
      ExaminationMethod = content.ExaminationMethod,
      DeviceCode = content.DeviceCode,
      DeviceName = content.DeviceName,
      OperId = command.OperId,
      OperTime = command.OperTime
    };
    await EnsureAffectedOneRowAsync(repository.CreateExaminationReportContentAsync(examinationContent), "检查专项内容");

    foreach (ExaminationItemRequest source in command.Items)
    {
      ExaminationItem item = new()
      {
        Id = repository.CreateGuid(),
        ReportVersionId = reportVersionId,
        SourceProjectName = source.SourceProjectName,
        SourceProjectCode = source.SourceProjectCode,
        StandardProjectCode = source.StandardProjectCode,
        OperId = command.OperId,
        OperTime = command.OperTime
      };
      await EnsureAffectedOneRowAsync(repository.CreateExaminationItemAsync(item), "检查项目");

      // 检查部位随所属检查项目保存，不在报告中重复归属；项目没有明确部位时集合为空。
      foreach (ExaminationSiteRequest site in source.Sites)
      {
        ExaminationSite entity = new()
        {
          Id = repository.CreateGuid(),
          ExaminationItemId = item.Id,
          SourceSiteCode = site.SourceSiteCode,
          SiteName = site.SiteName,
          OperId = command.OperId,
          OperTime = command.OperTime
        };
        await EnsureAffectedOneRowAsync(repository.CreateExaminationSiteAsync(entity), "检查部位");
      }
    }
  }

  /// <summary>
  /// 等待一条写语句并把非 1 的影响行数按业务拒绝抛出。
  /// </summary>
  /// <param name="affectedRowsTask">写语句返回的影响行数。</param>
  /// <param name="contentName">写入内容的中文名称，用于拒绝文案。</param>
  /// <returns>恰为 1 行时完成的异步操作。</returns>
  /// <exception cref="InvalidOperationException">影响行数不为 1 时抛出。</exception>
  private static async Task EnsureAffectedOneRowAsync(Task<int> affectedRowsTask, string contentName)
  {
    if (await affectedRowsTask != 1) throw new InvalidOperationException($"业务拒绝：{contentName}保存影响的行数异常，未完成提交。");
  }
}
