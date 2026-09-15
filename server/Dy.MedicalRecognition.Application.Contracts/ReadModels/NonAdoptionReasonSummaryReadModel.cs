namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record NonAdoptionReasonSummaryReadModel
{
  /// <summary>
  /// 原因代码
  /// </summary>
  public string ReasonCode { get; set; }
  /// <summary>
  /// 原因名称
  /// </summary>
  public string ReasonName { get; set; }
  /// <summary>
  /// 次数
  /// </summary>
  public int Count { get; set; }
  /// <summary>
  /// 占比
  /// </summary>
  public decimal Ratio { get; set; }
}
