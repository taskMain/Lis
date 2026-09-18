using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 完整报告提交输入中的公共版本信息，检验与检查两类报告共用。
/// </summary>
public sealed record MedicalReportVersionRequest
{
  /// <summary>来源报告名称。必填。</summary>
  public string SourceReportName { get; init; } = string.Empty;
  /// <summary>患者姓名。必填；保存与比较前去除首尾空白，报告版本表另按来源原值保存。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者性别代码。必填。</summary>
  public string PatientGenderCode { get; init; } = string.Empty;
  /// <summary>患者出生日期。必填；零值视为缺失。</summary>
  public DateTime PatientBirthDate { get; init; }
  /// <summary>患者联系电话。应填。</summary>
  public string? PatientPhoneNumber { get; init; }
  /// <summary>报告时年龄，以来源文本承载。应填。</summary>
  public string? AgeAtReport { get; init; }
  /// <summary>证件类型代码。必填；保存与比较前去除首尾空白。</summary>
  public string IdentityDocumentTypeCode { get; init; } = string.Empty;
  /// <summary>证件号码。必填；保存与比较前去除首尾空白并统一大写，报告版本表另按来源原值保存。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>就诊类型。必填；仅接受门诊、急诊、住院、体检、其他五值，不接受未知。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>就诊流水号。必填。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>住院号。条件应填（住院时提供）。</summary>
  public string? InpatientNo { get; init; }
  /// <summary>病区名称。条件应填（住院时提供）。</summary>
  public string? WardName { get; init; }
  /// <summary>病房名称。条件应填（住院时提供）。</summary>
  public string? RoomName { get; init; }
  /// <summary>床位号。条件应填（住院时提供）。</summary>
  public string? BedNo { get; init; }
  /// <summary>申请科室 ID。必填且与名称成对。</summary>
  public string ApplicationDeptId { get; init; } = string.Empty;
  /// <summary>申请科室名称。必填且与标识成对。</summary>
  public string ApplicationDeptName { get; init; } = string.Empty;
  /// <summary>申请医生 ID。必填且与名称成对。</summary>
  public string ApplicationDoctorId { get; init; } = string.Empty;
  /// <summary>申请医生名称。必填且与标识成对。</summary>
  public string ApplicationDoctorName { get; init; } = string.Empty;
  /// <summary>执行科室 ID。必填且与名称成对。</summary>
  public string ExecutionDeptId { get; init; } = string.Empty;
  /// <summary>执行科室名称。必填且与标识成对。</summary>
  public string ExecutionDeptName { get; init; } = string.Empty;
  /// <summary>报告科室 ID。必填且与名称成对。</summary>
  public string ReportDeptId { get; init; } = string.Empty;
  /// <summary>报告科室名称。必填且与标识成对。</summary>
  public string ReportDeptName { get; init; } = string.Empty;
  /// <summary>报告医生 ID。必填且与名称成对。</summary>
  public string ReportDoctorId { get; init; } = string.Empty;
  /// <summary>报告医生名称。必填且与标识成对。</summary>
  public string ReportDoctorName { get; init; } = string.Empty;
  /// <summary>审核医生 ID。必填且与名称成对。</summary>
  public string ReviewDoctorId { get; init; } = string.Empty;
  /// <summary>审核医生名称。必填且与标识成对。</summary>
  public string ReviewDoctorName { get; init; } = string.Empty;
  /// <summary>申请时间。必填；零值视为缺失。</summary>
  public DateTime ApplicationTime { get; init; }
  /// <summary>报告时间。必填；零值视为缺失。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>审核时间。应填；为空时不参与时间顺序校验。</summary>
  public DateTime? ReviewTime { get; init; }
  /// <summary>源端报告修改时间。必填；随版本保存，不参与版本顺序判断。</summary>
  public DateTime SourceModifiedTime { get; init; }
  /// <summary>来源保密标识。应填。</summary>
  public string? SourceConfidentialFlag { get; init; }
}
