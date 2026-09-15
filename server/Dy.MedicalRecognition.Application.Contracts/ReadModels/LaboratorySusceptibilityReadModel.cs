namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record LaboratorySusceptibilityReadModel
{
  /// <summary>
  /// 来源明细标识
  /// </summary>
  public string SourceDetailKey { get; set; }
  /// <summary>
  /// 受试药物编码
  /// </summary>
  public string DrugCode { get; set; }
  /// <summary>
  /// 受试药物名称
  /// </summary>
  public string DrugName { get; set; }
  /// <summary>
  /// 药敏结论编码
  /// </summary>
  public string SusceptibilityCode { get; set; }
  /// <summary>
  /// 来源结论
  /// </summary>
  public string SourceConclusionText { get; set; }
  /// <summary>
  /// 抗药结果编码
  /// </summary>
  public string ResistanceResultCode { get; set; }
  /// <summary>
  /// 纸片含药量
  /// </summary>
  public string DiskContent { get; set; }
  /// <summary>
  /// MIC
  /// </summary>
  public string MicValue { get; set; }
  /// <summary>
  /// 抑菌圈直径
  /// </summary>
  public string InhibitionZoneDiameter { get; set; }
  /// <summary>
  /// 参考值
  /// </summary>
  public string ReferenceValue { get; set; }
  /// <summary>
  /// 展示序号
  /// </summary>
  public int DisplayOrder { get; set; }
  /// <summary>
  /// 检测人ID
  /// </summary>
  public string InspectorId { get; set; }
  /// <summary>
  /// 检测人名称
  /// </summary>
  public string InspectorName { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public string TestingMethod { get; set; }
  /// <summary>
  /// 试验板序号
  /// </summary>
  public string TestPanelOrder { get; set; }
}
