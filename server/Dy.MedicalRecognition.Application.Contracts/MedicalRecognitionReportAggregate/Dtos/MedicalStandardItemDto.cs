using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 标准目录中项目层的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record MedicalStandardItemDto : Dto
{
  /// <summary>
  /// 标准项目标识；互认配置按该标识或项目编码引用标准项目。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  /// <remarks>创建时与分组须构成真实父子关系且均处于启用状态。</remarks>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  public partial Guid GroupId { get; set; }
  /// <summary>
  /// 标准项目编码。
  /// </summary>
  /// <remarks>全平台唯一；停用项目仍占用其编码。</remarks>
  public partial string Code { get; set; }
  /// <summary>
  /// 标准项目名称；创建后不允许通过普通修改改变。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 启用状态。
  /// </summary>
  /// <remarks>为 false 表示停用；项目状态与上级启停相互独立，仅当两者同时启用时该项目才属于当前有效标准项目。</remarks>
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
