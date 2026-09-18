using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 完整检查报告的提交输入：承载 multipart 文本部件反序列化后的完整 JSON 文档。
/// </summary>
/// <remarks>字段填报要求与检验报告同口径；来源项目名称必填、互认项目编码可空。</remarks>
public sealed record ExaminationReportVersionRequest
{
  /// <summary>来源报告单号；与报告类型、组织、医院、院区共同定位一份报告。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>公共版本信息。</summary>
  public MedicalReportVersionRequest Version { get; init; } = new();
  /// <summary>检查专项内容。</summary>
  public ExaminationReportContentRequest Content { get; init; } = new();
  /// <summary>检查项目集合；至少一条，每条内含检查部位集合。</summary>
  public IReadOnlyList<ExaminationItemRequest> Items { get; init; } = [];
}
