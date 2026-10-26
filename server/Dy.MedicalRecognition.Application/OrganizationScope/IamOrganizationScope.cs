namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 组织范围：医共体、医院、院区三级的 IAM 组织实例标识、名称与业务编码。
/// </summary>
/// <remarks>
/// 某一级在祖先链上不存在、已停用或未配置类型时对应字段为空，不用名称、层级深度或默认值补齐。
/// 业务编码取自 IAM 组织节点，与该级实例标识同源；组织未配置业务编码时对应编码为空，不用标识或名称替代。
/// </remarks>
/// <param name="OrganizationId">医共体组织实例标识；缺失时为空。</param>
/// <param name="OrganizationName">医共体名称；与标识同源，缺失时为空。</param>
/// <param name="OrganizationCode">医共体组织业务编码；未配置时为空。</param>
/// <param name="HospitalId">医院组织实例标识；缺失时为空。</param>
/// <param name="HospitalName">医院名称；缺失时为空。</param>
/// <param name="HospitalCode">医院组织业务编码；未配置时为空。</param>
/// <param name="BranchId">院区组织实例标识；缺失时为空。</param>
/// <param name="BranchName">院区名称；缺失时为空。</param>
/// <param name="BranchCode">院区组织业务编码；未配置时为空。</param>
internal sealed record IamOrganizationScope(
  Guid? OrganizationId,
  string? OrganizationName,
  string? OrganizationCode,
  Guid? HospitalId,
  string? HospitalName,
  string? HospitalCode,
  Guid? BranchId,
  string? BranchName,
  string? BranchCode)
{
  /// <summary>
  /// 三级都未解析出时的空范围。
  /// </summary>
  internal static IamOrganizationScope Empty { get; } = new(null, null, null, null, null, null, null, null, null);
}
