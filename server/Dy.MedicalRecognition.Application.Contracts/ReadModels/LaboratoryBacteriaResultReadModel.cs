namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record LaboratoryBacteriaResultReadModel
{
  /// <summary>
  /// 来源明细标识
  /// </summary>
  public string SourceDetailKey { get; set; }
  /// <summary>
  /// 来源菌种编码
  /// </summary>
  public string SourceOrganismCode { get; set; }
  /// <summary>
  /// 来源菌种名称
  /// </summary>
  public string SourceOrganismName { get; set; }
  /// <summary>
  /// 来源结果原文
  /// </summary>
  public string SourceResultText { get; set; }
  /// <summary>
  /// 检测结论
  /// </summary>
  public string DetectionConclusion { get; set; }
  /// <summary>
  /// 菌落计数
  /// </summary>
  public string ColonyCount { get; set; }
  /// <summary>
  /// 培养基
  /// </summary>
  public string CultureMedium { get; set; }
  /// <summary>
  /// 培养时间
  /// </summary>
  public string CultureTime { get; set; }
  /// <summary>
  /// 培养条件
  /// </summary>
  public string CultureCondition { get; set; }
  /// <summary>
  /// 发现方式
  /// </summary>
  public string DiscoveryMethod { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public string DetectionMethod { get; set; }
  /// <summary>
  /// 详细描述
  /// </summary>
  public string Description { get; set; }
  /// <summary>
  /// 检测仪器编码
  /// </summary>
  public string InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称
  /// </summary>
  public string InstrumentName { get; set; }
  /// <summary>
  /// 试验板编码
  /// </summary>
  public string TestPanelCode { get; set; }
  /// <summary>
  /// 试验板名称
  /// </summary>
  public string TestPanelName { get; set; }
  /// <summary>
  /// 检测人ID
  /// </summary>
  public string InspectorId { get; set; }
  /// <summary>
  /// 检测人名称
  /// </summary>
  public string InspectorName { get; set; }
}
