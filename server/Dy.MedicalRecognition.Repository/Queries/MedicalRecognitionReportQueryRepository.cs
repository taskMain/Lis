using Dy.Core.Abstractions.Modularity;
using Dy.Earthrace.Abstractions;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 标准项目目录四个只读查询的映射实现。
/// </summary>
/// <remarks>
/// 四个查询都只筛选与投影，不修改数据，也不判断业务状态。
/// 返回的记录范围、排序和派生列含义由查询映射文件中同名的语句决定；空值语义统一为不传即不过滤。
/// </remarks>
public sealed class MedicalRecognitionReportQueryRepository : IMedicalRecognitionReportQueryRepository, ITransientDependency<IMedicalRecognitionReportQueryRepository>
{
  /// <summary>
  /// 本仓储所用语句集的作用域名。
  /// </summary>
  /// <remarks>即查询映射文件声明的 SqlMap 作用域；与各语句标识共同构成框架定位语句的完整键。</remarks>
  private const string SqlScope = "MedicalRecognitionReportQuery";
  /// <summary>
  /// 执行上述四个只读查询的映射器。
  /// </summary>
  private readonly IDataMapper dataMapper;

  /// <summary>
  /// 接收默认数据映射器作为本仓储的数据访问入口。
  /// </summary>
  /// <remarks>本仓储的全部只读查询都通过该映射器执行。</remarks>
  /// <param name="dataMapper">装配层按默认别名解析出的数据映射器。</param>
  public MedicalRecognitionReportQueryRepository([FromKeyedServices(BuilderConst.DEFAULT_ALIAS)] IDataMapper dataMapper) => this.dataMapper = dataMapper;

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) =>
    await dataMapper.QueryAsync<MedicalStandardCategoryListItem>(new { ItemType = itemType }, scope: SqlScope, sqlId: "QueryMedicalStandardCategoryList");

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) =>
    await dataMapper.QueryAsync<MedicalStandardGroupListItem>(new { CategoryId = categoryId }, scope: SqlScope, sqlId: "QueryMedicalStandardGroupList");

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) =>
    await dataMapper.QueryAsync<MedicalStandardItemListItem>(new { CategoryId = categoryId, GroupId = groupId, Code = code, Name = name, IsValid = isValid }, scope: SqlScope, sqlId: "QueryMedicalStandardItemList");

  /// <inheritdoc/>
  public async Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) =>
    await dataMapper.QueryAsync<EffectiveMedicalStandardCatalogItem>(new { ItemType = itemType, CategoryName = categoryName }, scope: SqlScope, sqlId: "QueryEffectiveMedicalStandardCatalog");
}
