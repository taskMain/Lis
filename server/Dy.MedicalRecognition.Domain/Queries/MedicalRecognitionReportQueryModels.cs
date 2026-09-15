namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 标准项目分类列表的查询投影。
/// </summary>
/// <remarks>供目录维护界面按项目类型浏览分类。</remarks>
public sealed record MedicalStandardCategoryListItem
{
  /// <summary>
  /// 分类标识；查询结果的稳定排序兜底键。
  /// </summary>
  public Guid CategoryId { get; init; }
  /// <summary>
  /// 分类的项目类型；0=检验、1=检查。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 分类名称；在全平台范围内唯一。
  /// </summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>
  /// 分类的启用状态；停用为软删除，记录保留且可再启用。
  /// </summary>
  public bool IsValid { get; init; }
  /// <summary>
  /// 分类备注；可空，null 表示当前没有备注。
  /// </summary>
  public string? Remark { get; init; }
  /// <summary>
  /// 派生使用情况；只要该分类下存在分组即为已使用，与分组是否停用无关。
  /// </summary>
  public MedicalStandardUsageStatus UsageStatus { get; init; }
}

/// <summary>
/// 标准项目分组列表的查询投影。
/// </summary>
/// <remarks>供目录维护界面在指定分类下浏览分组。</remarks>
public sealed record MedicalStandardGroupListItem
{
  /// <summary>
  /// 分组标识；查询结果的稳定排序兜底键。
  /// </summary>
  public Guid GroupId { get; init; }
  /// <summary>
  /// 分组所属分类的标识；分组创建后归属不可变更。
  /// </summary>
  public Guid CategoryId { get; init; }
  /// <summary>
  /// 分组名称；在同一分类内唯一、跨分类可重名。
  /// </summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>
  /// 分组的启用状态；停用不级联改写其下标准项目。
  /// </summary>
  public bool IsValid { get; init; }
  /// <summary>
  /// 分组备注；可空，null 表示当前没有备注。
  /// </summary>
  public string? Remark { get; init; }
  /// <summary>
  /// 派生使用情况；只要该分组下存在标准项目即为已使用，与项目是否停用无关。
  /// </summary>
  public MedicalStandardUsageStatus UsageStatus { get; init; }
}

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
