using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 组织医院院区互认项目金额的读写实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态与业务键存在性，均由领域层判定；
/// 并发写入违反四个业务键的唯一约束时由数据库唯一索引拒绝，数据库异常按原样向外传播。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 组织医院院区互认项目金额语句集的作用域名；与 <c>OrganizationHospitalBranchRecognitionAmount.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  private const string OrganizationHospitalBranchRecognitionAmountScope = "OrganizationHospitalBranchRecognitionAmount";

  /// <summary>
  /// 按四个业务键读取单条金额记录。
  /// </summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>金额记录实体；该业务键尚无记录时返回 <see langword="null"/>。</returns>
  public async Task<OrganizationHospitalBranchRecognitionAmount?> GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, string standardProjectCode) =>
    await dataMapper.QuerySingleAsync<OrganizationHospitalBranchRecognitionAmount>(
      new { OrganizationCode = organizationCode, HospitalCode = hospitalCode, BranchCode = branchCode, StandardProjectCode = standardProjectCode },
      scope: OrganizationHospitalBranchRecognitionAmountScope);

  /// <summary>
  /// 插入一条金额记录。
  /// </summary>
  /// <param name="value">待保存的金额记录实体，含四个业务键、当前金额与操作字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount value) =>
    await dataMapper.InsertAsync(value, scope: OrganizationHospitalBranchRecognitionAmountScope);

  /// <summary>
  /// 按记录标识与四个业务键列条件更新当前金额与操作字段。
  /// </summary>
  /// <param name="value">携带记录标识、四个业务键、本次金额与操作字段的实体。</param>
  /// <returns>受影响行数：同时命中主键与四个业务键列为 1；记录不存在、业务键被改动或并发改动定位条件时为 0。</returns>
  public async Task<int> UpdateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount value) =>
    await dataMapper.UpdateAsync(value, scope: OrganizationHospitalBranchRecognitionAmountScope);
}
