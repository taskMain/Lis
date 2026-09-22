using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 接收侧提醒明细的行级投影：统计范围内按匹配生成时间落入的匹配项，含尚未反馈处理结果的项。
/// </summary>
/// <remarks>
/// 处理结果字段为空值表示该项未反馈，由应用层显示「未反馈」，不推断为不采纳；
/// 患者姓名取自匹配项绑定报告的报告主体检索列，证件号码取匹配记录保存值；
/// 来源三值来自匹配项绑定报告的主体，经左联取得，报告缺失时为空值。
/// </remarks>
public sealed record RecognitionReminderDetailItem
{
  /// <summary>互认匹配记录标识；用于打开匹配记录集合视图。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配项标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>互认匹配生成时间；提醒口径的业务时间。</summary>
  public DateTime MatchCreatedTime { get; init; }
  /// <summary>本次来源就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>本次来源就诊流水号。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>接收组织编码。</summary>
  public string ReceiverOrganizationCode { get; init; } = string.Empty;
  /// <summary>接收医院编码。</summary>
  public string ReceiverHospitalCode { get; init; } = string.Empty;
  /// <summary>接收院区编码。</summary>
  public string ReceiverBranchCode { get; init; } = string.Empty;
  /// <summary>来源组织编码；取自匹配项绑定报告的主体。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源医院编码。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>项目类型：取匹配项自身保存值。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>标准目录分类名称：实时取自标准目录，编码不在目录内时为空值。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称。</summary>
  public string? GroupName { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称：实时取自标准目录。</summary>
  public string? StandardItemName { get; init; }
  /// <summary>患者姓名：取自匹配项绑定报告的报告主体检索列。</summary>
  public string? PatientName { get; init; }
  /// <summary>患者证件号码：取匹配记录保存值。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>处理结果的互认时间；空值表示该项未反馈。</summary>
  public DateTime? ProcessingTime { get; init; }
  /// <summary>该项的处理结果决策；首次保存后不可更改，未反馈时为空值。</summary>
  public RecognitionResult? Decision { get; init; }
  /// <summary>互认科室ID：取处理结果自身保存值，未反馈为空值。</summary>
  public string? ProcessingDeptId { get; init; }
  /// <summary>互认科室名称：取各业务记录自身保存的名称。</summary>
  public string? ProcessingDeptName { get; init; }
  /// <summary>互认医生ID：取处理结果自身保存值。</summary>
  public string? ProcessingDoctorId { get; init; }
  /// <summary>互认医生名称。</summary>
  public string? ProcessingDoctorName { get; init; }
}
