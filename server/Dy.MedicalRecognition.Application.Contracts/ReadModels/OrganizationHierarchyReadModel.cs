namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record OrganizationHierarchyReadModel
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
  /// 医院集合
  /// </summary>
  public object Hospitals { get; set; }
}
