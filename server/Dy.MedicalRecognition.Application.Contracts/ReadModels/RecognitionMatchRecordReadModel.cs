namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionMatchRecordReadModel
{
  /// <summary>
  /// 互认匹配记录ID
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 互认匹配生成时间
  /// </summary>
  public DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 接收归属
  /// </summary>
  public object Receiver { get; set; }
  /// <summary>
  /// 患者脱敏信息
  /// </summary>
  public string MaskedPatientInfo { get; set; }
  /// <summary>
  /// 本次来源就诊类型
  /// </summary>
  public object VisitType { get; set; }
  /// <summary>
  /// 本次来源就诊流水号
  /// </summary>
  public string VisitSerialNo { get; set; }
  /// <summary>
  /// 是否已反馈
  /// </summary>
  public bool IsProcessed { get; set; }
  /// <summary>
  /// 互认时间
  /// </summary>
  public DateTime RecognitionTime { get; set; }
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
  /// 匹配项目集合
  /// </summary>
  public object MatchItems { get; set; }
}
