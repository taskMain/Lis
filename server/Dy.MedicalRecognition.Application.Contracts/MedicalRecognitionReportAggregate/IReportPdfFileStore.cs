namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告 PDF 的本地文件存储契约：按文件键保存、读取与删除。
/// </summary>
/// <remarks>
/// 只暴露三个操作，不暴露物理路径、不返回文件字节数组；文件字节不进业务 JSON、不进数据库。
/// 文件键由本契约的实现按配置提供的根目录解析，调用方不能提交或推断物理路径。
/// </remarks>
public interface IReportPdfFileStore
{
  /// <summary>
  /// 保存一个文件流并返回新生成的文件键。
  /// </summary>
  /// <remarks>
  /// 创建模式为不覆盖：文件名由平台生成，因此同名冲突属于存储异常；写入失败时删除半成品后再抛出。
  /// </remarks>
  /// <param name="content">待保存的文件流；调用方负责流的生命周期，本方法不关闭传入的流。</param>
  /// <returns>新生成的文件键，格式为日期目录加平台生成的文件名。</returns>
  /// <exception cref="ArgumentNullException"><paramref name="content"/> 为 null 时抛出。</exception>
  /// <exception cref="InvalidOperationException">存储根目录配置缺失、为空、不是绝对路径、目录不存在或不可写时抛出。</exception>
  Task<string> SaveAsync(Stream content);
  /// <summary>
  /// 按键读取文件并返回只读流。
  /// </summary>
  /// <remarks>文件键解析后越出存储根目录即拒绝；文件不存在时抛出明确业务事实。</remarks>
  /// <param name="fileKey">文件键。</param>
  /// <returns>该文件的只读流；调用方负责释放。</returns>
  /// <exception cref="InvalidOperationException">文件键越出根目录、文件不存在或存储根目录配置不可用时抛出。</exception>
  Task<Stream> OpenReadAsync(string fileKey);
  /// <summary>
  /// 按键删除文件。
  /// </summary>
  /// <remarks>删除不存在的键幂等成功，不抛出；写库失败后的文件补偿依赖该语义。</remarks>
  /// <param name="fileKey">文件键。</param>
  /// <returns>删除操作完成的异步任务；文件原本不存在时同样正常完成。</returns>
  /// <exception cref="InvalidOperationException">文件键越出根目录或存储根目录配置不可用时抛出。</exception>
  Task DeleteAsync(string fileKey);
}
