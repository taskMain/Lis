using Dy.Core.Abstractions.Modularity;
using Dy.Earthrace.Abstractions;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 标准项目目录与互认项目配置的只读查询映射实现。
/// </summary>
/// <remarks>
/// 全部查询都只筛选与投影，不修改数据，也不判断业务状态。
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
  /// 执行上述只读查询的映射器。
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

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus) =>
    await dataMapper.QueryAsync<RecognitionProjectConfigurationListItem>(new
    {
      OrganizationCode = organizationCode,
      StandardProjectCode = standardProjectCode,
      // 配置状态筛选比较的是互认配置表的布尔启用列，映射结果由下面的纯映射方法给出。
      IsValid = MapConfigurationStatusToIsValid(configurationStatus)
    }, scope: SqlScope, sqlId: "QueryRecognitionProjectConfigurationList");

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) =>
    await dataMapper.QueryAsync<RecognitionAmountListItem>(new
    {
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode
    }, scope: SqlScope, sqlId: "QueryRecognitionAmountList");

  /// <summary>
  /// 把互认配置列表的状态筛选条件映射为互认配置表布尔启用列的比较值。
  /// </summary>
  /// <remarks>
  /// 启用映射为 <see langword="true"/>、停用映射为 <see langword="false"/>；未传筛选条件返回 <see langword="null"/>，
  /// 表示启用列不参与过滤，启用与停用的配置都返回。
  /// 映射在进入查询语句前完成，查询语句只接收布尔比较值，不接收枚举值。
  /// 分支穷举而不使用弃元兜底：本项目未开启 <c>TreatWarningsAsErrors</c>，新增配置状态只会产生 CS8509 编译告警、
  /// 不构成编译失败；真正的兜底是运行期抛 <c>SwitchExpressionException</c>，因此不会静默退化为"不按启用列过滤"。
  /// </remarks>
  /// <param name="configurationStatus">调用方提交的配置状态筛选条件；不传时为 <see langword="null"/>。</param>
  /// <returns>启用列的比较值；不传筛选条件时为 <see langword="null"/>，表示不按启用列过滤。</returns>
  public static bool? MapConfigurationStatusToIsValid(ConfigurationStatus? configurationStatus) => configurationStatus switch
  {
    ConfigurationStatus.Enabled => true,
    ConfigurationStatus.Disabled => false,
    null => null
  };
}
