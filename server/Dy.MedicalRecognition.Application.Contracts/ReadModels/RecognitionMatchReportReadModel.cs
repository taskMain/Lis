using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 匹配报告公共信息：报告识别信息、来源医疗机构、两个业务时间、文件入口与该报告命中的项目。
/// </summary>
/// <remarks>
/// 来源组织、医院、院区名称由服务端按来源三值经组织路径解析点批量回填，页面不按编码拼接名称；
/// 文件入口承载平台保存的 PDF 文件标识、下载入口与原始文件名，以及来源影像状态与调阅地址；
/// 同一份联合报告命中多个互认项目时，本对象只返回一次，命中的项目在同对象的匹配项目集合下逐项返回。
/// </remarks>
public sealed record RecognitionMatchReportReadModel
{
  /// <summary>报告标识。</summary>
  public Guid ReportId { get; init; }
  /// <summary>报告版本标识；命中的当前有效版本。</summary>
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
  /// <summary>PDF 与影像入口。</summary>
  public PdfAndImageAccessReadModel File { get; init; } = new();
  /// <summary>本报告命中本次查询的互认项目集合；未命中的项目不成项。</summary>
  public IReadOnlyList<RecognitionMatchItemReadModel> MatchItems { get; init; } = [];
}
