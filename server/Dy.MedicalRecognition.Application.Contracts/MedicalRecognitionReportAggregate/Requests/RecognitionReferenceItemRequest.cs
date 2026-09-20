using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 实际引用项目的提交输入；实际引用时间与引用科室、引用医生都是项目级事实。
/// </summary>
/// <remarks>
/// 一次请求中的不同引用项目允许具有不同的引用时间、引用科室或引用医生；
/// 实际引用时间不得早于该匹配项已保存的互认时间、不得晚于本次请求接收时间，由领域校验判定；
/// 引用科室与引用医生的标识和名称成对完整性由领域校验整批判定。
/// </remarks>
public sealed record RecognitionReferenceItemRequest
{
  /// <summary>
  /// 被引用的互认匹配项标识；空标识由请求校验拒绝。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认匹配项ID不能为空。")]
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>
  /// 项目级的实际引用时间。
  /// </summary>
  /// <remarks>时间顺序、零值缺失与未来时间由领域校验整批判定。</remarks>
  public DateTime ReferencedTime { get; init; }
  /// <summary>
  /// 引用科室在来源系统中的标识；使用字符串承载，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：引用科室ID不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：引用科室ID不能是空白。")]
  public string ReferenceDeptId { get; init; } = string.Empty;
  /// <summary>
  /// 引用科室名称；与引用科室标识成对提供，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：引用科室名称不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：引用科室名称不能是空白。")]
  public string ReferenceDeptName { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生在来源系统中的人员标识；使用字符串承载，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：引用医生ID不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：引用医生ID不能是空白。")]
  public string ReferenceDoctorId { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生名称；与引用医生标识成对提供，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：引用医生名称不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：引用医生名称不能是空白。")]
  public string ReferenceDoctorName { get; init; } = string.Empty;
}
