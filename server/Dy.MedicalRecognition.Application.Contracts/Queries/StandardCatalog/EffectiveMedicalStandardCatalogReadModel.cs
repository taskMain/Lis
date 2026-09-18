namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>当前有效标准医疗项目目录根对象。</summary>
public sealed record EffectiveMedicalStandardCatalogReadModel
{
  /// <summary>按项目类型分组的目录集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogTypeReadModel> ItemTypes { get; init; } = [];
}
