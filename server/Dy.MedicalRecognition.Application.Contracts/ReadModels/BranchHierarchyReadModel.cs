namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record BranchHierarchyReadModel
{
  /// <summary>
  /// 院区编码
  /// </summary>
  public string BranchCode { get; set; }
  /// <summary>
  /// 院区名称
  /// </summary>
  public string BranchName { get; set; }
}
