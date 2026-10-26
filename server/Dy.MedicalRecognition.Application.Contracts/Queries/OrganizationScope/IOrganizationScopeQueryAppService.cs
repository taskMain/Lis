namespace Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

/// <summary>
/// 组织范围只读查询：当前身份的三级范围与平台入口的可选范围。
/// </summary>
/// <remarks>
/// 两个入口都同时返回组织实例标识、名称与组织业务编码：业务编码取自 IAM 组织节点配置的编码，
/// 组织未配置业务编码时为空，不用实例标识文本或名称替代。
/// </remarks>
public interface IOrganizationScopeQueryAppService : IApplicationService
{
  /// <summary>
  /// 查询当前工作身份的组织范围三级实例标识、名称与业务编码。
  /// </summary>
  /// <returns>当前身份的三级范围；链上不存在的层级对应字段为空，组织未配置业务编码时该层编码为空。</returns>
  /// <exception cref="Dy.IAM.Sdk.IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="Dy.IAM.Sdk.IamWorkIdentityRequiredException">请求未选择工作身份时抛出，由 IAM 错误处理呈现 409。</exception>
  Task<CurrentOrganizationScopeReadModel> QueryCurrentOrganizationScopeAsync();

  /// <summary>
  /// 按当前租户查询平台入口范围选择器的三级选项。
  /// </summary>
  /// <returns>医共体、医院、院区三级选项与未配置类型的节点数量；每个选项都带实例标识、名称与业务编码。</returns>
  /// <exception cref="Dy.IAM.Sdk.IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="Dy.IAM.Sdk.IamWorkIdentityRequiredException">请求未选择工作身份时抛出，由 IAM 错误处理呈现 409。</exception>
  Task<OrganizationScopeOptionsReadModel> QueryOrganizationScopeOptionsAsync();
}
