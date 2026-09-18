namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告版本 PDF 的只读返回数据；只承载该版本保存的下载名，不返回文件键、物理路径或对外下载地址。
/// </summary>
public sealed record MedicalReportVersionPdfQueryReadModel
{
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string FileName { get; init; } = string.Empty;
}
