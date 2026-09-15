using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录的中间层级，向上归属一个分类、向下容纳多个标准项目。
/// </summary>
[IPropertyChangedAware]
public partial class MedicalStandardGroup : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 分组主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  /// <remarks>创建后不可迁移，更新时回填原值。</remarks>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 分组名称；在同一分类内唯一，跨分类可重名。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 启用状态。
  /// </summary>
  /// <remarks>停用只改变本分组自身、不级联下级，数据保留可再启用；停用分组下的标准项目不再属于有效目录。</remarks>
  public partial bool IsValid { get; set; }
  /// <summary>
  /// 备注；按提交值整体覆盖，为空表示未填写。
  /// </summary>
  public partial string? Remark { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
