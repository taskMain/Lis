using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 接收侧互认使用汇总行：每个汇总维度的一个分组值，以及行内六项指标、同期互认率、不采纳原因汇总与预计节省金额。
/// </summary>
/// <remarks>
/// 每行为所选汇总维度的一个分组值，未参与分组的维度字段为 <see langword="null"/>，不做总计行；
/// 提醒次数按互认匹配生成时间统计，采纳、不采纳与金额按处理结果互认时间统计，引用次数按实际引用时间统计；
/// 同期互认率为行内采纳次数除以提醒次数，提醒为零时未计算、数值无业务含义；
/// 未反馈匹配项无处理结果、无互认科室归属，不计入互认科室维度的提醒次数与同期互认率。
/// </remarks>
public sealed record RecognitionUsageSummaryReadModel
{
  /// <summary>接收组织编码；仅医院、院区与互认科室维度行携带。</summary>
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>接收组织名称；按当页涉及编码批量回填。</summary>
  public string? ReceiverOrganizationName { get; init; }
  /// <summary>接收医院编码；仅医院、院区与互认科室维度行携带。</summary>
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收医院名称；按当页涉及编码批量回填。</summary>
  public string? ReceiverHospitalName { get; init; }
  /// <summary>接收院区编码；仅院区与互认科室维度行携带。</summary>
  public string? ReceiverBranchCode { get; init; }
  /// <summary>接收院区名称；按当页涉及编码批量回填。</summary>
  public string? ReceiverBranchName { get; init; }
  /// <summary>互认科室ID；按「接收院区 + 处理结果互认科室ID」归类，仅互认科室维度行携带。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称；取当前查询范围内互认时间最晚一条处理结果保存的名称。</summary>
  public string? RecognitionDeptName { get; init; }
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
  /// <summary>采纳次数；按处理结果互认时间统计。</summary>
  public int AdoptionCount { get; init; }
  /// <summary>不采纳次数；按处理结果互认时间统计。</summary>
  public int NonAdoptionCount { get; init; }
  /// <summary>引用次数；按引用事实实际引用时间统计。</summary>
  public int ReferenceCount { get; init; }
  /// <summary>提醒次数；按互认匹配生成时间统计，同一查询重复编码已去重。</summary>
  public int ReminderCount { get; init; }
  /// <summary>同期互认率；行内采纳次数除以提醒次数，短周期率可超过百分之百。</summary>
  public decimal SamePeriodRecognitionRate { get; init; }
  /// <summary>同期互认率是否已计算；提醒次数为零时为假、数值无业务含义。</summary>
  public bool SamePeriodRecognitionRateCalculated { get; init; }
  /// <summary>不采纳原因汇总集合；占比分母为行内不采纳次数，为零时不计算占比。</summary>
  public IReadOnlyList<NonAdoptionReasonSummaryReadModel> NonAdoptionReasons { get; init; } = [];
  /// <summary>预计节省金额；等于本行采纳项金额合计，不另建金额事实。</summary>
  public decimal EstimatedSavingAmount { get; init; }
  /// <summary>本行使用的汇总维度；调用方按其区分其余维度字段的业务含义。</summary>
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
}
