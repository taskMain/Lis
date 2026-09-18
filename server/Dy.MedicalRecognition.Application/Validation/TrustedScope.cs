namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 可信范围编码三元组：组织、医院与院区。
/// </summary>
/// <param name="OrganizationCode">去除首尾空白后的可信组织编码，必定非空。</param>
/// <param name="HospitalCode">去除首尾空白后的可信医院编码，必定非空。</param>
/// <param name="BranchCode">去除首尾空白后的可信院区编码；只解析组织与医院两层的入口返回空串。</param>
internal sealed record TrustedScope(string OrganizationCode, string HospitalCode, string BranchCode);
