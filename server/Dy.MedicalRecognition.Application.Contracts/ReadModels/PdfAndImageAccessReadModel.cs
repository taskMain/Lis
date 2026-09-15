namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record PdfAndImageAccessReadModel
{
  /// <summary>
  /// PDF文件标识
  /// </summary>
  public string PdfFileId { get; set; }
  /// <summary>
  /// PDF下载入口
  /// </summary>
  public string PdfDownloadUrl { get; set; }
  /// <summary>
  /// PDF原始文件名
  /// </summary>
  public string PdfOriginalFileName { get; set; }
  /// <summary>
  /// 来源影像状态
  /// </summary>
  public object SourceImageStatus { get; set; }
  /// <summary>
  /// 影像调阅地址
  /// </summary>
  public string ImageAccessUrl { get; set; }
}
