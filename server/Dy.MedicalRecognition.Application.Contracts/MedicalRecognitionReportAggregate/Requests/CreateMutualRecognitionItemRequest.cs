using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemRequest : Dto
{
  /// <summary>
  /// 组织编码
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public partial int RecognitionDurationDays { get; set; }
}
