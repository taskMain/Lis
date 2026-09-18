using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分组已创建。
/// </summary>
/// <remarks>聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardGroupCreatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardGroupCreatedEvent);
  /// <summary>
  /// 新建分组标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 新建分组所属分类标识。
  /// </summary>
  /// <remarks>分组创建后归属不可迁移。</remarks>
  public Guid CategoryId { get; set; }
  /// <summary>
  /// 分组名称。
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// 分组备注。
  /// </summary>
  public string? Remark { get; set; }
}
