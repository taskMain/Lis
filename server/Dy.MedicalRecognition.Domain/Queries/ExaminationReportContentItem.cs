namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 检查报告专项内容的查询投影行。
/// </summary>
public sealed record ExaminationReportContentItem
{
  /// <summary>检查专项内容标识。</summary>
  public Guid ContentId { get; init; }
  /// <summary>来源检查类型编码。</summary>
  public string? SourceExaminationTypeCode { get; init; }
  /// <summary>来源检查类型名称。</summary>
  public string? SourceExaminationTypeName { get; init; }
  /// <summary>报告备注。</summary>
  public string? ReportRemark { get; init; }
  /// <summary>整体异常标识。</summary>
  public string? OverallAbnormalFlag { get; init; }
  /// <summary>检查所见。</summary>
  public string Findings { get; init; } = string.Empty;
  /// <summary>检查结论。</summary>
  public string Conclusion { get; init; } = string.Empty;
  /// <summary>病情描述。</summary>
  public string? ConditionDescription { get; init; }
  /// <summary>检查目的。</summary>
  public string? ExaminationPurpose { get; init; }
  /// <summary>来源诊断编码。</summary>
  public string? SourceDiagnosisCode { get; init; }
  /// <summary>来源诊断名称。</summary>
  public string SourceDiagnosisName { get; init; } = string.Empty;
  /// <summary>实际检查时间。</summary>
  public DateTime ExaminationTime { get; init; }
  /// <summary>检查医生 ID。</summary>
  public string ExaminerId { get; init; } = string.Empty;
  /// <summary>检查医生名称。</summary>
  public string ExaminerName { get; init; } = string.Empty;
  /// <summary>来源影像状态。</summary>
  public SourceImageStatus SourceImageStatus { get; init; }
  /// <summary>影像调阅地址。</summary>
  public string? ImageAccessUrl { get; init; }
  /// <summary>检查方法。</summary>
  public string? ExaminationMethod { get; init; }
  /// <summary>设备编码。</summary>
  public string? DeviceCode { get; init; }
  /// <summary>设备名称。</summary>
  public string? DeviceName { get; init; }
}
