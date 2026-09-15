using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分类已更新。
/// </summary>
/// <remarks>更新范围：项目类型、名称与备注。聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardCategoryUpdatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardCategoryUpdatedEvent);
  /// <summary>
  /// 被修改的分类标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 修改后落库的项目类型。
  /// </summary>
  /// <remarks>该分类已有下级分组时，与修改前一致。</remarks>
  public MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 修改后落库的分类名称。
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// 修改后落库的分类备注。
  /// </summary>
  /// <remarks>为空表示本次改为不填写备注。</remarks>
  public string? Remark { get; set; }
}
