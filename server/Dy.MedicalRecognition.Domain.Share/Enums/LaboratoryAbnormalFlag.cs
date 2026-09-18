using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检验结果的异常标志，说明结果相对参考范围的位置；标志为空表示来源未提供或无法判定异常方向，不等于正常。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum LaboratoryAbnormalFlag
{
  /// <summary>
  /// 结果落在参考范围内，数值 1。
  /// </summary>
  [Description("正常")]
  Normal = 1,
  /// <summary>
  /// 结果高于参考范围上限，数值 2。
  /// </summary>
  [Description("偏高")]
  High = 2,
  /// <summary>
  /// 结果低于参考范围下限，数值 3。
  /// </summary>
  [Description("偏低")]
  Low = 3,
  /// <summary>
  /// 结果异常但无方向，数值 4；用于定性结果异常。
  /// </summary>
  [Description("其他异常")]
  OtherAbnormal = 4,
}
