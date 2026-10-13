using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;

/// <summary>
/// 医院管理员入口的本院区互认项目金额列表查询条件；院区必填，标准项目编码与关键字可选，缺省表示全部。
/// </summary>
/// <remarks>
/// 组织与医院只取自可信上下文，请求不提交也不得覆盖，因此本请求不含组织与医院字段；
/// 请求院区必须属于可信医院，不属于即拒绝，不返回其他医院的金额、也不降级为空集合。
/// 按项目类型、分类名称、分组名称、标准项目名称逐级升序返回，名称相同再按分类、分组、项目与配置标识升序兜底，不分页；不提供操作人、操作时间、内部标识、状态或授权结论字段。
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
  /// <summary>
  /// 关键字字面包含匹配条件，同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，任一列命中即返回；
  /// 比较不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。
  /// 缺失或去首尾空白后为空视为未提供，不附加过滤，因此不声明必填、也不拒绝空白文本。
  /// </summary>
  public string? Keyword { get; init; }
}
