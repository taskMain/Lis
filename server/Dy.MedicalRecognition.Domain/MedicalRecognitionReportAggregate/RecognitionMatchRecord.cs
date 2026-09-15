using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 一次互认匹配的记录，是互认匹配项与处理结果的汇总入口；一次成功的非空查询形成一条记录，无报告命中时不创建。
/// </summary>
[IPropertyChangedAware]
public partial class RecognitionMatchRecord : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 互认匹配记录主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 接收组织编码；记录下全部匹配项共用同一接收组织。
  /// </summary>
  public partial string ReceiverOrganizationCode { get; set; }
  /// <summary>
  /// 接收医院编码
  /// </summary>
  public partial string ReceiverHospitalCode { get; set; }
  /// <summary>
  /// 接收院区编码
  /// </summary>
  public partial string ReceiverBranchCode { get; set; }
  /// <summary>
  /// 证件类型代码
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 证件号码
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 就诊类型
  /// </summary>
  public partial VisitType VisitType { get; set; }
  /// <summary>
  /// 就诊流水号
  /// </summary>
  public partial string VisitSerialNo { get; set; }
  /// <summary>
  /// 互认匹配生成时间
  /// </summary>
  public partial DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 处理结果保存时间；在医生完成处理前为空，为空表示本次匹配尚待处理。
  /// </summary>
  public partial DateTime? DecisionSavedTime { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
