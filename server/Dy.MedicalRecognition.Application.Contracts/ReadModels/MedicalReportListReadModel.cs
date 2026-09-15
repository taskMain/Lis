namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record MedicalReportListReadModel
{
  /// <summary>
  /// 报告ID
  /// </summary>
  public Guid ReportId { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public string BranchCode { get; set; }
  /// <summary>
  /// 报告类型
  /// </summary>
  public object ReportType { get; set; }
  /// <summary>
  /// 报告单号
  /// </summary>
  public string ReportNo { get; set; }
  /// <summary>
  /// 报告时间
  /// </summary>
  public DateTime ReportTime { get; set; }
  /// <summary>
  /// 生命周期状态
  /// </summary>
  public object Status { get; set; }
}
