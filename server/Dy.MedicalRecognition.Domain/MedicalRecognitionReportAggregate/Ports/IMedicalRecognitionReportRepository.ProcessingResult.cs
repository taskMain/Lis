namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 互认处理结果的读写端口与匹配记录处理结果保存时间的写入端口。
/// </summary>
/// <remarks>
/// 只做数据读写并返回行数，不判断业务状态：整组完整性、时间顺序、决定内容、幂等与冲突判定都在领域层完成；
/// 处理结果表不保存标准项目编码，采纳项目的当前金额按接收组织、医院、院区与由匹配项反查得到的项目编码读取。
/// </remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 按主键读取一条互认匹配记录。
  /// </summary>
  /// <remarks>
  /// 供处理结果提交校验本次提交的接收三值与匹配记录归属一致、并取得匹配生成时间判定互认时间下界；
  /// 记录不存在时返回 <see langword="null"/>，调用方据此按匹配记录不存在拒绝。
  /// </remarks>
  /// <param name="id">互认匹配记录标识。</param>
  /// <returns>匹配记录；记录不存在时返回 <see langword="null"/>。</returns>
  Task<RecognitionMatchRecord?> GetRecognitionMatchRecordByIdAsync(Guid id);

  /// <summary>
  /// 按所属互认匹配记录读取该组已保存的全部处理结果。
  /// </summary>
  /// <remarks>
  /// 该组尚未提交过处理结果时返回空集合，调用方据此判定本次是首次保存；
  /// 返回顺序按互认匹配项标识升序，使同一组的返回顺序稳定；本方法不区分采纳与不采纳，也**不筛选报告版本有效性**。
  /// </remarks>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识，同时是本组的归属范围。</param>
  /// <returns>该组已保存的处理结果集合；尚无处理结果时为空集合。</returns>
  Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByRecordAsync(Guid recognitionMatchRecordId);

  /// <summary>
  /// 按所属互认匹配记录读取该组已保存的全部互认匹配项。
  /// </summary>
  /// <remarks>
  /// 供处理结果提交校验项目集合是否完整覆盖该组、是否夹带其他组的匹配项，并取得采纳项目的标准项目编码；
  /// 返回顺序按匹配项标识升序，与该组处理结果的返回顺序同一口径。
  /// </remarks>
  /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
  /// <returns>该组的匹配项集合；匹配记录没有匹配项时为空集合。</returns>
  Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByRecordAsync(Guid recognitionMatchRecordId);

  /// <summary>
  /// 按互认匹配项标识集合读取已保存的处理结果，供引用结果提交逐项反查该项目是否已采纳及该组已保存的互认时间。
  /// </summary>
  /// <remarks>
  /// 一次读取覆盖本次提交的全部匹配项，读取次数不随提交项数增长；
  /// 集合内不存在处理结果或决定为不采纳的匹配项都照样返回，由领域层按读回的决定判定；
  /// 返回顺序按匹配项标识升序。
  /// </remarks>
  /// <param name="recognitionMatchItemIds">本次提交的全部互认匹配项标识。</param>
  /// <returns>集合内命中的处理结果，按匹配项标识升序；全部未命中时为空集合。</returns>
  Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds);

  /// <summary>
  /// 插入一条互认处理结果。
  /// </summary>
  /// <remarks>
  /// 采纳项目的预计节省金额按保存时读取的当前金额写入，不采纳项目写入空值；
  /// 处理结果表不保存标准项目编码，项目编码由互认匹配项标识反查匹配项取得。
  /// </remarks>
  /// <param name="recognitionProcessingResult">待插入的处理结果实体，携带所属记录与匹配项、组级互认字段、该项目决定与预计节省金额。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateRecognitionProcessingResultAsync(RecognitionProcessingResult recognitionProcessingResult);

  /// <summary>
  /// 把该组的处理结果保存时间写入互认匹配记录，作为引用详情有效期的起算时间。
  /// </summary>
  /// <remarks>
  /// 只在匹配记录主键命中且该列当前为空值时更新一行：首次成功保存写入 1 行，
  /// 幂等重试与记录不存在都影响 0 行，调用方据此区分首次保存与其它情况；接收归属与匹配生成时间不在变更范围内。
  /// </remarks>
  /// <param name="recognitionMatchRecord">携带匹配记录标识、本次处理结果保存时间与操作字段的实体。</param>
  /// <returns>受影响行数：该组首次保存处理结果时为 1，否则为 0。</returns>
  Task<int> UpdateRecognitionMatchRecordDecisionSavedTimeAsync(RecognitionMatchRecord recognitionMatchRecord);
}
