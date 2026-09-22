using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 接收侧互认使用明细查询：医院管理员入口，接收组织与接收医院由可信上下文注入。
/// </summary>
/// <remarks>
/// 请求不提交本侧组织与医院字段；本侧接收院区可选，为空时按可信医院全院范围查询明细；
/// 来源组织恒为可信组织，请求只提交来源组医院与院区的可选筛选。明细类型必填。
/// </remarks>
public sealed record BranchRecognitionUsageDetailsQueryRequest
{
  /// <summary>统计开始日期；必填，按本地日与结束日期构成闭区间。</summary>
  public DateOnly StartTime { get; init; }
  /// <summary>统计结束日期；必填，按本地日与开始日期构成闭区间。</summary>
  public DateOnly EndTime { get; init; }
  /// <summary>本侧接收院区编码；可选，为空按可信医院全院范围查询，非空必须存在、启用且属于可信医院。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string? BranchCode { get; init; }
  /// <summary>来源医院编码筛选条件；不传表示不按来源医院过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源医院编码不能是空白。")]
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码筛选条件；不传表示不按来源院区过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源院区编码不能是空白。")]
  public string? SourceBranchCode { get; init; }
  /// <summary>互认科室ID筛选条件；不传表示不按互认科室过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认科室ID不能是空白。")]
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认医生ID筛选条件；只作明细查询条件，不形成汇总维度。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认医生ID不能是空白。")]
  public string? RecognitionDoctorId { get; init; }
  /// <summary>项目类型筛选条件；不传表示不按项目类型过滤。</summary>
  [EnumDataType(typeof(MedicalItemType), ErrorMessage = "参数校验失败：项目类型无效。")]
  public MedicalItemType? ItemType { get; init; }
  /// <summary>标准目录分类名称筛选条件；不传表示不按分类过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分类名称不能是空白。")]
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称筛选条件；不传表示不按分组过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：分组名称不能是空白。")]
  public string? GroupName { get; init; }
  /// <summary>标准项目编码筛选条件；不传表示不按标准项目过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public string? StandardProjectCode { get; init; }
  /// <summary>不采纳原因代码筛选条件；不传表示不按原因过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：不采纳原因代码不能是空白。")]
  public string? NonAdoptionReasonCode { get; init; }
  /// <summary>明细类型；必填，未提供时取默认值 0，按未定义枚举值拒绝。</summary>
  [EnumDataType(typeof(RecognitionUsageDetailType), ErrorMessage = "参数校验失败：明细类型无效。")]
  public RecognitionUsageDetailType DetailType { get; init; }
  /// <summary>分页对象：页码与页容量。</summary>
  public PageRequestDto Page { get; init; } = new();
}
