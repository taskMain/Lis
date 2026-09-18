using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>
/// 分类列表查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record MedicalStandardCategoryListQueryRequest
{
  /// <summary>
  /// 项目类型筛选条件；不传表示不按类型过滤，传入则须为已定义类型。
  /// </summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
}
