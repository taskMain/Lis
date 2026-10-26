namespace Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

/// <summary>
/// 平台入口范围选择器的可选项：按当前租户组织的医共体 → 医院 → 院区三级结构。
/// </summary>
/// <remarks>
/// 只包含启用且类型明确的节点；未配置类型或类型已停用的节点不进入选项，数量单独交付供页面提示。
/// </remarks>
public sealed record OrganizationScopeOptionsReadModel
{
  /// <summary>
  /// 医共体选项集合，按组织树中的顺序排列。
  /// </summary>
  public IReadOnlyList<OrganizationScopeOrganizationReadModel> Organizations { get; init; } = [];

  /// <summary>
  /// 未进入选项的节点数量：类型未配置或类型已停用的启用节点。
  /// </summary>
  public int UnconfiguredOrganizationCount { get; init; }
}
