using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 保存组织医院院区互认项目金额的命令：把某组织、医院、院区下某标准项目的当前金额维护为本次提交值。
/// </summary>
/// <remarks>
/// 平台管理员与医院管理员两个应用入口共用本命令，入口差异只体现在组织与医院的取值来源，命令本身不表达入口差异。
/// 四个业务键与金额由应用层完成校验后写入，操作人取自可信身份、操作时间取服务端当前时间，都不是调用方直接提交值；
/// 金额是否非负且最多两位小数、互认项目配置是否存在与影响行数判定由领域层在写入前与写入后判断。
/// </remarks>
[ObjectMap(typeof(OrganizationHospitalBranchRecognitionAmount), ObjectMapMode.OneWay)]
[ObjectMap(typeof(OrganizationHospitalBranchRecognitionAmountSavedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record SaveOrganizationHospitalBranchRecognitionAmountCommand : ICommand
{
  /// <summary>
  /// 组织编码；平台管理员入口取请求，医院管理员入口取可信上下文，决定金额归属。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码；来源同组织编码，必须属于该组织。
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码；两个入口都取请求，必须属于该医院与该组织。
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 标准项目编码；与组织、医院、院区共同构成业务键，该组织须已建立此编码的互认项目配置。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 本次提交的当前金额；只允许非负且最多两位小数，覆盖既有金额而不是在其上累加。
  /// </summary>
  public partial decimal CurrentAmount { get; set; }
  /// <summary>
  /// 操作人；取自可信身份上下文。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；取自服务端当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }

  /// <summary>
  /// 创建“组织医院院区互认项目金额已保存”事件；事件沿用命令的四个业务键、当前金额与操作字段。
  /// </summary>
  /// <param name="id">本次保存所影响金额记录的标识：新增时为服务端生成的主键，更新时为既有记录的主键。</param>
  /// <returns>待登记的保存事件。</returns>
  public OrganizationHospitalBranchRecognitionAmountSavedEvent CreateOrganizationHospitalBranchRecognitionAmountSavedEvent(Guid id)
  {
    return this.MapToOrganizationHospitalBranchRecognitionAmountSavedEvent(id: id, eventCreator: OperId, eventCreatedTime: OperTime);
  }
}
