namespace Dy.MedicalRecognition.Application.Contracts.Queries.TrustedScope;

/// <summary>可信范围的当前组织，用于页面展示调用方所在的组织。</summary>
/// <remarks>
/// 组织编码只取自服务端可信上下文，组织名称由服务端按该编码从外部组织服务解析，两者同源；
/// 只返回组织一层，不返回医院与院区。解析失败按拒绝结束，不返回空值、也不降级为只回编码。
/// </remarks>
public sealed record TrustedOrganizationScopeReadModel
{
  /// <summary>可信当前组织的业务编码。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>可信当前组织的名称，来源于外部组织服务的组织数据。</summary>
  public string OrganizationName { get; init; } = string.Empty;
}
