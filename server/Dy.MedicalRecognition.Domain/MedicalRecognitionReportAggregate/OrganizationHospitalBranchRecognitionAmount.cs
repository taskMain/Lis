using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 按组织、医院、院区与标准项目维护的互认项目当前金额。
/// </summary>
[IPropertyChangedAware]
public partial class OrganizationHospitalBranchRecognitionAmount : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 互认金额记录主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 组织编码
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 标准项目编码；与组织、医院、院区共同构成业务键，同一组合至多一条当前金额记录。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 当前金额；保存时由请求提交值覆盖，零元是有效配置。
  /// </summary>
  public partial decimal CurrentAmount { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；取命令携带的操作时间，不使用数据库当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
