using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

[IPropertyChangedAware]
public partial record MutualRecognitionItemDto : Dto
{
  /// <summary>
  /// ID
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目ID
  /// </summary>
  public partial Guid StandardItemId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public partial int RecognitionDurationDays { get; set; }
  /// <summary>
  /// 是否启用
  /// </summary>
  public partial bool IsValid { get; set; }
  /// <summary>
  /// 操作人
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
