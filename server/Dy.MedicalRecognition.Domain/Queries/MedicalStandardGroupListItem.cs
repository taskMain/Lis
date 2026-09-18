namespace Dy.MedicalRecognition.Domain.Queries;

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
