using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 获取引用详情请求：医院 HIS 在医生采纳匹配后按本次就诊取得可写入病历的项目内容。
/// </summary>
/// <remarks>
/// 请求只提供医院必然掌握的患者证件与本次来源就诊；当前医院与院区取自可信调用身份；
/// 平台患者标识、互认匹配记录标识、互认匹配项标识与互认时间都由平台按本次就诊定位，调用方不提交。
/// </remarks>
public sealed record RecognitionCitationDetailRequest
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
}
