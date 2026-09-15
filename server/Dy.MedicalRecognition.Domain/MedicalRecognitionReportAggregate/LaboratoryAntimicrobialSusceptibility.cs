using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 某株细菌对某种受试药物的药敏试验结果；结论针对该菌株与受试药物的组合成立。
/// </summary>
[IPropertyChangedAware]
public partial class LaboratoryAntimicrobialSusceptibility : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 药敏结果主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 细菌鉴定结果ID；一条药敏结果固定归属一株细菌鉴定结果，不以试验板序号代替本关联。
  /// </summary>
  public partial Guid BacteriaResultId { get; set; }
  /// <summary>
  /// 来源明细标识
  /// </summary>
  public partial string? SourceDetailKey { get; set; }
  /// <summary>
  /// 受试药物编码
  /// </summary>
  public partial string? DrugCode { get; set; }
  /// <summary>
  /// 受试药物名称；与纸片含药量、MIC、抑菌环直径、参考值一并按来源原文、单位和比较符号原样保存，不换算也不重新判读。
  /// </summary>
  public partial string DrugName { get; set; }
  /// <summary>
  /// 药敏结论编码
  /// </summary>
  public partial string? SusceptibilityCode { get; set; }
  /// <summary>
  /// 来源结论
  /// </summary>
  public partial string SourceConclusionText { get; set; }
  /// <summary>
  /// 抗药结果编码
  /// </summary>
  public partial string? ResistanceResultCode { get; set; }
  /// <summary>
  /// 纸片含药量
  /// </summary>
  public partial string? DiskContent { get; set; }
  /// <summary>
  /// 最低抑菌浓度（MIC）；按来源原文整串保存，不做数值解析。
  /// </summary>
  public partial string? MicValue { get; set; }
  /// <summary>
  /// 抑菌环直径
  /// </summary>
  public partial string? InhibitionZoneDiameter { get; set; }
  /// <summary>
  /// 参考值
  /// </summary>
  public partial string? ReferenceValue { get; set; }
  /// <summary>
  /// 展示序号
  /// </summary>
  public partial int DisplayOrder { get; set; }
  /// <summary>
  /// 检测人ID
  /// </summary>
  public partial string? InspectorId { get; set; }
  /// <summary>
  /// 检测人名称
  /// </summary>
  public partial string? InspectorName { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public partial string? TestingMethod { get; set; }
  /// <summary>
  /// 试验板序号
  /// </summary>
  public partial string? TestPanelOrder { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
