using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 互认匹配记录集合视图的单个匹配项投影：项目资料、来源归属、绑定报告与该项自身的反馈事实。
/// </summary>
/// <remarks>
/// 反馈状态与决策取各匹配项自身的处理结果，未反馈项的决策与原因为空值，由应用层显示「未反馈」；
/// 项目资料实时关联标准目录三表，编码不在目录内时名称为空值。
/// </remarks>
public sealed record RecognitionMatchRecordViewItem
{
  /// <summary>互认匹配项标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>项目类型：取匹配项自身保存值。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>标准目录分类名称：实时取自标准目录。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称。</summary>
  public string? GroupName { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称：实时取自标准目录。</summary>
  public string? StandardItemName { get; init; }
  /// <summary>来源组织编码：取自匹配项绑定报告的主体。</summary>
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源医院编码。</summary>
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码。</summary>
  public string? SourceBranchCode { get; init; }
  /// <summary>匹配项绑定报告的标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>匹配项绑定报告版本的标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>该项是否已保存处理结果；未反馈项不推断为不采纳。</summary>
  public bool IsProcessed { get; init; }
  /// <summary>该项的处理结果决策；首次保存后不可更改，未反馈时为空值。</summary>
  public RecognitionResult? Decision { get; init; }
  /// <summary>不采纳原因代码；仅不采纳决策携带。</summary>
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
}
