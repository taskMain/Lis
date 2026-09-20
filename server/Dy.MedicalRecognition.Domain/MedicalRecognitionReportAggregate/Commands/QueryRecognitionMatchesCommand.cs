using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 按患者身份与本次来源就诊查询可用的互认匹配：一次查询形成一个匹配记录与若干匹配项，只在非空匹配时写入。
/// </summary>
/// <remarks>
/// 接收三值取自信任调用身份，操作人与操作时间由应用服务写入，调用方都不提交；
/// 匹配生成时间由应用服务取本次查询的平台接收时间，同时作为匹配截止点与可互认时间的经过时间起点；
/// 本院报告匹配开关与排除时长取自平台级系统参数，由应用服务读取并校验后写入本命令，领域层只按这两个值判断本院报告是否参与匹配；
/// 项目编码整批校验与去重在领域层完成，调用方重复提交相同编码不影响结果。
/// </remarks>
[ObjectMap(typeof(RecognitionMatchesReturnedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record QueryRecognitionMatchesCommand : ICommand
{
  /// <summary>
  /// 可信接收组织编码；MVP 只匹配来源组织与接收组织一致的报告。
  /// </summary>
  public partial string ReceiverOrganizationCode { get; set; }
  /// <summary>
  /// 可信接收医院编码；与来源医院相同即本院报告，是否参与匹配由开关与排除时长决定。
  /// </summary>
  public partial string ReceiverHospitalCode { get; set; }
  /// <summary>
  /// 可信接收院区编码；本院报告判定不区分院区。
  /// </summary>
  public partial string ReceiverBranchCode { get; set; }
  /// <summary>
  /// 患者证件类型代码；按规范形式去除首尾空白后用于解析平台患者与筛选候选报告。
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 患者证件号码；按规范形式去除首尾空白并统一大写后使用。
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 本次来源就诊类型。
  /// </summary>
  public partial VisitType VisitType { get; set; }
  /// <summary>
  /// 本次来源就诊流水号。
  /// </summary>
  public partial string VisitSerialNo { get; set; }
  /// <summary>
  /// 本次匹配生成时间；取本次查询的平台接收时间，同时作为匹配截止点。
  /// </summary>
  public partial DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 本院报告匹配开关；为真表示本院报告按排除时长与可互认时间参与匹配，为假表示本院报告整体不参与。
  /// </summary>
  public partial bool OwnHospitalMatchEnabled { get; set; }
  /// <summary>
  /// 本院报告排除时长小时数；非负整数，允许零，零表示不增加额外等待。
  /// </summary>
  public partial int OwnHospitalExcludeHours { get; set; }
  /// <summary>
  /// 本次拟开项目集合；至少一项，相同标准项目编码重复出现时只处理一次。
  /// </summary>
  public partial IReadOnlyList<QueryRecognitionMatchesCommandItem> ProposedItems { get; set; } = [];
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；命令时间只用于领域事件，实体列另取命令携带的服务端时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 为本次非空匹配构造“匹配已返回”领域事件。
  /// </summary>
  /// <remarks>事件沿用命令的接收三值与匹配生成时间；匹配项标识集合由领域层在写入后补入。</remarks>
  /// <param name="id">本次保存的互认匹配记录标识。</param>
  /// <param name="matchItemIds">本次形成的全部互认匹配项标识。</param>
  /// <returns>待登记的匹配已返回事件。</returns>
  public RecognitionMatchesReturnedEvent CreateRecognitionMatchesReturnedEvent(Guid id, IReadOnlyList<Guid> matchItemIds)
  {
    RecognitionMatchesReturnedEvent recognitionMatchesReturnedEvent = this.MapToRecognitionMatchesReturnedEvent(
      recognitionMatchRecordId: id, eventCreator: OperId, eventCreatedTime: OperTime);
    recognitionMatchesReturnedEvent.MatchItemIds = matchItemIds;
    recognitionMatchesReturnedEvent.MatchedItemCount = matchItemIds.Count;
    return recognitionMatchesReturnedEvent;
  }
}
