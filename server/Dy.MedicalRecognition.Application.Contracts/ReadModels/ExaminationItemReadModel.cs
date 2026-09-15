namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record ExaminationItemReadModel
{
  /// <summary>
  /// 来源项目名称
  /// </summary>
  public string SourceProjectName { get; set; }
  /// <summary>
  /// 来源项目编码
  /// </summary>
  public string SourceProjectCode { get; set; }
  /// <summary>
  /// 互认项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 检查部位
  /// </summary>
  public object Sites { get; set; }
}
