namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告版本 PDF 下载的内容：该版本的只读文件流与该版本保存的下载名。
/// </summary>
/// <param name="Content">该版本的只读文件流；调用方负责释放。</param>
/// <param name="FileName">该版本保存的下载名。</param>
public readonly record struct ReportPdfDownload(Stream Content, string FileName);
