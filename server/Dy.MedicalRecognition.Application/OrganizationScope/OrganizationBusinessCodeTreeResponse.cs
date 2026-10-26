namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织业务编码读取结果：租户标识与其组织树节点。
/// </summary>
/// <remarks>只声明本项目使用的字段；IAM 返回的其他字段（如排序值、祖先链可用标记）在反序列化时被忽略。</remarks>
public sealed record OrganizationBusinessCodeTreeResponse
{
  /// <summary>租户标识。</summary>
  public Guid TenantId { get; init; }

  /// <summary>租户名称；用于排查，不参与组织范围判定。</summary>
  public string? TenantName { get; init; }

  /// <summary>租户是否启用；用于排查，不参与组织范围判定。</summary>
  public bool TenantIsEnabled { get; init; }

  /// <summary>扁平的组织树节点；没有节点时为空集合。</summary>
  public OrganizationBusinessCodeTreeNode[] Nodes { get; init; } = [];
}
