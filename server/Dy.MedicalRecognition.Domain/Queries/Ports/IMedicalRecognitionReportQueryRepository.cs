namespace Dy.MedicalRecognition.Domain.Queries.Ports;

/// <summary>
/// 标准项目目录的只读查询端口。
/// </summary>
/// <remarks>读取分类、分组、标准项目列表与当前有效目录；返回的内部投影不作为对外契约。</remarks>
public partial interface IMedicalRecognitionReportQueryRepository
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

  /// <summary>
  /// 查询指定组织范围内尚未建立互认配置、且三级均启用的标准项目目录。
  /// </summary>
  /// <remarks>
  /// 组织编码是必填的范围条件，调用方须先确认它是可信当前组织。
  /// 已建立互认配置的项目（含配置自身已停用的项目）与三级任一级停用的项目都不返回。
  /// 关键字按字面包含同时匹配标准项目编码、名称、分类名称与分组名称，符号处理同编码且不区分大小写。
  /// 传 <see langword="null"/> 表示不按关键字过滤；无可新增项目时返回空集合；排序同有效目录查询。
  /// </remarks>
  /// <param name="organizationCode">组织编码；只排除该组织已建立的配置。</param>
  /// <param name="keyword">关键字筛选条件。</param>
  /// <returns>展平的可新增标准项目行集合。</returns>
  Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QuerySelectableStandardItemListAsync(string organizationCode, string? keyword);

  /// <summary>
  /// 查询指定组织范围内的互认项目配置，并实时关联标准项目所属分类、分组和标准项目。
  /// </summary>
  /// <remarks>
  /// 组织编码是必填的范围条件，调用方须先确认它是可信当前组织。
  /// 关键字按字面包含同时匹配标准项目编码、名称、分类名称与分组名称，<c>%</c>、<c>_</c> 不作为通配符，比较不区分大小写。
  /// 传 <see langword="null"/> 表示不按该条件过滤；无匹配时返回空集合；按标准项目编码升序、配置标识升序。
  /// </remarks>
  /// <param name="organizationCode">配置所属组织编码；只返回该组织的配置。</param>
  /// <param name="keyword">关键字筛选条件。</param>
  /// <param name="configurationStatus">配置启用状态筛选条件。</param>
  /// <returns>互认配置与标准目录三层状态的投影集合。</returns>
  Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? keyword, ConfigurationStatus? configurationStatus);

  /// <summary>
  /// 查询指定组织、医院与院区范围内的互认项目金额列表，并实时关联标准项目所属分类、分组和标准项目。
  /// </summary>
  /// <remarks>
  /// 行集由该组织已建立的互认项目配置驱动，因此行数等于该组织已配置的标准项目数，且包含停用的配置；
  /// 金额表按组织编码、医院编码、院区编码与标准项目编码四个业务键左连接，未配置金额的行金额为空值。
  /// 标准项目编码按字面包含匹配，<c>%</c>、<c>_</c> 不作为通配符。
  /// 关键字按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，<c>%</c>、<c>_</c> 不作为通配符，比较不区分大小写。
  /// 两个筛选条件都传 <see langword="null"/> 表示不按该条件过滤；无匹配时返回空集合；只按标准项目编码升序。
  /// </remarks>
  /// <param name="organizationCode">互认配置所属组织编码；只返回该组织的配置。</param>
  /// <param name="hospitalCode">金额所属医院编码，作为金额表侧的连接条件。</param>
  /// <param name="branchCode">金额所属院区编码，作为金额表侧的连接条件。</param>
  /// <param name="standardProjectCode">标准项目编码筛选条件。</param>
  /// <param name="keyword">关键字筛选条件；四列任一命中即返回。</param>
  /// <returns>金额行与标准目录三层状态的投影集合。</returns>
  Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode, string? keyword);
}
