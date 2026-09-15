using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 检查报告下检查项目明细的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record ExaminationItemDto : Dto
{
  /// <summary>
  /// 检查项目明细标识；检查部位通过该标识确定归属。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属报告版本标识；明细按报告版本保存，不在版本之间复用。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 来源检查报告中的项目名称，按来源原文保存。
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
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
