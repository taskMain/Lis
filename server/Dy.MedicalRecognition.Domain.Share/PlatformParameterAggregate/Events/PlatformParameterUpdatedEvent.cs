using Dy.Core.Abstractions.Domain;

namespace Dy.MedicalRecognition.Domain.Share.PlatformParameterAggregate.Events;

/// <summary>
/// 已发生的领域事实：一条平台参数的值、说明或启用状态已被修改。
/// </summary>
public record PlatformParameterUpdatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = PlatformParameterConst.AggregateId;

  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(PlatformParameterUpdatedEvent);

  /// <summary>
  /// 参数主键标识。
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// 参数编码。
  /// </summary>
  public string ParameterCode { get; set; }

  /// <summary>
  /// 修改后的参数值。
  /// </summary>
  public string ParameterValue { get; set; }

  /// <summary>
  /// 修改后的启用状态。
  /// </summary>
  public bool IsValid { get; set; }
}
