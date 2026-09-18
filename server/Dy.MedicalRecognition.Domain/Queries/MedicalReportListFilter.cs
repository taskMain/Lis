namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告列表的查询条件，承载已规范化的筛选值。
/// </summary>
/// <remarks>
/// 患者证件号码与患者姓名在应用层按规范形式处理好之后传入：证件号码去首尾空白并统一大写、姓名去首尾空白，
/// 处理后为空表示不施加该筛选条件；筛选按前缀匹配，<c>%</c> 与 <c>_</c> 按普通字符处理。
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
  /// <summary>报告单号；为空表示不按单号过滤。</summary>
  public string? ReportNo { get; init; }
  /// <summary>患者证件号码前缀；为空表示不施加该条件，非空时已按规范形式处理。</summary>
  public string? IdentityDocumentNoPrefix { get; init; }
  /// <summary>患者姓名前缀；为空表示不施加该条件，非空时已去除首尾空白。</summary>
  public string? PatientNamePrefix { get; init; }
}
