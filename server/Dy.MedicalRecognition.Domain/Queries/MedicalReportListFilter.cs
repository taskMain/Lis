namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告列表的查询条件，承载已规范化的筛选值。
/// </summary>
/// <remarks>
/// 统一关键字在应用层去首尾空白后传入：为空表示不施加该条件；SQL 侧按报告单号精确、患者证件号码前缀（大小写不敏感）
/// 与患者姓名前缀三列任一命中匹配，通配符按普通字符处理。
/// 时间范围作用于报告主体上的报告时间，采用左闭右开区间。
/// 组织、医院与院区条件由入口决定：平台管理员入口取请求，医院管理员入口的组织与医院取可信上下文。
/// </remarks>
public sealed record MedicalReportListFilter
{
  /// <summary>组织编码；为空表示不按组织过滤。</summary>
  public string? OrganizationCode { get; init; }
  /// <summary>医院编码；为空表示不按医院过滤。</summary>
  public string? HospitalCode { get; init; }
  /// <summary>院区编码；为空表示不按院区过滤。</summary>
  public string? BranchCode { get; init; }
  /// <summary>报告时间下界（含）；为空表示不设下界。</summary>
  public DateTime? ReportTimeFrom { get; init; }
  /// <summary>报告时间上界（不含）；为空表示不设上界。</summary>
  public DateTime? ReportTimeTo { get; init; }
  /// <summary>报告类型；为空表示不按类型过滤。</summary>
  public MedicalReportType? ReportType { get; init; }
  /// <summary>统一关键字；为空表示不施加关键字条件，非空时已去首尾空白。</summary>
  public string? Keyword { get; init; }
  /// <summary>报告生命周期状态；为空表示不按状态过滤。</summary>
  public MedicalReportLifecycleStatus? ReportStatus { get; init; }
}