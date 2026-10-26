using System.Net;
using System.Text;
using System.Text.Json;
using Dy.Apron.Http.Client;
using Dy.Apron.Http.Common;
using Dy.Apron.Http.Common.ApiDescriptor;
using Dy.Core.Aspect.DynamicProxy;
using Dy.IAM.Apron;
using Dy.IAM.Sdk.AspNetCore;
using Dy.MedicalRecognition.Application.OrganizationScope;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// W1 组织业务编码读取点用例：代理路由、请求体、响应反序列化与三层业务编码填充。
/// </summary>
/// <remarks>
/// 客户端按产品装配方式注册（<c>AddIamManagementClient</c> 生成动态代理），只把底层传输层换成
/// 自定义 <see cref="HttpMessageHandler"/> 替身，因此断言的 URL、方法、请求体都来自产品代理路径。
/// 不连接真实 IAM 与数据库。
/// </remarks>
public sealed class W1OrganizationBusinessCodeTests
{
  /// <summary>测试租户标识。</summary>
  private static readonly Guid TenantId = Guid.Parse("9ae3125b-6d1d-4689-a4fc-2e21adf75acd");

  /// <summary>医共体组织实例标识。</summary>
  private static readonly Guid CommunityId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

  /// <summary>医院组织实例标识。</summary>
  private static readonly Guid HospitalId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

  /// <summary>院区组织实例标识。</summary>
  private static readonly Guid BranchId = Guid.Parse("a0000000-0000-0000-0000-000000000003");

  /// <summary>指定方法在代理里的路由与参数形态：URL、POST、单一 JSON 请求体。</summary>
  [Fact]
  public void Proxy_route_maps_the_query_method_to_the_iam_organization_endpoint()
  {
    ActionApiDescriptor action = ControllerApiDescriptor<IOrganizationBusinessCodeQueryClient>.FindAction(
      typeof(IOrganizationBusinessCodeQueryClient).GetMethod(nameof(IOrganizationBusinessCodeQueryClient.QueryOrganizationByTenantIdAsync))!);

    Assert.Equal("Api/Organization/QueryOrganizationByTenantId", action.Url);
    Assert.Equal("POST", action.HttpMethod);
    Assert.Empty(action.QueryParameters);
    Assert.Empty(action.FormParameters);
    Assert.Equal("request", Assert.Single(action.BodyParameters).Name);
  }

  /// <summary>读取点发出的是一次 POST 到组织树接口，请求体只含租户标识且为 camelCase。</summary>
  [Fact]
  public async Task Reader_posts_only_the_tenant_id_to_the_organization_tree_endpoint()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationBusinessCodeReader reader = BuildReader(handler);

    await reader.ReadAsync(TenantId);

    RecordedRequest request = Assert.Single(handler.Requests);
    Assert.Equal(HttpMethod.Post, request.Method);
    Assert.Equal("https://iam.test/Api/Organization/QueryOrganizationByTenantId", request.Url);
    Assert.Equal(TenantId, request.TenantId());

    // 框架默认序列化选项启用了引用保留，请求体会带 "$id" 元数据；业务字段仍只有租户标识。
    using JsonDocument body = JsonDocument.Parse(request.Body);
    Assert.Equal(
      ["tenantId"],
      body.RootElement.EnumerateObject().Select(property => property.Name).Where(name => name != "$id"));
  }

  /// <summary>节点带业务编码时，响应里的 <c>code</c> 落到节点业务编码字段。</summary>
  [Fact]
  public async Task Reader_reads_the_business_code_of_each_node()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationBusinessCodeReader reader = BuildReader(handler);

    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> nodes = await reader.ReadAsync(TenantId);

    Assert.Equal(["0101", "3301"], nodes.Values.Select(node => node.Code).Order());
    Assert.Equal(CommunityId, nodes[CommunityId].Id);
    Assert.Null(nodes[CommunityId].ParentOrganizationId);
    Assert.Equal(CommunityId, nodes[HospitalId].ParentOrganizationId);
    Assert.Equal("示范医院", nodes[HospitalId].Name);
    Assert.True(nodes[HospitalId].IsEnabled);
  }

  /// <summary>节点业务编码为空时编码为空值，不用标识或名称替代，节点仍然返回。</summary>
  [Fact]
  public async Task Reader_keeps_a_node_whose_business_code_is_null()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson(null, "3301"));
    IamOrganizationBusinessCodeReader reader = BuildReader(handler);

    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> nodes = await reader.ReadAsync(TenantId);

    Assert.True(nodes.ContainsKey(CommunityId));
    Assert.Null(nodes[CommunityId].Code);
    Assert.Equal("3301", nodes[HospitalId].Code);
  }

  /// <summary>三层齐全时把各层业务编码填到组织范围上，且整棵树只读一次。</summary>
  [Fact]
  public async Task Scope_fills_all_three_levels_when_codes_are_present()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationScopeResolver resolver = BuildResolver(handler);

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal((CommunityId, "示范医共体", "0101"), (scope.OrganizationId, scope.OrganizationName, scope.OrganizationCode));
    Assert.Equal((HospitalId, "示范医院", "3301"), (scope.HospitalId, scope.HospitalName, scope.HospitalCode));
    Assert.Equal((BranchId, "示范院区", null), (scope.BranchId, scope.BranchName, scope.BranchCode));
    Assert.Single(handler.Requests);
    Assert.Equal(TenantId, handler.Requests[0].TenantId());
  }

  /// <summary>缺失层级没有实例标识时，该层名称与业务编码都为空。</summary>
  [Fact]
  public async Task Scope_leaves_missing_level_name_and_code_empty()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationScopeResolver resolver = BuildResolver(handler, identityOrganizationId: HospitalId);

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal((HospitalId, "示范医院", "3301"), (scope.HospitalId, scope.HospitalName, scope.HospitalCode));
    Assert.Null(scope.BranchId);
    Assert.Null(scope.BranchName);
    Assert.Null(scope.BranchCode);
  }

  /// <summary>未装配编码读取点时三层编码为空，组织范围的标识与名称不受影响，也不发起编码请求。</summary>
  [Fact]
  public async Task Scope_leaves_codes_empty_when_no_code_reader_is_configured()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationScopeResolver resolver = new(
      new IamOrganizationDirectoryReader(FakeChain.Default),
      new FakeCurrentIamContext(BranchId));

    IamOrganizationScope scope = await resolver.ReadCurrentScopeAsync();

    Assert.Equal((CommunityId, "示范医共体", null), (scope.OrganizationId, scope.OrganizationName, scope.OrganizationCode));
    Assert.Equal((HospitalId, "示范医院", null), (scope.HospitalId, scope.HospitalName, scope.HospitalCode));
    Assert.Equal((BranchId, "示范院区", null), (scope.BranchId, scope.BranchName, scope.BranchCode));
    Assert.Empty(handler.Requests);
  }

  /// <summary>平台入口提交的三层目标同样补齐业务编码，整批共用一次读取。</summary>
  [Fact]
  public async Task Submitted_scopes_also_carry_business_codes()
  {
    RecordingHandler handler = RecordingHandler.Responding(TreeJson("0101", "3301"));
    IamOrganizationScopeResolver resolver = BuildResolver(handler);

    IReadOnlyList<IamOrganizationScope> scopes = await resolver.ResolveSubmittedScopesAsync(
      [new IamOrganizationScopeTarget(CommunityId, HospitalId, BranchId)],
      "missing organization", "missing hospital", "missing branch");

    IamOrganizationScope scope = Assert.Single(scopes);
    Assert.Equal(("0101", "3301", null), (scope.OrganizationCode, scope.HospitalCode, scope.BranchCode));
    Assert.Single(handler.Requests);
  }

  /// <summary>
  /// 模块里两个 IAM 管理客户端可同时注册：新增编码读取点不改变既有的组织管理客户端装配。
  /// </summary>
  [Fact]
  public void Module_registration_accepts_both_iam_management_clients()
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddMemoryCache();
    services.AddHttpClient();
    services.AddSingleton(new HttpServiceOptions());
    services.AddSingleton(new IamOptions { Authority = "https://iam.test", ApplicationCode = "medical-recognition" });
    services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
    services.AddSingleton<IIamManagementAuthentication>(new PassthroughManagementAuthentication());
    services.AddSingleton<LocalCache>();
    services.AddTransient(typeof(DynamicHttpProxyInterceptorClientProxy<>));
    services.AddTransient(typeof(AsyncInterceptor<>));

    services.AddIamManagementClient<Dy.IAM.Application.Contracts.OrganizationAggregate.IOrganizationAppService>();
    services.AddIamManagementClient<IOrganizationBusinessCodeQueryClient>();

    using ServiceProvider provider = services.BuildServiceProvider();
    Assert.NotNull(provider.GetRequiredService<IOrganizationBusinessCodeQueryClient>());
    Assert.NotNull(provider.GetRequiredService<Dy.IAM.Application.Contracts.OrganizationAggregate.IOrganizationAppService>());
  }

  /// <summary>构造读取点：客户端按产品装配方式由 IAM 管理接口代理生成，只替换传输层。</summary>
  /// <param name="handler">传输层替身。</param>
  /// <returns>组织业务编码读取点。</returns>
  private static IamOrganizationBusinessCodeReader BuildReader(RecordingHandler handler) =>
    new(BuildQueryClient(handler));

  /// <summary>构造解析点：目录替身给出三层链，编码读取点走真实代理与传输层替身。</summary>
  /// <param name="handler">传输层替身。</param>
  /// <param name="identityOrganizationId">当前身份所在组织；缺省为院区。</param>
  /// <returns>组织范围解析点。</returns>
  private static IamOrganizationScopeResolver BuildResolver(RecordingHandler handler, Guid? identityOrganizationId = null) =>
    new(
      new IamOrganizationDirectoryReader(FakeChain.Default),
      new FakeCurrentIamContext(identityOrganizationId ?? BranchId),
      BuildReader(handler));

  /// <summary>
  /// 按产品装配路径建立查询客户端：注册 IAM 管理接口客户端，再把该命名客户端的底层传输层换成替身。
  /// </summary>
  /// <param name="handler">传输层替身。</param>
  /// <returns>由框架动态代理实现的查询客户端。</returns>
  private static IOrganizationBusinessCodeQueryClient BuildQueryClient(RecordingHandler handler)
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddMemoryCache();
    services.AddHttpClient();
    services.AddSingleton(new HttpServiceOptions());
    services.AddSingleton(new IamOptions { Authority = "https://iam.test", ApplicationCode = "medical-recognition" });
    services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
    services.AddSingleton<IIamManagementAuthentication>(new PassthroughManagementAuthentication());
    services.AddSingleton<LocalCache>();
    services.AddTransient(typeof(DynamicHttpProxyInterceptorClientProxy<>));
    services.AddTransient(typeof(AsyncInterceptor<>));
    services.AddIamManagementClient<IOrganizationBusinessCodeQueryClient>();

    // 命名客户端的传输层换成本地替身：不连真实 IAM，其余代理与消息处理链路保持产品实现。
    services.AddHttpClient(typeof(IOrganizationBusinessCodeQueryClient).FullName!)
      .ConfigurePrimaryHttpMessageHandler(() => handler);

    return services.BuildServiceProvider().GetRequiredService<IOrganizationBusinessCodeQueryClient>();
  }

  /// <summary>构造 IAM 组织树响应 JSON：两个节点，组织业务编码可指定为空。</summary>
  /// <param name="communityCode">医共体节点业务编码；为 <see langword="null"/> 时输出 JSON null。</param>
  /// <param name="hospitalCode">医院节点业务编码。</param>
  /// <returns>响应 JSON 文本。</returns>
  private static string TreeJson(string? communityCode, string? hospitalCode) => JsonSerializer.Serialize(new
  {
    tenantId = TenantId,
    tenantName = "示范租户",
    tenantIsEnabled = true,
    nodes = new object[]
    {
      new { code = communityCode, id = CommunityId, parentOrganizationId = (Guid?)null, name = "示范医共体", sortOrder = 0, isEnabled = true, ancestorChainEnabled = true, firstDisabledAncestorId = (Guid?)null },
      new { code = hospitalCode, id = HospitalId, parentOrganizationId = (Guid?)CommunityId, name = "示范医院", sortOrder = 1, isEnabled = true, ancestorChainEnabled = true, firstDisabledAncestorId = (Guid?)null }
    }
  });

  /// <summary>记录代理真实发出的请求并返回固定响应的传输层替身。</summary>
  private sealed class RecordingHandler : HttpMessageHandler
  {
    /// <summary>固定响应 JSON。</summary>
    private readonly string responseJson;

    /// <summary>已截获的请求。</summary>
    internal List<RecordedRequest> Requests { get; } = [];

    /// <summary>以固定响应初始化替身。</summary>
    /// <param name="responseJson">响应 JSON 文本。</param>
    private RecordingHandler(string responseJson) => this.responseJson = responseJson;

    /// <summary>构造返回固定 JSON 的替身。</summary>
    /// <param name="responseJson">响应 JSON 文本。</param>
    /// <returns>传输层替身。</returns>
    internal static RecordingHandler Responding(string responseJson) => new(responseJson);

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
      Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.ToString(), body));
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
      };
    }
  }

  /// <summary>已截获的一次请求。</summary>
  /// <param name="Method">请求方法。</param>
  /// <param name="Url">完整请求地址。</param>
  /// <param name="Body">请求体文本；没有请求体时为空串。</param>
  private sealed record RecordedRequest(HttpMethod Method, string Url, string Body)
  {
    /// <summary>取请求体里的租户标识。</summary>
    /// <returns>请求体中的租户标识。</returns>
    internal Guid TenantId()
    {
      using JsonDocument body = JsonDocument.Parse(Body);
      return body.RootElement.GetProperty("tenantId").GetGuid();
    }
  }

  /// <summary>管理接口透传认证替身：只写入固定凭据头，不做真实凭据解析。</summary>
  private sealed class PassthroughManagementAuthentication : IIamManagementAuthentication
  {
    /// <inheritdoc/>
    public void Authorize(HttpRequestMessage request) =>
      request.Headers.TryAddWithoutValidation("Authorization", "Bearer test-token");
  }

  /// <summary>默认三层组织链的目录替身：固定标识与名称，供范围归类使用。</summary>
  private sealed class FakeChain : Dy.IAM.Sdk.IIamDirectoryClient
  {
    /// <summary>默认三层链。</summary>
    internal static FakeChain Default { get; } = new();

    /// <summary>标识到组织详情的映射。</summary>
    private readonly Dictionary<Guid, Dy.IAM.Sdk.IamDirectoryOrganization> organizations = new()
    {
      [CommunityId] = Organization(CommunityId, "示范医共体", null, "CountyMedicalCommunity", [CommunityId]),
      [HospitalId] = Organization(HospitalId, "示范医院", CommunityId, "Hospital", [CommunityId, HospitalId]),
      [BranchId] = Organization(BranchId, "示范院区", HospitalId, "Branch", [CommunityId, HospitalId, BranchId])
    };

    /// <inheritdoc/>
    public Task<Dy.IAM.Sdk.IamDirectoryReadResult> BatchReadDirectoryAsync(
      Dy.IAM.Sdk.IamDirectoryReadRequest request, CancellationToken cancellationToken)
    {
      Dy.IAM.Sdk.IamDirectoryReadItem[] items =
      [
        .. request.Ids.Select(id => organizations.TryGetValue(id, out Dy.IAM.Sdk.IamDirectoryOrganization? organization)
          ? new Dy.IAM.Sdk.IamDirectoryReadItem(id, true, new Dy.IAM.Sdk.IamDirectoryData { Organization = organization })
          : new Dy.IAM.Sdk.IamDirectoryReadItem(id, false, null!))
      ];
      return Task.FromResult(new Dy.IAM.Sdk.IamDirectoryReadResult(items, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc/>
    public Task<Dy.IAM.Sdk.IamEmploymentValidationResult> BatchValidateEmploymentsAsync(
      Dy.IAM.Sdk.IamEmploymentValidationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<Dy.IAM.Sdk.IamOrganizationDirectoryResult> QueryOrganizationDirectoryAsync(
      Dy.IAM.Sdk.IamOrganizationDirectoryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

    /// <summary>构造一个组织详情节点。</summary>
    /// <param name="id">组织标识。</param>
    /// <param name="name">组织名称。</param>
    /// <param name="parentId">父组织标识。</param>
    /// <param name="typeCode">组织类型编码。</param>
    /// <param name="path">从根到自身的组织标识序列。</param>
    /// <returns>组织详情。</returns>
    private static Dy.IAM.Sdk.IamDirectoryOrganization Organization(
      Guid id, string name, Guid? parentId, string typeCode, Guid[] path) =>
      new(id, name, parentId, true, [.. path.Select(item => new Dy.IAM.Sdk.IamDirectoryPathItem(item, $"组织-{item:N}"))])
      {
        OrganizationType = new Dy.IAM.Sdk.IamDirectoryOrganizationType(Guid.NewGuid(), typeCode, typeCode, true)
      };
  }

  /// <summary>IAM 当前上下文替身：认证通过，当前身份落在指定组织。</summary>
  private sealed class FakeCurrentIamContext : Dy.IAM.Sdk.ICurrentIamContext
  {
    /// <summary>当前身份。</summary>
    private readonly Dy.IAM.Sdk.IamIdentityContext identity;

    /// <summary>以当前身份所在组织构造上下文。</summary>
    /// <param name="organizationId">当前工作身份所在组织。</param>
    internal FakeCurrentIamContext(Guid organizationId)
    {
      identity = new Dy.IAM.Sdk.IamIdentityContext(
        Guid.Parse("d0000000-0000-0000-0000-000000000001"),
        Guid.Parse("d0000000-0000-0000-0000-000000000002"),
        TenantId,
        organizationId,
        null,
        "medical-recognition",
        Guid.Parse("d0000000-0000-0000-0000-000000000003"),
        1,
        1,
        1);
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => true;

    /// <inheritdoc/>
    public Dy.IAM.Sdk.IamIdentityContext? Current => identity;

    /// <inheritdoc/>
    public Dy.IAM.Sdk.IamIdentityContext RequireCurrent() => identity;
  }
}
