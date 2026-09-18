namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 检查部位的只读返回数据。
/// </summary>
public sealed record ExaminationSiteReadModel
{
  /// <summary>部位名称。</summary>
  public string SiteName { get; init; } = string.Empty;
  /// <summary>来源部位编码。</summary>
  public string? SourceSiteCode { get; init; }
}
