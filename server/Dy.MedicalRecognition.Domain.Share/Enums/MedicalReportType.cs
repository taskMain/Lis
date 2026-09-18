using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 报告类型的取值，区分检验报告与检查报告；与标准项目类型编号口径不同，两者不可按数值转换。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum MedicalReportType
{
  /// <summary>
  /// 检验报告，数值 1；语义同检验项目类型，数值不同。
  /// </summary>
  [Description("检验报告")]
  Laboratory = 1,
  /// <summary>
  /// 检查报告，数值 2；语义同检查项目类型，数值不同。
  /// </summary>
  [Description("检查报告")]
  Examination = 2,
}
