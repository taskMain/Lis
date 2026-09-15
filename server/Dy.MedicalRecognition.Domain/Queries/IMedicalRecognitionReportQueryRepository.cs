namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 标准项目目录的只读查询端口。
/// </summary>
/// <remarks>读取分类、分组、标准项目列表与当前有效目录；返回的内部投影不作为对外契约。</remarks>
public interface IMedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 查询标准项目分类列表。
  /// </summary>
  /// <remarks>传 <see langword="null"/> 表示不过滤；无匹配时返回空集合。</remarks>
  /// <param name="itemType">项目类型筛选。</param>
  /// <returns>分类集合。</returns>
  Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType);

  /// <summary>
  /// 查询标准项目分组列表。
  /// </summary>
  /// <remarks>传 <see langword="null"/> 表示不过滤；无匹配时返回空集合。</remarks>
  /// <param name="categoryId">所属分类筛选。</param>
  /// <returns>分组集合。</returns>
  Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId);

  /// <summary>
  /// 查询标准项目列表。
  /// </summary>
  /// <remarks>
  /// 支持分类、分组、编码、名称与启用状态的组合筛选。
  /// 编码与名称为字面包含条件，<c>%</c>、<c>_</c> 不作为通配符，名称的符号处理同编码。
  /// 传 <see langword="null"/> 表示不过滤；无匹配时返回空集合。
  /// </remarks>
  /// <param name="categoryId">所属分类筛选。</param>
  /// <param name="groupId">所属分组筛选。</param>
  /// <param name="code">编码筛选条件。</param>
  /// <param name="name">名称筛选条件。</param>
  /// <param name="isValid">项目启用状态筛选。</param>
  /// <returns>项目集合。</returns>
  Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid);

  /// <summary>
  /// 查询当前有效标准目录。
  /// </summary>
  /// <remarks>
  /// 只包含分类、分组与标准项目三级均启用的项目。
  /// 分类名称为字面包含条件，符号处理同编码。
  /// 传 <see langword="null"/> 表示不过滤；无匹配时返回空集合；按项目类型、分类、分组与名称升序。
  /// </remarks>
  /// <param name="itemType">项目类型筛选。</param>
  /// <param name="categoryName">分类名称筛选条件。</param>
  /// <returns>展平的有效项目行集合。</returns>
  Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName);
}
