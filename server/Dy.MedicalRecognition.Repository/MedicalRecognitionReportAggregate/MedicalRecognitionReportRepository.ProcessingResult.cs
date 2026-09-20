using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认处理结果的读写实现与匹配记录处理结果保存时间的写入实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态、不校验整组完整性，均由领域层判定；
/// 逐处显式传作用域，不使用共享映射器的上下文设置方法。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 互认处理结果语句集的作用域名；与 <c>RecognitionProcessingResult.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>逐调用传入该作用域名，不使用上下文设置方法，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string RecognitionProcessingResultScope = "RecognitionProcessingResult";

  /// <summary>
  /// 按主键读取一条互认匹配记录。
  /// </summary>
  /// <param name="id">互认匹配记录标识。</param>
  /// <returns>匹配记录；记录不存在时返回 <see langword="null"/>。</returns>
  public async Task<RecognitionMatchRecord?> GetRecognitionMatchRecordByIdAsync(Guid id) =>
    await dataMapper.QuerySingleAsync<RecognitionMatchRecord>(new { Id = id }, scope: RecognitionMatchRecordScope);

  /// <summary>
  /// 按所属互认匹配记录读取该组已保存的全部处理结果。
  /// </summary>
  /// <remarks>返回顺序按互认匹配项标识升序，由映射语句的排序子句保证；该组尚未提交过时返回空集合。</remarks>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
  /// <returns>该组的处理结果集合。</returns>
  public async Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByRecordAsync(Guid recognitionMatchRecordId) =>
    [.. await dataMapper.QueryAsync<RecognitionProcessingResult>(
      new { RecognitionMatchRecordId = recognitionMatchRecordId }, scope: RecognitionProcessingResultScope)];

  /// <summary>
  /// 按所属互认匹配记录读取该组已保存的全部互认匹配项。
  /// </summary>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
  /// <returns>该组的匹配项集合，按匹配项标识升序。</returns>
  public async Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByRecordAsync(Guid recognitionMatchRecordId) =>
    [.. await dataMapper.QueryAsync<RecognitionMatchItem>(
      new { RecognitionMatchRecordId = recognitionMatchRecordId }, scope: RecognitionMatchItemScope)];

  /// <summary>
  /// 按互认匹配项标识集合读取处理结果，一次读取覆盖本次提交的全部匹配项。
  /// </summary>
  /// <remarks>
  /// 引用结果提交按该方法逐项反查该项目是否已采纳，并取得该组已保存的互认时间；
  /// 读取次数不随提交项数增长；排序按匹配项标识升序，由映射语句的排序子句保证。
  /// </remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的处理结果，按匹配项标识升序。</returns>
  public async Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) =>
    [.. await dataMapper.QueryAsync<RecognitionProcessingResult>(
      new { RecognitionMatchItemIds = recognitionMatchItemIds }, scope: RecognitionProcessingResultScope)];

  /// <summary>
  /// 插入一条互认处理结果。
  /// </summary>
  /// <param name="value">待保存的处理结果实体。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateRecognitionProcessingResultAsync(RecognitionProcessingResult value) =>
    await dataMapper.InsertAsync(value, scope: RecognitionProcessingResultScope);

  /// <summary>
  /// 把该组的处理结果保存时间写入互认匹配记录。
  /// </summary>
  /// <param name="value">携带匹配记录标识、本次处理结果保存时间与操作字段的实体。</param>
  /// <returns>受影响行数：该组首次保存处理结果时为 1；重复提交或记录不存在时为 0。</returns>
  public async Task<int> UpdateRecognitionMatchRecordDecisionSavedTimeAsync(RecognitionMatchRecord value) =>
    await dataMapper.UpdateAsync(value, scope: RecognitionMatchRecordScope);
}
