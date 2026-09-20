using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认匹配记录与互认匹配项的写入实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的新增语句，不判断业务状态、不校验匹配项是否已存在，均由领域层判定；
/// 主键由领域层在写入前生成，并发撞键由主键唯一性兜底并与唯一约束异常一样按原样向外传播。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 互认匹配记录语句集的作用域名；与 <c>RecognitionMatchRecord.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>逐调用传入该作用域名，不使用上下文设置方法，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string RecognitionMatchRecordScope = "RecognitionMatchRecord";

  /// <summary>
  /// 互认匹配项语句集的作用域名；与 <c>RecognitionMatchItem.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>逐调用传入该作用域名，不使用上下文设置方法，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string RecognitionMatchItemScope = "RecognitionMatchItem";

  /// <summary>
  /// 插入一条互认匹配记录，处理结果保存时间写入空值。
  /// </summary>
  /// <param name="value">待保存的匹配记录实体，含接收三值、患者证件、就诊标识、匹配生成时间与操作字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateRecognitionMatchRecordAsync(RecognitionMatchRecord value) =>
    await dataMapper.InsertAsync(value, scope: RecognitionMatchRecordScope);

  /// <summary>
  /// 插入一条互认匹配项，报告标识与报告版本标识按本次选优结果原样保存。
  /// </summary>
  /// <param name="value">待保存的匹配项实体，含所属匹配记录、项目类型、标准项目编码、报告与版本标识与操作字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateRecognitionMatchItemAsync(RecognitionMatchItem value) =>
    await dataMapper.InsertAsync(value, scope: RecognitionMatchItemScope);

  /// <summary>
  /// 按互认匹配项标识集合读取匹配项，一次读取覆盖本次提交的全部匹配项。
  /// </summary>
  /// <remarks>读取次数不随提交项数增长；排序按匹配项标识升序，由映射语句的排序子句保证。</remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的匹配项，按匹配项标识升序。</returns>
  public async Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) =>
    [.. await dataMapper.QueryAsync<RecognitionMatchItem>(
      new { RecognitionMatchItemIds = recognitionMatchItemIds }, scope: RecognitionMatchItemScope)];

  /// <summary>
  /// 按互认匹配记录标识集合读取匹配记录，一次读取覆盖本次提交涉及的全部匹配记录。
  /// </summary>
  /// <remarks>
  /// 接收三值只保存在匹配记录上，引用结果提交的归属校验必须先反查所属记录，因此本方法随该用例一并补齐；
  /// 读取次数不随提交项数增长；排序按匹配记录标识升序，由映射语句的排序子句保证。
  /// </remarks>
  /// <param name="recognitionMatchRecordIds">本次提交涉及的全部互认匹配记录标识。</param>
  /// <returns>命中的匹配记录集合，按匹配记录标识升序。</returns>
  public async Task<IReadOnlyList<RecognitionMatchRecord>> QueryRecognitionMatchRecordsByIdsAsync(IReadOnlyList<Guid> recognitionMatchRecordIds) =>
    [.. await dataMapper.QueryAsync<RecognitionMatchRecord>(
      new { RecognitionMatchRecordIds = recognitionMatchRecordIds }, scope: RecognitionMatchRecordScope)];
}
