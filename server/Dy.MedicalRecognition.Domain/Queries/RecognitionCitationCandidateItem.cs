namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 引用详情候选记录的只读投影行：本次就诊下已经明确采纳、且所属匹配记录接收三值与患者证件一致的全部匹配项。
/// </summary>
/// <remarks>
/// 行集由可信接收三值、患者证件类型与号码、就诊类型与流水号共同限定，因此返回的行全部属于本次就诊；
/// **前三项接收三值与后四项患者就诊身份是候选集的限定键**：它们由语句的筛选子句在数据库侧消费，**不作为投影列返回**，
/// 因此这些属性在真实映射结果上恒为默认值，调用方不得按它们判断归属；需要归属或身份取值时以本次可信上下文与请求为准。
/// 测试替身按同一组限定键在内存中复现候选范围，使「别家医院、别家院区、别的患者的候选不返回」这一口径在替身上同样成立。
/// 有效期起点为所属匹配记录的处理结果保存时间，逐组各自计算，调用方按该列判定经过时间；
/// 排序为处理结果保存时间倒序加匹配项标识升序，调用方按该顺序取第一条即可得到同一报告版本与同一互认项目下保存时间最近的一条；
/// 投影只用于组装引用详情，不作为对外契约，也不携带任何写入所需的字段。
/// </remarks>
public sealed record RecognitionCitationCandidateItem
{
  /// <summary>所属互认匹配记录的接收组织编码；与本次可信组织一致是候选成立的条件。限定键，不作为投影列返回。</summary>
  public string ReceiverOrganizationCode { get; init; } = string.Empty;
  /// <summary>所属互认匹配记录的接收医院编码。限定键，不作为投影列返回。</summary>
  public string ReceiverHospitalCode { get; init; } = string.Empty;
  /// <summary>所属互认匹配记录的接收院区编码。限定键，不作为投影列返回。</summary>
  public string ReceiverBranchCode { get; init; } = string.Empty;
  /// <summary>匹配记录上的患者证件类型代码。限定键，不作为投影列返回。</summary>
  public string IdentityDocumentTypeCode { get; init; } = string.Empty;
  /// <summary>匹配记录上的患者证件号码。限定键，不作为投影列返回。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>匹配记录上的本次来源就诊类型。限定键，不作为投影列返回。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>匹配记录上的本次来源就诊流水号。限定键，不作为投影列返回。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>
  /// 所属匹配记录的处理结果保存时间；引用详情有效期的起点。
  /// </summary>
  /// <remarks>该列按平台首次成功保存该组完整处理结果的时间写入，不使用医院提交的互认时间；未保存处理结果时为空值。</remarks>
  public DateTime? DecisionSavedTime { get; init; }
  /// <summary>所属互认匹配记录标识。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配项标识；医院 HIS 随后提交引用结果时使用该标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>该匹配项已保存的互认结果；只返回采纳的匹配项是引用详情的逐项校验条件之一。</summary>
  public RecognitionResult RecognitionResult { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>匹配项绑定的报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>匹配项绑定的报告版本标识；项目内容按该标识取回，项目类型由该报告的当前版本决定。</summary>
  public Guid ReportVersionId { get; init; }
}
