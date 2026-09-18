namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 一条已校验通过的组织、医院、院区路径，同时携带三层的业务编码与名称。
/// </summary>
/// <remarks>
/// 业务编码取外部组织服务 <c>Id</c> 去空白后的值，名称取外部组织的 <c>Name</c>；
/// 保存入口用它写入金额行的归属列，查询入口用它返回三层名称，因此两侧对同一路径得到相同结论，
/// 也与既有互认配置表按去空白令牌写入的编码同源。</remarks>
/// <param name="OrganizationCode">已校验通过的组织业务编码。</param>
/// <param name="OrganizationName">组织名称，来源于外部组织服务的组织数据。</param>
/// <param name="HospitalCode">已校验通过且属于该组织的医院业务编码。</param>
/// <param name="HospitalName">医院名称，来源于外部组织服务的医院数据。</param>
/// <param name="BranchCode">已校验通过且属于该医院、该组织的院区业务编码。</param>
/// <param name="BranchName">院区名称，来源于外部组织服务的院区数据。</param>
internal sealed record OrganizationPath(
  string OrganizationCode,
  string OrganizationName,
  string HospitalCode,
  string HospitalName,
  string BranchCode,
  string BranchName);
