namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record HospitalHierarchyReadModel
{
  /// <summary>
  /// 医院编码
  /// </summary>
  public string HospitalCode { get; set; }
  /// <summary>
  /// 医院名称
  /// </summary>
  public string HospitalName { get; set; }
  /// <summary>
  /// 院区集合
  /// </summary>
  public object Branches { get; set; }
}
