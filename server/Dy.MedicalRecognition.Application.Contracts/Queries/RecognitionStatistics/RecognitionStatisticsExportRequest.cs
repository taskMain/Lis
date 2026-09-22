using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;

/// <summary>
/// 互认统计导出：平台管理员入口，来源组与接收组两组范围条件都取自请求，导出继承查询条件与汇总维度。
/// </summary>
/// <remarks>
/// 范围条件均可选，请求级空取值不附加该层过滤；提供的取值由服务端经组织路径解析校验；
/// 两组组织编码同时提供且不一致时按业务拒绝。导出不使用页面分页，取当前查询条件下全部记录并封顶十万行；
/// 导出类型必填，汇总维度必填并按导出类型校验来源侧维度子集（来源侧类型不接受互认科室维度）；
/// 不采纳原因代码仅作用于接收侧不采纳明细导出。
/// </remarks>
public sealed record RecognitionStatisticsExportRequest
{
  /// <summary>导出类型；必填，未提供时取默认值 0，按未定义枚举值拒绝。</summary>
  [EnumDataType(typeof(RecognitionStatisticsExportType), ErrorMessage = "参数校验失败：导出类型无效。")]
  public RecognitionStatisticsExportType ExportType { get; init; }
  /// <summary>统计开始日期；必填，按本地日与结束日期构成闭区间，并进入导出文件名。</summary>
  public DateOnly StartTime { get; init; }
  /// <summary>统计结束日期；必填，按本地日与开始日期构成闭区间，并进入导出文件名。</summary>
  public DateOnly EndTime { get; init; }
  /// <summary>接收组织编码筛选条件；不传表示不按接收组织过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：接收组织编码不能是空白。")]
  public string? ReceiverOrganizationCode { get; init; }
  /// <summary>接收医院编码筛选条件；不传表示不按接收医院过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：接收医院编码不能是空白。")]
  public string? ReceiverHospitalCode { get; init; }
  /// <summary>接收院区编码筛选条件；不传表示不按接收院区过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：接收院区编码不能是空白。")]
  public string? ReceiverBranchCode { get; init; }
  /// <summary>来源组织编码筛选条件；不传表示不按来源组织过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源组织编码不能是空白。")]
  public string? SourceOrganizationCode { get; init; }
  /// <summary>来源医院编码筛选条件；不传表示不按来源医院过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源医院编码不能是空白。")]
  public string? SourceHospitalCode { get; init; }
  /// <summary>来源院区编码筛选条件；不传表示不按来源院区过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：来源院区编码不能是空白。")]
  public string? SourceBranchCode { get; init; }
  /// <summary>互认科室ID筛选条件；不传表示不按互认科室过滤。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认科室ID不能是空白。")]
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认医生ID筛选条件；仅对接收侧明细导出生效。</summary>
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
  /// <summary>不采纳原因代码筛选条件；仅作用于接收侧不采纳明细导出。</summary>
  [NonEmpty(ErrorMessage = "参数校验失败：不采纳原因代码不能是空白。")]
  public string? NonAdoptionReasonCode { get; init; }
  /// <summary>
  /// 汇总维度；必填，未提供时取默认值 0，按未定义枚举值拒绝，并按导出类型校验来源侧维度子集。
  /// </summary>
  [EnumDataType(typeof(RecognitionStatisticsGroupDimension), ErrorMessage = "参数校验失败：汇总维度无效。")]
  public RecognitionStatisticsGroupDimension GroupDimension { get; init; }
}
