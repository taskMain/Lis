using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告列表查询：平台管理员入口；组织、医院与院区取自请求，三类条件都可用。
/// </summary>
public sealed record ReportListQueryRequest : MedicalReportListQueryRequest
{
  /// <summary>组织编码筛选条件；不传表示不按组织过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public string? OrganizationCode { get; init; }
  /// <summary>医院编码筛选条件；不传表示不按医院过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：医院编码不能是空白。")]
  public string? HospitalCode { get; init; }
  /// <summary>院区编码筛选条件；不传表示不按院区过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string? BranchCode { get; init; }
}
