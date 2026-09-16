using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 新建互认项目配置的命令：为可信组织建立一个标准项目的可互认时间配置，配置以启用状态创建。
/// </summary>
/// <remarks>
/// 组织编码取自登录令牌的组织声明、操作人取自登录令牌的用户标识，标准项目标识由应用层按请求提交的编码读取标准项目后写入，
/// 操作时间取服务端当前时间；这些字段都不是调用方提交值。
/// 创建后的配置即为启用状态，调用方不能提交启用状态。
/// 标准项目、所属分类与所属分组是否当前有效由领域层在写入前判断。
/// </remarks>
[ObjectMap(typeof(MutualRecognitionItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MutualRecognitionItemCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemCommand : ICommand
{
  /// <summary>
  /// 组织编码；取自登录令牌的组织声明，决定配置归属。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 标准项目标识；由应用层按请求提交的标准项目编码读取标准项目后写入，编码读不到标准项目即拒绝。
  /// </summary>
  public partial Guid StandardItemId { get; set; }
  /// <summary>
  /// 标准项目编码；与标准项目标识指向同一目录记录。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 可互认时间天数；只允许正整数。
  /// </summary>
  public partial int RecognitionDurationDays { get; set; }
  /// <summary>
  /// 操作人；取自登录令牌的用户标识。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；取自服务端当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 创建“互认项目配置已创建”事件；配置以启用状态创建，因此事件状态固定为启用。
  /// </summary>
  /// <param name="id">服务端生成的配置标识。</param>
  /// <returns>待登记的创建事件。</returns>
  public MutualRecognitionItemCreatedEvent CreateMutualRecognitionItemCreatedEvent(Guid id)
  {
    return this.MapToMutualRecognitionItemCreatedEvent(id: id, isValid: true, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
