using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 提交引用结果请求：医院 HIS 在将已采纳结果写入本次病历后反馈实际引用事实。
/// </summary>
/// <remarks>
/// 引用项目按互认匹配项标识定位，一次请求允许包含来自不同匹配记录的项目并整批原子保存；
/// 接收组织、医院、院区、本次来源就诊与互认时间由平台按各匹配项已保存的事实取得，调用方不重复提交；
/// 引用时间、引用科室与引用医生属于项目级实际引用事实，不作为组级公共字段。
/// </remarks>
public sealed record RecognitionReferenceSubmissionRequest
{
  /// <summary>
  /// 本次实际引用项目集合；至少一项，元素的匹配项标识、实际引用时间与引用科室与医生由请求校验逐项核对。
  /// </summary>
  /// <remarks>
  /// 集合不要求完整覆盖匹配记录或全部已采纳项目；未包含的匹配项只表示平台本次未收到其引用事实。
  /// </remarks>
  [Required(ErrorMessage = "参数校验失败：引用项目不能为空。")]
  [MinLength(1, ErrorMessage = "参数校验失败：引用项目至少提供一项。")]
  public IReadOnlyList<RecognitionReferenceItemRequest> ReferenceItems { get; init; } = [];
}
