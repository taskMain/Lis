namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认匹配项的处理结果，首次保存后不可更改。
/// </summary>
public enum RecognitionResult
{
  /// <summary>
  /// 采纳，数值 1；形成预计节省金额，不得带不采纳原因。
  /// </summary>
  Adopted = 1,
  /// <summary>
  /// 不采纳，数值 2；必须填写统一原因代码。
  /// </summary>
  NotAdopted = 2,
}
