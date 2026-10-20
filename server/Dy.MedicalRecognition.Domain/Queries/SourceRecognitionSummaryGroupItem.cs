using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 提供方医院被互认汇总的分组行投影：所选维度的一个分组值与被互认次数。
/// </summary>
/// <remarks>
/// 被互认次数只计采纳事实、不等待引用、不含金额；
/// 未参与分组的维度键为空值，应用层据此组装读模型并把空值保持为空。
/// </remarks>
public sealed record SourceRecognitionSummaryGroupItem
{
  /// <summary>提供方组织编码：取自报告主体；仅提供方医院与提供方院区维度行有值。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>提供方医院编码；仅提供方医院维度行有值。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>提供方院区编码；仅提供方院区维度行有值。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>项目类型：实时取自标准目录所属分类；仅标准项目维度行有值。</summary>
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准项目编码；仅标准项目维度行有值。</summary>
  public string? StandardProjectCode { get; init; }
  /// <summary>标准项目名称：实时取自标准目录；仅标准项目维度行有值。</summary>
  public string? StandardItemName { get; init; }
  /// <summary>标准目录分类名称；仅标准项目维度行有值。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；仅标准项目维度行有值。</summary>
  public string? GroupName { get; init; }
  /// <summary>被互认次数：本行范围内采纳事实数。</summary>
  public long RecognitionCount { get; init; }
}
