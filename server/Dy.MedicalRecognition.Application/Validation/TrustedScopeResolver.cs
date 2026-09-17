using Dy.Base.Application.Contracts.UserAggregate;
using Dy.Core.Abstractions.Http;

namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 四个互认项目金额入口共用的可信组织与可信医院解析点：登录令牌声明与当前登录用户档案共同构成可信上下文。
/// </summary>
/// <remarks>
/// 组织层与医院层按同一规则解析：令牌该层非空白即取令牌值；令牌该层缺失或空白时用当前登录用户档案补齐；
/// 令牌与用户档案都提供该层且不一致即拒绝；补齐后该层仍为空即拒绝，不使用默认值、不降级为空值。
/// 院区层不参与本解析：院区由请求提交，其归属由 <see cref="OrganizationPathResolver"/> 校验，不因用户档案携带院区而放行。
/// 两层都取自令牌时不再读取用户档案，因此该次请求不新增外部读取。
/// 本解析只服务两个金额保存入口与两个金额查询入口；互认项目配置入口继续按令牌声明取值。
/// 全部判定都先去除首尾空白，拒绝文案由调用方传入，各入口因此保留各自的对外错误消息。
/// </remarks>
internal sealed class TrustedScopeResolver
{
  /// <summary>
  /// 按登录用户标识读取用户档案的外部用户服务，经框架 HTTP 服务代理注入。
  /// </summary>
  private readonly IUserAppService userAppService;

  /// <summary>
  /// 接收外部用户服务作为令牌缺失层的补齐来源。
  /// </summary>
  /// <param name="userAppService">提供当前登录用户组织与医院归属的外部用户服务。</param>
  public TrustedScopeResolver(IUserAppService userAppService) => this.userAppService = userAppService;

  /// <summary>
  /// 解析当前请求的可信组织与可信医院，令牌缺失的层由当前登录用户档案补齐，取不到或冲突时按调用方给定的文案拒绝。
  /// </summary>
  /// <param name="httpRequestInfo">框架认证上下文；为 <see langword="null"/> 时组织层与医院层都视为缺失。</param>
  /// <param name="missingOrganizationMessage">组织层最终仍为空时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层最终仍为空时对外抛出的业务拒绝文案。</param>
  /// <returns>去除首尾空白后的可信组织与可信医院编码，两层都必定非空。</returns>
  /// <exception cref="InvalidOperationException">
  /// 令牌某层缺失且用户标识不能解析为非空 <see cref="Guid"/>、读不到用户档案、
  /// 令牌与用户档案在同层都提供且不一致，或补齐后仍有层为空时抛出。
  /// </exception>
  public async Task<TrustedScope> ResolveOrThrowAsync(
    HttpRequestInfo? httpRequestInfo,
    string missingOrganizationMessage,
    string missingHospitalMessage)
  {
    string tokenOrganizationCode = TrimCode(httpRequestInfo?.OrgId);
    string tokenHospitalCode = TrimCode(httpRequestInfo?.HosId);

    // 令牌已提供两层时可信上下文已经完整，不再读取用户档案，也不引入新的外部调用。
    if (!string.IsNullOrWhiteSpace(tokenOrganizationCode) && !string.IsNullOrWhiteSpace(tokenHospitalCode))
      return new TrustedScope(tokenOrganizationCode, tokenHospitalCode);

    if (!Guid.TryParse(httpRequestInfo?.UserId, out Guid userId) || userId == Guid.Empty)
      throw new InvalidOperationException("无法确定当前登录用户。");

    UserDto user = await userAppService.GetUserByIdAsync(new GetUserByIdRequest { Id = userId })
      ?? throw new InvalidOperationException("无法读取当前登录用户信息。");

    return ResolveOrThrow(
      tokenOrganizationCode,
      tokenHospitalCode,
      user.OrgId,
      user.HosId,
      missingOrganizationMessage,
      missingHospitalMessage);
  }

  /// <summary>
  /// 按「令牌优先、缺失层由用户档案补齐、冲突即拒绝」补齐一层可信编码，补齐后仍为空即按该层文案拒绝。
  /// </summary>
  /// <param name="tokenCode">令牌声明的该层编码；缺失时为去除首尾空白后的空串。</param>
  /// <param name="profileCode">当前登录用户档案的该层编码；可能为 <see langword="null"/>。</param>
  /// <param name="conflictMessage">令牌与用户档案在同层都提供且不一致时对外抛出的业务拒绝文案。</param>
  /// <param name="missingMessage">补齐后该层仍为空时对外抛出的业务拒绝文案。</param>
  /// <returns>去除首尾空白后的该层可信编码，必定非空。</returns>
  /// <exception cref="InvalidOperationException">令牌与用户档案冲突，或补齐后该层仍为空时抛出。</exception>
  private static string ResolveLayerOrThrow(string tokenCode, string? profileCode, string conflictMessage, string missingMessage)
  {
    string normalizedProfileCode = TrimCode(profileCode);

    // 令牌与用户档案都提供该层时以令牌为准，但不一致必须拒绝：静默取其一会让可信范围随来源不同而漂移。
    if (!string.IsNullOrWhiteSpace(tokenCode)
      && !string.IsNullOrWhiteSpace(normalizedProfileCode)
      && !string.Equals(tokenCode, normalizedProfileCode, StringComparison.Ordinal))
      throw new InvalidOperationException(conflictMessage);

    string resolvedCode = string.IsNullOrWhiteSpace(tokenCode) ? normalizedProfileCode : tokenCode;
    if (string.IsNullOrWhiteSpace(resolvedCode)) throw new InvalidOperationException(missingMessage);

    return resolvedCode;
  }

  /// <summary>
  /// 用已读取的用户档案补齐令牌缺失的组织层与医院层，两层判定顺序固定为组织、医院。
  /// </summary>
  /// <param name="tokenOrganizationCode">令牌声明的组织编码；缺失时为空串。</param>
  /// <param name="tokenHospitalCode">令牌声明的医院编码；缺失时为空串。</param>
  /// <param name="profileOrganizationCode">用户档案的组织归属编码；可能为 <see langword="null"/>。</param>
  /// <param name="profileHospitalCode">用户档案的医院归属编码；可能为 <see langword="null"/>。</param>
  /// <param name="missingOrganizationMessage">组织层最终仍为空时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层最终仍为空时对外抛出的业务拒绝文案。</param>
  /// <returns>补齐后的可信组织与可信医院编码。</returns>
  /// <exception cref="InvalidOperationException">任一层的令牌值与用户档案冲突，或补齐后该层仍为空时抛出。</exception>
  private static TrustedScope ResolveOrThrow(
    string tokenOrganizationCode,
    string tokenHospitalCode,
    string? profileOrganizationCode,
    string? profileHospitalCode,
    string missingOrganizationMessage,
    string missingHospitalMessage)
  {
    string organizationCode = ResolveLayerOrThrow(
      tokenOrganizationCode, profileOrganizationCode, "当前登录组织与用户归属组织不一致，不能访问该组织数据。", missingOrganizationMessage);
    string hospitalCode = ResolveLayerOrThrow(
      tokenHospitalCode, profileHospitalCode, "当前登录医院与用户归属医院不一致，不能访问该医院数据。", missingHospitalMessage);

    return new TrustedScope(organizationCode, hospitalCode);
  }

  /// <summary>
  /// 去除业务编码两端空白，使令牌声明、用户档案与请求提交值按同一口径比较。
  /// </summary>
  /// <param name="code">待处理的业务编码；可能为 <see langword="null"/>。</param>
  /// <returns>去除两端空白后的编码；输入为 <see langword="null"/> 时返回空串。</returns>
  private static string TrimCode(string? code) => code?.Trim() ?? string.Empty;

  /// <summary>
  /// 四个金额入口使用的可信组织与可信医院编码对。
  /// </summary>
  /// <remarks>只承载组织与医院两层：院区不由本解析点决定，因此类型内没有院区成员。</remarks>
  /// <param name="OrganizationCode">去除首尾空白后的可信组织编码，必定非空。</param>
  /// <param name="HospitalCode">去除首尾空白后的可信医院编码，必定非空。</param>
  internal sealed record TrustedScope(string OrganizationCode, string HospitalCode);
}
