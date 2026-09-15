namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionMatchItemReadModel
{
  /// <summary>
  /// 互认匹配项ID
  /// </summary>
  public Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 标准项目名称
  /// </summary>
  public string StandardProjectName { get; set; }
  /// <summary>
  /// 标本类型名称
  /// </summary>
  public string SpecimenTypeName { get; set; }
  /// <summary>
  /// 主要检验结果
  /// </summary>
  public object LaboratoryResults { get; set; }
  /// <summary>
  /// 检查部位集合
  /// </summary>
  public object ExaminationSites { get; set; }
  /// <summary>
  /// 整体异常标识
  /// </summary>
  public string OverallAbnormalFlag { get; set; }
}
