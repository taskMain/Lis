using System.ComponentModel.DataAnnotations;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Base.Application.Contracts.UserAggregate;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Queries.Ports;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录、互认项目配置与互认项目金额的写入口。
/// </summary>
/// <remarks>仅新建标准分组和新建标准项目声明显式事务；其余标准目录用例、四个互认项目配置用例与两个金额保存用例各只有一条写语句，不声明显式事务。</remarks>
public partial class MedicalRecognitionReportAppService : ApplicationService, IMedicalRecognitionReportAppService
{
  /// <summary>
  /// 标准目录、互认项目配置与金额的领域管理器。
  /// </summary>
  private readonly MedicalRecognitionReportManager manager;

  /// <summary>
  /// 互认项目配置写入口按标准项目编码解析服务端目录标识，以及金额保存前校验互认配置存在所需的仓储端口。
  /// </summary>
  private readonly IMedicalRecognitionReportRepository repository;

  /// <summary>
  /// 金额保存入口校验组织、医院、院区路径所用的解析点。
  /// </summary>
  /// <remarks>一次调用只解析一批目标路径，读取次数不随请求条数增长。</remarks>
  private readonly OrganizationPathResolver organizationPathResolver;

  /// <summary>
  /// 医院管理员金额保存入口解析可信组织与可信医院所用的解析点。
  /// </summary>
  /// <remarks>令牌已提供组织与医院两层时不读取用户档案；令牌缺失的层由当前登录用户档案补齐。</remarks>
  private readonly TrustedScopeResolver trustedScopeResolver;

  /// <summary>
  /// 报告 PDF 的本地文件存储端口：提交入口确认文件键可读，下载入口按键读取文件。
  /// </summary>
  private readonly IReportPdfFileStore reportPdfFileStore;

  /// <summary>
  /// 报告与报告版本的只读查询端口：下载入口按报告标识与版本标识读取报告身份与文件信息。
  /// </summary>
  private readonly IMedicalRecognitionReportQueryRepository queryReportRepository;

  /// <summary>
  /// 初始化写入口。
  /// </summary>
  /// <remarks>
  /// 保留执行目录业务规则校验、数据读写与事件登记的领域管理器，按编码读取标准目录的仓储端口，以及组织路径解析所用的外部组织服务；
  /// 可信范围解析所用的外部用户服务在构造函数内转交内部解析点。
  /// </remarks>
  /// <param name="manager">目录、互认配置、金额与报告采集领域管理器。</param>
  /// <param name="repository">标准目录读取、互认配置归属校验与保存前提校验使用的仓储端口。</param>
  /// <param name="organizationAppService">提供组织、医院与院区主数据的外部组织服务。</param>
  /// <param name="userAppService">提供当前登录用户组织与医院归属的外部用户服务，用于补齐令牌缺失的可信层。</param>
  /// <param name="reportPdfFileStore">报告 PDF 的本地文件存储端口。</param>
  /// <param name="queryReportRepository">报告与报告版本的只读查询端口。</param>
  public MedicalRecognitionReportAppService(
    MedicalRecognitionReportManager manager,
    IMedicalRecognitionReportRepository repository,
    IOrganizationAppService organizationAppService,
    IUserAppService userAppService,
    IReportPdfFileStore reportPdfFileStore,
    IMedicalRecognitionReportQueryRepository queryReportRepository)
  {
    this.manager = manager;
    this.repository = repository;
    organizationPathResolver = new OrganizationPathResolver(organizationAppService);
    trustedScopeResolver = new TrustedScopeResolver(userAppService);
    this.reportPdfFileStore = reportPdfFileStore;
    this.queryReportRepository = queryReportRepository;
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

  /// <summary>
  /// 新增互认项目配置。
  /// </summary>
  /// <remarks>
  /// 组织编码取自框架认证上下文（登录令牌的组织声明，经 <c>HttpRequestInfo.OrgId</c> 读取），
  /// 操作人取自登录令牌的用户标识（经 <c>HttpRequestInfo.UserId</c> 读取），两者都由应用层写入命令；
  /// 调用方只提交标准项目编码，服务端按该编码读取标准项目并取其标识保存，编码读不到标准项目即拒绝；
  /// 创建后的配置即为启用状态，调用方不能提交启用状态，标准项目、所属分类与所属分组必须当前有效。
  /// </remarks>
  /// <param name="request">新增互认项目配置请求。</param>
  /// <returns>是否创建成功。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或取值约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码、用户标识不能解析为非空 Guid，或按请求提交的编码读不到标准项目时抛出；
  /// 标准项目、所属分类或所属分组不存在或已停用时，该组织已配置此标准项目时，以及新增配置影响的行数不为 1（含 0 行）时同样抛出。
  /// 并发下两个请求可能同时通过查重，此时由唯一索引拒绝，数据库异常不翻译为业务拒绝。
  /// 以上情况都没有写入配置，也没有登记创建事件。
  /// </exception>
  public async Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var createMutualRecognitionItemCommand = request.MapToCreateMutualRecognitionItemCommand();
    createMutualRecognitionItemCommand.OrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMutualRecognitionItemCommand.OperId = userId;
    createMutualRecognitionItemCommand.OperTime = DateTimeOffset.UtcNow;
    MedicalStandardItem standardItem = await repository.GetMedicalStandardItemByCodeAsync(request.StandardProjectCode)
      ?? throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    createMutualRecognitionItemCommand.StandardItemId = standardItem.Id;
    return await manager.CreateMutualRecognitionItemAsync(createMutualRecognitionItemCommand);
  }

  /// <summary>
  /// 修改互认项目配置的可互认时间天数。
  /// </summary>
  /// <remarks>组织编码取自框架认证上下文（登录令牌的组织声明，经 <c>HttpRequestInfo.OrgId</c> 读取），用于按组织范围定位配置；配置停用时仍允许修改可互认时间。</remarks>
  /// <param name="request">修改可互认时间请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或取值约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，配置不存在或不属于当前可信组织（两者同一拒绝口径），
  /// 或更新影响的行数不为 1（含 0 行）时抛出，此时不登记修改事件。
  /// </exception>
  public async Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var updateMutualRecognitionItemConfigurationCommand = request.MapToUpdateMutualRecognitionItemConfigurationCommand();
    updateMutualRecognitionItemConfigurationCommand.OrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    updateMutualRecognitionItemConfigurationCommand.OperId = userId;
    updateMutualRecognitionItemConfigurationCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.UpdateMutualRecognitionItemConfigurationAsync(updateMutualRecognitionItemConfigurationCommand);
  }

  /// <summary>
  /// 启用已被停用的互认项目配置。
  /// </summary>
  /// <remarks>组织编码取自框架认证上下文（登录令牌的组织声明，经 <c>HttpRequestInfo.OrgId</c> 读取），用于按组织范围定位配置；重复启用为幂等成功且不登记事件。</remarks>
  /// <param name="request">启用互认项目配置请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或取值约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，配置不存在或不属于当前可信组织，
  /// 或标准项目、所属分类或所属分组不存在或已停用时抛出；
  /// 条件更新影响 0 行时按同一可信组织复读一次配置：复读已是启用状态按幂等成功返回、不抛出，复读仍是停用按并发冲突拒绝，复读读不到配置按配置不存在拒绝；
  /// 条件更新影响多行时同样抛出。
  /// </exception>
  public async Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var enableMutualRecognitionItemCommand = request.MapToEnableMutualRecognitionItemCommand();
    enableMutualRecognitionItemCommand.OrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    enableMutualRecognitionItemCommand.OperId = userId;
    enableMutualRecognitionItemCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.EnableMutualRecognitionItemAsync(enableMutualRecognitionItemCommand);
  }

  /// <summary>
  /// 停用互认项目配置。
  /// </summary>
  /// <remarks>组织编码取自框架认证上下文（登录令牌的组织声明，经 <c>HttpRequestInfo.OrgId</c> 读取），用于按组织范围定位配置；重复停用为幂等成功且不登记事件，停用不校验标准目录当前有效性。</remarks>
  /// <param name="request">停用互认项目配置请求。</param>
  /// <returns>保存成功返回 true。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或取值约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，或配置不存在、不属于当前可信组织时抛出；
  /// 条件更新影响 0 行时按同一可信组织复读一次配置：复读已是停用状态按幂等成功返回、不抛出，复读仍是启用按并发冲突拒绝，复读读不到配置按配置不存在拒绝；
  /// 条件更新影响多行时同样抛出。停用不校验标准目录当前有效性。
  /// </exception>
  public async Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var disableMutualRecognitionItemCommand = request.MapToDisableMutualRecognitionItemCommand();
    disableMutualRecognitionItemCommand.OrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    disableMutualRecognitionItemCommand.OperId = userId;
    disableMutualRecognitionItemCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.DisableMutualRecognitionItemAsync(disableMutualRecognitionItemCommand);
  }

  /// <summary>
  /// 保存组织医院院区互认项目金额（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 组织、医院与院区三个值按请求使用，服务端校验三者存在、启用且父子归属正确，不要求请求组织等于登录令牌的组织声明；
  /// 互认配置存在性按解析去空白后的组织编码与请求标准项目编码校验，未建立即拒绝；
  /// 操作人与操作时间由服务端写入，标准目录或互认配置停用都不阻断金额维护。
  /// 三个请求值可能带首尾空白，去空白由组织路径解析点统一完成，写入命令使用解析结果。
  /// 金额在契约上可空并声明必填，缺省由公共请求校验拒绝；校验通过后按已确定非空的值显式写入命令，不依赖自动映射。
  /// </remarks>
  /// <param name="request">保存金额请求，携带组织、医院、院区、标准项目编码与本次金额。</param>
  /// <returns>是否保存成功。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的用户标识不能解析为非空 Guid，或请求的组织、医院、院区不存在、已停用、父子归属不匹配时抛出；
  /// 当前组织未建立该标准项目的互认配置、金额为负数或超过两位小数、并发首次保存命中唯一约束、
  /// 条件更新影响 0 行且复读无法解释为成功或影响行数大于 1 时同样抛出，这些情况都不登记保存事件。
  /// </exception>
  public async Task<bool> SaveOrganizationHospitalBranchRecognitionAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    SaveOrganizationHospitalBranchRecognitionAmountCommand command = request.MapToSaveOrganizationHospitalBranchRecognitionAmountCommand();
    // 金额由两个入口在公共请求校验通过后显式写入，因此请求侧金额不参与自动映射（见 MedicalRecognitionReportDataMaps.cs）：
    // 自动映射对可空金额的缺省值静默写入 0，会把"未提交金额"变成一次真实的零元写入。
    command.CurrentAmount = request.CurrentAmount!.Value;
    // 平台管理员入口按请求的组织、医院、院区执行，跨组织同样放行：权限由权限系统负责，不在服务端做组织相等校验。
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(request.OrganizationCode, request.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
    return await SaveAmountAsync(command, path);
  }

  /// <summary>
  /// 保存本院区互认项目金额（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 组织与医院只取自可信上下文，请求不提交也不得覆盖；可信上下文的组织层与医院层按同一规则解析：
  /// 登录令牌该层非空白即取令牌值，令牌该层缺失或空白时用当前登录用户档案的 <c>OrgId</c>/<c>HosId</c> 补齐，
  /// 令牌与用户档案都提供该层且不一致即拒绝，补齐后该层仍为空即拒绝，不使用默认值、不降级为空值；
  /// 请求院区必须存在、启用且属于可信医院与可信组织，否则拒绝，院区层不参与可信回落；
  /// 其余校验与写入与平台管理员入口完全相同，两个入口映射到同一个命令与同一个领域方法。
  /// </remarks>
  /// <param name="request">保存金额请求，只携带院区、标准项目编码与本次金额。</param>
  /// <returns>是否保存成功。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信上下文的组织或医院取不到时抛出，不使用默认值、不降级为空值；
  /// 用户标识不能解析为非空 Guid，或请求院区不存在、已停用、不属于可信医院时同样抛出；
  /// 当前组织未建立该标准项目的互认配置、金额为负数或超过两位小数、并发首次保存命中唯一约束、
  /// 条件更新影响 0 行且复读无法解释为成功或影响行数大于 1 时同样抛出，这些情况都不登记保存事件。
  /// </exception>
  public async Task<bool> SaveBranchRecognitionAmountAsync(SaveBranchRecognitionAmountRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    SaveOrganizationHospitalBranchRecognitionAmountCommand command = request.MapToSaveOrganizationHospitalBranchRecognitionAmountCommand();
    // 金额来源与写入理由与平台管理员入口相同：自动映射会在金额缺省时静默写入 0，因此该字段不参与自动映射。
    command.CurrentAmount = request.CurrentAmount!.Value;
    command.OrganizationCode = trustedScope.OrganizationCode;
    command.HospitalCode = trustedScope.HospitalCode;
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
    return await SaveAmountAsync(command, path);
  }

  /// <summary>
  /// 用已校验的组织路径补齐金额保存命令的业务键与操作字段，并交给领域管理器保存。
  /// </summary>
  /// <remarks>两个入口的差异只体现在组织路径与命令字段的来源，解析完成后交给领域层的数据完全相同。</remarks>
  /// <param name="command">已携带标准项目编码、非空金额与请求取值的保存命令。</param>
  /// <param name="path">解析校验通过的组织路径，携带去空白后的三层业务编码。</param>
  /// <returns>是否保存成功。</returns>
  /// <exception cref="InvalidOperationException">用户标识不能解析为非空 Guid 时抛出；其余业务拒绝由领域管理器抛出。</exception>
  private async Task<bool> SaveAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountCommand command, OrganizationPath path)
  {
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    command.OrganizationCode = path.OrganizationCode;
    command.HospitalCode = path.HospitalCode;
    command.BranchCode = path.BranchCode;
    command.OperId = userId;
    command.OperTime = DateTimeOffset.UtcNow;
    return await manager.SaveOrganizationHospitalBranchRecognitionAmountAsync(command);
  }

  /// <summary>
  /// 解析报告采集链路的三层可信归属与本次请求的接收时间。
  /// </summary>
  /// <remarks>
  /// 组织、医院与院区三层都来自可信上下文：登录令牌该层非空白即取令牌值，令牌缺失该层时用当前登录用户档案的归属补齐，
  /// 令牌与档案冲突或补齐后仍为空即拒绝。院区不从请求取值，因此调用方不能扩大可信范围；
  /// 三层都必须存在、启用且父子归属正确。本次请求的接收时间由服务端取，用于校验业务时间与作废时间不晚于平台接收时点。
  /// </remarks>
  /// <returns>三层可信归属与本次请求的接收时间。</returns>
  /// <exception cref="InvalidOperationException">可信组织、医院或院区不可解析，或组织、医院、院区不存在、已停用、父子归属不匹配时抛出。</exception>
  private async Task<(OrganizationPath Path, DateTime ReceivedTime)> ResolveReportScopeAsync()
  {
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");

    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, trustedScope.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    return (path, DateTime.Now);
  }
}
