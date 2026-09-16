using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 启用互认项目配置的命令：把可信组织内已停用的配置改为启用，使该标准项目重新参与互认匹配。
/// </summary>
/// <remarks>
/// 组织编码、操作人与操作时间由应用层补入，调用方不能提交配置的原状态；重复启用为幂等成功且不登记事件。
/// </remarks>
[ObjectMap(typeof(MutualRecognitionItemEnabledEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
public record EnableMutualRecognitionItemCommand : ICommand
{
  /// <summary>
  /// 配置标识。
  /// </summary>
  public Guid Id { get; set; }
  /// <summary>
  /// 组织编码；取自登录令牌的组织声明，用于按组织范围定位配置，调用方不能提交。
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 操作人；取自登录令牌的用户标识。
  /// </summary>
  public Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；取自服务端当前时间。
  /// </summary>
  public DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 创建“互认项目配置已启用”事件；启用动作本身决定状态为启用，组织编码取命令中的可信组织编码，
  /// 标准项目编码取已校验归属的配置记录。
  /// </summary>
  /// <param name="standardProjectCode">已按组织范围读取到的配置记录中的标准项目编码。</param>
  /// <returns>待登记的启用事件。</returns>
  public MutualRecognitionItemEnabledEvent CreateMutualRecognitionItemEnabledEvent(string standardProjectCode)
  {
    return this.MapToMutualRecognitionItemEnabledEvent(standardProjectCode: standardProjectCode, isValid: true, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
