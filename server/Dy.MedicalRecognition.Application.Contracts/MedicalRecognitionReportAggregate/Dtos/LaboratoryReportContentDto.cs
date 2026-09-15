using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 检验报告专项内容的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record LaboratoryReportContentDto : Dto
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
  /// 来源系统的报告类别编码，仅用于展示与追溯。
  /// </summary>
  public partial string? ReportCategoryCode { get; set; }
  /// <summary>
  /// 来源系统的报告类别名称，与编码同源。
  /// </summary>
  public partial string? ReportCategoryName { get; set; }
  /// <summary>
  /// 来源报告备注，平台不补充默认文案。
  /// </summary>
  public partial string? ReportRemark { get; set; }
  /// <summary>
  /// 来源报告整体异常标识原文，未归一化为布尔值；仅用于列表提示与人工核对。
  /// </summary>
  public partial string? OverallAbnormalFlag { get; set; }
  /// <summary>
  /// 来源医嘱流水号，仅用于来源追溯。
  /// </summary>
  public partial string? SourceOrderSerialNo { get; set; }
  /// <summary>
  /// 标本采集时间，缺失时平台不推算补齐。
  /// </summary>
  public partial DateTime? SpecimenCollectedTime { get; set; }
  /// <summary>
  /// 标本送检时间，缺失时平台不推算补齐。
  /// </summary>
  public partial DateTime? SpecimenSubmittedTime { get; set; }
  /// <summary>
  /// 检验科接收标本时间，不替代检测完成时间。
  /// </summary>
  public partial DateTime? LaboratoryReceivedTime { get; set; }
  /// <summary>
  /// 院内标本号，用于人工核对与追溯；按来源原文保存，缺失不推算补齐。
  /// </summary>
  public partial string SourceSpecimenNo { get; set; }
  /// <summary>
  /// 标本类型编码，按来源原文保存；该编码不随互认匹配响应返回。
  /// </summary>
  public partial string SpecimenTypeCode { get; set; }
  /// <summary>
  /// 标本类型名称，与编码成对提供。
  /// </summary>
  public partial string SpecimenTypeName { get; set; }
  /// <summary>
  /// 检测完成时间，是互认时间窗判定的依据。
  /// </summary>
  public partial DateTime TestingCompletedTime { get; set; }
  /// <summary>
  /// 报告级检验人在来源系统中的人员标识，不要求为平台用户标识。
  /// </summary>
  public partial string InspectorId { get; set; }
  /// <summary>
  /// 报告级检验人名称，与标识成对提供。
  /// </summary>
  public partial string InspectorName { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
