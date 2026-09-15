using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 承载一份报告在平台侧的身份、当前生效版本指针与生命周期状态；报告只作废不物理删除，作废后不得再追加版本或恢复。
/// </summary>
[IPropertyChangedAware]
public partial class MedicalRecognitionReport : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 报告主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 报告类型
  /// </summary>
  public partial MedicalReportType ReportType { get; set; }
  /// <summary>
  /// 报告单号；与报告类型、组织、医院、院区共同构成业务组合键，平台据此判定后继报告属于同一份还是新报告，已存在的报告不新建。
  /// </summary>
  public partial string ReportNo { get; set; }
  /// <summary>
  /// 平台患者ID；通过患者标识关联，不内嵌患者主数据。
  /// </summary>
  public partial Guid PatientId { get; set; }
  /// <summary>
  /// 当前版本ID；必须指向本报告自己的一个版本。
  /// </summary>
  public partial Guid CurrentVersionId { get; set; }
  /// <summary>
  /// 生命周期状态；作废报告不再参与互认匹配。
  /// </summary>
  public partial MedicalReportLifecycleStatus Status { get; set; }
  /// <summary>
  /// 作废时间
  /// </summary>
  public partial DateTime? VoidedTime { get; set; }
  /// <summary>
  /// 作废原因
  /// </summary>
  public partial string? VoidReason { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
