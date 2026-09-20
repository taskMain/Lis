using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认匹配项的处理结果，首次保存后不可更改。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum RecognitionResult
{
  /// <summary>
  /// 采纳，数值 1；形成预计节省金额，不得带不采纳原因。
  /// </summary>
  [Description("采纳")]
  Adopted = 1,
  /// <summary>
  /// 不采纳，数值 2；必须填写统一原因代码。
  /// </summary>
  [Description("不采纳")]
  NotAdopted = 2,
}
