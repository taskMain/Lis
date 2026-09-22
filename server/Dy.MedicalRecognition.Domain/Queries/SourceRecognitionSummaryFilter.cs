using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 来源医院被互认汇总的查询条件，承载已解析的范围取值与必填的日期边界、汇总维度。
/// </summary>
/// <remarks>
/// 来源组与接收组的范围条件语义与接收侧汇总互换：来源三值取自报告主体、接收三值取自匹配记录；
/// 汇总维度只接受来源医院、来源院区与标准项目，取值已在应用层校验。
/// </remarks>
public sealed record SourceRecognitionSummaryFilter
{
  /// <summary>统计开始边界：起始日零点（含）。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束边界：结束日次日零点（不含）。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>汇总维度：来源医院、来源院区或标准项目。</summary>
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
  /// <summary>来源组织编码；为空表示不按来源组织过滤。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源医院编码；为空表示不按来源医院过滤。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码；为空表示不按来源院区过滤。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>接收组织编码；为空表示不按接收组织过滤。</summary>
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>接收医院编码；为空表示不按接收医院过滤。</summary>
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收院区编码；为空表示不按接收院区过滤。</summary>
  public string? ReceiverBranchCode { get; init; }
  /// <summary>项目类型；为空表示不按项目类型过滤。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称；为空表示不按分类过滤。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；为空表示不按分组过滤。</summary>
  public string? GroupName { get; init; }
  /// <summary>标准项目编码；为空表示不按标准项目过滤。</summary>
  public string? StandardProjectCode { get; init; }
}
