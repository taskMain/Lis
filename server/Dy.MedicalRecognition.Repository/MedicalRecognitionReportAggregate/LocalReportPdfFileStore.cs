using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告 PDF 的本地文件存储实现：按配置根目录解析文件键并落盘、读取与删除。
/// </summary>
/// <remarks>
/// 文件键格式为 <c>&lt;yyyyMMdd&gt;/&lt;guid:N&gt;.pdf</c>，日期目录用于分散单目录文件数量；
/// 解析后必须仍位于根目录之内，越出根目录即拒绝，因此调用方不能借助路径分隔符或相对片段越出存储范围。
/// 写入采用不覆盖模式并在写失败时删除半成品；删除不存在的键幂等成功。
/// 配置缺失、为空、目录不存在或不可写时抛出明确业务事实，不静默回落到应用基目录。
/// </remarks>
public sealed class LocalReportPdfFileStore : IReportPdfFileStore
{
  /// <summary>
  /// 报告 PDF 的存储配置，提供根目录与单文件上限。
  /// </summary>
  private readonly ReportPdfFileOptions options;

  /// <summary>
  /// 接收存储配置。
  /// </summary>
  /// <param name="options">按配置节绑定后的报告 PDF 存储配置。</param>
  public LocalReportPdfFileStore(ReportPdfFileOptions options) => this.options = options;

  /// <inheritdoc/>
  public async Task<string> SaveAsync(Stream content)
  {
    ArgumentNullException.ThrowIfNull(content);
    string rootDirectory = options.ResolveRootDirectoryOrThrow();

    string dateDirectory = DateTime.Now.ToString("yyyyMMdd");
    string fileKey = $"{dateDirectory}/{Guid.NewGuid():N}.pdf";
    string filePath = ResolvePathOrThrow(rootDirectory, fileKey);
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

    try
    {
      // 不覆盖模式：文件名由平台生成，同名冲突属于存储异常，不能静默覆盖既有版本的文件。
      await using FileStream target = new(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      await content.CopyToAsync(target);
    }
    catch
    {
      // 写失败时删除半成品，避免在存储目录留下无法追踪的残缺文件；删除失败只吞掉清理异常，原异常继续向外抛出。
      TryDelete(filePath);
      throw;
    }

    return fileKey;
  }

  /// <inheritdoc/>
  public Task<Stream> OpenReadAsync(string fileKey)
  {
    string rootDirectory = options.ResolveRootDirectoryOrThrow();
    string filePath = ResolvePathOrThrow(rootDirectory, fileKey);
    if (!File.Exists(filePath)) throw new InvalidOperationException("业务拒绝：报告 PDF 文件不存在。");

    // 只读流：读取期间不允许写入，避免下载过程中文件被改写。
    return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
  }

  /// <inheritdoc/>
  public Task DeleteAsync(string fileKey)
  {
    string rootDirectory = options.ResolveRootDirectoryOrThrow();
    string filePath = ResolvePathOrThrow(rootDirectory, fileKey);
    if (File.Exists(filePath)) File.Delete(filePath);

    return Task.CompletedTask;
  }

  /// <summary>
  /// 把文件键解析为根目录内的完整路径，解析结果越出根目录即拒绝。
  /// </summary>
  /// <param name="rootDirectory">已校验的存储根目录完整绝对路径。</param>
  /// <param name="fileKey">文件键。</param>
  /// <returns>根目录内的完整文件路径。</returns>
  /// <exception cref="InvalidOperationException">文件键为空白，或解析结果越出存储根目录时抛出。</exception>
  private static string ResolvePathOrThrow(string rootDirectory, string fileKey)
  {
    if (string.IsNullOrWhiteSpace(fileKey)) throw new InvalidOperationException("业务拒绝：报告 PDF 文件键不能为空。");

    string filePath = Path.GetFullPath(Path.Combine(rootDirectory, fileKey));
    string rootWithSeparator = Path.TrimEndingDirectorySeparator(rootDirectory) + Path.DirectorySeparatorChar;
    if (!filePath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
      throw new InvalidOperationException("业务拒绝：报告 PDF 文件键越出存储根目录。");

    return filePath;
  }

  /// <summary>
  /// 尽力删除指定路径的文件，用于写失败后的半成品清理。
  /// </summary>
  /// <param name="filePath">待删除的文件完整路径。</param>
  private static void TryDelete(string filePath)
  {
    try
    {
      if (File.Exists(filePath)) File.Delete(filePath);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // 清理失败不改变对外结果：写入失败的原异常继续向外抛出，残留的半成品由运维按既定风险口径处理。
    }
  }
}
