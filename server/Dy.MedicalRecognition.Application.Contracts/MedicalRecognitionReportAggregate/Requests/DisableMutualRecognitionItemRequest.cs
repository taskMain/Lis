using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

[IPropertyChangedAware]
public partial record DisableMutualRecognitionItemRequest : Dto
{
  /// <summary>
  /// ID
  /// </summary>
  public partial Guid Id { get; set; }
}
