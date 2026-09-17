using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Base.Application.Contracts.UserAggregate;
using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Domain.Queries;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 标准项目目录、互认项目配置与互认项目金额只读查询的应用服务实现。
/// </summary>
/// <remarks>全部查询都只筛选与投影，不修改数据，也不判断业务状态；查询不产生写入、审计事件或消息。</remarks>
public sealed class MedicalRecognitionReportQueryAppService : ApplicationService, IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 标准项目目录的只读查询端口。
  /// </summary>
  private readonly IMedicalRecognitionReportQueryRepository repository;

  /// <summary>
  /// 金额查询入口解析组织、医院与院区路径所用的解析点。
  /// </summary>
  /// <remarks>
  /// 一次调用只解析一批目标路径，组织全量只读一次、每家不同医院读取一次院区，
  /// 因此外部组织服务的读取次数不随返回行数增长。
  /// </remarks>
  private readonly OrganizationPathResolver organizationPathResolver;

  /// <summary>
  /// 医院管理员金额查询入口解析可信组织与可信医院所用的解析点。
  /// </summary>
  /// <remarks>令牌已提供组织与医院两层时不读取用户档案；令牌缺失的层由当前登录用户档案补齐。</remarks>
  private readonly TrustedScopeResolver trustedScopeResolver;

  /// <summary>
  /// 接收标准项目目录的只读查询端口与外部服务作为数据来源。
  /// </summary>
  /// <remarks>
  /// 组织路径解析器与可信范围解析器都在构造函数内建立：两者都是应用层内部类型，不能出现在公开构造函数的参数上；
  /// 可信范围解析所用的外部用户服务同样只在构造函数内转交内部解析点。
  /// </remarks>
  /// <param name="repository">目录只读查询端口。</param>
  /// <param name="organizationAppService">提供组织、医院与院区主数据的外部组织服务。</param>
  /// <param name="userAppService">提供当前登录用户组织与医院归属的外部用户服务，用于补齐令牌缺失的可信层。</param>
  public MedicalRecognitionReportQueryAppService(
    IMedicalRecognitionReportQueryRepository repository,
    IOrganizationAppService organizationAppService,
    IUserAppService userAppService)
  {
    this.repository = repository;
    organizationPathResolver = new OrganizationPathResolver(organizationAppService);
    trustedScopeResolver = new TrustedScopeResolver(userAppService);
  }

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
  /// 查询互认项目金额列表并映射为只读模型（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 组织、医院与院区按请求使用，服务端校验三者存在、启用且父子归属正确，不要求请求组织等于可信上下文组织，
  /// 因此平台管理员可以查询其他组织的金额；权限由权限系统负责，不在服务端做组织相等校验、也不静默过滤。
  /// 行集由该组织已建立的互认配置驱动，未配置金额的行按“未配置”返回；名称与归属由组织路径解析一次性批量读取取得。
  /// </remarks>
  /// <param name="request">金额列表查询条件，携带请求的组织、医院、院区与可选标准项目编码。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">组织、医院或院区缺失或为空白文本，或标准项目编码为空白文本时抛出；空白文本不代表“不过滤”，而是无效的筛选条件。</exception>
  /// <exception cref="InvalidOperationException">请求的组织、医院或院区不存在、已停用或父子归属不匹配时抛出；此时不返回该范围的数据，也不降级为空集合。</exception>
  public async Task<IEnumerable<RecognitionAmountReadModel>> QueryRecognitionAmountListAsync(RecognitionAmountListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    OrganizationPathResolver.OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathResolver.OrganizationPathTarget(request.OrganizationCode, request.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    IEnumerable<RecognitionAmountListItem> items =
      await repository.QueryRecognitionAmountListAsync(path.OrganizationCode, path.HospitalCode, path.BranchCode, request.StandardProjectCode);
    return items.Select(item => Map(item, path));
  }

  /// <summary>
  /// 查询本院区互认项目金额列表并映射为只读模型（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 组织与医院只取自可信上下文，请求不提交也不得覆盖；可信上下文的组织层与医院层按同一规则解析：
  /// 登录令牌该层非空白即取令牌值，令牌该层缺失或空白时用当前登录用户档案的 <c>OrgId</c>/<c>HosId</c> 补齐，
  /// 令牌与用户档案都提供该层且不一致即拒绝，补齐后该层仍为空即拒绝，不使用默认值、不降级为空值；
  /// 请求院区必须存在、启用且属于可信医院与可信组织，不属于即拒绝，不返回其他医院的数据、也不降级为空集合；
  /// 院区层不参与可信回落。其余查询与映射与平台管理员入口完全相同，两个入口共用同一投影与同一原因派生。
  /// </remarks>
  /// <param name="request">金额列表查询条件，只携带请求院区与可选标准项目编码。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定调用方意图的筛选范围。</exception>
  /// <exception cref="ValidationException">院区缺失或为空白文本，或标准项目编码为空白文本时抛出；空白文本不代表“不过滤”，而是无效的筛选条件。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信上下文中取不到组织或医院时抛出，此时不使用默认值、不降级为空值；
  /// 请求院区不存在、已停用或不属于可信医院时同样抛出，此时不返回其他医院的数据、也不降级为空集合。
  /// </exception>
  public async Task<IEnumerable<RecognitionAmountReadModel>> QueryBranchRecognitionAmountListAsync(BranchRecognitionAmountListQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScopeResolver.TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);

    OrganizationPathResolver.OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathResolver.OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    IEnumerable<RecognitionAmountListItem> items =
      await repository.QueryRecognitionAmountListAsync(path.OrganizationCode, path.HospitalCode, path.BranchCode, request.StandardProjectCode);
    return items.Select(item => Map(item, path));
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

  /// <summary>
  /// 把金额查询结果与已校验的组织路径映射为只读模型。
  /// </summary>
  /// <remarks>
  /// 三层名称取组织路径解析的一次性批量读取结果，不按行读取外部组织服务；
  /// “金额已配置”按当前金额是否有值派生，零元同样为真，与从未配置区分；
  /// 配置状态只由配置自身的启用状态决定，目录停用不改变该字段。
  /// </remarks>
  /// <param name="item">查询端口返回的金额行投影，含当前金额与标准目录三层状态。</param>
  /// <param name="path">已校验通过的组织路径，提供组织、医院与院区名称。</param>
  /// <returns>金额列表只读模型。</returns>
  private static RecognitionAmountReadModel Map(RecognitionAmountListItem item, OrganizationPathResolver.OrganizationPath path) => new()
  {
    StandardProjectCode = item.StandardProjectCode,
    StandardProjectName = item.StandardItemName,
    ItemType = item.ItemType,
    CategoryName = item.CategoryName,
    GroupName = item.GroupName,
    OrganizationName = path.OrganizationName,
    HospitalName = path.HospitalName,
    BranchName = path.BranchName,
    ConfigurationStatus = item.ConfigurationIsValid ? ConfigurationStatus.Enabled : ConfigurationStatus.Disabled,
    UnavailableReason = ResolveUnavailableReason(item),
    CurrentAmount = item.CurrentAmount,
    IsAmountConfigured = item.CurrentAmount.HasValue
  };

  /// <summary>
  /// 按所属分类、所属分组、标准项目的顺序派生第一条目录停用原因。
  /// </summary>
  /// <remarks>
  /// 只表达标准目录停用：目录三层全部启用时为 null，即使配置自身已停用或金额尚未配置也保持 null，
  /// 配置自身的状态只由配置状态字段表达，金额是否配置只由金额与已配置字段表达。
  /// </remarks>
  /// <param name="item">查询端口返回的金额行投影，含标准目录三层的启用状态。</param>
  /// <returns>第一条目录停用原因文案；目录三层全部启用时为 null。</returns>
  private static string? ResolveUnavailableReason(RecognitionAmountListItem item)
  {
    if (!item.CategoryIsValid) return "所属分类已停用";
    if (!item.GroupIsValid) return "所属分组已停用";
    if (!item.ItemIsValid) return "标准项目已停用";

    return null;
  }
}
