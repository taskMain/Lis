using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：一次互认匹配查询返回了非空匹配，平台已保存本次匹配记录与全部匹配项。
/// </summary>
/// <remarks>
/// 仅非空匹配登记本事件：没有报告命中时查询按成功空结果返回，不创建记录、不登记事件；
/// 每次非空查询都生成新的匹配记录标识与匹配项标识，重试按新查询重新计算并再次登记。
/// </remarks>
public record RecognitionMatchesReturnedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(RecognitionMatchesReturnedEvent);
  /// <summary>
  /// 本次匹配保存的互认匹配记录标识
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 本次匹配生成时间，取平台保存匹配记录的时间
  /// </summary>
  public DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 接收组织编码；取自可信调用身份
  /// </summary>
  public string ReceiverOrganizationCode { get; set; } = string.Empty;
  /// <summary>
  /// 接收医院编码；取自可信调用身份
  /// </summary>
  public string ReceiverHospitalCode { get; set; } = string.Empty;
  /// <summary>
  /// 接收院区编码；取自可信调用身份
  /// </summary>
  public string ReceiverBranchCode { get; set; } = string.Empty;
  /// <summary>
  /// 本次实际命中的互认项目数；等于匹配项标识集合的元素个数
  /// </summary>
  public int MatchedItemCount { get; set; }
  /// <summary>
  /// 本次形成的全部互认匹配项标识；每个标识对应一个命中的标准项目
  /// </summary>
  public IReadOnlyList<Guid> MatchItemIds { get; set; } = [];
}
