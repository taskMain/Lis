using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Http;
using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application.OrganizationScope;
using Dy.MedicalRecognition.Application.PlatformParameter;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;
using Dy.MedicalRecognition.Domain.Queries.Ports;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录、互认项目配置与互认项目金额的写入口。
/// </summary>
/// <remarks>新建标准分组、新建标准项目与批量新建互认项目配置声明显式事务；其余标准目录用例、其余四个互认项目配置用例与两个金额保存用例各只有一条写语句，不声明显式事务。</remarks>
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
  /// 报告链路解析当前工作身份的组织范围所用的解析点。
  /// </summary>
  /// <remarks>三层组织范围只取自当前工作身份的组织祖先链，调用方不能通过请求扩大范围；业务列只写组织业务编码。</remarks>
  private readonly IamReportScopeResolver reportScopeResolver;

  /// <summary>
  /// 已验证的 IAM 当前上下文，提供报告链路的操作人标识。
  /// </summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>
  /// 报告 PDF 的本地文件存储端口：提交入口确认文件键可读，下载入口按键读取文件。
  /// </summary>
  private readonly IReportPdfFileStore reportPdfFileStore;

  /// <summary>
  /// 报告与报告版本的只读查询端口：下载入口与互认匹配查询按报告标识与版本标识读取报告身份、文件信息与匹配响应内容。
  /// </summary>
  private readonly IMedicalRecognitionReportQueryRepository queryReportRepository;

  /// <summary>
  /// 平台参数读取入口：互认匹配查询入口读取本院报告匹配开关与排除时长。
  /// </summary>
  /// <remarks>每次读取当前参数值；参数不存在、停用或取值非法都由入口就地判定为整次失败。</remarks>
  private readonly PlatformParameterReader platformParameterReader;

  /// <summary>
  /// 初始化写入口。
  /// </summary>
  /// <remarks>
  /// 保留执行目录业务规则校验、数据读写与事件登记的领域管理器，按编码读取标准目录的仓储端口，以及组织路径解析所用的外部组织服务；
  /// 可信范围解析所用的外部用户服务在构造函数内转交内部解析点。
  /// </remarks>
  /// <param name="manager">目录、互认配置、金额与报告采集领域管理器。</param>
  /// <param name="repository">标准目录读取、互认配置归属校验与保存前提校验使用的仓储端口。</param>
  /// <param name="reportPdfFileStore">报告 PDF 的本地文件存储端口。</param>
  /// <param name="queryReportRepository">报告与报告版本的只读查询端口。</param>
  /// <param name="platformParameterRepository">平台参数持久化入口，读取本院报告匹配开关与排除时长。</param>
  /// <param name="directoryClient">IAM 共享目录客户端，提供当前工作身份组织链的详情与类型。</param>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文，提供当前工作身份与租户。</param>
  /// <param name="businessCodeQueryClient">IAM 组织业务编码查询客户端，按租户读取组织树节点的业务编码。</param>
  public MedicalRecognitionReportAppService(
    MedicalRecognitionReportManager manager,
    IMedicalRecognitionReportRepository repository,
    IReportPdfFileStore reportPdfFileStore,
    IMedicalRecognitionReportQueryRepository queryReportRepository,
    IPlatformParameterRepository platformParameterRepository,
    IIamDirectoryClient directoryClient,
    ICurrentIamContext currentIamContext,
    IOrganizationBusinessCodeQueryClient businessCodeQueryClient)
  {
    this.manager = manager;
    this.repository = repository;
    IamOrganizationDirectoryReader iamDirectoryReader = new(directoryClient);
    IamOrganizationBusinessCodeReader businessCodeReader = new(businessCodeQueryClient);
    organizationPathResolver = new OrganizationPathResolver(iamDirectoryReader, businessCodeReader, currentIamContext);
    reportScopeResolver = new IamReportScopeResolver(
      new IamOrganizationScopeResolver(new IamOrganizationDirectoryReader(directoryClient), currentIamContext, businessCodeReader));
    this.currentIamContext = currentIamContext;
    this.reportPdfFileStore = reportPdfFileStore;
    this.queryReportRepository = queryReportRepository;
    platformParameterReader = new PlatformParameterReader(platformParameterRepository);
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
    createMutualRecognitionItemCommand.OrganizationCode = await ResolveTrustedOrganizationCodeAsync("无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMutualRecognitionItemCommand.OperId = userId;
    createMutualRecognitionItemCommand.OperTime = DateTimeOffset.UtcNow;
    MedicalStandardItem standardItem = await repository.GetMedicalStandardItemByCodeAsync(request.StandardProjectCode)
      ?? throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    createMutualRecognitionItemCommand.StandardItemId = standardItem.Id;
    return await manager.CreateMutualRecognitionItemAsync(createMutualRecognitionItemCommand);
  }

  /// <summary>
  /// 批量新建互认项目配置。
  /// </summary>
  /// <remarks>
  /// 组织编码取自框架认证上下文（登录令牌的组织声明，经 <c>HttpRequestInfo.OrgId</c> 读取），
  /// 操作人取自登录令牌的用户标识（经 <c>HttpRequestInfo.UserId</c> 读取），两者都由应用层写入命令；
  /// 调用方只提交标准项目编码集合与批次上的可互认时间，服务端不读取目录：标准项目标识的解析、编码集合口径与
  /// 目录三层有效性判定都在领域层按编码集合一次读取完成，调用方不能提交内部标识或逐项天数；
  /// 本次提交的全部配置整批建立，任一编码不合规即整批拒绝。
  /// 本入口是事务边界：整批配置逐条写入，任一条写入失败时前面已写入的配置必须一并回滚，
  /// 因此声明显式事务；事务由框架的工作单元机制管理，应用服务不手工开启、提交或回滚。
  /// </remarks>
  /// <param name="request">批量新建互认项目配置请求。</param>
  /// <returns>是否整批创建成功。</returns>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或取值约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码、用户标识不能解析为非空 Guid 时抛出；
  /// 编码集合为空、单批超过上限、含空白编码或批次内编码重复、编码不存在或目录三层已停用、
  /// 该组织已配置集合中的标准项目，以及写入影响的行数合计不等于提交条数时同样抛出，此时不登记批量创建事件。
  /// 并发下两个请求可能同时通过查重，此时由唯一索引拒绝，数据库异常按原样向外传播、不翻译为业务拒绝。
  /// </exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> CreateMutualRecognitionItemsAsync(CreateMutualRecognitionItemsRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    var createMutualRecognitionItemsCommand = request.MapToCreateMutualRecognitionItemsCommand();
    createMutualRecognitionItemsCommand.OrganizationCode = await ResolveTrustedOrganizationCodeAsync("无法确定当前可信组织。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    createMutualRecognitionItemsCommand.OperId = userId;
    createMutualRecognitionItemsCommand.OperTime = DateTimeOffset.UtcNow;
    return await manager.CreateMutualRecognitionItemsAsync(createMutualRecognitionItemsCommand);
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
    updateMutualRecognitionItemConfigurationCommand.OrganizationCode = await ResolveTrustedOrganizationCodeAsync("无法确定当前可信组织。");
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
    enableMutualRecognitionItemCommand.OrganizationCode = await ResolveTrustedOrganizationCodeAsync("无法确定当前可信组织。");
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
    disableMutualRecognitionItemCommand.OrganizationCode = await ResolveTrustedOrganizationCodeAsync("无法确定当前可信组织。");
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
    // 平台管理员入口按请求的组织、医院、院区业务编码校验归属，跨组织同样放行：权限由权限系统负责，不在服务端做组织相等校验。
    // 提交值是业务编码，编码到实例标识的解析、存在性、启用与父子归属校验都在组织路径解析点内完成，不在入口内做编码格式解析。
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
  /// 组织与医院只取自可信上下文，请求不提交也不得覆盖：组织层只取 IAM 上下文的组织标识，医院层与院区层由该组织
  /// 沿组织祖先链按组织类型编码归类得到；该层缺失或同一类型出现多个祖先即拒绝，不使用默认值、不降级为空值；
  /// 请求院区必须存在、启用且属于可信医院与可信组织，否则拒绝，院区不从请求值扩大可信范围；
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
    IamOrganizationScope trustedScope = await reportScopeResolver.ReadRequiredOrganizationHospitalAsync(
      "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    SaveOrganizationHospitalBranchRecognitionAmountCommand command = request.MapToSaveOrganizationHospitalBranchRecognitionAmountCommand();
    // 金额来源与写入理由与平台管理员入口相同：自动映射会在金额缺省时静默写入 0，因此该字段不参与自动映射。
    command.CurrentAmount = request.CurrentAmount!.Value;
    // 可信组织与医院取自已解析的可信范围（编码与实例标识同源），请求院区按业务编码参与同一路径校验：
    // 院区编码不是标识，不在入口内做格式解析，院区是否存在、启用并属于可信医院由组织路径解析点判定。
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, request.BranchCode)],
      "无法确定当前可信组织。",
      "无法确定当前可信医院。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
    return await SaveAmountAsync(command, path);
  }

  /// <summary>
  /// 用已校验的组织路径补齐金额保存命令的业务键与操作字段，并交给领域管理器保存。
  /// </summary>
  /// <remarks>两个入口的差异只体现在组织路径与命令字段的来源，解析完成后交给领域层的数据完全相同。</remarks>
  /// <param name="command">已携带标准项目编码、非空金额与请求取值的保存命令。</param>
  /// <param name="path">解析校验通过的组织路径，同时携带三层实例标识与去空白后的三层业务编码。</param>
  /// <returns>是否保存成功。</returns>
  /// <exception cref="InvalidOperationException">用户标识不能解析为非空 Guid 时抛出；其余业务拒绝由领域管理器抛出。</exception>
  private async Task<bool> SaveAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountCommand command, OrganizationPath path)
  {
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out var userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    // 编码列写业务编码：编码取自 IAM 组织节点，未配置编码时为空串，不用实例标识替代。
    command.OrganizationCode = path.OrganizationCode;
    command.HospitalCode = path.HospitalCode;
    command.BranchCode = path.BranchCode;
    command.OperId = userId;
    command.OperTime = DateTimeOffset.UtcNow;
    return await manager.SaveOrganizationHospitalBranchRecognitionAmountAsync(command);
  }

  /// <summary>
  /// 读取当前工作身份的组织层业务编码，供只按组织归属的业务（互认项目配置）写入编码列。
  /// </summary>
  /// <remarks>
  /// 组织层只取自当前工作身份的组织祖先链，不接受调用方提交；取 IAM 组织节点上配置的业务编码，
  /// 组织层缺失或该节点未配置业务编码时按入口文案拒绝，不返回空串写入业务列。
  /// </remarks>
  /// <param name="missingOrganizationMessage">组织层缺失或未配置业务编码时对外抛出的业务拒绝文案。</param>
  /// <returns>组织业务编码。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">组织层缺失或该节点未配置业务编码时抛出。</exception>
  private async Task<string> ResolveTrustedOrganizationCodeAsync(string missingOrganizationMessage)
  {
    IamOrganizationScope scope = await reportScopeResolver.ReadRequiredOrganizationAsync(missingOrganizationMessage);
    return string.IsNullOrWhiteSpace(scope.OrganizationCode)
      ? throw new InvalidOperationException(missingOrganizationMessage)
      : scope.OrganizationCode;
  }

  /// <summary>
  /// 解析报告采集链路的当前工作身份三层组织范围与本次请求的接收时间。
  /// </summary>
  /// <remarks>
  /// 组织、医院与院区三层都取自当前工作身份的组织祖先链：按组织类型归类为医共体、医院与院区，
  /// 任一层缺失、节点已停用或同一类型出现多个祖先即按该层文案拒绝，不使用默认值、不按名称或层级深度推断。
  /// 院区不从请求取值，因此调用方不能扩大可信范围。本次请求的接收时间由服务端取，用于校验业务时间与作废时间不晚于平台接收时点。
  /// </remarks>
  /// <returns>三层组织实例标识与本次请求的接收时间。</returns>
  /// <exception cref="IamAuthenticationException">请求未携带有效 IAM 凭据时抛出。</exception>
  /// <exception cref="IamWorkIdentityRequiredException">请求未选择工作身份时抛出。</exception>
  /// <exception cref="InvalidOperationException">任一层缺失或同一类型出现多个祖先时抛出。</exception>
  private async Task<(IamOrganizationScope Scope, DateTime ReceivedTime)> ResolveReportScopeAsync()
  {
    IamOrganizationScope scope = await reportScopeResolver.ReadRequiredScopeAsync(
      "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");

    return (scope, DateTime.Now);
  }
}
