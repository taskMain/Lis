using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 报告公共上下文：医院 HIS 写入病历时用于注明结果出处的必要信息。
/// </summary>
/// <remarks>
/// 来源组织、医院、院区名称由服务端按来源三值经组织路径解析点批量回填，页面不按编码拼接名称；
/// 检查所见与检查结论属于整份报告的公共内容，随本对象返回，医生据此写入病历；
/// 文件入口承载平台保存的 PDF 文件标识、下载入口与原始文件名，以及来源影像状态与调阅地址。
/// </remarks>
public sealed record RecognitionReportContextReadModel
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识。</summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>报告类型。</summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>
  /// 报告类型中文；由服务端按枚举声明解析，页面直接展示。
  /// </summary>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ReportTypeText => EnumDescriptorText.Get(ReportType, MedicalReportTypeDescriptorList.List);
  /// <summary>来源报告单号。</summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>来源报告名称。</summary>
  public string ReportName { get; init; } = string.Empty;
  /// <summary>来源组织编码。</summary>
  public string SourceOrganizationCode { get; init; } = string.Empty;
  /// <summary>来源组织名称；由服务端回填，不按编码拼接。</summary>
  public string SourceOrganizationName { get; init; } = string.Empty;
  /// <summary>来源医院编码。</summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>来源医院名称；由服务端回填。</summary>
  public string SourceHospitalName { get; init; } = string.Empty;
  /// <summary>来源院区编码。</summary>
  public string SourceBranchCode { get; init; } = string.Empty;
  /// <summary>来源院区名称；由服务端回填。</summary>
  public string SourceBranchName { get; init; } = string.Empty;
  /// <summary>检查或检验时间；检验取检测完成时间，检查取实际检查时间。</summary>
  public DateTime ClinicalTime { get; init; }
  /// <summary>报告时间。</summary>
  public DateTime ReportTime { get; init; }
  /// <summary>来源申请医生标识；来源未提供时为空。</summary>
  public string? SourceApplicantDoctorId { get; init; }
  /// <summary>来源申请医生名称；来源未提供时为空。</summary>
  public string? SourceApplicantDoctorName { get; init; }
  /// <summary>来源审核医生标识；来源未提供时为空。</summary>
  public string? SourceReviewerDoctorId { get; init; }
  /// <summary>来源审核医生名称；来源未提供时为空。</summary>
  public string? SourceReviewerDoctorName { get; init; }
  /// <summary>检查所见，完整原文；检验报告没有业务值。</summary>
  public string? ExaminationFindings { get; init; }
  /// <summary>检查结论，完整原文；检验报告没有业务值。</summary>
  public string? ExaminationConclusion { get; init; }
  /// <summary>PDF 与影像入口。</summary>
  public PdfAndImageAccessReadModel File { get; init; } = new();
}
