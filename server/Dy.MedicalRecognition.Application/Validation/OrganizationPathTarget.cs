namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 一条待校验的目标组织路径：三层承载调用方提交的组织业务编码。
/// </summary>
/// <remarks>
/// 提交值就是业务编码，不按实例标识解析字符串，也不从编码的形态或前缀推导层级；层级关系只由 IAM 目录的父组织标识决定。
/// 编码是业务键：同一编码在 IAM 目录中可以对应多个组织节点，因此本类型不承载实例标识，
/// 解析结果也不为数据用途回填实例标识，避免把某一个候选当成已定论的唯一节点。
/// 某层编码为空表示该层不参与本次路径校验；名称一律按容错口径另取，不在本类型上承载。
/// </remarks>
internal sealed record OrganizationPathTarget
{
  /// <summary>组织业务编码；为空表示该层不参与本次校验。</summary>
  internal string? OrganizationCode { get; init; }

  /// <summary>医院业务编码；为空表示该层不参与本次校验。</summary>
  internal string? HospitalCode { get; init; }

  /// <summary>院区业务编码；为空表示该层不参与本次校验。</summary>
  internal string? BranchCode { get; init; }

  /// <summary>
  /// 以三层业务编码构造目标路径。
  /// </summary>
  /// <param name="organizationCode">组织业务编码。</param>
  /// <param name="hospitalCode">医院业务编码。</param>
  /// <param name="branchCode">院区业务编码。</param>
  internal OrganizationPathTarget(string? organizationCode, string? hospitalCode, string? branchCode)
  {
    OrganizationCode = organizationCode;
    HospitalCode = hospitalCode;
    BranchCode = branchCode;
  }
}
