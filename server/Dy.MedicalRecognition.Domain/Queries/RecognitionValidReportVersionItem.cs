namespace Dy.MedicalRecognition.Domain.Queries;

/// <summary>
/// 报告版本有效性查询投影行：一个仍为当前有效版本的报告版本标识。
/// </summary>
/// <remarks>
/// 行集由本次待校验的版本集合决定，只返回同时满足两个条件的版本：该版本仍是所属报告的当前版本，且所属报告的生命周期状态有效；
/// 调用方按"未出现在返回集合中"判定该绑定版本已失效。
/// 投影以单列行类型承载而不是标量集合：本平台实测观察到，标量集合在该语句上的结果处理不成立（结果反序列化抛空引用异常，见阶段 5 测试报告的票 08 段）。
/// 投影只用于校验绑定版本是否仍有效，不作为对外契约。
/// </remarks>
public sealed record RecognitionValidReportVersionItem
{
  /// <summary>仍为当前有效版本的报告版本标识。</summary>
  public Guid Id { get; init; }
}
