using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 按组织、医院、院区与标准项目累计的互认金额。
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
  /// 标准项目编码；与组织、医院、院区共同构成归集组合键，同一组合的金额在已有值上累加。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 当前金额；由互认采纳结果累计产生，不允许由调用方直接提交金额替换既有累计值。
  /// </summary>
  public partial decimal CurrentAmount { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
