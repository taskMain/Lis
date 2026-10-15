namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 互认项目页导出生成的文件：文件名、文件格式与文件字节。
/// </summary>
/// <remarks>
/// 本读模型是应用层与导出控制器之间的交付形态；控制器将其组装为文件流响应，
/// 不返回文件键、物理路径或对外下载地址，导出过程不写入数据库、不留存文件副本。
/// 与统计导出读模型分类型声明：两者分属不同页面的交付契约，随各自页面独立演进。
/// </remarks>
public sealed record ConfigurationExportFileReadModel
{
  /// <summary>导出文件名；由页面名称与组装时刻组成。</summary>
  public string FileName { get; init; } = string.Empty;
  /// <summary>导出文件格式；当前统一为 Excel 工作簿格式。</summary>
  public string FileFormat { get; init; } = string.Empty;
  /// <summary>导出文件字节；由应用层在内存中组装。</summary>
  public byte[] FileStream { get; init; } = [];
}
