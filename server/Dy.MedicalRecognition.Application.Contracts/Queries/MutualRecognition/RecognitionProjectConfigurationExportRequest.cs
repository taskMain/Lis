using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;

/// <summary>
/// 互认项目配置导出条件；组织必填且须等于服务端可信当前组织，关键字可选，传入空白文本按校验拒绝。
/// </summary>
/// <remarks>
/// 导出与对应列表查询同一行集与排序，同时包含启用与停用的配置，因此不设配置状态筛选字段；
/// 导出取全部记录，不承载分页对象。关键字口径与列表请求一致：按字面包含同时匹配标准项目编码、标准项目名称、
/// 所属分类名称与所属分组名称，不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。
/// </remarks>
public sealed record RecognitionProjectConfigurationExportRequest
{
  /// <summary>
  /// 导出目标组织编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  /// <remarks>这里只表达导出目标组织；该组织是否为服务端可信当前组织由应用层校验，不一致时拒绝而不降级为空集合。</remarks>
  [Required(ErrorMessage = "参数校验失败：组织编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>
  /// 关键字字面包含匹配条件，同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，任一列命中即导出；比较不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。不传表示不按关键字过滤，传入则须为非空白文本。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：关键字不能是空白。")]
  public string? Keyword { get; init; }
}
