namespace Dy.MedicalRecognition.Application.Contracts.Queries.OrganizationScope;

/// <summary>
/// 范围选择器的医院选项。
/// </summary>
/// <remarks>
/// 实例标识、名称与业务编码同源，取自同一组织树节点；业务编码取自 IAM 组织节点配置的编码，
/// 该节点未配置业务编码时为空串，不用实例标识文本或名称替代。
/// </remarks>
public sealed record OrganizationScopeHospitalReadModel
{
  /// <summary>
  /// 医院组织实例标识，作为选项的唯一标识。
  /// </summary>
  public Guid OrganizationId { get; init; }

  /// <summary>
  /// 医院名称，取组织树中的名称，用于展示。
  /// </summary>
  public string OrganizationName { get; init; } = string.Empty;

  /// <summary>
  /// 医院组织业务编码，即业务入口提交的医院标识；节点未配置业务编码时为空串。
  /// </summary>
  public string OrganizationCode { get; init; } = string.Empty;

  /// <summary>
  /// 该医院下的院区选项。
  /// </summary>
  public IReadOnlyList<OrganizationScopeBranchReadModel> Branches { get; init; } = [];
}
