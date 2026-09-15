using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 新建标准医疗项目分类。
/// </summary>
/// <remarks>标识与初始启用状态由领域层生成，不在此命令；分类名称在全部项目类型范围内唯一。</remarks>
[ObjectMap(typeof(MedicalStandardCategory), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MedicalStandardCategoryCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMedicalStandardCategoryCommand : ICommand
{
  /// <summary>
  /// 新分类的项目类型，取自请求体。
  /// </summary>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 新分类的名称。
  /// </summary>
  /// <remarks>在全部项目类型范围内唯一。</remarks>
  public partial string Name { get; set; }
  /// <summary>
  /// 新分类的备注，可空；为空表示不填写。
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
  /// 为本次新建的分类构造“分类已创建”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <param name="id">新建分类主键。</param>
  /// <returns>分类创建领域事件。</returns>
  public MedicalStandardCategoryCreatedEvent CreateMedicalStandardCategoryCreatedEvent(Guid id)
  {
    return this.MapToMedicalStandardCategoryCreatedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
