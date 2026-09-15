using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 检查项目下检查部位的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record ExaminationSiteDto : Dto
{
  /// <summary>
  /// 检查部位标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属检查项目明细标识；检查部位不能脱离检查项目单独存在，项目无明确部位时该集合允许为空。
  /// </summary>
  public partial Guid ExaminationItemId { get; set; }
  /// <summary>
  /// 来源系统的部位编码，仅用于来源追溯。
  /// </summary>
  public partial string? SourceSiteCode { get; set; }
  /// <summary>
  /// 部位名称，按来源原文保存；平台不映射到统一解剖部位字典。
  /// </summary>
  public partial string SiteName { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
