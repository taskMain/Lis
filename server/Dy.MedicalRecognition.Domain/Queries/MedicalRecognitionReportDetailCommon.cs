namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告版本的公共信息查询投影行。
/// </summary>
/// <remarks>公共信息按来源原值保存；患者联系电话在应用层返回前脱敏。</remarks>
public sealed record MedicalRecognitionReportDetailCommon
{
  /// <summary>患者姓名。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者性别代码。</summary>
  public string PatientGenderCode { get; init; } = string.Empty;
  /// <summary>患者出生日期。</summary>
  public DateTime PatientBirthDate { get; init; }
  /// <summary>患者联系电话；来源未提供时为 <see langword="null"/>。</summary>
  public string? PatientPhoneNumber { get; init; }
  /// <summary>报告时年龄。</summary>
  public string? AgeAtReport { get; init; }
  /// <summary>证件类型代码。</summary>
  public string IdentityDocumentTypeCode { get; init; } = string.Empty;
  /// <summary>证件号码。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>就诊流水号。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>来源报告名称。</summary>
  public string SourceReportName { get; init; } = string.Empty;
  /// <summary>申请科室 ID。</summary>
  public string ApplicationDeptId { get; init; } = string.Empty;
  /// <summary>申请科室名称。</summary>
  public string ApplicationDeptName { get; init; } = string.Empty;
  /// <summary>申请医生 ID。</summary>
  public string ApplicationDoctorId { get; init; } = string.Empty;
  /// <summary>申请医生名称。</summary>
  public string ApplicationDoctorName { get; init; } = string.Empty;
  /// <summary>执行科室 ID。</summary>
  public string ExecutionDeptId { get; init; } = string.Empty;
  /// <summary>执行科室名称。</summary>
  public string ExecutionDeptName { get; init; } = string.Empty;
  /// <summary>报告科室 ID。</summary>
  public string ReportDeptId { get; init; } = string.Empty;
  /// <summary>报告科室名称。</summary>
  public string ReportDeptName { get; init; } = string.Empty;
  /// <summary>报告医生 ID。</summary>
  public string ReportDoctorId { get; init; } = string.Empty;
  /// <summary>报告医生名称。</summary>
  public string ReportDoctorName { get; init; } = string.Empty;
  /// <summary>审核医生 ID。</summary>
  public string ReviewDoctorId { get; init; } = string.Empty;
  /// <summary>审核医生名称。</summary>
  public string ReviewDoctorName { get; init; } = string.Empty;
  /// <summary>审核时间；来源未提供时为 <see langword="null"/>。</summary>
  /// <remarks>审核时间是应填字段，来源可以留空，因此该列可空；投影类型必须与列可空性一致，否则读取会因空值绑定失败。</remarks>
  public DateTime? ReviewTime { get; init; }
  /// <summary>住院号。</summary>
  public string? InpatientNo { get; init; }
  /// <summary>病区名称。</summary>
  public string? WardName { get; init; }
  /// <summary>病房名称。</summary>
  public string? RoomName { get; init; }
  /// <summary>床位号。</summary>
  public string? BedNo { get; init; }
  /// <summary>申请时间。</summary>
  public DateTime ApplicationTime { get; init; }
  /// <summary>报告时间。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>来源保密标识。</summary>
  public string? SourceConfidentialFlag { get; init; }
}
