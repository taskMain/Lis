using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 启用标准项目分类的请求。
/// </summary>
/// <remarks>按既有记录改状态，不新建记录。</remarks>
[IPropertyChangedAware]
public partial record EnableMedicalStandardCategoryRequest : Dto
{
  /// <summary>
  /// 分类标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空。")]
  public partial Guid Id { get; set; }
}
