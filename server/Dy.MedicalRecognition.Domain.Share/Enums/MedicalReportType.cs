namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 报告类型的取值，区分检验报告与检查报告；与标准项目类型编号口径不同，两者不可按数值转换。
/// </summary>
public enum MedicalReportType
{
  /// <summary>
  /// 检验报告，数值 1；语义同检验项目类型，数值不同。
  /// </summary>
  Laboratory = 1,
  /// <summary>
  /// 检查报告，数值 2；语义同检查项目类型，数值不同。
  /// </summary>
  Examination = 2,
}
