using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 新建标准项目分组的请求。
/// </summary>
/// <remarks>分组名称在同一分类内唯一，跨分类可重名；分组以启用状态创建，标识与操作信息由服务端写入。</remarks>
[IPropertyChangedAware]
public partial record CreateMedicalStandardGroupRequest : Dto
{
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝；上级分类必须存在且处于启用状态。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空。")]
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 分组名称；在同一分类内唯一，跨分类可重名。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：名称不能为空。")]
  [StringLength(200, ErrorMessage = "参数校验失败：名称长度不能超过 200。")]
  public partial string Name { get; set; }
  /// <summary>
  /// 备注；为空表示本次不填写备注。
  /// </summary>
  [StringLength(600, ErrorMessage = "参数校验失败：备注长度不能超过 600。")]
  public partial string? Remark { get; set; }
}
