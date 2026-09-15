namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record StatisticsExportFileReadModel
{
  /// <summary>
  /// 文件名
  /// </summary>
  public string FileName { get; set; }
  /// <summary>
  /// 文件格式
  /// </summary>
  public string FileFormat { get; set; }
  /// <summary>
  /// 文件流
  /// </summary>
  public byte[] FileStream { get; set; }
}
