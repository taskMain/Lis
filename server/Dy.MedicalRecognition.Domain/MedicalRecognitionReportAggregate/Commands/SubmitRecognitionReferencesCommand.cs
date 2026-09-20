using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 医院 HIS 在已采纳结果成功写入本次病历后提交实际引用事实：每个项目只经互认匹配项标识定位。
/// </summary>
/// <remarks>
/// 一次请求允许包含来自不同互认匹配记录的项目，全部项目整体校验并原子保存，任一项目失败时整次不保存、不提供逐条部分结果；
/// 接收组织、医院与院区取自信任调用身份，操作人取自登录上下文，调用方都不提交；
/// 本次来源就诊与互认时间由平台按各匹配项已保存的事实反查取得，调用方不重复提交；
/// 引用结果不校验绑定报告版本在提交时是否仍有效，也不受引用详情有效期限制，已经发生的引用事实不因提交时报告已失效而被拒绝。
/// </remarks>
[ObjectMap(typeof(RecognitionReferencesRecordedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record SubmitRecognitionReferencesCommand : ICommand
{
  /// <summary>
  /// 可信接收组织编码；用于校验各匹配项所属匹配记录的接收组织与本次调用归属一致。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 可信接收医院编码；用于校验各匹配项所属匹配记录的接收医院与本次调用归属一致。
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 可信接收院区编码；用于校验各匹配项所属匹配记录的接收院区与本次调用归属一致。
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 本次提交的实际引用项目集合；不要求覆盖所属匹配记录或该组全部已采纳项目，未提交项目只表示平台本次未收到引用事实。
  /// </summary>
  public partial IReadOnlyList<SubmitRecognitionReferenceCommandItem> ReferenceItems { get; set; } = [];
  /// <summary>
  /// 操作时间；由应用服务取服务端协调世界时，写入引用事实的审计字段并作为领域事件的发生时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
  /// <summary>
  /// 本次请求的接收时间；由应用服务取服务端本地墙上时间，作为实际引用时间的上界。
  /// </summary>
  /// <remarks>
  /// 实际引用时间取自医院提交的无时区业务时间，因此上界必须与业务时间的写入口径同源，不使用协调世界时。
  /// </remarks>
  public partial DateTime ReceivedTime { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }

  /// <summary>
  /// 为本次整批保存的引用事实构造“引用结果已记录”领域事件。
  /// </summary>
  /// <remarks>
  /// 匹配项标识集合与引用事实集合按同一顺序逐项对应，下标相同的两项描述同一个匹配项的本次引用事实；
  /// 引用只形成引用事实，不承载采纳次数、来源医院被认次数或预计节省金额。
  /// </remarks>
  /// <param name="matchItemIds">本次形成引用事实的全部互认匹配项标识，按匹配项标识升序排列。</param>
  /// <param name="referenceFacts">本次保存的引用事实，下标与匹配项标识集合一致。</param>
  /// <returns>待登记的引用结果已记录事件。</returns>
  public RecognitionReferencesRecordedEvent CreateRecognitionReferencesRecordedEvent(
    IReadOnlyList<Guid> matchItemIds,
    IReadOnlyList<RecognitionReferenceFact> referenceFacts)
  {
    RecognitionReferencesRecordedEvent recognitionReferencesRecordedEvent = this.MapToRecognitionReferencesRecordedEvent(
      eventCreator: OperId, eventCreatedTime: OperTime);
    recognitionReferencesRecordedEvent.ReferencedMatchItemIds = matchItemIds;
    recognitionReferencesRecordedEvent.ReferenceFacts = referenceFacts;
    return recognitionReferencesRecordedEvent;
  }
}
