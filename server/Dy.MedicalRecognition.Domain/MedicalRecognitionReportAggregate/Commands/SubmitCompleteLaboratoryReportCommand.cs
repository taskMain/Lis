using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 提交一份完整检验报告：把报告业务标识、公共版本信息、检验专项内容与全部检验明细一次送进平台。
/// </summary>
/// <remarks>
/// 本命令是本次完整报告提交的唯一提交单元，由应用服务在单一工作单元内交给管理器；
/// 管理器内部按序完成定位或创建报告、解析平台患者、追加版本并更新当前版本指向与报告主体检索列、
/// 写入检验专项内容、普通结果、细菌鉴定结果与药敏结果，最后登记事件；任一步失败时整次提交不保存任何内容。
/// 组织、医院与院区来自可信调用身份，PDF 文件键与下载名由控制器落盘后给出，操作人取自可信身份。
/// </remarks>
public sealed record SubmitCompleteLaboratoryReportCommand : ICommand
{
  /// <summary>来源报告单号；与组织、医院、院区、报告类型共同定位一份报告。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>公共版本信息与业务时间。</summary>
  public MedicalReportVersionRequest Version { get; set; } = new();
  /// <summary>检验专项内容。</summary>
  public LaboratoryReportContentRequest Content { get; set; } = new();
  /// <summary>普通检验结果集合；至少一条。</summary>
  public IReadOnlyList<LaboratoryResultItemRequest> Results { get; set; } = [];
  /// <summary>细菌鉴定结果集合；每条内含药敏结果集合，可空。</summary>
  public IReadOnlyList<LaboratoryBacteriaResultRequest> BacteriaResults { get; set; } = [];
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
