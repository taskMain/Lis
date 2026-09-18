namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>当前有效标准目录的分组节点。</summary>
public sealed record EffectiveMedicalStandardCatalogGroupReadModel
{
  /// <summary>分组名称。</summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>该分组下的项目集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogItemReadModel> Items { get; init; } = [];
}
