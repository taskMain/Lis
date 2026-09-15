namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record MedicalReportVersionContentReadModel
{
  /// <summary>
  /// 报告公共信息
  /// </summary>
  public object Common { get; set; }
  /// <summary>
  /// 检验报告内容
  /// </summary>
  public object LaboratoryContent { get; set; }
  /// <summary>
  /// 检查报告内容
  /// </summary>
  public object ExaminationContent { get; set; }
}
