using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 药敏结果的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record LaboratoryAntimicrobialSusceptibilityDto : Dto
{
  /// <summary>
  /// 药敏结果标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属细菌鉴定结果标识；药敏结果不以试验板序号代替该关联。
  /// </summary>
  public partial Guid BacteriaResultId { get; set; }
  /// <summary>
  /// 来源报文内的明细标识，用于来源明细去重与追溯。
  /// </summary>
  public partial string? SourceDetailKey { get; set; }
  /// <summary>
  /// 受试药物编码，平台不映射药物字典。
  /// </summary>
  public partial string? DrugCode { get; set; }
  /// <summary>
  /// 受试药物名称，按来源原文保存。
  /// </summary>
  public partial string DrugName { get; set; }
  /// <summary>
  /// 药敏结论编码，缺省时以来源结论文本表达判读结果。
  /// </summary>
  public partial string? SusceptibilityCode { get; set; }
  /// <summary>
  /// 来源系统的最终药敏结论原文，平台不重新判读敏感或耐药。
  /// </summary>
  public partial string SourceConclusionText { get; set; }
  /// <summary>
  /// 抗药结果编码，仅用于追溯。
  /// </summary>
  public partial string? ResistanceResultCode { get; set; }
  /// <summary>
  /// 纸片含药量，按来源文本、单位和比较符号原样保存，平台不拆分、不换算。
  /// </summary>
  public partial string? DiskContent { get; set; }
  /// <summary>
  /// 最低抑菌浓度值，按来源文本、单位和比较符号原样保存，平台不拆分、不换算。
  /// </summary>
  public partial string? MicValue { get; set; }
  /// <summary>
  /// 抑菌环直径，按来源文本、单位和比较符号原样保存，平台不拆分、不换算。
  /// </summary>
  public partial string? InhibitionZoneDiameter { get; set; }
  /// <summary>
  /// 药敏判读参考值，按来源原文与比较符号保存，平台不据此重新判定。
  /// </summary>
  public partial string? ReferenceValue { get; set; }
  /// <summary>
  /// 展示顺序；同一细菌鉴定结果下不重复，平台不依赖数组或数据库顺序。
  /// </summary>
  public partial int DisplayOrder { get; set; }
  /// <summary>
  /// 明细检测人在来源系统中的人员标识，与名称成对提供。
  /// </summary>
  public partial string? InspectorId { get; set; }
  /// <summary>
  /// 明细检测人名称，平台不依据姓名反查人员。
  /// </summary>
  public partial string? InspectorName { get; set; }
  /// <summary>
  /// 检测方法，按来源原文保存。
  /// </summary>
  public partial string? TestingMethod { get; set; }
  /// <summary>
  /// 试验板序号，仅用于来源版式追溯。
  /// </summary>
  public partial string? TestPanelOrder { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
