using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 停用标准项目分组的请求。
/// </summary>
/// <remarks>分组及其下级标准项目数据保留、可重新启用。</remarks>
[IPropertyChangedAware]
public partial record DisableMedicalStandardGroupRequest : Dto
{
  /// <summary>
  /// 分组标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分组ID不能为空。")]
  public partial Guid Id { get; set; }
}
