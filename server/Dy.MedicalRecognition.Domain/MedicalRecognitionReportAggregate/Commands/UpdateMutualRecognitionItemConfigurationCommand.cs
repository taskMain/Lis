using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 修改互认项目配置可互认时间的命令：只改变可互认时间天数，配置归属、标准项目与启用状态保持原值。
/// </summary>
/// <remarks>组织编码、操作人与操作时间由应用层补入；配置是否属于该组织由领域层在写入前按组织范围读取判断。</remarks>
[ObjectMap(typeof(MutualRecognitionItem), ObjectMapMode.OneWay)]
[ObjectMap(typeof(MutualRecognitionItemConfigurationUpdatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record UpdateMutualRecognitionItemConfigurationCommand : ICommand
{
  /// <summary>
  /// 配置标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 组织编码；取自登录令牌的组织声明，用于按组织范围定位配置，调用方不能提交。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 可互认时间天数；只允许正整数，与当前值相同时仍照常保存。
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
  /// 创建“互认项目配置已修改”事件；组织编码取命令中的可信组织编码，标准项目编码取已校验归属的配置记录。
  /// </summary>
  /// <param name="standardProjectCode">已按组织范围读取到的配置记录中的标准项目编码。</param>
  /// <returns>待登记的修改事件。</returns>
  public MutualRecognitionItemConfigurationUpdatedEvent CreateMutualRecognitionItemConfigurationUpdatedEvent(string standardProjectCode)
  {
    return this.MapToMutualRecognitionItemConfigurationUpdatedEvent(standardProjectCode: standardProjectCode, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
