using Dy.IAM.Sdk;

namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织范围解析点：把当前工作身份的组织归为医共体、医院、院区三层，并校验平台入口提交的目标范围。
/// </summary>
/// <remarks>
/// 当前身份的组织层只取自 <c>ICurrentIamContext.OrganizationId</c>，沿组织祖先链读取并按 <c>OrganizationType.Code</c> 归类；
/// 各层组织业务编码由组织业务编码读取点按当前租户一次读取后按标识补齐，读取不到时该层编码为空。
/// 平台入口按请求提交的组织实例标识校验存在性、启用状态与父子归属。
/// 停用节点按不存在处理；同一类型出现多个祖先时拒绝，不任取一条；缺失层级返回空，由业务规则按各自文案拒绝。
/// 本解析点只读 IAM 目录，不写库、不发事件、不判断角色或权限。
/// </remarks>
internal sealed class IamOrganizationScopeResolver
{
  /// <summary>
  /// 组织详情读取点。
  /// </summary>
  private readonly IamOrganizationDirectoryReader directoryReader;

  /// <summary>
  /// 已验证的 IAM 当前上下文，提供当前工作身份与租户。
  /// </summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>
  /// 组织业务编码读取点；未装配时各层业务编码为空。
  /// </summary>
  private readonly IamOrganizationBusinessCodeReader? businessCodeReader;

  /// <summary>
  /// 接收组织详情读取点与 IAM 当前上下文；未提供组织业务编码读取点时各层业务编码为空。
  /// </summary>
  /// <param name="directoryReader">组织详情读取点。</param>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文。</param>
  /// <param name="businessCodeReader">组织业务编码读取点；为 <see langword="null"/> 时不读取业务编码。</param>
  internal IamOrganizationScopeResolver(
    IamOrganizationDirectoryReader directoryReader,
    ICurrentIamContext currentIamContext,
    IamOrganizationBusinessCodeReader? businessCodeReader = null)
  {
    this.directoryReader = directoryReader;
    this.currentIamContext = currentIamContext;
    this.businessCodeReader = businessCodeReader;
  }

  /// <summary>
  /// 读取当前工作身份的组织范围；缺失层级以空值返回。
  /// </summary>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>当前身份的三级范围；链上不存在的层级为空。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">同一类型在祖先链上出现多个节点时抛出。</exception>
  internal async Task<IamOrganizationScope> ReadCurrentScopeAsync(CancellationToken cancellationToken = default)
  {
    Guid organizationId = RequireOrganizationId();

    // 先读当前节点：路径给出祖先标识，再按一批读取祖先详情与类型，读取次数不随层级深度增长。
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> currentOrganizations =
      await directoryReader.ReadAsync([organizationId], cancellationToken);
    if (!currentOrganizations.TryGetValue(organizationId, out IamDirectoryOrganization? current) || !current.IsEnabled)
    {
      return IamOrganizationScope.Empty;
    }

    Guid[] ancestorIds = [.. current.Path.Select(item => item.Id).Where(id => id != organizationId)];
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> ancestors = ancestorIds.Length == 0
      ? new Dictionary<Guid, IamDirectoryOrganization>()
      : await directoryReader.ReadAsync(ancestorIds, cancellationToken);

    List<IamDirectoryOrganization> nodes = [current];
    nodes.AddRange(ancestors.Values);
    return BuildScope(nodes, await ReadBusinessCodesAsync());
  }

  /// <summary>
  /// 校验平台入口提交的组织与医院两层：两层都要求存在、启用、类型相符且医院父级为该组织。
  /// </summary>
  /// <param name="organizationId">提交的医共体组织实例标识。</param>
  /// <param name="hospitalId">提交的医院组织实例标识。</param>
  /// <param name="missingOrganizationMessage">医共体层不成立时抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>组织与医院两层都非空的组织范围。</returns>
  /// <exception cref="InvalidOperationException">任一层不成立时抛出。</exception>
  internal async Task<IamOrganizationScope> ResolveSubmittedOrganizationHospitalAsync(
    Guid organizationId,
    Guid hospitalId,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    CancellationToken cancellationToken = default)
  {
    // 单条入口复用批量入口：只有一份校验实现，读取次数与批量入口同一口径。
    IReadOnlyList<IamOrganizationScope> scopes = await ResolveSubmittedOrganizationHospitalsAsync(
      [new IamOrganizationScopeTarget(organizationId, hospitalId, Guid.Empty)],
      missingOrganizationMessage, missingHospitalMessage, cancellationToken);
    return scopes[0];
  }

  /// <summary>
  /// 校验平台入口提交的一批组织与医院两层目标：两层都要求存在、启用、类型相符且医院父级为该组织。
  /// </summary>
  /// <remarks>
  /// 整批目标共用一次目录读取与一次编码整树读取，读取次数只随去重后的组织数量增长，不随目标条数增长；
  /// 解析结果与输入等长且同序，院区层不参与本入口，返回时为空。
  /// </remarks>
  /// <param name="targets">目标范围集合，至少一条；院区层不参与本入口。</param>
  /// <param name="missingOrganizationMessage">医共体不存在、已停用或类型不符时抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在、已停用、类型不符或不属于该医共体时的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>与输入等长且同序的已校验范围，院区层为空。</returns>
  /// <exception cref="ArgumentNullException">目标集合为 null 时抛出。</exception>
  /// <exception cref="ArgumentException">目标集合为空时抛出。</exception>
  /// <exception cref="InvalidOperationException">任一条目标的两层校验不成立时抛出。</exception>
  internal async Task<IReadOnlyList<IamOrganizationScope>> ResolveSubmittedOrganizationHospitalsAsync(
    IReadOnlyList<IamOrganizationScopeTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条目标组织范围。", nameof(targets));

    // 整批目标共用一次目录读取：去重后的标识按 100 个一批切分，读取次数不随目标条数增长。
    Guid[] organizationIds = [.. targets.SelectMany(target => new[] { target.OrganizationId, target.HospitalId })];
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> organizations =
      await directoryReader.ReadAsync(organizationIds, cancellationToken);
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes = await ReadBusinessCodesAsync();

    List<IamOrganizationScope> scopes = [];
    foreach (IamOrganizationScopeTarget target in targets)
    {
      IamDirectoryOrganization organization = RequireNode(
        organizations, target.OrganizationId, IamOrganizationTypeCodes.CountyMedicalCommunity, missingOrganizationMessage);
      IamDirectoryOrganization hospital = RequireNode(
        organizations, target.HospitalId, IamOrganizationTypeCodes.Hospital, missingHospitalMessage);
      if (hospital.ParentOrganizationId != organization.Id) throw new InvalidOperationException(missingHospitalMessage);

      scopes.Add(new IamOrganizationScope(
        organization.Id, organization.Name, BusinessCodeOf(businessCodes, organization.Id),
        hospital.Id, hospital.Name, BusinessCodeOf(businessCodes, hospital.Id),
        null, null, null));
    }

    return scopes;
  }

  /// <summary>
  /// 校验平台入口提交的一批目标组织范围。
  /// </summary>
  /// <param name="targets">目标范围集合，至少一条。</param>
  /// <param name="missingOrganizationMessage">医共体不存在、已停用或类型不符时抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在、已停用、类型不符或不属于该医共体时的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区不存在、已停用、类型不符或不属于该医院时的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>与输入等长且同序的已校验范围。</returns>
  /// <exception cref="ArgumentNullException">目标集合为 null 时抛出。</exception>
  /// <exception cref="ArgumentException">目标集合为空时抛出。</exception>
  /// <exception cref="InvalidOperationException">任一条目标范围校验不成立时抛出。</exception>
  internal async Task<IReadOnlyList<IamOrganizationScope>> ResolveSubmittedScopesAsync(
    IReadOnlyList<IamOrganizationScopeTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条目标组织范围。", nameof(targets));

    // 整批目标共用一次目录读取：去重后的标识按 100 个一批切分，读取次数不随目标条数增长。
    Guid[] organizationIds = [.. targets.SelectMany(target => new[] { target.OrganizationId, target.HospitalId, target.BranchId })];
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> organizations =
      await directoryReader.ReadAsync(organizationIds, cancellationToken);
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes = await ReadBusinessCodesAsync();

    List<IamOrganizationScope> scopes = [];
    foreach (IamOrganizationScopeTarget target in targets)
    {
      IamDirectoryOrganization organization = RequireNode(
        organizations, target.OrganizationId, IamOrganizationTypeCodes.CountyMedicalCommunity, missingOrganizationMessage);
      IamDirectoryOrganization hospital = RequireNode(
        organizations, target.HospitalId, IamOrganizationTypeCodes.Hospital, missingHospitalMessage);
      if (hospital.ParentOrganizationId != organization.Id) throw new InvalidOperationException(missingHospitalMessage);

      IamDirectoryOrganization branch = RequireNode(
        organizations, target.BranchId, IamOrganizationTypeCodes.Branch, missingBranchMessage);
      if (branch.ParentOrganizationId != hospital.Id) throw new InvalidOperationException(missingBranchMessage);

      scopes.Add(new IamOrganizationScope(
        organization.Id, organization.Name, BusinessCodeOf(businessCodes, organization.Id),
        hospital.Id, hospital.Name, BusinessCodeOf(businessCodes, hospital.Id),
        branch.Id, branch.Name, BusinessCodeOf(businessCodes, branch.Id)));
    }

    return scopes;
  }

  /// <summary>
  /// 取当前工作身份所在的组织标识；未选择工作身份时按 IAM 契约抛出工作身份必需异常。
  /// </summary>
  /// <returns>当前工作身份的组织标识。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  private Guid RequireOrganizationId()
  {
    IamIdentityContext identity = currentIamContext.RequireCurrent();
    return identity.OrganizationId is { } organizationId && organizationId != Guid.Empty
      ? organizationId
      : throw new IamWorkIdentityRequiredException();
  }

  /// <summary>
  /// 读取当前租户的组织业务编码；未装配读取点或租户不可用时返回空映射。
  /// </summary>
  /// <returns>组织标识到组织节点的映射；没有编码来源时为空映射。</returns>
  private async Task<IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode>> ReadBusinessCodesAsync()
  {
    if (businessCodeReader is null) return new Dictionary<Guid, OrganizationBusinessCodeTreeNode>();

    Guid? tenantId = currentIamContext.Current?.TenantId;
    return tenantId is { } selectedTenantId && selectedTenantId != Guid.Empty
      ? await businessCodeReader.ReadAsync(selectedTenantId)
      : new Dictionary<Guid, OrganizationBusinessCodeTreeNode>();
  }

  /// <summary>
  /// 取指定组织的业务编码；组织不在编码结果中时为空。
  /// </summary>
  /// <param name="businessCodes">组织标识到组织节点的映射。</param>
  /// <param name="organizationId">组织实例标识。</param>
  /// <returns>组织业务编码；未配置时为空。</returns>
  private static string? BusinessCodeOf(IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes, Guid organizationId) =>
    businessCodes.TryGetValue(organizationId, out OrganizationBusinessCodeTreeNode? node) ? node.Code : null;

  /// <summary>
  /// 把当前身份链上的节点按组织类型归为三层；缺失层级留空，同类型多节点拒绝。
  /// </summary>
  /// <param name="nodes">当前节点与祖先链节点。</param>
  /// <param name="businessCodes">组织标识到组织节点的映射，提供各层业务编码。</param>
  /// <returns>三级组织范围。</returns>
  /// <exception cref="InvalidOperationException">同一类型出现多个节点时抛出。</exception>
  private static IamOrganizationScope BuildScope(
    IReadOnlyList<IamDirectoryOrganization> nodes,
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes)
  {
    IamDirectoryOrganization? organization = FindSingle(nodes, IamOrganizationTypeCodes.CountyMedicalCommunity);
    IamDirectoryOrganization? hospital = FindSingle(nodes, IamOrganizationTypeCodes.Hospital);
    IamDirectoryOrganization? branch = FindSingle(nodes, IamOrganizationTypeCodes.Branch);
    return new IamOrganizationScope(
      organization?.Id, organization?.Name, organization is null ? null : BusinessCodeOf(businessCodes, organization.Id),
      hospital?.Id, hospital?.Name, hospital is null ? null : BusinessCodeOf(businessCodes, hospital.Id),
      branch?.Id, branch?.Name, branch is null ? null : BusinessCodeOf(businessCodes, branch.Id));
  }

  /// <summary>
  /// 在节点集合中按组织类型取唯一节点；出现多个同类型节点时拒绝，不任取一条。
  /// </summary>
  /// <param name="nodes">待归类节点。</param>
  /// <param name="typeCode">目标组织类型编码。</param>
  /// <returns>唯一匹配节点；没有匹配时为空。</returns>
  /// <exception cref="InvalidOperationException">匹配到多个同类型节点时抛出。</exception>
  private static IamDirectoryOrganization? FindSingle(IReadOnlyList<IamDirectoryOrganization> nodes, string typeCode)
  {
    IamDirectoryOrganization[] matches =
    [
      .. nodes.Where(node => node.IsEnabled
        && string.Equals(node.OrganizationType?.Code, typeCode, StringComparison.Ordinal))
    ];
    return matches.Length switch
    {
      > 1 => throw new InvalidOperationException("业务拒绝：当前身份的组织链存在多个同类型组织，不能确定可信范围。"),
      _ => matches.SingleOrDefault()
    };
  }

  /// <summary>
  /// 取请求提交的目标节点，要求存在、启用且组织类型符合该层。
  /// </summary>
  /// <param name="organizations">标识到组织详情的映射。</param>
  /// <param name="organizationId">目标组织标识。</param>
  /// <param name="typeCode">该层要求的组织类型编码。</param>
  /// <param name="missingMessage">不成立时抛出的业务拒绝文案。</param>
  /// <returns>已校验的组织详情。</returns>
  /// <exception cref="InvalidOperationException">节点不存在、已停用或类型不符时抛出。</exception>
  private static IamDirectoryOrganization RequireNode(
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> organizations,
    Guid organizationId,
    string typeCode,
    string missingMessage)
  {
    if (organizationId == Guid.Empty
      || !organizations.TryGetValue(organizationId, out IamDirectoryOrganization? organization)
      || !organization.IsEnabled
      || !string.Equals(organization.OrganizationType?.Code, typeCode, StringComparison.Ordinal))
    {
      throw new InvalidOperationException(missingMessage);
    }

    return organization;
  }
}
