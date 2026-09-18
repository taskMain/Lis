using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 停用指定的标准医疗项目分组。
/// </summary>
/// <remarks>软删除，数据保留可再启用；只改自身不级联下级；停用后其下标准项目离开有效目录；已停用时直接成功且不落库不登记事件。</remarks>
[ObjectMap(typeof(MedicalStandardGroupDisabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record DisableMedicalStandardGroupCommand : ICommand
{
  /// <summary>
  /// 待停用分组标识，取自请求体。
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
  /// 为本次状态变更构造“分组已停用”领域事件。
  /// </summary>
  /// <returns>分组停用领域事件。</returns>
  public MedicalStandardGroupDisabledEvent CreateMedicalStandardGroupDisabledEvent()
  {
    return this.MapToMedicalStandardGroupDisabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
