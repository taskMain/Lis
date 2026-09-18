namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 版本文件信息；不包含文件标识、物理路径与对外下载地址。
/// </summary>
public sealed record MedicalReportVersionFileReadModel
{
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string FileName { get; init; } = string.Empty;
}
