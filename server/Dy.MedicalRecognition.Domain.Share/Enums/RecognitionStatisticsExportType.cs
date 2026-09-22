using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认使用统计导出的粒度与视角取值；导出统一为 Excel 格式，不提供独立于七类的互认匹配记录导出。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值；
/// 成员中文说明同时是导出文件名的「导出类型名称」部分。
/// 来源侧被互认明细只反映被采纳事实；导出请求按导出类型校验汇总维度子集（来源侧类型不接受互认科室维度）。
/// </remarks>
[EnumDescriptor]
public enum RecognitionStatisticsExportType
{
  /// <summary>
  /// 接收侧使用汇总，数值 1；不逐匹配项展开。
  /// </summary>
  [Description("接收侧互认使用汇总")]
  RecognitionUsageSummary = 1,
  /// <summary>
  /// 接收侧提醒明细，数值 2；含尚未反馈处理结果项；按互认匹配项一行展开，并携带匹配记录与匹配项标识。
  /// </summary>
  [Description("接收侧提醒明细")]
  RecognitionReminderDetails = 2,
  /// <summary>
  /// 接收侧采纳明细，数值 3；随行导出预计节省金额；按互认匹配项一行展开，并携带匹配记录与匹配项标识。
  /// </summary>
  [Description("接收侧采纳明细")]
  RecognitionAdoptionDetails = 3,
  /// <summary>
  /// 接收侧不采纳明细，数值 4；携带原因代码与说明；按互认匹配项一行展开，并携带匹配记录与匹配项标识。
  /// </summary>
  [Description("接收侧不采纳明细")]
  RecognitionNonAdoptionDetails = 4,
  /// <summary>
  /// 接收侧引用明细，数值 5；反映已采纳项目的引用；按互认匹配项一行展开，并携带匹配记录与匹配项标识。
  /// </summary>
  [Description("接收侧引用明细")]
  RecognitionReferenceDetails = 5,
  /// <summary>
  /// 来源侧被互认汇总，数值 6；本组织作为来源方。
  /// </summary>
  [Description("来源医院被互认汇总")]
  SourceRecognitionSummary = 6,
  /// <summary>
  /// 来源侧被互认明细，数值 7；只反映被采纳事实。
  /// </summary>
  [Description("来源医院被互认明细")]
  SourceRecognitionDetails = 7,
}
