using Dy.IAM.Sdk;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;
using Dy.MedicalRecognition.Application.OrganizationScope;

namespace Dy.MedicalRecognition.Application.Queries.OrganizationScope;

/// <summary>
/// 组织范围只读查询的应用服务实现：当前身份的三级范围与平台入口的可选范围。
/// </summary>
/// <remarks>
/// 只读取 IAM 组织目录，不查询本平台数据库、不写事件、不做权限判定。
/// 当前身份的组织链解析与平台入口的目标范围校验共用同一个解析点，两处对同一组织数据给出一致结论。
/// 三级范围与选择器选项都同时给出实例标识、名称与组织业务编码：实例标识与名称取自 IAM 组织目录，
/// 业务编码按当前租户从组织业务编码读取点取得；组织未配置业务编码时该项为空，不用实例标识文本或名称替代。
/// </remarks>
public sealed class OrganizationScopeQueryAppService : ApplicationService, IOrganizationScopeQueryAppService
{
  /// <summary>
  /// 当前身份与平台入口共用的组织范围解析点。
  /// </summary>
  private readonly IamOrganizationScopeResolver scopeResolver;

  /// <summary>
  /// 组织详情批量读取点，供选择列表读取组织树节点的组织类型。
  /// </summary>
  private readonly IamOrganizationDirectoryReader directoryReader;

  /// <summary>
  /// 已验证的 IAM 当前上下文，提供租户范围。
  /// </summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>
  /// IAM 组织管理契约客户端，提供当前租户的组织树。
  /// </summary>
  private readonly IOrganizationAppService organizationAppService;

  /// <summary>
  /// 组织业务编码读取点：按当前租户读取组织树节点的业务编码，供三级范围与选择器选项同时返回编码。
  /// </summary>
  private readonly IamOrganizationBusinessCodeReader businessCodeReader;

  /// <summary>
  /// 初始化组织范围查询应用服务。
  /// </summary>
  /// <remarks>
  /// 组织业务编码读取点在构造函数内建立：它是应用层内部类型，不能出现在公开构造函数的参数上；
  /// 同一读取点既交给范围解析点补齐当前身份三级的编码，也供选择器选项按节点补齐编码。
  /// </remarks>
  /// <param name="directoryClient">IAM 共享目录客户端，提供组织详情与类型。</param>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文。</param>
  /// <param name="organizationAppService">IAM 组织管理契约客户端，提供当前租户的组织树。</param>
  /// <param name="businessCodeQueryClient">IAM 组织业务编码查询客户端，按租户读取组织树节点的业务编码。</param>
  public OrganizationScopeQueryAppService(
    IIamDirectoryClient directoryClient,
    ICurrentIamContext currentIamContext,
    IOrganizationAppService organizationAppService,
    IOrganizationBusinessCodeQueryClient businessCodeQueryClient)
  {
    directoryReader = new IamOrganizationDirectoryReader(directoryClient);
    businessCodeReader = new IamOrganizationBusinessCodeReader(businessCodeQueryClient);
    scopeResolver = new IamOrganizationScopeResolver(directoryReader, currentIamContext, businessCodeReader);
    this.currentIamContext = currentIamContext;
    this.organizationAppService = organizationAppService;
  }

  /// <inheritdoc/>
  public async Task<CurrentOrganizationScopeReadModel> QueryCurrentOrganizationScopeAsync()
  {
    IamOrganizationScope scope = await scopeResolver.ReadCurrentScopeAsync();
    return new CurrentOrganizationScopeReadModel
    {
      OrganizationId = scope.OrganizationId,
      OrganizationName = scope.OrganizationName,
      OrganizationCode = scope.OrganizationCode,
      HospitalId = scope.HospitalId,
      HospitalName = scope.HospitalName,
      HospitalCode = scope.HospitalCode,
      BranchId = scope.BranchId,
      BranchName = scope.BranchName,
      BranchCode = scope.BranchCode
    };
  }

  /// <inheritdoc/>
  public async Task<OrganizationScopeOptionsReadModel> QueryOrganizationScopeOptionsAsync()
  {
    IamIdentityContext identity = currentIamContext.RequireCurrent();
    Guid tenantId = identity.TenantId is { } selectedTenantId && selectedTenantId != Guid.Empty
      ? selectedTenantId
      : throw new IamWorkIdentityRequiredException();

    OrganizationTreeResponse tree = await organizationAppService.QueryOrganizationByTenantIdAsync(
      new QueryOrganizationByTenantIdRequest { TenantId = tenantId });

    // 组织树不返回组织类型：按节点标识批量读取详情与类型，读取次数只随节点数量增长。
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> organizations =
      await directoryReader.ReadAsync([.. tree.Nodes.Select(node => node.Id)]);

    // 已发布契约的组织树节点不带业务编码：按同一租户读取一次组织树节点编码，读取次数只随租户增长，不随节点数量增长。
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes = await businessCodeReader.ReadAsync(tenantId);

    return OrganizationScopeOptionsBuilder.Build(tree.Nodes, organizations, businessCodes);
  }
}
