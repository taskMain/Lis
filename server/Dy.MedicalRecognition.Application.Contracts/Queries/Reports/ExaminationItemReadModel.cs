namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 检查项目的只读返回数据。
/// </summary>
public sealed record ExaminationItemReadModel
{
  /// <summary>来源项目名称。</summary>
  public string SourceProjectName { get; init; } = string.Empty;
  /// <summary>来源项目编码。</summary>
  public string? SourceProjectCode { get; init; }
  /// <summary>互认项目编码。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>检查部位；项目没有明确部位时为空集合。</summary>
  public IReadOnlyList<ExaminationSiteReadModel> Sites { get; init; } = [];
}
