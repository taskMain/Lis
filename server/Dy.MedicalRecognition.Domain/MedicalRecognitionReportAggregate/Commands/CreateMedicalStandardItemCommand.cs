using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 在指定分类与分组下新建标准医疗项目。
/// </summary>
/// <remarks>标识与初始启用状态由领域层生成，不在此命令；所属分组须存在、启用且属于所选分类；编码全平台唯一，检验与检查之间不可重复，停用项目仍占用其编码。</remarks>
[ObjectMap(typeof(MedicalStandardItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MedicalStandardItemCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMedicalStandardItemCommand : ICommand
{
  /// <summary>
  /// 新标准项目所属分类标识，取自请求体；须存在且处于启用状态。
  /// </summary>
  public partial Guid CategoryId { get; set; }
  /// <summary>
  /// 新标准项目所属分组标识。
  /// </summary>
  /// <remarks>取自请求体；须存在、启用且属于所选分类。</remarks>
  public partial Guid GroupId { get; set; }
  /// <summary>
  /// 新标准项目编码。
  /// </summary>
  /// <remarks>取自请求体；全平台唯一，检验与检查之间不可重复，停用项目仍占用其编码。</remarks>
  public partial string Code { get; set; }
  /// <summary>
  /// 新标准项目名称，取自请求体；不参与编码唯一性判断。
  /// </summary>
  public partial string Name { get; set; }
  /// <summary>
  /// 新标准项目的备注，可空；为空表示不填写。
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
  /// 为本次新建的标准项目构造“标准项目已创建”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <param name="id">新建标准项目主键。</param>
  /// <returns>标准项目创建领域事件。</returns>
  public MedicalStandardItemCreatedEvent CreateMedicalStandardItemCreatedEvent(Guid id)
  {
    return this.MapToMedicalStandardItemCreatedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
