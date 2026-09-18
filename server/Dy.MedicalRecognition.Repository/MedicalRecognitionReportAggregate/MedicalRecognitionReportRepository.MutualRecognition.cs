using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认项目配置的读写实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态、存在性与配置唯一性，均由领域层判定；
/// 并发写入违反“组织编码 + 标准项目编码”唯一约束时由数据库唯一索引拒绝，数据库异常按原样向外传播。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 互认项目配置语句集的作用域名；与 <c>MutualRecognitionItem.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>每个互认配置方法都逐调用传入该作用域名，不使用 <c>SetContext</c> 修改仓储上下文，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string MutualRecognitionItemScope = "MutualRecognitionItem";

  /// <summary>
  /// 插入一条互认项目配置记录。
  /// </summary>
  /// <param name="value">待保存的互认项目配置实体，含组织编码、关联的标准项目标识与编码、可互认时间天数。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem value) =>
    await dataMapper.InsertAsync(value, scope: MutualRecognitionItemScope);

  /// <summary>
  /// 在当前组织范围内按配置标识读取互认项目配置。
  /// </summary>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">可信组织编码，其他组织的同标识配置不会被返回。</param>
  /// <returns>互认项目配置实体；配置不存在或不属于该组织时返回 <see langword="null"/>。</returns>
  public async Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode) => await dataMapper.QuerySingleAsync<MutualRecognitionItem>(new { Id = id, OrganizationCode = organizationCode }, scope: MutualRecognitionItemScope);

  /// <summary>
  /// 按配置标识与可信组织更新可互认时间天数和操作字段。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织、本次可互认时间天数与操作字段的实体。</param>
  /// <returns>受影响行数：命中该组织内的该配置为 1，配置不存在、越组织或天数相同不会阻止更新；未命中为 0。</returns>
  public async Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope);

  /// <summary>
  /// 将指定互认项目配置由停用改为启用。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织与操作字段的启用命令。</param>
  /// <returns>受影响行数：该组织内该配置当前为停用态并更新成功为 1；已处于启用态、配置不存在或越组织为 0，调用方据此区分幂等与并发结果。</returns>
  public async Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope);

  /// <summary>
  /// 将指定互认项目配置由启用改为停用。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织与操作字段的停用命令。</param>
  /// <returns>受影响行数：该组织内该配置当前为启用态并更新成功为 1；已处于停用态、配置不存在或越组织为 0，调用方据此区分幂等与并发结果。</returns>
  public async Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope);

  /// <summary>
  /// 按组织编码与标准项目编码读取互认项目配置，供金额保存前校验保存前提。
  /// </summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>互认项目配置实体；未建立时返回 <see langword="null"/>。</returns>
  public async Task<MutualRecognitionItem?> GetMutualRecognitionItemByOrganizationAndProjectAsync(string organizationCode, string standardProjectCode) => await dataMapper.QuerySingleAsync<MutualRecognitionItem>(new { OrganizationCode = organizationCode, StandardProjectCode = standardProjectCode }, scope: MutualRecognitionItemScope);
}
