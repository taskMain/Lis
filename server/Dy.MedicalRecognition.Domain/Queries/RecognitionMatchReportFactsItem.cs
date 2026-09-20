namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 互认匹配响应的报告事实查询投影行：本次匹配项绑定的报告版本对应的报告公共信息、业务时间与文件入口。
/// </summary>
/// <remarks>
/// 行集由本次匹配项绑定的版本集合决定，每个版本恰好一行；
/// 标本类型名称与整体异常标识只在检验侧有值、来源影像状态与影像调阅地址只在检查侧有值，
/// 为空值表示该报告类型没有这一项事实，调用方按报告类型决定使用哪一侧。
/// 投影只用于组装匹配响应，不作为对外契约。
/// </remarks>
public sealed record RecognitionMatchReportFactsItem
{
  /// <summary>
  /// 报告标识。
  /// </summary>
  public Guid ReportId { get; init; }
  /// <summary>
  /// 报告版本标识；本次匹配项绑定的版本。
  /// </summary>
  public Guid ReportVersionId { get; init; }
  /// <summary>
  /// 报告类型；决定匹配响应按检验还是检查组织项目级内容。
  /// </summary>
  public MedicalReportType ReportType { get; init; }
  /// <summary>
  /// 来源报告单号。
  /// </summary>
  public string ReportNo { get; init; } = string.Empty;
  /// <summary>
  /// 来源报告名称；取自报告版本的来源原值。
  /// </summary>
  public string ReportName { get; init; } = string.Empty;
  /// <summary>
  /// 来源组织编码；供应用层批量回填来源医疗机构名称。
  /// </summary>
  public string SourceOrganizationCode { get; init; } = string.Empty;
  /// <summary>
  /// 来源医院编码。
  /// </summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>
  /// 来源院区编码。
  /// </summary>
  public string SourceBranchCode { get; init; } = string.Empty;
  /// <summary>
  /// 检查或检验时间；检验取检测完成时间、检查取实际检查时间。
  /// </summary>
  public DateTime ClinicalTime { get; init; }
  /// <summary>
  /// 报告时间；取自报告主体上冗余保存的报告时间列。
  /// </summary>
  public DateTime ReportTime { get; init; }
  /// <summary>
  /// PDF 文件标识；平台保存的文件键。
  /// </summary>
  public string PdfFileId { get; init; } = string.Empty;
  /// <summary>
  /// PDF 文件名；该版本保存的原始文件名。
  /// </summary>
  public string PdfFileName { get; init; } = string.Empty;
  /// <summary>
  /// 标本类型名称；检查报告没有业务值。
  /// </summary>
  public string? SpecimenTypeName { get; init; }
  /// <summary>
  /// 来源报告的整体异常标识；只表达来源医院的判断，应用层不重新计算。
  /// </summary>
  public string? OverallAbnormalFlag { get; init; }
  /// <summary>
  /// 来源影像状态；检验报告没有业务值。
  /// </summary>
  public SourceImageStatus? SourceImageStatus { get; init; }
  /// <summary>
  /// 影像调阅地址；检验报告或来源未提供时为空。
  /// </summary>
  public string? ImageAccessUrl { get; init; }
}
