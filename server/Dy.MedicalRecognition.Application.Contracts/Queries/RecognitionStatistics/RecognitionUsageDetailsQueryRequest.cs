using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 使用方互认使用明细查询：平台管理员入口，接收组与来源组两组范围条件都取自请求。
/// </summary>
/// <remarks>
/// 范围条件均可选，请求级空取值不附加该层过滤；提供的取值由服务端经组织路径解析校验；
/// 两组组织编码同时提供且不一致时按业务拒绝。明细类型必填，未提供时取默认值 0，按未定义枚举值拒绝。
/// </remarks>
public sealed record RecognitionUsageDetailsQueryRequest
{
  /// <summary>统计开始日期；必填，按本地日与结束日期构成闭区间。</summary>
  public DateOnly StartTime { get; init; }
  /// <summary>统计结束日期；必填，按本地日与开始日期构成闭区间。</summary>
  public DateOnly EndTime { get; init; }
  /// <summary>使用方组织编码筛选条件；不传表示不按组织过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public string? OrganizationCode { get; init; }
  /// <summary>使用方医院编码筛选条件；不传表示不按医院过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：医院编码不能是空白。")]
  public string? HospitalCode { get; init; }
  /// <summary>使用方院区编码筛选条件；不传表示不按院区过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string? BranchCode { get; init; }
  /// <summary>提供方组织编码筛选条件；不传表示不按提供方组织过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：提供方组织编码不能是空白。")]
  public string? SourceOrganizationCode { get; init; }
  /// <summary>提供方医院编码筛选条件；不传表示不按提供方医院过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：提供方医院编码不能是空白。")]
  public string? SourceHospitalCode { get; init; }
  /// <summary>提供方院区编码筛选条件；不传表示不按提供方院区过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：提供方院区编码不能是空白。")]
  public string? SourceBranchCode { get; init; }
  /// <summary>
  /// 统一关键字筛选条件；按互认科室ID/名称、标准项目编码/名称、互认医生ID/姓名任一前缀命中（服务端大小写不敏感），
  /// 任一命中即通过。空白（含全空白字符）视为未提供、不附加条件；其余输入去首尾空白后参与匹配。
  /// </summary>
  public string? Keyword { get; init; }
  /// <summary>项目类型筛选条件；不传表示不按项目类型过滤。</summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称筛选条件；不传表示不按分类过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类名称不能是空白。")]
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称筛选条件；不传表示不按分组过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分组名称不能是空白。")]
  public string? GroupName { get; init; }
  /// <summary>不采纳原因代码筛选条件；不传表示不按原因过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：不采纳原因代码不能是空白。")]
  public string? NonAdoptionReasonCode { get; init; }
  /// <summary>明细类型；必填，未提供时取默认值 0，按未定义枚举值拒绝。</summary>
  [EnumDataType(typeof(RecognitionUsageDetailType), ErrorMessage = "参数校验失败：明细类型无效。")]
  public RecognitionUsageDetailType DetailType { get; init; }
  /// <summary>分页对象：页码与页容量。</summary>
  public PageRequestDto Page { get; init; } = new();
}
