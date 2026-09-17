namespace Dy.MedicalRecognition.Application;

/// <summary>
/// 从框架认证上下文读取当前请求可信组织的统一解析点，供互认项目配置的写入与查询共用。
/// </summary>
/// <remarks>
/// 一律先去除首尾空白再判定是否可用，写入侧与查询侧因此对“可信组织带首尾空白”给出一致结论；
/// 空值、空串与纯空白都按取不到可信值拒绝，不使用默认值或空串继续写入或查询。
/// 拒绝文案由调用方传入，各入口因此保留各自的对外错误消息。
/// </remarks>
internal static class TrustedOrganizationResolver
{
  /// <summary>
  /// 解析当前请求的可信组织编码，取不到时按调用方给定的文案拒绝。
  /// </summary>
  /// <param name="organizationCode">框架认证上下文中的组织编码；上下文中没有组织时为 <see langword="null"/>。</param>
  /// <param name="missingOrganizationMessage">取不到可信组织时对外抛出的业务拒绝文案。</param>
  /// <returns>去除首尾空白后的可信组织编码，必定非空。</returns>
  /// <exception cref="InvalidOperationException">组织编码缺失、为空串或去除首尾空白后为空时抛出。</exception>
  internal static string ResolveOrThrow(string? organizationCode, string missingOrganizationMessage)
  {
    string resolvedOrganizationCode = organizationCode?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(resolvedOrganizationCode)) throw new InvalidOperationException(missingOrganizationMessage);

    return resolvedOrganizationCode;
  }
}
