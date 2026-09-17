using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：某组织、医院、院区下某标准项目的当前金额已被保存为本次提交值。
/// </summary>
public record OrganizationHospitalBranchRecognitionAmountSavedEvent : DomainEvent
{
  /// <summary>
  /// 本事件所属聚合的标识。
  /// </summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>
  /// 事件类型标识，取本类型名称。
  /// </summary>
  public override string EventType { get; } = nameof(OrganizationHospitalBranchRecognitionAmountSavedEvent);
  /// <summary>
  /// 本次保存所影响金额记录的标识
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public string BranchCode { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 本次保存后的当前金额
  /// </summary>
  public decimal CurrentAmount { get; set; }
}
