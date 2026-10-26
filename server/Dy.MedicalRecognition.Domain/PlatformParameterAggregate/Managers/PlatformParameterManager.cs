using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Commands;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;

namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Managers;

/// <summary>
/// 平台参数的维护规则：按参数编码校验取值，保存新增或更新并登记对应领域事件。
/// </summary>
/// <remarks>取值校验与业务读取共用 <see cref="PlatformParameterDefinition"/> 的判定；本类不控制事务生命周期。</remarks>
public sealed class PlatformParameterManager : DomainService
{
  /// <summary>
  /// 平台参数的持久化入口。
  /// </summary>
  private readonly IPlatformParameterRepository repository;

  /// <summary>
  /// 初始化平台参数管理器。
  /// </summary>
  /// <param name="repository">平台参数持久化入口。</param>
  public PlatformParameterManager(IPlatformParameterRepository repository) => this.repository = repository;

  /// <summary>
  /// 新建一条平台参数。
  /// </summary>
  /// <param name="command">新建参数命令，携带已从可信上下文取得的操作字段。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">参数编码未登记、参数值非法或保存未影响任何行时抛出。</exception>
  public async Task<bool> CreateAsync(CreatePlatformParameterCommand command)
  {
    ValidateValue(command.ParameterCode, command.ParameterValue);
    PlatformParameter parameter = command.MapToPlatformParameter();
    parameter.Id = repository.CreateGuid();
    if (await repository.CreatePlatformParameterAsync(parameter) != 1) throw new InvalidOperationException("业务拒绝：参数保存失败。");
    AddEvent(command.CreatePlatformParameterCreatedEvent(parameter.Id));
    return true;
  }

  /// <summary>
  /// 修改一条已存在的平台参数。
  /// </summary>
  /// <param name="command">修改参数命令，携带已从可信上下文取得的操作字段。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">参数编码未登记、参数值非法或目标参数不存在时抛出。</exception>
  public async Task<bool> UpdateAsync(UpdatePlatformParameterCommand command)
  {
    ValidateValue(command.ParameterCode, command.ParameterValue);
    PlatformParameter parameter = command.MapToPlatformParameter();
    if (await repository.UpdatePlatformParameterAsync(parameter) != 1) throw new InvalidOperationException("业务拒绝：参数不存在。");
    AddEvent(command.CreatePlatformParameterUpdatedEvent());
    return true;
  }

  /// <summary>
  /// 校验参数编码已登记，且参数值符合该编码对应的规则。
  /// </summary>
  /// <param name="parameterCode">参数编码。</param>
  /// <param name="parameterValue">待保存的参数值。</param>
  /// <exception cref="InvalidOperationException">编码未登记或参数值非法时抛出。</exception>
  private static void ValidateValue(string parameterCode, string? parameterValue)
  {
    if (!PlatformParameterDefinition.IsKnownCode(parameterCode)) throw new InvalidOperationException("业务拒绝：参数编码不存在。");
    if (!PlatformParameterDefinition.IsValueValid(parameterCode, parameterValue))
      throw new InvalidOperationException($"业务拒绝：{PlatformParameterDefinition.GetDisplayName(parameterCode)}取值非法。");
  }
}