namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 检查项目的查询投影行。
/// </summary>
public sealed record ExaminationItemView
{
  /// <summary>检查项目标识；检查部位按本标识归属。</summary>
  public Guid ItemId { get; init; }
  /// <summary>来源项目名称。</summary>
  public string SourceProjectName { get; init; } = string.Empty;
  /// <summary>来源项目编码。</summary>
  public string? SourceProjectCode { get; init; }
  /// <summary>互认项目编码。</summary>
  public string? StandardProjectCode { get; init; }
}
