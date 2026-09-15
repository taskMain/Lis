using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 细菌鉴定结果的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record LaboratoryBacteriaResultDto : Dto
{
  /// <summary>
  /// 细菌鉴定结果标识；其下药敏结果通过该标识确定归属。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属报告版本标识；一个报告版本可包含多条细菌鉴定结果。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源报文内的明细标识，用于来源明细去重与追溯。
  /// </summary>
  public partial string? SourceDetailKey { get; set; }
  /// <summary>
  /// 来源系统的菌种编码，仅用于追溯。
  /// </summary>
  public partial string? SourceOrganismCode { get; set; }
  /// <summary>
  /// 来源系统的菌种名称；未检出具体菌种时为空。
  /// </summary>
  public partial string? SourceOrganismName { get; set; }
  /// <summary>
  /// 来源系统的结果原文，平台不解析、不换算。
  /// </summary>
  public partial string SourceResultText { get; set; }
  /// <summary>
  /// 检测结论；未检出具体菌种时也应给出明确结论。
  /// </summary>
  public partial string DetectionConclusion { get; set; }
  /// <summary>
  /// 菌落计数，按来源实际内容保存。
  /// </summary>
  public partial string? ColonyCount { get; set; }
  /// <summary>
  /// 培养基，按来源原文保存。
  /// </summary>
  public partial string? CultureMedium { get; set; }
  /// <summary>
  /// 培养时间，以来源文本承载。
  /// </summary>
  public partial string? CultureTime { get; set; }
  /// <summary>
  /// 培养条件，按来源实际内容保存。
  /// </summary>
  public partial string? CultureCondition { get; set; }
  /// <summary>
  /// 发现方式，平台不据此重新判定临床结果。
  /// </summary>
  public partial string? DiscoveryMethod { get; set; }
  /// <summary>
  /// 检测方法，按来源原文保存。
  /// </summary>
  public partial string? DetectionMethod { get; set; }
  /// <summary>
  /// 详细描述，仅用于展示与追溯。
  /// </summary>
  public partial string? Description { get; set; }
  /// <summary>
  /// 检测仪器编码，与名称同源。
  /// </summary>
  public partial string? InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称，仅用于展示与追溯。
  /// </summary>
  public partial string? InstrumentName { get; set; }
  /// <summary>
  /// 试验板编码，仅用于来源追溯。
  /// </summary>
  public partial string? TestPanelCode { get; set; }
  /// <summary>
  /// 试验板名称；只在本结果上保存一次。
  /// </summary>
  public partial string? TestPanelName { get; set; }
  /// <summary>
  /// 明细检测人在来源系统中的人员标识，与名称成对提供；缺失不阻止报告接收。
  /// </summary>
  public partial string? InspectorId { get; set; }
  /// <summary>
  /// 明细检测人名称，平台不依据姓名反查人员。
  /// </summary>
  public partial string? InspectorName { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
