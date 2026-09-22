using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 来源医院被互认明细查询：医院管理员入口，来源组织与来源医院固定为可信上下文。
/// </summary>
/// <remarks>
/// 请求不提交来源组织与来源医院字段；本侧来源院区可选，为空时按可信医院全部来源院区查询；
/// 接收组织恒为可信组织，请求只提交接收组医院与院区的可选筛选。来源侧明细只反映被采纳事实。
/// </remarks>
public sealed record BranchSourceRecognitionDetailsQueryRequest
{
  /// <summary>统计开始日期；必填，按本地日与结束日期构成闭区间。</summary>
  public DateOnly StartTime { get; init; }
  /// <summary>统计结束日期；必填，按本地日与开始日期构成闭区间。</summary>
  public DateOnly EndTime { get; init; }
  /// <summary>本侧来源院区编码；可选，为空按可信医院全部来源院区查询，非空必须存在、启用且属于可信医院。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源院区编码不能是空白。")]
  public string? SourceBranchCode { get; init; }
  /// <summary>接收医院编码筛选条件；不传表示不按接收医院过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：接收医院编码不能是空白。")]
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收院区编码筛选条件；不传表示不按接收院区过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：接收院区编码不能是空白。")]
  public string? ReceiverBranchCode { get; init; }
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
  /// <summary>分页对象：页码与页容量。</summary>
  public PageRequestDto Page { get; init; } = new();
}
