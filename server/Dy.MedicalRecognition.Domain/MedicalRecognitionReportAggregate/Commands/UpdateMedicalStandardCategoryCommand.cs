using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 修改标准医疗项目分类。
/// </summary>
/// <remarks>保存分类的名称、项目类型与备注；分类已有下级分组时项目类型不可改变；分类名称在全部项目类型范围内唯一，重名校验排除本分类自身。</remarks>
[ObjectMap(typeof(MedicalStandardCategory), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MedicalStandardCategoryUpdatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record UpdateMedicalStandardCategoryCommand : ICommand
{
  /// <summary>
  /// 待修改分类标识，取自请求体。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 期望保存的项目类型。
  /// </summary>
  /// <remarks>分类已有下级分组时不可改变。</remarks>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 期望保存的分类名称。
  /// </summary>
  /// <remarks>取自请求体；在全部项目类型范围内唯一，重名校验排除本分类自身。</remarks>
  public partial string Name { get; set; }
  /// <summary>
  /// 期望保存的分类备注，可空；本次值整体覆盖原备注。
  /// </summary>
  public partial string? Remark { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；命令时间只用于领域事件，实体列另取数据库当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 为本次修改构造“分类已更新”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <returns>分类更新领域事件。</returns>
  public MedicalStandardCategoryUpdatedEvent CreateMedicalStandardCategoryUpdatedEvent()
  {
    return this.MapToMedicalStandardCategoryUpdatedEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
