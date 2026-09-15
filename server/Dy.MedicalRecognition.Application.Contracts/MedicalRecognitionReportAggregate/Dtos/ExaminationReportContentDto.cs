using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 检查报告专项内容的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record ExaminationReportContentDto : Dto
{
  /// <summary>
  /// 专项内容标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属报告版本标识；一个报告版本至多一份专项内容。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源系统的检查类型编码，仅用于展示与追溯。
  /// </summary>
  public partial string? SourceExaminationTypeCode { get; set; }
  /// <summary>
  /// 来源系统的检查类型名称，与编码同源。
  /// </summary>
  public partial string? SourceExaminationTypeName { get; set; }
  /// <summary>
  /// 来源报告备注，平台不补充默认文案。
  /// </summary>
  public partial string? ReportRemark { get; set; }
  /// <summary>
  /// 来源报告整体异常标识原文，未归一化为布尔值；仅用于列表提示与人工核对。
  /// </summary>
  public partial string? OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 检查所见，按来源原文保存；必填，平台不重新推断或改写。
  /// </summary>
  public partial string Findings { get; set; }
  /// <summary>
  /// 检查结论，按来源原文保存；必填，平台不重新推断或改写。
  /// </summary>
  public partial string Conclusion { get; set; }
  /// <summary>
  /// 病情描述，仅用于展示与追溯。
  /// </summary>
  public partial string? ConditionDescription { get; set; }
  /// <summary>
  /// 检查目的，仅用于展示与追溯。
  /// </summary>
  public partial string? ExaminationPurpose { get; set; }
  /// <summary>
  /// 来源临床诊断编码，平台不映射平台诊断字典；诊断文本按来源原文保存。
  /// </summary>
  public partial string? SourceDiagnosisCode { get; set; }
  /// <summary>
  /// 来源临床诊断名称，按来源原文保存。
  /// </summary>
  public partial string SourceDiagnosisName { get; set; }
  /// <summary>
  /// 实际检查时间；是互认匹配时间窗的判定依据。
  /// </summary>
  public partial DateTime ExaminationTime { get; set; }
  /// <summary>
  /// 检查医生在来源系统中的人员标识，不要求为平台用户标识。
  /// </summary>
  public partial string ExaminerId { get; set; }
  /// <summary>
  /// 检查医生名称，平台不依据姓名反查人员。
  /// </summary>
  public partial string ExaminerName { get; set; }
  /// <summary>
  /// 来源影像状态；1=有影像、2=无影像、3=未知；无影像时调阅地址必须为空，且不按地址反推状态。
  /// </summary>
  public partial SourceImageStatus SourceImageStatus { get; set; }
  /// <summary>
  /// 影像调阅地址；平台原样保存与返回，不改写。
  /// </summary>
  public partial string? ImageAccessUrl { get; set; }
  /// <summary>
  /// 检查方法，按来源原文保存。
  /// </summary>
  public partial string? ExaminationMethod { get; set; }
  /// <summary>
  /// 检查设备编码，与名称同源。
  /// </summary>
  public partial string? DeviceCode { get; set; }
  /// <summary>
  /// 检查设备名称，仅用于展示与追溯。
  /// </summary>
  public partial string? DeviceName { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
