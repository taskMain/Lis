namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionCitationItemReadModel
{
  /// <summary>
  /// 互认匹配项ID
  /// </summary>
  public Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 报告ID
  /// </summary>
  public Guid ReportId { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public Guid ReportVersionId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 标准项目名称
  /// </summary>
  public string StandardProjectName { get; set; }
  /// <summary>
  /// 检验结果
  /// </summary>
  public object LaboratoryResults { get; set; }
  /// <summary>
  /// 检查部位集合
  /// </summary>
  public object ExaminationSites { get; set; }
}
