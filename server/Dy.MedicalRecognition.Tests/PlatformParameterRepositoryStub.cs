using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 平台参数存储的共享替身：按参数编码预置读取值、预置停用状态与预置读取异常，供既有应用服务用例注入。
/// </summary>
/// <remarks>
/// 本替身承接原先外部系统参数服务替身的注入位置，使互认匹配与引用详情读取改走本应用参数表后，
/// 既有失败语义用例仍能分别构造缺失、停用、非法值与读取失败；新增与更新由维护入口专用替身承担，这里不实现。
/// </remarks>
internal sealed class PlatformParameterRepositoryStub : IPlatformParameterRepository
{
  /// <summary>
  /// 按参数编码预置的返回值；未预置的编码按“参数不存在”返回空值。
  /// </summary>
  public Dictionary<string, string> ValuesByCode { get; } = [];

  /// <summary>
  /// 预置停用的参数编码集合；读取时据此返回停用记录。
  /// </summary>
  public HashSet<string> DisabledCodes { get; } = [];

  /// <summary>
  /// 非空时表示参数存储读取失败：按该异常向外抛出，用于构造读取异常前置。
  /// </summary>
  public Exception? Failure { get; set; }

  /// <summary>
  /// 按参数编码读取的调用次数。
  /// </summary>
  public int ReadCount { get; private set; }

  /// <summary>
  /// 按读取顺序记录的参数编码，供用例核对读取的是哪几个平台参数。
  /// </summary>
  public List<string> ReadCodes { get; } = [];

  /// <summary>
  /// 最近一次读取的参数编码；用于核对读取入口只按编码取值。
  /// </summary>
  public string? LastCode { get; private set; }

  /// <inheritdoc/>
  public Guid CreateGuid() => Guid.NewGuid();

  /// <inheritdoc/>
  public Task<PlatformParameter?> GetPlatformParameterByCodeAsync(string parameterCode)
  {
    ReadCount++;
    ReadCodes.Add(parameterCode);
    LastCode = parameterCode;
    if (Failure is not null) throw Failure;

    return Task.FromResult(ValuesByCode.TryGetValue(parameterCode, out string? value)
      ? new PlatformParameter
      {
        Id = Guid.NewGuid(),
        ParameterCode = parameterCode,
        ParameterValue = value,
        IsValid = !DisabledCodes.Contains(parameterCode),
        OperId = Guid.Empty,
        OperTime = DateTimeOffset.UtcNow
      }
      : null);
  }

  /// <inheritdoc/>
  public Task<IReadOnlyList<PlatformParameter>> QueryAllPlatformParameterAsync() =>
    Task.FromResult<IReadOnlyList<PlatformParameter>>(
    [
      .. ValuesByCode.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => new PlatformParameter
      {
        Id = Guid.NewGuid(),
        ParameterCode = item.Key,
        ParameterValue = item.Value,
        IsValid = !DisabledCodes.Contains(item.Key),
        OperId = Guid.Empty,
        OperTime = DateTimeOffset.UtcNow
      })
    ]);

  /// <inheritdoc/>
  public Task<int> CreatePlatformParameterAsync(PlatformParameter parameter) =>
    throw new NotSupportedException("共享读取替身不实现平台参数新增。");

  /// <inheritdoc/>
  public Task<int> UpdatePlatformParameterAsync(PlatformParameter parameter) =>
    throw new NotSupportedException("共享读取替身不实现平台参数更新。");
}
