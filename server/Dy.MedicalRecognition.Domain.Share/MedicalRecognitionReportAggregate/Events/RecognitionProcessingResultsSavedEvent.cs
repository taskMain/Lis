using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：某个互认匹配记录的全部匹配项处理结果已整批保存。
/// </summary>
/// <remarks>
/// 整批先校验后写入，写入成功后只登记一次；幂等命中与任一批次失败都不登记；
/// 互认时间、互认科室与互认医生是组级事实，各匹配项的决定与不采纳原因按集合下标逐项对应；
/// 预计节省金额只在采纳处理结果首次成功保存时形成，未配置当前金额按零元形成。
/// </remarks>
public record RecognitionProcessingResultsSavedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(RecognitionProcessingResultsSavedEvent);
  /// <summary>
  /// 本次保存处理结果的互认匹配记录标识
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 本次保存的全部互认匹配项标识；与决定集合、不采纳原因集合按同一顺序逐项对应
  /// </summary>
  public IReadOnlyList<Guid> RecognitionMatchItemIds { get; set; } = [];
  /// <summary>
  /// 医生实际完成本组决定的互认时间；适用于本次保存的全部匹配项
  /// </summary>
  public DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 互认科室在来源系统中的标识；使用字符串承载
  /// </summary>
  public string RecognitionDeptId { get; set; } = string.Empty;
  /// <summary>
  /// 互认科室名称；与互认科室标识成对保存
  /// </summary>
  public string RecognitionDeptName { get; set; } = string.Empty;
  /// <summary>
  /// 互认医生在来源系统中的人员标识；使用字符串承载
  /// </summary>
  public string RecognitionDoctorId { get; set; } = string.Empty;
  /// <summary>
  /// 互认医生名称；与互认医生标识成对保存
  /// </summary>
  public string RecognitionDoctorName { get; set; } = string.Empty;
  /// <summary>
  /// 各匹配项的决定；下标与互认匹配项标识集合一致，采纳与不采纳逐项区分
  /// </summary>
  public IReadOnlyList<RecognitionResult> Results { get; set; } = [];
  /// <summary>
  /// 各匹配项的不采纳原因；下标与互认匹配项标识集合一致，采纳项对应位置为 <see langword="null"/>
  /// </summary>
  public IReadOnlyList<RecognitionNonAdoptionReason?> NonAdoptionReasons { get; set; } = [];
  /// <summary>
  /// 本次采纳项目形成的预计节省金额合计，单位元；全部不采纳时为零
  /// </summary>
  public decimal EstimatedSavingAmount { get; set; }
}
