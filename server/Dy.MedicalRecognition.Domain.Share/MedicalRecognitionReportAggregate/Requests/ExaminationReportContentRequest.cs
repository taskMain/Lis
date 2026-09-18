using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 检查报告专项内容的提交输入。
/// </summary>
public sealed record ExaminationReportContentRequest
{
  /// <summary>来源检查类型编码。应填。</summary>
  public string? SourceExaminationTypeCode { get; init; }
  /// <summary>来源检查类型名称。应填。</summary>
  public string? SourceExaminationTypeName { get; init; }
  /// <summary>报告备注。应填。</summary>
  public string? ReportRemark { get; init; }
  /// <summary>整体异常标识。应填；不参与接收判断。</summary>
  public string? OverallAbnormalFlag { get; init; }
  /// <summary>检查所见，完整原文。必填。</summary>
  public string Findings { get; init; } = string.Empty;
  /// <summary>检查结论，完整原文。必填。</summary>
  public string Conclusion { get; init; } = string.Empty;
  /// <summary>病情描述。应填。</summary>
  public string? ConditionDescription { get; init; }
  /// <summary>检查目的。应填。</summary>
  public string? ExaminationPurpose { get; init; }
  /// <summary>来源诊断编码。应填。</summary>
  public string? SourceDiagnosisCode { get; init; }
  /// <summary>来源诊断名称。必填。</summary>
  public string SourceDiagnosisName { get; init; } = string.Empty;
  /// <summary>实际检查时间。必填；零值视为缺失。</summary>
  public DateTime ExaminationTime { get; init; }
  /// <summary>检查医生 ID。必填且与名称成对。</summary>
  public string ExaminerId { get; init; } = string.Empty;
  /// <summary>检查医生名称。必填且与标识成对。</summary>
  public string ExaminerName { get; init; } = string.Empty;
  /// <summary>来源影像状态。应填；仅接受有影像、无影像、未知三值，字段未提供时按未知处理并保存。</summary>
  public SourceImageStatus? SourceImageStatus { get; init; }
  /// <summary>影像调阅地址。来源影像状态为无影像时必须为空；有影像而未提供地址时仍可接收。</summary>
  public string? ImageAccessUrl { get; init; }
  /// <summary>检查方法。应填。</summary>
  public string? ExaminationMethod { get; init; }
  /// <summary>设备编码。应填。</summary>
  public string? DeviceCode { get; init; }
  /// <summary>设备名称。应填。</summary>
  public string? DeviceName { get; init; }
}
