namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionUsageDetailReadModel
{
  /// <summary>
  /// 匹配记录ID
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 匹配项ID
  /// </summary>
  public Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 互认匹配生成时间
  /// </summary>
  public DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 本次来源就诊类型
  /// </summary>
  public object VisitType { get; set; }
  /// <summary>
  /// 本次来源就诊流水号
  /// </summary>
  public string VisitSerialNo { get; set; }
  /// <summary>
  /// 来源归属
  /// </summary>
  public object Source { get; set; }
  /// <summary>
  /// 接收归属
  /// </summary>
  public object Receiver { get; set; }
  /// <summary>
  /// 互认项目
  /// </summary>
  public object Item { get; set; }
  /// <summary>
  /// 业务时间
  /// </summary>
  public DateTime BusinessTime { get; set; }
  /// <summary>
  /// 未反馈
  /// </summary>
  public bool IsUnprocessed { get; set; }
  /// <summary>
  /// 患者脱敏信息
  /// </summary>
  public string MaskedPatientInfo { get; set; }
  /// <summary>
  /// 互认科室ID
  /// </summary>
  public string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称
  /// </summary>
  public string RecognitionDeptName { get; set; }
  /// <summary>
  /// 互认医生ID
  /// </summary>
  public string RecognitionDoctorId { get; set; }
  /// <summary>
  /// 互认医生名称
  /// </summary>
  public string RecognitionDoctorName { get; set; }
  /// <summary>
  /// 处理事实
  /// </summary>
  public object ProcessingResult { get; set; }
  /// <summary>
  /// 引用事实
  /// </summary>
  public object Reference { get; set; }
}
