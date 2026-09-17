using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 平台管理员入口的互认项目金额列表查询条件；组织、医院与院区必填，标准项目编码可选，缺省表示全部。
/// </summary>
/// <remarks>
/// 组织、医院、院区取值与保存入口一致：三个值按请求使用，服务端校验其存在、启用与父子归属，
/// 不要求请求组织等于可信上下文组织；这里只表达查询目标，不构成授权结论。
/// 按标准项目编码升序返回，不分页；不提供操作人、操作时间、内部标识、状态或授权结论字段。
/// </remarks>
public sealed record RecognitionAmountListQueryRequest
{
  /// <summary>
  /// 金额所属组织编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：组织编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>
  /// 金额所属医院编码；必填，空值或纯空白由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：医院编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：医院编码不能是空白。")]
  public string HospitalCode { get; init; } = string.Empty;
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
