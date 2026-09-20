namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 项目级引用内容：供医院 HIS 写入本次病历的互认项目内容。
/// </summary>
/// <remarks>
/// 检验项目返回检验结果明细，检查项目返回命中项目的检查部位；
/// 检查所见与检查结论属于整份报告的公共内容，随报告公共上下文返回，不在项目级重复。
/// </remarks>
public sealed record RecognitionCitationItemReadModel
{
  /// <summary>互认匹配项标识；医院 HIS 原样保存并在随后提交引用结果时使用。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string StandardProjectName { get; init; } = string.Empty;
  /// <summary>检验结果明细；检查项目为空集合。</summary>
  public IReadOnlyList<RecognitionCitationLaboratoryResultReadModel> LaboratoryResults { get; init; } = [];
  /// <summary>检查部位集合；检验项目为空集合。</summary>
  public IReadOnlyList<RecognitionExaminationSiteReadModel> ExaminationSites { get; init; } = [];
}
