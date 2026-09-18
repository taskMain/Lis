using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告版本列表查询条件：按报告标识定位报告。
/// </summary>
public sealed record MedicalReportVersionListQueryRequest
{
  /// <summary>报告标识；必填，为空 Guid 即拒绝。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：报告标识不能为空Guid。")]
  public Guid ReportId { get; init; }
}
