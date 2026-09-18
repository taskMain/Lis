using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告历史版本列表项；按平台成功形成顺序返回某个报告的全部版本。
/// </summary>
/// <remarks>
/// 责任人员以结构化字段返回：报告医生名称与审核医生名称在来源提供时返回，检验版本另带检验人名称与明细检测人，
/// 检查版本另带检查医生名称，另一类的字段无业务值；不做字符串拼接、不做优先级取值，各版本只返回自身提供的人员事实。
/// 是否当前有效版本与是否已被后续版本替代两个标识都由服务端派生。
/// </remarks>
public sealed record MedicalReportVersionListReadModel
{
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>平台内部版本序号。</summary>
  public int VersionSequence { get; init; }
  /// <summary>源端报告修改时间；来源未提供时无业务值。</summary>
  public DateTime? SourceModifiedTime { get; init; }
  /// <summary>平台接收时间。</summary>
  public DateTime PlatformReceivedTime { get; init; }
  /// <summary>报告医生名称；该版本自身提供的人员事实。</summary>
  public string? ReportDoctorName { get; init; }
  /// <summary>审核医生名称；该版本自身提供的人员事实。</summary>
  public string? ReviewDoctorName { get; init; }
  /// <summary>检验人名称；检验版本才有值。</summary>
  public string? InspectorName { get; init; }
  /// <summary>明细检测人；检验版本才有值，按该版本普通检验结果的检测人名称去重后返回。</summary>
  public string? DetailInspectors { get; init; }
  /// <summary>检查医生名称；检查版本才有值。</summary>
  public string? ExaminerName { get; init; }
  /// <summary>来源报告备注；来源未提供时无业务值。</summary>
  public string? SourceReportRemark { get; init; }
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>是否当前有效版本。</summary>
  public bool IsCurrentVersion { get; init; }
  /// <summary>是否已被后续版本替代。</summary>
  public bool IsSuperseded { get; init; }
  /// <summary>报告生命周期状态；供版本列表标识报告已作废。</summary>
  public MedicalReportLifecycleStatus ReportStatus { get; init; }
  /// <summary>
  /// 报告生命周期状态中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ReportStatusText => EnumDescriptorText.Get(ReportStatus, MedicalReportLifecycleStatusDescriptorList.List);
}
