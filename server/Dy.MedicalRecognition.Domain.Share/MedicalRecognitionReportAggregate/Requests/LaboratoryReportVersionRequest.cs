using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 完整检验报告的提交输入：承载 multipart 文本部件反序列化后的完整 JSON 文档。
/// </summary>
/// <remarks>
/// 字段填报要求：必填为缺失即整份报告失败；成对为标识与名称同时有值或同时为空；
/// 应填为来源系统存在时必须提供、不存在时允许为空；条件应填为仅在对应条件下适用。
/// 必填时间字段省略或传零值都视为缺失；<see cref="StandardProjectCode"/> 即互认项目编码，
/// 缺失或当前不可用都不影响接收。必填与成对、唯一与时间顺序校验由领域管理器判定。
/// </remarks>
public sealed record LaboratoryReportVersionRequest
{
  /// <summary>来源报告单号；与报告类型、组织、医院、院区共同定位一份报告。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>公共版本信息。</summary>
  public MedicalReportVersionRequest Version { get; init; } = new();
  /// <summary>检验专项内容。</summary>
  public LaboratoryReportContentRequest Content { get; init; } = new();
  /// <summary>普通检验结果集合；至少一条。</summary>
  public IReadOnlyList<LaboratoryResultItemRequest> Results { get; init; } = [];
  /// <summary>细菌鉴定结果集合；每条内含药敏结果集合，可空。</summary>
  public IReadOnlyList<LaboratoryBacteriaResultRequest> BacteriaResults { get; init; } = [];
}
