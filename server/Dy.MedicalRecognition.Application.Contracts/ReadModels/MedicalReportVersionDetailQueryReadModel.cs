namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record MedicalReportVersionDetailQueryReadModel
{
  /// <summary>
  /// 报告类型
  /// </summary>
  public object ReportType { get; set; }
  /// <summary>
  /// 报告单号
  /// </summary>
  public string ReportNo { get; set; }
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
  /// 报告内容
  /// </summary>
  public object Content { get; set; }
  /// <summary>
  /// PDF文件
  /// </summary>
  public object File { get; set; }
}
