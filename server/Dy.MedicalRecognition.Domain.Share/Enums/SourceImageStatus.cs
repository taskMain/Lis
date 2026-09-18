using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检查报告来源影像的状态；平台不接收影像文件，也不按调阅地址是否为空反推状态，状态与地址的一致性由来源侧负责。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum SourceImageStatus
{
  /// <summary>
  /// 有影像，数值 1；地址缺失表示暂不可调阅。
  /// </summary>
  [Description("有影像")]
  Available = 1,
  /// <summary>
  /// 无影像，数值 2；调阅地址必须为空。
  /// </summary>
  [Description("无影像")]
  None = 2,
  /// <summary>
  /// 状态未知，数值 3；调阅地址可有可无。
  /// </summary>
  [Description("未知")]
  Unknown = 3,
}
