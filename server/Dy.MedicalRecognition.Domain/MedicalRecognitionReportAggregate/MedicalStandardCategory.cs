using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录的最高层级，规定其下的标准项目归入检验还是检查。
/// </summary>
[IPropertyChangedAware]
public partial class MedicalStandardCategory : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 分类主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 项目类型；分类已有下级分组时不允许改变项目类型。
  /// </summary>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 分类名称；在全部项目类型范围内唯一。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 启用状态。
  /// </summary>
  /// <remarks>停用只改变本分类自身、不级联下级；分类及其下级数据保留并可重新启用。</remarks>
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
