namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 标准项目列表的查询投影。
/// </summary>
/// <remarks>供目录维护界面按分类、分组、编码、名称与启用状态组合筛选。</remarks>
public sealed record MedicalStandardItemListItem
{
  /// <summary>
  /// 标准项目标识；查询结果的稳定排序兜底键。
  /// </summary>
  public Guid ItemId { get; init; }
  /// <summary>
  /// 所属分类标识；本投影的分类与该项目的分组所属分类必然一致。
  /// </summary>
  public Guid CategoryId { get; init; }
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  public Guid GroupId { get; init; }
  /// <summary>
  /// 项目类型；0=检验、1=检查，取自所属分类，不是标准项目自身的独立列。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 标准项目编码；在全平台范围内唯一，停用项目仍占用其编码。
  /// </summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目名称；用于展示与互认结果说明，不参与编码唯一性判断。
  /// </summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>
  /// 项目自身的启用状态；不代表当前有效性，还须分类与分组同时启用。
  /// </summary>
  public bool IsValid { get; init; }
  /// <summary>
  /// 标准项目备注；可空，null 表示当前没有备注。
  /// </summary>
  public string? Remark { get; init; }
}
