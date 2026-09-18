using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告列表项；报告管理与历史版本的两个入口共用同一返回契约。
/// </summary>
/// <remarks>
/// 来源组织、医院、院区名称由服务端按名称回填口径对当页整体赋值，页面不按编码拼接名称；
/// 报告时间取报告主体上的检索列，恒有值且等于当前版本的报告时间；当前版本序号按报告的当前版本指向关联版本取得；
/// 报告类型与生命周期状态的中文由服务端按枚举声明派生后随契约返回，前端不重写一份文案。
/// </remarks>
public sealed record MedicalReportListReadModel
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>来源组织编码。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>来源医院编码。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>来源院区编码。</summary>
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>来源组织名称；由服务端回填，不按编码拼接。</summary>
  public string OrganizationName { get; init; } = string.Empty;
  /// <summary>来源医院名称；由服务端回填。</summary>
  public string HospitalName { get; init; } = string.Empty;
  /// <summary>来源院区名称；由服务端回填。</summary>
  public string BranchName { get; init; } = string.Empty;
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>
  /// 报告类型中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ReportTypeText => EnumDescriptorText.Get(ReportType, MedicalReportTypeDescriptorList.List);
  /// <summary>报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>报告时间；取报告主体上的检索列，恒有值。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>当前版本序号；按报告的当前版本指向关联版本取得。</summary>
  public int CurrentVersionSequence { get; init; }
  /// <summary>患者姓名；完整返回，仅作为明确输入的查询条件展示。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>证件号码；完整返回，仅作为明确输入的查询条件展示。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>生命周期状态。</summary>
  public MedicalReportLifecycleStatus Status { get; init; }
  /// <summary>
  /// 生命周期状态中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string StatusText => EnumDescriptorText.Get(Status, MedicalReportLifecycleStatusDescriptorList.List);
}
