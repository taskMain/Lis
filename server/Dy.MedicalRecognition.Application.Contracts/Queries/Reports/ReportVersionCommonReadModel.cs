using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告公共信息；检验与检查两类报告共用，取值按来源原值保存。
/// </summary>
public sealed record ReportVersionCommonReadModel
{
  /// <summary>患者姓名。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者性别代码。</summary>
  public string PatientGenderCode { get; init; } = string.Empty;
  /// <summary>患者出生日期。</summary>
  public DateTime PatientBirthDate { get; init; }
  /// <summary>患者联系电话；按来源原值返回，来源未提供时为 <see langword="null"/>。</summary>
  public string? PatientPhoneNumber { get; init; }
  /// <summary>报告时年龄，以来源文本承载。</summary>
  public string? AgeAtReport { get; init; }
  /// <summary>证件类型代码。</summary>
  public string IdentityDocumentTypeCode { get; init; } = string.Empty;
  /// <summary>证件号码。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>
  /// 就诊类型中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  public string VisitTypeText => EnumDescriptorText.Get(VisitType, VisitTypeDescriptorList.List);
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
