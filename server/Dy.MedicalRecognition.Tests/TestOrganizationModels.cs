namespace Dy.MedicalRecognition.Tests;

/// <summary>测试替身使用的组织投影。</summary>
public sealed class OrganizationDto
{
  public string Id { get; init; } = string.Empty;
  public string Name { get; init; } = string.Empty;
  public bool IsValid { get; init; }
}

/// <summary>测试替身使用的医院投影。</summary>
public sealed class HospitalDto
{
  public string Id { get; init; } = string.Empty;
  public string Name { get; init; } = string.Empty;
  public string OrgId { get; init; } = string.Empty;
  public bool IsValid { get; init; }
}

/// <summary>测试替身使用的院区投影。</summary>
public sealed class BranchDto
{
  public string Id { get; init; } = string.Empty;
  public string Name { get; init; } = string.Empty;
  public string HosId { get; init; } = string.Empty;
  public string OrgId { get; init; } = string.Empty;
  public bool IsValid { get; init; }
}

/// <summary>测试替身使用的用户档案。</summary>
public sealed class UserDto
{
  public Guid Id { get; init; }
  public string OrgId { get; init; } = string.Empty;
  public string HosId { get; init; } = string.Empty;
  public string? BranchId { get; init; }
  public string? Name { get; init; }
}

/// <summary>测试替身组织读取的最小接口。</summary>
public interface IOrganizationAppService
{
  Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync();
  Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest request);
  Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest request);
}

/// <summary>按组织读取医院的请求。</summary>
public sealed class QueryAllValidHospitalByOrgIdRequest { public string OrgId { get; init; } = string.Empty; }

/// <summary>按医院读取院区的请求。</summary>
public sealed class QueryAllValidBranchByHosIdRequest { public string HosId { get; init; } = string.Empty; }

/// <summary>测试替身用户读取的最小接口。</summary>
public interface IUserAppService
{
  Task<UserDto> GetUserByIdAsync(GetUserByIdRequest request);
}

/// <summary>按用户标识读取档案的请求。</summary>
public sealed class GetUserByIdRequest { public Guid Id { get; init; } }

/// <summary>测试占位类型 QueryAllValidBranchByOrgIdRequest。</summary>
public sealed class QueryAllValidBranchByOrgIdRequest { }

/// <summary>测试占位类型 GetOrganizationByIdRequest。</summary>
public sealed class GetOrganizationByIdRequest { }

/// <summary>测试占位类型 GetHospitalByIdRequest。</summary>
public sealed class GetHospitalByIdRequest { }

/// <summary>测试占位类型 GetBranchByIdRequest。</summary>
public sealed class GetBranchByIdRequest { }

/// <summary>测试占位类型 QueryDeptBySubApplicationIdRequest。</summary>
public sealed class QueryDeptBySubApplicationIdRequest { }

/// <summary>测试占位类型 DeptDto。</summary>
public sealed class DeptDto { }

/// <summary>测试占位类型 QueryDeptByOrgIdRequest。</summary>
public sealed class QueryDeptByOrgIdRequest { }

/// <summary>测试占位类型 QueryDeptByHosIdRequest。</summary>
public sealed class QueryDeptByHosIdRequest { }

/// <summary>测试占位类型 QueryDeptByBranchIdRequest。</summary>
public sealed class QueryDeptByBranchIdRequest { }

/// <summary>测试占位类型 GetDeptByBranchIdAndCodeRequest。</summary>
public sealed class GetDeptByBranchIdAndCodeRequest { }

/// <summary>测试占位类型 GetDeptByIdRequest。</summary>
public sealed class GetDeptByIdRequest { }

/// <summary>测试占位类型 DeptWardDto。</summary>
public sealed class DeptWardDto { }

/// <summary>测试占位类型 GetDeptWardByIdRequest。</summary>
public sealed class GetDeptWardByIdRequest { }

/// <summary>测试占位类型 QueryDeptWardByDeptRequest。</summary>
public sealed class QueryDeptWardByDeptRequest { }

/// <summary>测试占位类型 QueryDeptWardByWardRequest。</summary>
public sealed class QueryDeptWardByWardRequest { }

/// <summary>测试占位类型 WardDto。</summary>
public sealed class WardDto { }

/// <summary>测试占位类型 QueryWardByBranchRequest。</summary>
public sealed class QueryWardByBranchRequest { }

/// <summary>测试占位类型 QueryWardBySubApplicationIdRequest。</summary>
public sealed class QueryWardBySubApplicationIdRequest { }

/// <summary>测试占位类型 GetWardByIdRequest。</summary>
public sealed class GetWardByIdRequest { }

/// <summary>测试占位类型 CreateOrganizationRequest。</summary>
public sealed class CreateOrganizationRequest { }

/// <summary>测试占位类型 UpdateOrganizationRequest。</summary>
public sealed class UpdateOrganizationRequest { }

/// <summary>测试占位类型 DeleteOrganizationRequest。</summary>
public sealed class DeleteOrganizationRequest { }

/// <summary>测试占位类型 EnableOrganizationRequest。</summary>
public sealed class EnableOrganizationRequest { }

/// <summary>测试占位类型 DisableOrganizationRequest。</summary>
public sealed class DisableOrganizationRequest { }

/// <summary>测试占位类型 CreateHospitalRequest。</summary>
public sealed class CreateHospitalRequest { }

/// <summary>测试占位类型 UpdateHospitalRequest。</summary>
public sealed class UpdateHospitalRequest { }

/// <summary>测试占位类型 DeleteHospitalRequest。</summary>
public sealed class DeleteHospitalRequest { }

/// <summary>测试占位类型 EnableHospitalRequest。</summary>
public sealed class EnableHospitalRequest { }

/// <summary>测试占位类型 DisableHospitalRequest。</summary>
public sealed class DisableHospitalRequest { }

/// <summary>测试占位类型 CreateBranchRequest。</summary>
public sealed class CreateBranchRequest { }

/// <summary>测试占位类型 UpdateBranchRequest。</summary>
public sealed class UpdateBranchRequest { }

/// <summary>测试占位类型 DeleteBranchRequest。</summary>
public sealed class DeleteBranchRequest { }

/// <summary>测试占位类型 EnableBranchRequest。</summary>
public sealed class EnableBranchRequest { }

/// <summary>测试占位类型 DisableBranchRequest。</summary>
public sealed class DisableBranchRequest { }

/// <summary>测试占位类型 CreateDeptRequest。</summary>
public sealed class CreateDeptRequest { }

/// <summary>测试占位类型 UpdateDeptRequest。</summary>
public sealed class UpdateDeptRequest { }

/// <summary>测试占位类型 DeleteDeptRequest。</summary>
public sealed class DeleteDeptRequest { }

/// <summary>测试占位类型 EnableDeptRequest。</summary>
public sealed class EnableDeptRequest { }

/// <summary>测试占位类型 DisableDeptRequest。</summary>
public sealed class DisableDeptRequest { }

/// <summary>测试占位类型 MoveDeptUpRequest。</summary>
public sealed class MoveDeptUpRequest { }

/// <summary>测试占位类型 MoveDeptDownRequest。</summary>
public sealed class MoveDeptDownRequest { }

/// <summary>测试占位类型 CreateDeptWardRequest。</summary>
public sealed class CreateDeptWardRequest { }

/// <summary>测试占位类型 UpdateDeptWardRequest。</summary>
public sealed class UpdateDeptWardRequest { }

/// <summary>测试占位类型 DeleteDeptWardRequest。</summary>
public sealed class DeleteDeptWardRequest { }

/// <summary>测试占位类型 EnableDeptWardRequest。</summary>
public sealed class EnableDeptWardRequest { }

/// <summary>测试占位类型 DisableDeptWardRequest。</summary>
public sealed class DisableDeptWardRequest { }

/// <summary>测试占位类型 CreateWardRequest。</summary>
public sealed class CreateWardRequest { }

/// <summary>测试占位类型 UpdateWardRequest。</summary>
public sealed class UpdateWardRequest { }

/// <summary>测试占位类型 DeleteWardRequest。</summary>
public sealed class DeleteWardRequest { }

/// <summary>测试占位类型 EnableWardRequest。</summary>
public sealed class EnableWardRequest { }

/// <summary>测试占位类型 DisableWardRequest。</summary>
public sealed class DisableWardRequest { }

/// <summary>测试占位类型 MoveWardUpRequest。</summary>
public sealed class MoveWardUpRequest { }

/// <summary>测试占位类型 MoveWardDownRequest。</summary>
public sealed class MoveWardDownRequest { }
