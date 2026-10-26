namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织树节点：组织业务编码与父子关系的只读投影。
/// </summary>
/// <remarks>业务编码可能为空，表示该组织尚未配置业务编码；此时按空值传递，不用标识、名称或层级推断替代。</remarks>
public sealed record OrganizationBusinessCodeTreeNode
{
  /// <summary>组织实例标识。</summary>
  public Guid Id { get; init; }

  /// <summary>父组织实例标识；根节点为空。</summary>
  public Guid? ParentOrganizationId { get; init; }

  /// <summary>组织名称。</summary>
  public string? Name { get; init; }

  /// <summary>组织是否启用。</summary>
  public bool IsEnabled { get; init; }

  /// <summary>组织业务编码；未配置时为空。</summary>
  public string? Code { get; init; }
}
