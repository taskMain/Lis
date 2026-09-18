namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 组织医院院区互认项目金额的读写契约。
/// </summary>
/// <remarks>只做数据读写并返回行数或查询结果，不判断业务状态。</remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 按组织、医院、院区与标准项目四个业务键读取单条金额记录。
  /// </summary>
  /// <remarks>
  /// 供领域层在"新增"与"更新"之间二选一，并在条件更新影响 0 行时复读一次；
  /// 业务键上存在唯一索引，因此至多命中一行；不存在时返回 <see langword="null"/>。
  /// </remarks>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>金额记录。</returns>
  Task<OrganizationHospitalBranchRecognitionAmount?> GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, string standardProjectCode);

  /// <summary>
  /// 插入一条组织医院院区互认项目金额记录。
  /// </summary>
  /// <remarks>四个业务键与当前金额取实体携带值，操作时间取命令时间；并发首次保存同一业务键由四列唯一索引拒绝，数据库异常按原样向外传播。</remarks>
  /// <param name="organizationHospitalBranchRecognitionAmount">待插入的金额记录实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount);

  /// <summary>
  /// 按记录标识与四个业务键列条件更新该条金额的当前金额与操作字段。
  /// </summary>
  /// <remarks>
  /// 业务键列不在变更范围内：记录已被删除、业务键被改动或并发改动定位条件时都影响 0 行，
  /// 调用方据此区分并发冲突与业务拒绝；条件同时包含主键与四个业务键列，避免标识命中却落到别的归属上。
  /// </remarks>
  /// <param name="organizationHospitalBranchRecognitionAmount">携带记录标识、四个业务键、本次金额与操作字段的实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount);
}
