namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionMatchesResponseReadModel
{
  /// <summary>
  /// 是否存在匹配
  /// </summary>
  public bool HasMatches { get; set; }
  /// <summary>
  /// 互认匹配记录ID
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 匹配生成时间
  /// </summary>
  public DateTime MatchCreatedTime { get; set; }
  /// <summary>
  /// 报告集合
  /// </summary>
  public object Reports { get; set; }
}
