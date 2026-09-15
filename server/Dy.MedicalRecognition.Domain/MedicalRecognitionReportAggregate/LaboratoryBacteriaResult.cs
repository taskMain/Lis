using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检验报告下的细菌培养鉴定结果，其下的药敏结果通过本记录标识归属；一个报告版本可包含多条。
/// </summary>
[IPropertyChangedAware]
public partial class LaboratoryBacteriaResult : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 细菌鉴定结果主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源明细标识
  /// </summary>
  public partial string? SourceDetailKey { get; set; }
  /// <summary>
  /// 来源菌种编码
  /// </summary>
  public partial string? SourceOrganismCode { get; set; }
  /// <summary>
  /// 来源菌种名称
  /// </summary>
  public partial string? SourceOrganismName { get; set; }
  /// <summary>
  /// 来源结果原文
  /// </summary>
  public partial string SourceResultText { get; set; }
  /// <summary>
  /// 检测结论；保存来源系统的原文表述，平台不重写、不归一化菌种名称。
  /// </summary>
  public partial string DetectionConclusion { get; set; }
  /// <summary>
  /// 菌落计数
  /// </summary>
  public partial string? ColonyCount { get; set; }
  /// <summary>
  /// 培养基；培养、菌落与检测描述按来源实际内容保存，平台不标准化也不重建培养过程。
  /// </summary>
  public partial string? CultureMedium { get; set; }
  /// <summary>
  /// 培养时间
  /// </summary>
  public partial string? CultureTime { get; set; }
  /// <summary>
  /// 培养条件
  /// </summary>
  public partial string? CultureCondition { get; set; }
  /// <summary>
  /// 发现方式
  /// </summary>
  public partial string? DiscoveryMethod { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public partial string? DetectionMethod { get; set; }
  /// <summary>
  /// 详细描述
  /// </summary>
  public partial string? Description { get; set; }
  /// <summary>
  /// 检测仪器编码
  /// </summary>
  public partial string? InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称
  /// </summary>
  public partial string? InstrumentName { get; set; }
  /// <summary>
  /// 试验板编码
  /// </summary>
  public partial string? TestPanelCode { get; set; }
  /// <summary>
  /// 试验板名称
  /// </summary>
  public partial string? TestPanelName { get; set; }
  /// <summary>
  /// 检测人ID；明细检测人缺失不阻止报告接收。
  /// </summary>
  public partial string? InspectorId { get; set; }
  /// <summary>
  /// 检测人名称
  /// </summary>
  public partial string? InspectorName { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
