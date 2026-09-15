namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 来源就诊类型的取值，与就诊流水号共同识别就诊。
/// </summary>
public enum VisitType
{
  /// <summary>
  /// 门诊，数值 1。
  /// </summary>
  Outpatient = 1,
  /// <summary>
  /// 急诊，数值 2。
  /// </summary>
  Emergency = 2,
  /// <summary>
  /// 住院，数值 3；床位等字段不参与自动匹配。
  /// </summary>
  Inpatient = 3,
  /// <summary>
  /// 体检，数值 4。
  /// </summary>
  PhysicalExam = 4,
  /// <summary>
  /// 其他，数值 5；只用于确实无法归入前四类的特殊就诊类型，不接受未知取值，也不得作为来源字段缺失时的默认值。
  /// </summary>
  Other = 5,
}
