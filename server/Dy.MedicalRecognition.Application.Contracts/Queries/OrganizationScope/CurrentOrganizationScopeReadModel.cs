namespace Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

/// <summary>
/// 当前身份的组织范围：医共体、医院、院区三级的组织实例标识、名称与组织业务编码。
/// </summary>
/// <remarks>
/// 三级都取自当前工作身份所在组织及其祖先链，调用方不能通过请求指定；某一级在链上不存在或未配置类型时该项为空，
/// 由展示层按「未知」处理，不用名称、层级深度或默认值补齐。
/// 三级的实例标识、名称与业务编码同源，取自同一批 IAM 组织节点；组织未配置业务编码时该项为空，
/// 既不用实例标识文本补齐，也不用名称补齐，调用方按空值处理。
/// </remarks>
public sealed record CurrentOrganizationScopeReadModel
{
  /// <summary>
  /// 医共体组织实例标识；当前身份的祖先链上没有该类别时为空。
  /// </summary>
  public Guid? OrganizationId { get; init; }

  /// <summary>
  /// 医共体名称；与 <see cref="OrganizationId"/> 同源，缺失时为空。
  /// </summary>
  public string? OrganizationName { get; init; }

  /// <summary>
  /// 医共体组织业务编码，即业务入口提交的组织标识；与 <see cref="OrganizationId"/> 同源，
  /// 该层缺失或组织未配置业务编码时为空。
  /// </summary>
  public string? OrganizationCode { get; init; }

  /// <summary>
  /// 医院组织实例标识；当前身份的祖先链上没有该类别时为空。
  /// </summary>
  public Guid? HospitalId { get; init; }

  /// <summary>
  /// 医院名称；与 <see cref="HospitalId"/> 同源，缺失时为空。
  /// </summary>
  public string? HospitalName { get; init; }

  /// <summary>
  /// 医院组织业务编码，即业务入口提交的医院标识；与 <see cref="HospitalId"/> 同源，
  /// 该层缺失或组织未配置业务编码时为空。
  /// </summary>
  public string? HospitalCode { get; init; }

  /// <summary>
  /// 院区组织实例标识；当前身份的祖先链上没有该类别时为空。
  /// </summary>
  public Guid? BranchId { get; init; }

  /// <summary>
  /// 院区名称；与 <see cref="BranchId"/> 同源，缺失时为空。
  /// </summary>
  public string? BranchName { get; init; }

  /// <summary>
  /// 院区组织业务编码，即业务入口提交的院区标识；与 <see cref="BranchId"/> 同源，
  /// 该层缺失或组织未配置业务编码时为空。
  /// </summary>
  public string? BranchCode { get; init; }
}
