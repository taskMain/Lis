namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record LaboratoryReportContentReadModel
{
  /// <summary>
  /// 报告类别编码
  /// </summary>
  public string ReportCategoryCode { get; set; }
  /// <summary>
  /// 报告类别名称
  /// </summary>
  public string ReportCategoryName { get; set; }
  /// <summary>
  /// 报告备注
  /// </summary>
  public string ReportRemark { get; set; }
  /// <summary>
  /// 整体异常标识
  /// </summary>
  public string OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 来源医嘱流水号
  /// </summary>
  public string SourceOrderSerialNo { get; set; }
  /// <summary>
  /// 标本采集时间
  /// </summary>
  public DateTime SpecimenCollectedTime { get; set; }
  /// <summary>
  /// 标本送检时间
  /// </summary>
  public DateTime SpecimenSubmittedTime { get; set; }
  /// <summary>
  /// 检验科接收时间
  /// </summary>
  public DateTime LaboratoryReceivedTime { get; set; }
  /// <summary>
  /// 院内标本号
  /// </summary>
  public string SourceSpecimenNo { get; set; }
  /// <summary>
  /// 标本类型编码
  /// </summary>
  public string SpecimenTypeCode { get; set; }
  /// <summary>
  /// 标本类型名称
  /// </summary>
  public string SpecimenTypeName { get; set; }
  /// <summary>
  /// 检测完成时间
  /// </summary>
  public DateTime TestingCompletedTime { get; set; }
  /// <summary>
  /// 检验人ID
  /// </summary>
  public string InspectorId { get; set; }
  /// <summary>
  /// 检验人名称
  /// </summary>
  public string InspectorName { get; set; }
  /// <summary>
  /// 普通检验结果
  /// </summary>
  public object Results { get; set; }
  /// <summary>
  /// 细菌鉴定结果
  /// </summary>
  public object BacteriaResults { get; set; }
  /// <summary>
  /// 药敏结果
  /// </summary>
  public object Susceptibilities { get; set; }
}
