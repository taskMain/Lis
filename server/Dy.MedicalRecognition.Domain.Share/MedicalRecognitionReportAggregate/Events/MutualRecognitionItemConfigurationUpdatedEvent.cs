using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：可信组织内一条互认项目配置的可互认时间已被修改，其余归属与状态未变。
/// </summary>
public record MutualRecognitionItemConfigurationUpdatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MutualRecognitionItemConfigurationUpdatedEvent);
  /// <summary>
  /// 配置标识
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 组织编码；配置归属组织
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目编码；配置引用的标准项目
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 修改后的可互认时间天数
  /// </summary>
  public int RecognitionDurationDays { get; set; }
}
