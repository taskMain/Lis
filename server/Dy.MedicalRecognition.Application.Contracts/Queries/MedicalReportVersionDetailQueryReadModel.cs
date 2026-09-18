using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告版本详情；按报告类型返回检验或检查内容读模型之一。
/// </summary>
/// <remarks>
/// 责任人员字段口径与版本列表项一致；患者姓名与证件号码完整返回；患者联系电话按服务端脱敏后的值返回。
/// </remarks>
public sealed record MedicalReportVersionDetailQueryReadModel
{
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>
  /// 报告类型中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ReportTypeText => EnumDescriptorText.Get(ReportType, MedicalReportTypeDescriptorList.List);
  /// <summary>报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
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
  /// <summary>明细检测人；检验版本才有值。</summary>
  public string? DetailInspectors { get; init; }
  /// <summary>检查医生名称；检查版本才有值。</summary>
  public string? ExaminerName { get; init; }
  /// <summary>来源报告备注；来源未提供时无业务值。</summary>
  public string? SourceReportRemark { get; init; }
  /// <summary>该版本的完整结构化内容；公共信息必填，检验与检查两段按报告类型二选一。</summary>
  public MedicalReportVersionContentReadModel Content { get; init; } = new();
  /// <summary>该版本的文件信息。</summary>
  public MedicalReportVersionFileReadModel File { get; init; } = new();
}
