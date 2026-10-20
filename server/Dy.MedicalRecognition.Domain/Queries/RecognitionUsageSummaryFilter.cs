using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 使用方互认使用汇总的查询条件，承载已解析的范围取值与必填的日期边界、汇总维度。
/// </summary>
/// <remarks>
/// 范围条件由应用层完成组织路径解析与可信注入后传入，为空表示不施加该层过滤；
/// 日期边界按本地日闭区间换算为「起始日零点（含）至结束日次日零点（不含）」，恒有值；
/// 汇总维度决定语句内分组键的 CASE 分支，取值已在应用层按枚举与提供方子集校验。
/// </remarks>
public sealed record RecognitionUsageSummaryFilter
{
  /// <summary>统计开始边界：起始日零点（含）。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束边界：结束日次日零点（不含），与开始边界共同表达本地日闭区间。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>汇总维度：医院、院区、互认科室或标准项目。</summary>
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
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
  /// 语句侧按互认科室ID/名称、标准项目编码/名称逐列前缀匹配（大小写不敏感）后 OR 组合。
  /// </summary>
  public string? Keyword { get; init; }
  /// <summary>项目类型；为空表示不按项目类型过滤。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称；为空表示不按分类过滤。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；为空表示不按分组过滤。</summary>
  public string? GroupName { get; init; }
}
