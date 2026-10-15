using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;

/// <summary>
/// 医院管理员入口的本院区金额页导出条件；院区必填且由服务端校验属于可信医院，关键字可选，缺失或去首尾空白后为空视为未提供。
/// </summary>
/// <remarks>
/// 导出与对应列表查询同一行集与排序，行集由可信医院已建立的互认配置驱动、含未配置金额行；
/// 组织与医院只取自可信上下文，请求不提交也不得覆盖，因此本请求不含组织与医院字段；
/// 请求院区必须存在、启用且属于可信医院，不属于即拒绝，这里只表达导出目标院区，不构成授权结论。
/// 导出取全部记录，不承载金额状态筛选字段与分页对象，也不承载列表契约保留但页面不使用的标准项目编码字段。
/// 关键字口径与列表请求一致：按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，
/// 不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。
/// </remarks>
public sealed record BranchRecognitionAmountExportRequest
{
  /// <summary>
  /// 金额所属院区编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：院区编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>
  /// 关键字字面包含匹配条件，同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，任一列命中即导出；
  /// 比较不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。
  /// 缺失或去首尾空白后为空视为未提供，不附加过滤，因此不声明必填、也不拒绝空白文本。
  /// </summary>
  public string? Keyword { get; init; }
}
