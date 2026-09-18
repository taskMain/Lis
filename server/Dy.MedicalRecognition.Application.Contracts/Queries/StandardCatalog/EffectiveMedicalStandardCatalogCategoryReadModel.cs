namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>当前有效标准目录的分类节点。</summary>
public sealed record EffectiveMedicalStandardCatalogCategoryReadModel
{
  /// <summary>分类名称。</summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>该分类下的分组集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogGroupReadModel> Groups { get; init; } = [];
}
