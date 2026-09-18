using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 提交一份完整检查报告：把报告业务标识、公共版本信息、检查专项内容与全部检查项目一次送进平台。
/// </summary>
/// <remarks>内部步骤与失败语义与检验报告提交一致，明细替换为检查项目与检查部位。</remarks>
public sealed record SubmitCompleteExaminationReportCommand : ICommand
{
  /// <summary>来源报告单号；与组织、医院、院区、报告类型共同定位一份报告。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>公共版本信息与业务时间。</summary>
  public MedicalReportVersionRequest Version { get; set; } = new();
  /// <summary>检查专项内容。</summary>
  public ExaminationReportContentRequest Content { get; set; } = new();
  /// <summary>检查项目集合；至少一条，每条内含检查部位集合。</summary>
  public IReadOnlyList<ExaminationItemRequest> Items { get; set; } = [];
  /// <summary>组织编码；取自可信上下文。</summary>
  public string OrganizationCode { get; set; } = string.Empty;
  /// <summary>医院编码；取自可信上下文。</summary>
  public string HospitalCode { get; set; } = string.Empty;
  /// <summary>院区编码；取自可信上下文。</summary>
  public string BranchCode { get; set; } = string.Empty;
  /// <summary>PDF 文件键；由控制器落盘后产生，指向存储中的文件。</summary>
  public string PdfFileId { get; set; } = string.Empty;
  /// <summary>PDF 下载名；文件名缺失或不安全时为「报告单号加扩展名」的回退名。</summary>
  public string PdfFileName { get; set; } = string.Empty;
  /// <summary>操作人标识；由应用服务从登录上下文解析。</summary>
  public Guid OperId { get; set; }
  /// <summary>操作时间；命令时间用于领域事件与实体操作字段。</summary>
  public DateTimeOffset OperTime { get; set; }
  /// <summary>本次请求的接收时间；用于校验业务时间不晚于平台接收时点。</summary>
  public DateTime ReceivedTime { get; set; }
}
