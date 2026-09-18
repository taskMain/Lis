using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 已发生的领域事实：某份互认报告在平台侧创建。
/// </summary>
/// <remarks>只在报告不存在并由报告版本追加步骤创建该报告时登记；报告已存在时仅取得报告，不重复登记。</remarks>
public record MedicalRecognitionReportCreatedEvent : DomainEvent
{
  /// <summary>本事件所属聚合的标识。</summary>
  public override string AggregateId { get; } = MedicalRecognitionReportConst.AggregateId;
  /// <summary>事件类型标识，取本类型名称。</summary>
  public override string EventType { get; } = nameof(MedicalRecognitionReportCreatedEvent);
  /// <summary>新建报告标识。</summary>
  public Guid Id { get; set; }
  /// <summary>组织编码。</summary>
  public string OrganizationCode { get; set; } = string.Empty;
  /// <summary>医院编码。</summary>
  public string HospitalCode { get; set; } = string.Empty;
  /// <summary>院区编码。</summary>
  public string BranchCode { get; set; } = string.Empty;
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; set; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; set; } = string.Empty;
  /// <summary>平台患者标识。</summary>
  public Guid PatientId { get; set; }
}
