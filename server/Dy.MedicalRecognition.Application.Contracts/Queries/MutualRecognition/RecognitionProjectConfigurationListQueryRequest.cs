using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;

/// <summary>
/// 互认项目配置列表查询条件；关键字与状态筛选可选，缺省表示全部，同时传入时取交集。
/// </summary>
/// <remarks>按项目类型、分类名称、分组名称、标准项目名称逐级升序返回，名称相同再按分类、分组、项目与配置标识升序兜底，不分页；关键字按字面包含同时匹配标准项目编码、名称、所属分类名称与所属分组名称（S2-D29）。</remarks>
public sealed record RecognitionProjectConfigurationListQueryRequest
{
  /// <summary>
  /// 配置所属组织编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  /// <remarks>这里只表达查询目标组织，不构成授权结论；该组织是否属于调用方授权范围由服务端校验，越权时拒绝而不降级为空集合。</remarks>
  [Required(ErrorMessage = "参数校验失败：组织编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>
  /// 关键字字面包含匹配条件，同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，任一列命中即返回；比较不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。不传表示不按关键字过滤，传入则须为非空白文本。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：关键字不能是空白。")]
  public string? Keyword { get; init; }
  /// <summary>
  /// 配置状态筛选条件；不传表示启用与停用的配置都返回。
  /// </summary>
  [EnumDataType(typeof(ConfigurationStatus), ErrorMessage = "参数校验失败：配置状态无效。")]
  public ConfigurationStatus? ConfigurationStatus { get; init; }
}
