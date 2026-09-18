using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 来源就诊类型的取值，与就诊流水号共同识别就诊。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum VisitType
{
  /// <summary>
  /// 门诊，数值 1。
  /// </summary>
  [Description("门诊")]
  Outpatient = 1,
  /// <summary>
  /// 急诊，数值 2。
  /// </summary>
  [Description("急诊")]
  Emergency = 2,
  /// <summary>
  /// 住院，数值 3；床位等字段不参与自动匹配。
  /// </summary>
  [Description("住院")]
  Inpatient = 3,
  /// <summary>
  /// 体检，数值 4。
  /// </summary>
  [Description("体检")]
  PhysicalExam = 4,
  /// <summary>
  /// 其他，数值 5；只用于确实无法归入前四类的特殊就诊类型，不接受未知取值，也不得作为来源字段缺失时的默认值。
  /// </summary>
  [Description("其他")]
  Other = 5,
}
