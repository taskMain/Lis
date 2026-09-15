using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

public record MutualRecognitionItemEnabledEvent : DomainEvent
{
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  public override string EventType { get; } = nameof(MutualRecognitionItemEnabledEvent);
  /// <summary>
  /// ID
  /// </summary>
  public Guid Id { get; set; }
}
