using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Base.Application.Contracts.UserAggregate;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 互认项目金额保存写入的用例与领域行为校验：首次保存新建、覆盖不累加、零元算已配置、
/// 金额非负与最多两位小数的领域拒绝、保存前提为互认配置存在、同值重复保存仍登记事件、
/// 影响行数判定（0 行复读三分支与多行不变量）、
/// 两个入口的取值来源与院区归属、事件字段与失败不登记事件，以及契约字段与实体语义修正。
/// </summary>
/// <remarks>
/// 对应阶段 3 验证矩阵 V1、V2、V3、V4、V5、V6、V7、V11、V12、V13、V14、V15、V16、V25、V26、V28
/// 中不依赖真实数据库的部分，以及两个写入口"最多一条写语句、不声明工作单元"的静态约定。
/// 真实数据库上的唯一索引冲突结果、并发交错与物理表运行证据属于阶段 3 的运行面，不在本文件验证。
/// 本文件使用手写的最小测试替身：内存金额与互认配置数据、可脚本化的影响行数与并发复读结果、
/// 可观察的领域事件队列，以及按需失败的外部组织服务替身；金额读取不在替身中脚本化，
/// 因为"读取不到记录"本身就是并发首插用例所需的真实行为。
/// </remarks>
public sealed class Stage3WritePathTests
{
  /// <summary>可信组织编码，代表登录令牌携带的当前组织。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>另一个组织编码，用于验证平台管理员入口按请求组织保存。</summary>
  private const string OtherOrganization = "ORG-B";

  /// <summary>可信医院编码，代表登录令牌携带的当前医院。</summary>
  private const string TrustedHospital = "HOS-1";

  /// <summary>另一家医院编码，用于构造越权院区。</summary>
  private const string OtherHospital = "HOS-2";

  /// <summary>可信医院下的院区编码。</summary>
  private const string TrustedBranch = "BRH-1";

  /// <summary>另一家医院下的院区编码，用于构造请求院区不属于可信医院的场景。</summary>
  private const string OtherHospitalBranch = "BRH-2";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("3a1c7e42-8d55-4c6b-9f21-0b7d5e6a1c34");

  /// <summary>用例使用的标准项目编码。</summary>
  private const string ProjectCode = "P1";

  /// <summary>未建立互认配置的标准项目编码。</summary>
  private const string UnconfiguredProjectCode = "P2";

  /// <summary>首次保存使用的金额。</summary>
  private const decimal FirstAmount = 125.50m;

  /// <summary>覆盖保存使用的金额，与首次保存值不同且大于首次值，用于验证不累加。</summary>
  private const decimal SecondAmount = 300.25m;

  /// <summary>领域层保存金额的业务拒绝文案；负数与超过两位小数共用同一拒绝口径。</summary>
  private const string InvalidAmountMessage = "业务拒绝：金额不得小于零且最多两位小数。";

  /// <summary>互认配置不存在时的业务拒绝文案。</summary>
  private const string MissingConfigurationMessage = "业务拒绝：当前组织未建立该标准项目的互认项目配置。";

  /// <summary>并发冲突无法由复读解释时的业务拒绝文案。</summary>
  private const string ConcurrentConflictMessage = "业务拒绝：金额已被其他操作变更，请刷新后重试。";

  /// <summary>影响行数大于 1 时按持久化不变量异常拒绝，不返回成功。</summary>
  private const string InvariantMessage = "业务拒绝：金额保存影响的行数异常，未完成保存。";

  /// <summary>Command 携带的操作时间，用于核对事件时间来源于命令而不是数据库当前时间。</summary>
  private static readonly DateTimeOffset CommandOperTime = new(2026, 10, 8, 9, 15, 0, TimeSpan.Zero);

  /// <summary>构建写入口：注入领域管理器、仓储端口、外部组织服务与外部用户服务。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <param name="organizationService">提供组织、医院与院区路径的外部组织服务替身。</param>
  /// <returns>可直接调用的写入口。</returns>
  private static MedicalRecognitionReportAppService CreateAppService(FakeReportRepository repository, FakeOrganizationAppService organizationService)
    => new(new MedicalRecognitionReportManager(repository), repository, organizationService, new StubUserAppService(),
      new StubReportPdfFileStore(), new StubReportQueryRepository());

  /// <summary>构建写入口，并指定补齐令牌缺失层所用的外部用户服务替身。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <param name="organizationService">提供组织、医院与院区路径的外部组织服务替身。</param>
  /// <param name="userService">提供当前登录用户档案的外部用户服务替身。</param>
  /// <returns>可直接调用的写入口。</returns>
  private static MedicalRecognitionReportAppService CreateAppService(
    FakeReportRepository repository, FakeOrganizationAppService organizationService, StubUserAppService userService)
    => new(new MedicalRecognitionReportManager(repository), repository, organizationService, userService,
      new StubReportPdfFileStore(), new StubReportQueryRepository());

  /// <summary>构建领域管理器，用于绕过应用层直接校验领域规则。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <returns>可直接调用的领域管理器。</returns>
  private static MedicalRecognitionReportManager CreateManager(FakeReportRepository repository) => new(repository);

  /// <summary>
  /// 构造合法前置数据：启用的组织、医院、院区，以及该组织下已建立的互认项目配置。
  /// </summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <returns>已装载组织路径与互认配置的组织服务替身。</returns>
  private static FakeOrganizationAppService CreateConfiguredOrganization(FakeReportRepository repository)
  {
    AddMutualRecognitionConfiguration(repository, TrustedOrganization, ProjectCode, isValid: true);
    return BuildOrganizationService();
  }

  /// <summary>构造只含启用的组织、可信医院与另一家医院的路径替身。</summary>
  /// <returns>外部组织服务替身。</returns>
  private static FakeOrganizationAppService BuildOrganizationService() => new()
  {
    Organizations = [new OrganizationDto { Id = TrustedOrganization, Name = "示例组织", IsValid = true }],
    HospitalsByOrganization =
    {
      [TrustedOrganization] =
      [
        new HospitalDto { Id = TrustedHospital, Name = "示例医院", OrgId = TrustedOrganization, IsValid = true },
        new HospitalDto { Id = OtherHospital, Name = "另一家医院", OrgId = TrustedOrganization, IsValid = true }
      ]
    },
    BranchesByHospital =
    {
      [TrustedHospital] = [new BranchDto { Id = TrustedBranch, Name = "示例院区", HosId = TrustedHospital, OrgId = TrustedOrganization, IsValid = true }],
      [OtherHospital] = [new BranchDto { Id = OtherHospitalBranch, Name = "另一家医院院区", HosId = OtherHospital, OrgId = TrustedOrganization, IsValid = true }]
    }
  };

  /// <summary>向仓储替身登记一条已存在的互认项目配置。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <param name="organizationCode">配置归属组织编码。</param>
  /// <param name="standardProjectCode">配置的标准项目编码。</param>
  /// <param name="isValid">配置当前启用状态。</param>
  private static void AddMutualRecognitionConfiguration(FakeReportRepository repository, string organizationCode, string standardProjectCode, bool isValid)
  {
    repository.Configurations[(organizationCode, standardProjectCode)] = new MutualRecognitionItem
    {
      Id = Guid.NewGuid(),
      OrganizationCode = organizationCode,
      StandardProjectCode = standardProjectCode,
      IsValid = isValid,
      OperId = Guid.Empty,
      OperTime = CommandOperTime
    };
  }

  /// <summary>向仓储替身直接放置一条已存在的金额记录（模拟此前已保存过的数据）。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <param name="amount">已存在的当前金额。</param>
  private static void AddSavedAmount(FakeReportRepository repository, decimal amount) =>
    repository.Amounts[BusinessKey(TrustedOrganization, TrustedHospital, TrustedBranch, ProjectCode)] = new OrganizationHospitalBranchRecognitionAmount
    {
      Id = Guid.NewGuid(),
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      StandardProjectCode = ProjectCode,
      CurrentAmount = amount,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };

  /// <summary>业务键：四个业务键列构成的读取与写入定位组合。</summary>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>四元业务键。</returns>
  private static (string OrganizationCode, string HospitalCode, string BranchCode, string StandardProjectCode) BusinessKey(
    string organizationCode, string hospitalCode, string branchCode, string standardProjectCode) =>
    (organizationCode, hospitalCode, branchCode, standardProjectCode);

  /// <summary>构建平台管理员入口请求。</summary>
  /// <param name="organizationCode">请求组织编码。</param>
  /// <param name="hospitalCode">请求医院编码。</param>
  /// <param name="branchCode">请求院区编码。</param>
  /// <param name="standardProjectCode">请求标准项目编码。</param>
  /// <param name="currentAmount">请求金额。</param>
  /// <returns>可直接交给写入口的请求。</returns>
  private static SaveOrganizationHospitalBranchRecognitionAmountRequest PlatformRequest(
    string organizationCode = TrustedOrganization,
    string hospitalCode = TrustedHospital,
    string branchCode = TrustedBranch,
    string standardProjectCode = ProjectCode,
    decimal currentAmount = FirstAmount) => new()
    {
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode,
      CurrentAmount = currentAmount
    };

  /// <summary>构建医院管理员入口请求；该请求不含组织与医院。</summary>
  /// <param name="standardProjectCode">请求标准项目编码。</param>
  /// <param name="currentAmount">请求金额。</param>
  /// <param name="branchCode">请求院区编码。</param>
  /// <returns>可直接交给写入口的请求。</returns>
  private static SaveBranchRecognitionAmountRequest BranchRequest(
    string standardProjectCode = ProjectCode, decimal currentAmount = FirstAmount, string branchCode = TrustedBranch) => new()
    {
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode,
      CurrentAmount = currentAmount
    };

  /// <summary>把当前请求的可信上下文设置为可信组织、可信医院与可信院区。</summary>
  /// <param name="organizationCode">令牌中的组织编码。</param>
  /// <param name="hospitalCode">令牌中的医院编码。</param>
  private static void UseTrustedContext(string organizationCode = TrustedOrganization, string hospitalCode = TrustedHospital) =>
    TrustedRequestContext.Use(organizationCode, hospitalCode, TrustedBranch, TrustedOperId.ToString());

  /// <summary>执行平台管理员保存，并捕获登记的领域事件。</summary>
  /// <param name="appService">待调用的写入口。</param>
  /// <param name="request">保存请求。</param>
  /// <returns>捕获到的事件集合与保存返回值。</returns>
  private static Task<(RecordingEventQueue Events, bool Result)> SavePlatformAsync(
    MedicalRecognitionReportAppService appService, SaveOrganizationHospitalBranchRecognitionAmountRequest request) =>
    ExecuteAsync(() => appService.SaveOrganizationHospitalBranchRecognitionAmountAsync(request));

  /// <summary>执行医院管理员保存，并捕获登记的领域事件。</summary>
  /// <param name="appService">待调用的写入口。</param>
  /// <param name="request">保存请求。</param>
  /// <returns>捕获到的事件集合与保存返回值。</returns>
  private static Task<(RecordingEventQueue Events, bool Result)> SaveBranchAsync(
    MedicalRecognitionReportAppService appService, SaveBranchRecognitionAmountRequest request) =>
    ExecuteAsync(() => appService.SaveBranchRecognitionAmountAsync(request));

  /// <summary>执行预期被业务拒绝的保存，并捕获登记的领域事件。</summary>
  /// <param name="operation">预期抛业务拒绝的保存操作。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static Task<(RecordingEventQueue Events, InvalidOperationException Error)> SaveExpectingRejectionAsync(Func<Task<bool>> operation) =>
    ExecuteExpectingRejectionAsync(operation);

  /// <summary>断言仓储替身上没有任何写语句被执行，且没有登记事件。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  /// <param name="events">捕获到的事件集合。</param>
  private static void AssertNothingWritten(FakeReportRepository repository, RecordingEventQueue events)
  {
    Assert.Equal(0, repository.WriteCalls);
    Assert.Empty(repository.Amounts);
    Assert.Empty(events.Events);
  }

  /// <summary>驱动更新影响 0 行、复读金额已等于本次提交值的并发场景。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  private static void ScriptConcurrentSameValueUpdate(FakeReportRepository repository)
  {
    repository.ScriptedWriteRows.Enqueue(0);
    repository.ConcurrentAmountApplied = SecondAmount;
  }

  /// <summary>驱动更新影响 0 行、且复读结果与本次提交值不同的并发场景。</summary>
  /// <param name="repository">承载金额与互认配置数据的最小仓储替身。</param>
  private static void ScriptConcurrentConflictingUpdate(FakeReportRepository repository)
  {
    repository.ScriptedWriteRows.Enqueue(0);
    repository.ConcurrentAmountApplied = 999.99m;
  }

  /// <summary>在捕获领域事件的前提下执行写操作。</summary>
  /// <param name="operation">待执行的写操作。</param>
  /// <returns>捕获到的事件集合与写操作返回值。</returns>
  private static async Task<(RecordingEventQueue Events, bool Result)> ExecuteAsync(Func<Task<bool>> operation)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await operation());
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获领域事件的前提下执行预期失败的业务拒绝。</summary>
  /// <param name="operation">预期抛业务拒绝的写操作。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static async Task<(RecordingEventQueue Events, InvalidOperationException Error)> ExecuteExpectingRejectionAsync(Func<Task<bool>> operation)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<InvalidOperationException>(operation));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>
  /// 把记录用事件队列挂到框架的当前事件队列位置，使领域层登记的事件可被观察。
  /// 原因与阶段 2 相同：框架事件队列由请求管道按请求绑定，测试进程没有请求管道。
  /// </summary>
  /// <returns>记录本次登记事件的队列。</returns>
  private static RecordingEventQueue InstallRecordingEventQueue()
  {
    RecordingEventQueue queue = new();
    EventQueueProperty.SetValue(null, queue);
    return queue;
  }

  /// <summary>清空测试进程的当前事件队列，避免用例之间相互影响。</summary>
  private static void UninstallRecordingEventQueue() => EventQueueProperty.SetValue(null, null);

  /// <summary>框架当前事件队列属性；测试需要写入其内部 setter 才能观察领域事件登记。</summary>
  private static readonly PropertyInfo EventQueueProperty = typeof(EventBusFactory)
    .GetProperty(nameof(EventBusFactory.CurrentEventQueue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

  /// <summary>定位仓储工程中的互认配置 SqlMap 文件，固定在聚合目录内。</summary>
  /// <param name="fileName">SqlMap 文件名。</param>
  /// <returns>该 SqlMap 文件的绝对路径。</returns>
  /// <remarks>
  /// 映射文件的物理位置受宿主默认资源模式约束：程序集根命名空间下一层目录内的直接文件才会被注册，
  /// 因此互认配置映射固定在 <c>MedicalRecognitionReportAggregate</c>。
  /// </remarks>
  /// <exception cref="FileNotFoundException">聚合目录内不存在该文件时抛出。</exception>
  private static string FindRepositorySqlMap(string fileName) =>
    SourceSyntaxGuard.FindRepositoryFile("MedicalRecognitionReportAggregate", fileName);

  /// <summary>读取领域层写路径源码文件并解析为语法树。</summary>
  /// <param name="fileName">领域层源码文件名。</param>
  /// <returns>该文件的编译单元语法树根节点。</returns>
  /// <remarks>
  /// 领域源码只允许出现在聚合根目录与按角色划分的 <c>Managers</c>、<c>Ports</c> 子目录中；
  /// 白名单之外的副本会让本用例失败，避免源码被挪走后断言读到错位的同名文件。
  /// </remarks>
  /// <exception cref="FileNotFoundException">三个允许目录内都不存在该文件时抛出。</exception>
  private static CompilationUnitSyntax ReadDomainSource(string fileName)
  {
    string aggregateDirectory = Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate");
    string[] allowedDirectories =
    [
      aggregateDirectory,
      Path.Combine(aggregateDirectory, "Managers"),
      Path.Combine(aggregateDirectory, "Ports")
    ];
    string[] matches = [.. allowedDirectories.Select(directory => Path.Combine(directory, fileName)).Where(File.Exists)];
    string sourceFile = matches.Length == 1
      ? matches[0]
      : throw new FileNotFoundException($"聚合根目录、Managers 或 Ports 内未唯一找到领域源码 '{fileName}'（命中 {matches.Length} 处）。");
    return SourceSyntaxGuard.Parse(File.ReadAllText(sourceFile));
  }

  /// <summary>
  /// V1：首次保存（该业务键无记录）时新增恰好 1 行，行为字段取请求与可信操作信息，
  /// 并登记金额已保存事件；事件标识取新增记录的主键。
  /// </summary>
  [Fact]
  public async Task Save_platform_amount_inserts_exactly_one_row_and_registers_saved_event()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(CreateAppService(repository, organizationService), PlatformRequest());

    Assert.True(result);
    Assert.Equal(1, repository.InsertCalls);
    Assert.Equal(0, repository.UpdateCalls);
    Assert.Equal(1, repository.WriteCalls);
    OrganizationHospitalBranchRecognitionAmount saved = Assert.Single(repository.Amounts.Values);
    Assert.Equal(TrustedOrganization, saved.OrganizationCode);
    Assert.Equal(TrustedHospital, saved.HospitalCode);
    Assert.Equal(TrustedBranch, saved.BranchCode);
    Assert.Equal(ProjectCode, saved.StandardProjectCode);
    Assert.Equal(FirstAmount, saved.CurrentAmount);
    Assert.Equal(TrustedOperId, saved.OperId);
    OrganizationHospitalBranchRecognitionAmountSavedEvent savedEvent =
      Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
    Assert.Equal(saved.Id, savedEvent.Id);
    Assert.Equal(TrustedOrganization, savedEvent.OrganizationCode);
    Assert.Equal(TrustedHospital, savedEvent.HospitalCode);
    Assert.Equal(TrustedBranch, savedEvent.BranchCode);
    Assert.Equal(ProjectCode, savedEvent.StandardProjectCode);
    Assert.Equal(FirstAmount, savedEvent.CurrentAmount);
  }

  /// <summary>V2：覆盖已有金额时按业务键更新恰好 1 行，落库金额取本次提交值，不在既有值上累加。</summary>
  [Fact]
  public async Task Save_platform_amount_updates_exactly_one_row_and_overwrites_instead_of_accumulating()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(
      CreateAppService(repository, organizationService), PlatformRequest(currentAmount: SecondAmount));

    Assert.True(result);
    Assert.Equal(0, repository.InsertCalls);
    Assert.Equal(1, repository.UpdateCalls);
    Assert.Equal(1, repository.WriteCalls);
    // 累加会得到 425.75；覆盖必须精确等于本次提交值。
    Assert.Equal(SecondAmount, Assert.Single(repository.Amounts.Values).CurrentAmount);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>V3：保存 0.00 成功并登记事件，金额为 0.00 的落库值不被改写成未配置。</summary>
  /// <remarks>
  /// 零元与"从未配置"的区分由查询入口的映射派生（当前金额有值即为已配置），
  /// 该映射由 Stage3QueryTests.Amount_list_returns_one_row_per_configuration_and_distinguishes_unconfigured_from_zero
  /// 经真实查询入口断言；本用例只负责保存侧的零元写入，不再自行构造读模型断言自己赋的值。
  /// </remarks>
  [Fact]
  public async Task Save_platform_amount_accepts_zero_and_read_model_distinguishes_it_from_unconfigured()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(
      CreateAppService(repository, organizationService), PlatformRequest(currentAmount: 0m));

    Assert.True(result);
    Assert.Equal(0m, Assert.Single(repository.Amounts.Values).CurrentAmount);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>V4：负数金额由领域层按业务拒绝，无写入、无事件。</summary>
  [Fact]
  public async Task Save_platform_amount_rejects_negative_amount_without_writing_or_registering_event()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(currentAmount: -0.01m)));

    Assert.Equal(InvalidAmountMessage, error.Message);
    AssertNothingWritten(repository, events);
  }

  /// <summary>V5：超过两位小数的金额由领域层按业务拒绝（契约层放行，物理列不能静默四舍五入）。</summary>
  [Fact]
  public async Task Save_platform_amount_rejects_amount_with_more_than_two_decimal_places()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(currentAmount: 1.005m)));

    Assert.Equal(InvalidAmountMessage, error.Message);
    AssertNothingWritten(repository, events);
  }

  /// <summary>V4/V5：领域管理器对金额边界的判定口径与写入口一致，且不经过应用层。</summary>
  [Fact]
  public async Task Manager_rejects_negative_amount_and_amount_beyond_two_decimals()
  {
    FakeReportRepository repository = new();
    AddMutualRecognitionConfiguration(repository, TrustedOrganization, ProjectCode, isValid: true);
    MedicalRecognitionReportManager manager = CreateManager(repository);

    (RecordingEventQueue negativeEvents, InvalidOperationException negativeError) = await ExecuteExpectingRejectionAsync(
      () => manager.SaveOrganizationHospitalBranchRecognitionAmountAsync(AmountCommand(-1m)));
    Assert.Equal(InvalidAmountMessage, negativeError.Message);
    AssertNothingWritten(repository, negativeEvents);

    foreach (decimal scaledAmount in new[] { 0.001m, 12.345m })
    {
      (RecordingEventQueue scaledEvents, InvalidOperationException scaledError) = await ExecuteExpectingRejectionAsync(
        () => manager.SaveOrganizationHospitalBranchRecognitionAmountAsync(AmountCommand(scaledAmount)));
      Assert.Equal(InvalidAmountMessage, scaledError.Message);
      AssertNothingWritten(repository, scaledEvents);
    }

    // 边界另一侧：恰好零元与恰好两位小数必须被接受，避免判据把合法值一并拒绝。
    (RecordingEventQueue acceptedEvents, bool accepted) = await ExecuteAsync(
      () => manager.SaveOrganizationHospitalBranchRecognitionAmountAsync(AmountCommand(10.50m)));
    Assert.True(accepted);
    Assert.Equal(10.50m, Assert.Single(repository.Amounts.Values).CurrentAmount);
    Assert.Single(acceptedEvents.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>构建金额保存命令，字段与应用层解析后的命令一致。</summary>
  /// <param name="currentAmount">本次提交金额。</param>
  /// <returns>可直接交给领域管理器的命令。</returns>
  private static SaveOrganizationHospitalBranchRecognitionAmountCommand AmountCommand(decimal currentAmount) => new()
  {
    OrganizationCode = TrustedOrganization,
    HospitalCode = TrustedHospital,
    BranchCode = TrustedBranch,
    StandardProjectCode = ProjectCode,
    CurrentAmount = currentAmount,
    OperId = TrustedOperId,
    OperTime = CommandOperTime
  };

  /// <summary>
  /// V6：金额在契约层声明必填，且该声明在公共请求校验处真实生效——缺省金额被拒绝，而不是静默写入零元。
  /// </summary>
  /// <remarks>
  /// 只断言特性存在不足以证明必填生效：不可空值类型上的 <see cref="RequiredAttribute"/> 判的是装箱值是否为 null，恒为真。
  /// 因此本用例同时固定可空性（可空是特性生效的前提）与校验器的真实拒绝行为，并确认被拒绝时不写入、不登记事件。
  /// </remarks>
  [Fact]
  public async Task Amount_request_declares_required_amount()
  {
    Assert.Contains(
      typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest).GetProperty(nameof(SaveOrganizationHospitalBranchRecognitionAmountRequest.CurrentAmount))!
        .GetCustomAttributes<RequiredAttribute>(),
      attribute => attribute is not null);
    Assert.Contains(
      typeof(SaveBranchRecognitionAmountRequest).GetProperty(nameof(SaveBranchRecognitionAmountRequest.CurrentAmount))!
        .GetCustomAttributes<RequiredAttribute>(),
      attribute => attribute is not null);

    // 可空性不是实现细节：不可空时缺省会被反序列化成 0，必填声明恒真、零元静默落库。
    Assert.Equal(typeof(decimal?), typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest)
      .GetProperty(nameof(SaveOrganizationHospitalBranchRecognitionAmountRequest.CurrentAmount))!.PropertyType);
    Assert.Equal(typeof(decimal?), typeof(SaveBranchRecognitionAmountRequest)
      .GetProperty(nameof(SaveBranchRecognitionAmountRequest.CurrentAmount))!.PropertyType);

    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, organizationService);

    // 金额缺省：公共请求校验必须在此之前拒绝，未提交的金额不得变成一次真实的零元写入。
    MedicalRecognitionRequestValidator.Validate(PlatformRequest(currentAmount: 0m));
    ValidationException missingAmountError = Assert.Throws<ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new SaveOrganizationHospitalBranchRecognitionAmountRequest
      {
        OrganizationCode = TrustedOrganization,
        HospitalCode = TrustedHospital,
        BranchCode = TrustedBranch,
        StandardProjectCode = ProjectCode,
        CurrentAmount = null
      }));
    Assert.Contains("参数校验失败：当前金额不能为空。", missingAmountError.Message);

    // 领域层仍按自己声明的金额边界拒绝：契约放行的是"非负且最多两位小数"之外的负数。
    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => appService.SaveOrganizationHospitalBranchRecognitionAmountAsync(PlatformRequest(currentAmount: -1m)));
    Assert.Equal(InvalidAmountMessage, error.Message);
    AssertNothingWritten(repository, events);
  }

  /// <summary>V6：零元与合法两位小数金额通过公共请求校验，必填声明不得把合法值一并拒绝。</summary>
  [Fact]
  public void Amount_request_accepts_zero_and_regular_amount()
  {
    MedicalRecognitionRequestValidator.Validate(new SaveOrganizationHospitalBranchRecognitionAmountRequest
    {
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      StandardProjectCode = ProjectCode,
      CurrentAmount = 0m
    });
    MedicalRecognitionRequestValidator.Validate(new SaveBranchRecognitionAmountRequest
    {
      BranchCode = TrustedBranch,
      StandardProjectCode = ProjectCode,
      CurrentAmount = 12.34m
    });
  }

  /// <summary>V7：同值重复保存仍成功、更新操作字段并登记事件，不做无变更短路。</summary>
  [Fact]
  public async Task Save_platform_amount_with_unchanged_value_still_updates_and_registers_event()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    Guid previousId = repository.Amounts.Values.Single().Id;

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(
      CreateAppService(repository, organizationService), PlatformRequest(currentAmount: FirstAmount));

    Assert.True(result);
    Assert.Equal(0, repository.InsertCalls);
    Assert.Equal(1, repository.UpdateCalls);
    OrganizationHospitalBranchRecognitionAmount saved = Assert.Single(repository.Amounts.Values);
    Assert.Equal(FirstAmount, saved.CurrentAmount);
    Assert.Equal(previousId, saved.Id);
    Assert.Equal(TrustedOperId, saved.OperId);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>V11：当前组织未建立该标准项目的互认配置即拒绝，不写入、不登记事件。</summary>
  [Fact]
  public async Task Save_platform_amount_is_rejected_when_mutual_recognition_configuration_is_missing()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(standardProjectCode: UnconfiguredProjectCode)));

    Assert.Equal(MissingConfigurationMessage, error.Message);
    AssertNothingWritten(repository, events);
  }

  /// <summary>
  /// V12：平台管理员入口采用请求提交的组织、医院与院区（跨组织同样按请求执行），
  /// 操作人只来自可信上下文，请求不含操作人与操作时间。
  /// </summary>
  [Fact]
  public async Task Platform_entrypoint_uses_request_path_and_trusted_operator_only()
  {
    UseTrustedContext(organizationCode: TrustedOrganization);
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = BuildOrganizationService();
    organizationService.Organizations.Add(new OrganizationDto { Id = OtherOrganization, Name = "另一个组织", IsValid = true });
    organizationService.HospitalsByOrganization[OtherOrganization] =
      [new HospitalDto { Id = OtherHospital, Name = "另一个组织的医院", OrgId = OtherOrganization, IsValid = true }];
    organizationService.BranchesByHospital[OtherHospital] = [];
    organizationService.BranchesByHospital[OtherHospital].Add(
      new BranchDto { Id = OtherHospitalBranch, Name = "另一个组织的院区", HosId = OtherHospital, OrgId = OtherOrganization, IsValid = true });
    AddMutualRecognitionConfiguration(repository, OtherOrganization, ProjectCode, isValid: true);

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(
      CreateAppService(repository, organizationService),
      PlatformRequest(organizationCode: OtherOrganization, hospitalCode: OtherHospital, branchCode: OtherHospitalBranch));

    Assert.True(result);
    OrganizationHospitalBranchRecognitionAmount saved = Assert.Single(repository.Amounts.Values);
    Assert.Equal(OtherOrganization, saved.OrganizationCode);
    Assert.Equal(OtherHospital, saved.HospitalCode);
    Assert.Equal(OtherHospitalBranch, saved.BranchCode);
    Assert.Equal(TrustedOperId, saved.OperId);

    // 请求不得提交操作人、操作时间与内部标识。
    Assert.Null(typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest).GetProperty("OperId"));
    Assert.Null(typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest).GetProperty("OperTime"));
    Assert.Null(typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest).GetProperty("Id"));
    Assert.Equal(TrustedOperId, Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>()).EventCreator);
  }

  /// <summary>V13/V30：医院管理员入口的组织与医院取可信上下文、院区取请求，且请求不能提交组织与医院。</summary>
  [Fact]
  public async Task Branch_entrypoint_takes_path_from_trusted_context_for_own_branch()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, bool result) = await SaveBranchAsync(CreateAppService(repository, organizationService), BranchRequest());

    Assert.True(result);
    OrganizationHospitalBranchRecognitionAmount saved = Assert.Single(repository.Amounts.Values);
    Assert.Equal(TrustedOrganization, saved.OrganizationCode);
    Assert.Equal(TrustedHospital, saved.HospitalCode);
    Assert.Equal(TrustedBranch, saved.BranchCode);
    Assert.Equal(FirstAmount, saved.CurrentAmount);
    Assert.Equal(TrustedOperId, saved.OperId);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());

    // 请求不得提交组织与医院。
    Assert.Null(typeof(SaveBranchRecognitionAmountRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(SaveBranchRecognitionAmountRequest).GetProperty("HospitalCode"));
    Assert.Null(typeof(SaveBranchRecognitionAmountRequest).GetProperty("OperId"));
    Assert.Null(typeof(SaveBranchRecognitionAmountRequest).GetProperty("OperTime"));
  }

  /// <summary>V13/V30：请求院区属于另一家医院时拒绝，不写入、不登记事件。</summary>
  [Fact]
  public async Task Branch_entrypoint_rejects_branch_of_another_hospital()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveBranchRecognitionAmountAsync(BranchRequest(branchCode: OtherHospitalBranch)));

    Assert.Contains("院区", error.Message);
    AssertNothingWritten(repository, events);
  }

  /// <summary>V14：可信组织与医院都由令牌与该登录用户的档案提供且都取不到时拒绝，不使用默认值、不降级为空值。</summary>
  /// <remarks>
  /// 可信范围解析先取登录令牌声明，令牌缺失的层由当前登录用户档案补齐。两类取不到的情形都必须拒绝：
  /// 读不到用户档案（此时无法补齐），以及档案可读但该层为 null、空串或纯空白（补齐后仍为空）；
  /// 两种情况都发生在组织路径解析之前，因此组织服务一次都不被调用，也不写入任何数据。
  /// </remarks>
  [Fact]
  public async Task Branch_entrypoint_is_rejected_when_trusted_organization_or_hospital_is_missing()
  {
    FakeReportRepository repository = new();
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);

    foreach ((string? organizationCode, string? hospitalCode, string expectedMessage) in new[]
    {
      (null, TrustedHospital, "组织"),
      ("   ", TrustedHospital, "组织"),
      (TrustedOrganization, null, "医院"),
      (TrustedOrganization, "   ", "医院")
    })
    {
      TrustedRequestContext.Use(organizationCode, hospitalCode, TrustedBranch, TrustedOperId.ToString());

      // 情形一：读不到用户档案，令牌缺失层无从补齐。
      (RecordingEventQueue missingProfileEvents, InvalidOperationException missingProfileError) = await SaveExpectingRejectionAsync(
        () => CreateAppService(repository, organizationService, new StubUserAppService()).SaveBranchRecognitionAmountAsync(BranchRequest()));
      Assert.Contains("用户", missingProfileError.Message);
      AssertNothingWritten(repository, missingProfileEvents);

      // 情形二：档案可读但组织与医院都取不到值，补齐后该层仍为空。
      StubUserAppService emptyProfileService = new();
      emptyProfileService.UserById[TrustedOperId] = new UserDto { Id = TrustedOperId, OrgId = "", HosId = "", BranchId = null };
      (RecordingEventQueue emptyProfileEvents, InvalidOperationException emptyProfileError) = await SaveExpectingRejectionAsync(
        () => CreateAppService(repository, organizationService, emptyProfileService).SaveBranchRecognitionAmountAsync(BranchRequest()));
      Assert.Contains(expectedMessage, emptyProfileError.Message);
      AssertNothingWritten(repository, emptyProfileEvents);

      // 两种情形都必须在读取任何组织路径之前拒绝。
      Assert.Equal(0, organizationService.OrganizationReadCount);
    }
  }

  /// <summary>V15：标准目录或互认配置停用后仍可保存金额，保存前提只判配置存在、不判配置有效。</summary>
  /// <remarks>
  /// 与"配置不存在即拒绝"对照构造：同一业务键下存在一条已停用配置，本用例的判据是保存成功；
  /// 若保存路径改成"停用即拒绝"，本用例会失败。保存路径必须真的读到这条配置，因此同时断言配置读取被调用过，
  /// 避免"在读取之前就拒绝"的实现让本用例静默通过。
  /// </remarks>
  [Fact]
  public async Task Save_platform_amount_succeeds_when_configuration_is_disabled()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddMutualRecognitionConfiguration(repository, TrustedOrganization, ProjectCode, isValid: false);
    FakeOrganizationAppService organizationService = BuildOrganizationService();

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(CreateAppService(repository, organizationService), PlatformRequest());

    Assert.True(result);
    Assert.Equal(1, repository.ConfigurationReadCalls);
    Assert.Equal(1, repository.InsertCalls);
    Assert.Equal(FirstAmount, Assert.Single(repository.Amounts.Values).CurrentAmount);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>
  /// 影响行数 0 行的复读三分支之一：复读读不到记录时按记录已不存在拒绝，无写入、无事件。
  /// </summary>
  /// <remarks>复读结果用脚本化读取构造：第一次读取命中既有记录，第二次读取返回空表示记录已被删除。</remarks>
  [Fact]
  public async Task Save_platform_amount_is_rejected_when_zero_rows_affected_and_record_is_gone()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    OrganizationHospitalBranchRecognitionAmount existing = repository.Amounts.Values.Single();
    repository.ScriptedAmountReads.Enqueue(existing);
    repository.ScriptedAmountReads.Enqueue(null);
    repository.ScriptedWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(currentAmount: SecondAmount)));

    Assert.Equal("业务拒绝：金额记录不存在。", error.Message);
    Assert.Equal(1, repository.UpdateCalls);
    Assert.Equal(2, repository.AmountReadCalls);
    // 已存在的记录保持原值：既没有写入，也没有被并发复读改写。
    Assert.Equal(FirstAmount, Assert.Single(repository.Amounts.Values).CurrentAmount);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// 影响行数 0 行的复读三分支之二：复读金额已等于本次提交值时按成功处理并登记事件，
  /// 只复读一次，不循环重试、不自动重放。
  /// </summary>
  [Fact]
  public async Task Save_platform_amount_treats_zero_rows_as_success_when_concurrent_value_already_matches()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    ScriptConcurrentSameValueUpdate(repository);

    (RecordingEventQueue events, bool result) = await SavePlatformAsync(
      CreateAppService(repository, organizationService), PlatformRequest(currentAmount: SecondAmount));

    Assert.True(result);
    Assert.Equal(1, repository.UpdateCalls);
    Assert.Equal(2, repository.AmountReadCalls);
    Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
  }

  /// <summary>
  /// 影响行数 0 行的复读三分支之三：复读金额与本次提交值仍不同时返回并发冲突并提示刷新后重试。
  /// </summary>
  [Fact]
  public async Task Save_platform_amount_rejects_when_zero_rows_affected_and_concurrent_value_differs()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    ScriptConcurrentConflictingUpdate(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(currentAmount: SecondAmount)));

    Assert.Equal(ConcurrentConflictMessage, error.Message);
    Assert.Equal(2, repository.AmountReadCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>影响行数大于 1 是持久化不变量异常：不返回成功、不登记事件。</summary>
  [Fact]
  public async Task Save_platform_amount_rejects_when_update_affects_more_than_one_row()
  {
    UseTrustedContext();
    FakeReportRepository repository = new();
    AddSavedAmount(repository, FirstAmount);
    FakeOrganizationAppService organizationService = CreateConfiguredOrganization(repository);
    repository.ScriptedWriteRows.Enqueue(2);

    (RecordingEventQueue events, InvalidOperationException error) = await SaveExpectingRejectionAsync(
      () => CreateAppService(repository, organizationService).SaveOrganizationHospitalBranchRecognitionAmountAsync(
        PlatformRequest(currentAmount: SecondAmount)));

    Assert.Equal(InvariantMessage, error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V25：事件字段取自命令与受影响记录标识，事件创建人与创建时间取命令的操作人/操作时间；
  /// 事件不携带入口标识、页面名称或提示文案。
  /// </summary>
  /// <remarks>
  /// 事件时间在应用入口取服务端当前时间，无法在用例内固定；因此操作人与操作时间的映射由本用例直接以携带固定操作时间的命令调用领域管理器验证，
  /// 失败路径不登记事件则在各自的失败用例内以 Assert.Empty(events.Events) 验证。
  /// </remarks>
  [Fact]
  public async Task Saved_event_fields_come_from_command_and_failures_register_no_event()
  {
    FakeReportRepository repository = new();
    AddMutualRecognitionConfiguration(repository, TrustedOrganization, ProjectCode, isValid: true);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SaveOrganizationHospitalBranchRecognitionAmountAsync(AmountCommand(FirstAmount)));

    Assert.True(result);
    OrganizationHospitalBranchRecognitionAmountSavedEvent savedEvent =
      Assert.Single(events.Events.OfType<OrganizationHospitalBranchRecognitionAmountSavedEvent>());
    Assert.Equal(TrustedOperId, savedEvent.EventCreator);
    Assert.Equal(CommandOperTime, savedEvent.EventCreatedTime);
    Assert.Equal(nameof(OrganizationHospitalBranchRecognitionAmountSavedEvent), savedEvent.EventType);
    Assert.Equal(MedicalRecognitionReportConst.AggregateId, savedEvent.AggregateId);
    Assert.Equal(TrustedOrganization, savedEvent.OrganizationCode);
    Assert.Equal(TrustedHospital, savedEvent.HospitalCode);
    Assert.Equal(TrustedBranch, savedEvent.BranchCode);
    Assert.Equal(ProjectCode, savedEvent.StandardProjectCode);
    Assert.Equal(FirstAmount, savedEvent.CurrentAmount);
    Assert.Equal(Assert.Single(repository.Amounts.Values).Id, savedEvent.Id);
    Assert.Single(events.Events);
  }

  /// <summary>
  /// V26：两个保存请求与读模型的字段、可空性与设计一致；读模型不返回作用域编码与最后修改字段，
  /// 且 CurrentAmount 可空。
  /// </summary>
  [Fact]
  public void Save_requests_and_read_model_expose_only_the_designed_members()
  {
    // 写请求沿用 IPropertyChangedAware 的源生成形态，属性集合还包含生成器补入的变更跟踪成员，因此按业务字段过滤后再断言。
    Assert.Equal(
      ["BranchCode", "CurrentAmount", "StandardProjectCode"],
      BusinessPropertyNames(typeof(SaveBranchRecognitionAmountRequest)));
    Assert.Equal(
      ["BranchCode", "CurrentAmount", "HospitalCode", "OrganizationCode", "StandardProjectCode"],
      BusinessPropertyNames(typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest)));
    Assert.NotNull(typeof(SaveBranchRecognitionAmountRequest).GetProperty(nameof(SaveBranchRecognitionAmountRequest.CurrentAmount)));

    PropertyInfo[] readModelProperties = [.. typeof(RecognitionAmountReadModel).GetProperties().OrderBy(property => property.Name, StringComparer.Ordinal)];
    Assert.Equal(
      [
        "BranchName", "CategoryName", "ConfigurationStatus", "ConfigurationStatusText", "CurrentAmount", "GroupName",
        "HospitalName", "IsAmountConfigured", "ItemType", "ItemTypeText", "OrganizationName", "StandardProjectCode",
        "StandardProjectName", "UnavailableReason"
      ],
      readModelProperties.Select(property => property.Name));
    Assert.Equal(14, readModelProperties.Length);
    Assert.Equal(typeof(decimal?), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.CurrentAmount))!.PropertyType);
    // 可空引用类型不能直接写 typeof(string?)：用可空性注解上下文判定 UnavailableReason 与三个名称字段的声明口径。
    NullabilityInfoContext nullability = new();
    Assert.Equal(typeof(string), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.UnavailableReason))!.PropertyType);
    Assert.Equal(
      NullabilityState.Nullable,
      nullability.Create(typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.UnavailableReason))!).ReadState);
    Assert.Equal(
      NullabilityState.NotNull,
      nullability.Create(typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.StandardProjectCode))!).ReadState);
    Assert.Equal(typeof(MedicalItemType), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ItemType))!.PropertyType);
    Assert.Equal(typeof(ConfigurationStatus), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ConfigurationStatus))!.PropertyType);
    // 两个枚举中文的声明口径与阶段 2 一致：项目类型中文可空（未登记取值降级为 null），配置状态中文非空（未登记取值按严格解析抛出）。
    Assert.Equal(typeof(string), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ItemTypeText))!.PropertyType);
    Assert.Equal(typeof(string), typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ConfigurationStatusText))!.PropertyType);
    Assert.Equal(
      NullabilityState.Nullable,
      nullability.Create(typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ItemTypeText))!).ReadState);
    Assert.Equal(
      NullabilityState.NotNull,
      nullability.Create(typeof(RecognitionAmountReadModel).GetProperty(nameof(RecognitionAmountReadModel.ConfigurationStatusText))!).ReadState);
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("OrganizationCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("HospitalCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("BranchCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("LastModifiedTime"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("LastModifiedBy"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("IsStandardCatalogValid"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("IsAvailableForNewMatch"));
    Assert.Equal("Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount", typeof(RecognitionAmountReadModel).Namespace);
  }

  /// <summary>两个保存入口各保持最多一条写语句，且都不声明 <c>WorkUnitAttribute</c>。</summary>
  [Fact]
  public void Amount_write_entrypoints_declare_no_work_unit()
  {
    Type appService = typeof(MedicalRecognitionReportAppService);
    foreach (string name in new[] { "SaveOrganizationHospitalBranchRecognitionAmountAsync", "SaveBranchRecognitionAmountAsync" })
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }
  }

  /// <summary>两个保存入口先执行公共请求校验，再调用领域管理器。</summary>
  [Fact]
  public void Amount_write_entrypoints_validate_request_before_domain_call()
  {
    string source = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Application", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportAppService.cs"));

    foreach ((string name, string signature) in new[]
    {
      ("SaveOrganizationHospitalBranchRecognitionAmountAsync",
        "public async Task<bool> SaveOrganizationHospitalBranchRecognitionAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountRequest request)"),
      ("SaveBranchRecognitionAmountAsync",
        "public async Task<bool> SaveBranchRecognitionAmountAsync(SaveBranchRecognitionAmountRequest request)")
    })
    {
      string body = ExtractDeclaredMethodBody(source, signature, name);
      int validateIndex = body.IndexOf("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal);
      // 两个入口都先校验、再解析组织路径、最后交给同一个领域方法；以公共校验必须先于后续两个步骤出现为判据。
      int mapIndex = body.IndexOf(".MapToSaveOrganizationHospitalBranchRecognitionAmountCommand()", StringComparison.Ordinal);
      int resolveIndex = body.IndexOf("organizationPathResolver.ResolveOrThrow(", StringComparison.Ordinal);
      int domainIndex = body.IndexOf("SaveAmountAsync(command, path)", StringComparison.Ordinal);
      Assert.True(validateIndex >= 0, $"{name}: 缺少公共请求校验调用");
      Assert.True(mapIndex > validateIndex, $"{name}: 命令映射缺失或早于公共请求校验");
      Assert.True(resolveIndex > validateIndex, $"{name}: 组织路径解析缺失或早于公共请求校验");
      Assert.True(domainIndex > validateIndex, $"{name}: 未调用金额保存领域步骤或早于公共请求校验");
    }
  }

  /// <summary>
  /// 按完整声明签名定位唯一方法声明并切出方法体，避免同名或相似声明被误命中。
  /// </summary>
  /// <param name="source">应用服务的 C# 源码文本。</param>
  /// <param name="signature">方法声明的完整签名前缀。</param>
  /// <param name="name">方法名，仅用于异常信息。</param>
  /// <returns>方法体源码文本（不含最外层花括号）。</returns>
  /// <exception cref="InvalidOperationException">签名没有命中唯一声明，或方法体花括号不配平时抛出。</exception>
  private static string ExtractDeclaredMethodBody(string source, string signature, string name)
  {
    int declarationIndex = source.IndexOf(signature, StringComparison.Ordinal);
    if (declarationIndex < 0) throw new InvalidOperationException($"{name}: 未在应用服务源码中找到方法声明。");
    if (source.IndexOf(signature, declarationIndex + signature.Length, StringComparison.Ordinal) >= 0)
      throw new InvalidOperationException($"{name}: 方法声明不唯一。");

    int bodyStart = source.IndexOf('{', declarationIndex + signature.Length);
    if (bodyStart < 0) throw new InvalidOperationException($"{name}: 方法体起点缺失。");

    int depth = 0;
    for (int index = bodyStart; index < source.Length; index++)
    {
      if (source[index] == '{') depth++;
      else if (source[index] == '}')
      {
        depth--;
        if (depth == 0) return source[(bodyStart + 1)..index];
      }
    }

    throw new InvalidOperationException($"{name}: 方法体花括号不配平。");
  }

  /// <summary>
  /// V28：实体注释与语义按「当前金额维护」修正，写入路径不含任何累加运算。
  /// </summary>
  [Fact]
  public void Amount_entity_semantics_describe_current_amount_maintenance_without_accumulation()
  {
    string entitySource = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate", "OrganizationHospitalBranchRecognitionAmount.cs"));

    Assert.DoesNotContain("累计", entitySource, StringComparison.Ordinal);
    Assert.DoesNotContain("累加", entitySource, StringComparison.Ordinal);
    Assert.Contains("维护的互认项目当前金额", entitySource, StringComparison.Ordinal);
    Assert.Contains("保存时由请求提交值覆盖", entitySource, StringComparison.Ordinal);
    Assert.Contains("取命令携带的操作时间", entitySource, StringComparison.Ordinal);

    // 写入路径不得出现金额的复合赋值（+= 等），也不得出现把 CurrentAmount 当作算术操作数的运算；
    // 累计写入在语法上必然表现为其中一种形态，而比较大小的判定表达式不是算术运算，不会被本判据命中。
    SyntaxKind[] arithmeticKinds =
    [
      SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, SyntaxKind.MultiplyExpression,
      SyntaxKind.DivideExpression, SyntaxKind.ModuloExpression
    ];
    foreach (string fileName in new[] { "MedicalRecognitionReportManager.cs", "OrganizationHospitalBranchRecognitionAmount.cs" })
    {
      CompilationUnitSyntax root = ReadDomainSource(fileName);
      Assert.DoesNotContain(
        root.DescendantNodes().OfType<AssignmentExpressionSyntax>(),
        assignment => assignment.Kind() == SyntaxKind.AddAssignmentExpression || assignment.Kind() == SyntaxKind.SubtractAssignmentExpression);
      Assert.DoesNotContain(
        root.DescendantNodes().OfType<BinaryExpressionSyntax>(),
        binary => arithmeticKinds.Contains(binary.Kind())
          && binary.DescendantNodesAndSelf().Any(node => node.ToString().Contains("CurrentAmount", StringComparison.Ordinal)));
    }
  }

  /// <summary>
  /// 互认配置按业务键读取语句：新语句必须注册在互认配置作用域下、按组织与标准项目编码定位单行，
  /// 且不改动该文件既有语句。
  /// </summary>
  [Fact]
  public void Mutual_configuration_sql_map_exposes_business_key_lookup()
  {
    System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Load(FindRepositorySqlMap("MutualRecognitionItem.xml"));
    System.Xml.Linq.XElement statement = document.Descendants()
      .Single(element => (string?)element.Attribute("Id") == "GetMutualRecognitionItemByOrganizationAndProject");

    Assert.Equal("MutualRecognitionItem", (string?)document.Root!.Attribute("Scope"));
    Assert.Contains("mrec_mutual_recognition_item", statement.Value, StringComparison.Ordinal);
    Assert.Contains("organization_code = $OrganizationCode", statement.Value, StringComparison.Ordinal);
    Assert.Contains("standard_project_code = $StandardProjectCode", statement.Value, StringComparison.Ordinal);
    Assert.True(statement.PreviousNode is System.Xml.Linq.XComment);

    // 既有语句保持不变：这四条语句的定位条件与调用点约定逐字冻结。
    string existing = string.Concat(document.Descendants()
      .Where(element => (string?)element.Attribute("Id") is "GetMutualRecognitionItemById" or "UpdateMutualRecognitionItemConfiguration" or "EnableMutualRecognitionItem" or "DisableMutualRecognitionItem")
      .Select(element => element.Value));
    Assert.Contains("where id = $Id and organization_code = $OrganizationCode", existing, StringComparison.Ordinal);
    Assert.Contains("where id = $Id and organization_code = $OrganizationCode and is_valid = false", existing, StringComparison.Ordinal);
    Assert.Contains("where id = $Id and organization_code = $OrganizationCode and is_valid = true", existing, StringComparison.Ordinal);
  }

  /// <summary>
  /// 取一个请求类型的业务字段名（升序），排除 <c>IPropertyChangedAware</c> 源生成器补入的变更跟踪成员。
  /// </summary>
  /// <param name="requestType">请求类型。</param>
  /// <returns>按名称升序排列的业务字段名。</returns>
  private static string[] BusinessPropertyNames(Type requestType) =>
    [.. requestType.GetProperties()
      .Select(property => property.Name)
      .Where(name => name is not "ChangedProperties" and not "HasPropertyChanged")
      .Order(StringComparer.Ordinal)];

  /// <summary>
  /// 记录领域层登记事件的测试替身，用于断言哪些成功路径登记了事件、哪些失败路径没有登记。
  /// </summary>
  private sealed class RecordingEventQueue : IEventQueue
  {
    /// <summary>本次测试期间登记的事件，按登记顺序保存。</summary>
    public List<IEvent> Events { get; } = [];

    /// <summary>记录一个待发布事件。</summary>
    /// <param name="eventData">登记的领域事件。</param>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Enqueue<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Add(eventData);

    /// <summary>记录队列不提供按对象移除能力。</summary>
    /// <param name="eventData">待移除事件。</param>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Remove<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Remove(eventData);

    /// <summary>记录队列不提供按接口移除能力。</summary>
    /// <param name="eventData">待移除事件。</param>
    public void RemoveEvent(IEvent eventData) => Events.Remove(eventData);

    /// <summary>记录队列不提供按类型批量移除能力。</summary>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Remove<TEventData>() where TEventData : IEvent => Events.RemoveAll(@event => @event is TEventData);

    /// <summary>记录队列不在测试中发布事件，仅观察登记结果。</summary>
    /// <returns>已完成的任务。</returns>
    public Task FlushAsync() => Task.CompletedTask;

    /// <summary>清空已登记事件。</summary>
    public void Clear() => Events.Clear();
  }

  /// <summary>
  /// 金额与互认配置读写的内存测试替身：提供四个业务键定位的金额数据、互认配置存在性判断、
  /// 可脚本化的影响行数与并发复读结果，并记录写语句调用次数与最后一次写入内容。
  /// </summary>
  /// <remarks>
  /// 金额的按业务键读取不做脚本化：并发首插用例需要的"读取不到记录"就是该读取在空数据下的真实行为，
  /// 若在此处脚本化就会连管理器的业务键定位一起替换掉。
  /// 复读金额由 <see cref="ConcurrentAmountApplied"/> 表达：读取时若该值非空，
  /// 用叠加在当前金额上的方式返回，使"复读金额等于提交值"与"复读金额不同"两种并发结果都可构造。
  /// </remarks>
  private sealed class FakeReportRepository : IMedicalRecognitionReportRepository
  {
    /// <summary>内存金额数据，按四个业务键定位。</summary>
    public Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode, string StandardProjectCode), OrganizationHospitalBranchRecognitionAmount> Amounts { get; } = [];

    /// <summary>内存互认配置数据，按组织编码与标准项目编码定位。</summary>
    public Dictionary<(string OrganizationCode, string StandardProjectCode), MutualRecognitionItem> Configurations { get; } = [];

    /// <summary>按顺序脚本化写语句影响行数；为空时按内存数据写出并返回 1。</summary>
    public Queue<int> ScriptedWriteRows { get; } = [];

    /// <summary>按顺序脚本化金额按业务键读取的结果；为空时按内存数据读取。</summary>
    public Queue<OrganizationHospitalBranchRecognitionAmount?> ScriptedAmountReads { get; } = [];

    /// <summary>非空时表示已发生并发写入：按业务键读取返回该金额，用于构造"复读金额等于提交值"与"复读金额不同"两种并发结果。</summary>
    public decimal? ConcurrentAmountApplied { get; set; }

    /// <summary>金额按业务键读取的次数。</summary>
    public int AmountReadCalls { get; private set; }

    /// <summary>按组织与标准项目编码读取互认配置的次数，用于观察保存前提校验是否真的检查了配置存在性。</summary>
    public int ConfigurationReadCalls { get; private set; }

    /// <summary>新增语句调用次数。</summary>
    public int InsertCalls { get; private set; }

    /// <summary>更新语句调用次数。</summary>
    public int UpdateCalls { get; private set; }

    /// <summary>全部写语句（新增与更新）的合计调用次数。</summary>
    public int WriteCalls => InsertCalls + UpdateCalls;

    /// <summary>最后一次新增写入的金额记录。</summary>
    public OrganizationHospitalBranchRecognitionAmount? LastInserted { get; private set; }

    /// <summary>最后一次更新写入的金额记录。</summary>
    public OrganizationHospitalBranchRecognitionAmount? LastUpdated { get; private set; }

    /// <summary>生成一个新的金额记录主键。</summary>
    /// <returns>新的主键标识。</returns>
    public Guid CreateGuid() => Guid.NewGuid();

    /// <summary>按四个业务键读取单行金额。</summary>
    /// <param name="organizationCode">组织编码。</param>
    /// <param name="hospitalCode">医院编码。</param>
    /// <param name="branchCode">院区编码。</param>
    /// <param name="standardProjectCode">标准项目编码。</param>
    /// <returns>金额记录；不存在时返回 <see langword="null"/>。</returns>
    public Task<OrganizationHospitalBranchRecognitionAmount?> GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
      string organizationCode, string hospitalCode, string branchCode, string standardProjectCode)
    {
      AmountReadCalls++;
      if (ScriptedAmountReads.Count > 0) return Task.FromResult(ScriptedAmountReads.Dequeue());
      if (!Amounts.TryGetValue((organizationCode, hospitalCode, branchCode, standardProjectCode), out OrganizationHospitalBranchRecognitionAmount? amount)) return Task.FromResult<OrganizationHospitalBranchRecognitionAmount?>(null);

      return Task.FromResult<OrganizationHospitalBranchRecognitionAmount?>(new OrganizationHospitalBranchRecognitionAmount
      {
        Id = amount.Id,
        OrganizationCode = amount.OrganizationCode,
        HospitalCode = amount.HospitalCode,
        BranchCode = amount.BranchCode,
        StandardProjectCode = amount.StandardProjectCode,
        // 并发写入用当次读取返回的金额表达：未配置并发写入时返回已存金额，配置后返回并发方实际写入的金额。
        CurrentAmount = ConcurrentAmountApplied ?? amount.CurrentAmount,
        OperId = amount.OperId,
        OperTime = amount.OperTime
      });
    }

    /// <summary>新增一条金额记录，模拟影响行数。</summary>
    /// <param name="amount">待插入金额记录。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> CreateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount amount)
    {
      InsertCalls++;
      LastInserted = amount;

      (string, string, string, string) key = (amount.OrganizationCode, amount.HospitalCode, amount.BranchCode, amount.StandardProjectCode);
      if (ScriptedWriteRows.Count > 0)
      {
        int rows = ScriptedWriteRows.Dequeue();
        if (rows > 0) Amounts[key] = amount;
        return Task.FromResult(rows);
      }

      Amounts[key] = amount;
      return Task.FromResult(1);
    }

    /// <summary>按记录标识与四个业务键条件更新金额与操作字段，模拟影响行数。</summary>
    /// <param name="amount">待更新金额记录。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> UpdateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount amount)
    {
      UpdateCalls++;
      LastUpdated = amount;
      if (ScriptedWriteRows.Count > 0) return Task.FromResult(ScriptedWriteRows.Dequeue());

      (string, string, string, string) key = (amount.OrganizationCode, amount.HospitalCode, amount.BranchCode, amount.StandardProjectCode);
      if (!Amounts.TryGetValue(key, out OrganizationHospitalBranchRecognitionAmount? existing) || existing.Id != amount.Id) return Task.FromResult(0);

      existing.CurrentAmount = amount.CurrentAmount;
      existing.OperId = amount.OperId;
      existing.OperTime = amount.OperTime;
      return Task.FromResult(1);
    }

    /// <summary>按组织与标准项目编码读取互认配置的存在性，用于保存前提校验。</summary>
    /// <param name="organizationCode">组织编码。</param>
    /// <param name="standardProjectCode">标准项目编码。</param>
    /// <returns>互认配置；未建立时返回 <see langword="null"/>。</returns>
    public Task<MutualRecognitionItem?> GetMutualRecognitionItemByOrganizationAndProjectAsync(string organizationCode, string standardProjectCode)
    {
      ConfigurationReadCalls++;
      return Task.FromResult(Configurations.TryGetValue((organizationCode, standardProjectCode), out MutualRecognitionItem? configuration) ? configuration : null);
    }

    /// <summary>本测试未使用：在指定组织范围内按配置标识读取互认配置。</summary>
    /// <param name="id">配置标识。</param>
    /// <param name="organizationCode">可信组织编码。</param>
    /// <returns>不返回结果。</returns>
    public Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增互认配置。</summary>
    /// <param name="mutualRecognitionItem">待插入配置。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem mutualRecognitionItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：修改互认配置可互认时间。</summary>
    /// <param name="mutualRecognitionItem">待更新配置。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem mutualRecognitionItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：启用互认配置。</summary>
    /// <param name="enableMutualRecognitionItemCommand">启用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand enableMutualRecognitionItemCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：停用互认配置。</summary>
    /// <param name="disableMutualRecognitionItemCommand">停用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand disableMutualRecognitionItemCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按主键读取标准项目。</summary>
    /// <param name="id">标准项目标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按编码读取标准项目。</summary>
    /// <param name="code">标准项目编码。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按主键读取分类。</summary>
    /// <param name="id">分类标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按主键读取分组。</summary>
    /// <param name="id">分组标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：分类名称查重。</summary>
    /// <param name="name">分类名称。</param>
    /// <param name="excludedId">排除标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<bool> MedicalStandardCategoryNameExistsAsync(string name, Guid? excludedId = null) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：分类下级分组判断。</summary>
    /// <param name="categoryId">分类标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<bool> MedicalStandardCategoryHasGroupsAsync(Guid categoryId) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：分组名称查重。</summary>
    /// <param name="categoryId">分类标识。</param>
    /// <param name="name">分组名称。</param>
    /// <param name="excludedId">排除标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<bool> MedicalStandardGroupNameExistsAsync(Guid categoryId, string name, Guid? excludedId = null) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：标准项目编码查重。</summary>
    /// <param name="code">标准项目编码。</param>
    /// <returns>不返回结果。</returns>
    public Task<bool> MedicalStandardItemCodeExistsAsync(string code) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增分类。</summary>
    /// <param name="medicalStandardCategory">分类实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：修改分类。</summary>
    /// <param name="medicalStandardCategory">分类实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：启用分类。</summary>
    /// <param name="enableMedicalStandardCategoryCommand">启用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand enableMedicalStandardCategoryCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：停用分类。</summary>
    /// <param name="disableMedicalStandardCategoryCommand">停用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand disableMedicalStandardCategoryCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增分组。</summary>
    /// <param name="medicalStandardGroup">分组实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：修改分组。</summary>
    /// <param name="medicalStandardGroup">分组实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：启用分组。</summary>
    /// <param name="enableMedicalStandardGroupCommand">启用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand enableMedicalStandardGroupCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：停用分组。</summary>
    /// <param name="disableMedicalStandardGroupCommand">停用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand disableMedicalStandardGroupCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增标准项目。</summary>
    /// <param name="medicalStandardItem">标准项目实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMedicalStandardItemAsync(MedicalStandardItem medicalStandardItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：修改标准项目备注。</summary>
    /// <param name="medicalStandardItem">标准项目实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> ChangeMedicalStandardItemRemarkAsync(MedicalStandardItem medicalStandardItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：启用标准项目。</summary>
    /// <param name="enableMedicalStandardItemCommand">启用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand enableMedicalStandardItemCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：停用标准项目。</summary>
    /// <param name="disableMedicalStandardItemCommand">停用命令。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand disableMedicalStandardItemCommand) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按证件键读取平台患者。</summary>
    /// <param name="identityDocumentTypeCode">证件类型代码。</param>
    /// <param name="identityDocumentNo">证件号码。</param>
    /// <returns>不返回结果。</returns>
    public Task<PlatformPatient?> GetPlatformPatientByDocumentAsync(string identityDocumentTypeCode, string identityDocumentNo) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增平台患者。</summary>
    /// <param name="platformPatient">平台患者实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreatePlatformPatientAsync(PlatformPatient platformPatient) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按报告标识读取报告。</summary>
    /// <param name="id">报告标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按五个业务键定位报告。</summary>
    /// <param name="organizationCode">组织编码。</param>
    /// <param name="hospitalCode">医院编码。</param>
    /// <param name="branchCode">院区编码。</param>
    /// <param name="reportType">报告类型。</param>
    /// <param name="reportNo">来源报告单号。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByBusinessKeyAsync(
      string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增报告。</summary>
    /// <param name="medicalRecognitionReport">报告实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：更新报告的当前版本指向与检索列。</summary>
    /// <param name="medicalRecognitionReport">报告实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateMedicalRecognitionReportCurrentVersionAsync(MedicalRecognitionReport medicalRecognitionReport) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：把报告作废。</summary>
    /// <param name="medicalRecognitionReport">报告实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> VoidMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：读取报告当前最大版本序号。</summary>
    /// <param name="reportId">报告标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> GetMedicalReportMaxVersionNumberAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按版本标识读取版本。</summary>
    /// <param name="id">报告版本标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<MedicalReportVersion?> GetMedicalReportVersionByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增报告版本。</summary>
    /// <param name="medicalReportVersion">报告版本实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateMedicalReportVersionAsync(MedicalReportVersion medicalReportVersion) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增检验专项内容。</summary>
    /// <param name="laboratoryReportContent">检验专项内容实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateLaboratoryReportContentAsync(LaboratoryReportContent laboratoryReportContent) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增普通检验结果。</summary>
    /// <param name="laboratoryResultItem">普通检验结果实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateLaboratoryResultItemAsync(LaboratoryResultItem laboratoryResultItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增细菌鉴定结果。</summary>
    /// <param name="laboratoryBacteriaResult">细菌鉴定结果实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateLaboratoryBacteriaResultAsync(LaboratoryBacteriaResult laboratoryBacteriaResult) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增药敏结果。</summary>
    /// <param name="laboratoryAntimicrobialSusceptibility">药敏结果实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateLaboratoryAntimicrobialSusceptibilityAsync(LaboratoryAntimicrobialSusceptibility laboratoryAntimicrobialSusceptibility) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增检查专项内容。</summary>
    /// <param name="examinationReportContent">检查专项内容实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateExaminationReportContentAsync(ExaminationReportContent examinationReportContent) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增检查项目。</summary>
    /// <param name="examinationItem">检查项目实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateExaminationItemAsync(ExaminationItem examinationItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增检查部位。</summary>
    /// <param name="examinationSite">检查部位实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateExaminationSiteAsync(ExaminationSite examinationSite) => throw new NotSupportedException(UnusedMember);

    /// <summary>未使用的仓储成员统一提示，避免测试静默走过未被覆盖的写入路径。</summary>
    private const string UnusedMember = "本测试未使用该仓储成员。";
  }

  /// <summary>
  /// 外部组织服务的手写替身：只提供保存入口需要的组织与医院读取，其余成员调用即失败。
  /// </summary>
  /// <remarks>金额保存只做路径校验，不读写科室、病区与平台组织写方法，因此其余成员一律抛出。</remarks>
  private sealed class FakeOrganizationAppService : IOrganizationAppService
  {
    /// <summary>组织读取返回的启用组织集合。</summary>
    public List<OrganizationDto> Organizations { get; init; } = [];

    /// <summary>按组织编码预置的医院读取结果。</summary>
    public Dictionary<string, List<HospitalDto>> HospitalsByOrganization { get; } = [];

    /// <summary>按医院编码预置的院区读取结果。</summary>
    public Dictionary<string, List<BranchDto>> BranchesByHospital { get; } = [];

    /// <summary>组织全量读取的调用次数；可信范围拒绝时该计数必须保持为 0。</summary>
    public int OrganizationReadCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync()
    {
      OrganizationReadCount++;
      return Task.FromResult<IEnumerable<OrganizationDto>>(Organizations);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest queryAllValidHospitalByOrgIdRequest) =>
      Task.FromResult<IEnumerable<HospitalDto>>(HospitalsByOrganization.TryGetValue(queryAllValidHospitalByOrgIdRequest.OrgId, out List<HospitalDto>? hospitals) ? hospitals : []);

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest queryAllValidBranchByHosIdRequest) =>
      Task.FromResult<IEnumerable<BranchDto>>(BranchesByHospital.TryGetValue(queryAllValidBranchByHosIdRequest.HosId, out List<BranchDto>? branches) ? branches : []);

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByOrgIdAsync(QueryAllValidBranchByOrgIdRequest queryAllValidBranchByOrgIdRequest) => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<OrganizationDto> GetOrganizationByIdAsync(GetOrganizationByIdRequest getOrganizationByIdRequest) => Unsupported<OrganizationDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

    /// <inheritdoc/>
    public Task<HospitalDto> GetHospitalByIdAsync(GetHospitalByIdRequest getHospitalByIdRequest) => Unsupported<HospitalDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<BranchDto> GetBranchByIdAsync(GetBranchByIdRequest getBranchByIdRequest) => Unsupported<BranchDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptBySubApplicationIdAsync(QueryDeptBySubApplicationIdRequest queryDeptBySubApplicationIdRequest) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByOrgIdAsync(QueryDeptByOrgIdRequest queryDeptByOrgIdRequest) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByHosIdAsync(QueryDeptByHosIdRequest queryDeptByHosIdRequest) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByBranchIdAsync(QueryDeptByBranchIdRequest queryDeptByBranchIdRequest) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByBranchIdAndCodeAsync(GetDeptByBranchIdAndCodeRequest getDeptByBranchIdAndCodeRequest) => Unsupported<DeptDto>();

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByIdAsync(GetDeptByIdRequest getDeptByIdRequest) => Unsupported<DeptDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryAllDeptAsync() => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<DeptWardDto> GetDeptWardByIdAsync(GetDeptWardByIdRequest getDeptWardByIdRequest) => Unsupported<DeptWardDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByDeptAsync(QueryDeptWardByDeptRequest queryDeptWardByDeptRequest) => Unsupported<IEnumerable<DeptWardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByWardAsync(QueryDeptWardByWardRequest queryDeptWardByWardRequest) => Unsupported<IEnumerable<DeptWardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardByBranchAsync(QueryWardByBranchRequest queryWardByBranchRequest) => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardBySubApplicationIdAsync(QueryWardBySubApplicationIdRequest queryWardBySubApplicationIdRequest) => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<WardDto> GetWardByIdAsync(GetWardByIdRequest getWardByIdRequest) => Unsupported<WardDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryAllWardAsync() => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<string> CreateOrganizationAsync(CreateOrganizationRequest createOrganizationRequest) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateOrganizationAsync(UpdateOrganizationRequest updateOrganizationRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteOrganizationAsync(DeleteOrganizationRequest deleteOrganizationRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableOrganizationAsync(EnableOrganizationRequest enableOrganizationRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableOrganizationAsync(DisableOrganizationRequest disableOrganizationRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<string> CreateHospitalAsync(CreateHospitalRequest createHospitalRequest) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateHospitalAsync(UpdateHospitalRequest updateHospitalRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteHospitalAsync(DeleteHospitalRequest deleteHospitalRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableHospitalAsync(EnableHospitalRequest enableHospitalRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableHospitalAsync(DisableHospitalRequest disableHospitalRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<string> CreateBranchAsync(CreateBranchRequest createBranchRequest) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateBranchAsync(UpdateBranchRequest updateBranchRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteBranchAsync(DeleteBranchRequest deleteBranchRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableBranchAsync(EnableBranchRequest enableBranchRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableBranchAsync(DisableBranchRequest disableBranchRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateDeptAsync(CreateDeptRequest createDeptRequest) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptAsync(UpdateDeptRequest updateDeptRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptAsync(DeleteDeptRequest deleteDeptRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptAsync(EnableDeptRequest enableDeptRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptAsync(DisableDeptRequest disableDeptRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptUpAsync(MoveDeptUpRequest moveDeptUpRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptDownAsync(MoveDeptDownRequest moveDeptDownRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateDeptWardAsync(CreateDeptWardRequest createDeptWardRequest) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptWardAsync(UpdateDeptWardRequest updateDeptWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptWardAsync(DeleteDeptWardRequest deleteDeptWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptWardAsync(EnableDeptWardRequest enableDeptWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptWardAsync(DisableDeptWardRequest disableDeptWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateWardAsync(CreateWardRequest createWardRequest) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateWardAsync(UpdateWardRequest updateWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteWardAsync(DeleteWardRequest deleteWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableWardAsync(EnableWardRequest enableWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableWardAsync(DisableWardRequest disableWardRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardUpAsync(MoveWardUpRequest moveWardUpRequest) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardDownAsync(MoveWardDownRequest moveWardDownRequest) => Unsupported<int>();

    /// <summary>金额保存不调用该组织服务成员。</summary>
    /// <typeparam name="TResult">成员返回类型。</typeparam>
    /// <returns>不会返回。</returns>
    /// <exception cref="NotSupportedException">该成员被调用时抛出。</exception>
    private static Task<TResult> Unsupported<TResult>() => throw new NotSupportedException("金额保存只读取组织全量、指定组织的启用医院与指定医院的启用院区。");
  }
}
