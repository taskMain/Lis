using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 启用指定的标准医疗项目分组。
/// </summary>
/// <remarks>只改自身不级联下级；父分类停用时也可启用，但下级项目在父分类启用前不属于有效目录；已启用时直接成功且不落库不登记事件。</remarks>
[ObjectMap(typeof(MedicalStandardGroupEnabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record EnableMedicalStandardGroupCommand : ICommand
{
  /// <summary>
  /// 待启用分组标识，取自请求体。
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
  /// 为本次状态变更构造“分组已启用”领域事件。
  /// </summary>
  /// <returns>分组启用领域事件。</returns>
  public MedicalStandardGroupEnabledEvent CreateMedicalStandardGroupEnabledEvent()
  {
    return this.MapToMedicalStandardGroupEnabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
