namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 互认引用事实的读写端口：按互认匹配项标识集合读取既有事实，并按匹配项写入新的引用事实。
/// </summary>
/// <remarks>
/// 只做数据读写并返回行数，不判断业务状态：匹配项存在性、决定为采纳、归属一致、时间序与幂等冲突判定都在领域层完成；
/// 引用事实表不保存标准项目编码，项目编码由互认匹配项标识反查匹配项取得；
/// 同一匹配项至多一个引用事实由该列上的唯一索引兜底，撞键时数据库异常按原样向外传播，实现不做识别与翻译。
/// </remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 按互认匹配项标识集合读取已保存的引用事实，供引用结果提交做幂等识别与冲突判定。
  /// </summary>
  /// <remarks>
  /// 一次读取覆盖本次提交的全部匹配项，读取次数不随提交项数增长；
  /// 集合内尚未提交过引用事实的匹配项不产生行，调用方据此判定该匹配项本次是首次保存；返回顺序按匹配项标识升序。
  /// </remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的引用事实，按匹配项标识升序；全部未命中时为空集合。</returns>
  Task<IReadOnlyList<RecognitionReference>> QueryRecognitionReferencesByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds);

  /// <summary>
  /// 插入一条互认引用事实，记录一个互认匹配项被实际写入病历的项目级事实。
  /// </summary>
  /// <remarks>
  /// 引用科室与引用医生的标识与名称按请求原样保存，不向权限系统补查；
  /// 同一匹配项已有引用事实时数据库按唯一索引拒绝本次写入，该异常按原样向外传播，本层不决定业务是否可以保存；
  /// 引用事实首次保存后不可更正、撤销或删除，因此本方法只新增、不更新既有行。
  /// </remarks>
  /// <param name="recognitionReference">待插入的引用事实实体，携带匹配项标识、实际引用时间、引用科室与医生与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateRecognitionReferenceAsync(RecognitionReference recognitionReference);
}
