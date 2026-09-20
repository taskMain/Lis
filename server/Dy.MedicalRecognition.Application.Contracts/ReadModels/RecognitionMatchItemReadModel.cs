namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 项目级匹配内容：命中项目与本次命中该项目对应的报告内容。
/// </summary>
/// <remarks>
/// 检验项目返回标本类型名称与主要检验结果明细，检查项目返回命中项目的检查部位与整体异常标识；
/// 标本类型编码与院内标本号只用于来源追溯，不进入匹配响应；
/// 平台不保存也不返回固定的互认超期时间，有效期由每次匹配查询按当前配置动态判断。
/// </remarks>
public sealed record RecognitionMatchItemReadModel
{
  /// <summary>互认匹配项标识；是后续提交处理结果与引用结果的定位依据。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string StandardProjectName { get; init; } = string.Empty;
  /// <summary>标本类型名称；检查项目没有业务值。</summary>
  public string? SpecimenTypeName { get; init; }
  /// <summary>本次命中项目对应的主要检验结果明细；检查项目为空集合。</summary>
  public IReadOnlyList<RecognitionMatchLaboratoryResultReadModel> LaboratoryResults { get; init; } = [];
  /// <summary>命中项目的检查部位集合；检验项目为空集合。</summary>
  public IReadOnlyList<RecognitionExaminationSiteReadModel> ExaminationSites { get; init; } = [];
  /// <summary>来源报告的整体异常标识；只表达来源医院判断，平台不重新计算。</summary>
  public string? OverallAbnormalFlag { get; init; }
}
