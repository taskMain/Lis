using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

[IPropertyChangedAware]
public partial record UpdateMutualRecognitionItemConfigurationRequest : Dto
{
  /// <summary>
  /// ID
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public partial int RecognitionDurationDays { get; set; }
}
