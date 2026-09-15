using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 组织医院院区互认项目金额的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record OrganizationHospitalBranchRecognitionAmountDto : Dto
{
  /// <summary>
  /// 金额记录标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 金额归属的组织编码；该组织下须已存在对应标准项目的互认配置。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 金额归属的医院编码，为组织下的医院层级编码。
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 金额归属的院区编码；金额精确到院区，不在同一医院的不同院区之间共用。
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 标准项目编码；与组织、医院、院区共同构成配置唯一键，标准目录停用不阻止金额维护。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 当前金额，单位元；不得小于零且最多两位小数，零元是有效配置；为接收侧统计口径，不代表实际收费、医保结算或医院收入。
  /// </summary>
  public partial decimal CurrentAmount { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
