using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 互认匹配记录集合视图：按匹配记录标识取单记录的组级信息，弹窗呈现。
/// </summary>
/// <remarks>
/// 入口只挂接收侧提醒、采纳、不采纳与引用四类明细行；记录级的是否已反馈取组内全部匹配项的反馈状态，
/// 未反馈记录没有互认时间、互认科室与互认医生；患者姓名与证件号码按业务原值返回。
/// </remarks>
public sealed record RecognitionMatchRecordReadModel
{
  /// <summary>互认匹配记录标识。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配生成时间。</summary>
  public DateTime MatchCreatedTime { get; init; }
  /// <summary>接收归属：接收组织、医院与院区编码及回填名称。</summary>
  public ReceiverOrganizationReadModel Receiver { get; init; } = new();
  /// <summary>患者姓名；按业务原值返回。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者证件号码；按业务原值返回。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
  /// <summary>本次来源就诊类型。</summary>
  public VisitType VisitType { get; init; }
  /// <summary>
  /// 本次来源就诊类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>就诊类型来自报告主体且集合封闭，未登记取值按严格解析抛出，不静默降级。</remarks>
  public string VisitTypeText => EnumDescriptorText.Get(VisitType, VisitTypeDescriptorList.List);
  /// <summary>本次来源就诊流水号。</summary>
  public string VisitSerialNo { get; init; } = string.Empty;
  /// <summary>记录是否已反馈；组内全部匹配项均已保存处理结果时为真。</summary>
  public bool IsProcessed { get; init; }
  /// <summary>处理结果的互认时间；未反馈记录无值。</summary>
  public DateTime? RecognitionTime { get; init; }
  /// <summary>互认科室ID；取处理结果保存值，未反馈记录无值。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称；取处理结果保存的名称。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>互认医生ID；取处理结果保存的医院业务人员标识。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>互认医生名称；取处理结果保存的名称。</summary>
  public string? RecognitionDoctorName { get; init; }
  /// <summary>全部匹配项集合；逐项携带项目资料、来源归属与各自的反馈状态。</summary>
  public IReadOnlyList<RecognitionMatchRecordItemReadModel> MatchItems { get; init; } = [];
}
