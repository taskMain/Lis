namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 互认匹配候选报告的查询投影行：一份报告版本在某一个标准项目上形成的一条候选。
/// </summary>
/// <remarks>
/// 同一份报告版本命中多个标准项目时产生多行，每行只承载该项目对应的取数结果；
/// 匹配基准时间按报告类型取自不同专项内容表，一行内只可能有一侧的取值；
/// 投影只用于领域层逐项目选优，不作为对外契约。
/// </remarks>
public sealed record RecognitionMatchCandidateReportItem
{
  /// <summary>
  /// 报告标识；同一报告的多个版本与多个项目共用该标识。
  /// </summary>
  public Guid ReportId { get; init; }
  /// <summary>
  /// 报告版本标识；取自报告的当前版本指向，同时是选优的稳定兜底排序键。
  /// </summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>
  /// 来源医院编码；与接收医院相同即本院报告，是否参与匹配由本院报告两个参数决定。
  /// </summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>
  /// 项目类型；检验取检验、检查取检查，与本次实际命中一侧的项目表同源。
  /// </summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 标准项目编码；本次实际命中一侧的项目表列，检验与检查各自取自己那侧的编码。
  /// </summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>
  /// 匹配基准时间；检验取检测完成时间、检查取实际检查时间，为空表示对应专项内容缺失。
  /// </summary>
  public DateTime? MatchBaselineTime { get; init; }
  /// <summary>
  /// 报告时间；取自报告主体上冗余保存的报告时间列，恒等于当前版本取值。
  /// </summary>
  public DateTime ReportTime { get; init; }
}
