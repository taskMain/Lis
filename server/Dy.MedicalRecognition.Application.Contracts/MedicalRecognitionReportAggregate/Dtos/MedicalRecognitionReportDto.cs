using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 互认报告主体的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record MedicalRecognitionReportDto : Dto
{
  /// <summary>
  /// 报告标识；版本与匹配项通过该标识或版本标识回到本主体。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告归属的组织编码。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 报告归属的医院编码，为组织下的医院层级编码。
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 报告归属的院区编码，为医院下的院区层级编码。
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 报告类型；1=检验、2=检查，与组织、医院、院区及报告单号共同确定报告身份。
  /// </summary>
  public partial MedicalReportType ReportType { get; set; }
  /// <summary>
  /// 来源报告单号；已存在的报告只取得不新建。
  /// </summary>
  public partial string ReportNo { get; set; }
  /// <summary>
  /// 平台患者标识；首次提交时关联，后续版本不得迁移。
  /// </summary>
  public partial Guid PatientId { get; set; }
  /// <summary>
  /// 当前生效版本标识；每次成功追加版本后指向新版本，报告作废后指向保持不变。
  /// </summary>
  public partial Guid CurrentVersionId { get; set; }
  /// <summary>
  /// 报告生命周期状态；1=当前有效、2=已作废，只由当前有效单向流转到已作废，作废后不得再追加版本或恢复。
  /// </summary>
  public partial MedicalReportLifecycleStatus Status { get; set; }
  /// <summary>
  /// 报告被作废的业务时间；仅在已作废时非空。
  /// </summary>
  public partial DateTime? VoidedTime { get; set; }
  /// <summary>
  /// 作废原因文本，与作废时间成对出现。
  /// </summary>
  public partial string? VoidReason { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
