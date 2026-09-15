using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

[ObjectMap(typeof(MutualRecognitionItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MutualRecognitionItemCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemCommand : ICommand
{
  /// <summary>
  /// 组织编码
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
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

  public MutualRecognitionItemCreatedEvent CreateMutualRecognitionItemCreatedEvent(Guid id)
  {
    return this.MapToMutualRecognitionItemCreatedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
