using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 标准项目已创建。
/// </summary>
/// <remarks>聚合标识与事件类型标识由 <see cref="DomainEvent"/> 约定，全部标准目录事件归入同一聚合。</remarks>
public record MedicalStandardItemCreatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MedicalStandardItemCreatedEvent);
  /// <summary>
  /// 新建标准项目标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 新建标准项目所属分类标识。
  /// </summary>
  public Guid CategoryId { get; set; }
  /// <summary>
  /// 新建标准项目所属分组标识。
  /// </summary>
  /// <remarks>创建时已校验其属于上述分类。</remarks>
  public Guid GroupId { get; set; }
  /// <summary>
  /// 标准项目编码。
  /// </summary>
  public string Code { get; set; }
  /// <summary>
  /// 标准项目名称。
  /// </summary>
  public string Name { get; set; }
  /// <summary>
  /// 标准项目备注。
  /// </summary>
  public string? Remark { get; set; }
}
