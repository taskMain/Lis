namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告 PDF 本地文件存储的配置：存储根目录与单文件上限。
/// </summary>
/// <remarks>
/// 对应配置节 <c>ReportPdfFiles</c>；根目录必须是完整绝对路径，单文件上限单位为字节。
/// 受版本控制的默认配置只放空根目录与默认上限，开发期绝对路径写在开发环境配置中；
/// 部署环境按部署配置提供实际目录。
/// </remarks>
public sealed class ReportPdfFileOptions
{
  /// <summary>
  /// 单文件上限的默认值：八十兆字节。
  /// </summary>
  /// <remarks>
  /// 该值必须低于宿主请求体上限（由框架程序集设置，实测约 104857600 字节）：
  /// 两者取值相同时，超限文件会使整个请求体超过宿主上限而在读取阶段即被拒绝，控制器的超限拒绝分支不可达。
  /// </remarks>
  public const long DefaultMaxFileBytes = 83886080;

  /// <summary>
  /// 配置节名称；与宿主配置文件中的节名一一对应。
  /// </summary>
  public const string SectionName = "ReportPdfFiles";

  /// <summary>
  /// 存储根目录的完整绝对路径；缺失或为空时视为未配置。
  /// </summary>
  public string RootDirectory { get; set; } = string.Empty;

  /// <summary>
  /// 单文件上限，单位字节；必须大于零，且低于宿主请求体上限。
  /// </summary>
  public long MaxFileBytes { get; set; } = DefaultMaxFileBytes;

  /// <summary>
  /// 校验配置并返回根目录的完整绝对路径。
  /// </summary>
  /// <remarks>
  /// 根目录非空、为绝对路径、目录存在且可写，四项任一不成立即抛出明确业务事实，不静默回落到应用基目录。
  /// 可写性用生成并立即删除一个探测文件判定，避免仅凭目录存在就认定可写。
  /// </remarks>
  /// <returns>已校验的根目录完整绝对路径。</returns>
  /// <exception cref="InvalidOperationException">
  /// 根目录未配置、为空白、不是绝对路径、目录不存在或不可写，或单文件上限不大于零时抛出。
  /// </exception>
  public string ResolveRootDirectoryOrThrow()
  {
    if (string.IsNullOrWhiteSpace(RootDirectory)) throw new InvalidOperationException("业务拒绝：报告 PDF 存储根目录未配置。");
    if (!Path.IsPathFullyQualified(RootDirectory)) throw new InvalidOperationException("业务拒绝：报告 PDF 存储根目录必须是完整绝对路径。");
    if (!Directory.Exists(RootDirectory)) throw new InvalidOperationException("业务拒绝：报告 PDF 存储根目录不存在。");
    if (MaxFileBytes <= 0) throw new InvalidOperationException("业务拒绝：报告 PDF 单文件上限必须大于零。");

    string probePath = Path.Combine(RootDirectory, $".write-probe-{Guid.NewGuid():N}");
    try
    {
      using FileStream probe = new(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      throw new InvalidOperationException("业务拒绝：报告 PDF 存储根目录不可写。", exception);
    }

    return Path.GetFullPath(RootDirectory);
  }
}
