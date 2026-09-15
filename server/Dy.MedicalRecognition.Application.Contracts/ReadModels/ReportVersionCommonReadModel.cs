namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record ReportVersionCommonReadModel
{
  /// <summary>
  /// 患者姓名
  /// </summary>
  public string PatientName { get; set; }
  /// <summary>
  /// 患者性别代码
  /// </summary>
  public string PatientGenderCode { get; set; }
  /// <summary>
  /// 患者出生日期
  /// </summary>
  public DateTime PatientBirthDate { get; set; }
  /// <summary>
  /// 患者联系电话
  /// </summary>
  public string PatientPhoneNumber { get; set; }
  /// <summary>
  /// 报告时年龄
  /// </summary>
  public string AgeAtReport { get; set; }
  /// <summary>
  /// 证件类型代码
  /// </summary>
  public string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 证件号码
  /// </summary>
  public string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 就诊类型
  /// </summary>
  public object VisitType { get; set; }
  /// <summary>
  /// 就诊流水号
  /// </summary>
  public string VisitSerialNo { get; set; }
  /// <summary>
  /// 来源报告名称
  /// </summary>
  public string SourceReportName { get; set; }
  /// <summary>
  /// 申请科室ID
  /// </summary>
  public string ApplicationDeptId { get; set; }
  /// <summary>
  /// 申请科室名称
  /// </summary>
  public string ApplicationDeptName { get; set; }
  /// <summary>
  /// 申请医生ID
  /// </summary>
  public string ApplicationDoctorId { get; set; }
  /// <summary>
  /// 申请医生名称
  /// </summary>
  public string ApplicationDoctorName { get; set; }
  /// <summary>
  /// 执行科室ID
  /// </summary>
  public string ExecutionDeptId { get; set; }
  /// <summary>
  /// 执行科室名称
  /// </summary>
  public string ExecutionDeptName { get; set; }
  /// <summary>
  /// 报告科室ID
  /// </summary>
  public string ReportDeptId { get; set; }
  /// <summary>
  /// 报告科室名称
  /// </summary>
  public string ReportDeptName { get; set; }
  /// <summary>
  /// 报告医生ID
  /// </summary>
  public string ReportDoctorId { get; set; }
  /// <summary>
  /// 报告医生名称
  /// </summary>
  public string ReportDoctorName { get; set; }
  /// <summary>
  /// 审核医生ID
  /// </summary>
  public string ReviewDoctorId { get; set; }
  /// <summary>
  /// 审核医生名称
  /// </summary>
  public string ReviewDoctorName { get; set; }
  /// <summary>
  /// 审核时间
  /// </summary>
  public DateTime ReviewTime { get; set; }
  /// <summary>
  /// 住院号
  /// </summary>
  public string InpatientNo { get; set; }
  /// <summary>
  /// 病区名称
  /// </summary>
  public string WardName { get; set; }
  /// <summary>
  /// 病房名称
  /// </summary>
  public string RoomName { get; set; }
  /// <summary>
  /// 床位号
  /// </summary>
  public string BedNo { get; set; }
  /// <summary>
  /// 申请时间
  /// </summary>
  public DateTime ApplicationTime { get; set; }
  /// <summary>
  /// 报告时间
  /// </summary>
  public DateTime ReportTime { get; set; }
  /// <summary>
  /// 来源保密标识
  /// </summary>
  public string SourceConfidentialFlag { get; set; }
}
