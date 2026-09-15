using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 医生对某个互认匹配项的最终处理结论。
/// </summary>
[IPropertyChangedAware]
public partial class RecognitionProcessingResult : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 互认处理结果主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 互认匹配记录ID
  /// </summary>
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 互认匹配项ID；每个匹配项承载一项项目级处理结论。
  /// </summary>
  public partial Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 互认时间
  /// </summary>
  public partial DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 互认结果
  /// </summary>
  public partial RecognitionResult RecognitionResult { get; set; }
  /// <summary>
  /// 互认科室ID
  /// </summary>
  public partial string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称
  /// </summary>
  public partial string RecognitionDeptName { get; set; }
  /// <summary>
  /// 互认医生ID
  /// </summary>
  public partial string RecognitionDoctorId { get; set; }
  /// <summary>
  /// 互认医生名称
  /// </summary>
  public partial string RecognitionDoctorName { get; set; }
  /// <summary>
  /// 不采纳原因；采纳时不得带有不采纳原因，不采纳时必须有原因。
  /// </summary>
  public partial RecognitionNonAdoptionReason? NonAdoptionReason { get; set; }
  /// <summary>
  /// 不采纳补充说明；不采纳时可选填写。
  /// </summary>
  public partial string? NonAdoptionDescription { get; set; }
  /// <summary>
  /// 预计节省金额；在采纳成立时产生并参与金额累计。
  /// </summary>
  public partial decimal? EstimatedSavingAmount { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
