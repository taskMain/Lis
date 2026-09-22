using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 接收侧互认使用明细的行类型，区分提醒、采纳、不采纳与引用四类事实。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// 本枚举只表达接收侧明细行的归类，不跨侧复用于来源侧被互认明细。
/// </remarks>
[EnumDescriptor]
public enum RecognitionUsageDetailType
{
  /// <summary>
  /// 提醒行，数值 1；表示已下发但医院未反馈处理结果的匹配项。
  /// </summary>
  [Description("提醒")]
  Reminder = 1,
  /// <summary>
  /// 采纳行，数值 2；对应首次保存且不可更改的采纳处理结果。
  /// </summary>
  [Description("采纳")]
  Adopted = 2,
  /// <summary>
  /// 不采纳行，数值 3；对应首次保存且不可更改的不采纳处理结果。
  /// </summary>
  [Description("不采纳")]
  NotAdopted = 3,
  /// <summary>
  /// 引用行，数值 4；对应已采纳项目被医生引用写入病历的事实。
  /// </summary>
  [Description("引用")]
  Referenced = 4,
}
