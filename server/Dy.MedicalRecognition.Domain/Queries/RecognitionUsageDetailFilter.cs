using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 使用方互认使用明细的查询条件，承载已解析的范围取值、明细级筛选与必填的日期边界。
/// </summary>
/// <remarks>
/// 不采纳原因代码只作明细查询条件，由调用方按明细类型决定是否传入，语句把它作为可选谓词消费；
/// 统一关键字在语句侧覆盖互认科室、标准项目与互认医生的命中列；日期边界与范围条件的语义同汇总筛选。
/// </remarks>
public sealed record RecognitionUsageDetailFilter
{
  /// <summary>统计开始边界：起始日零点（含）。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束边界：结束日次日零点（不含）。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>使用方组织编码；为空表示不按组织过滤。</summary>
  public string? OrganizationCode { get; init; }
  /// <summary>使用方医院编码；为空表示不按医院过滤。</summary>
  public string? HospitalCode { get; init; }
  /// <summary>使用方院区编码；为空表示不按院区过滤。</summary>
  public string? BranchCode { get; init; }
  /// <summary>提供方组织编码；为空表示不按提供方组织过滤。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>提供方医院编码；为空表示不按提供方医院过滤。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>提供方院区编码；为空表示不按提供方院区过滤。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>
  /// 统一关键字；由应用层经空白归一传入（空白视为未提供、非空白去首尾空白），
  /// 语句侧按互认科室ID/名称、标准项目编码/名称、互认医生ID/姓名逐列前缀匹配（大小写不敏感）后 OR 组合。
  /// </summary>
  public string? Keyword { get; init; }
  /// <summary>不采纳原因代码；为空表示不按原因过滤。</summary>
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  /// <summary>项目类型；为空表示不按项目类型过滤。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称；为空表示不按分类过滤。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；为空表示不按分组过滤。</summary>
  public string? GroupName { get; init; }
  /// <summary>互认科室 ID 精确条件；为空表示不按科室过滤。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>标准项目编码精确条件；为空表示不按项目过滤。</summary>
  public string? StandardProjectCode { get; init; }
}
