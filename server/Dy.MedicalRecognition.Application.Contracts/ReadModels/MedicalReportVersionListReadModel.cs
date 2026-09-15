namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record MedicalReportVersionListReadModel
{
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public Guid ReportVersionId { get; set; }
  /// <summary>
  /// 平台内部版本序号
  /// </summary>
  public int VersionSequence { get; set; }
  /// <summary>
  /// 源端报告修改时间
  /// </summary>
  public DateTime SourceModifiedTime { get; set; }
  /// <summary>
  /// 平台接收时间
  /// </summary>
  public DateTime PlatformReceivedTime { get; set; }
  /// <summary>
  /// 报告责任人员
  /// </summary>
  public string ResponsibleStaff { get; set; }
  /// <summary>
  /// 来源报告备注
  /// </summary>
  public string SourceReportRemark { get; set; }
  /// <summary>
  /// PDF文件名
  /// </summary>
  public string PdfFileName { get; set; }
  /// <summary>
  /// 是否当前有效版本
  /// </summary>
  public bool IsCurrentVersion { get; set; }
  /// <summary>
  /// 是否已被后续版本替代
  /// </summary>
  public bool IsSuperseded { get; set; }
  /// <summary>
  /// 报告生命周期状态
  /// </summary>
  public object ReportStatus { get; set; }
}
