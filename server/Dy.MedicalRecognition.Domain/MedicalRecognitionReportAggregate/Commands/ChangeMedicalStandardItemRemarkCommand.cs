using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 用本次提交的文本整体覆盖标准医疗项目的备注。
/// </summary>
/// <remarks>编码、名称、所属分类与分组、启用状态均不在此命令。</remarks>
[ObjectMap(typeof(MedicalStandardItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MedicalStandardItemRemarkChangedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record ChangeMedicalStandardItemRemarkCommand : ICommand
{
  /// <summary>
  /// 待修改备注的标准项目标识，取自请求体。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 覆盖后的备注文本。
  /// </summary>
  /// <remarks>可空；不区分空串与 <see langword="null"/>，本次值整体覆盖原备注，为空表示本次改为不填写备注。</remarks>
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
  /// 为本次备注修改构造“标准项目备注已修改”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <returns>备注变更领域事件。</returns>
  public MedicalStandardItemRemarkChangedEvent CreateMedicalStandardItemRemarkChangedEvent()
  {
    return this.MapToMedicalStandardItemRemarkChangedEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
