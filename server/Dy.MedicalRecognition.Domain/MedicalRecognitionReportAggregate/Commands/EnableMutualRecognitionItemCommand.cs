using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

[ObjectMap(typeof(MutualRecognitionItemEnabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record EnableMutualRecognitionItemCommand : ICommand
{
  /// <summary>
  /// ID
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 操作人
  /// </summary>
  public Guid OperId { get; set; }
  /// <summary>
  /// 操作时间
  /// </summary>
  public DateTimeOffset OperTime { get; set; }

  public MutualRecognitionItemEnabledEvent CreateMutualRecognitionItemEnabledEvent()
  {
    return this.MapToMutualRecognitionItemEnabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
