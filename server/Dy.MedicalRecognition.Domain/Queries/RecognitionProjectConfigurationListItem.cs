namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 互认项目配置列表的查询投影；在配置字段之外携带标准目录三层的启用状态，供应用层派生目录停用原因。
/// </summary>
/// <remarks>
/// 分类、分组和标准项目的启用状态只用于派生不可用原因，不作为对外契约的字段返回。
/// 投影不携带操作人和操作时间，配置的创建与修改信息不对外返回。
/// </remarks>
public sealed record RecognitionProjectConfigurationListItem
{
  /// <summary>
  /// 互认配置标识。
  /// </summary>
  public Guid ConfigurationId { get; init; }
  /// <summary>
  /// 标准项目编码；查询结果按该编码升序排列。
  /// </summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目名称；实时取自标准目录，互认配置不重复保存。
  /// </summary>
  public string StandardItemName { get; init; } = string.Empty;
  /// <summary>
  /// 项目类型；0=检验、1=检查，实时取自标准项目所属分类。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 所属分类名称；实时取自标准目录。
  /// </summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>
  /// 所属分组名称；实时取自标准目录。
  /// </summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>
  /// 可互认时间天数；从报告时间起按连续24小时计算。
  /// </summary>
  public int RecognitionDurationDays { get; init; }
  /// <summary>
  /// 配置自身的启用状态；true 为启用、false 为停用，与标准目录的启用状态无关。
  /// </summary>
  public bool IsValid { get; init; }
  /// <summary>
  /// 所属分类当前是否启用；停用时应用层派生的不可用原因为“所属分类已停用”。
  /// </summary>
  public bool CategoryIsValid { get; init; }
  /// <summary>
  /// 所属分组当前是否启用；分类启用而该值为 false 时派生的不可用原因为“所属分组已停用”。
  /// </summary>
  public bool GroupIsValid { get; init; }
  /// <summary>
  /// 标准项目当前是否启用；分类与分组都启用而该值为 false 时派生的不可用原因为“标准项目已停用”。
  /// </summary>
  public bool ItemIsValid { get; init; }
}
