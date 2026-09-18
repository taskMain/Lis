namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 医院接入与下载的 multipart 表单：两个命名部件。
/// </summary>
/// <remarks>
/// 文本部件 <see cref="ReportJson"/> 承载完整报告 JSON 文档，文件部件 <see cref="PdfFile"/> 承载 PDF 文件流；
/// 本类型只做 multipart 绑定，业务校验与状态判断都在应用层与领域层。
/// </remarks>
public sealed class ReportSubmissionForm
{
  /// <summary>完整报告 JSON 文档的文本部件。</summary>
  public string? ReportJson { get; set; }
  /// <summary>PDF 文件流的文件部件。</summary>
  public IFormFile? PdfFile { get; set; }
}
