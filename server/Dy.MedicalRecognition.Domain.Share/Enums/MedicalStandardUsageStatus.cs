using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 目录对象是否存在下级对象的展示口径。
/// </summary>
/// <remarks>
/// 只表示有无下级对象，与对象自身是否启用无关，也不替代启用状态。
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum MedicalStandardUsageStatus
{
  /// <summary>
  /// 未使用，数值 0。
  /// </summary>
  /// <remarks>不存在任何下级对象。</remarks>
  [Description("未使用")]
  Unused = 0,
  /// <summary>
  /// 已使用，数值 1。
  /// </summary>
  /// <remarks>存在下级对象即视为已使用，与下级对象自身是否启用无关，包含全部下级均已停用的情况。</remarks>
  [Description("已使用")]
  InUse = 1
}
