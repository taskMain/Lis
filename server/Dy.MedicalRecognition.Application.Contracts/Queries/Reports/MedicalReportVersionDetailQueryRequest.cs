using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告版本详情查询条件：报告标识用于归属定位，报告版本标识用于定位版本。
/// </summary>
public sealed record MedicalReportVersionDetailQueryRequest
{
  /// <summary>报告标识；必填，为空 Guid 即拒绝。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：报告标识不能为空Guid。")]
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识；必填，为空 Guid 即拒绝。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：报告版本标识不能为空Guid。")]
  public Guid ReportVersionId { get; init; }
}
