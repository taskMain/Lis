namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionProcessingResultDetailReadModel
{
  /// <summary>
  /// 是否已反馈
  /// </summary>
  public bool IsProcessed { get; set; }
  /// <summary>
  /// 互认时间
  /// </summary>
  public DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 处理结果
  /// </summary>
  public string Decision { get; set; }
  /// <summary>
  /// 不采纳原因代码
  /// </summary>
  public string NonAdoptionReasonCode { get; set; }
  /// <summary>
  /// 不采纳原因名称
  /// </summary>
  public string NonAdoptionReasonName { get; set; }
  /// <summary>
  /// 不采纳补充说明
  /// </summary>
  public string NonAdoptionSupplementDescription { get; set; }
  /// <summary>
  /// 预计节省金额
  /// </summary>
  public decimal EstimatedSavingAmount { get; set; }
}
