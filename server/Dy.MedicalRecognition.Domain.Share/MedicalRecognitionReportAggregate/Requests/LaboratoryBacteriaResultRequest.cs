namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 细菌鉴定结果的提交输入；每条内含药敏结果集合。
/// </summary>
public sealed record LaboratoryBacteriaResultRequest
{
  /// <summary>来源明细标识。应填；提供时同一报告版本内不得重复。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>来源菌种编码。应填。</summary>
  public string? SourceOrganismCode { get; init; }
  /// <summary>来源菌种名称。检出具体菌种时必填；培养未检出时不要求。</summary>
  public string? SourceOrganismName { get; init; }
  /// <summary>来源结果原文。必填。</summary>
  public string SourceResultText { get; init; } = string.Empty;
  /// <summary>检测结论。必填；培养未检出时仍提交一条以表达未检出。</summary>
  public string DetectionConclusion { get; init; } = string.Empty;
  /// <summary>菌落计数。应填。</summary>
  public string? ColonyCount { get; init; }
  /// <summary>培养基。应填。</summary>
  public string? CultureMedium { get; init; }
  /// <summary>培养时间，以来源文本承载。应填。</summary>
  public string? CultureTime { get; init; }
  /// <summary>培养条件。应填。</summary>
  public string? CultureCondition { get; init; }
  /// <summary>发现方式。应填。</summary>
  public string? DiscoveryMethod { get; init; }
  /// <summary>检测方法。应填。</summary>
  public string? DetectionMethod { get; init; }
  /// <summary>详细描述。应填。</summary>
  public string? Description { get; init; }
  /// <summary>检测仪器编码。应填。</summary>
  public string? InstrumentCode { get; init; }
  /// <summary>检测仪器名称。应填。</summary>
  public string? InstrumentName { get; init; }
  /// <summary>试验板编码。应填。</summary>
  public string? TestPanelCode { get; init; }
  /// <summary>试验板名称。应填。</summary>
  public string? TestPanelName { get; init; }
  /// <summary>检测人 ID。应填且与名称成对。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。应填且与标识成对。</summary>
  public string? InspectorName { get; init; }
  /// <summary>本条细菌鉴定结果下的药敏结果集合；受试药物名称、来源结论与展示序号必填。</summary>
  public IReadOnlyList<LaboratoryAntimicrobialSusceptibilityRequest> Susceptibilities { get; init; } = [];
}
