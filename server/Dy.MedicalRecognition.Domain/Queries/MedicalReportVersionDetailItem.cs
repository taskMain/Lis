namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告版本详情的查询投影，展平后的版本行（含报告身份与公共信息）。
/// </summary>
public sealed record MedicalReportVersionDetailItem
{
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>版本序号。</summary>
  public int VersionSequence { get; init; }
  /// <summary>源端报告修改时间。</summary>
  public DateTime SourceModifiedTime { get; init; }
  /// <summary>平台接收时间。</summary>
  public DateTime PlatformReceivedTime { get; init; }
  /// <summary>报告医生名称。</summary>
  public string ReportDoctorName { get; init; } = string.Empty;
  /// <summary>审核医生名称。</summary>
  public string ReviewDoctorName { get; init; } = string.Empty;
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>来源报告备注；检验与检查的专项内容里可空，未提供时无业务值。</summary>
  public string? SourceReportRemark { get; init; }
}
