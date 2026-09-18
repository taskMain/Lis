namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 当前有效标准目录的查询投影，展平后的项目行。
/// </summary>
public sealed record EffectiveMedicalStandardCatalogItem
{
  /// <summary>
  /// 所属分类的项目类型；0=检验、1=检查。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 标准项目标识。
  /// </summary>
  public Guid ItemId { get; init; }
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  public Guid CategoryId { get; init; }
  /// <summary>
  /// 所属分类名称；用于按类型与分类组装层级结构的展示键。
  /// </summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  public Guid GroupId { get; init; }
  /// <summary>
  /// 所属分组名称；用于分类内部的层级展示。
  /// </summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目编码。
  /// </summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目名称。
  /// </summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目备注；可空，null 表示当前没有备注。
  /// </summary>
  public string? Remark { get; init; }
  /// <summary>
  /// 最后一次写入该标准项目的操作人标识；供识别业务追溯使用。
  /// </summary>
  public Guid OperId { get; init; }
  /// <summary>
  /// 最后一次写入该标准项目的操作时间；由数据库当前时间写入。
  /// </summary>
  public DateTimeOffset OperTime { get; init; }
}
