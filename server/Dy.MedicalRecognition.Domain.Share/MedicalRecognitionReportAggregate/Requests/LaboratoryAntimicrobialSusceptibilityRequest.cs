namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 药敏结果的提交输入；挂在所属细菌鉴定结果下，展示序号在同一报告版本、同一细菌鉴定结果下不得重复。
/// </summary>
public sealed record LaboratoryAntimicrobialSusceptibilityRequest
{
  /// <summary>来源明细标识。应填。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>受试药物编码。应填。</summary>
  public string? DrugCode { get; init; }
  /// <summary>受试药物名称。必填。</summary>
  public string DrugName { get; init; } = string.Empty;
  /// <summary>药敏结论编码。应填。</summary>
  public string? SusceptibilityCode { get; init; }
  /// <summary>来源结论。必填。</summary>
  public string SourceConclusionText { get; init; } = string.Empty;
  /// <summary>抗药结果编码。应填。</summary>
  public string? ResistanceResultCode { get; init; }
  /// <summary>纸片含药量，按来源文本保存。应填。</summary>
  public string? DiskContent { get; init; }
  /// <summary>最低抑菌浓度，按来源文本整串保存，不解析。应填。</summary>
  public string? MicValue { get; init; }
  /// <summary>抑菌环直径，按来源文本保存。应填。</summary>
  public string? InhibitionZoneDiameter { get; init; }
  /// <summary>参考值。应填。</summary>
  public string? ReferenceValue { get; init; }
  /// <summary>展示序号。必填；同一报告版本、同一细菌鉴定结果下不得重复。</summary>
  public int DisplayOrder { get; init; }
  /// <summary>检测人 ID。应填且与名称成对。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。应填且与标识成对。</summary>
  public string? InspectorName { get; init; }
  /// <summary>检测方法。应填。</summary>
  public string? TestingMethod { get; init; }
  /// <summary>试验板序号。应填。</summary>
  public string? TestPanelOrder { get; init; }
}
