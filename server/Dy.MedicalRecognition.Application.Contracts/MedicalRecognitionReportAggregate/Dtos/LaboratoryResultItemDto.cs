using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 普通检验结果明细的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record LaboratoryResultItemDto : Dto
{
  /// <summary>
  /// 结果明细标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属报告版本标识。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源报文内的明细标识，用于来源明细去重与追溯。
  /// </summary>
  public partial string? SourceDetailKey { get; set; }
  /// <summary>
  /// 来源检验报告中的项目名称，按来源原文保存。
  /// </summary>
  public partial string SourceProjectName { get; set; }
  /// <summary>
  /// 来源系统的项目编码；来源未提供时为空，仅用于来源追溯。
  /// </summary>
  public partial string? SourceProjectCode { get; set; }
  /// <summary>
  /// 命中的互认项目编码；未命中时仍保留在报告内用于展示。
  /// </summary>
  public partial string? StandardProjectCode { get; set; }
  /// <summary>
  /// 来源系统的结果原文，含比较符号，按来源原样保存，平台不解析、不换算。
  /// </summary>
  public partial string SourceResultText { get; set; }
  /// <summary>
  /// 结果类型；1=数值型、2=定性型、3=文本型，为来源受控取值。
  /// </summary>
  public partial LaboratoryResultType ResultType { get; set; }
  /// <summary>
  /// LOINC 编码，平台不代为映射。
  /// </summary>
  public partial string? LoincCode { get; set; }
  /// <summary>
  /// 结果单位，按来源原文保存。
  /// </summary>
  public partial string? Unit { get; set; }
  /// <summary>
  /// 参考范围，按来源原样保存，平台不据此重新判定异常标志。
  /// </summary>
  public partial string? ReferenceRange { get; set; }
  /// <summary>
  /// 检测方法，按来源原文保存。
  /// </summary>
  public partial string? TestingMethod { get; set; }
  /// <summary>
  /// 检测仪器编码，与名称同源。
  /// </summary>
  public partial string? InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称，仅用于展示与追溯。
  /// </summary>
  public partial string? InstrumentName { get; set; }
  /// <summary>
  /// 展示顺序，来源业务序号；同一报告版本内不重复。
  /// </summary>
  public partial int DisplayOrder { get; set; }
  /// <summary>
  /// 异常标志；1=正常、2=偏高、3=偏低、4=其他异常，按来源原样保存，缺失不重算。
  /// </summary>
  public partial LaboratoryAbnormalFlag? AbnormalFlag { get; set; }
  /// <summary>
  /// 危急值标志；按来源判断原样保存，平台不承担通知与处置。
  /// </summary>
  public partial bool? CriticalValueFlag { get; set; }
  /// <summary>
  /// 检验收费项目编码，仅用于来源侧对账追溯。
  /// </summary>
  public partial string? LaboratoryChargeItemCode { get; set; }
  /// <summary>
  /// 医保收费项目编码，仅用于来源侧对账追溯。
  /// </summary>
  public partial string? InsuranceChargeItemCode { get; set; }
  /// <summary>
  /// 明细检测人在来源系统中的人员标识，与名称成对提供。
  /// </summary>
  public partial string? InspectorId { get; set; }
  /// <summary>
  /// 明细检测人名称，平台不依据姓名反查人员。
  /// </summary>
  public partial string? InspectorName { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
