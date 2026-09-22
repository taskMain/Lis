using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 来源医院被互认汇总行：每个来源侧汇总维度的一个分组值与被互认次数。
/// </summary>
/// <remarks>
/// 被互认次数只计采纳事实、不等待引用、不含金额；来源侧仅接受来源医院、来源院区与标准项目三个维度，
/// 汇总维度枚举的互认科室值在来源侧按业务拒绝。
/// </remarks>
public sealed record SourceRecognitionSummaryReadModel
{
  /// <summary>来源组织编码；仅来源医院与来源院区维度行携带。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源组织名称；按当页涉及编码批量回填。</summary>
  public string? SourceOrganizationName { get; init; }
  /// <summary>来源医院编码；仅来源医院维度行携带。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源医院名称；按当页涉及编码批量回填。</summary>
  public string? SourceHospitalName { get; init; }
  /// <summary>来源院区编码；仅来源院区维度行携带。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>来源院区名称；按当页涉及编码批量回填。</summary>
  public string? SourceBranchName { get; init; }
  /// <summary>项目类型；仅标准项目维度行携带。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按「未知类型」安全展示。</remarks>
  public string? ItemTypeText => ItemType is null ? null : EnumDescriptorText.GetOrNull(ItemType.Value, MedicalItemTypeDescriptorList.List);
  /// <summary>标准项目编码；仅标准项目维度行携带。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>标准项目名称；取自标准目录当前版本。</summary>
  public string? StandardProjectName { get; init; }
  /// <summary>标准目录分类名称；仅标准项目维度行携带。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；仅标准项目维度行携带。</summary>
  public string? GroupName { get; init; }
  /// <summary>统计开始日期；按本地日闭区间解释。</summary>
  public DateTime PeriodStart { get; init; }
  /// <summary>统计结束日期；按本地日闭区间解释。</summary>
  public DateTime PeriodEnd { get; init; }
  /// <summary>被互认次数；只计采纳事实。</summary>
  public int RecognitionCount { get; init; }
  /// <summary>本行使用的汇总维度；调用方按其区分其余维度字段的业务含义。</summary>
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
}
