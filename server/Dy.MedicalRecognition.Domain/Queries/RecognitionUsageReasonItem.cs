using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 接收侧汇总的不采纳原因行投影：与汇总分组键同套的一个分组值加一种原因代码及其次数。
/// </summary>
/// <remarks>
/// 原因行独立成行返回，避免使用方言字符串聚合；应用层按分组键把原因行装配进所属汇总行，
/// 占比由应用层以行内不采纳次数为分母计算，不在投影内。
/// </remarks>
public sealed record RecognitionUsageReasonItem
{
  /// <summary>接收组织编码；分组键随所选维度取值，未参与分组的维度为空值。</summary>
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>接收医院编码。</summary>
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收院区编码。</summary>
  public string? ReceiverBranchCode { get; init; }
  /// <summary>互认科室ID。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>项目类型。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准项目编码。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>标准项目名称。</summary>
  public string? StandardItemName { get; init; }
  /// <summary>标准目录分类名称。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称。</summary>
  public string? GroupName { get; init; }
  /// <summary>不采纳原因：平台统一原因代码，「其他」原因按统一代码归集。</summary>
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  /// <summary>该分组值下此原因的不采纳次数。</summary>
  public long ReasonCount { get; init; }
}
