using Dy.Core.Abstractions.Http;
using IamHttpMethod = Dy.Core.Abstractions.Http.HttpMethod;

namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织业务编码读取点：按租户读取 IAM 组织树节点及其业务编码。
/// </summary>
/// <remarks>
/// 已发布的 IAM 强类型契约不在组织节点上声明业务编码，`System.Text.Json` 会静默丢弃该字段，
/// 因此本接口以自有契约直接映射同一个 HTTP 接口，用于读取 <c>organization.code</c>。
/// 代理由 <c>AddIamManagementClient</c> 生成：绑定的 Authority 取配置节 <c>Iam</c>，每次调用透传当前已验证的访问令牌，不另配客户端凭据。
/// 路由由方法上的 <see cref="HttpMethodAttribute"/> 显式给出：接口名与 <c>Api/{控制器名}/{动作名}</c> 的默认推导在本读取点不成立，
/// 显式路由可避免重命名接口即改接口地址。
/// POST 且只有一个非路由参数时，该参数序列化为 JSON 请求体。
/// </remarks>
public interface IOrganizationBusinessCodeQueryClient
{
  /// <summary>
  /// 按租户读取组织树节点，节点携带组织业务编码与父子关系。
  /// </summary>
  /// <param name="request">租户标识请求体。</param>
  /// <returns>该租户的组织树；节点业务编码可能为空，由调用方按业务口径处理。</returns>
  [HttpMethodAttribute(IamHttpMethod.POST, Route = "Api/Organization/QueryOrganizationByTenantId")]
  Task<OrganizationBusinessCodeTreeResponse> QueryOrganizationByTenantIdAsync(OrganizationBusinessCodeQueryRequest request);
}
