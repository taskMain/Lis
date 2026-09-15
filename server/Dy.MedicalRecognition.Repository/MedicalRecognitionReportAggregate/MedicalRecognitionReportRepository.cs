using Dy.Core.Abstractions.Data;
using Dy.Core.Abstractions.Models;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准项目分类、分组、标准项目与互认项目配置的读写实现。
/// </summary>
/// <remarks>只发映射文件中已有的语句，不判断业务状态、存在性与名称编码可用性，均由领域层判定；并发下的名称与编码重复由数据库唯一索引兜底。</remarks>
public partial class MedicalRecognitionReportRepository : IMedicalRecognitionReportRepository, IHasDataMapper
{
  /// <summary>
  /// 互认项目配置语句集的作用域名；与识别报告共用同一映射文件。
  /// </summary>
  private const string MedicalRecognitionReportScope = "MedicalRecognitionReport";
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
  public async Task<bool> MedicalStandardItemCodeExistsAsync(string code) => await dataMapper.QuerySingleAsync<long>(new { Code = code }, scope: MedicalStandardItemScope, sqlId: "MedicalStandardItemCodeExists") > 0;
  /// <summary>
  /// 插入一条互认项目配置记录，并记录本次操作人。
  /// </summary>
  /// <param name="value">待保存的互认项目配置实体，含组织编码、关联的标准项目标识与编码、可互认时间天数。</param>
  /// <returns>受影响行数，插入成功为 1；调用方以大于 0 判定写入成功。</returns>
  public async Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem value) => await dataMapper.InsertAsync(value, scope: MedicalRecognitionReportScope, sqlId: "CreateMutualRecognitionItem");
  /// <summary>
  /// 按配置标识更新可互认时间天数；不改变组织编码与启用状态。
  /// </summary>
  /// <param name="value">携带配置标识、本次可互认时间天数与操作人的实体。</param>
  /// <returns>受影响行数：命中该配置为 1，配置不存在为 0。可互认时间天数或操作人为空值时对应字段不参与更新，因此该语句无法把天数改为 0。</returns>
  public async Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem value) => await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope, sqlId: "UpdateMutualRecognitionItemConfiguration");
  /// <summary>
  /// 将指定互认项目配置由停用改为启用。
  /// </summary>
  /// <param name="value">携带配置标识与本次操作人、操作时间的启用命令。</param>
  /// <returns>受影响行数：命中该配置为 1，配置不存在为 0；与其他目录对象的启停不同，本语句不带“当前必须为停用态”的条件，重复启用同样会刷新操作人与操作时间。</returns>
  public async Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope, sqlId: "EnableMutualRecognitionItem");
  /// <summary>
  /// 将指定互认项目配置由启用改为停用。
  /// </summary>
  /// <param name="value">携带配置标识与本次操作人、操作时间的停用命令。</param>
  /// <returns>受影响行数：命中该配置为 1，配置不存在为 0；与其他目录对象的启停不同，本语句不带“当前必须为启用态”的条件，重复停用同样会刷新操作人与操作时间。</returns>
  public async Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalRecognitionReportScope, sqlId: "DisableMutualRecognitionItem");
}
