using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 标准目录中项目类型的取值，区分检验与检查。
/// </summary>
/// <remarks>
/// 标准项目编码跨这两类仍然全局唯一，检验与检查之间不可重复。
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum MedicalItemType
{
  /// <summary>
  /// 检验，数值 0。
  /// </summary>
  /// <remarks>对应检验报告，亦为项目类型默认值。</remarks>
  [Description("检验")]
  Laboratory = 0,
  /// <summary>
  /// 检查，数值 1。
  /// </summary>
  /// <remarks>对应检查报告及其部位、影像信息。</remarks>
  [Description("检查")]
  Examination = 1,
}
