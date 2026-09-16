using System.Data.Common;
using Dy.Core.Abstractions.Data;
using Dy.Core.Abstractions.Models;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准项目分类、分组、标准项目与互认项目配置的读写实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态、存在性与名称编码可用性，均由领域层判定；并发下的名称与编码重复由数据库唯一索引兜底。
/// 互认项目配置写入的唯一约束冲突由本层识别为持久化事实并翻译为重复配置异常，是否构成业务拒绝仍由领域层决定。
/// </remarks>
public partial class MedicalRecognitionReportRepository : IMedicalRecognitionReportRepository, IHasDataMapper
{
  /// <summary>
  /// 互认项目配置语句集的作用域名；与 <c>MutualRecognitionItem.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>每个互认配置方法都逐调用传入该作用域名，不使用 <c>SetContext</c> 修改仓储上下文，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string MutualRecognitionItemScope = "MutualRecognitionItem";
  /// <summary>
  /// 唯一约束冲突的 SQLSTATE 值，当前目标数据库 Provider 为 PostgreSQL。
  /// </summary>
  private const string UniqueViolationSqlState = "23505";
  /// <summary>
  /// 分类语句集的作用域名。
  /// </summary>
  private const string MedicalStandardCategoryScope = "MedicalStandardCategory";
  /// <summary>
  /// 分组语句集的作用域名；分组按分类归属维护。
  /// </summary>
  private const string MedicalStandardGroupScope = "MedicalStandardGroup";
  /// <summary>
  /// 标准项目语句集的作用域名。
  /// </summary>
  private const string MedicalStandardItemScope = "MedicalStandardItem";
  private IDataMapper dataMapper = default!;
  /// <summary>
  /// 框架写入的映射器实例，供本仓储的全部读写方法使用。
  /// </summary>
  public IDataMapper DataMapper { get => dataMapper; set => dataMapper = value; }
  /// <inheritdoc/>
  public Guid CreateGuid() => dataMapper.CreateGuid();
  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardCategoryAsync(MedicalStandardCategory value) => await dataMapper.InsertAsync(value, scope: MedicalStandardCategoryScope, sqlId: "CreateMedicalStandardCategory");
  /// <inheritdoc/>
  public async Task<int> UpdateMedicalStandardCategoryAsync(MedicalStandardCategory value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope, sqlId: "UpdateMedicalStandardCategory");
  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope, sqlId: "EnableMedicalStandardCategory");
  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope, sqlId: "DisableMedicalStandardCategory");
  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardGroupAsync(MedicalStandardGroup value) => await dataMapper.InsertAsync(value, scope: MedicalStandardGroupScope, sqlId: "CreateMedicalStandardGroup");
  /// <inheritdoc/>
  public async Task<int> UpdateMedicalStandardGroupAsync(MedicalStandardGroup value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope, sqlId: "UpdateMedicalStandardGroup");
  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope, sqlId: "EnableMedicalStandardGroup");
  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope, sqlId: "DisableMedicalStandardGroup");
  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardItemAsync(MedicalStandardItem value) => await dataMapper.InsertAsync(value, scope: MedicalStandardItemScope, sqlId: "CreateMedicalStandardItem");
  /// <inheritdoc/>
  public async Task<int> ChangeMedicalStandardItemRemarkAsync(MedicalStandardItem value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope, sqlId: "ChangeMedicalStandardItemRemark");
  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope, sqlId: "EnableMedicalStandardItem");
  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope, sqlId: "DisableMedicalStandardItem");
  /// <inheritdoc/>
  public async Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardCategory>(new { Id = id }, scope: MedicalStandardCategoryScope, sqlId: "GetMedicalStandardCategoryById");
  /// <inheritdoc/>
  public async Task<bool> MedicalStandardCategoryNameExistsAsync(string name, Guid? excludedId = null) => await dataMapper.QuerySingleAsync<long>(new { Name = name, ExcludedId = excludedId }, scope: MedicalStandardCategoryScope, sqlId: "MedicalStandardCategoryNameExists") > 0;
  /// <inheritdoc/>
  public async Task<bool> MedicalStandardCategoryHasGroupsAsync(Guid categoryId) => await dataMapper.QuerySingleAsync<long>(new { CategoryId = categoryId }, scope: MedicalStandardCategoryScope, sqlId: "MedicalStandardCategoryHasGroups") > 0;
  /// <inheritdoc/>
  public async Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardGroup>(new { Id = id }, scope: MedicalStandardGroupScope, sqlId: "GetMedicalStandardGroupById");
  /// <inheritdoc/>
  public async Task<bool> MedicalStandardGroupNameExistsAsync(Guid categoryId, string name, Guid? excludedId = null) => await dataMapper.QuerySingleAsync<long>(new { CategoryId = categoryId, Name = name, ExcludedId = excludedId }, scope: MedicalStandardGroupScope, sqlId: "MedicalStandardGroupNameExists") > 0;
  /// <inheritdoc/>
  public async Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardItem>(new { Id = id }, scope: MedicalStandardItemScope, sqlId: "GetMedicalStandardItemById");
  /// <inheritdoc/>
  public async Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code) => await dataMapper.QuerySingleAsync<MedicalStandardItem>(new { Code = code }, scope: MedicalStandardItemScope, sqlId: "GetMedicalStandardItemByCode");
  /// <inheritdoc/>
  public async Task<bool> MedicalStandardItemCodeExistsAsync(string code) => await dataMapper.QuerySingleAsync<long>(new { Code = code }, scope: MedicalStandardItemScope, sqlId: "MedicalStandardItemCodeExists") > 0;
  /// <summary>
  /// 插入一条互认项目配置记录，并把唯一约束冲突翻译为领域可识别的重复配置异常。
  /// </summary>
  /// <param name="value">待保存的互认项目配置实体，含组织编码、关联的标准项目标识与编码、可互认时间天数。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  /// <exception cref="DuplicateMutualRecognitionItemException">写入违反“组织编码 + 标准项目编码”唯一约束时抛出。</exception>
  public async Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem value)
  {
    try
    {
      return await dataMapper.InsertAsync(value, scope: MutualRecognitionItemScope, sqlId: "CreateMutualRecognitionItem");
    }
    catch (Exception exception) when (IsUniqueConstraintViolation(exception))
    {
      // 唯一约束冲突是持久化事实，这里只翻译异常类型；重复配置的业务文案由领域层给出。
      throw new DuplicateMutualRecognitionItemException("同一组织下已存在同一标准项目的互认项目配置。", exception);
    }
  }
  /// <summary>
  /// 在当前组织范围内按配置标识读取互认项目配置。
  /// </summary>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">可信组织编码，其他组织的同标识配置不会被返回。</param>
  /// <returns>互认项目配置实体；配置不存在或不属于该组织时返回 <see langword="null"/>。</returns>
  public async Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode) => await dataMapper.QuerySingleAsync<MutualRecognitionItem>(new { Id = id, OrganizationCode = organizationCode }, scope: MutualRecognitionItemScope, sqlId: "GetMutualRecognitionItemById");
  /// <summary>
  /// 按配置标识与可信组织更新可互认时间天数和操作字段。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织、本次可互认时间天数与操作字段的实体。</param>
  /// <returns>受影响行数：命中该组织内的该配置为 1，配置不存在、越组织或天数相同不会阻止更新；未命中为 0。</returns>
  public async Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope, sqlId: "UpdateMutualRecognitionItemConfiguration");
  /// <summary>
  /// 将指定互认项目配置由停用改为启用。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织与操作字段的启用命令。</param>
  /// <returns>受影响行数：该组织内该配置当前为停用态并更新成功为 1；已处于启用态、配置不存在或越组织为 0，调用方据此区分幂等与并发结果。</returns>
  public async Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope, sqlId: "EnableMutualRecognitionItem");
  /// <summary>
  /// 将指定互认项目配置由启用改为停用。
  /// </summary>
  /// <param name="value">携带配置标识、可信组织与操作字段的停用命令。</param>
  /// <returns>受影响行数：该组织内该配置当前为启用态并更新成功为 1；已处于停用态、配置不存在或越组织为 0，调用方据此区分幂等与并发结果。</returns>
  public async Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MutualRecognitionItemScope, sqlId: "DisableMutualRecognitionItem");
  /// <summary>
  /// 判断数据映射器抛出的异常是否由唯一约束冲突引起。
  /// </summary>
  /// <remarks>
  /// 框架的数据映射器把数据库异常统一包装为 <c>DBException</c>，原始异常保留在内部异常链上，
  /// 因此必须沿内部异常链查找框架中立的数据库异常类型，再按 SQLSTATE 判断冲突，
  /// 不在本仓储引用具体数据库 Provider 的类型或错误码。
  /// 当前目标数据库 Provider 的唯一约束冲突 SQLSTATE 为 23505；其他 Provider 未提供该值时按普通数据库错误继续向外传播。
  /// </remarks>
  /// <param name="exception">数据映射器抛出的异常。</param>
  /// <returns>内部异常链上存在唯一约束冲突时为 <see langword="true"/>。</returns>
  private static bool IsUniqueConstraintViolation(Exception exception)
  {
    for (Exception? current = exception; current is not null; current = current.InnerException)
    {
      if (current is DbException databaseException && databaseException.SqlState == UniqueViolationSqlState) return true;
    }

    return false;
  }
}
