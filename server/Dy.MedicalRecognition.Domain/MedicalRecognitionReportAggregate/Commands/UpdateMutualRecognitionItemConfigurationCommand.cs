using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

[ObjectMap(typeof(MutualRecognitionItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MutualRecognitionItemConfigurationUpdatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record UpdateMutualRecognitionItemConfigurationCommand : ICommand
{
  /// <summary>
  /// ID
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public partial int RecognitionDurationDays { get; set; }
  /// <summary>
  /// 操作人
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  public MutualRecognitionItemConfigurationUpdatedEvent CreateMutualRecognitionItemConfigurationUpdatedEvent()
  {
    return this.MapToMutualRecognitionItemConfigurationUpdatedEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
