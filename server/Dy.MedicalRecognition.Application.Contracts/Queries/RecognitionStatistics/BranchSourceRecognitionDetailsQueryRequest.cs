using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 提供方医院被互认明细查询：医院管理员入口，提供方组织与提供方医院固定为可信上下文。
/// </summary>
/// <remarks>
/// 请求不提交提供方组织与提供方医院字段；本侧提供方院区可选，为空时按可信医院全部提供方院区查询；
/// 使用方组织恒为可信组织，请求只提交接收组医院与院区的可选筛选。提供方明细只反映被采纳事实。
/// </remarks>
public sealed record BranchSourceRecognitionDetailsQueryRequest
{
  /// <summary>统计开始日期；必填，按本地日与结束日期构成闭区间。</summary>
  public DateOnly StartTime { get; init; }
  /// <summary>统计结束日期；必填，按本地日与开始日期构成闭区间。</summary>
  public DateOnly EndTime { get; init; }
  /// <summary>本侧提供方院区编码；可选，为空按可信医院全部提供方院区查询，非空必须存在、启用且属于可信医院。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：提供方院区编码不能是空白。")]
  public string? SourceBranchCode { get; init; }
  /// <summary>使用方医院编码筛选条件；不传表示不按使用方医院过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：使用方医院编码不能是空白。")]
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>使用方院区编码筛选条件；不传表示不按使用方院区过滤，取值限可信组织。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：使用方院区编码不能是空白。")]
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
  /// <summary>
  /// 统一关键字筛选条件；按互认科室ID/名称、标准项目编码/名称任一前缀命中（服务端大小写不敏感，提供方语句无互认医生列），
  /// 任一命中即通过。空白（含全空白字符）视为未提供、不附加条件；其余输入去首尾空白后参与匹配。
  /// </summary>
  public string? Keyword { get; init; }
  /// <summary>分页对象：页码与页容量。</summary>
  public PageRequestDto Page { get; init; } = new();
}
