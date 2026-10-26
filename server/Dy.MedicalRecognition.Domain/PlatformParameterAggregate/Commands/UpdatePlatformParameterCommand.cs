using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.Share.PlatformParameterAggregate.Events;

namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Commands;

/// <summary>
/// 修改一条已存在的平台参数。
/// </summary>
/// <remarks>参数编码用于定位，保存时不允许改变；操作人与操作时间由应用层从可信上下文写入。</remarks>
[ObjectMap(typeof(PlatformParameter), ObjectMapMode.OneWay)]
[ObjectMap(typeof(PlatformParameterUpdatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record UpdatePlatformParameterCommand : ICommand
{
  /// <summary>
  /// 参数主键标识。
  /// </summary>
  public partial Guid Id { get; set; }

  /// <summary>
  /// 参数编码；保存时只用于定位与校验，不改变已有编码。
  /// </summary>
  public partial string ParameterCode { get; set; }

  /// <summary>
  /// 修改后的参数值；按参数编码对应的规则校验。
  /// </summary>
  public partial string ParameterValue { get; set; }

  /// <summary>
  /// 修改后的参数说明；仅用于维护界面展示。
  /// </summary>
  public partial string? Description { get; set; }

  /// <summary>
  /// 修改后的启用状态；停用后业务读取按缺失处理。
  /// </summary>
  public partial bool IsValid { get; set; }

  /// <summary>
  /// 操作人标识；由应用服务从 IAM 当前上下文取得。
  /// </summary>
  public partial Guid OperId { get; set; }

  /// <summary>
  /// 操作时间；由应用服务在保存前取得。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 为本次修改的参数构造“参数已更新”领域事件。
  /// </summary>
  /// <returns>参数更新领域事件。</returns>
  public PlatformParameterUpdatedEvent CreatePlatformParameterUpdatedEvent() =>
    this.MapToPlatformParameterUpdatedEvent(eventCreator: OperId, eventCreatedTime: OperTime);
}
