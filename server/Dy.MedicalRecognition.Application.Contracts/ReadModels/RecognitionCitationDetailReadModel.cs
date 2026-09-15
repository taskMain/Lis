namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionCitationDetailReadModel
{
  /// <summary>
  /// 匹配项集合
  /// </summary>
  public object MatchItems { get; set; }
  /// <summary>
  /// 报告公共上下文集合
  /// </summary>
  public object ReportContext { get; set; }
}
