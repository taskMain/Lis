using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 修改标准医疗项目分组。
/// </summary>
/// <remarks>保存分组的名称与备注；本命令不含所属分类，由领域层回填原分类标识，分组归属不可迁移；分组名称在同一分类内唯一，重名校验排除本分组自身。</remarks>
[ObjectMap(typeof(MedicalStandardGroup), ObjectMapMode.OneWay)]
[IPropertyChangedAware]
public partial record UpdateMedicalStandardGroupCommand : ICommand
{
  /// <summary>
  /// 待修改分组标识，取自请求体。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 期望保存的分组名称。
  /// </summary>
  /// <remarks>取自请求体；同一分类内唯一，重名校验排除本分组自身。</remarks>
  public partial string Name { get; set; }
  /// <summary>
  /// 期望保存的分组备注，可空；本次值整体覆盖原备注。
  /// </summary>
  public partial string? Remark { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；命令时间只用于领域事件，实体列另取数据库当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 为本次修改构造“分组已更新”领域事件。
  /// </summary>
  /// <remarks>事件创建人与创建时间取自命令携带的操作人与操作时间。</remarks>
  /// <param name="categoryId">该分组原有分类的标识。</param>
  /// <returns>分组更新领域事件。</returns>
  public MedicalStandardGroupUpdatedEvent CreateMedicalStandardGroupUpdatedEvent(Guid categoryId)
  {
    return new MedicalStandardGroupUpdatedEvent
    {
      Id = Id,
      CategoryId = categoryId,
      Name = Name,
      Remark = Remark,
      EventCreator = OperId,
      EventCreatedTime = OperTime
    };
  }
}
