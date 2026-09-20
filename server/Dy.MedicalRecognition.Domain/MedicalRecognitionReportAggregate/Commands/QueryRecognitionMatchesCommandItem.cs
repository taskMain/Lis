namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 互认匹配查询命令中的一个拟开项目：项目类型与平台选定的标准项目编码。
/// </summary>
/// <remarks>
/// 平台不接收院内项目编码，也不维护院内项目与互认项目的对照；
/// 同一次查询重复提供相同标准项目编码时由领域层去重，只处理一次。
/// </remarks>
public sealed record QueryRecognitionMatchesCommandItem
{
  /// <summary>
  /// 项目类型；用于核对本次提交的类型与该项目互认配置归属的标准目录分类一致。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 平台选定的标准项目编码；用于按当前可信组织读取互认配置与标准目录层级。
  /// </summary>
  public string StandardProjectCode { get; init; } = string.Empty;
}
