using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 标准项目备注已修改。
/// </summary>
/// <remarks>聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardItemRemarkChangedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardItemRemarkChangedEvent);
  /// <summary>
  /// 被修改备注的标准项目标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 覆盖后落库的备注文本。
  /// </summary>
  /// <remarks>为空表示本次改为不填写备注。</remarks>
  public string? Remark { get; set; }
}
