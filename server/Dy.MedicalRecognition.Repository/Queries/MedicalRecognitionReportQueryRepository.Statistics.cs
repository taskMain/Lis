using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.Abstractions.Modularity;
using Dy.Earthrace.Abstractions;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 互认统计与导出的只读查询映射实现：两侧汇总、接收侧四类明细、来源侧明细与匹配记录集合视图。
/// </summary>
/// <remarks>
/// 全部查询都只筛选与投影，不修改数据，也不判断业务状态；筛选条件的组织路径解析与分页窗口校验在应用层完成。
/// 方法名去掉 <c>Async</c> 后缀即语句标识，框架据此定位查询作用域 <c>MedicalRecognitionReportQuery</c> 下的语句；
/// 计数语句与数据语句共用同一套筛选条件，汇总的计数与分页都作用于聚合后的分组行，
/// 分页窗口由数据映射器的当前 Provider 适配层附加，语句保持 Provider 中立。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryRepository
{
  /// <inheritdoc/>
  public async Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<RecognitionUsageSummaryGroupItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter) =>
    await dataMapper.QueryAsync<RecognitionUsageReasonItem>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<SourceRecognitionSummaryGroupItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<RecognitionReminderDetailItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<RecognitionAdoptionDetailItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<RecognitionNonAdoptionDetailItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<RecognitionReferenceDetailItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter) =>
    await dataMapper.QuerySingleAsync<long>(filter, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize) =>
    await dataMapper.QueryAsync<SourceRecognitionDetailItem>(
      filter, scope: SqlScope, pagination: new Pagination(skipCount, pageSize));

  /// <inheritdoc/>
  public async Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId) =>
    await dataMapper.QuerySingleAsync<RecognitionMatchRecordView>(
      new { RecognitionMatchRecordId = recognitionMatchRecordId }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId) =>
    await dataMapper.QueryAsync<RecognitionMatchRecordViewItem>(
      new { RecognitionMatchRecordId = recognitionMatchRecordId }, scope: SqlScope);
}
