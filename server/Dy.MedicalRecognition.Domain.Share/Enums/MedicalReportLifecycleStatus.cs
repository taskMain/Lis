using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 报告在平台内的生命周期状态，作废后不再回转。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum MedicalReportLifecycleStatus
{
  /// <summary>
  /// 有效，数值 1；版本与匹配结果参与后续业务。
  /// </summary>
  [Description("有效")]
  Effective = 1,
  /// <summary>
  /// 已作废，数值 2；作废时间与原因成对记录，不可逆。
  /// </summary>
  [Description("已作废")]
  Voided = 2,
}
