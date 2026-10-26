using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application.Contracts.Queries.Session;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 当前会话只读入口：返回已验证 IAM 身份中的账号与会话组织。
/// </summary>
/// <remarks>
/// 只读取认证上下文并投影响应，不做权限判定、不查询数据库、不缓存跨请求结果。
/// 未携带有效凭据时认证中间件返回 401；未选择工作身份时组织字段为空且不报错。
/// </remarks>
[ApiController]
[Route("api/v1/current-session")]
public sealed class CurrentSessionController : ControllerBase
{
  /// <summary>
  /// 已验证的 IAM 当前上下文，提供本次请求的账号与会话组织。
  /// </summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>
  /// 初始化当前会话只读入口。
  /// </summary>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文。</param>
  public CurrentSessionController(ICurrentIamContext currentIamContext)
  {
    this.currentIamContext = currentIamContext;
  }

  /// <summary>
  /// 读取当前账号与会话组织。
  /// </summary>
  /// <returns>包含用户、会话、工作身份、租户与组织标识的只读快照。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出，由认证中间件呈现为 401。</exception>
  [HttpGet]
  public ActionResult<CurrentSessionReadModel> Get()
  {
    IamIdentityContext identity = currentIamContext.RequireCurrent();
    return Ok(new CurrentSessionReadModel
    {
      UserId = identity.UserId,
      SessionId = identity.SessionId,
      WorkIdentityId = identity.WorkIdentityId,
      TenantId = identity.TenantId,
      OrganizationId = identity.OrganizationId
    });
  }
}
