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
