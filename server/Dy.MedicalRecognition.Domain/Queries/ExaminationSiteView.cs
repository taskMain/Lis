namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 检查部位的查询投影行。
/// </summary>
public sealed record ExaminationSiteView
{
  /// <summary>部位名称。</summary>
  public string SiteName { get; init; } = string.Empty;
  /// <summary>来源部位编码。</summary>
  public string? SourceSiteCode { get; init; }
}
