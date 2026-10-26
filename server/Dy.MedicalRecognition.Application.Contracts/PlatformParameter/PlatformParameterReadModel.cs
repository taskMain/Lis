namespace Dy.MedicalRecognition.Application.Contracts.PlatformParameter;

/// <summary>
/// 平台参数维护列表的一行：参数编码、值、说明、启用状态与最近操作信息。
/// </summary>
public sealed record PlatformParameterReadModel
{
  /// <summary>
  /// 参数编码；全平台唯一，作为维护入口的定位键。
  /// </summary>
  public string ParameterCode { get; init; } = string.Empty;

  /// <summary>
  /// 当前参数值；按编码对应的业务规则保存。
  /// </summary>
  public string ParameterValue { get; init; } = string.Empty;

  /// <summary>
  /// 参数说明；仅用于维护界面展示。
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// 参数是否启用；停用后业务读取按缺失处理。
  /// </summary>
  public bool IsValid { get; init; }

  /// <summary>
  /// 最近一次保存的操作人标识。
  /// </summary>
  public Guid OperId { get; init; }

  /// <summary>
  /// 最近一次保存的操作时间。
  /// </summary>
  public DateTimeOffset OperTime { get; init; }
}
