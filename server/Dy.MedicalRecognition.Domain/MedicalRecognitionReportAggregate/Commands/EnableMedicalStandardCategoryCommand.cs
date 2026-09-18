using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 启用指定的标准医疗项目分类。
/// </summary>
/// <remarks>只改本分类状态而不级联下级；已启用时直接成功且不落库不登记事件。</remarks>
[ObjectMap(typeof(MedicalStandardCategoryEnabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record EnableMedicalStandardCategoryCommand : ICommand
{
  /// <summary>
  /// 待启用分类标识，取自请求体。
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
  /// 为本次状态变更构造“分类已启用”领域事件。
  /// </summary>
  /// <returns>分类启用领域事件。</returns>
  public MedicalStandardCategoryEnabledEvent CreateMedicalStandardCategoryEnabledEvent()
  {
    return this.MapToMedicalStandardCategoryEnabledEvent(eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
