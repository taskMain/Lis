using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Repository.Queries;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 2 互认项目配置查询的应用层行为校验（矩阵 V12-V14、V19-V20 中不依赖数据库的部分）：
/// 可信组织校验、标准目录三层停用原因的优先级与空值语义、字段映射完整性、筛选条件下传与空结果。
/// </summary>
/// <remarks>
/// 查询端口使用手写替身，不连接数据库；组织范围过滤、目录关联和排序的真实 SQL 行为留给建表后的真实库集成验证。
/// 可信当前组织通过框架公开工厂 <see cref="Dy.Core.Abstractions.Http.HttpContextInfoFactory"/> 注入，与写入口用例共用
/// <see cref="TrustedRequestContext"/>，不再反射写内部设置器。
/// </remarks>
public sealed class Stage2QueryTests
{
  /// <summary>可信当前组织编码；用例通过框架请求上下文注入该值。</summary>
  private const string TrustedOrganizationCode = "ORG-A";
  /// <summary>与可信当前组织不同的组织编码，用于越权查询用例。</summary>
  private const string OtherOrganizationCode = "ORG-B";
  /// <summary>用例注入的令牌用户标识；查询不读取操作人，只用于构造完整的认证上下文。</summary>
  private const string TrustedOperId = "2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e";

  /// <summary>目录三层全部启用时不可用原因为 null，且配置自身停用不改写该字段（V13）。</summary>
  [Fact]
  public async Task Configuration_list_has_no_unavailable_reason_when_catalog_layers_are_enabled()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    MedicalRecognitionReportQueryAppService service = new(new FakeQueryRepository([BuildItem(configurationIsValid: false)]), new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel item = Assert.Single(await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));

    Assert.Null(item.UnavailableReason);
    Assert.Equal(ConfigurationStatus.Disabled, item.ConfigurationStatus);
  }

  /// <summary>只有一层停用时返回该层对应的原因文案（V13）。</summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="expectedReason">期望的不可用原因文案。</param>
  [Theory]
  [InlineData(false, true, true, "所属分类已停用")]
  [InlineData(true, false, true, "所属分组已停用")]
  [InlineData(true, true, false, "标准项目已停用")]
  public async Task Unavailable_reason_reports_the_single_disabled_catalog_layer(bool categoryIsValid, bool groupIsValid, bool itemIsValid, string expectedReason)
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    MedicalRecognitionReportQueryAppService service = new(new FakeQueryRepository([BuildItem(categoryIsValid, groupIsValid, itemIsValid)]), new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel item = Assert.Single(await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));

    Assert.Equal(expectedReason, item.UnavailableReason);
  }

  /// <summary>多层同时停用时按“分类 → 分组 → 标准项目”的顺序返回第一条原因（V20）。</summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="expectedReason">期望的不可用原因文案。</param>
  [Theory]
  [InlineData(false, false, false, "所属分类已停用")]
  [InlineData(false, true, false, "所属分类已停用")]
  [InlineData(false, false, true, "所属分类已停用")]
  [InlineData(true, false, false, "所属分组已停用")]
  [InlineData(true, false, true, "所属分组已停用")]
  [InlineData(true, true, false, "标准项目已停用")]
  public async Task Unavailable_reason_follows_category_group_item_priority(bool categoryIsValid, bool groupIsValid, bool itemIsValid, string expectedReason)
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    MedicalRecognitionReportQueryAppService service = new(new FakeQueryRepository([BuildItem(categoryIsValid, groupIsValid, itemIsValid)]), new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel item = Assert.Single(await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));

    Assert.Equal(expectedReason, item.UnavailableReason);
  }

  /// <summary>上层恢复后实时显露下一层的原因，三层全部恢复后回到 null（V20）。</summary>
  [Fact]
  public async Task Unavailable_reason_reveals_the_next_layer_after_the_upper_layer_is_restored()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeQueryRepository repository = new([BuildItem(false, false, false)]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    Assert.Equal("所属分类已停用", await QueryOnlyReasonAsync(service));

    repository.Items = [BuildItem(true, false, false)];
    Assert.Equal("所属分组已停用", await QueryOnlyReasonAsync(service));

    repository.Items = [BuildItem(true, true, false)];
    Assert.Equal("标准项目已停用", await QueryOnlyReasonAsync(service));

    repository.Items = [BuildItem(true, true, true)];
    Assert.Null(await QueryOnlyReasonAsync(service));
  }

  /// <summary>请求组织与可信当前组织不一致时拒绝查询，且不进入查询端口、不返回其他组织的数据（V19）。</summary>
  [Fact]
  public async Task Query_rejects_request_organization_that_differs_from_trusted_context()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionProjectConfigurationListAsync(Query(OtherOrganizationCode)));

    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>可信请求上下文中取不到组织时同样拒绝，不把查询降级为空集合（V19、V22）。</summary>
  [Fact]
  public async Task Query_rejects_request_when_trusted_organization_is_missing()
  {
    TrustedRequestContext.Use(string.Empty, TrustedOperId);
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));

    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>
  /// 可信当前组织带首尾空白时按去除空白后的值比较，请求组织等于去除空白后的值即可通过查询；
  /// 该口径与写入口共用同一个解析点，避免两侧对同一令牌组织得出不同结论。
  /// </summary>
  [Fact]
  public async Task Query_compares_request_organization_with_trimmed_trusted_organization()
  {
    TrustedRequestContext.Use($"  {TrustedOrganizationCode}  ", TrustedOperId);
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel item = Assert.Single(
      await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));

    Assert.Null(item.UnavailableReason);
    Assert.Equal(1, repository.CallCount);
    Assert.Equal(TrustedOrganizationCode, repository.ReceivedQuery?.OrganizationCode);
  }

  /// <summary>
  /// 请求组织与可信组织同源但都可能带首尾空白：请求侧同样裁剪后比较，并以下传裁剪后的可信值查询。
  /// 原因：只裁剪可信侧会造成"写入成功、查询被判越权"的读写不对称。
  /// </summary>
  [Fact]
  public async Task Query_accepts_request_organization_that_trims_to_the_trusted_organization()
  {
    TrustedRequestContext.Use($"  {TrustedOrganizationCode}  ", TrustedOperId);
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel item = Assert.Single(
      await service.QueryRecognitionProjectConfigurationListAsync(Query($"  {TrustedOrganizationCode}  ")));

    Assert.Equal("PROBE-001", item.StandardProjectCode);
    Assert.Equal(1, repository.CallCount);
    Assert.Equal(TrustedOrganizationCode, repository.ReceivedQuery?.OrganizationCode);
  }

  /// <summary>组织范围与可选筛选条件原样下传查询端口，返回项逐字段映射为只读模型并保持查询端口给出的顺序（V12、V14）。</summary>
  [Fact]
  public async Task Query_passes_filters_to_the_query_port_and_maps_every_field()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeQueryRepository repository = new(
    [
      BuildItem(configurationId: Guid.Parse("11111111-1111-1111-1111-111111111111"), standardProjectCode: "PROBE-001", standardItemName: "血常规", categoryName: "临床检验", groupName: "血液学", recognitionDurationDays: 30, itemType: MedicalItemType.Laboratory),
      BuildItem(configurationId: Guid.Parse("22222222-2222-2222-2222-222222222222"), standardProjectCode: "PROBE-002", standardItemName: "胸部CT", categoryName: "医学影像", groupName: "CT", recognitionDurationDays: 90, itemType: MedicalItemType.Examination, configurationIsValid: false)
    ]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    IEnumerable<RecognitionProjectConfigurationReadModel> result = await service.QueryRecognitionProjectConfigurationListAsync(
      new RecognitionProjectConfigurationListQueryRequest
      {
        OrganizationCode = TrustedOrganizationCode, StandardProjectCode = "PROBE", ConfigurationStatus = ConfigurationStatus.Enabled
      });
    RecognitionProjectConfigurationReadModel[] items = [.. result];

    Assert.Equal(TrustedOrganizationCode, repository.ReceivedQuery?.OrganizationCode);
    Assert.Equal("PROBE", repository.ReceivedQuery?.StandardProjectCode);
    Assert.Equal(ConfigurationStatus.Enabled, repository.ReceivedQuery?.ConfigurationStatus);
    Assert.Equal(2, items.Length);
    // 查询端口的调用次数只随筛选维度增长，不随返回行数增长（两行结果仍只查询一次）。
    Assert.Equal(1, repository.CallCount);

    Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), items[0].ConfigurationId);
    Assert.Equal("PROBE-001", items[0].StandardProjectCode);
    Assert.Equal("血常规", items[0].StandardItemName);
    Assert.Equal(MedicalItemType.Laboratory, items[0].ItemType);
    Assert.Equal("临床检验", items[0].CategoryName);
    Assert.Equal("血液学", items[0].GroupName);
    Assert.Equal(30, items[0].RecognitionDurationDays);
    Assert.Equal(ConfigurationStatus.Enabled, items[0].ConfigurationStatus);
    Assert.Null(items[0].UnavailableReason);

    Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), items[1].ConfigurationId);
    Assert.Equal("PROBE-002", items[1].StandardProjectCode);
    Assert.Equal("胸部CT", items[1].StandardItemName);
    Assert.Equal(MedicalItemType.Examination, items[1].ItemType);
    Assert.Equal("医学影像", items[1].CategoryName);
    Assert.Equal("CT", items[1].GroupName);
    Assert.Equal(90, items[1].RecognitionDurationDays);
    Assert.Equal(ConfigurationStatus.Disabled, items[1].ConfigurationStatus);
  }

  /// <summary>
  /// 结果顺序由查询端口的排序键决定，应用层原样保持端口给出的顺序：既不重新排序也不丢弃。
  /// 端口按乱序返回时输出必须与输入顺序一致，否则"顺序由 SQL 排序键决定"的约定会被应用层静默改写，
  /// 而只断言"两行结果被逐字段映射"的用例发现不了这种改写。查询语句自身的稳定排序键由
  /// <c>Stage1SqlMapProbeTests.Recognition_configuration_query_freezes_organization_sort_and_projection_binding</c> 冻结。
  /// </summary>
  [Fact]
  public async Task Query_preserves_the_order_given_by_the_query_port()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeQueryRepository repository = new(
    [
      BuildItem(configurationId: Guid.Parse("33333333-3333-3333-3333-333333333333"), standardProjectCode: "PROBE-003", standardItemName: "第三项"),
      BuildItem(configurationId: Guid.Parse("11111111-1111-1111-1111-111111111111"), standardProjectCode: "PROBE-001", standardItemName: "第一项"),
      BuildItem(configurationId: Guid.Parse("22222222-2222-2222-2222-222222222222"), standardProjectCode: "PROBE-002", standardItemName: "第二项")
    ]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    RecognitionProjectConfigurationReadModel[] items =
      [.. await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode))];

    Assert.Equal(["PROBE-003", "PROBE-001", "PROBE-002"], items.Select(item => item.StandardProjectCode).ToArray());
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>没有匹配配置时返回空集合而不是 null，未传的可选筛选条件保持为空值下传（V14）。</summary>
  [Fact]
  public async Task Query_returns_empty_collection_when_no_configuration_matches()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeQueryRepository repository = new([]);
    MedicalRecognitionReportQueryAppService service = new(repository, new StubOrganizationAppService(), new StubUserAppService());

    IEnumerable<RecognitionProjectConfigurationReadModel> items =
      await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode));

    Assert.Empty(items);
    Assert.Equal(TrustedOrganizationCode, repository.ReceivedQuery?.OrganizationCode);
    Assert.Null(repository.ReceivedQuery?.StandardProjectCode);
    Assert.Null(repository.ReceivedQuery?.ConfigurationStatus);
  }

  /// <summary>互认配置查询是纯查询：入口不声明工作单元，避免查询路径引入事务或事件。</summary>
  [Fact]
  public void Query_entrypoint_does_not_declare_a_work_unit()
  {
    MethodInfo method = typeof(MedicalRecognitionReportQueryAppService).GetMethod(nameof(MedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync))!;

    Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
  }

  /// <summary>
  /// 配置状态筛选映射为互认配置表布尔启用列的比较值：启用为 true、停用为 false、不传筛选条件时不按该列过滤。
  /// 该断言覆盖离线用例此前只验证“枚举已下传查询端口”所缺少的筛选值语义。
  /// </summary>
  [Fact]
  public void Configuration_status_filter_maps_to_the_enabled_column()
  {
    Assert.True(MedicalRecognitionReportQueryRepository.MapConfigurationStatusToIsValid(ConfigurationStatus.Enabled));
    Assert.False(MedicalRecognitionReportQueryRepository.MapConfigurationStatusToIsValid(ConfigurationStatus.Disabled));
    Assert.Null(MedicalRecognitionReportQueryRepository.MapConfigurationStatusToIsValid(null));
  }

  /// <summary>发起一次组织编码查询并返回唯一结果的不可用原因。</summary>
  /// <param name="service">被测查询应用服务。</param>
  /// <returns>本次查询结果的不可用原因。</returns>
  private static async Task<string?> QueryOnlyReasonAsync(MedicalRecognitionReportQueryAppService service)
  {
    RecognitionProjectConfigurationReadModel item = Assert.Single(await service.QueryRecognitionProjectConfigurationListAsync(Query(TrustedOrganizationCode)));
    return item.UnavailableReason;
  }

  /// <summary>构造一个只提交组织编码的列表查询条件。</summary>
  /// <param name="organizationCode">请求查询的组织编码。</param>
  /// <returns>列表查询条件。</returns>
  private static RecognitionProjectConfigurationListQueryRequest Query(string organizationCode) => new() { OrganizationCode = organizationCode };

  /// <summary>构造一条互认配置查询投影，只把用例关心的字段参数化，其余字段取固定值。</summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="configurationIsValid">配置自身是否启用。</param>
  /// <param name="configurationId">配置标识；不传表示随机生成。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="standardItemName">标准项目名称。</param>
  /// <param name="categoryName">所属分类名称。</param>
  /// <param name="groupName">所属分组名称。</param>
  /// <param name="recognitionDurationDays">可互认时间天数。</param>
  /// <param name="itemType">项目类型。</param>
  /// <returns>查询端口返回的互认配置投影。</returns>
  private static RecognitionProjectConfigurationListItem BuildItem(
    bool categoryIsValid = true,
    bool groupIsValid = true,
    bool itemIsValid = true,
    bool configurationIsValid = true,
    Guid? configurationId = null,
    string standardProjectCode = "PROBE-001",
    string standardItemName = "血常规",
    string categoryName = "临床检验",
    string groupName = "血液学",
    int recognitionDurationDays = 30,
    MedicalItemType itemType = MedicalItemType.Laboratory) => new()
    {
      ConfigurationId = configurationId ?? Guid.NewGuid(),
      StandardProjectCode = standardProjectCode,
      StandardItemName = standardItemName,
      ItemType = itemType,
      CategoryName = categoryName,
      GroupName = groupName,
      RecognitionDurationDays = recognitionDurationDays,
      IsValid = configurationIsValid,
      CategoryIsValid = categoryIsValid,
      GroupIsValid = groupIsValid,
      ItemIsValid = itemIsValid
    };

  /// <summary>
  /// 互认配置查询端口的手写替身：记录本次查询收到的筛选条件与调用次数，并按测试设置的集合返回结果。
  /// </summary>
  private sealed class FakeQueryRepository : IMedicalRecognitionReportQueryRepository
  {
    /// <summary>
    /// 创建替身并设置首次查询返回的投影集合。
    /// </summary>
    /// <param name="items">首次查询返回的互认配置投影集合。</param>
    public FakeQueryRepository(IEnumerable<RecognitionProjectConfigurationListItem> items) => Items = [.. items];

    /// <summary>查询返回的互认配置投影集合；可在用例中重新赋值以模拟目录状态变化。</summary>
    public IEnumerable<RecognitionProjectConfigurationListItem> Items { get; set; }

    /// <summary>本次查询收到的筛选条件；未进入查询端口时为 null。</summary>
    public (string OrganizationCode, string? StandardProjectCode, ConfigurationStatus? ConfigurationStatus)? ReceivedQuery { get; private set; }

    /// <summary>进入互认配置查询端口的次数。</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus)
    {
      CallCount++;
      ReceivedQuery = (organizationCode, standardProjectCode, configurationStatus);
      return Task.FromResult(Items);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) =>
      throw new NotSupportedException("本测试只覆盖互认项目配置列表查询。");
  }
}
