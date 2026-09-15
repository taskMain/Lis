namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record SourceRecognitionSummaryReadModel
{
  /// <summary>
  /// 来源组织编码
  /// </summary>
  public string SourceOrganizationCode { get; set; }
  /// <summary>
  /// 来源组织名称
  /// </summary>
  public string SourceOrganizationName { get; set; }
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
  /// 项目类型
  /// </summary>
  public object ItemType { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 分类名称
  /// </summary>
  public string CategoryName { get; set; }
  /// <summary>
  /// 分组名称
  /// </summary>
  public string GroupName { get; set; }
  /// <summary>
  /// 统计开始时间
  /// </summary>
  public DateTime PeriodStart { get; set; }
  /// <summary>
  /// 统计结束时间
  /// </summary>
  public DateTime PeriodEnd { get; set; }
  /// <summary>
  /// 被互认次数
  /// </summary>
  public int RecognitionCount { get; set; }
  /// <summary>
  /// 汇总维度
  /// </summary>
  public object GroupDimension { get; set; }
}
