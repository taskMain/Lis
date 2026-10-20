using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 提供方医院被互认汇总的查询条件，承载已解析的范围取值与必填的日期边界、汇总维度。
/// </summary>
/// <remarks>
/// 来源组与接收组的范围条件语义与使用方汇总互换：来源三值取自报告主体、接收三值取自匹配记录；
/// 汇总维度只接受提供方医院、提供方院区与标准项目，取值已在应用层校验。
/// </remarks>
public sealed record SourceRecognitionSummaryFilter
{
  /// <summary>统计开始边界：起始日零点（含）。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束边界：结束日次日零点（不含）。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>汇总维度：提供方医院、提供方院区或标准项目。</summary>
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
  /// <summary>提供方组织编码；为空表示不按提供方组织过滤。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>提供方医院编码；为空表示不按提供方医院过滤。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>提供方院区编码；为空表示不按提供方院区过滤。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>使用方组织编码；为空表示不按使用方组织过滤。</summary>
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>使用方医院编码；为空表示不按使用方医院过滤。</summary>
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>使用方院区编码；为空表示不按使用方院区过滤。</summary>
  public string? ReceiverBranchCode { get; init; }
  /// <summary>项目类型；为空表示不按项目类型过滤。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称；为空表示不按分类过滤。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；为空表示不按分组过滤。</summary>
  public string? GroupName { get; init; }
  /// <summary>
  /// 统一关键字；由应用层经空白归一传入（空白视为未提供、非空白去首尾空白），
  /// 语句侧按互认科室ID/名称、标准项目编码/名称逐列前缀匹配（大小写不敏感）后 OR 组合；提供方语句不承载互认医生列。
  /// </summary>
  public string? Keyword { get; init; }
}
