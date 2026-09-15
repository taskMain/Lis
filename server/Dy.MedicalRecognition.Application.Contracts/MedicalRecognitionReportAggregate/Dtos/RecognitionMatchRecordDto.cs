using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 互认匹配记录的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record RecognitionMatchRecordDto : Dto
{
  /// <summary>
  /// 匹配记录标识；医院侧以此提交该组全部匹配项的处理结果。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 接收组织编码，来自服务端可信身份上下文，不由请求提交；记录下全部匹配项共用该组织。
  /// </summary>
  public partial string ReceiverOrganizationCode { get; set; }
  /// <summary>
  /// 接收医院编码，来自服务端可信身份上下文；决定采纳时读取哪一组织的互认金额。
  /// </summary>
  public partial string ReceiverHospitalCode { get; set; }
  /// <summary>
  /// 接收院区编码，来自服务端可信身份上下文；金额与统计归属到该院区层级。
  /// </summary>
  public partial string ReceiverBranchCode { get; set; }
  /// <summary>
  /// 查询时提交的患者证件类型代码，与证件号码组合定位患者。
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 查询时提交的患者证件号码，按证件类型与号码精确解析患者。
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 本次来源就诊类型；1=门诊、2=急诊、3=住院、4=体检、5=其他。
  /// </summary>
  public partial VisitType VisitType { get; set; }
  /// <summary>
  /// 本次来源院内就诊流水号，与就诊类型共同构成后续操作的严格匹配条件。
  /// </summary>
  public partial string VisitSerialNo { get; set; }
  /// <summary>
  /// 平台生成该匹配记录的时间；提醒统计的业务时间，无报告命中时不创建记录。
  /// </summary>
  public partial DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 处理结果首次成功保存的时间；为空表示尚未反馈。
  /// </summary>
  public partial DateTime? DecisionSavedTime { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
