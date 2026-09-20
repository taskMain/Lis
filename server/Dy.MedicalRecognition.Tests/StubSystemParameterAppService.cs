using Dy.Base.Application.Contracts.SystemParameterAggregate;
using Dy.Core.Abstractions.Domain.Dtos;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 外部系统参数服务的共享替身：按参数编码预置返回值、预置"参数不存在"与预置抛出异常三类行为，其余成员调用即失败。
/// </summary>
/// <remarks>
/// 本阶段是本平台首次调用系统参数服务，严格失败语义需要在不连接平台的前提下分别构造取值缺失、取值非法与参数服务失败三类前置；
/// 取值非法由预置的返回文本表达，取值缺失由不预置返回文本表达，服务失败由 <see cref="Failure"/> 表达。
/// 未实现的成员一律抛出，避免用例在未覆盖的路径上静默走过。
/// </remarks>
internal sealed class StubSystemParameterAppService : ISystemParameterAppService
{
  /// <summary>
  /// 按参数编码预置的返回值；未预置的编码按"参数不存在"返回空值。
  /// </summary>
  public Dictionary<string, string> ValuesByCode { get; } = [];

  /// <summary>
  /// 非空时表示参数服务读取失败：按该异常向外抛出，用于构造参数服务异常前置。
  /// </summary>
  public Exception? Failure { get; set; }

  /// <summary>
  /// 按参数编码读取的调用次数。
  /// </summary>
  public int ReadCount { get; private set; }

  /// <summary>
  /// 按读取顺序记录的参数编码，供用例核对读取的是哪几个平台级参数。
  /// </summary>
  public List<string> ReadCodes { get; } = [];

  /// <summary>
  /// 最近一次读取使用的参数编码与三个维度传值，用于核对读取不传组织、医院与院区维度。
  /// </summary>
  public GetSystemParameterByCodeRequest? LastRequest { get; private set; }

  /// <inheritdoc/>
  public Task<SystemParameterDto> GetSystemParameterByCodeAsync(GetSystemParameterByCodeRequest getSystemParameterByCodeRequest)
  {
    ReadCount++;
    ReadCodes.Add(getSystemParameterByCodeRequest.Code);
    LastRequest = getSystemParameterByCodeRequest;
    if (Failure is not null) throw Failure;

    return Task.FromResult(ValuesByCode.TryGetValue(getSystemParameterByCodeRequest.Code, out string? value)
      ? new SystemParameterDto { Code = getSystemParameterByCodeRequest.Code, Value = value }
      : null!);
  }

  /// <inheritdoc/>
  public Task<string> CreateSystemParameterAsync(CreateSystemParameterRequest createSystemParameterRequest) => Unsupported<string>();

  /// <inheritdoc/>
  public Task<int> UpdateSystemParameterAsync(UpdateSystemParameterRequest updateSystemParameterRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteSystemParameterAsync(DeleteSystemParameterRequest deleteSystemParameterRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<IEnumerable<SystemParameterDto>> QuerySystemParameterByModuleCodeAsync(QuerySystemParameterByModuleCodeRequest querySystemParameterByModuleCodeRequest) =>
    Unsupported<IEnumerable<SystemParameterDto>>();

  /// <inheritdoc/>
  public Task<Guid> CreateSystemParameterConfigAsync(CreateSystemParameterConfigRequest createSystemParameterConfigRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> UpdateSystemParameterConfigAsync(UpdateSystemParameterConfigRequest updateSystemParameterConfigRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteSystemParameterConfigAsync(DeleteSystemParameterConfigRequest deleteSystemParameterConfigRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<SystemParameterConfigDto> GetOrgSystemParameterConfigByCodeAsync(GetOrgSystemParameterConfigByCodeRequest getOrgSystemParameterConfigByCodeRequest) =>
    Unsupported<SystemParameterConfigDto>();

  /// <inheritdoc/>
  public Task<SystemParameterConfigDto> GetSystemParameterConfigByIdAsync(GetSystemParameterConfigByIdRequest getSystemParameterConfigByIdRequest) =>
    Unsupported<SystemParameterConfigDto>();

  /// <inheritdoc/>
  public Task<string?> GetOrgSystemParameterValueByCodeAsync(GetOrgSystemParameterValueByCodeRequest getOrgSystemParameterValueByCodeRequest) =>
    Unsupported<string?>();

  /// <inheritdoc/>
  public Task<IEnumerable<CodeValueDto>> QueryOrgSystemParameterValuesAsync(QueryOrgSystemParameterValuesRequest queryOrgSystemParameterValuesRequest) =>
    Unsupported<IEnumerable<CodeValueDto>>();

  /// <summary>本替身未实现该外部系统参数服务成员。</summary>
  /// <typeparam name="TResult">成员返回类型。</typeparam>
  /// <returns>不会返回。</returns>
  /// <exception cref="NotSupportedException">该成员被调用时抛出。</exception>
  private static Task<TResult> Unsupported<TResult>() =>
    throw new NotSupportedException("共享替身只实现按参数编码读取平台级参数值。");
}
