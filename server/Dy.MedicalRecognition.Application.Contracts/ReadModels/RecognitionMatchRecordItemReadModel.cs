using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 互认匹配记录集合视图中的单个匹配项：项目资料、来源归属、绑定报告与该项自身的反馈事实。
/// </summary>
public sealed record RecognitionMatchRecordItemReadModel
{
  /// <summary>互认匹配项标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>互认项目：项目类型与标准目录资料。</summary>
  public RecognitionStatisticsItemReadModel Item { get; init; } = new();
  /// <summary>来源归属：来源组织、医院与院区编码及回填名称。</summary>
  public SourceOrganizationReadModel Source { get; init; } = new();
  /// <summary>匹配项绑定报告的标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>匹配项绑定报告版本的标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>该项是否已反馈处理结果；未反馈项不推断为不采纳。</summary>
  public bool IsProcessed { get; init; }
  /// <summary>该项的处理结果决策；首次保存后不可更改，未反馈时无值。</summary>
  public RecognitionResult? Decision { get; init; }
  /// <summary>
  /// 处理结果决策中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份决策文案。
  /// </summary>
  /// <remarks>未反馈项没有决策取值，返回 null 由页面显示「未反馈」。</remarks>
  public string? DecisionText => Decision is null ? null : EnumDescriptorText.GetOrNull(Decision.Value, RecognitionResultDescriptorList.List);
  /// <summary>不采纳原因代码；仅不采纳决策携带。</summary>
  public string? NonAdoptionReasonCode { get; init; }
  /// <summary>不采纳原因名称；取自平台统一原因定义。</summary>
  public string? NonAdoptionReasonName { get; init; }
}
