using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：一份检查报告已作废。
/// </summary>
public record ExaminationReportVoidedEvent : DomainEvent
{
  /// <summary>本事件所属聚合的标识。</summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>事件类型标识，取本类型名称。</summary>
  public override string EventType { get; } = nameof(ExaminationReportVoidedEvent);
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; set; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>作废时间。</summary>
  public DateTime VoidedTime { get; set; }
  /// <summary>作废原因。</summary>
  public string VoidReason { get; set; } = string.Empty;
}
