namespace Dy.MedicalRecognition.Domain.Queries.Ports;

/// <summary>
/// 互认匹配查询所需的只读投影：候选报告筛选与匹配响应的报告事实读取。
/// </summary>
/// <remarks>
/// 两条查询都只按业务范围与集合读取并排序，不修改数据，也不判断业务状态；
/// 匹配基准时间、项目类型与标准项目编码的合并取数口径由映射语句决定，本层不重新计算。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 按可信接收三值与已解析平台患者、就诊范围读取标准项目下的候选报告。
  /// </summary>
  /// <remarks>
  /// 只返回当前有效报告，且只取报告的当前版本指向对应的版本；
  /// 候选范围同时按平台患者标识与当前版本上的证件限定：已解析到的患者是唯一权威的候选归属，证件条件只表达该患者的主数据取值；
  /// 排序为匹配基准时间倒序、报告时间倒序、报告版本标识升序，调用方按该顺序逐项目取第一条即可得到最晚出具的一份稳定候选；
  /// 标准项目编码集合为空时返回空集合，由调用方表达为成功的空匹配。
  /// </remarks>
  /// <param name="organizationCode">可信接收组织编码，同时作为 MVP 的来源组织范围。</param>
  /// <param name="hospitalCode">可信接收医院编码，与来源医院是否相同决定本院报告是否参与匹配。</param>
  /// <param name="branchCode">可信接收院区编码。</param>
  /// <param name="patientId">本次按证件解析到的平台患者标识，候选报告主体必须关联该患者。</param>
  /// <param name="identityDocumentTypeCode">患者证件类型代码，应为去除首尾空白后的规范形式。</param>
  /// <param name="identityDocumentNo">患者证件号码，应为去除首尾空白并统一大写后的规范形式。</param>
  /// <param name="visitType">本次来源就诊类型。</param>
  /// <param name="visitSerialNo">本次来源就诊流水号。</param>
  /// <param name="standardProjectCodes">本次查询的全部标准项目编码，去重后传入。</param>
  /// <returns>候选报告行集合；没有任何候选时为空集合。</returns>
  Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    Guid patientId,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo,
    IReadOnlyList<string> standardProjectCodes);

  /// <summary>
  /// 按本次匹配项绑定的一组报告版本读取匹配响应的报告事实。
  /// </summary>
  /// <remarks>
  /// 每个绑定的版本恰好一行；检验侧的标本类型名称与整体异常标识、检查侧的来源影像状态与影像调阅地址按报告类型分别有值；
  /// 排序按报告版本标识升序，使同一批的返回顺序稳定；集合为空时返回空集合。
  /// </remarks>
  /// <param name="reportVersionIds">本次匹配项绑定的报告版本标识集合，来自领域层逐项目选优的结果。</param>
  /// <returns>报告事实行集合；集合为空或版本不可读时为空集合。</returns>
  Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds);

  /// <summary>
  /// 从一组报告版本中筛出仍为当前有效版本的标识。
  /// </summary>
  /// <remarks>
  /// 报告已形成后续版本或已作废时该版本不再有效，因此未出现在返回集合中的标识即已失效；
  /// 集合为空时返回空集合；排序按报告版本标识升序，使同一批的返回顺序稳定。
  /// 投影为单列行类型而不是标量集合：本平台的数据映射只对有属性名的行类型做结果反序列化。
  /// 供处理结果提交在未命中幂等时校验匹配项绑定报告版本是否仍为当前有效版本，以及获取引用详情逐项校验绑定版本。
  /// </remarks>
  /// <param name="reportVersionIds">待判定的报告版本标识集合，取本次匹配项绑定的版本去重后的结果。</param>
  /// <returns>仍为当前有效版本的行集合，每行携带该版本标识；全部失效时为空集合。</returns>
  Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds);
}
