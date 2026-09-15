namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionMatchReportReadModel
{
  /// <summary>
  /// 报告ID
  /// </summary>
  public Guid ReportId { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public Guid ReportVersionId { get; set; }
  /// <summary>
  /// 报告类型
  /// </summary>
  public object ReportType { get; set; }
  /// <summary>
  /// 来源报告单号
  /// </summary>
  public string ReportNo { get; set; }
  /// <summary>
  /// 来源报告名称
  /// </summary>
  public string ReportName { get; set; }
  /// <summary>
  /// 来源组织编码
  /// </summary>
  public string SourceOrganizationCode { get; set; }
  /// <summary>
  /// 来源医院编码
  /// </summary>
  public string SourceHospitalCode { get; set; }
  /// <summary>
  /// 来源医院名称
  /// </summary>
  public string SourceHospitalName { get; set; }
  /// <summary>
  /// 来源院区编码
  /// </summary>
  public string SourceBranchCode { get; set; }
  /// <summary>
  /// 来源院区名称
  /// </summary>
  public string SourceBranchName { get; set; }
  /// <summary>
  /// 检查或检验时间
  /// </summary>
  public DateTime ClinicalTime { get; set; }
  /// <summary>
  /// 报告时间
  /// </summary>
  public DateTime ReportTime { get; set; }
  /// <summary>
  /// 文件信息
  /// </summary>
  public object File { get; set; }
  /// <summary>
  /// 匹配项目集合
  /// </summary>
  public object MatchItems { get; set; }
}
