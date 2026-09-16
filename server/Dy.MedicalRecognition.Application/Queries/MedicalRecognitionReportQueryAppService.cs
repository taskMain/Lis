using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Domain.Queries;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 标准项目目录与互认项目配置只读查询的应用服务实现。
/// </summary>
/// <remarks>全部查询都只筛选与投影，不修改数据，也不判断业务状态。</remarks>
public sealed class MedicalRecognitionReportQueryAppService : ApplicationService, IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 标准项目目录的只读查询端口。
  /// </summary>
  private readonly IMedicalRecognitionReportQueryRepository repository;

  /// <summary>
  /// 接收标准项目目录的只读查询端口作为数据来源。
  /// </summary>
  /// <param name="repository">目录只读查询端口。</param>
  public MedicalRecognitionReportQueryAppService(IMedicalRecognitionReportQueryRepository repository) => this.repository = repository;

  /// <summary>
  /// 查询分类列表并映射为只读模型。
  /// </summary>
  /// <remarks>结果包含已停用分类。</remarks>
  /// <param name="request">分类列表查询条件。</param>
  /// <returns>分类列表只读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">项目类型已传入但不是已定义的项目类型枚举值时抛出，避免按无效类型查询出空结果。</exception>
  public async Task<IEnumerable<MedicalStandardCategoryListReadModel>> QueryMedicalStandardCategoryListAsync(MedicalStandardCategoryListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    return (await repository.QueryMedicalStandardCategoryListAsync(request.ItemType)).Select(Map);
  }

  /// <summary>
  /// 查询分组列表并映射为只读模型。
  /// </summary>
  /// <remarks>结果包含已停用分组。</remarks>
  /// <param name="request">分组列表查询条件。</param>
  /// <returns>分组列表只读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">所属分类已传入但为空 Guid 时抛出；空 Guid 不代表“不过滤”，而是无法匹配任何分类的无效条件。</exception>
  public async Task<IEnumerable<MedicalStandardGroupListReadModel>> QueryMedicalStandardGroupListAsync(MedicalStandardGroupListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    return (await repository.QueryMedicalStandardGroupListAsync(request.CategoryId)).Select(Map);
  }

  /// <summary>
  /// 查询标准项目列表并映射为只读模型。
  /// </summary>
  /// <remarks>多个筛选条件取交集。</remarks>
  /// <param name="request">标准项目列表查询条件。</param>
  /// <returns>标准项目列表只读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">所属分类或所属分组已传入但为空 Guid 时抛出，或者编码或名称已传入但为空白文本时抛出；空白文本不代表“不过滤”，而是会放大匹配结果的无效条件。</exception>
  public async Task<IEnumerable<MedicalStandardItemListReadModel>> QueryMedicalStandardItemListAsync(MedicalStandardItemListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    return (await repository.QueryMedicalStandardItemListAsync(request.CategoryId, request.GroupId, request.Code, request.Name, request.IsValid)).Select(Map);
  }

  /// <summary>
  /// 查询当前有效标准目录并组装为只读模型。
  /// </summary>
  /// <remarks>只包含分类、分组与标准项目三级均启用的项目。</remarks>
  /// <param name="request">当前有效目录查询条件。</param>
  /// <returns>当前有效目录只读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">项目类型已传入但不是已定义的项目类型枚举值时抛出，或者分类名称已传入但为空白文本时抛出；空白文本不代表“不过滤”，而是会放大匹配结果的无效条件。</exception>
  public async Task<EffectiveMedicalStandardCatalogReadModel> QueryEffectiveMedicalStandardCatalogAsync(EffectiveMedicalStandardCatalogQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    IEnumerable<EffectiveMedicalStandardCatalogItem> items = await repository.QueryEffectiveMedicalStandardCatalogAsync(request.ItemType, request.CategoryName);
    return new EffectiveMedicalStandardCatalogReadModel
    {
      ItemTypes = items.GroupBy(item => item.ItemType).OrderBy(group => group.Key).Select(MapType).ToArray()
    };
  }

  /// <summary>
  /// 查询标准项目互认配置列表并映射为只读模型。
  /// </summary>
  /// <remarks>
  /// 只返回可信当前组织自己的配置；标准项目名称、类型、分类与分组按当前标准目录实时关联。
  /// 不可用原因只表达所属分类、分组或标准项目停用，按“分类 → 分组 → 标准项目”返回第一条，目录三层全部启用时为 null。
  /// </remarks>
  /// <param name="request">互认配置列表查询条件。</param>
  /// <returns>互认配置只读模型集合；无匹配配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">组织编码缺失或为空白文本，标准项目编码已传入但为空白文本，或者配置状态已传入但不是已定义的配置状态枚举值时抛出；空白文本不代表“不过滤”，而是无效的筛选条件。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织，或请求组织与可信当前组织不一致时抛出；此时不返回该组织的数据，也不降级为空集合。</exception>
  public async Task<IEnumerable<RecognitionProjectConfigurationReadModel>> QueryRecognitionProjectConfigurationListAsync(RecognitionProjectConfigurationListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    // 组织范围只信服务端可信当前组织：请求组织与之不一致即拒绝，不返回其他组织的数据、也不降级为空集合。
    // 可信组织与写入侧共用统一解析点，先去除首尾空白再判定，空白组织一律拒绝。
    string currentOrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定有效的当前组织。");
    // 请求组织与可信组织同源，两侧都可能带首尾空白：比较与下传统一用去空白后的可信值，
    // 否则可信组织带空白时会出现"写入成功、查询被误判为越权"的读写不对称。
    if (!string.Equals(request.OrganizationCode.Trim(), currentOrganizationCode, StringComparison.Ordinal))
    {
      throw new InvalidOperationException("请求组织与当前登录组织不一致，不能查询该组织的互认配置。");
    }

    IEnumerable<RecognitionProjectConfigurationListItem> items = await repository.QueryRecognitionProjectConfigurationListAsync(currentOrganizationCode, request.StandardProjectCode, request.ConfigurationStatus);
    return items.Select(Map);
  }

  /// <summary>
  /// 把一个项目类型下的目录明细组装为项目类型节点。
  /// </summary>
  /// <param name="type">按项目类型分组的目录明细，组内仍为平铺记录。</param>
  /// <returns>项目类型节点。</returns>
  private static EffectiveMedicalStandardCatalogTypeReadModel MapType(IGrouping<MedicalItemType, EffectiveMedicalStandardCatalogItem> type) => new()
  {
    ItemType = type.Key,
    Categories = type.GroupBy(item => item.CategoryId).OrderBy(group => group.First().CategoryName).ThenBy(group => group.Key).Select(MapCategory).ToArray()
  };

  /// <summary>
  /// 把一个分类下的目录明细组装为分类节点。
  /// </summary>
  /// <param name="category">按分类标识分组的目录明细。</param>
  /// <returns>分类节点。</returns>
  private static EffectiveMedicalStandardCatalogCategoryReadModel MapCategory(IGrouping<Guid, EffectiveMedicalStandardCatalogItem> category) => new()
  {
    CategoryName = category.First().CategoryName,
    Groups = category.GroupBy(item => item.GroupId).OrderBy(group => group.First().GroupName).ThenBy(group => group.Key).Select(MapGroup).ToArray()
  };

  /// <summary>
  /// 把一个分组下的目录明细组装为分组节点。
  /// </summary>
  /// <param name="group">按分组标识分组的目录明细。</param>
  /// <returns>分组节点。</returns>
  private static EffectiveMedicalStandardCatalogGroupReadModel MapGroup(IGrouping<Guid, EffectiveMedicalStandardCatalogItem> group) => new()
  {
    GroupName = group.First().GroupName,
    Items = group.OrderBy(item => item.Name).ThenBy(item => item.ItemId).Select(item => new EffectiveMedicalStandardCatalogItemReadModel
    {
      Code = item.Code, Name = item.Name, Remark = item.Remark, OperId = item.OperId, OperTime = item.OperTime
    }).ToArray()
  };

  /// <summary>
  /// 把分类查询结果映射为只读模型。
  /// </summary>
  /// <param name="item">查询端口返回的分类列表项。</param>
  /// <returns>分类列表只读模型。</returns>
  private static MedicalStandardCategoryListReadModel Map(MedicalStandardCategoryListItem item) => new()
  {
    CategoryId = item.CategoryId, ItemType = item.ItemType, Name = item.Name, IsValid = item.IsValid, Remark = item.Remark, UsageStatus = item.UsageStatus
  };

  /// <summary>
  /// 把分组查询结果映射为只读模型。
  /// </summary>
  /// <param name="item">查询端口返回的分组列表项。</param>
  /// <returns>分组列表只读模型。</returns>
  private static MedicalStandardGroupListReadModel Map(MedicalStandardGroupListItem item) => new()
  {
    GroupId = item.GroupId, CategoryId = item.CategoryId, Name = item.Name, IsValid = item.IsValid, Remark = item.Remark, UsageStatus = item.UsageStatus
  };

  /// <summary>
  /// 把标准项目查询结果映射为只读模型。
  /// </summary>
  /// <param name="item">查询端口返回的标准项目列表项。</param>
  /// <returns>标准项目列表只读模型。</returns>
  private static MedicalStandardItemListReadModel Map(MedicalStandardItemListItem item) => new()
  {
    ItemId = item.ItemId, CategoryId = item.CategoryId, GroupId = item.GroupId, ItemType = item.ItemType, Code = item.Code, Name = item.Name, IsValid = item.IsValid, Remark = item.Remark
  };

  /// <summary>
  /// 把互认配置查询结果映射为只读模型。
  /// </summary>
  /// <param name="item">查询端口返回的互认配置投影，含标准目录三层的启用状态。</param>
  /// <returns>互认配置列表只读模型。</returns>
  private static RecognitionProjectConfigurationReadModel Map(RecognitionProjectConfigurationListItem item) => new()
  {
    ConfigurationId = item.ConfigurationId,
    StandardProjectCode = item.StandardProjectCode,
    StandardItemName = item.StandardItemName,
    ItemType = item.ItemType,
    CategoryName = item.CategoryName,
    GroupName = item.GroupName,
    RecognitionDurationDays = item.RecognitionDurationDays,
    ConfigurationStatus = item.IsValid ? ConfigurationStatus.Enabled : ConfigurationStatus.Disabled,
    UnavailableReason = ResolveUnavailableReason(item)
  };

  /// <summary>
  /// 按所属分类、所属分组、标准项目的顺序派生第一条目录停用原因。
  /// </summary>
  /// <remarks>
  /// 只表达标准目录停用：目录三层全部启用时为 null，即使配置自身已停用也保持 null，配置自身的状态只由配置状态字段表达。
  /// </remarks>
  /// <param name="item">查询端口返回的互认配置投影，含标准目录三层的启用状态。</param>
  /// <returns>第一条目录停用原因文案；目录三层全部启用时为 null。</returns>
  private static string? ResolveUnavailableReason(RecognitionProjectConfigurationListItem item)
  {
    if (!item.CategoryIsValid) return "所属分类已停用";
    if (!item.GroupIsValid) return "所属分组已停用";
    if (!item.ItemIsValid) return "标准项目已停用";

    return null;
  }
}
