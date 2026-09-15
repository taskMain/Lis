namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record MedicalReportVersionPdfQueryReadModel
{
  /// <summary>
  /// 文件标识
  /// </summary>
  public string FileId { get; set; }
  /// <summary>
  /// 文件名
  /// </summary>
  public string FileName { get; set; }
  /// <summary>
  /// PDF下载入口
  /// </summary>
  public string PdfDownloadUrl { get; set; }
}
