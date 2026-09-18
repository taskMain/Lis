using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 报告作废请求：只提交来源报告单号、作废时间与原因。
/// </summary>
/// <remarks>
/// 组织、医院、院区取自可信上下文，报告类型由调用的作废接口确定，操作人与操作时间由服务端写入；
/// 作废时间不得早于当前版本平台接收时间、不得晚于本次请求接收时间，两端含边界；
/// 已作废报告在作废时间与原因完全一致时幂等成功，任一不同返回作废信息冲突。
/// </remarks>
public sealed record MedicalReportVoidRequest
{
  /// <summary>来源报告单号。必填。</summary>
  [Required(ErrorMessage = "参数校验失败：报告单号不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：报告单号不能是空白。")]
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>作废时间。必填；零值视为缺失。</summary>
  public DateTime VoidedTime { get; init; }
  /// <summary>作废原因。必填；纯空白由请求校验拒绝。</summary>
  [Required(ErrorMessage = "参数校验失败：作废原因不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：作废原因不能是空白。")]
  public string VoidReason { get; init; } = string.Empty;
}
