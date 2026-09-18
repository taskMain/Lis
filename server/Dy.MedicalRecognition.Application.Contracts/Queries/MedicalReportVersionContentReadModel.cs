namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 完整结构化报告内容：报告公共信息与检验、检查两段内容。
/// </summary>
/// <remarks>检验报告内容与检查报告内容按报告类型二选一返回，另一支为空引用。</remarks>
public sealed record MedicalReportVersionContentReadModel
{
  /// <summary>报告公共信息。</summary>
  public ReportVersionCommonReadModel Common { get; init; } = new();
  /// <summary>检验报告内容；检查报告时为 <see langword="null"/>。</summary>
  public LaboratoryReportContentViewReadModel? LaboratoryContent { get; init; }
  /// <summary>检查报告内容；检验报告时为 <see langword="null"/>。</summary>
  public ExaminationReportContentViewReadModel? ExaminationContent { get; init; }
}
