using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 新建标准项目分类的请求。
/// </summary>
/// <remarks>分类名称在全部项目类型范围内唯一；分类以启用状态创建，标识与操作信息由服务端写入。</remarks>
[IPropertyChangedAware]
public partial record CreateMedicalStandardCategoryRequest : Dto
{
  /// <summary>
  /// 项目类型。
  /// </summary>
  /// <remarks>0=检验、1=检查；取值不是已定义项目类型时校验失败。</remarks>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 分类名称；在全部项目类型范围内唯一。
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
