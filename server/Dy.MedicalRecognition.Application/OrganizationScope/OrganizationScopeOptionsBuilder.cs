using Dy.IAM.Sdk;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 平台入口范围选择器的组装点：把当前租户的组织树节点与已读取的组织类型装配为三级选项。
/// </summary>
/// <remarks>
/// 只保留启用且祖先链启用的节点；类型未配置或类型已停用的节点不进入选项，数量单独交付。
/// 医共体、医院、院区按父子关系装配，父级不在可选集合中的节点不进入选项。
/// 每个选项同时给出实例标识、名称与组织业务编码：实例标识与名称取自组织树节点，业务编码取自组织业务编码读取结果；
/// 节点未配置业务编码时该项为空串，不用实例标识文本或名称替代。
/// </remarks>
internal static class OrganizationScopeOptionsBuilder
{
  /// <summary>
  /// 组装三级选项。
  /// </summary>
  /// <param name="candidates">当前租户的组织树节点，已按启用与祖先链启用筛选。</param>
  /// <param name="organizations">标识到组织详情的映射，提供各节点的组织类型。</param>
  /// <param name="businessCodes">标识到组织树节点的映射，提供各节点的组织业务编码；未登记或未配置编码的节点按空串处理。</param>
  /// <returns>医共体、医院、院区三级选项与未配置类型的节点数量。</returns>
  internal static OrganizationScopeOptionsReadModel Build(
    IReadOnlyList<OrganizationNodeResponse> candidates,
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> organizations,
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes)
  {
    ArgumentNullException.ThrowIfNull(candidates);
    ArgumentNullException.ThrowIfNull(organizations);
    ArgumentNullException.ThrowIfNull(businessCodes);

    List<OrganizationNodeResponse> selectable = [.. candidates.Where(node => node.IsEnabled && node.AncestorChainEnabled)];

    // 一个节点可有多个启用类型；按集合包含关系归类，不取第一个类型。
    Dictionary<Guid, string[]> typeCodes = [];
    int unconfiguredOrganizationCount = 0;
    foreach (OrganizationNodeResponse node in selectable)
    {
      IamDirectoryOrganizationType[] types = organizations.TryGetValue(node.Id, out IamDirectoryOrganization? organization)
        ? [.. organization.OrganizationTypes.Where(type => type.IsEnabled)]
        : [];
      if (types.Length == 0)
      {
        unconfiguredOrganizationCount++;
        continue;
      }

      typeCodes[node.Id] = [.. types.Select(type => type.Code)];
    }

    List<OrganizationNodeResponse> communities = [.. selectable.Where(node => IsType(typeCodes, node.Id, IamOrganizationTypeCodes.CountyMedicalCommunity))];
    List<OrganizationNodeResponse> hospitals = [.. selectable.Where(node => IsType(typeCodes, node.Id, IamOrganizationTypeCodes.Hospital))];
    List<OrganizationNodeResponse> branches = [.. selectable.Where(node => IsType(typeCodes, node.Id, IamOrganizationTypeCodes.Branch))];

    Dictionary<Guid, List<OrganizationNodeResponse>> hospitalsByCommunity = hospitals
      .Where(node => node.ParentOrganizationId is not null)
      .GroupBy(node => node.ParentOrganizationId!.Value)
      .ToDictionary(group => group.Key, group => group.ToList());
    Dictionary<Guid, List<OrganizationNodeResponse>> branchesByHospital = branches
      .Where(node => node.ParentOrganizationId is not null)
      .GroupBy(node => node.ParentOrganizationId!.Value)
      .ToDictionary(group => group.Key, group => group.ToList());

    List<OrganizationScopeOrganizationReadModel> organizationsResult = [];
    foreach (OrganizationNodeResponse community in communities
      .OrderBy(node => node.SortOrder)
      .ThenBy(node => node.Name, StringComparer.Ordinal))
    {
      List<OrganizationScopeHospitalReadModel> hospitalResults = [];
      foreach (OrganizationNodeResponse hospital in (hospitalsByCommunity.GetValueOrDefault(community.Id) ?? [])
        .OrderBy(node => node.SortOrder)
        .ThenBy(node => node.Name, StringComparer.Ordinal))
      {
        IReadOnlyList<OrganizationScopeBranchReadModel> branchResults =
        [
          .. (branchesByHospital.GetValueOrDefault(hospital.Id) ?? [])
            .OrderBy(node => node.SortOrder)
            .ThenBy(node => node.Name, StringComparer.Ordinal)
            .Select(node => new OrganizationScopeBranchReadModel
            {
              OrganizationId = node.Id,
              OrganizationName = node.Name,
              OrganizationCode = BusinessCodeOf(businessCodes, node.Id)
            })
        ];
        hospitalResults.Add(new OrganizationScopeHospitalReadModel
        {
          OrganizationId = hospital.Id,
          OrganizationName = hospital.Name,
          OrganizationCode = BusinessCodeOf(businessCodes, hospital.Id),
          Branches = branchResults
        });
      }

      organizationsResult.Add(new OrganizationScopeOrganizationReadModel
      {
        OrganizationId = community.Id,
        OrganizationName = community.Name,
        OrganizationCode = BusinessCodeOf(businessCodes, community.Id),
        Hospitals = hospitalResults
      });
    }

    return new OrganizationScopeOptionsReadModel
    {
      Organizations = organizationsResult,
      UnconfiguredOrganizationCount = unconfiguredOrganizationCount
    };
  }

  /// <summary>
  /// 取节点的组织业务编码。
  /// </summary>
  /// <remarks>编码只取自组织业务编码读取结果：未登记该节点或节点未配置编码时返回空串，不用实例标识文本或名称替代。</remarks>
  /// <param name="businessCodes">标识到组织树节点的映射。</param>
  /// <param name="organizationId">组织实例标识。</param>
  /// <returns>组织业务编码；未配置时为空串。</returns>
  private static string BusinessCodeOf(IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> businessCodes, Guid organizationId) =>
    businessCodes.TryGetValue(organizationId, out OrganizationBusinessCodeTreeNode? node) ? node.Code ?? string.Empty : string.Empty;

  /// <summary>
  /// 判断节点是否属于指定组织类型编码。
  /// </summary>
  /// <param name="typeCodes">节点标识到组织类型编码的映射。</param>
  /// <param name="organizationId">待判断的组织标识。</param>
  /// <param name="typeCode">目标组织类型编码。</param>
  /// <returns>类型编码一致时为 <see langword="true"/>。</returns>
  private static bool IsType(IReadOnlyDictionary<Guid, string[]> typeCodes, Guid organizationId, string typeCode) =>
    typeCodes.TryGetValue(organizationId, out string[]? codes) && codes.Contains(typeCode, StringComparer.Ordinal);
}
