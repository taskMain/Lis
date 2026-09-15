using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录的末级对象，同时归属一个分类与一个分组。
/// </summary>
[IPropertyChangedAware]
public partial class MedicalStandardItem : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 标准项目主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属分类标识；创建后不可变更。
  /// </summary>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  /// <remarks>所属分组必须属于该分类，且创建后不可变更。</remarks>
  public partial Guid GroupId { get; set; }
  /// <summary>
  /// 标准项目编码。
  /// </summary>
  /// <remarks>在全部项目类型范围内全局唯一，检验与检查之间不可重复；已停用项目仍占用其编码。</remarks>
  public partial string Code { get; set; }
  /// <summary>
  /// 标准项目名称；不参与编码唯一性判断。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 启用状态。
  /// </summary>
  /// <remarks>停用后不再参与有效目录匹配；项目状态与上级启停相互独立，仅当两者同时启用时才属于当前有效标准项目。</remarks>
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
