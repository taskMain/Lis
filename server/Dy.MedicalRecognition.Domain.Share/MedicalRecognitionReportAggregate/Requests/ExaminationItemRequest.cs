namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 检查项目的提交输入；每条内含检查部位集合，项目没有明确部位时部位集合允许为空。
/// </summary>
public sealed record ExaminationItemRequest
{
  /// <summary>来源项目名称。必填。</summary>
  public string SourceProjectName { get; init; } = string.Empty;
  /// <summary>来源项目编码。应填。</summary>
  public string? SourceProjectCode { get; init; }
  /// <summary>互认项目编码。应填；缺失不影响接收，只影响互认匹配资格。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>本条检查项目下的检查部位集合；每条部位名称必填。</summary>
  public IReadOnlyList<ExaminationSiteRequest> Sites { get; init; } = [];
}
