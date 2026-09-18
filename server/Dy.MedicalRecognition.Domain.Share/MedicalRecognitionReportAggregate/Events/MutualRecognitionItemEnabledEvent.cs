using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：可信组织内一条互认项目配置已被启用，该标准项目重新参与互认匹配。
/// </summary>
public record MutualRecognitionItemEnabledEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MutualRecognitionItemEnabledEvent);
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
  /// 启用后的状态；启用动作固定为启用，供订阅者按状态处理
  /// </summary>
  public bool IsValid { get; set; }
}
