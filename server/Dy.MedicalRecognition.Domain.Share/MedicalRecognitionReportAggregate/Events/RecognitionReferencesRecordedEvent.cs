using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：医院明确反馈的互认项目引用事实已整批保存。
/// </summary>
/// <remarks>
/// 一次请求中的全部引用项目整体校验并原子保存，写入成功后只登记一次；任一项失败或冲突都不保存、不登记；
/// 引用只形成引用事实，不增加采纳次数、来源医院被互认次数或预计节省金额；
/// 引用项目标识集合与引用事实集合逐项对应，同一顺序下第 n 个标识即第 n 条事实的匹配项。
/// </remarks>
public record RecognitionReferencesRecordedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(RecognitionReferencesRecordedEvent);
  /// <summary>
  /// 本次形成引用事实的全部互认匹配项标识
  /// </summary>
  public IReadOnlyList<Guid> ReferencedMatchItemIds { get; set; } = [];
  /// <summary>
  /// 本次保存的引用事实；每项携带该匹配项的实际引用时间、引用科室与引用医生
  /// </summary>
  public IReadOnlyList<RecognitionReferenceFact> ReferenceFacts { get; set; } = [];
}
