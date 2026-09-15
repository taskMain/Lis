using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 修改标准项目分类的请求。
/// </summary>
/// <remarks>启用状态保持不变。</remarks>
[IPropertyChangedAware]
public partial record UpdateMedicalStandardCategoryRequest : Dto
{
  /// <summary>
  /// 分类标识。
  /// </summary>
  /// <remarks>必需值，空 Guid 由请求校验拒绝。</remarks>
  [NonEmpty(ErrorMessage = "参数校验失败：分类ID不能为空。")]
  public partial Guid Id { get; set; }
  /// <summary>
  /// 项目类型。
  /// </summary>
  /// <remarks>0=检验、1=检查；存在下级分组时不可变更。</remarks>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 分类名称。
  /// </summary>
  /// <remarks>在全部项目类型范围内唯一；改名时排除自身。</remarks>
  [Required(ErrorMessage = "参数校验失败：名称不能为空。")]
  [StringLength(200, ErrorMessage = "参数校验失败：名称长度不能超过 200。")]
  public partial string Name { get; set; }
  /// <summary>
  /// 备注；按本次提交值整份覆盖，传空表示清空备注。
  /// </summary>
  [StringLength(600, ErrorMessage = "参数校验失败：备注长度不能超过 600。")]
  public partial string? Remark { get; set; }
}
