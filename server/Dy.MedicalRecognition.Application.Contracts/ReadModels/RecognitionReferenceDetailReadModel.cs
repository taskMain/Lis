namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionReferenceDetailReadModel
{
  /// <summary>
  /// 是否已引用
  /// </summary>
  public bool IsReferenced { get; set; }
  /// <summary>
  /// 实际引用时间
  /// </summary>
  public DateTime ReferenceTime { get; set; }
  /// <summary>
  /// 引用科室ID
  /// </summary>
  public string ReferenceDeptId { get; set; }
  /// <summary>
  /// 引用科室名称
  /// </summary>
  public string ReferenceDeptName { get; set; }
  /// <summary>
  /// 引用医生ID
  /// </summary>
  public string ReferenceDoctorId { get; set; }
  /// <summary>
  /// 引用医生名称
  /// </summary>
  public string ReferenceDoctorName { get; set; }
}
