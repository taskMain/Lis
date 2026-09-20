namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 互认引用详情：本次就诊下仍可引用的已采纳项目，以及各项目所属报告的必要公共上下文。
/// </summary>
/// <remarks>
/// 项目集合只包含决定为采纳、归属一致且绑定报告版本仍为当前有效版本的项目；
/// 同次就诊存在多条绑定相同报告版本与项目的有效采纳时只返回保存时间最近的一条，较早事实仍保留并参与既有统计；
/// 报告公共上下文按报告归并，同一报告命中多个互认项目时只返回一次；
/// 全部相关项目均不可返回时按平台实际确认的原因拒绝，不以成功空集合掩盖原因。
/// </remarks>
public sealed record RecognitionCitationDetailReadModel
{
  /// <summary>本次就诊下仍可引用的已采纳项目集合。</summary>
  public IReadOnlyList<RecognitionCitationItemReadModel> MatchItems { get; init; } = [];
  /// <summary>项目所属报告的必要公共上下文集合。</summary>
  public IReadOnlyList<RecognitionReportContextReadModel> ReportContext { get; init; } = [];
}
