namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 标准目录中项目类型的取值，区分检验与检查。
/// </summary>
/// <remarks>标准项目编码跨这两类仍然全局唯一，检验与检查之间不可重复。</remarks>
public enum MedicalItemType
{
  /// <summary>
  /// 检验，数值 0。
  /// </summary>
  /// <remarks>对应检验报告，亦为项目类型默认值。</remarks>
  Laboratory = 0,
  /// <summary>
  /// 检查，数值 1。
  /// </summary>
  /// <remarks>对应检查报告及其部位、影像信息。</remarks>
  Examination = 1,
}
