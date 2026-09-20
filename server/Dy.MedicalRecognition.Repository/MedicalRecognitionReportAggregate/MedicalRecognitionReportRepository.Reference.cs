using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认引用事实的读取与写入实现。
/// </summary>
/// <remarks>
/// 只发映射文件中已有的语句，不判断业务状态、不判断重复引用是否可接受，均由领域层判定；
/// 逐处显式传作用域，不使用共享映射器的上下文设置方法；
/// 引用事实表按互认匹配项标识建唯一索引，同一匹配项已被引用时数据库拒绝本次写入，
/// 本实现不识别也不加工该拒绝，数据库异常按原样向外传播。
/// </remarks>
public partial class MedicalRecognitionReportRepository
{
  /// <summary>
  /// 互认引用事实语句集的作用域名；与 <c>RecognitionReference.xml</c> 的 <c>SqlMap Scope</c> 一一对应。
  /// </summary>
  /// <remarks>逐调用传入该作用域名，不使用上下文设置方法，避免共享仓储实例之间的作用域串扰。</remarks>
  private const string RecognitionReferenceScope = "RecognitionReference";

  /// <summary>
  /// 按互认匹配项标识集合读取已保存的引用事实，一次读取覆盖本次提交的全部匹配项。
  /// </summary>
  /// <remarks>读取次数不随提交项数增长；排序按匹配项标识升序，由映射语句的排序子句保证。</remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的引用事实，按匹配项标识升序。</returns>
  public async Task<IReadOnlyList<RecognitionReference>> QueryRecognitionReferencesByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) =>
    [.. await dataMapper.QueryAsync<RecognitionReference>(
      new { RecognitionMatchItemIds = recognitionMatchItemIds }, scope: RecognitionReferenceScope)];

  /// <summary>
  /// 插入一条互认引用事实。
  /// </summary>
  /// <remarks>
  /// 同一匹配项至多一条引用事实由该列上的唯一索引兜底；
  /// 撞键时数据库异常按原样向外传播，本层不做识别与翻译，也不自动改用更新路径、不重试。
  /// </remarks>
  /// <param name="value">待保存的引用事实实体，含匹配项标识、实际引用时间、引用科室与医生与操作字段。</param>
  /// <returns>受影响行数，插入成功为 1。</returns>
  public async Task<int> CreateRecognitionReferenceAsync(RecognitionReference value) =>
    await dataMapper.InsertAsync(value, scope: RecognitionReferenceScope);
}
