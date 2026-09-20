namespace Dy.MedicalRecognition.Domain.Queries.Ports;

/// <summary>
/// 获取引用详情所需的只读投影：本次就诊的候选采纳记录与报告公共上下文。
/// </summary>
/// <remarks>
/// 两条查询都只按可信业务范围与集合读取并排序，不修改数据，也不判断业务状态：
/// 候选记录的有效期判定、报告版本有效性逐项校验与同版本同项目的择优都由应用层承担；
/// 报告公共上下文按本次要返回的版本集合一次读回，数据库往返次数不随返回的匹配项数增长。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 按可信接收三值与患者证件、本次来源就诊读取已经采纳的互认匹配项。
  /// </summary>
  /// <remarks>
  /// 行集由可信接收组织、医院、院区与匹配记录上的患者证件类型、证件号码、就诊类型、就诊流水号共同严格匹配，
  /// 因此返回的行全部属于本次就诊，不返回其他医院、院区、患者或就诊的记录；
  /// 只返回已经保存处理结果且决定为采纳的匹配项，未保存处理结果的匹配项不返回；
  /// 排序为处理结果保存时间倒序加互认匹配项标识升序，调用方按该顺序取第一条即可得到同一报告版本与同一互认项目下保存时间最近的一条采纳；
  /// 证件类型代码与证件号码按去除首尾空白后的规范形式传入；没有匹配记录时返回空集合。
  /// </remarks>
  /// <param name="organizationCode">可信接收组织编码。</param>
  /// <param name="hospitalCode">可信接收医院编码。</param>
  /// <param name="branchCode">可信接收院区编码。</param>
  /// <param name="identityDocumentTypeCode">患者证件类型代码，应为去除首尾空白后的规范形式。</param>
  /// <param name="identityDocumentNo">患者证件号码，应为去除首尾空白并统一大写后的规范形式。</param>
  /// <param name="visitType">本次来源就诊类型。</param>
  /// <param name="visitSerialNo">本次来源就诊流水号。</param>
  /// <returns>候选采纳记录行集合；没有匹配记录时为空集合。</returns>
  Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo);

  /// <summary>
  /// 按本次要返回的一组报告版本读取引用详情的报告公共上下文。
  /// </summary>
  /// <remarks>
  /// 行集由传入的版本集合决定，每个版本恰好一行；集合内不存在的版本不产生行；
  /// 检查或检验时间按报告类型合并取值，检查所见与检查结论、来源影像状态与影像调阅地址只在检查侧有值；
  /// 排序按报告版本标识升序，使同一批的返回顺序稳定；集合为空时返回空集合。
  /// </remarks>
  /// <param name="reportVersionIds">本次要返回的报告版本标识集合，取候选记录去重后的结果。</param>
  /// <returns>报告公共上下文行集合；集合为空或版本不可读时为空集合。</returns>
  Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(
    IReadOnlyList<Guid> reportVersionIds);

  /// <summary>
  /// 按一组标准项目编码读取它们在标准目录中的展示名称。
  /// </summary>
  /// <remarks>
  /// 引用详情返回标准项目名称，与来源报告明细上的来源项目名称是两个来源；
  /// 读取不按互认配置或标准目录的启用状态过滤：已采纳项目不因配置或目录停用而失去引用详情；
  /// 编码在标准目录中不存在时不产生行，调用方按名称缺失返回空白文本；
  /// 排序按标准项目编码升序，使同一批的返回顺序稳定；集合为空时返回空集合。
  /// </remarks>
  /// <param name="standardProjectCodes">本次要取名称的标准项目编码集合，去重后传入。</param>
  /// <returns>标准项目编码与名称的行集合；集合为空或编码均不存在时为空集合。</returns>
  Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(
    IReadOnlyList<string> standardProjectCodes);
}
