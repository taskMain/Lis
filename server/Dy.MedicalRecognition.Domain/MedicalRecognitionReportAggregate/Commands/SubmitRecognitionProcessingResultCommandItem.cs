using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 互认处理结果提交命令中的一个匹配项决定：匹配项标识、采纳或不采纳结果，以及不采纳原因与补充说明。
/// </summary>
/// <remarks>
/// 互认时间、互认科室与互认医生属于组级事实，不在本类型上重复提供；
/// 采纳时不得带不采纳原因，不采纳时必须提供原因代码，选择其他情形确需复查时补充说明必填；
/// 本类型不承载项目编码：处理结果表按匹配项标识定位，项目编码由该标识反查匹配项取得。
/// </remarks>
public sealed record SubmitRecognitionProcessingResultCommandItem
{
  /// <summary>
  /// 被决定的互认匹配项标识；必须属于本次提交的互认匹配记录。
  /// </summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>
  /// 该匹配项的采纳或不采纳结果。
  /// </summary>
  public RecognitionResult Result { get; init; }
  /// <summary>
  /// 不采纳原因；仅在不采纳时提供，采纳时必须为空。
  /// </summary>
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  /// <summary>
  /// 不采纳的补充说明；选择其他情形确需复查时必填，其余原因可选填写。
  /// </summary>
  public string? NonAdoptionDescription { get; init; }
}
