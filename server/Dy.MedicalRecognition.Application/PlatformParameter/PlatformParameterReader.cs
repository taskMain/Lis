using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;
using PlatformParameterEntity = Dy.MedicalRecognition.Domain.PlatformParameterAggregate.PlatformParameter;

namespace Dy.MedicalRecognition.Application.PlatformParameter;

/// <summary>
/// 业务读取平台参数的统一入口：按编码读取当前值并执行参数值规则。
/// </summary>
/// <remarks>每次调用都从参数存储读取，不使用进程内缓存或静态字段；缺失、停用或非法值都原样抛出业务拒绝。</remarks>
internal sealed class PlatformParameterReader
{
  /// <summary>平台参数持久化入口。</summary>
  private readonly IPlatformParameterRepository repository;

  /// <summary>初始化平台参数读取入口。</summary>
  /// <param name="repository">平台参数持久化入口。</param>
  internal PlatformParameterReader(IPlatformParameterRepository repository) => this.repository = repository;

  /// <summary>读取本院报告匹配开关。</summary>
  /// <returns>开关是否开启。</returns>
  /// <exception cref="InvalidOperationException">参数缺失、停用或取值不是 0/1 时抛出。</exception>
  internal async Task<bool> ReadOwnHospitalMatchSwitchAsync()
  {
    string value = await GetRequiredValueAsync(PlatformParameterDefinition.OwnHospitalMatchSwitch);
    return string.Equals(value, "1", StringComparison.Ordinal);
  }

  /// <summary>读取本院报告排除时长。</summary>
  /// <returns>非负整数小时。</returns>
  /// <exception cref="InvalidOperationException">参数缺失、停用或取值不是非负整数时抛出。</exception>
  internal async Task<int> ReadOwnHospitalExcludeHoursAsync()
  {
    string value = await GetRequiredValueAsync(PlatformParameterDefinition.OwnHospitalExcludeHours);
    return int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
  }

  /// <summary>读取引用详情有效时长。</summary>
  /// <returns>正整数小时。</returns>
  /// <exception cref="InvalidOperationException">参数缺失、停用或取值不是正整数时抛出。</exception>
  internal async Task<int> ReadCitationDetailValidHoursAsync()
  {
    string value = await GetRequiredValueAsync(PlatformParameterDefinition.CitationDetailValidHours);
    return int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// 按编码读取当前参数值，并统一处理缺失、停用与非法取值。
  /// </summary>
  /// <param name="parameterCode">本应用维护的参数编码。</param>
  /// <returns>通过规则校验的参数值。</returns>
  /// <exception cref="InvalidOperationException">参数缺失、停用或取值非法时抛出。</exception>
  private async Task<string> GetRequiredValueAsync(string parameterCode)
  {
    string displayName = PlatformParameterDefinition.GetDisplayName(parameterCode);
    PlatformParameterEntity? parameter = await repository.GetPlatformParameterByCodeAsync(parameterCode);
    if (parameter is null || !parameter.IsValid) throw new InvalidOperationException($"业务拒绝：未配置{displayName}。");
    if (!PlatformParameterDefinition.IsValueValid(parameterCode, parameter.ParameterValue))
      throw new InvalidOperationException($"业务拒绝：{displayName}取值非法。");

    return parameter.ParameterValue;
  }
}