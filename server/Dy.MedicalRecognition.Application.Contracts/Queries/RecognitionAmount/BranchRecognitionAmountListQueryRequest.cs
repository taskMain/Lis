using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;

/// <summary>
/// 医院管理员入口的本院区互认项目金额列表查询条件；院区必填，标准项目编码可选，缺省表示全部。
/// </summary>
/// <remarks>
/// 组织与医院只取自可信上下文，请求不提交也不得覆盖，因此本请求不含组织与医院字段；
/// 请求院区必须属于可信医院，不属于即拒绝，不返回其他医院的金额、也不降级为空集合。
/// 按标准项目编码升序返回，不分页；不提供操作人、操作时间、内部标识、状态或授权结论字段。
/// </remarks>
public sealed record BranchRecognitionAmountListQueryRequest
{
  /// <summary>
  /// 金额所属院区编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：院区编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目编码字面包含匹配条件；`%`、`_` 按普通字符处理，不作为通配符。不传表示不按编码过滤，传入则须为非空白文本。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public string? StandardProjectCode { get; init; }
}
