using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.Share.PlatformParameterAggregate.Events;

namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Commands;

/// <summary>
/// 新建一条平台参数。
/// </summary>
/// <remarks>参数编码由应用层按维护请求定位；主键由领域层生成，操作人与操作时间由应用层从可信上下文写入。</remarks>
[ObjectMap(typeof(PlatformParameter), ObjectMapMode.OneWay)]
[ObjectMap(typeof(PlatformParameterCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreatePlatformParameterCommand : ICommand
{
  /// <summary>
  /// 参数编码；全平台唯一。
  /// </summary>
  public partial string ParameterCode { get; set; }

  /// <summary>
  /// 参数值；按参数编码对应的规则校验。
  /// </summary>
  public partial string ParameterValue { get; set; }

  /// <summary>
  /// 参数说明；仅用于维护界面展示。
  /// </summary>
  public partial string? Description { get; set; }

  /// <summary>
  /// 是否启用；停用后业务读取按缺失处理。
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
  /// 为本次新建的参数构造“参数已创建”领域事件。
  /// </summary>
  /// <param name="id">新建参数的主键标识。</param>
  /// <returns>参数创建领域事件。</returns>
  public PlatformParameterCreatedEvent CreatePlatformParameterCreatedEvent(Guid id) =>
    this.MapToPlatformParameterCreatedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
}
