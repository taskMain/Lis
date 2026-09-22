using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 接收侧互认使用汇总的分组行投影：所选维度的一个分组值，以及行内六项指标与预计节省金额。
/// </summary>
/// <remarks>
/// 未参与分组的维度键为空值，应用层据此组装读模型并把空值保持为空；
/// 同期互认率与不采纳原因占比由应用层按行内次数计算，不在投影内；
/// 次数用长整型承载计数列的数据库返回类型，金额合计对空行集归零。
/// </remarks>
public sealed record RecognitionUsageSummaryGroupItem
{
  /// <summary>接收组织编码；仅医院、院区与互认科室维度行有值。</summary>
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>接收医院编码；仅医院、院区与互认科室维度行有值。</summary>
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收院区编码；仅院区与互认科室维度行有值。</summary>
  public string? ReceiverBranchCode { get; init; }
  /// <summary>互认科室ID；仅互认科室维度行有值。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称：范围内互认时间最晚一条处理结果保存的名称；仅互认科室维度行有值。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>项目类型：实时取自标准目录所属分类；仅标准项目维度行有值。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准项目编码；仅标准项目维度行有值。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>标准项目名称：实时取自标准目录；仅标准项目维度行有值。</summary>
  public string? StandardItemName { get; init; }
  /// <summary>标准目录分类名称；仅标准项目维度行有值。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；仅标准项目维度行有值。</summary>
  public string? GroupName { get; init; }
  /// <summary>提醒次数：按匹配生成时间统计的匹配项数。</summary>
  public long ReminderCount { get; init; }
  /// <summary>采纳次数：按处理结果互认时间统计。</summary>
  public long AdoptionCount { get; init; }
  /// <summary>不采纳次数：按处理结果互认时间统计。</summary>
  public long NonAdoptionCount { get; init; }
  /// <summary>引用次数：按引用事实实际引用时间统计。</summary>
  public long ReferenceCount { get; init; }
  /// <summary>预计节省金额：本行采纳项金额合计，按互认时间过滤，无采纳时为零。</summary>
  public decimal EstimatedSavingAmount { get; init; }
}
