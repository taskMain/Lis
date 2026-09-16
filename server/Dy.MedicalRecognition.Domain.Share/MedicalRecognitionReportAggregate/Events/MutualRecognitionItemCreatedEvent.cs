using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：可信组织下已建立一条互认项目配置，配置以启用状态创建。
/// </summary>
public record MutualRecognitionItemCreatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MutualRecognitionItemCreatedEvent);
  /// <summary>
  /// 配置标识
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public int RecognitionDurationDays { get; set; }
  /// <summary>
  /// 创建后的启用状态；新增配置固定为启用，供订阅者按状态处理
  /// </summary>
  public bool IsValid { get; set; }
}
