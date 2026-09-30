using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：可信组织下已一次性建立同一可互认时间的多条互认项目配置，全部以启用状态创建。
/// </summary>
/// <remarks>
/// 一次请求的多条配置整体校验并原子写入，写入成功后只登记一次；
/// 任一编码不合规、目录三层不满足或组织内已存在配置都不写入、不登记；
/// 快照按请求提交的编码顺序逐项对应，同一序号下的快照即该编码建立后的配置事实。
/// </remarks>
public record MutualRecognitionItemsCreatedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(MutualRecognitionItemsCreatedEvent);
  /// <summary>
  /// 本次建立的全部配置所属的组织编码
  /// </summary>
  public string OrganizationCode { get; set; } = string.Empty;
  /// <summary>
  /// 本次建立的配置快照；按请求提交的标准项目编码顺序排列，每项携带该配置的标识、编码、可互认时间与启用状态
  /// </summary>
  public IReadOnlyList<MutualRecognitionItemCreatedSnapshot> Items { get; set; } = [];
}
