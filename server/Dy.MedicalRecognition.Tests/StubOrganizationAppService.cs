using Dy.Base.Application.Contracts.OrganizationAggregate;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 外部组织服务的最小共享替身：只实现组织路径解析实际使用的三类读取，其余成员调用即失败。
/// </summary>
/// <remarks>
/// 阶段 2 的写入口用例只通过构造函数注入组织服务、不会调用它；阶段 3 的写入口用例自带可脚本化的替身。
/// 该替身存在的唯一目的是让不使用组织服务的用例仍能构造写入口，因此未实现的成员一律抛出，
/// 避免用例在未覆盖的路径上静默走过。
/// </remarks>
internal sealed class StubOrganizationAppService : IOrganizationAppService
{
  /// <summary>组织读取返回的启用组织集合；本替身默认为空。</summary>
  public List<OrganizationDto> Organizations { get; init; } = [];

  /// <summary>按组织编码预置的医院读取结果；本替身默认为空。</summary>
  public Dictionary<string, List<HospitalDto>> HospitalsByOrganization { get; } = [];

  /// <summary>按医院编码预置的院区读取结果；本替身默认为空。</summary>
  public Dictionary<string, List<BranchDto>> BranchesByHospital { get; } = [];

  /// <summary>组织全量读取的调用次数。</summary>
  public int OrganizationReadCount { get; private set; }

  /// <summary>医院读取的调用次数；按组织路径解析口径，每个不同组织一次。</summary>
  public int HospitalReadCount { get; private set; }

  /// <summary>院区读取的调用次数；按组织路径解析口径，每家不同医院一次。</summary>
  public int BranchReadCount { get; private set; }

  /// <inheritdoc/>
  public Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync()
  {
    OrganizationReadCount++;
    return Task.FromResult<IEnumerable<OrganizationDto>>(Organizations);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest queryAllValidHospitalByOrgIdRequest)
  {
    HospitalReadCount++;
    return Task.FromResult<IEnumerable<HospitalDto>>(HospitalsByOrganization.TryGetValue(queryAllValidHospitalByOrgIdRequest.OrgId, out List<HospitalDto>? hospitals) ? hospitals : []);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest queryAllValidBranchByHosIdRequest)
  {
    BranchReadCount++;
    return Task.FromResult<IEnumerable<BranchDto>>(BranchesByHospital.TryGetValue(queryAllValidBranchByHosIdRequest.HosId, out List<BranchDto>? branches) ? branches : []);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<BranchDto>> QueryAllValidBranchByOrgIdAsync(QueryAllValidBranchByOrgIdRequest queryAllValidBranchByOrgIdRequest) => Unsupported<IEnumerable<BranchDto>>();

  /// <inheritdoc/>
  public Task<OrganizationDto> GetOrganizationByIdAsync(GetOrganizationByIdRequest getOrganizationByIdRequest) => Unsupported<OrganizationDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<HospitalDto>> QueryAllHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

  /// <inheritdoc/>
  public Task<HospitalDto> GetHospitalByIdAsync(GetHospitalByIdRequest getHospitalByIdRequest) => Unsupported<HospitalDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<BranchDto>> QueryAllBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<BranchDto>> QueryAllValidBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

  /// <inheritdoc/>
  public Task<BranchDto> GetBranchByIdAsync(GetBranchByIdRequest getBranchByIdRequest) => Unsupported<BranchDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptDto>> QueryDeptBySubApplicationIdAsync(QueryDeptBySubApplicationIdRequest queryDeptBySubApplicationIdRequest) => Unsupported<IEnumerable<DeptDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptDto>> QueryDeptByOrgIdAsync(QueryDeptByOrgIdRequest queryDeptByOrgIdRequest) => Unsupported<IEnumerable<DeptDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptDto>> QueryDeptByHosIdAsync(QueryDeptByHosIdRequest queryDeptByHosIdRequest) => Unsupported<IEnumerable<DeptDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptDto>> QueryDeptByBranchIdAsync(QueryDeptByBranchIdRequest queryDeptByBranchIdRequest) => Unsupported<IEnumerable<DeptDto>>();

  /// <inheritdoc/>
  public Task<DeptDto> GetDeptByBranchIdAndCodeAsync(GetDeptByBranchIdAndCodeRequest getDeptByBranchIdAndCodeRequest) => Unsupported<DeptDto>();

  /// <inheritdoc/>
  public Task<DeptDto> GetDeptByIdAsync(GetDeptByIdRequest getDeptByIdRequest) => Unsupported<DeptDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptDto>> QueryAllDeptAsync() => Unsupported<IEnumerable<DeptDto>>();

  /// <inheritdoc/>
  public Task<DeptWardDto> GetDeptWardByIdAsync(GetDeptWardByIdRequest getDeptWardByIdRequest) => Unsupported<DeptWardDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptWardDto>> QueryDeptWardByDeptAsync(QueryDeptWardByDeptRequest queryDeptWardByDeptRequest) => Unsupported<IEnumerable<DeptWardDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<DeptWardDto>> QueryDeptWardByWardAsync(QueryDeptWardByWardRequest queryDeptWardByWardRequest) => Unsupported<IEnumerable<DeptWardDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<WardDto>> QueryWardByBranchAsync(QueryWardByBranchRequest queryWardByBranchRequest) => Unsupported<IEnumerable<WardDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<WardDto>> QueryWardBySubApplicationIdAsync(QueryWardBySubApplicationIdRequest queryWardBySubApplicationIdRequest) => Unsupported<IEnumerable<WardDto>>();

  /// <inheritdoc/>
  public Task<WardDto> GetWardByIdAsync(GetWardByIdRequest getWardByIdRequest) => Unsupported<WardDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<WardDto>> QueryAllWardAsync() => Unsupported<IEnumerable<WardDto>>();

  /// <inheritdoc/>
  public Task<string> CreateOrganizationAsync(CreateOrganizationRequest createOrganizationRequest) => Unsupported<string>();

  /// <inheritdoc/>
  public Task<int> UpdateOrganizationAsync(UpdateOrganizationRequest updateOrganizationRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteOrganizationAsync(DeleteOrganizationRequest deleteOrganizationRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableOrganizationAsync(EnableOrganizationRequest enableOrganizationRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableOrganizationAsync(DisableOrganizationRequest disableOrganizationRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<string> CreateHospitalAsync(CreateHospitalRequest createHospitalRequest) => Unsupported<string>();

  /// <inheritdoc/>
  public Task<int> UpdateHospitalAsync(UpdateHospitalRequest updateHospitalRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteHospitalAsync(DeleteHospitalRequest deleteHospitalRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableHospitalAsync(EnableHospitalRequest enableHospitalRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableHospitalAsync(DisableHospitalRequest disableHospitalRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<string> CreateBranchAsync(CreateBranchRequest createBranchRequest) => Unsupported<string>();

  /// <inheritdoc/>
  public Task<int> UpdateBranchAsync(UpdateBranchRequest updateBranchRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteBranchAsync(DeleteBranchRequest deleteBranchRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableBranchAsync(EnableBranchRequest enableBranchRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableBranchAsync(DisableBranchRequest disableBranchRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<Guid> CreateDeptAsync(CreateDeptRequest createDeptRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> UpdateDeptAsync(UpdateDeptRequest updateDeptRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteDeptAsync(DeleteDeptRequest deleteDeptRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableDeptAsync(EnableDeptRequest enableDeptRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableDeptAsync(DisableDeptRequest disableDeptRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> MoveDeptUpAsync(MoveDeptUpRequest moveDeptUpRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> MoveDeptDownAsync(MoveDeptDownRequest moveDeptDownRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<Guid> CreateDeptWardAsync(CreateDeptWardRequest createDeptWardRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> UpdateDeptWardAsync(UpdateDeptWardRequest updateDeptWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteDeptWardAsync(DeleteDeptWardRequest deleteDeptWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableDeptWardAsync(EnableDeptWardRequest enableDeptWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableDeptWardAsync(DisableDeptWardRequest disableDeptWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<Guid> CreateWardAsync(CreateWardRequest createWardRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> UpdateWardAsync(UpdateWardRequest updateWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteWardAsync(DeleteWardRequest deleteWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> EnableWardAsync(EnableWardRequest enableWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableWardAsync(DisableWardRequest disableWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> MoveWardUpAsync(MoveWardUpRequest moveWardUpRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> MoveWardDownAsync(MoveWardDownRequest moveWardDownRequest) => Unsupported<int>();

  /// <summary>本替身未实现该外部组织服务成员。</summary>
  /// <typeparam name="TResult">成员返回类型。</typeparam>
  /// <returns>不会返回。</returns>
  /// <exception cref="NotSupportedException">该成员被调用时抛出。</exception>
  private static Task<TResult> Unsupported<TResult>() => throw new NotSupportedException("共享替身只实现组织路径解析使用的组织、医院与院区读取。");
}
