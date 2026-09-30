namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 批量建立互认项目配置时每一条配置的创建快照：该配置的标识、标准项目编码、可互认时间与创建后的启用状态。
/// </summary>
/// <remarks>
/// 同一批次内各配置共用批次上的可互认时间，因此每个快照的天数与批次取值相同；
/// 本类型只承载已保存事实的载荷，不充当对外返回数据。
/// </remarks>
public sealed record MutualRecognitionItemCreatedSnapshot
{
  /// <summary>
  /// 配置标识；由服务端在写入前生成
  /// </summary>
  public Guid Id { get; init; }
  /// <summary>
  /// 标准项目编码；经目录校验且在该组织内尚未配置的编码
  /// </summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>
  /// 可互认时间天数；取自本次提交统一提供的天数
  /// </summary>
  public int RecognitionDurationDays { get; init; }
  /// <summary>
  /// 创建后的启用状态；新建配置固定为启用，供订阅者按状态处理
  /// </summary>
  public bool IsValid { get; init; }
}
