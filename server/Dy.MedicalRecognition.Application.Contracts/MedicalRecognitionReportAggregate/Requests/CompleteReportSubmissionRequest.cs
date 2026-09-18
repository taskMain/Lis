using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 完整报告提交入口的请求：承载已校验并已落盘的 PDF 文件键与下载名，以及完整报告 JSON 文档原文。
/// </summary>
/// <remarks>
/// 医院接入契约是宿主控制器的 multipart 动作：控制器完成 PDF 校验与落盘后才调用本入口；
/// 本入口不接收文件字节，只接收文件键与下载名。因此直接调用本入口时必须提供存储中可读的文件键，
/// 文件键指向不存在或不可读的文件时拒绝，且不产生版本与业务数据。
/// </remarks>
public sealed record CompleteReportSubmissionRequest
{
  /// <summary>完整报告 JSON 文档原文；由调用方按报告类型反序列化为检验或检查文档。</summary>
  [Required(ErrorMessage = "参数校验失败：报告文档不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：报告文档不能是空白。")]
  public string ReportJson { get; init; } = string.Empty;
  /// <summary>PDF 文件键；由控制器落盘后产生。</summary>
  [Required(ErrorMessage = "参数校验失败：PDF 文件键不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：PDF 文件键不能是空白。")]
  public string PdfFileId { get; init; } = string.Empty;
  /// <summary>PDF 下载名；文件名缺失或不安全时为「报告单号加扩展名」的回退名。</summary>
  [Required(ErrorMessage = "参数校验失败：PDF 下载名不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：PDF 下载名不能是空白。")]
  public string PdfFileName { get; init; } = string.Empty;
}
