using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

[ObjectMap(typeof(MutualRecognitionItemDisabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record DisableMutualRecognitionItemCommand : ICommand
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

  public MutualRecognitionItemDisabledEvent CreateMutualRecognitionItemDisabledEvent()
  {
    return this.MapToMutualRecognitionItemDisabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
