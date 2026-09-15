namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record OwnHospitalMatchParameterReadModel
{
  /// <summary>
  /// 本院报告匹配开关
  /// </summary>
  public bool OwnHospitalMatchEnabled { get; set; }
  /// <summary>
  /// 本院报告排除时长小时
  /// </summary>
  public int OwnHospitalExclusionHours { get; set; }
}
