using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检验报告的内容层，承载该报告版本共有的标本与流转信息；一个报告版本至多一条。
/// </summary>
[IPropertyChangedAware]
public partial class LaboratoryReportContent : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 检验报告内容主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 报告类别编码
  /// </summary>
  public partial string? ReportCategoryCode { get; set; }
  /// <summary>
  /// 报告类别名称
  /// </summary>
  public partial string? ReportCategoryName { get; set; }
  /// <summary>
  /// 报告备注
  /// </summary>
  public partial string? ReportRemark { get; set; }
  /// <summary>
  /// 整体异常标识
  /// </summary>
  public partial string? OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 来源医嘱流水号
  /// </summary>
  public partial string? SourceOrderSerialNo { get; set; }
  /// <summary>
  /// 标本采集时间；与送检、接收、检测完成时间及检验人姓名均按来源原文保存，平台不做归一化。
  /// </summary>
  public partial DateTime? SpecimenCollectedTime { get; set; }
  /// <summary>
  /// 标本送检时间
  /// </summary>
  public partial DateTime? SpecimenSubmittedTime { get; set; }
  /// <summary>
  /// 检验科接收时间
  /// </summary>
  public partial DateTime? LaboratoryReceivedTime { get; set; }
  /// <summary>
  /// 院内标本号；创建后不再改写，使其下检验项目结果始终可追溯到同一份标本。
  /// </summary>
  public partial string SourceSpecimenNo { get; set; }
  /// <summary>
  /// 标本类型编码；创建后不再改写，按来源原文保存。
  /// </summary>
  public partial string SpecimenTypeCode { get; set; }
  /// <summary>
  /// 标本类型名称
  /// </summary>
  public partial string SpecimenTypeName { get; set; }
  /// <summary>
  /// 检测完成时间
  /// </summary>
  public partial DateTime TestingCompletedTime { get; set; }
  /// <summary>
  /// 检验人ID
  /// </summary>
  public partial string InspectorId { get; set; }
  /// <summary>
  /// 检验人名称
  /// </summary>
  public partial string InspectorName { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
