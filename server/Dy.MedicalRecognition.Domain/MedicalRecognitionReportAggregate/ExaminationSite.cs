using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检查项目的检查部位，记录来源部位编码与部位名称；检查项目没有明确部位时该集合允许为空。
/// </summary>
[IPropertyChangedAware]
public partial class ExaminationSite : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 检查部位主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 检查项目ID；一个检查部位固定归属一条检查项目，只在该项目下有意义。
  /// </summary>
  public partial Guid ExaminationItemId { get; set; }
  /// <summary>
  /// 来源部位编码；按来源原文保存，平台不统一也不推断部位编码。
  /// </summary>
  public partial string? SourceSiteCode { get; set; }
  /// <summary>
  /// 部位名称；按来源报告原文保存，平台不映射到统一解剖部位字典。
  /// </summary>
  public partial string SiteName { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
