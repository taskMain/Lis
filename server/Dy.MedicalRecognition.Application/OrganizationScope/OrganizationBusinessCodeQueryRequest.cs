namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织业务编码读取请求：只承载租户标识。
/// </summary>
/// <remarks>属性名按 Web 默认 JSON 约定序列化为 <c>tenantId</c>，与 IAM 组织树读取接口的请求体一致。</remarks>
public sealed record OrganizationBusinessCodeQueryRequest
{
  /// <summary>租户标识；为空或全零时读取点不发起请求。</summary>
  public Guid TenantId { get; init; }
}
