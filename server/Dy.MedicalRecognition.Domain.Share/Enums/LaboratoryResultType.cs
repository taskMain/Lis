namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检验结果的数据形态，决定结果值如何解读与展示。
/// </summary>
public enum LaboratoryResultType
{
  /// <summary>
  /// 数值型结果，数值 1；配合单位与参考范围判定异常。
  /// </summary>
  Numeric = 1,
  /// <summary>
  /// 定性型结果，数值 2；取来源给出的固定结论。
  /// </summary>
  Qualitative = 2,
  /// <summary>
  /// 文本型结果，数值 3；按来源原文展示，不做数值解析。
  /// </summary>
  Textual = 3,
}
