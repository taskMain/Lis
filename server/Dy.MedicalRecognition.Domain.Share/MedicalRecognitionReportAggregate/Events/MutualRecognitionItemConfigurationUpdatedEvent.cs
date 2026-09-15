using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

public record MutualRecognitionItemConfigurationUpdatedEvent : DomainEvent
{
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  public override string EventType { get; } = nameof(MutualRecognitionItemConfigurationUpdatedEvent);
  /// <summary>
  /// ID
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public int RecognitionDurationDays { get; set; }
}
