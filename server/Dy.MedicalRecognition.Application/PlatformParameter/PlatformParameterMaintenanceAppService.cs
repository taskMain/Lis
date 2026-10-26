using System.ComponentModel.DataAnnotations;
using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application.Contracts.PlatformParameter;
using Dy.MedicalRecognition.Application.Contracts.PlatformParameter.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Commands;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Managers;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;
using PlatformParameterEntity = Dy.MedicalRecognition.Domain.PlatformParameterAggregate.PlatformParameter;

namespace Dy.MedicalRecognition.Application.PlatformParameter;

/// <summary>
/// 平台参数维护入口：查询全部参数并按编码保存单条参数。
/// </summary>
/// <remarks>
/// 保存前按编码查询是否已存在，区分新增与更新；保存校验与业务读取共用 <see cref="PlatformParameterDefinition"/> 的取值规则。
/// 两个入口由手工维护控制器独占路由调用；接口不继承自动端点标记，框架不生成重复端点。
/// </remarks>
public sealed class PlatformParameterMaintenanceAppService : IPlatformParameterMaintenanceAppService
{
  /// <summary>平台参数持久化入口。</summary>
  private readonly IPlatformParameterRepository repository;

  /// <summary>平台参数维护规则。</summary>
  private readonly PlatformParameterManager manager;

  /// <summary>已验证的 IAM 当前上下文，提供保存操作人。</summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>初始化维护入口。</summary>
  /// <param name="repository">平台参数持久化入口。</param>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文。</param>
  public PlatformParameterMaintenanceAppService(IPlatformParameterRepository repository, ICurrentIamContext currentIamContext)
  {
    this.repository = repository;
    manager = new PlatformParameterManager(repository);
    this.currentIamContext = currentIamContext;
  }

  /// <inheritdoc/>
  public async Task<IReadOnlyList<PlatformParameterReadModel>> QueryPlatformParameterListAsync()
  {
    IReadOnlyList<PlatformParameterEntity> parameters = await repository.QueryAllPlatformParameterAsync();
    return
    [
      .. parameters.Select(parameter => new PlatformParameterReadModel
      {
        ParameterCode = parameter.ParameterCode,
        ParameterValue = parameter.ParameterValue,
        Description = parameter.Description,
        IsValid = parameter.IsValid,
        OperId = parameter.OperId,
        OperTime = parameter.OperTime
      })
    ];
  }

  /// <inheritdoc/>
  public async Task<bool> SavePlatformParameterAsync(string parameterCode, SavePlatformParameterRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    if (!PlatformParameterDefinition.IsKnownCode(parameterCode)) throw new InvalidOperationException("业务拒绝：参数编码不存在。");

    IamIdentityContext identity = currentIamContext.RequireCurrent();
    Guid operId = identity.UserId != Guid.Empty ? identity.UserId : throw new InvalidOperationException("无法确定有效的操作人。");
    bool isValid = request.IsValid ?? throw new InvalidOperationException("业务拒绝：参数启用状态缺失。");
    DateTimeOffset operTime = DateTimeOffset.UtcNow;

    // 保存前按编码查询是否已存在：命中改走更新，未命中才新增，不依赖唯一索引报错来区分。
    PlatformParameterEntity? existing = await repository.GetPlatformParameterByCodeAsync(parameterCode);
    if (existing is null)
    {
      return await manager.CreateAsync(new CreatePlatformParameterCommand
      {
        ParameterCode = parameterCode,
        ParameterValue = request.ParameterValue,
        Description = request.Description,
        IsValid = isValid,
        OperId = operId,
        OperTime = operTime
      });
    }

    return await manager.UpdateAsync(new UpdatePlatformParameterCommand
    {
      Id = existing.Id,
      ParameterCode = parameterCode,
      ParameterValue = request.ParameterValue,
      Description = request.Description,
      IsValid = isValid,
      OperId = operId,
      OperTime = operTime
    });
  }
}