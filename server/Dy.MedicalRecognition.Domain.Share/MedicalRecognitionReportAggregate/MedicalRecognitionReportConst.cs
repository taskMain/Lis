namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;

/// <summary>
/// 医学报告互认聚合的固定标识常量，供领域事件与订阅方识别事件所属的聚合。
/// </summary>
public static class MedicalRecognitionReportConst
{
  /// <summary>
  /// 聚合标识；取聚合根的完整类型名，不得改写字面量。
  /// </summary>
  public const string AggregateId = "Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.MedicalRecognitionReport";
}
