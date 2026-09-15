using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认匹配记录下的单个匹配项，是医生逐项做出互认处理的单位；未提交引用结果的匹配项视为未被引用，不改写为未引用事实。
/// </summary>
[IPropertyChangedAware]
public partial class RecognitionMatchItem : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 互认匹配项主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 互认匹配记录ID
  /// </summary>
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 项目类型
  /// </summary>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 报告ID
  /// </summary>
  public partial Guid ReportId { get; set; }
  /// <summary>
  /// 报告版本ID；指向同一患者的某个历史报告版本，本标识与报告标识一经写入不再更换，保证处理意见始终指向同一份依据。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
