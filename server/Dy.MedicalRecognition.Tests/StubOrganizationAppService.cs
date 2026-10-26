namespace Dy.MedicalRecognition.Tests;

/// <summary>外部组织服务的最小共享替身：只实现组织、医院、院区三类读取。</summary>
internal sealed class StubOrganizationAppService : IOrganizationAppService
{
  public List<OrganizationDto> Organizations { get; init; } = [];
  public Dictionary<string, List<HospitalDto>> HospitalsByOrganization { get; } = [];
  public Dictionary<string, List<BranchDto>> BranchesByHospital { get; } = [];
  public int OrganizationReadCount { get; private set; }
  public int HospitalReadCount { get; private set; }
  public int BranchReadCount { get; private set; }
  public Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync() { OrganizationReadCount++; return Task.FromResult<IEnumerable<OrganizationDto>>(Organizations); }
  public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest request) { HospitalReadCount++; return Task.FromResult<IEnumerable<HospitalDto>>(HospitalsByOrganization.TryGetValue(request.OrgId, out List<HospitalDto>? hospitals) ? hospitals : []); }
  public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest request) { BranchReadCount++; return Task.FromResult<IEnumerable<BranchDto>>(BranchesByHospital.TryGetValue(request.HosId, out List<BranchDto>? branches) ? branches : []); }
}
