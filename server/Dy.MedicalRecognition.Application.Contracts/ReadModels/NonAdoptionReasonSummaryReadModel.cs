namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 汇总行内嵌的不采纳原因汇总项：原因代码、名称与该分组行内的次数与占比。
/// </summary>
public sealed record NonAdoptionReasonSummaryReadModel
{
  /// <summary>平台统一的不采纳原因代码；「其他」原因统一归入其他代码。</summary>
  public string ReasonCode { get; init; } = string.Empty;
  /// <summary>不采纳原因名称；取自平台统一原因定义。</summary>
  public string ReasonName { get; init; } = string.Empty;
  /// <summary>该分组行内此原因的不采纳次数。</summary>
  public int Count { get; init; }
  /// <summary>
  /// 该原因次数占本行不采纳总次数的比例；行内不采纳次数为零时不计算，
  /// 占比分母取行内不采纳次数（S6-D3 确认口径）。
  /// </summary>
  public decimal Ratio { get; init; }
}
