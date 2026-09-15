using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录与互认项目配置的写入口。
/// </summary>
/// <remarks>仅新建标准分组和新建标准项目声明显式事务，其余用例各只有一条写语句。</remarks>
public partial class MedicalRecognitionReportAppService : ApplicationService, IMedicalRecognitionReportAppService
{
  /// <summary>
  /// 标准目录与互认项目配置的领域管理器。
  /// </summary>
  private readonly MedicalRecognitionReportManager manager;

  /// <summary>
  /// 初始化写入口。
  /// </summary>
  /// <remarks>保留执行目录业务规则校验、数据读写与事件登记的领域管理器。</remarks>
  /// <param name="manager">目录领域管理器。</param>
  public MedicalRecognitionReportAppService(MedicalRecognitionReportManager manager)
  {
    this.manager = manager;
  }

  /// <summary>
  /// 新建标准项目分类。
  /// </summary>
  /// <remarks>分类名称在全部项目类型范围内唯一；分类以启用状态创建，标识与操作信息由服务端写入。</remarks>
  /// <param name="request">新建分类请求。</param>
  /// <returns>是否创建成功。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">登录上下文没有可解析的操作人标识，或分类名称已存在、分类保存未写入任何行时抛出。</exception>
  public async Task<bool> CreateMedicalStandardCategoryAsync(CreateMedicalStandardCategoryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var createMedicalStandardCategoryCommand = request.MapToCreateMedicalStandardCategoryCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMedicalStandardCategoryCommand.OperId = userId;
    createMedicalStandardCategoryCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.CreateMedicalStandardCategoryAsync(createMedicalStandardCategoryCommand);
  }

  /// <summary>
  /// 修改标准项目分类的名称、项目类型与备注。
  /// </summary>
  /// <param name="request">修改分类请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类不存在、名称与其他分类重复、分类已有下级分组时修改项目类型、更新未写入任何行时抛出。</exception>
  public async Task<bool> UpdateMedicalStandardCategoryAsync(UpdateMedicalStandardCategoryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var updateMedicalStandardCategoryCommand = request.MapToUpdateMedicalStandardCategoryCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    updateMedicalStandardCategoryCommand.OperId = userId;
    updateMedicalStandardCategoryCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.UpdateMedicalStandardCategoryAsync(updateMedicalStandardCategoryCommand);
  }

  /// <summary>
  /// 启用已被停用的标准项目分类，使其重新参与有效目录。
  /// </summary>
  /// <param name="request">启用分类请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var enableMedicalStandardCategoryCommand = request.MapToEnableMedicalStandardCategoryCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    enableMedicalStandardCategoryCommand.OperId = userId;
    enableMedicalStandardCategoryCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.EnableMedicalStandardCategoryAsync(enableMedicalStandardCategoryCommand);
  }

  /// <summary>
  /// 停用标准项目分类，使其退出有效目录。
  /// </summary>
  /// <param name="request">停用分类请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var disableMedicalStandardCategoryCommand = request.MapToDisableMedicalStandardCategoryCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    disableMedicalStandardCategoryCommand.OperId = userId;
    disableMedicalStandardCategoryCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.DisableMedicalStandardCategoryAsync(disableMedicalStandardCategoryCommand);
  }

  /// <summary>
  /// 在指定分类下新建标准项目分组。
  /// </summary>
  /// <remarks>要求上级分类处于启用状态。</remarks>
  /// <param name="request">新建分组请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或上级分类不存在、上级分类已停用、该分类下已有同名分组、分组保存未写入任何行时抛出。</exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> CreateMedicalStandardGroupAsync(CreateMedicalStandardGroupRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var createMedicalStandardGroupCommand = request.MapToCreateMedicalStandardGroupCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMedicalStandardGroupCommand.OperId = userId;
    createMedicalStandardGroupCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.CreateMedicalStandardGroupAsync(createMedicalStandardGroupCommand);
  }

  /// <summary>
  /// 修改标准项目分组的名称与备注。
  /// </summary>
  /// <param name="request">修改分组请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组不存在、同一分类内已有同名分组、更新未写入任何行时抛出。</exception>
  public async Task<bool> UpdateMedicalStandardGroupAsync(UpdateMedicalStandardGroupRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var updateMedicalStandardGroupCommand = request.MapToUpdateMedicalStandardGroupCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    updateMedicalStandardGroupCommand.OperId = userId;
    updateMedicalStandardGroupCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.UpdateMedicalStandardGroupAsync(updateMedicalStandardGroupCommand);
  }

  /// <summary>
  /// 启用已被停用的标准项目分组。
  /// </summary>
  /// <remarks>启用后该分组及其下级标准项目重新参与有效目录。</remarks>
  /// <param name="request">启用分组请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var enableMedicalStandardGroupCommand = request.MapToEnableMedicalStandardGroupCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    enableMedicalStandardGroupCommand.OperId = userId;
    enableMedicalStandardGroupCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.EnableMedicalStandardGroupAsync(enableMedicalStandardGroupCommand);
  }

  /// <summary>
  /// 停用标准项目分组，使其退出有效目录。
  /// </summary>
  /// <param name="request">停用分组请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var disableMedicalStandardGroupCommand = request.MapToDisableMedicalStandardGroupCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    disableMedicalStandardGroupCommand.OperId = userId;
    disableMedicalStandardGroupCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.DisableMedicalStandardGroupAsync(disableMedicalStandardGroupCommand);
  }

  /// <summary>
  /// 在指定分类和分组下新建标准项目。
  /// </summary>
  /// <remarks>要求分类和分组均启用且分组属于该分类。</remarks>
  /// <param name="request">新建标准项目请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类或分组不存在、分类或分组任一处已停用、分组不属于所选分类、编码已被其他标准项目占用、保存未写入任何行时抛出。</exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> CreateMedicalStandardItemAsync(CreateMedicalStandardItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var createMedicalStandardItemCommand = request.MapToCreateMedicalStandardItemCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMedicalStandardItemCommand.OperId = userId;
    createMedicalStandardItemCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.CreateMedicalStandardItemAsync(createMedicalStandardItemCommand);
  }

  /// <summary>
  /// 用本次提交的文本整体覆盖标准项目的备注。
  /// </summary>
  /// <param name="request">修改备注请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目不存在、更新未写入任何行时抛出。</exception>
  public async Task<bool> ChangeMedicalStandardItemRemarkAsync(ChangeMedicalStandardItemRemarkRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var changeMedicalStandardItemRemarkCommand = request.MapToChangeMedicalStandardItemRemarkCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    changeMedicalStandardItemRemarkCommand.OperId = userId;
    changeMedicalStandardItemRemarkCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.ChangeMedicalStandardItemRemarkAsync(changeMedicalStandardItemRemarkCommand);
  }

  /// <summary>
  /// 启用已被停用的标准项目，使其重新参与有效目录匹配。
  /// </summary>
  /// <param name="request">启用标准项目请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> EnableMedicalStandardItemAsync(EnableMedicalStandardItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var enableMedicalStandardItemCommand = request.MapToEnableMedicalStandardItemCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    enableMedicalStandardItemCommand.OperId = userId;
    enableMedicalStandardItemCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.EnableMedicalStandardItemAsync(enableMedicalStandardItemCommand);
  }

  /// <summary>
  /// 停用标准项目。
  /// </summary>
  /// <remarks>停用后该项目不再参与有效目录匹配；数据保留、可重新启用。</remarks>
  /// <param name="request">停用标准项目请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或长度约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目不存在、状态更新未写入任何行时抛出。</exception>
  public async Task<bool> DisableMedicalStandardItemAsync(DisableMedicalStandardItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var disableMedicalStandardItemCommand = request.MapToDisableMedicalStandardItemCommand();
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    disableMedicalStandardItemCommand.OperId = userId;
    disableMedicalStandardItemCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.DisableMedicalStandardItemAsync(disableMedicalStandardItemCommand);
  }

  public async Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemRequest request)
  {
    var createMutualRecognitionItemCommand = request.MapToCreateMutualRecognitionItemCommand();
    if (Guid.TryParse(HttpRequestInfo?.UserId, out var userId)) createMutualRecognitionItemCommand.OperId = userId;
    createMutualRecognitionItemCommand.OperTime = DateTimeOffset.Now;
    return await manager.CreateMutualRecognitionItemAsync(createMutualRecognitionItemCommand);
  }

  public async Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationRequest request)
  {
    var updateMutualRecognitionItemConfigurationCommand = request.MapToUpdateMutualRecognitionItemConfigurationCommand();
    if (Guid.TryParse(HttpRequestInfo?.UserId, out var userId)) updateMutualRecognitionItemConfigurationCommand.OperId = userId;
    updateMutualRecognitionItemConfigurationCommand.OperTime = DateTimeOffset.Now;
    return await manager.UpdateMutualRecognitionItemConfigurationAsync(updateMutualRecognitionItemConfigurationCommand);
  }

  public async Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemRequest request)
  {
    var enableMutualRecognitionItemCommand = request.MapToEnableMutualRecognitionItemCommand();
    if (Guid.TryParse(HttpRequestInfo?.UserId, out var userId)) enableMutualRecognitionItemCommand.OperId = userId;
    enableMutualRecognitionItemCommand.OperTime = DateTimeOffset.Now;
    return await manager.EnableMutualRecognitionItemAsync(enableMutualRecognitionItemCommand);
  }

  public async Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemRequest request)
  {
    var disableMutualRecognitionItemCommand = request.MapToDisableMutualRecognitionItemCommand();
    if (Guid.TryParse(HttpRequestInfo?.UserId, out var userId)) disableMutualRecognitionItemCommand.OperId = userId;
    disableMutualRecognitionItemCommand.OperTime = DateTimeOffset.Now;
    return await manager.DisableMutualRecognitionItemAsync(disableMutualRecognitionItemCommand);
  }
}
