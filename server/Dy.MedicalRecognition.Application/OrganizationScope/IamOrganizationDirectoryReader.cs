using Dy.IAM.Sdk;

namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 按标识批量读取 IAM 组织详情：读取当前节点与祖先链、平台入口的目标范围都以本读取点为唯一来源。
/// </summary>
/// <remarks>
/// 单次目录批量读取最多 100 个标识，超出时按批切分；读取次数只随去重后的组织数量增长，不随调用方条目数量增长。
/// 未找到的组织不出现在结果中，由调用方按各层业务口径拒绝。
/// </remarks>
internal sealed class IamOrganizationDirectoryReader
{
  /// <summary>
  /// IAM 共享目录客户端，提供组织详情与组织类型。
  /// </summary>
  private readonly IIamDirectoryClient directoryClient;

  /// <summary>
  /// 接收 IAM 共享目录客户端作为组织详情的读取来源。
  /// </summary>
  /// <param name="directoryClient">IAM 共享目录客户端。</param>
  internal IamOrganizationDirectoryReader(IIamDirectoryClient directoryClient) => this.directoryClient = directoryClient;

  /// <summary>
  /// 批量读取组织详情，按标识索引返回。
  /// </summary>
  /// <param name="organizationIds">待读取的组织实例标识；重复标识只读取一次。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>标识到组织详情的映射；未找到的组织不在映射中。</returns>
  internal async Task<IReadOnlyDictionary<Guid, IamDirectoryOrganization>> ReadAsync(
    IReadOnlyList<Guid> organizationIds,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(organizationIds);

    // 去重后再切分：同一组织在一条路径上重复出现时只读取一次。
    Guid[] distinctIds = [.. organizationIds.Where(id => id != Guid.Empty).Distinct()];
    Dictionary<Guid, IamDirectoryOrganization> organizations = [];
    foreach (Guid[] batch in distinctIds.Chunk(100))
    {
      IamDirectoryReadResult result = await directoryClient.BatchReadDirectoryAsync(
        new IamDirectoryReadRequest("Organization", batch, null), cancellationToken);
      foreach (IamDirectoryReadItem item in result.Items)
      {
        if (item.Found && item.Data?.Organization is { } organization)
        {
          organizations[item.Id] = organization;
        }
      }
    }

    return organizations;
  }
}
