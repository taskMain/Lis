using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 批量新建互认项目配置的命令：为可信组织一次性建立同一可互认时间的多条配置，全部以启用状态创建。
/// </summary>
/// <remarks>
/// 组织编码取自登录令牌的组织声明、操作人取自登录令牌的用户标识，操作时间取服务端当前时间，这些字段都由应用层注入而不来自调用方；
/// 命令只承载调用方提交的标准项目编码集合，标准项目标识与所属分类、所属分组由领域层按编码集合一次读取后取得，
/// 因此命令不携带内部标识，也不携带逐元素的可互认时间；
/// 编码集合的口径、三层目录有效性与组织内配置唯一性由领域层在写入前整批判断。
/// </remarks>
[ObjectMap(typeof(MutualRecognitionItemsCreatedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemsCommand : ICommand
{
  /// <summary>
  /// 组织编码；取自登录令牌的组织声明，决定全部新建配置的归属。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 本次建立配置的标准项目编码集合；元素是调用方提交的业务编码，标准项目标识由领域层按编码集合读取目录后取得。
  /// </summary>
  public partial IReadOnlyList<string> StandardProjectCodes { get; set; } = [];
  /// <summary>
  /// 可互认时间天数；本次提交的全部配置共用该取值，只允许正整数。
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
  /// 创建“互认项目配置已批量创建”事件；配置全部以启用状态创建，因此每条快照的启用状态固定为启用。
  /// </summary>
  /// <param name="items">本次建立的配置快照，按请求提交的标准项目编码顺序排列。</param>
  /// <returns>待登记的批量创建事件。</returns>
  public MutualRecognitionItemsCreatedEvent CreateMutualRecognitionItemsCreatedEvent(IReadOnlyList<MutualRecognitionItemCreatedSnapshot> items)
  {
    return this.MapToMutualRecognitionItemsCreatedEvent(items: items, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
