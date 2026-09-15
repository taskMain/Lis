namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record ReceiverOrganizationReadModel
{
  /// <summary>
  /// 组织编码
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 组织名称
  /// </summary>
  public string OrganizationName { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public string HospitalCode { get; set; }
  /// <summary>
  /// 医院名称
  /// </summary>
  public string HospitalName { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public string BranchCode { get; set; }
  /// <summary>
  /// 院区名称
  /// </summary>
  public string BranchName { get; set; }
}
