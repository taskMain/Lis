using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 互认项目配置列表查询条件；编码与状态筛选可选，缺省表示全部，同时传入时取交集。
/// </summary>
/// <remarks>按标准项目编码升序返回，不分页；不提供名称、项目类型、分类或分组筛选。</remarks>
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
  /// 标准项目编码字面包含匹配条件；`%`、`_` 按普通字符处理，不作为通配符。不传表示不按编码过滤，传入则须为非空白文本。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public string? StandardProjectCode { get; init; }
  /// <summary>
  /// 配置状态筛选条件；不传表示启用与停用的配置都返回。
  /// </summary>
  [EnumDataType(typeof(ConfigurationStatus), ErrorMessage = "参数校验失败：配置状态无效。")]
  public ConfigurationStatus? ConfigurationStatus { get; init; }
}
