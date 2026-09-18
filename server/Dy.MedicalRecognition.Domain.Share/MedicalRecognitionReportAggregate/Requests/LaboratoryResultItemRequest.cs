using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 普通检验结果的提交输入。
/// </summary>
public sealed record LaboratoryResultItemRequest
{
  /// <summary>来源明细标识。应填；提供时同一报告版本内不得重复，全部缺失时仍接收。</summary>
  public string? SourceDetailKey { get; init; }
  /// <summary>来源项目名称。必填。</summary>
  public string SourceProjectName { get; init; } = string.Empty;
  /// <summary>来源项目编码。应填。</summary>
  public string? SourceProjectCode { get; init; }
  /// <summary>互认项目编码。应填；缺失或当前不可用都不影响接收，随明细原样保存。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>来源结果原文。必填。</summary>
  public string SourceResultText { get; init; } = string.Empty;
  /// <summary>结果类型。必填；仅接受数值型、定性型、文本型。</summary>
  public LaboratoryResultType ResultType { get; init; }
  /// <summary>LOINC 编码。应填。</summary>
  public string? LoincCode { get; init; }
  /// <summary>单位。应填。</summary>
  public string? Unit { get; init; }
  /// <summary>参考范围。应填。</summary>
  public string? ReferenceRange { get; init; }
  /// <summary>检测方法。应填。</summary>
  public string? TestingMethod { get; init; }
  /// <summary>检测仪器编码。应填。</summary>
  public string? InstrumentCode { get; init; }
  /// <summary>检测仪器名称。应填。</summary>
  public string? InstrumentName { get; init; }
  /// <summary>展示序号。必填；同一报告版本内不得重复。</summary>
  public int DisplayOrder { get; init; }
  /// <summary>异常标志。应填；提供时仅接受受控四值。</summary>
  public LaboratoryAbnormalFlag? AbnormalFlag { get; init; }
  /// <summary>危急值标志。应填。</summary>
  public bool? CriticalValueFlag { get; init; }
  /// <summary>检验收费项目编码。应填。</summary>
  public string? LaboratoryChargeItemCode { get; init; }
  /// <summary>医保收费项目编码。应填。</summary>
  public string? MedicalInsuranceChargeItemCode { get; init; }
  /// <summary>检测人 ID。应填且与名称成对。</summary>
  public string? InspectorId { get; init; }
  /// <summary>检测人名称。应填且与标识成对。</summary>
  public string? InspectorName { get; init; }
}
