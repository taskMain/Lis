using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>
/// 分组列表查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record MedicalStandardGroupListQueryRequest
{
  /// <summary>
  /// 所属分类筛选条件；不传表示不按分类过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空Guid。")]
  public Guid? CategoryId { get; init; }
}
