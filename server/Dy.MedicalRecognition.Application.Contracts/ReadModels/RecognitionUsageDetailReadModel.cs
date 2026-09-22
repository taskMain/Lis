using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 接收侧互认使用明细行：提醒、采纳、不采纳与引用四类事实的行级投影。
/// </summary>
/// <remarks>
/// 行的业务时间随明细类型不同：提醒按互认匹配生成时间、采纳与不采纳按互认时间、引用按实际引用时间；
/// 未反馈匹配项只进入提醒明细（<see cref="IsUnprocessed"/> 为真），不推断为不采纳；
/// 患者姓名取自匹配项绑定报告的报告主体检索列，证件号码取匹配记录保存值，均按业务原值返回。
/// </remarks>
public sealed record RecognitionUsageDetailReadModel
{
  /// <summary>互认匹配记录标识；用于打开匹配记录集合视图。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配项标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>互认匹配生成时间；提醒口径的业务时间。</summary>
  public DateTime MatchCreatedTime { get; init; }
  /// <summary>本次来源就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>
  /// 本次来源就诊类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>就诊类型来自报告主体且集合封闭，未登记取值按严格解析抛出，不静默降级。</remarks>
  public string VisitTypeText => EnumDescriptorText.Get(VisitType, VisitTypeDescriptorList.List);
  /// <summary>本次来源就诊流水号。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>来源归属：来源组织、医院与院区编码及回填名称。</summary>
  public SourceOrganizationReadModel Source { get; init; } = new();
  /// <summary>接收归属：接收组织、医院与院区编码及回填名称。</summary>
  public ReceiverOrganizationReadModel Receiver { get; init; } = new();
  /// <summary>互认项目：项目类型与标准目录资料。</summary>
  public RecognitionStatisticsItemReadModel Item { get; init; } = new();
  /// <summary>业务时间；随明细类型分别取匹配生成时间、互认时间或实际引用时间。</summary>
  public DateTime BusinessTime { get; init; }
  /// <summary>是否未反馈处理结果；未反馈项只进入提醒明细。</summary>
  public bool IsUnprocessed { get; init; }
  /// <summary>患者姓名；按业务原值返回，取自匹配项绑定报告的报告主体检索列。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者证件号码；取匹配记录保存值，按业务原值返回。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>互认科室ID；取各业务记录自身保存值，未反馈项无科室归属。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称；取各业务记录自身保存的名称。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>互认医生ID；取各业务记录自身保存的医院业务人员标识。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>互认医生名称；取各业务记录自身保存的名称。</summary>
  public string? RecognitionDoctorName { get; init; }
  /// <summary>处理结果事实；恒返回非空对象，未反馈项以 <see cref="RecognitionProcessingResultDetailReadModel.IsProcessed"/> 为假区分。</summary>
  public RecognitionProcessingResultDetailReadModel? ProcessingResult { get; init; }
  /// <summary>引用事实；未引用项为 <see langword="null"/>。</summary>
  public RecognitionReferenceDetailReadModel? Reference { get; init; }
}
