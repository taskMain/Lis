using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 标准目录中分组层的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record MedicalStandardGroupDto : Dto
{
  /// <summary>
  /// 分组标识；下级标准项目通过该标识建立归属。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属分类标识；分组创建时确定，此后不跨分类迁移。
  /// </summary>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 分组名称；在同一分类内唯一，跨分类可重名。
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
