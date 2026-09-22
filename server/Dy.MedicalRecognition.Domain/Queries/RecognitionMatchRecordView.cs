using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 互认匹配记录集合视图的组级行投影：按标识取单记录的记录级归属、患者就诊与反馈状态。
/// </summary>
/// <remarks>
/// 记录是否已反馈由组内全部匹配项的反馈状态聚合判定；记录级的互认时间、互认科室与互认医生在组内聚合归一，
/// 未反馈记录没有这些取值；患者姓名经匹配项绑定报告的报告主体检索列取得，组内取值相同。
/// </remarks>
public sealed record RecognitionMatchRecordView
{
  /// <summary>互认匹配记录标识。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配生成时间。</summary>
  public DateTime MatchCreatedTime { get; init; }
  /// <summary>接收组织编码。</summary>
  public string ReceiverOrganizationCode { get; init; } = string.Empty;
  /// <summary>接收医院编码。</summary>
  public string ReceiverHospitalCode { get; init; } = string.Empty;
  /// <summary>接收院区编码。</summary>
  public string ReceiverBranchCode { get; init; } = string.Empty;
  /// <summary>患者姓名：组内各匹配项绑定同一患者，聚合归一为单值。</summary>
  public string? PatientName { get; init; }
  /// <summary>患者证件号码：取匹配记录保存值。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>本次来源就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>本次来源就诊流水号。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>记录是否已反馈：组内全部匹配项均已保存处理结果时为真。</summary>
  public bool IsProcessed { get; init; }
  /// <summary>记录级的互认时间；未反馈记录为空值。</summary>
  public DateTime? RecognitionTime { get; init; }
  /// <summary>记录级的互认科室ID；未反馈记录为空值。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>记录级的互认科室名称。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>记录级的互认医生ID。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>记录级的互认医生名称。</summary>
  public string? RecognitionDoctorName { get; init; }
}
