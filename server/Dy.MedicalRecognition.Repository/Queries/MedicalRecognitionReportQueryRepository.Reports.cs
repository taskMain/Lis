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
  /// <inheritdoc/>
  public async Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope, sqlId: "CountMedicalReportList");

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<MedicalReportListItem>(
      filter, scope: SqlScope, sqlId: "QueryMedicalReportList", pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) =>
    await dataMapper.QueryAsync<MedicalReportVersionItem>(new { ReportId = reportId }, scope: SqlScope, sqlId: "QueryMedicalReportVersionList");

  /// <inheritdoc/>
  public async Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersionDetailItem>(
      new { ReportId = reportId, ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "GetMedicalReportVersionDetail");

  /// <inheritdoc/>
  public async Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<LaboratoryReportContentItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "GetLaboratoryReportContentByVersion");

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<LaboratoryResultItemView>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "QueryLaboratoryResultItemsByVersion");

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<LaboratoryBacteriaResultItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "QueryLaboratoryBacteriaResultsByVersion");

  /// <inheritdoc/>
  public async Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) =>
    await dataMapper.QueryAsync<LaboratorySusceptibilityItem>(
      new { BacteriaResultId = bacteriaResultId }, scope: SqlScope, sqlId: "QueryLaboratorySusceptibilitiesByBacteria");

  /// <inheritdoc/>
  public async Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<ExaminationReportContentItem>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "GetExaminationReportContentByVersion");

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) =>
    await dataMapper.QueryAsync<ExaminationItemView>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "QueryExaminationItemsByVersion");

  /// <inheritdoc/>
  public async Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
    await dataMapper.QueryAsync<ExaminationSiteView>(
      new { ExaminationItemId = examinationItemId }, scope: SqlScope, sqlId: "QueryExaminationSitesByItem");

  /// <inheritdoc/>
  public async Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) =>
    await dataMapper.QuerySingleAsync<MedicalReportScopeItem>(new { ReportId = reportId }, scope: SqlScope, sqlId: "GetMedicalReportScope");

  /// <inheritdoc/>
  public async Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalRecognitionReportDetailCommon>(
      new { ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "GetMedicalReportVersionCommon");

  /// <inheritdoc/>
  public async Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) =>
    await dataMapper.QuerySingleAsync<MedicalReportVersionFileItem>(
      new { ReportId = reportId, ReportVersionId = reportVersionId }, scope: SqlScope, sqlId: "GetMedicalReportVersionFile");
}
