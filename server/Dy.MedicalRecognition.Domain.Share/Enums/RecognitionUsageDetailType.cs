namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 接收侧互认使用明细的行类型，区分提醒、采纳、不采纳与引用四类事实。
/// </summary>
public enum RecognitionUsageDetailType
{
  /// <summary>
  /// 提醒行，数值 1；表示已下发但医院未反馈处理结果的匹配项。
  /// </summary>
  Reminder = 1,
  /// <summary>
  /// 采纳行，数值 2；对应首次保存且不可更改的采纳处理结果。
  /// </summary>
  Adopted = 2,
  /// <summary>
  /// 不采纳行，数值 3；对应首次保存且不可更改的不采纳处理结果。
  /// </summary>
  NotAdopted = 3,
  /// <summary>
  /// 引用行，数值 4；对应已采纳项目被医生引用写入病历的事实。
  /// </summary>
  Referenced = 4,
}
