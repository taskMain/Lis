namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 检验报告专项内容的查询投影行。
/// </summary>
public sealed record LaboratoryReportContentItem
{
  /// <summary>检验专项内容标识。</summary>
  public Guid ContentId { get; init; }
  /// <summary>报告类别编码。</summary>
  public string? ReportCategoryCode { get; init; }
  /// <summary>报告类别名称。</summary>
  public string? ReportCategoryName { get; init; }
  /// <summary>报告备注。</summary>
  public string? ReportRemark { get; init; }
  /// <summary>整体异常标识。</summary>
  public string? OverallAbnormalFlag { get; init; }
  /// <summary>来源医嘱流水号。</summary>
  public string? SourceOrderSerialNo { get; init; }
  /// <summary>标本采集时间。</summary>
  public DateTime? SpecimenCollectedTime { get; init; }
  /// <summary>标本送检时间。</summary>
  public DateTime? SpecimenSubmittedTime { get; init; }
  /// <summary>检验科接收时间。</summary>
  public DateTime? LaboratoryReceivedTime { get; init; }
  /// <summary>院内标本号。</summary>
  public string SourceSpecimenNo { get; init; } = string.Empty;
  /// <summary>标本类型编码。</summary>
  public string SpecimenTypeCode { get; init; } = string.Empty;
  /// <summary>标本类型名称。</summary>
  public string SpecimenTypeName { get; init; } = string.Empty;
  /// <summary>检测完成时间。</summary>
  public DateTime TestingCompletedTime { get; init; }
  /// <summary>检验人 ID。</summary>
  public string InspectorId { get; init; } = string.Empty;
  /// <summary>检验人名称。</summary>
  public string InspectorName { get; init; } = string.Empty;
}
