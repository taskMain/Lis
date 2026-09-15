using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

public record MutualRecognitionItemDisabledEvent : DomainEvent
{
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  public override string EventType { get; } = nameof(MutualRecognitionItemDisabledEvent);
  /// <summary>
  /// ID
  /// </summary>
  public Guid Id { get; set; }
}
