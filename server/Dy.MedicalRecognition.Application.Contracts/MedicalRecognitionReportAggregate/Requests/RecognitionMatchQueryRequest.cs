using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 互认匹配查询请求：医院 HIS 在拟开项目时查询同一患者在当前组织内仍可使用的结果。
/// </summary>
/// <remarks>
/// 接收组织、医院与院区取自可信调用身份，操作人取自登录上下文，调用方都不提交；
/// 查询截止点取平台收到本次查询的时间，因此不接收挂号时间、入院时间与统一就诊时间；
/// 项目集合至少包含一个拟开标准项目，每个项目明确检查或检验类型与平台选定的标准项目编码。
/// </remarks>
public sealed record RecognitionMatchQueryRequest
{
  /// <summary>
  /// 患者证件类型代码；缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：证件类型代码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：证件类型代码不能是空白。")]
  public string IdentityDocumentTypeCode { get; init; } = string.Empty;
  /// <summary>
  /// 患者证件号码；缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：证件号码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：证件号码不能是空白。")]
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>
  /// 本次来源就诊类型；只接受门诊、急诊、住院、体检与其他五个已登记取值，未登记取值由请求校验拒绝。
  /// </summary>
  [EnumDataType(typeof(VisitType), ErrorMessage = "参数校验失败：就诊类型无效。")]
  public VisitType VisitType { get; init; }
  /// <summary>
  /// 本次来源就诊流水号；缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：就诊流水号不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：就诊流水号不能是空白。")]
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>
  /// 拟开标准项目集合；至少一个项目，每个项目的项目类型与标准项目编码由请求校验逐项核对。
  /// </summary>
  /// <remarks>
  /// 集合为空时平台无法形成任何匹配，因此按必填拒绝；元素的必填由校验入口递归进入元素后判定。
  /// </remarks>
  [Required(ErrorMessage = "参数校验失败：拟开标准项目不能为空。")]
  [MinLength(1, ErrorMessage = "参数校验失败：拟开标准项目至少提供一项。")]
  public IReadOnlyList<RecognitionMatchProposedItemRequest> ProposedItems { get; init; } = [];
}
