namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 检查部位的查询投影行。
/// </summary>
/// <remarks>投影携带检查项目归属，使按检查项目集合一次读回的部位仍能归到对应的命中项目下。</remarks>
public sealed record ExaminationSiteView
{
  /// <summary>所属检查项目标识，检查部位的固定归属。</summary>
  public Guid ExaminationItemId { get; init; }
  /// <summary>部位名称。</summary>
  public string SiteName { get; init; } = string.Empty;
  /// <summary>来源部位编码。</summary>
  public string? SourceSiteCode { get; init; }
}
