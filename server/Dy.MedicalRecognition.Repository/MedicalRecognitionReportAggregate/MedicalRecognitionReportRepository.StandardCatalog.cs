using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准项目分类、分组与标准项目的读写实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态、存在性与名称编码可用性，均由领域层判定；
/// 并发下的名称与编码重复由数据库唯一索引兜底。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
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

  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardCategoryAsync(MedicalStandardCategory value) => await dataMapper.InsertAsync(value, scope: MedicalStandardCategoryScope);

  /// <inheritdoc/>
  public async Task<int> UpdateMedicalStandardCategoryAsync(MedicalStandardCategory value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope);

  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope);

  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardCategoryScope);

  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardGroupAsync(MedicalStandardGroup value) => await dataMapper.InsertAsync(value, scope: MedicalStandardGroupScope);

  /// <inheritdoc/>
  public async Task<int> UpdateMedicalStandardGroupAsync(MedicalStandardGroup value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope);

  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope);

  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardGroupScope);

  /// <inheritdoc/>
  public async Task<int> CreateMedicalStandardItemAsync(MedicalStandardItem value) => await dataMapper.InsertAsync(value, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<int> ChangeMedicalStandardItemRemarkAsync(MedicalStandardItem value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<int> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<int> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand value) => await dataMapper.UpdateAsync(value, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardCategory>(new { Id = id }, scope: MedicalStandardCategoryScope);

  /// <inheritdoc/>
  public async Task<bool> MedicalStandardCategoryNameExistsAsync(string name, Guid? excludedId = null) => await dataMapper.QuerySingleAsync<long>(new { Name = name, ExcludedId = excludedId }, scope: MedicalStandardCategoryScope) > 0;

  /// <inheritdoc/>
  public async Task<bool> MedicalStandardCategoryHasGroupsAsync(Guid categoryId) => await dataMapper.QuerySingleAsync<long>(new { CategoryId = categoryId }, scope: MedicalStandardCategoryScope) > 0;

  /// <inheritdoc/>
  public async Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardGroup>(new { Id = id }, scope: MedicalStandardGroupScope);

  /// <inheritdoc/>
  public async Task<bool> MedicalStandardGroupNameExistsAsync(Guid categoryId, string name, Guid? excludedId = null) => await dataMapper.QuerySingleAsync<long>(new { CategoryId = categoryId, Name = name, ExcludedId = excludedId }, scope: MedicalStandardGroupScope) > 0;

  /// <inheritdoc/>
  public async Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id) => await dataMapper.QuerySingleAsync<MedicalStandardItem>(new { Id = id }, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code) => await dataMapper.QuerySingleAsync<MedicalStandardItem>(new { Code = code }, scope: MedicalStandardItemScope);

  /// <inheritdoc/>
  public async Task<bool> MedicalStandardItemCodeExistsAsync(string code) => await dataMapper.QuerySingleAsync<long>(new { Code = code }, scope: MedicalStandardItemScope) > 0;
}
