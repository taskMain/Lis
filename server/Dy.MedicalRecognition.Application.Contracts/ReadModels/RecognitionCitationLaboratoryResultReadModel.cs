namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionCitationLaboratoryResultReadModel
{
  /// <summary>
  /// 项目名称
  /// </summary>
  public string ResultItemName { get; set; }
  /// <summary>
  /// 来源结果原文
  /// </summary>
  public string SourceResultContent { get; set; }
  /// <summary>
  /// 单位
  /// </summary>
  public string Unit { get; set; }
  /// <summary>
  /// 来源参考范围
  /// </summary>
  public string SourceReferenceRange { get; set; }
  /// <summary>
  /// 来源异常标志
  /// </summary>
  public string SourceAbnormalFlag { get; set; }
  /// <summary>
  /// 来源危急值标志
  /// </summary>
  public string SourceCriticalValueFlag { get; set; }
}
