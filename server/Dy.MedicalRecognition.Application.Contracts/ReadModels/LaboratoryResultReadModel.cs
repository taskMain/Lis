namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record LaboratoryResultReadModel
{
  /// <summary>
  /// 来源明细标识
  /// </summary>
  public string SourceDetailKey { get; set; }
  /// <summary>
  /// 来源项目名称
  /// </summary>
  public string SourceProjectName { get; set; }
  /// <summary>
  /// 来源项目编码
  /// </summary>
  public string SourceProjectCode { get; set; }
  /// <summary>
  /// 互认项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 来源结果原文
  /// </summary>
  public string SourceResultText { get; set; }
  /// <summary>
  /// 结果类型
  /// </summary>
  public object ResultType { get; set; }
  /// <summary>
  /// LOINC编码
  /// </summary>
  public string LoincCode { get; set; }
  /// <summary>
  /// 单位
  /// </summary>
  public string Unit { get; set; }
  /// <summary>
  /// 参考范围
  /// </summary>
  public string ReferenceRange { get; set; }
  /// <summary>
  /// 检测方法
  /// </summary>
  public string TestingMethod { get; set; }
  /// <summary>
  /// 检测仪器编码
  /// </summary>
  public string InstrumentCode { get; set; }
  /// <summary>
  /// 检测仪器名称
  /// </summary>
  public string InstrumentName { get; set; }
  /// <summary>
  /// 展示序号
  /// </summary>
  public int DisplayOrder { get; set; }
  /// <summary>
  /// 异常标志
  /// </summary>
  public string AbnormalFlag { get; set; }
  /// <summary>
  /// 危急值标志
  /// </summary>
  public bool CriticalValueFlag { get; set; }
  /// <summary>
  /// 检验收费项目编码
  /// </summary>
  public string LaboratoryChargeItemCode { get; set; }
  /// <summary>
  /// 医保收费项目编码
  /// </summary>
  public string InsuranceChargeItemCode { get; set; }
  /// <summary>
  /// 检测人ID
  /// </summary>
  public string InspectorId { get; set; }
  /// <summary>
  /// 检测人名称
  /// </summary>
  public string InspectorName { get; set; }
}
