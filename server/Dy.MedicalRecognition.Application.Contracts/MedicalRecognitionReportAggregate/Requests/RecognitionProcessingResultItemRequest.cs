using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 互认匹配项决定的提交输入；一条记录对应本组内一个匹配项的最终决定。
/// </summary>
/// <remarks>
/// 采纳时不得带不采纳原因，不采纳时必须提供平台统一原因代码，选择其他情形确需复查时补充说明必填；
/// 采纳与不采纳的取值组合、原因值域与补充说明的对应关系由领域校验整批判定。
/// </remarks>
public sealed record RecognitionProcessingResultItemRequest
{
  /// <summary>
  /// 被决定的互认匹配项标识；空标识由请求校验拒绝。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认匹配项ID不能为空。")]
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>
  /// 采纳或不采纳结果；只接受采纳与不采纳两个已登记取值，未登记取值由请求校验拒绝。
  /// </summary>
  [EnumDataType(typeof(RecognitionResult), ErrorMessage = "参数校验失败：互认结果无效。")]
  public RecognitionResult Result { get; init; }
  /// <summary>
  /// 不采纳原因；仅在不采纳时填写，采纳时不得带有原因。
  /// </summary>
  [EnumDataType(typeof(RecognitionNonAdoptionReason), ErrorMessage = "参数校验失败：不采纳原因无效。")]
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  /// <summary>
  /// 不采纳的补充说明；不采纳时可选填写，选择其他情形确需复查时必填。
  /// </summary>
  [StringLength(600, ErrorMessage = "参数校验失败：不采纳补充说明长度不能超过 600。")]
  public string? NonAdoptionDescription { get; init; }
}
