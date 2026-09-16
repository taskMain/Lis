using Dy.Core.Abstractions.Http;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 把当前请求的认证上下文设置为指定的可信组织与操作人，供写入口与查询入口的用例共用。
/// </summary>
/// <remarks>
/// 使用框架公开工厂 <see cref="HttpContextInfoFactory"/> 构造请求信息并设置为当前上下文，模拟框架按 Bearer token 声明构造出的请求；
/// 不再通过反射写入内部设置器。写入只作用于当前用例的异步上下文，不连接数据库、不修改任何生产代码。
/// </remarks>
internal static class TrustedRequestContext
{
  /// <summary>
  /// 把当前请求上下文设置为指定的可信组织与操作人。
  /// </summary>
  /// <param name="organizationCode">令牌中的组织编码；传空串或纯空白表示上下文中没有可用组织。</param>
  /// <param name="userId">令牌中的用户标识；传无法解析为 Guid 的文本表示操作人不可解析。</param>
  internal static void Use(string organizationCode, string userId)
  {
    HttpContextInfoFactory
      .CreateHttpContextInfo(null, 0, 0, HttpContextInfoFactory.CreateHttpRequestInfo(
        orgId: organizationCode,
        subApplication: "SUB",
        hosId: "HOS-1",
        branchId: "BRH-1",
        deptId: "DPT-1",
        userId: userId), null)
      .SetCurrent();
  }
}
