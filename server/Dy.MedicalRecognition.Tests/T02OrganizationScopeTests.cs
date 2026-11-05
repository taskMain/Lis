using System.Reflection;
using Dy.IAM.Sdk;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;
using Dy.MedicalRecognition.Application.OrganizationScope;
using Dy.MedicalRecognition.Application.Queries.OrganizationScope;
using Xunit;
// 本工程测试目录另有同名的外部组织服务契约，IAM 组织管理契约按别名引用，避免两者混用。
using IamOrganizationAppService = Dy.IAM.Application.Contracts.OrganizationAggregate.IOrganizationAppService;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// T02 票 04 的组织范围解析与选择列表行为用例（矩阵 V5～V9、V16 的应用面）。
/// </summary>
/// <remarks>
/// 用可控的 IAM 目录替身构造组织链与组织类型，验证三层归类、缺失与停用、同类型多祖先拒绝、工作身份要求、
/// 平台入口父子归属校验、批量读取次数与选择列表组装。真实 IAM 目录的取证结果记录在阶段测试报告。
/// 范围返回值与选择器选项都按「实例标识 + 名称 + 组织业务编码」三件同时交付：
/// 编码取自组织业务编码读取结果，节点未配置编码时为空，用例据同一批夹具同时断言三者，避免编码被标识或名称替代。
/// </remarks>
public sealed class T02OrganizationScopeTests
{
  /// <summary>医共体组织实例标识。</summary>
  private static readonly Guid CommunityId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

  /// <summary>第一家医院的组织实例标识。</summary>
  private static readonly Guid HospitalAId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

  /// <summary>第二家医院的组织实例标识。</summary>
  private static readonly Guid HospitalBId = Guid.Parse("a0000000-0000-0000-0000-000000000003");

  /// <summary>第一家医院下院区的组织实例标识。</summary>
  private static readonly Guid BranchAId = Guid.Parse("a0000000-0000-0000-0000-000000000004");

  /// <summary>第二家医院下院区的组织实例标识。</summary>
  private static readonly Guid BranchBId = Guid.Parse("a0000000-0000-0000-0000-000000000005");

  /// <summary>未配置任何类型的组织实例标识，用于缺失类别场景。</summary>
  private static readonly Guid UntypedOrganizationId = Guid.Parse("a0000000-0000-0000-0000-000000000006");

  /// <summary>另一个医共体的组织实例标识，用于跨组织组合场景。</summary>
  private static readonly Guid OtherCommunityId = Guid.Parse("a0000000-0000-0000-0000-000000000007");

  /// <summary>另一个医共体下医院的组织实例标识，用于跨组织组合场景。</summary>
  private static readonly Guid OtherHospitalId = Guid.Parse("a0000000-0000-0000-0000-000000000008");

  /// <summary>组织类型已停用的组织实例标识，用于类型停用场景。</summary>
  private static readonly Guid DisabledTypeOrganizationId = Guid.Parse("a0000000-0000-0000-0000-00000000000b");

  /// <summary>测试租户标识。</summary>
  private static readonly Guid TenantId = Guid.Parse("b0000000-0000-0000-0000-000000000001");

  /// <summary>医共体层缺失时对外抛出的业务拒绝文案。</summary>
  private const string MissingOrganizationMessage = "业务拒绝：当前身份的医共体不存在或已停用。";

  /// <summary>医院层缺失时对外抛出的业务拒绝文案。</summary>
  private const string MissingHospitalMessage = "业务拒绝：医院不存在、已停用或不属于所选医共体。";

  /// <summary>院区层缺失时对外抛出的业务拒绝文案。</summary>
  private const string MissingBranchMessage = "业务拒绝：院区不存在、已停用或不属于所选医院。";

  /// <summary>
  /// V5：当前身份在院区节点时，当前节点与祖先按组织类型归类出医共体、医院、院区三层。
  /// </summary>
  [Fact]
  public async Task Current_scope_classifies_the_identity_ancestry_into_three_levels()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal((CommunityId, "县医共体"), (scope.OrganizationId, scope.OrganizationName));
    Assert.Equal((HospitalAId, "测试医院一"), (scope.HospitalId, scope.HospitalName));
    Assert.Equal((BranchAId, "测试院区1-1"), (scope.BranchId, scope.BranchName));

    // 读取次数：当前节点一次，祖先一次；不随层级深度重复读取。
    Assert.Equal(2, directory.Requests.Count);
    Assert.All(directory.Requests, ids => Assert.InRange(ids.Length, 1, 100));
  }

  /// <summary>节点有多个类型时按集合包含关系归类，不依赖数组顺序。</summary>
  [Fact]
  public async Task Current_scope_classifies_multi_typed_nodes_independently_of_type_order()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamDirectoryOrganization branch = directory.Organizations[BranchAId];
    directory.Organizations[BranchAId] = branch with
    {
      OrganizationTypes =
      [
        new IamDirectoryOrganizationType(Guid.NewGuid(), "Dept", "科室", true),
        .. branch.OrganizationTypes
      ]
    };
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal(BranchAId, scope.BranchId);
  }

  /// <summary>
  /// V5：当前身份在医院节点时，当前节点与祖先归类出医共体与医院，院区为空。
  /// </summary>
  [Fact]
  public async Task Current_scope_classifies_a_hospital_identity_without_a_branch()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(HospitalAId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal((CommunityId, "县医共体"), (scope.OrganizationId, scope.OrganizationName));
    Assert.Equal((HospitalAId, "测试医院一"), (scope.HospitalId, scope.HospitalName));
    Assert.Null(scope.BranchId);
  }

  /// <summary>
  /// V5：当前身份所在链没有配置组织类型时，三层都为空，不用名称或层级深度推断。
  /// </summary>
  [Fact]
  public async Task Current_scope_returns_empty_levels_when_the_chain_has_no_types()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(UntypedOrganizationId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal(IamOrganizationScope.Empty, scope);
  }

  /// <summary>
  /// V8：停用节点按不存在处理，不进入任何层级。
  /// </summary>
  [Fact]
  public async Task Current_scope_treats_disabled_nodes_as_absent()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    directory.Organizations[HospitalAId] = Organization(HospitalAId, "测试医院一", CommunityId, IamOrganizationTypeCodes.Hospital,
      isEnabled: false, Path(CommunityId, HospitalAId));
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal(CommunityId, scope.OrganizationId);
    Assert.Null(scope.HospitalId);
    Assert.Equal(BranchAId, scope.BranchId);
  }

  /// <summary>
  /// V8：同一类型在祖先链上出现多个节点时拒绝，不任取一条。
  /// </summary>
  [Fact]
  public async Task Current_scope_rejects_duplicate_same_type_ancestors()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    Guid secondHospitalId = Guid.Parse("a0000000-0000-0000-0000-000000000009");
    directory.Organizations[secondHospitalId] = Organization(secondHospitalId, "第二家医院", CommunityId, IamOrganizationTypeCodes.Hospital,
      path: Path(CommunityId, secondHospitalId));
    directory.Organizations[BranchAId] = Organization(BranchAId, "测试院区1-1", secondHospitalId, IamOrganizationTypeCodes.Branch,
      path: Path(CommunityId, HospitalAId, secondHospitalId, BranchAId));
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ReadCurrentScopeAsync());

    Assert.Contains("多个同类型组织", error.Message, StringComparison.Ordinal);
  }

  /// <summary>
  /// V9：未选择工作身份时抛工作身份必需异常，由 IAM 错误处理呈现 409。
  /// </summary>
  [Fact]
  public async Task Current_scope_requires_a_selected_work_identity()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(null));

    await Assert.ThrowsAsync<IamWorkIdentityRequiredException>(() => resolver.ReadCurrentScopeAsync());
    Assert.Empty(directory.Requests);
  }

  /// <summary>
  /// V6、V16：平台入口按请求提交的组织实例校验父子归属，接受与当前身份不同的合法范围。
  /// </summary>
  [Fact]
  public async Task Submitted_scopes_accept_a_valid_path_of_another_hospital()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    IReadOnlyList<IamOrganizationScope> scopes = await resolver.ResolveSubmittedScopesAsync(
      [new IamOrganizationScopeTarget(CommunityId, HospitalBId, BranchBId)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage);

    IamOrganizationScope scope = Assert.Single(scopes);
    Assert.Equal((CommunityId, HospitalBId, BranchBId), (scope.OrganizationId, scope.HospitalId, scope.BranchId));
    Assert.Equal("测试医院二", scope.HospitalName);
  }

  /// <summary>
  /// V6：医院不属于请求提交的医共体时按医院层文案拒绝。
  /// </summary>
  [Fact]
  public async Task Submitted_scopes_reject_a_hospital_of_another_organization()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      resolver.ResolveSubmittedScopesAsync(
        [new IamOrganizationScopeTarget(CommunityId, OtherHospitalId, BranchBId)],
        MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingHospitalMessage, error.Message);
  }

  /// <summary>
  /// V6：院区不属于请求提交的医院时按院区层文案拒绝。
  /// </summary>
  [Fact]
  public async Task Submitted_scopes_reject_a_branch_of_another_hospital()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      resolver.ResolveSubmittedScopesAsync(
        [new IamOrganizationScopeTarget(CommunityId, HospitalAId, BranchBId)],
        MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingBranchMessage, error.Message);
  }

  /// <summary>
  /// V8：请求提交的目标范围含停用节点时按该层文案拒绝。
  /// </summary>
  [Fact]
  public async Task Submitted_scopes_reject_disabled_nodes()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    directory.Organizations[BranchBId] = Organization(BranchBId, "测试院区2-1", HospitalBId, IamOrganizationTypeCodes.Branch,
      isEnabled: false, Path(CommunityId, HospitalBId, BranchBId));
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      resolver.ResolveSubmittedScopesAsync(
        [new IamOrganizationScopeTarget(CommunityId, HospitalBId, BranchBId)],
        MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingBranchMessage, error.Message);
  }

  /// <summary>
  /// V7：一次目录读取覆盖整批目标，读取次数不随目标条数增长。
  /// </summary>
  [Fact]
  public async Task Submitted_scopes_read_the_directory_once_for_many_targets()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationScopeResolver resolver = BuildResolver(directory, IdentityAt(BranchAId));
    IamOrganizationScopeTarget[] targets =
    [
      .. Enumerable.Range(0, 20).Select(_ => new IamOrganizationScopeTarget(CommunityId, HospitalAId, BranchAId))
    ];

    IReadOnlyList<IamOrganizationScope> scopes = await resolver.ResolveSubmittedScopesAsync(
      targets, MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage);

    Assert.Equal(20, scopes.Count);
    Assert.Single(directory.Requests);
    Assert.Equal(3, directory.Requests[0].Distinct().Count());
  }

  /// <summary>
  /// V7：目录批量读取按 100 个标识切分，超出上限时增加批次而不放大单批。
  /// </summary>
  [Fact]
  public async Task Directory_reader_splits_batches_at_the_documented_limit()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    IamOrganizationDirectoryReader reader = new(directory);
    Guid[] ids = [.. Enumerable.Range(0, 150).Select(index => Guid.Parse($"c0000000-0000-0000-0000-{index:D12}"))];

    await reader.ReadAsync(ids);

    Assert.Equal(2, directory.Requests.Count);
    Assert.Equal([100, 50], directory.Requests.Select(request => request.Length));
  }

  /// <summary>
  /// V5、C4：选择列表按父子关系装配三级选项，未配置类型与停用节点不进入选项并单独计数；
  /// 每个候选节点同时给出实例标识、名称与组织业务编码。
  /// </summary>
  [Fact]
  public void Options_group_nodes_by_parent_and_report_unconfigured_types()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    Guid deptId = Guid.Parse("a0000000-0000-0000-0000-00000000000a");
    directory.Organizations[deptId] = Organization(deptId, "内科", HospitalAId, "Dept", path: Path(CommunityId, HospitalAId, deptId));
    OrganizationNodeResponse[] nodes =
    [
      Node(CommunityId, "县医共体", null),
      Node(HospitalAId, "测试医院一", CommunityId),
      Node(BranchAId, "测试院区1-1", HospitalAId),
      Node(HospitalBId, "测试医院二", CommunityId),
      Node(BranchBId, "测试院区2-1", HospitalBId),
      Node(UntypedOrganizationId, "普通开发", CommunityId),
      Node(deptId, "内科", HospitalAId),
      Node(OtherHospitalId, "无父级医院", Guid.NewGuid())
    ];

    OrganizationScopeOptionsReadModel options = OrganizationScopeOptionsBuilder.Build(
      nodes,
      directory.Organizations,
      BusinessCodes(
        (CommunityId, "C01"),
        (HospitalAId, "H01"),
        (BranchAId, "B01"),
        (HospitalBId, "H02"),
        (BranchBId, "B02"),
        (deptId, "DEPT"),
        (OtherHospitalId, "H03")));

    OrganizationScopeOrganizationReadModel organization = Assert.Single(options.Organizations);
    Assert.Equal((CommunityId, "县医共体", "C01"), (organization.OrganizationId, organization.OrganizationName, organization.OrganizationCode));
    Assert.Equal([HospitalAId, HospitalBId], organization.Hospitals.Select(hospital => hospital.OrganizationId));
    Assert.Equal(["H01", "H02"], organization.Hospitals.Select(hospital => hospital.OrganizationCode));
    Assert.Equal([BranchAId], organization.Hospitals[0].Branches.Select(branch => branch.OrganizationId));
    Assert.Equal(["B01"], organization.Hospitals[0].Branches.Select(branch => branch.OrganizationCode));
    Assert.Equal([BranchBId], organization.Hospitals[1].Branches.Select(branch => branch.OrganizationId));
    Assert.Equal(["B02"], organization.Hospitals[1].Branches.Select(branch => branch.OrganizationCode));
    Assert.Equal(1, options.UnconfiguredOrganizationCount);
  }

  /// <summary>
  /// V5：停用节点不进入选择列表，也不计入未配置类型数量。
  /// </summary>
  [Fact]
  public void Options_skip_disabled_nodes()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    OrganizationNodeResponse[] nodes =
    [
      Node(CommunityId, "县医共体", null),
      Node(HospitalAId, "测试医院一", CommunityId, isEnabled: false),
      Node(BranchAId, "测试院区1-1", HospitalAId, isEnabled: false)
    ];

    OrganizationScopeOptionsReadModel options = OrganizationScopeOptionsBuilder.Build(
      nodes, directory.Organizations, BusinessCodes((CommunityId, "C01")));

    OrganizationScopeOrganizationReadModel organization = Assert.Single(options.Organizations);
    Assert.Equal("C01", organization.OrganizationCode);
    Assert.Empty(organization.Hospitals);
    Assert.Equal(0, options.UnconfiguredOrganizationCount);
  }

  /// <summary>
  /// V5：节点自身启用但其组织类型已停用时，该节点不进入选项，并计入未配置类型数量。
  /// </summary>
  [Fact]
  public void Options_skip_nodes_whose_organization_type_is_disabled()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    directory.Organizations[DisabledTypeOrganizationId] = Organization(
      DisabledTypeOrganizationId, "停用类型医院", CommunityId, IamOrganizationTypeCodes.Hospital,
      isTypeEnabled: false, path: Path(CommunityId, DisabledTypeOrganizationId));
    OrganizationNodeResponse[] nodes =
    [
      Node(CommunityId, "县医共体", null),
      Node(DisabledTypeOrganizationId, "停用类型医院", CommunityId)
    ];

    OrganizationScopeOptionsReadModel options = OrganizationScopeOptionsBuilder.Build(
      nodes, directory.Organizations, BusinessCodes());

    OrganizationScopeOrganizationReadModel organization = Assert.Single(options.Organizations);
    // 无编码来源时编码为空串：不降级为实例标识文本，也不降级为名称。
    Assert.Equal(string.Empty, organization.OrganizationCode);
    Assert.NotEqual(CommunityId.ToString(), organization.OrganizationCode);
    Assert.NotEqual(organization.OrganizationName, organization.OrganizationCode);
    Assert.Empty(organization.Hospitals);
    Assert.Equal(1, options.UnconfiguredOrganizationCount);
  }

  /// <summary>
  /// V9：未选择工作身份（租户标识为空）时选择列表抛工作身份必需异常，不读取组织树与组织详情。
  /// </summary>
  [Fact]
  public async Task Options_require_a_selected_work_identity()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    FakeOrganizationCodeQueryClient codeClient = new();
    OrganizationScopeQueryAppService service = new(
      directory,
      new FakeCurrentIamContext { Identity = IdentityWithoutTenant() },
      null!,
      codeClient);

    await Assert.ThrowsAsync<IamWorkIdentityRequiredException>(() => service.QueryOrganizationScopeOptionsAsync());
    Assert.Empty(directory.Requests);
    Assert.Equal(0, codeClient.QueryCount);
  }

  /// <summary>
  /// 当前身份的三级范围同时返回实例标识、名称与组织业务编码，编码按组织实例标识从组织业务编码读取结果补齐。
  /// </summary>
  [Fact]
  public async Task Current_scope_query_returns_instance_ids_names_and_business_codes()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    FakeOrganizationCodeQueryClient codeClient = new()
    {
      Codes = { [CommunityId] = "C01", [HospitalAId] = "H01", [BranchAId] = "B01" }
    };
    OrganizationScopeQueryAppService service = new(
      directory,
      new FakeCurrentIamContext { Identity = IdentityAt(BranchAId) },
      null!,
      codeClient);

    CurrentOrganizationScopeReadModel scope = await service.QueryCurrentOrganizationScopeAsync();

    Assert.Equal(
      (CommunityId, "县医共体", "C01"),
      (scope.OrganizationId, scope.OrganizationName, scope.OrganizationCode));
    Assert.Equal(
      (HospitalAId, "测试医院一", "H01"),
      (scope.HospitalId, scope.HospitalName, scope.HospitalCode));
    Assert.Equal(
      (BranchAId, "测试院区1-1", "B01"),
      (scope.BranchId, scope.BranchName, scope.BranchCode));
    // 编码按租户一次读取：读取次数只随租户增长，不随层级数量增长。
    Assert.Equal(1, codeClient.QueryCount);
  }

  /// <summary>
  /// 组织未配置业务编码时该层编码为空，不用实例标识文本或名称替代，同层的实例标识与名称照常返回。
  /// </summary>
  [Fact]
  public async Task Current_scope_query_leaves_a_level_code_empty_when_the_node_has_no_code()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    FakeOrganizationCodeQueryClient codeClient = new()
    {
      Codes = { [CommunityId] = "C01", [BranchAId] = "B01" }
    };
    OrganizationScopeQueryAppService service = new(
      directory,
      new FakeCurrentIamContext { Identity = IdentityAt(BranchAId) },
      null!,
      codeClient);

    CurrentOrganizationScopeReadModel scope = await service.QueryCurrentOrganizationScopeAsync();

    Assert.Equal((HospitalAId, "测试医院一"), (scope.HospitalId, scope.HospitalName));
    Assert.Null(scope.HospitalCode);
    Assert.Equal("B01", scope.BranchCode);
  }

  /// <summary>
  /// 选择器选项的每个候选节点同时返回实例标识、名称与组织业务编码，编码只按租户读取一次。
  /// </summary>
  [Fact]
  public async Task Options_query_returns_business_codes_for_every_candidate_node()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    FakeOrganizationCodeQueryClient codeClient = new()
    {
      Codes =
      {
        [CommunityId] = "C01",
        [HospitalAId] = "H01",
        [BranchAId] = "B01",
        [HospitalBId] = "H02",
        [BranchBId] = "B02"
      }
    };
    OrganizationNodeResponse[] nodes =
    [
      Node(CommunityId, "县医共体", null),
      Node(HospitalAId, "测试医院一", CommunityId),
      Node(BranchAId, "测试院区1-1", HospitalAId),
      Node(HospitalBId, "测试医院二", CommunityId),
      Node(BranchBId, "测试院区2-1", HospitalBId)
    ];
    IamOrganizationAppService treeService = FakeOrganizationTreeProxy.Create(new OrganizationTreeResponse
    {
      TenantId = TenantId,
      TenantName = "演示租户",
      TenantIsEnabled = true,
      Nodes = nodes
    });
    OrganizationScopeQueryAppService service = new(
      directory,
      new FakeCurrentIamContext { Identity = IdentityAt(BranchAId) },
      treeService,
      codeClient);

    OrganizationScopeOptionsReadModel options = await service.QueryOrganizationScopeOptionsAsync();

    OrganizationScopeOrganizationReadModel organization = Assert.Single(options.Organizations);
    Assert.Equal((CommunityId, "县医共体", "C01"), (organization.OrganizationId, organization.OrganizationName, organization.OrganizationCode));
    Assert.Equal(["H01", "H02"], organization.Hospitals.Select(hospital => hospital.OrganizationCode));
    Assert.Equal(["B01"], organization.Hospitals[0].Branches.Select(branch => branch.OrganizationCode));
    Assert.Equal(["B02"], organization.Hospitals[1].Branches.Select(branch => branch.OrganizationCode));
    Assert.Equal("测试院区1-1", organization.Hospitals[0].Branches[0].OrganizationName);
    Assert.Equal(BranchAId, organization.Hospitals[0].Branches[0].OrganizationId);
    // 编码按租户一次读取：读取次数不随节点数量增长。
    Assert.Equal(1, codeClient.QueryCount);
  }

  /// <summary>
  /// 选择器选项在节点未配置业务编码时给出空串，不用实例标识文本替代。
  /// </summary>
  [Fact]
  public void Options_return_an_empty_business_code_when_the_node_has_none()
  {
    FakeIamDirectoryClient directory = BuildDirectory();
    OrganizationNodeResponse[] nodes =
    [
      Node(CommunityId, "县医共体", null),
      Node(HospitalAId, "测试医院一", CommunityId)
    ];

    OrganizationScopeOptionsReadModel options = OrganizationScopeOptionsBuilder.Build(
      nodes, directory.Organizations, BusinessCodes((CommunityId, "C01"), (HospitalAId, null)));

    OrganizationScopeOrganizationReadModel organization = Assert.Single(options.Organizations);
    Assert.Equal("C01", organization.OrganizationCode);
    OrganizationScopeHospitalReadModel hospital = Assert.Single(organization.Hospitals);
    Assert.Equal(string.Empty, hospital.OrganizationCode);
    Assert.NotEqual(HospitalAId.ToString(), hospital.OrganizationCode);
    Assert.NotEqual(hospital.OrganizationName, hospital.OrganizationCode);
  }

  /// <summary>
  /// 范围返回值与选择器选项的对外字段：三级/三级选项各自同时声明实例标识、名称与组织业务编码，
  /// 选项另带下级集合，当前身份的编码与名称按可空声明（缺失层级与未配置编码都为空）。
  /// </summary>
  [Fact]
  public void Scope_contracts_declare_ids_names_and_business_codes()
  {
    Assert.Equal(
      ["BranchCode", "BranchId", "BranchName", "HospitalCode", "HospitalId", "HospitalName", "OrganizationCode", "OrganizationId", "OrganizationName"],
      DeclaredPropertyNames(typeof(CurrentOrganizationScopeReadModel)));
    Assert.Equal(
      ["Hospitals", "OrganizationCode", "OrganizationId", "OrganizationName"],
      DeclaredPropertyNames(typeof(OrganizationScopeOrganizationReadModel)));
    Assert.Equal(
      ["Branches", "OrganizationCode", "OrganizationId", "OrganizationName"],
      DeclaredPropertyNames(typeof(OrganizationScopeHospitalReadModel)));
    Assert.Equal(
      ["OrganizationCode", "OrganizationId", "OrganizationName"],
      DeclaredPropertyNames(typeof(OrganizationScopeBranchReadModel)));

    // 实例标识为可空 Guid（缺失层级为空），编码与名称为字符串；业务编码不得声明为标识类型。
    Assert.Equal(typeof(Guid?), typeof(CurrentOrganizationScopeReadModel).GetProperty("OrganizationId")!.PropertyType);
    Assert.Equal(typeof(string), typeof(CurrentOrganizationScopeReadModel).GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(typeof(string), typeof(CurrentOrganizationScopeReadModel).GetProperty("HospitalCode")!.PropertyType);
    Assert.Equal(typeof(string), typeof(CurrentOrganizationScopeReadModel).GetProperty("BranchCode")!.PropertyType);
    Assert.Equal(typeof(Guid), typeof(OrganizationScopeOrganizationReadModel).GetProperty("OrganizationId")!.PropertyType);
    Assert.Equal(typeof(string), typeof(OrganizationScopeOrganizationReadModel).GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(typeof(string), typeof(OrganizationScopeHospitalReadModel).GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(typeof(string), typeof(OrganizationScopeBranchReadModel).GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(NullabilityState.Nullable, ReadNullability(typeof(CurrentOrganizationScopeReadModel), "OrganizationCode"));
    Assert.Equal(NullabilityState.NotNull, ReadNullability(typeof(OrganizationScopeOrganizationReadModel), "OrganizationCode"));
  }

  /// <summary>
  /// 读取类型声明的公开属性名，按名称排序，用于核对对外字段集合没有多出或缺少。
  /// </summary>
  /// <param name="type">待核对的类型。</param>
  /// <returns>按名称排序的属性名。</returns>
  private static string[] DeclaredPropertyNames(Type type) =>
    [.. type.GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal)];

  /// <summary>
  /// 读取属性的可空性标注状态，用于核对契约的可空声明与字段语义一致。
  /// </summary>
  /// <param name="type">属性所属类型。</param>
  /// <param name="propertyName">属性名。</param>
  /// <returns>读取侧的可空性标注状态。</returns>
  private static NullabilityState ReadNullability(Type type, string propertyName) =>
    new NullabilityInfoContext().Create(type.GetProperty(propertyName)!).ReadState;

  /// <summary>
  /// 构造标识到组织树节点的映射，供选择器选项读取组织业务编码；编码为 <see langword="null"/> 表示该节点未配置编码。
  /// </summary>
  /// <param name="nodes">组织实例标识与其业务编码。</param>
  /// <returns>按组织实例标识索引的组织树节点映射。</returns>
  private static Dictionary<Guid, OrganizationBusinessCodeTreeNode> BusinessCodes(params (Guid Id, string? Code)[] nodes) =>
    nodes.ToDictionary(
      node => node.Id,
      node => new OrganizationBusinessCodeTreeNode { Id = node.Id, Name = $"组织-{node.Id:N}", IsEnabled = true, Code = node.Code });

  /// <summary>
  /// 构造覆盖两条医院链、一个未配置类型节点与另一个医共体的目录替身。
  /// </summary>
  /// <returns>已装载组织数据的目录替身。</returns>
  private static FakeIamDirectoryClient BuildDirectory()
  {
    FakeIamDirectoryClient directory = new();
    directory.Organizations[CommunityId] = Organization(CommunityId, "县医共体", null, IamOrganizationTypeCodes.CountyMedicalCommunity,
      path: Path(CommunityId));
    directory.Organizations[OtherCommunityId] = Organization(OtherCommunityId, "其他医共体", null, IamOrganizationTypeCodes.CountyMedicalCommunity,
      path: Path(OtherCommunityId));
    directory.Organizations[HospitalAId] = Organization(HospitalAId, "测试医院一", CommunityId, IamOrganizationTypeCodes.Hospital,
      path: Path(CommunityId, HospitalAId));
    directory.Organizations[HospitalBId] = Organization(HospitalBId, "测试医院二", CommunityId, IamOrganizationTypeCodes.Hospital,
      path: Path(CommunityId, HospitalBId));
    directory.Organizations[BranchAId] = Organization(BranchAId, "测试院区1-1", HospitalAId, IamOrganizationTypeCodes.Branch,
      path: Path(CommunityId, HospitalAId, BranchAId));
    directory.Organizations[BranchBId] = Organization(BranchBId, "测试院区2-1", HospitalBId, IamOrganizationTypeCodes.Branch,
      path: Path(CommunityId, HospitalBId, BranchBId));
    directory.Organizations[UntypedOrganizationId] = Organization(UntypedOrganizationId, "普通开发", null, null,
      path: Path(UntypedOrganizationId));
    directory.Organizations[OtherHospitalId] = Organization(OtherHospitalId, "其他医院", OtherCommunityId, IamOrganizationTypeCodes.Hospital,
      path: Path(OtherCommunityId, OtherHospitalId));
    return directory;
  }

  /// <summary>
  /// 用指定目录替身与当前身份建立解析点。
  /// </summary>
  /// <param name="directory">目录替身。</param>
  /// <param name="identity">当前身份；为 <see langword="null"/> 时按未认证处理。</param>
  /// <returns>组织范围解析点。</returns>
  private static IamOrganizationScopeResolver BuildResolver(FakeIamDirectoryClient directory, IamIdentityContext? identity) =>
    new(new IamOrganizationDirectoryReader(directory), new FakeCurrentIamContext { Identity = identity });

  /// <summary>
  /// 构造当前身份：工作身份的组织取指定节点。
  /// </summary>
  /// <param name="organizationId">当前工作身份所在组织；为空表示未选择工作身份。</param>
  /// <returns>IAM 身份上下文。</returns>
  private static IamIdentityContext IdentityAt(Guid? organizationId) => new(
    Guid.Parse("d0000000-0000-0000-0000-000000000001"),
    organizationId is null ? null : Guid.Parse("d0000000-0000-0000-0000-000000000002"),
    TenantId,
    organizationId,
    null,
    "medical-recognition",
    Guid.Parse("d0000000-0000-0000-0000-000000000003"),
    1,
    1,
    1);

  /// <summary>
  /// 构造未选择工作身份的身份证实：租户标识为空，组织标识为空。
  /// </summary>
  /// <returns>IAM 身份上下文。</returns>
  private static IamIdentityContext IdentityWithoutTenant() => new(
    Guid.Parse("d0000000-0000-0000-0000-000000000001"),
    null,
    null,
    null,
    null,
    "medical-recognition",
    Guid.Parse("d0000000-0000-0000-0000-000000000003"),
    1,
    1,
    1);

  /// <summary>
  /// 构造 IAM 组织详情。
  /// </summary>
  /// <param name="id">组织标识。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="parentId">父组织标识。</param>
  /// <param name="typeCode">组织类型编码；为空表示未配置类型。</param>
  /// <param name="isEnabled">组织是否启用。</param>
  /// <param name="path">组织路径，最后一项为自身。</param>
  /// <param name="isTypeEnabled">组织类型是否启用；为 <see langword="false"/> 时构造已停用类型。</param>
  /// <returns>IAM 组织详情。</returns>
  private static IamDirectoryOrganization Organization(
    Guid id, string name, Guid? parentId, string? typeCode, bool isEnabled = true, IamDirectoryPathItem[]? path = null,
    bool isTypeEnabled = true) =>
    new(id, name, parentId, isEnabled, path ?? Path(id))
    {
      OrganizationTypes = typeCode is null
        ? []
        : [new IamDirectoryOrganizationType(Guid.NewGuid(), typeCode, typeCode, isTypeEnabled)]
    };

  /// <summary>
  /// 构造组织路径条目序列。
  /// </summary>
  /// <param name="ids">从根到自身的组织标识序列。</param>
  /// <returns>组织路径条目。</returns>
  private static IamDirectoryPathItem[] Path(params Guid[] ids) =>
    [.. ids.Select(id => new IamDirectoryPathItem(id, $"组织-{id:N}"))];

  /// <summary>
  /// 构造组织树节点。
  /// </summary>
  /// <param name="id">组织标识。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="parentId">父组织标识。</param>
  /// <param name="isEnabled">组织是否启用。</param>
  /// <returns>组织树节点。</returns>
  private static OrganizationNodeResponse Node(Guid id, string name, Guid? parentId, bool isEnabled = true) => new()
  {
    Id = id,
    Name = name,
    ParentOrganizationId = parentId,
    SortOrder = 0,
    IsEnabled = isEnabled,
    AncestorChainEnabled = isEnabled
  };

  /// <summary>
  /// IAM 共享目录客户端替身：按标识返回预置组织详情，并记录每次批量读取的标识。
  /// </summary>
  private sealed class FakeIamDirectoryClient : IIamDirectoryClient
  {
    /// <summary>标识到组织详情的预置数据。</summary>
    internal Dictionary<Guid, IamDirectoryOrganization> Organizations { get; } = [];

    /// <summary>每次批量读取请求的标识序列。</summary>
    internal List<Guid[]> Requests { get; } = [];

    /// <inheritdoc/>
    public Task<IamDirectoryReadResult> BatchReadDirectoryAsync(IamDirectoryReadRequest request, CancellationToken cancellationToken)
    {
      Requests.Add([.. request.Ids]);
      IamDirectoryReadItem[] items =
      [
        .. request.Ids.Select(id => Organizations.TryGetValue(id, out IamDirectoryOrganization? organization)
          ? new IamDirectoryReadItem(id, true, new IamDirectoryData { Organization = organization })
          : new IamDirectoryReadItem(id, false, null!))
      ];
      return Task.FromResult(new IamDirectoryReadResult(items, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc/>
    public Task<IamEmploymentValidationResult> BatchValidateEmploymentsAsync(IamEmploymentValidationRequest request, CancellationToken cancellationToken) =>
      throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IamOrganizationDirectoryResult> QueryOrganizationDirectoryAsync(IamOrganizationDirectoryRequest request, CancellationToken cancellationToken) =>
      throw new NotSupportedException();

    public Task<IamOrganizationsByCodeResult> QueryOrganizationsByCodeAsync(IamOrganizationsByCodeRequest request, CancellationToken cancellationToken) =>
      throw new NotSupportedException();
  }

  /// <summary>
  /// 组织业务编码读取替身：按用例预置的组织实例标识与业务编码返回组织树节点，并记录读取次数。
  /// </summary>
  private sealed class FakeOrganizationCodeQueryClient : IOrganizationBusinessCodeQueryClient
  {
    /// <summary>组织实例标识到业务编码的预置数据；未登记的组织按未配置业务编码处理。</summary>
    internal Dictionary<Guid, string> Codes { get; init; } = [];

    /// <summary>按租户读取组织树的累计次数。</summary>
    internal int QueryCount { get; private set; }

    /// <inheritdoc/>
    public Task<OrganizationBusinessCodeTreeResponse> QueryOrganizationByTenantIdAsync(OrganizationBusinessCodeQueryRequest request)
    {
      QueryCount++;
      OrganizationBusinessCodeTreeNode[] nodes =
      [
        .. Codes.Select(pair => new OrganizationBusinessCodeTreeNode
        {
          Id = pair.Key,
          Name = $"组织-{pair.Key:N}",
          IsEnabled = true,
          Code = pair.Value
        })
      ];
      return Task.FromResult(new OrganizationBusinessCodeTreeResponse
      {
        TenantId = request.TenantId,
        TenantName = "演示租户",
        TenantIsEnabled = true,
        Nodes = nodes
      });
    }
  }

  /// <summary>
  /// IAM 组织管理契约替身：只提供「按租户读取组织树」，其余成员被调用时抛出，避免用例借用未声明的依赖。
  /// </summary>
  /// <remarks>
  /// 契约成员数量多且与组织范围查询无关，按动态代理实现，仅覆写本工程实际调用的那一个方法；
  /// 代理实例需按本类型取回配置，因此本类型不封闭。
  /// </remarks>
  private class FakeOrganizationTreeProxy : DispatchProxy
  {
    /// <summary>按租户读取组织树的返回结果。</summary>
    internal OrganizationTreeResponse Tree { get; set; } = new();

    /// <summary>组织树读取的累计次数。</summary>
    internal int QueryCount { get; private set; }

    /// <summary>按给定组织树构造契约替身。</summary>
    /// <param name="tree">组织树读取返回结果。</param>
    /// <returns>只支持组织树读取的契约替身。</returns>
    internal static IamOrganizationAppService Create(OrganizationTreeResponse tree)
    {
      IamOrganizationAppService service = DispatchProxy.Create<IamOrganizationAppService, FakeOrganizationTreeProxy>();
      ((FakeOrganizationTreeProxy)service).Tree = tree;
      return service;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
      if (targetMethod?.Name == nameof(IamOrganizationAppService.QueryOrganizationByTenantIdAsync))
      {
        QueryCount++;
        return Task.FromResult(Tree);
      }

      throw new NotSupportedException($"用例未提供 IAM 组织管理契约成员 {targetMethod?.Name} 的替身行为。");
    }
  }

  /// <summary>
  /// IAM 当前上下文替身：按可设置的身份证实是否认证。
  /// </summary>
  private sealed class FakeCurrentIamContext : ICurrentIamContext
  {
    /// <summary>当前身份；为空表示未认证。</summary>
    internal IamIdentityContext? Identity { get; init; }

    /// <inheritdoc/>
    public bool IsAuthenticated => Identity is not null;

    /// <inheritdoc/>
    public IamIdentityContext? Current => Identity;

    /// <inheritdoc/>
    public IamIdentityContext RequireCurrent() => Identity ?? throw new IamAuthenticationException();
  }
}
