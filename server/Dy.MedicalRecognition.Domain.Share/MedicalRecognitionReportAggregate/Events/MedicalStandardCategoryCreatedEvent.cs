using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 分类已创建。
/// </summary>
/// <remarks>聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardCategoryCreatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardCategoryCreatedEvent);
  /// <summary>
  /// 新建分类标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 分类的项目类型；决定归入检验目录还是检查目录。
  /// </summary>
  public MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 分类名称。
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// 分类备注。
  /// </summary>
  public string? Remark { get; set; }
}
