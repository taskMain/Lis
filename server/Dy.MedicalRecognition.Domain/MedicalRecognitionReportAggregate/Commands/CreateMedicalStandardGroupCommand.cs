using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 在指定标准医疗项目分类下新建分组。
/// </summary>
/// <remarks>标识与初始启用状态由领域层生成，不在此命令；所属分类须存在且处于启用状态；分组名称在同一分类内唯一，跨分类可重名。</remarks>
[ObjectMap(typeof(MedicalStandardGroup), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MedicalStandardGroupCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMedicalStandardGroupCommand : ICommand
{
  /// <summary>
  /// 新分组所属分类标识，取自请求体；须存在且处于启用状态。
  /// </summary>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 新分组的名称。
  /// </summary>
  /// <remarks>同一分类内唯一，跨分类可重名。</remarks>
  public partial string Name { get; set; }
  /// <summary>
  /// 新分组的备注，可空；为空表示不填写。
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
  /// 为本次新建的分组构造“分组已创建”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <param name="id">新建分组主键。</param>
  /// <returns>分组创建领域事件。</returns>
  public MedicalStandardGroupCreatedEvent CreateMedicalStandardGroupCreatedEvent(Guid id)
  {
    return this.MapToMedicalStandardGroupCreatedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
