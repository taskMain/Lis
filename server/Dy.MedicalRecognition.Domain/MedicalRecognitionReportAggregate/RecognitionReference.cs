using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认项目在诊疗中被引用的记录；引用只形成引用次数，不增加采纳次数与预计节省金额，首次保存后不可更正、撤销、删除或补改。
/// </summary>
[IPropertyChangedAware]
public partial class RecognitionReference : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 引用记录主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 互认匹配项ID
  /// </summary>
  public partial Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 实际引用时间；与引用科室、引用医生均来自医院提交的引用事实，引用记录由已采纳的匹配项登记产生。
  /// </summary>
  public partial DateTime ReferencedTime { get; set; }
  /// <summary>
  /// 引用科室ID
  /// </summary>
  public partial string ReferenceDeptId { get; set; }
  /// <summary>
  /// 引用科室名称
  /// </summary>
  public partial string ReferenceDeptName { get; set; }
  /// <summary>
  /// 引用医生ID
  /// </summary>
  public partial string ReferenceDoctorId { get; set; }
  /// <summary>
  /// 引用医生名称
  /// </summary>
  public partial string ReferenceDoctorName { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
