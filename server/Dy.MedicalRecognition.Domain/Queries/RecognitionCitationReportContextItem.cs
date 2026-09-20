namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 引用详情的报告公共上下文查询投影行：供医院 HIS 写入病历时注明结果出处所需的报告信息。
/// </summary>
/// <remarks>
/// 行集由本次要返回的报告版本标识集合决定，每个版本恰好一行；
/// 来源组织、医院与院区只保存编码，名称由应用层按来源三值经组织路径解析点批量回填，本投影不携带名称；
/// 检查或检验时间按报告类型合并取值：检验取检测完成时间、检查取实际检查时间；
/// 检查所见与检查结论只在检查侧有值，检验报告为空值；
/// 来源影像状态与影像调阅地址只在检查侧有值，检验报告为空值。
/// 投影只用于组装引用详情，不作为对外契约。
/// </remarks>
public sealed record RecognitionCitationReportContextItem
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识；本次要返回的版本。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>报告类型；决定项目级内容取检验结果还是检查部位，也决定检查所见与影像字段是否有值。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>来源报告名称；取自报告版本的来源原值。</summary>
  public string ReportName { get; init; } = string.Empty;
  /// <summary>来源组织编码；供应用层批量回填来源医疗机构名称。</summary>
  public string SourceOrganizationCode { get; init; } = string.Empty;
  /// <summary>来源医院编码；供应用层批量回填来源医疗机构名称。</summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>来源院区编码；供应用层批量回填来源医疗机构名称。</summary>
  public string SourceBranchCode { get; init; } = string.Empty;
  /// <summary>检查或检验时间；检验取检测完成时间、检查取实际检查时间。</summary>
  public DateTime ClinicalTime { get; init; }
  /// <summary>报告时间；取自报告主体上冗余保存的报告时间列。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>来源申请医生标识；来源未提供时为空。</summary>
  public string? SourceApplicantDoctorId { get; init; }
  /// <summary>来源申请医生名称；来源未提供时为空。</summary>
  public string? SourceApplicantDoctorName { get; init; }
  /// <summary>来源审核医生标识；来源未提供时为空。</summary>
  public string? SourceReviewerDoctorId { get; init; }
  /// <summary>来源审核医生名称；来源未提供时为空。</summary>
  public string? SourceReviewerDoctorName { get; init; }
  /// <summary>检查所见，完整原文；检验报告或来源未提供时为空。</summary>
  public string? ExaminationFindings { get; init; }
  /// <summary>检查结论，完整原文；检验报告或来源未提供时为空。</summary>
  public string? ExaminationConclusion { get; init; }
  /// <summary>PDF 文件标识；平台保存的文件键。</summary>
  public string PdfFileId { get; init; } = string.Empty;
  /// <summary>PDF 文件名；该版本保存的原始文件名。</summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>来源影像状态；检验报告或来源未提供时为空。</summary>
  public SourceImageStatus? SourceImageStatus { get; init; }
  /// <summary>影像调阅地址；检验报告、来源未提供或无影像时为空。</summary>
  public string? ImageAccessUrl { get; init; }
}
