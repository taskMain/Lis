using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检查报告的内容层，承载该报告版本共有的描述信息与影像状态；一个报告版本至多一条。
/// </summary>
[IPropertyChangedAware]
public partial class ExaminationReportContent : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 检查报告内容主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源检查类型编码
  /// </summary>
  public partial string? SourceExaminationTypeCode { get; set; }
  /// <summary>
  /// 来源检查类型名称
  /// </summary>
  public partial string? SourceExaminationTypeName { get; set; }
  /// <summary>
  /// 报告备注
  /// </summary>
  public partial string? ReportRemark { get; set; }
  /// <summary>
  /// 整体异常标识
  /// </summary>
  public partial string? OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 检查所见；必填，与检查结论、诊断文本等一并按来源原文保存，平台不重新推断或改写。
  /// </summary>
  public partial string Findings { get; set; }
  /// <summary>
  /// 检查结论；必填，保存检查医生的原文表述，平台不生成也不改写。
  /// </summary>
  public partial string Conclusion { get; set; }
  /// <summary>
  /// 病情描述
  /// </summary>
  public partial string? ConditionDescription { get; set; }
  /// <summary>
  /// 检查目的
  /// </summary>
  public partial string? ExaminationPurpose { get; set; }
  /// <summary>
  /// 来源诊断编码
  /// </summary>
  public partial string? SourceDiagnosisCode { get; set; }
  /// <summary>
  /// 来源诊断名称
  /// </summary>
  public partial string SourceDiagnosisName { get; set; }
  /// <summary>
  /// 实际检查时间
  /// </summary>
  public partial DateTime ExaminationTime { get; set; }
  /// <summary>
  /// 检查医生ID
  /// </summary>
  public partial string ExaminerId { get; set; }
  /// <summary>
  /// 检查医生名称
  /// </summary>
  public partial string ExaminerName { get; set; }
  /// <summary>
  /// 来源影像状态
  /// </summary>
  public partial SourceImageStatus SourceImageStatus { get; set; }
  /// <summary>
  /// 影像调阅地址；仅在该报告确实产生影像时才有值。
  /// </summary>
  public partial string? ImageAccessUrl { get; set; }
  /// <summary>
  /// 检查方法
  /// </summary>
  public partial string? ExaminationMethod { get; set; }
  /// <summary>
  /// 设备编码
  /// </summary>
  public partial string? DeviceCode { get; set; }
  /// <summary>
  /// 设备名称
  /// </summary>
  public partial string? DeviceName { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
