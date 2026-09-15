namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionStatisticsItemReadModel
{
  /// <summary>
  /// 项目类型
  /// </summary>
  public object ItemType { get; set; }
  /// <summary>
  /// 分类名称
  /// </summary>
  public string CategoryName { get; set; }
  /// <summary>
  /// 分组名称
  /// </summary>
  public string GroupName { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 标准项目名称
  /// </summary>
  public string StandardProjectName { get; set; }
}
