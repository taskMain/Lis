using Dy.Core.Abstractions.Domain;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 作废一份检验报告：只改变生命周期状态、作废时间与作废原因。
/// </summary>
/// <remarks>
/// 报告由组织、医院、院区、报告类型与报告单号定位；报告不存在时拒绝且不创建占位记录；
/// 已作废且作废时间与原因完全一致时幂等成功且不重复登记事件，任一不同返回作废信息冲突；
/// 作废时间不得早于当前版本平台接收时间、不得晚于本次请求接收时间。
/// </remarks>
public sealed record VoidLaboratoryReportCommand : ICommand
{
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>作废时间；两端边界包含。</summary>
  public DateTime VoidedTime { get; set; }
  /// <summary>作废原因。</summary>
  public string VoidReason { get; set; } = string.Empty;
  /// <summary>组织编码；取自可信上下文。</summary>
  public string OrganizationCode { get; set; } = string.Empty;
  /// <summary>医院编码；取自可信上下文。</summary>
  public string HospitalCode { get; set; } = string.Empty;
  /// <summary>院区编码；取自可信上下文。</summary>
  public string BranchCode { get; set; } = string.Empty;
  /// <summary>操作人标识；由应用服务从登录上下文解析。</summary>
  public Guid OperId { get; set; }
  /// <summary>操作时间；命令时间用于领域事件与实体操作字段。</summary>
  public DateTimeOffset OperTime { get; set; }
  /// <summary>本次请求的接收时间；用于校验作废时间不晚于平台接收时点。</summary>
  public DateTime ReceivedTime { get; set; }
}
