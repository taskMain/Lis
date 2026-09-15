using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

public record MutualRecognitionItemCreatedEvent : DomainEvent
{
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  public override string EventType { get; } = nameof(MutualRecognitionItemCreatedEvent);
  /// <summary>
  /// ID
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
}
