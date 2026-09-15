using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 分类列表查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record MedicalStandardCategoryListQueryRequest
{
  /// <summary>
  /// 项目类型筛选条件；不传表示不按类型过滤，传入则须为已定义类型。
  /// </summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
}

/// <summary>
/// 分组列表查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record MedicalStandardGroupListQueryRequest
{
  /// <summary>
  /// 所属分类筛选条件；不传表示不按分类过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空Guid。")]
  public Guid? CategoryId { get; init; }
}

/// <summary>
/// 标准项目列表查询条件；筛选条件均可选，同时传入时取交集。
/// </summary>
public sealed record MedicalStandardItemListQueryRequest
{
  /// <summary>
  /// 所属分类筛选条件；不传表示不按分类过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空Guid。")]
  public Guid? CategoryId { get; init; }
  /// <summary>
  /// 所属分组筛选条件；不传表示不按分组过滤，传入则须为非空标识。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分组ID不能为空Guid。")]
  public Guid? GroupId { get; init; }
  /// <summary>
  /// 标准项目编码包含匹配条件；按字面匹配，非空值时不可为空白。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：编码不能是空白。")]
  public string? Code { get; init; }
  /// <summary>
  /// 标准项目名称包含匹配条件；匹配规则同编码。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：名称不能是空白。")]
  public string? Name { get; init; }
  /// <summary>
  /// 启用状态筛选条件；不传表示启用与停用的项目都返回。
  /// </summary>
  public bool? IsValid { get; init; }
}

/// <summary>
/// 当前有效标准目录查询条件；筛选条件均可选，缺省表示全部。
/// </summary>
public sealed record EffectiveMedicalStandardCatalogQueryRequest
{
  /// <summary>
  /// 项目类型筛选条件；不传表示不按类型过滤，传入则须为已定义类型。
  /// </summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
  /// <summary>
  /// 分类名称包含匹配条件；按字面匹配，非空值时不可为空白。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类名称不能是空白。")]
  public string? CategoryName { get; init; }
}

/// <summary>标准医疗项目分类列表项。</summary>
public sealed record MedicalStandardCategoryListReadModel
{
  /// <summary>分类ID。</summary>
  public Guid CategoryId { get; init; }
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>分类名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>是否启用。</summary>
  public bool IsValid { get; init; }
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
  /// <summary>是否存在下级分组。</summary>
  public MedicalStandardUsageStatus UsageStatus { get; init; }
}

/// <summary>标准医疗项目分组列表项。</summary>
public sealed record MedicalStandardGroupListReadModel
{
  /// <summary>分组ID。</summary>
  public Guid GroupId { get; init; }
  /// <summary>所属分类ID。</summary>
  public Guid CategoryId { get; init; }
  /// <summary>分组名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>是否启用。</summary>
  public bool IsValid { get; init; }
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
  /// <summary>是否存在下级标准项目。</summary>
  public MedicalStandardUsageStatus UsageStatus { get; init; }
}

/// <summary>标准医疗项目列表项。</summary>
public sealed record MedicalStandardItemListReadModel
{
  /// <summary>标准项目ID。</summary>
  public Guid ItemId { get; init; }
  /// <summary>所属分类ID。</summary>
  public Guid CategoryId { get; init; }
  /// <summary>所属分组ID。</summary>
  public Guid GroupId { get; init; }
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>标准项目编码。</summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>是否启用。</summary>
  public bool IsValid { get; init; }
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
}

/// <summary>当前有效标准医疗项目目录根对象。</summary>
public sealed record EffectiveMedicalStandardCatalogReadModel
{
  /// <summary>按项目类型分组的目录集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogTypeReadModel> ItemTypes { get; init; } = [];
}

/// <summary>当前有效标准目录的项目类型节点。</summary>
public sealed record EffectiveMedicalStandardCatalogTypeReadModel
{
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>该类型下的分类集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogCategoryReadModel> Categories { get; init; } = [];
}

/// <summary>当前有效标准目录的分类节点。</summary>
public sealed record EffectiveMedicalStandardCatalogCategoryReadModel
{
  /// <summary>分类名称。</summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>该分类下的分组集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogGroupReadModel> Groups { get; init; } = [];
}

/// <summary>当前有效标准目录的分组节点。</summary>
public sealed record EffectiveMedicalStandardCatalogGroupReadModel
{
  /// <summary>分组名称。</summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>该分组下的项目集合。</summary>
  public IReadOnlyList<EffectiveMedicalStandardCatalogItemReadModel> Items { get; init; } = [];
}

/// <summary>当前有效标准目录的项目明细。</summary>
public sealed record EffectiveMedicalStandardCatalogItemReadModel
{
  /// <summary>标准项目编码。</summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
  /// <summary>最后实际维护该项目的操作人。</summary>
  public Guid OperId { get; init; }
  /// <summary>最后实际维护该项目的操作时间。</summary>
  public DateTimeOffset OperTime { get; init; }
}
