namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 互认匹配查询的返回值：本次查询是否命中，以及命中时按报告归并的匹配内容。
/// </summary>
/// <remarks>
/// 空匹配是成功结果：记录标识与生成时间为空、报告集合为空，平台不创建匹配记录也不登记事件；
/// 报告集合按报告归并，同一联合报告命中多个互认项目时报告公共信息只返回一次，各项目在报告的匹配项目集合下分别成项。
/// </remarks>
public sealed record RecognitionMatchesResponseReadModel
{
  /// <summary>本次查询是否存在匹配。</summary>
  public bool HasMatches { get; init; }
  /// <summary>本次互认匹配记录标识；空匹配时为 <see langword="null"/>。</summary>
  public Guid? RecognitionMatchRecordId { get; init; }
  /// <summary>本次匹配生成时间，取平台保存匹配记录的时间；空匹配时为 <see langword="null"/>。</summary>
  public DateTime? MatchCreatedTime { get; init; }
  /// <summary>匹配到的报告集合；空匹配时为空集合。</summary>
  public IReadOnlyList<RecognitionMatchReportReadModel> Reports { get; init; } = [];
}
