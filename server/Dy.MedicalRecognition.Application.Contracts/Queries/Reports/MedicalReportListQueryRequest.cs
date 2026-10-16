using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
namespace Dy.MedicalRecognition.Application.Contracts.Queries.Reports;

/// <summary>
/// 报告列表查询的公共筛选条件；除分页外全部可选，缺省表示不施加该条件。
/// </summary>
/// <remarks>
/// 统一关键字同时匹配报告单号（精确）、患者证件号码（前缀，大小写不敏感）与患者姓名（前缀），任一命中即返回；
/// 该匹配只作为明确输入的查询条件，不用于患者身份匹配。
/// 时间范围作用于报告主体上的报告时间，采用左闭右开区间。
/// 分页以业务筛选加 <c>page: { pageIndex, pageSize }</c> 提交，页码与页容量不出现在顶层。
/// 两个报告列表入口的取值来源不同，因此各自声明请求类型，本类型只作为两个入口的共同父类承载筛选字段与分页对象。
/// </remarks>
public record MedicalReportListQueryRequest
{
  /// <summary>分页对象：页码与页容量。</summary>
  public PageRequestDto Page { get; init; } = new();
  /// <summary>报告时间的起始日；含边界，不传表示不设下界。只提交年月日，不携带时刻与时区。</summary>
  public DateOnly? ReportDateFrom { get; init; }
  /// <summary>报告时间的结束日；不含上界（按结束日次日零点处理），不传表示不设上界。只提交年月日，不携带时刻与时区。</summary>
  public DateOnly? ReportDateTo { get; init; }
  /// <summary>报告类型筛选条件；不传表示不按类型过滤。</summary>
  [EnumDataType(typeof(MedicalReportType), ErrorMessage = "参数校验失败：报告类型无效。")]
  public MedicalReportType? ReportType { get; init; }
  /// <summary>统一关键字筛选条件；同时按报告单号精确、患者证件号码前缀（大小写不敏感）与患者姓名前缀匹配，任一命中即返回。输入先去首尾空白，处理为空时不施加该条件。</summary>
  public string? Keyword { get; init; }
  /// <summary>报告生命周期状态筛选条件；不传表示不按状态过滤。</summary>
  [EnumDataType(typeof(MedicalReportLifecycleStatus), ErrorMessage = "参数校验失败：报告状态无效。")]
  public MedicalReportLifecycleStatus? ReportStatus { get; init; }
}
