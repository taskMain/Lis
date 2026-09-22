namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 统计导出生成的文件：文件名、文件格式与文件字节。
/// </summary>
/// <remarks>
/// 本读模型保留为应用层与导出控制器之间的内部交付形态；控制器将其组装为文件流响应，
/// 不返回文件键、物理路径或对外下载地址，导出过程不写入数据库、不留存文件副本。
/// </remarks>
public sealed record StatisticsExportFileReadModel
{
  /// <summary>导出文件名；由导出类型名称与起止日期组成。</summary>
  public string FileName { get; init; } = string.Empty;
  /// <summary>导出文件格式；当前统一为 Excel 工作簿格式。</summary>
  public string FileFormat { get; init; } = string.Empty;
  /// <summary>导出文件字节；由应用层在内存中组装。</summary>
  public byte[] FileStream { get; init; } = [];
}
