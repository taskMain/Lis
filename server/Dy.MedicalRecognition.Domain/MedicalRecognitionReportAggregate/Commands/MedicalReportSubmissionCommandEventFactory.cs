using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 报告提交与作废命令的事件工厂：把命令携带的业务事实投影为领域事件。
/// </summary>
/// <remarks>事件创建人与创建时间都取自命令携带的操作人与操作时间，事件不携带文件键、物理路径或提示文案。</remarks>
public static class MedicalReportSubmissionCommandEventFactory
{
  /// <summary>
  /// 构造「互认报告已创建」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="report">已创建的报告实体。</param>
  /// <returns>报告已创建事件。</returns>
  public static MedicalRecognitionReportCreatedEvent CreateMedicalRecognitionReportCreatedEvent(
    this SubmitCompleteLaboratoryReportCommand command, MedicalRecognitionReport report) =>
    new()
    {
      Id = report.Id,
      OrganizationCode = report.OrganizationCode,
      HospitalCode = report.HospitalCode,
      BranchCode = report.BranchCode,
      ReportType = report.ReportType,
      ReportNo = report.ReportNo,
      PatientId = report.PatientId,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「互认报告已创建」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="report">已创建的报告实体。</param>
  /// <returns>报告已创建事件。</returns>
  public static MedicalRecognitionReportCreatedEvent CreateMedicalRecognitionReportCreatedEvent(
    this SubmitCompleteExaminationReportCommand command, MedicalRecognitionReport report) =>
    new()
    {
      Id = report.Id,
      OrganizationCode = report.OrganizationCode,
      HospitalCode = report.HospitalCode,
      BranchCode = report.BranchCode,
      ReportType = report.ReportType,
      ReportNo = report.ReportNo,
      PatientId = report.PatientId,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「报告版本已追加并成为当前版本」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="report">本次提交所属报告。</param>
  /// <param name="version">本次追加的版本实体。</param>
  /// <returns>版本已追加事件。</returns>
  public static MedicalReportVersionAppendedAsCurrentEvent CreateMedicalReportVersionAppendedAsCurrentEvent(
    this SubmitCompleteLaboratoryReportCommand command, MedicalRecognitionReport report, MedicalReportVersion version) =>
    new()
    {
      ReportId = report.Id,
      ReportVersionId = version.Id,
      VersionNumber = version.VersionNumber,
      ReceivedTime = version.ReceivedTime,
      ReportNo = report.ReportNo,
      ReportType = report.ReportType,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「报告版本已追加并成为当前版本」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="report">本次提交所属报告。</param>
  /// <param name="version">本次追加的版本实体。</param>
  /// <returns>版本已追加事件。</returns>
  public static MedicalReportVersionAppendedAsCurrentEvent CreateMedicalReportVersionAppendedAsCurrentEvent(
    this SubmitCompleteExaminationReportCommand command, MedicalRecognitionReport report, MedicalReportVersion version) =>
    new()
    {
      ReportId = report.Id,
      ReportVersionId = version.Id,
      VersionNumber = version.VersionNumber,
      ReceivedTime = version.ReceivedTime,
      ReportNo = report.ReportNo,
      ReportType = report.ReportType,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「完整检验报告已提交」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">本次提交形成的报告版本标识。</param>
  /// <param name="versionNumber">本次提交形成的版本序号。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="reportType">报告类型。</param>
  /// <returns>提交完成事件。</returns>
  public static CompleteLaboratoryReportSubmittedEvent CreateCompleteLaboratoryReportSubmittedEvent(
    this SubmitCompleteLaboratoryReportCommand command, Guid reportId, Guid reportVersionId, int versionNumber, string reportNo, MedicalReportType reportType) =>
    new()
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId,
      VersionNumber = versionNumber,
      ReportNo = reportNo,
      ReportType = reportType,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「完整检查报告已提交」事件。
  /// </summary>
  /// <param name="command">本次提交命令，提供操作人与操作时间。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">本次提交形成的报告版本标识。</param>
  /// <param name="versionNumber">本次提交形成的版本序号。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="reportType">报告类型。</param>
  /// <returns>提交完成事件。</returns>
  public static CompleteExaminationReportSubmittedEvent CreateCompleteExaminationReportSubmittedEvent(
    this SubmitCompleteExaminationReportCommand command, Guid reportId, Guid reportVersionId, int versionNumber, string reportNo, MedicalReportType reportType) =>
    new()
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId,
      VersionNumber = versionNumber,
      ReportNo = reportNo,
      ReportType = reportType,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「检验报告已作废」事件。
  /// </summary>
  /// <param name="command">本次作废命令，提供操作人与操作时间。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="voidedTime">最终生效的作废时间。</param>
  /// <param name="voidReason">最终生效的作废原因。</param>
  /// <returns>作废事件。</returns>
  public static LaboratoryReportVoidedEvent CreateLaboratoryReportVoidedEvent(
    this VoidLaboratoryReportCommand command, Guid reportId, string reportNo, DateTime voidedTime, string voidReason) =>
    new()
    {
      ReportId = reportId,
      ReportNo = reportNo,
      VoidedTime = voidedTime,
      VoidReason = voidReason,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };

  /// <summary>
  /// 构造「检查报告已作废」事件。
  /// </summary>
  /// <param name="command">本次作废命令，提供操作人与操作时间。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="voidedTime">最终生效的作废时间。</param>
  /// <param name="voidReason">最终生效的作废原因。</param>
  /// <returns>作废事件。</returns>
  public static ExaminationReportVoidedEvent CreateExaminationReportVoidedEvent(
    this VoidExaminationReportCommand command, Guid reportId, string reportNo, DateTime voidedTime, string voidReason) =>
    new()
    {
      ReportId = reportId,
      ReportNo = reportNo,
      VoidedTime = voidedTime,
      VoidReason = voidReason,
      EventCreator = command.OperId,
      EventCreatedTime = command.OperTime
    };
}
