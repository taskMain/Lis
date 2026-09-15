using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 检查报告下的单个检查项目，参与互认匹配的最小单位；一个报告版本包含多条，每条可挂接多条检查部位。
/// </summary>
[IPropertyChangedAware]
public partial class ExaminationItem : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 检查项目主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告版本ID；一条检查项目固定归属一个报告版本，明细按版本保存、不在版本之间复用。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源项目名称；保存来源原文，匹配时只按来源项目编码比对，不改写本名称。
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
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
