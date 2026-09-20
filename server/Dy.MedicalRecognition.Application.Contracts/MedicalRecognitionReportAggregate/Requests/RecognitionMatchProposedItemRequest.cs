using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 拟开标准项目的提交输入；每个项目明确检查或检验类型并直接提供平台选定的标准项目编码。
/// </summary>
/// <remarks>
/// 平台不接收院内项目编码，也不维护院内项目与互认项目的对照；
/// 同一次查询重复提供相同标准项目编码时只处理一次，由领域校验去重。
/// </remarks>
public sealed record RecognitionMatchProposedItemRequest
{
  /// <summary>
  /// 项目类型；只接受检验与检查两个已登记取值，未登记取值由请求校验拒绝。
  /// </summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 平台选定的标准项目编码；缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：标准项目编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public string StandardProjectCode { get; init; } = string.Empty;
}
