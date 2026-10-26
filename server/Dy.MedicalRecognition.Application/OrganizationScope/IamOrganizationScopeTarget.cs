namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// 平台入口提交的一条目标组织范围：医共体、医院、院区三级组织实例标识。
/// </summary>
/// <param name="OrganizationId">请求提交的医共体组织实例标识。</param>
/// <param name="HospitalId">请求提交的医院组织实例标识。</param>
/// <param name="BranchId">请求提交的院区组织实例标识。</param>
internal readonly record struct IamOrganizationScopeTarget(Guid OrganizationId, Guid HospitalId, Guid BranchId);
