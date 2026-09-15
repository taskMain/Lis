namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 目录对象是否存在下级对象的展示口径。
/// </summary>
/// <remarks>只表示有无下级对象，与对象自身是否启用无关，也不替代启用状态。</remarks>
public enum MedicalStandardUsageStatus
{
  /// <summary>
  /// 未使用，数值 0。
  /// </summary>
  /// <remarks>不存在任何下级对象。</remarks>
  Unused = 0,
  /// <summary>
  /// 使用中，数值 1。
  /// </summary>
  /// <remarks>下级对象包含已停用者。</remarks>
  InUse = 1
}
