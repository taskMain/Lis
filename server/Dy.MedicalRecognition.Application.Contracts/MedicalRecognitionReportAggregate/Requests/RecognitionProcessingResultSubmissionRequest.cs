using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 互认处理结果提交请求：医院 HIS 提交医生对本组全部匹配项作出的明确决定。
/// </summary>
/// <remarks>
/// 互认时间、互认科室与互认医生共同适用于本次提交的全部匹配项，只在组级提供一次；
/// 接收组织、医院与院区取自可信调用身份，操作人取自登录上下文，调用方都不提交；
/// 项目集合必须完整覆盖本组全部匹配项，缺项、重复项或夹带其他组的匹配项由领域校验整批拒绝。
/// </remarks>
[IPropertyChangedAware]
public partial record RecognitionProcessingResultSubmissionRequest : Dto
{
  /// <summary>
  /// 本次提交对应的互认匹配记录标识；空标识由请求校验拒绝。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认匹配记录ID不能为空。")]
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 医生实际完成本组决定的互认时间。
  /// </summary>
  /// <remarks>
  /// 互认时间不得早于匹配记录生成时间、不得晚于本次请求接收时间，两端含边界；
  /// 时间顺序与零值缺失由领域校验整批判定，请求校验只核对必填字段与标识类型。
  /// </remarks>
  public partial DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 互认科室在来源系统中的标识；使用字符串承载，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：互认科室ID不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：互认科室ID不能是空白。")]
  public partial string RecognitionDeptId { get; set; } = string.Empty;
  /// <summary>
  /// 互认科室名称；与互认科室标识成对提供，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：互认科室名称不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：互认科室名称不能是空白。")]
  public partial string RecognitionDeptName { get; set; } = string.Empty;
  /// <summary>
  /// 互认医生在来源系统中的人员标识；使用字符串承载，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：互认医生ID不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：互认医生ID不能是空白。")]
  public partial string RecognitionDoctorId { get; set; } = string.Empty;
  /// <summary>
  /// 互认医生名称；与互认医生标识成对提供，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：互认医生名称不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：互认医生名称不能是空白。")]
  public partial string RecognitionDoctorName { get; set; } = string.Empty;
  /// <summary>
  /// 组内全部互认匹配项的决定集合；至少一项，元素的匹配项标识与决定由请求校验逐项核对。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：互认匹配项决定不能为空。")]
  [MinLength(1, ErrorMessage = "参数校验失败：互认匹配项决定至少提供一项。")]
  public partial IReadOnlyList<RecognitionProcessingResultItemRequest> ProcessingResults { get; set; } = [];
}
