using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>当前有效标准目录的项目类型节点。</summary>
public sealed record EffectiveMedicalStandardCatalogTypeReadModel
{
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按"未知类型"安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>该类型下的分类集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogCategoryReadModel> Categories { get; init; } = [];
}
