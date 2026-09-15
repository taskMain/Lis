using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 新建标准项目的请求。
/// </summary>
/// <remarks>标准项目编码全平台唯一，停用项目仍占用其编码；项目类型继承自所属分类；项目以启用状态创建，标识与操作信息由服务端写入。</remarks>
[IPropertyChangedAware]
public partial record CreateMedicalStandardItemRequest : Dto
{
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝；分类必须存在且处于启用状态。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空。")]
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝；分组必须存在、启用且属于所选分类。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分组ID不能为空。")]
  public partial Guid GroupId { get; set; }
  /// <summary>
  /// 标准项目编码。
  /// </summary>
  /// <remarks>全平台唯一，停用项目仍占用其编码。</remarks>
  [Required(ErrorMessage = "参数校验失败：标准项目编码不能为空。")]
  [StringLength(200, ErrorMessage = "参数校验失败：标准项目编码长度不能超过 200。")]
  public partial string Code { get; set; }
  /// <summary>
  /// 标准项目名称；创建后不允许通过普通修改改变。
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
