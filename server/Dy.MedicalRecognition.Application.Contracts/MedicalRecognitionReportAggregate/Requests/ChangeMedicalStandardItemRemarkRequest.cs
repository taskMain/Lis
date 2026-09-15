using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 仅修改标准项目备注的请求。
/// </summary>
[IPropertyChangedAware]
public partial record ChangeMedicalStandardItemRemarkRequest : Dto
{
  /// <summary>
  /// 标准项目标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝；项目编码、名称、所属分类与分组、启用状态均不在本次变更范围内。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目ID不能为空。")]
  public partial Guid Id { get; set; }
  /// <summary>
  /// 备注；按本次提交值整份覆盖，传空表示清空备注。
  /// </summary>
  [StringLength(600, ErrorMessage = "参数校验失败：备注长度不能超过 600。")]
  public partial string? Remark { get; set; }
}
