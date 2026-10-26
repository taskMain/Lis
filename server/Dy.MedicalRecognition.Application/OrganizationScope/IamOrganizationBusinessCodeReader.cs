namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织业务编码读取点：按租户读取组织树，并按组织实例标识索引节点与其业务编码。
/// </summary>
/// <remarks>
/// 读取次数只随租户增长：一个租户一次整树读取，调用方按标识查表，不按标识逐条读取。
/// 未配置业务编码的节点仍进入结果，其编码为空；调用方按各自业务口径处理空值。
/// 管理接口代理不透传取消令牌，因此本读取点不接受取消令牌。
/// 本读取点只读 IAM，不写库、不发事件、不判断权限。
/// </remarks>
internal sealed class IamOrganizationBusinessCodeReader
{
  /// <summary>
  /// 组织业务编码查询客户端，由 IAM 管理接口代理提供。
  /// </summary>
  private readonly IOrganizationBusinessCodeQueryClient queryClient;

  /// <summary>
  /// 接收组织业务编码查询客户端作为唯一读取来源。
  /// </summary>
  /// <param name="queryClient">组织业务编码查询客户端。</param>
  internal IamOrganizationBusinessCodeReader(IOrganizationBusinessCodeQueryClient queryClient) => this.queryClient = queryClient;

  /// <summary>
  /// 读取指定租户的组织树，并按组织实例标识索引。
  /// </summary>
  /// <param name="tenantId">租户标识；全零时返回空结果且不发起请求。</param>
  /// <returns>标识到组织树节点的映射；重复标识保留最后一条。</returns>
  internal async Task<IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode>> ReadAsync(Guid tenantId)
  {
    if (tenantId == Guid.Empty) return new Dictionary<Guid, OrganizationBusinessCodeTreeNode>();

    OrganizationBusinessCodeTreeResponse response = await queryClient.QueryOrganizationByTenantIdAsync(
      new OrganizationBusinessCodeQueryRequest { TenantId = tenantId });

    Dictionary<Guid, OrganizationBusinessCodeTreeNode> nodes = [];
    foreach (OrganizationBusinessCodeTreeNode node in response.Nodes ?? [])
    {
      if (node.Id != Guid.Empty) nodes[node.Id] = node;
    }

    return nodes;
  }
}
