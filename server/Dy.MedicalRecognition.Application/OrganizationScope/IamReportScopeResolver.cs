using Dy.IAM.Sdk;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 报告链路的组织范围解析点：把当前工作身份的组织链归为医共体、医院、院区三级组织实例，并校验平台入口提交的目标范围。
/// </summary>
/// <remarks>
/// 本院入口的三层范围只取自当前工作身份的组织祖先链，调用方不能通过请求扩大范围；
/// 平台入口按请求提交的组织实例校验类型、启用状态与父子归属，与本院入口共用同一份目录读取与归类口径。
/// 本解析点只读 IAM 目录，不写库，不发事件，不判断角色或权限；缺失层级按各入口的业务文案拒绝。
/// </remarks>
internal sealed class IamReportScopeResolver
{
  /// <summary>
  /// 组织范围解析点，提供当前身份的三级范围与提交范围的归属校验。
  /// </summary>
  private readonly IamOrganizationScopeResolver scopeResolver;

  /// <summary>
  /// 接收组织范围解析点。
  /// </summary>
  /// <param name="scopeResolver">当前身份与提交范围共用的组织范围解析点。</param>
  internal IamReportScopeResolver(IamOrganizationScopeResolver scopeResolver) => this.scopeResolver = scopeResolver;

  /// <summary>
  /// 读取当前工作身份的组织范围；缺失层级以空值返回，由调用方按展示或业务口径处理。
  /// </summary>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>当前身份的三级范围。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  internal Task<IamOrganizationScope> ReadCurrentScopeAsync(CancellationToken cancellationToken = default) =>
    scopeResolver.ReadCurrentScopeAsync(cancellationToken);

  /// <summary>
  /// 读取当前工作身份的三级组织范围；任一层缺失时按该层文案拒绝，不使用默认值。
  /// </summary>
  /// <param name="missingOrganizationMessage">医共体层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>三层都非空的当前身份组织范围。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">任一层缺失或同一类型出现多个祖先时抛出。</exception>
  internal async Task<IamOrganizationScope> ReadRequiredScopeAsync(
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage,
    CancellationToken cancellationToken = default)
  {
    IamOrganizationScope scope = await scopeResolver.ReadCurrentScopeAsync(cancellationToken);
    if (scope.OrganizationId is null) throw new InvalidOperationException(missingOrganizationMessage);
    if (scope.HospitalId is null) throw new InvalidOperationException(missingHospitalMessage);
    if (scope.BranchId is null) throw new InvalidOperationException(missingBranchMessage);

    return scope;
  }

  /// <summary>
  /// 读取当前工作身份的组织层；只用于只按组织归属的业务（互认项目配置），医院与院区层不参与判定。
  /// </summary>
  /// <param name="missingOrganizationMessage">组织层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>当前身份的组织层范围，同时携带组织实例标识与组织业务编码。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">组织层缺失或同一类型出现多个祖先时抛出。</exception>
  internal async Task<IamOrganizationScope> ReadRequiredOrganizationAsync(string missingOrganizationMessage, CancellationToken cancellationToken = default)
  {
    IamOrganizationScope scope = await scopeResolver.ReadCurrentScopeAsync(cancellationToken);
    if (scope.OrganizationId is null) throw new InvalidOperationException(missingOrganizationMessage);

    return scope;
  }

  /// <summary>
  /// 读取当前工作身份的组织与医院两层；院区层由请求提交并另经归属校验。
  /// </summary>
  /// <param name="missingOrganizationMessage">组织层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层缺失时对外抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>组织与医院两层都非空的当前身份组织范围。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">组织层或医院层缺失，或同一类型出现多个祖先时抛出。</exception>
  internal async Task<IamOrganizationScope> ReadRequiredOrganizationHospitalAsync(
    string missingOrganizationMessage,
    string missingHospitalMessage,
    CancellationToken cancellationToken = default)
  {
    IamOrganizationScope scope = await scopeResolver.ReadCurrentScopeAsync(cancellationToken);
    if (scope.OrganizationId is null) throw new InvalidOperationException(missingOrganizationMessage);
    if (scope.HospitalId is null) throw new InvalidOperationException(missingHospitalMessage);

    return scope;
  }

  /// <summary>
  /// 校验平台入口提交的组织与医院两层，要求两层存在、启用、类型相符且医院父级为该组织。
  /// </summary>
  /// <param name="organizationId">提交的医共体组织实例标识。</param>
  /// <param name="hospitalId">提交的医院组织实例标识。</param>
  /// <param name="missingOrganizationMessage">医共体层不成立时抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>组织与医院两层都非空的组织范围。</returns>
  internal Task<IamOrganizationScope> ResolveSubmittedOrganizationHospitalAsync(
    Guid organizationId,
    Guid hospitalId,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    CancellationToken cancellationToken = default) =>
    scopeResolver.ResolveSubmittedOrganizationHospitalAsync(
      organizationId, hospitalId, missingOrganizationMessage, missingHospitalMessage, cancellationToken);

  /// <summary>
  /// 校验平台入口提交的一条目标组织范围，要求三层存在、启用、类型相符且父子归属成立。
  /// </summary>
  /// <param name="target">请求提交的目标组织范围。</param>
  /// <param name="missingOrganizationMessage">医共体层不成立时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时对外抛出的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区层不成立时对外抛出的业务拒绝文案。</param>
  /// <param name="cancellationToken">取消令牌。</param>
  /// <returns>已校验通过的三级组织范围。</returns>
  /// <exception cref="InvalidOperationException">任一层不存在、已停用、类型不符或父子归属不匹配时抛出。</exception>
  internal async Task<IamOrganizationScope> ResolveSubmittedScopeAsync(
    IamOrganizationScopeTarget target,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage,
    CancellationToken cancellationToken = default)
  {
    IReadOnlyList<IamOrganizationScope> scopes = await scopeResolver.ResolveSubmittedScopesAsync(
      [target], missingOrganizationMessage, missingHospitalMessage, missingBranchMessage, cancellationToken);
    return scopes[0];
  }
}
