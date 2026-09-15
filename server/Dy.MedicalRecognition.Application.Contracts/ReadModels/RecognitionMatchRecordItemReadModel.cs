namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionMatchRecordItemReadModel
{
  /// <summary>
  /// 互认匹配项ID
  /// </summary>
  public Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 互认项目
  /// </summary>
  public object Item { get; set; }
  /// <summary>
  /// 来源归属
  /// </summary>
  public object Source { get; set; }
  /// <summary>
  /// 报告ID
  /// </summary>
  public Guid ReportId { get; set; }
  /// <summary>
  /// 报告版本ID
  /// </summary>
  public Guid ReportVersionId { get; set; }
  /// <summary>
  /// 是否已反馈
  /// </summary>
  public bool IsProcessed { get; set; }
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
}
