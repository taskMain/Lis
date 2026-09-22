using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 互认匹配记录集合视图请求：按匹配记录标识取单记录，弹窗呈现，不分页。
/// </summary>
public sealed record RecognitionMatchRecordQueryRequest
{
  /// <summary>互认匹配记录标识；必填，为空 Guid 即拒绝。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认匹配记录标识不能为空Guid。")]
  public Guid RecognitionMatchRecordId { get; init; }
}
