namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record CitationDetailValidityParameterReadModel
{
  /// <summary>
  /// 有效时长分钟
  /// </summary>
  public int ValidityDurationMinutes { get; set; }
}
