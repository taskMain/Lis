using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.Abstractions.Modularity;
using Dy.Earthrace.Abstractions;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 报告管理端两个入口共用的只读查询映射实现：报告列表、版本列表、版本详情内容与下载文件信息。
/// </summary>
/// <remarks>
/// 全部查询都只筛选与投影，不修改数据，也不判断业务状态；筛选条件的规范化与分页窗口校验在应用层完成。
/// 报告列表按先取总数、再按偏移与页容量取当页实现，计数语句与数据语句共用同一套筛选条件。
/// 文件键只在本层到宿主内部流转，不进入对外契约。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 检查部位语句集的作用域名；与 <c>ExaminationSite.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>按检查项目集合读取检查部位的语句注册在该实体作用域下，与同文件其余查询的查询作用域不同。</remarks>
  private const string ExaminationSiteScope = "ExaminationSite";

  /// <inheritdoc/>
  public async Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<MedicalReportListItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) =>
    await dataMapper.QueryAsync<MedicalReportVersionItem>(new { ReportId = reportId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersionDetailItem>(
      new { ReportId = reportId, ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<LaboratoryReportContentItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<LaboratoryResultItemView>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) =>
    await dataMapper.QueryAsync<LaboratoryResultItemView>(
      // 集合参数与候选查询同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      new { ReportVersionIds = reportVersionIds }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<LaboratoryBacteriaResultItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) =>
    await dataMapper.QueryAsync<LaboratorySusceptibilityItem>(
      new { BacteriaResultId = bacteriaResultId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<ExaminationReportContentItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<ExaminationItemView>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) =>
    await dataMapper.QueryAsync<ExaminationItemView>(
      // 集合参数与候选查询同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      new { ReportVersionIds = reportVersionIds }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
    await dataMapper.QueryAsync<ExaminationSiteView>(
      new { ExaminationItemId = examinationItemId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds)
  {
    // 空集合必须在进入映射语句之前短路：该语句用集合参数写法，空集合在真实 Provider 上展开为空值列表，语句语法不成立。
    // 检验报告的匹配响应没有检查项目，这条路径在正常业务上必然经过，因此这里不把空集合交给映射器。
    if (examinationItemIds.Count == 0) return [];

    // 该语句注册在检查部位的实体作用域下，与同文件其余查询的查询作用域不同，因此这里逐调用传入实体作用域名。
    return await dataMapper.QueryAsync<ExaminationSiteView>(
      new { ExaminationItemIds = examinationItemIds }, scope: ExaminationSiteScope);
  }

  /// <inheritdoc/>
  public async Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) =>
    await dataMapper.QuerySingleAsync<MedicalReportScopeItem>(new { ReportId = reportId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReportDetailCommon>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersionFileItem>(
      new { ReportId = reportId, ReportVersionId = reportVersionId }, scope: SqlScope);
}
