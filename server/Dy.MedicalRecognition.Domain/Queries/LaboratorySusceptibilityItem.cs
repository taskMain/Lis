namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 药敏结果的查询投影行。
/// </summary>
public sealed record LaboratorySusceptibilityItem
{
  /// <summary>来源明细标识。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>受试药物编码。</summary>
  public string? DrugCode { get; init; }
  /// <summary>受试药物名称。</summary>
  public string DrugName { get; init; } = string.Empty;
  /// <summary>药敏结论编码。</summary>
  public string? SusceptibilityCode { get; init; }
  /// <summary>来源结论。</summary>
  public string SourceConclusionText { get; init; } = string.Empty;
  /// <summary>抗药结果编码。</summary>
  public string? ResistanceResultCode { get; init; }
  /// <summary>纸片含药量，来源文本。</summary>
  public string? DiskContent { get; init; }
  /// <summary>最低抑菌浓度，来源文本。</summary>
  public string? MicValue { get; init; }
  /// <summary>抑菌环直径，来源文本。</summary>
  public string? InhibitionZoneDiameter { get; init; }
  /// <summary>参考值。</summary>
  public string? ReferenceValue { get; init; }
  /// <summary>展示序号。</summary>
  public int DisplayOrder { get; init; }
  /// <summary>检测人 ID。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。</summary>
  public string? InspectorName { get; init; }
  /// <summary>检测方法。</summary>
  public string? TestingMethod { get; init; }
  /// <summary>试验板序号。</summary>
  public string? TestPanelOrder { get; init; }
}
