using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检验结果的数据形态，决定结果值如何解读与展示。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum LaboratoryResultType
{
  /// <summary>
  /// 数值型结果，数值 1；配合单位与参考范围判定异常。
  /// </summary>
  [Description("数值型")]
  Numeric = 1,
  /// <summary>
  /// 定性型结果，数值 2；取来源给出的固定结论。
  /// </summary>
  [Description("定性型")]
  Qualitative = 2,
  /// <summary>
  /// 文本型结果，数值 3；按来源原文展示，不做数值解析。
  /// </summary>
  [Description("文本型")]
  Textual = 3,
}
