using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分组已更新。
/// </summary>
/// <remarks>更新范围：名称与备注；并携带所属分类标识。聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardGroupUpdatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardGroupUpdatedEvent);
  /// <summary>
  /// 被修改的分组标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  /// <remarks>由领域层回填原值，本次修改未迁移分组归属。</remarks>
  public Guid CategoryId { get; set; }
  /// <summary>
  /// 修改后落库的分组名称。
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// 修改后落库的分组备注。
  /// </summary>
  /// <remarks>为空表示本次改为不填写备注。</remarks>
  public string? Remark { get; set; }
}
