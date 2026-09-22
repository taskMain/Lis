using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认使用统计的分组维度。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// 来源侧汇总只接受来源医院、来源院区与标准项目三个维度，互认科室维度值在来源侧按业务拒绝。
/// </remarks>
[EnumDescriptor]
public enum RecognitionStatisticsGroupDimension
{
  /// <summary>
  /// 按医院分组，数值 1；取自业务归属范围内的医院。
  /// </summary>
  [Description("医院")]
  Hospital = 1,
  /// <summary>
  /// 按院区分组，数值 2；院区隶属于该分组所用的医院。
  /// </summary>
  [Description("院区")]
  Branch = 2,
  /// <summary>
  /// 按互认科室分组，数值 3；取自处理结果保存的科室。
  /// </summary>
  [Description("互认科室")]
  RecognitionDepartment = 3,
  /// <summary>
  /// 按标准项目分组，数值 4；取自匹配命中的标准项目。
  /// </summary>
  [Description("标准项目")]
  StandardItem = 4,
}
