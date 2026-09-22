using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 明细与集合视图承载的互认处理结果事实：反馈状态、互认时间、处理结果决策与不采纳原因。
/// </summary>
public sealed record RecognitionProcessingResultDetailReadModel
{
  /// <summary>是否已反馈处理结果；未反馈的匹配项只进入提醒明细，不推断为不采纳。</summary>
  public bool IsProcessed { get; init; }
  /// <summary>处理结果的互认时间；采纳、不采纳与预计节省金额按该时间统计，未反馈时无值。</summary>
  public DateTime? RecognitionTime { get; init; }
  /// <summary>处理结果决策；首次保存后不可更改，未反馈时无值。</summary>
  public RecognitionResult? Decision { get; init; }
  /// <summary>
  /// 处理结果决策中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份决策文案。
  /// </summary>
  /// <remarks>未反馈项没有决策取值，返回 null 由页面显示「未反馈」。</remarks>
  public string? DecisionText => Decision is null ? null : EnumDescriptorText.GetOrNull(Decision.Value, RecognitionResultDescriptorList.List);
  /// <summary>不采纳原因代码；仅不采纳决策携带。</summary>
  public string? NonAdoptionReasonCode { get; init; }
  /// <summary>不采纳原因名称；取自平台统一原因定义。</summary>
  public string? NonAdoptionReasonName { get; init; }
  /// <summary>不采纳补充说明；只在明细与导出中展示，原因汇总不聚合该文本。</summary>
  public string? NonAdoptionSupplementDescription { get; init; }
  /// <summary>该采纳项的预计节省金额；只随采纳事实形成，不另建金额事实。</summary>
  public decimal EstimatedSavingAmount { get; init; }
}
