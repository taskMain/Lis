namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告版本下载所需的查询投影：报告身份与归属、版本标识、文件键与下载名。
/// </summary>
/// <remarks>文件键只用于仓储到宿主内部定位文件，不进入对外契约。</remarks>
public sealed record MedicalReportVersionFileItem
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>组织编码；来源归属的一部分。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>医院编码；来源归属的一部分。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>院区编码；来源归属的一部分。</summary>
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>PDF 文件键；只用于按键读取文件，不对外返回。</summary>
  public string PdfFileId { get; init; } = string.Empty;
  /// <summary>PDF 文件名；该版本保存的下载名。</summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>报告生命周期状态；供下载动作确认已作废报告的历史版本仍可下载。</summary>
  public MedicalReportLifecycleStatus ReportStatus { get; init; }
}
