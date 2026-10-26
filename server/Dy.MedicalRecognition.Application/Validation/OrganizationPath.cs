using Dy.MedicalRecognition.Application.OrganizationScope;

namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 一条已校验通过的组织、医院、院区路径，每层同时携带实例标识、名称与组织业务编码。
/// </summary>
/// <remarks>
/// 实例标识用于 IAM 目录读取与业务表的实例标识列；业务编码是 IAM 组织节点上配置的编码，用于业务表的编码列与按编码比对。
/// 两者同源但不可互相替代：编码为 <see langword="null"/> 的节点在路径上按空串承载，不用实例标识、名称或层级前缀推导替代。
/// 未参与解析的层级，其实例标识为空 Guid、名称与编码为空串。
/// 组织路径解析点校验提交编码时只判存在性、类型与能定论的父子归属，不为数据用途挑选节点：
/// 该入口返回的路径实例标识为空，编码与名称按提交值与容错取名口径填充；实例标识只在按会话身份解析范围时才有值。
/// </remarks>
/// <param name="OrganizationId">医共体组织实例标识。</param>
/// <param name="OrganizationName">医共体名称。</param>
/// <param name="OrganizationCode">医共体组织业务编码。</param>
/// <param name="HospitalId">医院组织实例标识。</param>
/// <param name="HospitalName">医院名称。</param>
/// <param name="HospitalCode">医院组织业务编码。</param>
/// <param name="BranchId">院区组织实例标识。</param>
/// <param name="BranchName">院区名称。</param>
/// <param name="BranchCode">院区组织业务编码。</param>
internal sealed record OrganizationPath(
  Guid OrganizationId,
  string OrganizationName,
  string OrganizationCode,
  Guid HospitalId,
  string HospitalName,
  string HospitalCode,
  Guid BranchId,
  string BranchName,
  string BranchCode)
{
  /// <summary>
  /// 按已解析的组织范围装配路径：实例标识与名称取自 IAM 目录，业务编码取自组织节点，节点未配置编码的层为空串。
  /// </summary>
  /// <param name="scope">已解析的组织范围。</param>
  /// <returns>该范围对应的组织路径。</returns>
  internal static OrganizationPath FromScope(IamOrganizationScope scope) => new(
    scope.OrganizationId ?? Guid.Empty,
    scope.OrganizationName ?? string.Empty,
    scope.OrganizationCode ?? string.Empty,
    scope.HospitalId ?? Guid.Empty,
    scope.HospitalName ?? string.Empty,
    scope.HospitalCode ?? string.Empty,
    scope.BranchId ?? Guid.Empty,
    scope.BranchName ?? string.Empty,
    scope.BranchCode ?? string.Empty);
}
