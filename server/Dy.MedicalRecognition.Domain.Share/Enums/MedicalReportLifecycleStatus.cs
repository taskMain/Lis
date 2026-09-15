namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 报告在平台内的生命周期状态，作废后不再回转。
/// </summary>
public enum MedicalReportLifecycleStatus
{
  /// <summary>
  /// 有效，数值 1；版本与匹配结果参与后续业务。
  /// </summary>
  Effective = 1,
  /// <summary>
  /// 已作废，数值 2；作废时间与原因成对记录，不可逆。
  /// </summary>
  Voided = 2,
}
