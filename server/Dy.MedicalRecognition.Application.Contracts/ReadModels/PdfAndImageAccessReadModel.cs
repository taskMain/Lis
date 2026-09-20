using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// PDF 与影像入口：平台保存的 PDF 文件标识、下载入口与原始文件名，以及来源影像状态与调阅地址。
/// </summary>
/// <remarks>
/// 匹配响应与引用详情共用同一入口形状；下载入口不设置独立有效期、一次性读取或消费状态，
/// 每次下载按调用医院与院区、匹配归属及绑定报告版本是否仍为当前有效版本判定；
/// 影像调阅地址是来源医院提供的外部信息，平台不代理、不缓存也不保证其可用性。
/// </remarks>
public sealed record PdfAndImageAccessReadModel
{
  /// <summary>PDF 文件标识；平台保存的文件键。</summary>
  public string? PdfFileId { get; init; }
  /// <summary>PDF 下载入口。</summary>
  public string? PdfDownloadUrl { get; init; }
  /// <summary>PDF 原始文件名；报告版本保存的下载名。</summary>
  public string? PdfOriginalFileName { get; init; }
  /// <summary>来源影像状态；检验报告或来源未提供时为空。</summary>
  public SourceImageStatus? SourceImageStatus { get; init; }
  /// <summary>
  /// 来源影像状态中文；来源未提供时为空。
  /// </summary>
  public string? SourceImageStatusText => SourceImageStatus is SourceImageStatus imageStatus
    ? EnumDescriptorText.GetOrNull(imageStatus, SourceImageStatusDescriptorList.List)
    : null;
  /// <summary>影像调阅地址；无影像时为空。</summary>
  public string? ImageAccessUrl { get; init; }
}
