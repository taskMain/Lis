namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检验结果的异常标志，说明结果相对参考范围的位置；标志为空表示来源未提供或无法判定异常方向，不等于正常。
/// </summary>
public enum LaboratoryAbnormalFlag
{
  /// <summary>
  /// 结果落在参考范围内，数值 1。
  /// </summary>
  Normal = 1,
  /// <summary>
  /// 结果高于参考范围上限，数值 2。
  /// </summary>
  High = 2,
  /// <summary>
  /// 结果低于参考范围下限，数值 3。
  /// </summary>
  Low = 3,
  /// <summary>
  /// 结果异常但无方向，数值 4；用于定性结果异常。
  /// </summary>
  OtherAbnormal = 4,
}
