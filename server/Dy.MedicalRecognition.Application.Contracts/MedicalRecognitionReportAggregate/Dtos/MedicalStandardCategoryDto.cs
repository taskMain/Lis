using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 标准目录中分类层的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record MedicalStandardCategoryDto : Dto
{
  /// <summary>
  /// 分类标识；下级分组通过该标识建立归属。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 项目类型。
  /// </summary>
  /// <remarks>0=检验、1=检查；分类存在下级分组时不可再修改。</remarks>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 分类名称；在全部项目类型范围内唯一。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 启用状态。
  /// </summary>
  /// <remarks>为 false 表示停用，停用是软删除，数据保留且可再启用，不级联改写下级状态。</remarks>
  public partial bool IsValid { get; set; }
  /// <summary>
  /// 备注；传空表示清空备注。
  /// </summary>
  public partial string? Remark { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
