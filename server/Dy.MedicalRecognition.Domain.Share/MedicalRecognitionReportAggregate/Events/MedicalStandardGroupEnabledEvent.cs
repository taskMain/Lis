using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分组已启用。
/// </summary>
/// <remarks>启用不级联：父分类与下级标准项目的启用状态未变化。聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardGroupEnabledEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardGroupEnabledEvent);
  /// <summary>
  /// 被启用的分组标识。
  /// </summary>
  /// <remarks>该分组在本次事件之前处于停用状态。</remarks>
  public Guid Id { get; set; }
}
