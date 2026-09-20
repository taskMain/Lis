namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 一次非空互认匹配查询的结果：已保存的匹配记录与本次形成的全部匹配项。
/// </summary>
/// <remarks>
/// 空匹配不创建任何记录，由 <see cref="Empty"/> 表达；匹配项按本次拟开项目的提交顺序排列，
/// 每项固定绑定一份报告版本，报告公共信息与项目级内容由应用服务按该绑定一次读取后组装为匹配响应；
/// 命中项目的标准项目名称随结果一并返回：名称实时取自本次组织的标准目录互认配置，应用层不再为取名称发起查询。
/// </remarks>
public sealed record RecognitionMatchResult
{
  /// <summary>
  /// 空匹配结果：记录标识与生成时间为空、匹配项集合为空，平台不创建记录、不登记事件。
  /// </summary>
  public static RecognitionMatchResult Empty { get; } = new();
  /// <summary>
  /// 本次互认匹配记录标识；空匹配时为 <see langword="null"/>。
  /// </summary>
  public Guid? RecognitionMatchRecordId { get; init; }
  /// <summary>
  /// 本次匹配生成时间，取平台保存匹配记录的时间；空匹配时为 <see langword="null"/>。
  /// </summary>
  public DateTime? MatchCreatedTime { get; init; }
  /// <summary>
  /// 本次形成的全部匹配项，按本次拟开项目的提交顺序排列；空匹配时为空集合。
  /// </summary>
  public IReadOnlyList<RecognitionMatchItem> MatchItems { get; init; } = [];
  /// <summary>
  /// 本次命中项目在标准目录中的展示名称，按标准项目编码索引；空匹配时为空映射。
  /// </summary>
  public IReadOnlyDictionary<string, string> StandardProjectNames { get; init; } =
    new Dictionary<string, string>(StringComparer.Ordinal);
}
