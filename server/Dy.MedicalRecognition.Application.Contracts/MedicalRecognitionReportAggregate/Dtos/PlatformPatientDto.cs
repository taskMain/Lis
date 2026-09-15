using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 平台患者的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record PlatformPatientDto : Dto
{
  /// <summary>
  /// 平台患者标识；报告与报告版本通过该标识关联患者。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 证件类型代码，按来源受控值保存、不映射平台字典。
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 证件号码，不用姓名相似度、联系电话或院内卡替代识别；身份信息冲突时拒绝关联。
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 患者姓名；仅用于身份信息一致性核对，不参与识别。
  /// </summary>
  public partial string PatientName { get; set; }
  /// <summary>
  /// 患者性别代码，按来源受控值保存；仅用于身份信息一致性核对，不参与识别。
  /// </summary>
  public partial string PatientGenderCode { get; set; }
  /// <summary>
  /// 患者出生日期；仅用于身份信息一致性核对，不参与识别。
  /// </summary>
  public partial DateTime PatientBirthDate { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
