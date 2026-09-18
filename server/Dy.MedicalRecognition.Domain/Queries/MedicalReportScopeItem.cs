namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告的来源归属查询投影：报告身份与三层业务归属。
/// </summary>
/// <remarks>只承载范围校验所需的列，不承载报告内容。</remarks>
public sealed record MedicalReportScopeItem
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>组织编码。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>医院编码。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>院区编码。</summary>
  public string BranchCode { get; init; } = string.Empty;
}
