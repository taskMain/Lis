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
  /// 把当前请求上下文设置为指定的可信组织与操作人，可信医院与院区取用例默认值。
  /// </summary>
  /// <param name="organizationCode">令牌中的组织编码；传空串或纯空白表示上下文中没有可用组织。</param>
  /// <param name="userId">令牌中的用户标识；传无法解析为 Guid 的文本表示操作人不可解析。</param>
  internal static void Use(string organizationCode, string userId) => Use(organizationCode, DefaultHospitalCode, DefaultBranchCode, userId);

  /// <summary>
  /// 把当前请求上下文设置为指定的可信组织、可信医院、可信院区与操作人。
  /// </summary>
  /// <remarks>医院与院区都可以传入空串或纯空白，用于构造"可信医院缺失"的上下文；本重载只扩展可构造的上下文，不改变既有重载的行为语义。</remarks>
  /// <param name="organizationCode">令牌中的组织编码；传空串或纯空白表示上下文中没有可用组织。</param>
  /// <param name="hospitalCode">令牌中的医院编码；传空串或纯空白表示上下文中没有可用医院。</param>
  /// <param name="branchCode">令牌中的院区编码；传空串或纯空白表示上下文中没有可用院区。</param>
  /// <param name="userId">令牌中的用户标识；传无法解析为 Guid 的文本表示操作人不可解析。</param>
  internal static void Use(string? organizationCode, string? hospitalCode, string? branchCode, string userId)
  {
    // 工厂的形参声明为非空字符串，但框架上下文正是用空串表达"该层缺失"；
    // 这里必须把 null 与空白原样交给框架，才能构造出"可信组织或可信医院缺失"的上下文，
    // 因此用 null 包容运算符显式表达该意图，而不是用空串把缺失伪装成有效值。
    HttpContextInfoFactory
      .CreateHttpContextInfo(null, 0, 0, HttpContextInfoFactory.CreateHttpRequestInfo(
        orgId: organizationCode!,
        subApplication: "SUB",
        hosId: hospitalCode!,
        branchId: branchCode!,
        deptId: "DPT-1",
        userId: userId), null)
      .SetCurrent();
  }

  /// <summary>既有重载使用的默认医院编码，保持既有调用点的上下文取值不变。</summary>
  private const string DefaultHospitalCode = "HOS-1";

  /// <summary>既有重载使用的默认院区编码，保持既有调用点的上下文取值不变。</summary>
  private const string DefaultBranchCode = "BRH-1";

  /// <summary>
  /// 读取当前请求的认证上下文，供直接调用可信范围解析点的用例与各入口取得同一份入参。
  /// </summary>
  /// <returns>当前请求的 <see cref="HttpRequestInfo"/>；未设置当前上下文时返回 <see langword="null"/>。</returns>
  internal static HttpRequestInfo? Current() => HttpContextInfo.Current?.RequestInfo;
}
