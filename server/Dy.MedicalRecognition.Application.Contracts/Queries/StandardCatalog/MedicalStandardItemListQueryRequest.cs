using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>
/// 标准项目列表查询条件；筛选条件均可选，同时传入时取交集。
/// </summary>
public sealed record MedicalStandardItemListQueryRequest
{
  /// <summary>
  /// 所属分类筛选条件；不传表示不按分类过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空Guid。")]
  public Guid? CategoryId { get; init; }
  /// <summary>
  /// 所属分组筛选条件；不传表示不按分组过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分组ID不能为空Guid。")]
  public Guid? GroupId { get; init; }
  /// <summary>
  /// 标准项目编码包含匹配条件；按字面匹配，非空值时不可为空白。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：编码不能是空白。")]
  public string? Code { get; init; }
  /// <summary>
  /// 标准项目名称包含匹配条件；匹配规则同编码。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：名称不能是空白。")]
  public string? Name { get; init; }
  /// <summary>
  /// 启用状态筛选条件；不传表示启用与停用的项目都返回。
  /// </summary>
  public bool? IsValid { get; init; }
}
