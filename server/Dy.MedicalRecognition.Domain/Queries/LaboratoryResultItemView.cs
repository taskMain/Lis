namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 普通检验结果的查询投影行。
/// </summary>
/// <remarks>投影携带报告版本归属，使按报告版本集合一次读回的结果仍能归到对应的报告下。</remarks>
public sealed record LaboratoryResultItemView
{
  /// <summary>所属报告版本标识，普通结果的固定归属。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>来源明细标识。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>来源项目名称。</summary>
  public string SourceProjectName { get; init; } = string.Empty;
  /// <summary>来源项目编码。</summary>
  public string? SourceProjectCode { get; init; }
  /// <summary>互认项目编码。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>来源结果原文。</summary>
  public string SourceResultText { get; init; } = string.Empty;
  /// <summary>结果类型。</summary>
  public LaboratoryResultType ResultType { get; init; }
  /// <summary>LOINC 编码。</summary>
  public string? LoincCode { get; init; }
  /// <summary>单位。</summary>
  public string? Unit { get; init; }
  /// <summary>参考范围。</summary>
  public string? ReferenceRange { get; init; }
  /// <summary>检测方法。</summary>
  public string? TestingMethod { get; init; }
  /// <summary>检测仪器编码。</summary>
  public string? InstrumentCode { get; init; }
  /// <summary>检测仪器名称。</summary>
  public string? InstrumentName { get; init; }
  /// <summary>展示序号。</summary>
  public int DisplayOrder { get; init; }
  /// <summary>异常标志。</summary>
  public LaboratoryAbnormalFlag? AbnormalFlag { get; init; }
  /// <summary>危急值标志。</summary>
  public bool? CriticalValueFlag { get; init; }
  /// <summary>检验收费项目编码。</summary>
  public string? LaboratoryChargeItemCode { get; init; }
  /// <summary>医保收费项目编码。</summary>
  public string? MedicalInsuranceChargeItemCode { get; init; }
  /// <summary>检测人 ID。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。</summary>
  public string? InspectorName { get; init; }
}
