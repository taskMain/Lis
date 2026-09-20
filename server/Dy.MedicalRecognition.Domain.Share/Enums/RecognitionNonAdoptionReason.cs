using System.ComponentModel;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 不采纳互认结果的平台统一原因代码；仅在处理结果为不采纳时填写，平台不提供医院自行新增、修改或停用原因代码的能力。
/// </summary>
/// <remarks>
/// 本枚举属于对外契约的一部分：启用 SourceGen 描述器生成后，应用层的 OpenAPI 转换器据其补充 `enum`、
/// `x-enumNames` 与 `x-enumDescriptions`，前端按数值交互并可从事先声明的中文说明取值。
/// </remarks>
[EnumDescriptor]
public enum RecognitionNonAdoptionReason
{
  /// <summary>
  /// 病情变化致结果难以满足诊疗需求，数值 1。
  /// </summary>
  [Description("病情变化致结果难以满足诊疗需求")]
  CurrentConditionMismatch = 1,
  /// <summary>
  /// 结果在疾病发展演变中变化较快，数值 2。
  /// </summary>
  [Description("结果在疾病发展演变中变化较快")]
  RapidDiseaseProgression = 2,
  /// <summary>
  /// 手术、输血等重大医疗措施前，数值 3。
  /// </summary>
  [Description("手术、输血等重大医疗措施前")]
  BeforeMajorMedicalMeasure = 3,
  /// <summary>
  /// 患者处于急诊、急救等紧急状态，数值 4。
  /// </summary>
  [Description("患者处于急诊、急救等紧急状态")]
  EmergencyCare = 4,
  /// <summary>
  /// 涉及司法、伤残及病退等鉴定，数值 5。
  /// </summary>
  [Description("涉及司法、伤残及病退等鉴定")]
  ForensicOrDisabilityAssessment = 5,
  /// <summary>
  /// 其他情形确需复查，数值 6；补充说明必填。
  /// </summary>
  [Description("其他情形确需复查")]
  OtherReviewRequired = 6,
}
