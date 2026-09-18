namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告列表的查询投影，展平后的报告行。
/// </summary>
/// <remarks>
/// 只承载报告主体上的检索列与按当前版本指向关联取得的版本序号，不承载专项目明细；
/// 来源组织、医院、院区名称不在此投影内，由应用层按名称回填口径对当页整体赋值。
/// </remarks>
public sealed record MedicalReportListItem
{
  /// <summary>报告标识；分页排序的稳定兜底键。</summary>
  public Guid ReportId { get; init; }
  /// <summary>组织编码；来源归属的一部分。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>医院编码；来源归属的一部分。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>院区编码；来源归属的一部分。</summary>
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>报告时间；取自报告主体上的检索列，恒有值。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>当前版本序号；按报告的当前版本指向关联报告版本取得。</summary>
  public int CurrentVersionSequence { get; init; }
  /// <summary>患者姓名；取自报告主体上的检索列。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>证件号码；取自报告主体上的检索列。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>生命周期状态。</summary>
  public MedicalReportLifecycleStatus Status { get; init; }
}
