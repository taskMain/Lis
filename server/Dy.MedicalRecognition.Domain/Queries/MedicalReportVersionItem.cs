namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告版本列表的查询投影，展平后的版本行。
/// </summary>
/// <remarks>
/// 同时给出「是否当前有效版本」与「是否已被后续版本替代」两个派生标识与报告生命周期状态；
/// 责任人员只取该版本自身提供的事实，不做字符串拼接、不做优先级取值。
/// </remarks>
public sealed record MedicalReportVersionItem
{
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告类型；供版本列表读取该版本对应的专项内容。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>版本序号；按平台成功形成顺序递增。</summary>
  public int VersionSequence { get; init; }
  /// <summary>源端报告修改时间；来源未提供时无业务值。</summary>
  public DateTime SourceModifiedTime { get; init; }
  /// <summary>平台接收时间。</summary>
  public DateTime PlatformReceivedTime { get; init; }
  /// <summary>报告医生名称。</summary>
  public string ReportDoctorName { get; init; } = string.Empty;
  /// <summary>审核医生名称。</summary>
  public string ReviewDoctorName { get; init; } = string.Empty;
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>是否当前有效版本。</summary>
  public bool IsCurrentVersion { get; init; }
  /// <summary>是否已被后续版本替代。</summary>
  public bool IsSuperseded { get; init; }
  /// <summary>报告生命周期状态。</summary>
  public MedicalReportLifecycleStatus ReportStatus { get; init; }
}
