namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record ExaminationReportContentReadModel
{
  /// <summary>
  /// 来源检查类型编码
  /// </summary>
  public string SourceExaminationTypeCode { get; set; }
  /// <summary>
  /// 来源检查类型名称
  /// </summary>
  public string SourceExaminationTypeName { get; set; }
  /// <summary>
  /// 报告备注
  /// </summary>
  public string ReportRemark { get; set; }
  /// <summary>
  /// 整体异常标识
  /// </summary>
  public string OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 检查所见
  /// </summary>
  public string Findings { get; set; }
  /// <summary>
  /// 检查结论
  /// </summary>
  public string Conclusion { get; set; }
  /// <summary>
  /// 病情描述
  /// </summary>
  public string ConditionDescription { get; set; }
  /// <summary>
  /// 检查目的
  /// </summary>
  public string ExaminationPurpose { get; set; }
  /// <summary>
  /// 来源诊断编码
  /// </summary>
  public string SourceDiagnosisCode { get; set; }
  /// <summary>
  /// 来源诊断名称
  /// </summary>
  public string SourceDiagnosisName { get; set; }
  /// <summary>
  /// 实际检查时间
  /// </summary>
  public DateTime ExaminationTime { get; set; }
  /// <summary>
  /// 检查医生ID
  /// </summary>
  public string ExaminerId { get; set; }
  /// <summary>
  /// 检查医生名称
  /// </summary>
  public string ExaminerName { get; set; }
  /// <summary>
  /// 来源影像状态
  /// </summary>
  public object SourceImageStatus { get; set; }
  /// <summary>
  /// 影像调阅地址
  /// </summary>
  public string ImageAccessUrl { get; set; }
  /// <summary>
  /// 检查方法
  /// </summary>
  public string ExaminationMethod { get; set; }
  /// <summary>
  /// 设备编码
  /// </summary>
  public string DeviceCode { get; set; }
  /// <summary>
  /// 设备名称
  /// </summary>
  public string DeviceName { get; set; }
  /// <summary>
  /// 检查项目
  /// </summary>
  public object Items { get; set; }
}
