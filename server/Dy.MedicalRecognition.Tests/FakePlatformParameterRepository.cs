using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 平台参数持久化替身：按编码记录读取次数与顺序，并保存新增和更新结果供行为断言。
/// </summary>
/// <remarks>
/// 默认没有任何参数，缺失、停用与非法取值因此可以在同一替身上分别构造；
/// <see cref="ValuesByCode"/> 供既有系统参数读取用例直接预置值，保存入口写入的记录同时维护该字典。
/// </remarks>
internal sealed class FakePlatformParameterRepository : IPlatformParameterRepository
{
  /// <summary>按参数编码预置的参数值，供读取入口与列表查询使用。</summary>
  internal Dictionary<string, string> ValuesByCode { get; } = new(StringComparer.Ordinal);

  /// <summary>已标记停用的参数编码集合。</summary>
  internal HashSet<string> DisabledCodes { get; } = new(StringComparer.Ordinal);

  /// <summary>每次读取的参数编码，按调用顺序记录。</summary>
  internal List<string> ReadCodes { get; } = [];

  /// <summary>读取调用次数。</summary>
  internal int ReadCount { get; private set; }

  /// <summary>非空时读取入口抛出该异常，用于构造参数存储读取失败。</summary>
  internal Exception? Failure { get; init; }

  /// <summary>新增调用次数。</summary>
  internal int CreateCount { get; private set; }

  /// <summary>更新调用次数。</summary>
  internal int UpdateCount { get; private set; }

  /// <summary>最近一次新增写入的实体。</summary>
  internal PlatformParameter? LastCreated { get; private set; }

  /// <summary>最近一次更新写入的实体。</summary>
  internal PlatformParameter? LastUpdated { get; private set; }

  /// <summary>按编码保存的实体；新增与更新都写入这里。</summary>
  private Dictionary<string, PlatformParameter> Stored { get; } = new(StringComparer.Ordinal);

  /// <summary>
  /// 预置一条读取值，并设置启用状态。
  /// </summary>
  /// <param name="parameterCode">参数编码。</param>
  /// <param name="value">参数值。</param>
  /// <param name="isValid">是否启用。</param>
  internal void Set(string parameterCode, string value, bool isValid = true)
  {
    ValuesByCode[parameterCode] = value;
    if (isValid) DisabledCodes.Remove(parameterCode);
    else DisabledCodes.Add(parameterCode);
  }

  /// <inheritdoc/>
  public Guid CreateGuid() => Guid.NewGuid();

  /// <inheritdoc/>
  public Task<PlatformParameter?> GetPlatformParameterByCodeAsync(string parameterCode)
  {
    ReadCount++;
    ReadCodes.Add(parameterCode);
    if (Failure is not null) throw Failure;

    if (Stored.TryGetValue(parameterCode, out PlatformParameter? stored)) return Task.FromResult<PlatformParameter?>(stored);
    if (!ValuesByCode.TryGetValue(parameterCode, out string? value)) return Task.FromResult<PlatformParameter?>(null);

    return Task.FromResult<PlatformParameter?>(new PlatformParameter
    {
      Id = Guid.NewGuid(),
      ParameterCode = parameterCode,
      ParameterValue = value,
      Description = null,
      IsValid = !DisabledCodes.Contains(parameterCode),
      OperId = Guid.Empty,
      OperTime = DateTimeOffset.UtcNow
    });
  }

  /// <inheritdoc/>
  public Task<IReadOnlyList<PlatformParameter>> QueryAllPlatformParameterAsync()
  {
    List<PlatformParameter> parameters = [];
    foreach ((string code, PlatformParameter parameter) in Stored.OrderBy(item => item.Key, StringComparer.Ordinal))
      parameters.Add(parameter);
    foreach ((string code, string value) in ValuesByCode.OrderBy(item => item.Key, StringComparer.Ordinal))
    {
      if (Stored.ContainsKey(code)) continue;
      parameters.Add(new PlatformParameter
      {
        Id = Guid.NewGuid(),
        ParameterCode = code,
        ParameterValue = value,
        IsValid = !DisabledCodes.Contains(code)
      });
    }

    return Task.FromResult<IReadOnlyList<PlatformParameter>>([.. parameters.OrderBy(parameter => parameter.ParameterCode, StringComparer.Ordinal)]);
  }

  /// <inheritdoc/>
  public Task<int> CreatePlatformParameterAsync(PlatformParameter parameter)
  {
    CreateCount++;
    LastCreated = parameter;
    Stored[parameter.ParameterCode] = parameter;
    ValuesByCode[parameter.ParameterCode] = parameter.ParameterValue;
    if (parameter.IsValid) DisabledCodes.Remove(parameter.ParameterCode);
    else DisabledCodes.Add(parameter.ParameterCode);
    return Task.FromResult(1);
  }

  /// <inheritdoc/>
  public Task<int> UpdatePlatformParameterAsync(PlatformParameter parameter)
  {
    UpdateCount++;
    LastUpdated = parameter;
    Stored[parameter.ParameterCode] = parameter;
    ValuesByCode[parameter.ParameterCode] = parameter.ParameterValue;
    if (parameter.IsValid) DisabledCodes.Remove(parameter.ParameterCode);
    else DisabledCodes.Add(parameter.ParameterCode);
    return Task.FromResult(1);
  }
}
