namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 检验匹配的主要结果明细；逐条按来源原文返回，平台不解析、换算或重新判定。
/// </summary>
/// <remarks>异常标志与危急值标志只表达来源医院判断；不要求按来源报告中的业务序号返回。</remarks>
public sealed record RecognitionMatchLaboratoryResultReadModel
{
  /// <summary>检验项目名称。</summary>
  public string ResultItemName { get; init; } = string.Empty;
  /// <summary>来源结果原文。</summary>
  public string SourceResultContent { get; init; } = string.Empty;
  /// <summary>单位；来源未提供时为空。</summary>
  public string? Unit { get; init; }
  /// <summary>来源参考范围；来源未提供时为空。</summary>
  public string? SourceReferenceRange { get; init; }
  /// <summary>来源异常标志；来源未提供时为空。</summary>
  public string? SourceAbnormalFlag { get; init; }
  /// <summary>来源危急值标志；来源未提供时为空。</summary>
  public string? SourceCriticalValueFlag { get; init; }
}
