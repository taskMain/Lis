using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 启用指定的标准医疗项目。
/// </summary>
/// <remarks>只改自身不级联父级；父级停用时也可启用，但父级启用前该项目不属于有效目录；已启用时直接成功且不落库不登记事件。</remarks>
[ObjectMap(typeof(MedicalStandardItemEnabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record EnableMedicalStandardItemCommand : ICommand
{
  /// <summary>
  /// 待启用标准项目标识，取自请求体。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；命令时间只用于领域事件，实体列另取数据库当前时间。
  /// </summary>
  public DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 为本次状态变更构造“标准项目已启用”领域事件。
  /// </summary>
  /// <returns>标准项目启用领域事件。</returns>
  public MedicalStandardItemEnabledEvent CreateMedicalStandardItemEnabledEvent()
  {
    return this.MapToMedicalStandardItemEnabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
