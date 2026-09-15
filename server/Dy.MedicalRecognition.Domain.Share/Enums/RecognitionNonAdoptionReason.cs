namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 不采纳互认结果的平台统一原因代码；仅在处理结果为不采纳时填写，平台不提供医院自行新增、修改或停用原因代码的能力。
/// </summary>
public enum RecognitionNonAdoptionReason
{
  /// <summary>
  /// 病情变化致结果难以满足诊疗需求，数值 1。
  /// </summary>
  CurrentConditionMismatch = 1,
  /// <summary>
  /// 结果在疾病发展演变中变化较快，数值 2。
  /// </summary>
  RapidDiseaseProgression = 2,
  /// <summary>
  /// 手术、输血等重大医疗措施前，数值 3。
  /// </summary>
  BeforeMajorMedicalMeasure = 3,
  /// <summary>
  /// 患者处于急诊、急救等紧急状态，数值 4。
  /// </summary>
  EmergencyCare = 4,
  /// <summary>
  /// 涉及司法、伤残及病退等鉴定，数值 5。
  /// </summary>
  ForensicOrDisabilityAssessment = 5,
  /// <summary>
  /// 其他情形确需复查，数值 6；补充说明必填。
  /// </summary>
  OtherReviewRequired = 6,
}
