namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 检查部位的提交输入；随所属检查项目保存，不在报告中重复归属。
/// </summary>
public sealed record ExaminationSiteRequest
{
  /// <summary>来源部位编码。应填。</summary>
  public string? SourceSiteCode { get; init; }
  /// <summary>部位名称。必填。</summary>
  public string SiteName { get; init; } = string.Empty;
}
