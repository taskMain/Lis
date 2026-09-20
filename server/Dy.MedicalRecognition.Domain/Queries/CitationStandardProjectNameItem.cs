namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 引用详情的标准项目名称查询投影行：标准项目编码与它在标准目录中的展示名称。
/// </summary>
/// <remarks>
/// 名称取自标准目录的标准项目行，与来源报告明细上的来源项目名称是两个来源；
/// 引用详情不因互认配置或标准目录当前停用而拒绝已采纳项目，因此该投影不携带启用状态，读取时也不按启用状态过滤。
/// 投影只用于组装引用详情，不作为对外契约。
/// </remarks>
public sealed record CitationStandardProjectNameItem
{
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目在标准目录中的名称。</summary>
  public string StandardProjectName { get; init; } = string.Empty;
}
