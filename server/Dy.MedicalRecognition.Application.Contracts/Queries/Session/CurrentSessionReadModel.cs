namespace Dy.MedicalRecognition.Application.Contracts.Queries.Session;

/// <summary>
/// 当前 IAM 会话的只读快照：账号标识与会话组织。
/// </summary>
/// <remarks>
/// 组织相关字段来自本次登录选择的工作身份；未选择工作身份时三者为空，调用方不得按空值推断权限范围。
/// </remarks>
public sealed record CurrentSessionReadModel
{
  /// <summary>
  /// 全局用户标识，对应 IAM 令牌的 <c>sub</c>。
  /// </summary>
  public Guid UserId { get; init; }

  /// <summary>
  /// 签发时的会话标识，用于同一会话内的上下文核对。
  /// </summary>
  public Guid SessionId { get; init; }

  /// <summary>
  /// 本次工作身份的组织用户映射标识；未选择工作身份时为空。
  /// </summary>
  public Guid? WorkIdentityId { get; init; }

  /// <summary>
  /// 本次工作身份所属租户标识；未选择工作身份时为空。
  /// </summary>
  public Guid? TenantId { get; init; }

  /// <summary>
  /// 本次工作身份所在组织标识；未选择工作身份时为空。
  /// </summary>
  public Guid? OrganizationId { get; init; }
}
