using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 平台内的患者身份主数据；报告与报告版本只通过患者标识关联，不内嵌患者主数据。
/// </summary>
[IPropertyChangedAware]
public partial class PlatformPatient : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 平台患者主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 身份证号码；平台使用该号码精确关联跨院患者，不以电话号码等可变更字段作为识别依据。
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 患者姓名；与性别代码、出生日期只用于一致性核对，不参与患者识别。
  /// </summary>
  public partial string PatientName { get; set; }
  /// <summary>
  /// 患者性别代码
  /// </summary>
  public partial string PatientGenderCode { get; set; }
  /// <summary>
  /// 患者出生日期
  /// </summary>
  public partial DateTime PatientBirthDate { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
