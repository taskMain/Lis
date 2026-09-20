using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 互认匹配记录与互认匹配项的读写端口。
/// </summary>
/// <remarks>
/// 只做数据读写并返回行数，不判断业务状态；本能力只新增记录与匹配项，不更新既有行。
/// </remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 插入一条互认匹配记录。
  /// </summary>
  /// <remarks>处理结果保存时间写入空值，表示该组尚待作出处理决定；主键由调用方在写入前生成。</remarks>
  /// <param name="recognitionMatchRecord">待插入的匹配记录实体，携带接收三值、患者证件、就诊标识、匹配生成时间与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateRecognitionMatchRecordAsync(RecognitionMatchRecord recognitionMatchRecord);

  /// <summary>
  /// 插入一条互认匹配项。
  /// </summary>
  /// <remarks>报告标识与报告版本标识按本次选优结果原样保存，一经写入不再更换。</remarks>
  /// <param name="recognitionMatchItem">待插入的匹配项实体，携带所属匹配记录、项目类型、标准项目编码、报告与版本标识与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateRecognitionMatchItemAsync(RecognitionMatchItem recognitionMatchItem);

  /// <summary>
  /// 按互认匹配项标识集合读取匹配项，供引用结果提交逐项反查所属匹配记录与绑定报告版本。
  /// </summary>
  /// <remarks>
  /// 一次读取覆盖本次提交的全部匹配项，读取次数不随提交项数增长；
  /// 集合内不存在的标识不产生行，调用方据此按匹配项不存在拒绝；返回顺序按匹配项标识升序。
  /// 本方法不筛选报告版本有效性：引用结果提交有意不校验绑定报告版本在提交时是否仍为当前有效版本。
  /// </remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的匹配项，按匹配项标识升序；全部未命中时为空集合。</returns>
  Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds);

  /// <summary>
  /// 按互认匹配记录标识集合读取匹配记录，供引用结果提交校验各匹配项所属记录的接收三值与本次调用归属一致。
  /// </summary>
  /// <remarks>
  /// 接收三值只保存在匹配记录上，匹配项与处理结果都不保存该三值，因此归属校验必须反查所属记录；
  /// 一次读取覆盖本次提交涉及的全部匹配记录，读取次数不随提交项数增长；
  /// 记录不存在时该标识不产生行，由领域层按匹配项反查不到所属记录拒绝。
  /// </remarks>
  /// <param name="recognitionMatchRecordIds">本次提交涉及的全部互认匹配记录标识。</param>
  /// <returns>命中的匹配记录集合；全部未命中时为空集合。</returns>
  Task<IReadOnlyList<RecognitionMatchRecord>> QueryRecognitionMatchRecordsByIdsAsync(IReadOnlyList<Guid> recognitionMatchRecordIds);
}
