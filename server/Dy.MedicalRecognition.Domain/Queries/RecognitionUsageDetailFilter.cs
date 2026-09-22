using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 接收侧互认使用明细的查询条件，承载已解析的范围取值、明细级筛选与必填的日期边界。
/// </summary>
/// <remarks>
/// 互认医生与不采纳原因代码只作明细查询条件；不采纳原因代码由调用方按明细类型决定是否传入，
/// 语句把它作为可选谓词消费；日期边界与范围条件的语义同汇总筛选。
/// </remarks>
public sealed record RecognitionUsageDetailFilter
{
  /// <summary>统计开始边界：起始日零点（含）。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束边界：结束日次日零点（不含）。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>接收组织编码；为空表示不按组织过滤。</summary>
  public string? OrganizationCode { get; init; }
  /// <summary>接收医院编码；为空表示不按医院过滤。</summary>
  public string? HospitalCode { get; init; }
  /// <summary>接收院区编码；为空表示不按院区过滤。</summary>
  public string? BranchCode { get; init; }
  /// <summary>来源组织编码；为空表示不按来源组织过滤。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源医院编码；为空表示不按来源医院过滤。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码；为空表示不按来源院区过滤。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>互认科室ID；为空表示不按互认科室过滤。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认医生ID；为空表示不按互认医生过滤。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>不采纳原因代码；为空表示不按原因过滤。</summary>
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  /// <summary>项目类型；为空表示不按项目类型过滤。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称；为空表示不按分类过滤。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；为空表示不按分组过滤。</summary>
  public string? GroupName { get; init; }
  /// <summary>标准项目编码；为空表示不按标准项目过滤。</summary>
  public string? StandardProjectCode { get; init; }
}
