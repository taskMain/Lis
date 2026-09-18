namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 细菌鉴定结果的只读返回数据；每条内含按展示序号返回的药敏结果。
/// </summary>
public sealed record LaboratoryBacteriaResultReadModel
{
  /// <summary>来源明细标识。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>来源菌种编码。</summary>
  public string? SourceOrganismCode { get; init; }
  /// <summary>来源菌种名称。</summary>
  public string? SourceOrganismName { get; init; }
  /// <summary>来源结果原文。</summary>
  public string SourceResultText { get; init; } = string.Empty;
  /// <summary>检测结论。</summary>
  public string DetectionConclusion { get; init; } = string.Empty;
  /// <summary>菌落计数。</summary>
  public string? ColonyCount { get; init; }
  /// <summary>培养基。</summary>
  public string? CultureMedium { get; init; }
  /// <summary>培养时间，来源文本。</summary>
  public string? CultureTime { get; init; }
  /// <summary>培养条件。</summary>
  public string? CultureCondition { get; init; }
  /// <summary>发现方式。</summary>
  public string? DiscoveryMethod { get; init; }
  /// <summary>检测方法。</summary>
  public string? DetectionMethod { get; init; }
  /// <summary>详细描述。</summary>
  public string? Description { get; init; }
  /// <summary>检测仪器编码。</summary>
  public string? InstrumentCode { get; init; }
  /// <summary>检测仪器名称。</summary>
  public string? InstrumentName { get; init; }
  /// <summary>试验板编码。</summary>
  public string? TestPanelCode { get; init; }
  /// <summary>试验板名称。</summary>
  public string? TestPanelName { get; init; }
  /// <summary>检测人 ID。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。</summary>
  public string? InspectorName { get; init; }
  /// <summary>本条细菌鉴定结果下的药敏结果；按展示序号返回。</summary>
  public IReadOnlyList<LaboratoryAntimicrobialSusceptibilityReadModel> Susceptibilities { get; init; } = [];
}
