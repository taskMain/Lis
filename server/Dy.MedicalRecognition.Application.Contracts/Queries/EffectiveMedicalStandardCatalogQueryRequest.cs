using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 当前有效标准目录查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record EffectiveMedicalStandardCatalogQueryRequest
{
  /// <summary>
  /// 项目类型筛选条件；不传表示不按类型过滤，传入则须为已定义类型。
  /// </summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
  /// <summary>
  /// 分类名称包含匹配条件；按字面匹配，非空值时不可为空白。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类名称不能是空白。")]
  public string? CategoryName { get; init; }
}
