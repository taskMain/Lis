namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record ExaminationSiteReadModel
{
  /// <summary>
  /// 部位名称
  /// </summary>
  public string SiteName { get; set; }
  /// <summary>
  /// 来源部位编码
  /// </summary>
  public string SourceSiteCode { get; set; }
}
