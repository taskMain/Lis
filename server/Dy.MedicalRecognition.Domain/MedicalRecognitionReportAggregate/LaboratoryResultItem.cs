using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检验报告下的单个检验项目结果，是检验报告参与互认匹配的最小单位；一个报告版本包含多条。
/// </summary>
[IPropertyChangedAware]
public partial class LaboratoryResultItem : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 检验项目结果主键。
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
  /// 来源项目名称
  /// </summary>
  public partial string SourceProjectName { get; set; }
  /// <summary>
  /// 来源项目编码
  /// </summary>
  public partial string? SourceProjectCode { get; set; }
  /// <summary>
  /// 互认项目编码；仅在成功映射到标准目录后才有值，为空表示尚未映射。
  /// </summary>
  public partial string? StandardProjectCode { get; set; }
  /// <summary>
  /// 来源结果原文；按来源系统原文保存，平台不做单位换算。
  /// </summary>
  public partial string SourceResultText { get; set; }
  /// <summary>
  /// 结果类型
  /// </summary>
  public partial LaboratoryResultType ResultType { get; set; }
  /// <summary>
  /// LOINC 标准编码；仅在成功映射到标准目录后才有值，为空表示尚未映射。
  /// </summary>
  public partial string? LoincCode { get; set; }
  /// <summary>
  /// 单位；与结果、参考范围、异常与危急值标志均按来源原样保存，平台不解析、不换算、不重新判定。
  /// </summary>
  public partial string? Unit { get; set; }
  /// <summary>
  /// 参考范围
  /// </summary>
  public partial string? ReferenceRange { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public partial string? TestingMethod { get; set; }
  /// <summary>
  /// 检测仪器编码
  /// </summary>
  public partial string? InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称
  /// </summary>
  public partial string? InstrumentName { get; set; }
  /// <summary>
  /// 展示序号；用于还原来源报告中的项目排列顺序。
  /// </summary>
  public partial int DisplayOrder { get; set; }
  /// <summary>
  /// 异常标志
  /// </summary>
  public partial LaboratoryAbnormalFlag? AbnormalFlag { get; set; }
  /// <summary>
  /// 危急值标志
  /// </summary>
  public partial bool? CriticalValueFlag { get; set; }
  /// <summary>
  /// 检验收费项目编码
  /// </summary>
  public partial string? LaboratoryChargeItemCode { get; set; }
  /// <summary>
  /// 医保收费项目编码
  /// </summary>
  public partial string? InsuranceChargeItemCode { get; set; }
  /// <summary>
  /// 检测人ID
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
