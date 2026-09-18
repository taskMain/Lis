using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告列表查询的公共筛选条件；除分页外全部可选，缺省表示不施加该条件。
/// </summary>
/// <remarks>
/// 患者证件号码与患者姓名按规范形式做前缀匹配，该匹配只作为明确输入的查询条件，不用于患者身份匹配。
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
  /// <summary>报告单号筛选条件；不传表示不按单号过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：报告单号不能是空白。")]
  public string? ReportNo { get; init; }
  /// <summary>患者证件号码前缀匹配条件；输入先按规范形式处理（去首尾空白并统一大写），处理为空时不施加该条件。</summary>
  public string? IdentityDocumentNo { get; init; }
  /// <summary>患者姓名前缀匹配条件；输入先去首尾空白，处理为空时不施加该条件。</summary>
  public string? PatientName { get; init; }
}
