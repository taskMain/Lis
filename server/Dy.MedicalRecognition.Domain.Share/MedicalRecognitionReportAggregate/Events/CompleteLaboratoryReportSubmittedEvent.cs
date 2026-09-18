using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：一份完整检验报告已提交。
/// </summary>
public record CompleteLaboratoryReportSubmittedEvent : DomainEvent
{
  /// <summary>本事件所属聚合的标识。</summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>事件类型标识，取本类型名称。</summary>
  public override string EventType { get; } = nameof(CompleteLaboratoryReportSubmittedEvent);
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; set; }
  /// <summary>本次提交形成的报告版本标识。</summary>
  public Guid ReportVersionId { get; set; }
  /// <summary>本次提交形成的版本序号。</summary>
  public int VersionNumber { get; set; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; set; }
}
