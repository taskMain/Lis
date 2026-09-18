namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 一条待校验的目标组织路径，只承载外部组织服务的业务编码，不承载名称。
/// </summary>
/// <remarks>名称一律取外部组织服务的当前值，调用方提交或上下文携带的名称不参与校验，也不作为返回结果。</remarks>
/// <param name="OrganizationCode">目标组织的业务编码；两端空白会被去除。</param>
/// <param name="HospitalCode">目标医院的业务编码，必须属于该组织；两端空白会被去除。</param>
/// <param name="BranchCode">目标院区的业务编码，必须属于该医院；两端空白会被去除。</param>
internal sealed record OrganizationPathTarget(string? OrganizationCode, string? HospitalCode, string? BranchCode);
