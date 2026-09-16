using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认项目配置的启用状态；数值 1 启用、2 停用。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum ConfigurationStatus
{
  /// <summary>
  /// 配置启用，数值 1；参与互认匹配与后续引用。
  /// </summary>
  [Description("启用")]
  Enabled = 1,
  /// <summary>
  /// 配置停用，数值 2；数据保留，可重新启用；与标准目录实体的布尔启用状态字段无关，两者不可互换。
  /// </summary>
  [Description("停用")]
  Disabled = 2,
}
