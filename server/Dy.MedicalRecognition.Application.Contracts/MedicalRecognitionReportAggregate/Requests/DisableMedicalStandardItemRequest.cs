using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 停用标准项目的请求。
/// </summary>
/// <remarks>项目数据与备注保留、可重新启用。</remarks>
[IPropertyChangedAware]
public partial record DisableMedicalStandardItemRequest : Dto
{
  /// <summary>
  /// 标准项目标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目ID不能为空。")]
  public partial Guid Id { get; set; }
}
