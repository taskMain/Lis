using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分类已停用。
/// </summary>
/// <remarks>停用不级联：其下分组与标准项目未被级联停用。聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardCategoryDisabledEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardCategoryDisabledEvent);
  /// <summary>
  /// 被停用的分类标识。
  /// </summary>
  /// <remarks>该分类在本次事件之前处于启用状态。</remarks>
  public Guid Id { get; set; }
}
