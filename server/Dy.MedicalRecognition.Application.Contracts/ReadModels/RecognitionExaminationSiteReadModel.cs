namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 检查部位；匹配响应与引用详情共用同一部位形状。
/// </summary>
public sealed record RecognitionExaminationSiteReadModel
{
  /// <summary>部位名称。</summary>
  public string SiteName { get; init; } = string.Empty;
  /// <summary>来源部位编码；来源未提供时为空。</summary>
  public string? SourceSiteCode { get; init; }
}
