namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 检验报告专项内容的提交输入。
/// </summary>
public sealed record LaboratoryReportContentRequest
{
  /// <summary>报告类别编码。应填。</summary>
  public string? ReportCategoryCode { get; init; }
  /// <summary>报告类别名称。应填。</summary>
  public string? ReportCategoryName { get; init; }
  /// <summary>报告备注。应填。</summary>
  public string? ReportRemark { get; init; }
  /// <summary>整体异常标识。应填；不参与接收判断。</summary>
  public string? OverallAbnormalFlag { get; init; }
  /// <summary>来源医嘱流水号。应填。</summary>
  public string? SourceOrderSerialNo { get; init; }
  /// <summary>标本采集时间。应填。</summary>
  public DateTime? SpecimenCollectedTime { get; init; }
  /// <summary>标本送检时间。应填。</summary>
  public DateTime? SpecimenSubmittedTime { get; init; }
  /// <summary>检验科接收时间。应填。</summary>
  public DateTime? LaboratoryReceivedTime { get; init; }
  /// <summary>院内标本号。必填。</summary>
  public string SourceSpecimenNo { get; init; } = string.Empty;
  /// <summary>标本类型编码。必填。</summary>
  public string SpecimenTypeCode { get; init; } = string.Empty;
  /// <summary>标本类型名称。必填。</summary>
  public string SpecimenTypeName { get; init; } = string.Empty;
  /// <summary>检测完成时间。必填；零值视为缺失。</summary>
  public DateTime TestingCompletedTime { get; init; }
  /// <summary>检验人 ID。必填且与名称成对。</summary>
  public string InspectorId { get; init; } = string.Empty;
  /// <summary>检验人名称。必填且与标识成对。</summary>
  public string InspectorName { get; init; } = string.Empty;
}
