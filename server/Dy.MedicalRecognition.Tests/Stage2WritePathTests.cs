using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Xml.Linq;
using Dy.Core.Abstractions.Data;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.Abstractions.EventBus;
using Dy.Earthrace.Abstractions;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 2 互认项目配置写入口的用例与领域行为校验：新增即为启用状态并登记创建事件、目录三层停用按层拒绝、
/// 可互认时间正整数与可构造边界、停用后仍可修改、启用前校验目录、重复启停幂等、组织与操作人只取自登录令牌、
/// 事件字段与失败不登记事件、条件更新影响行数判定（启用与停用的 0 行复读三分支、多行不变量）、
/// 跨组织归属拒绝、仓储层唯一约束冲突识别与其它数据库错误的继续传播，以及互认配置 SqlMap 的静态约定。
/// </summary>
/// <remarks>
/// 对应阶段 2 验证矩阵 V1、V3、V4、V6、V8、V9、V11、V16、V26、V27 中不依赖真实数据库的部分，以及四个写入口的 SqlMap 静态约定。
/// 真实数据库上的唯一约束冲突结果、并发交错与物理表运行证据属于阶段 2 的 V2、V15、V17、V26 运行面，不在本文件验证。
/// 本文件使用手写的最小测试替身：内存目录与配置数据、可脚本化的影响行数与读取序列、可观察的领域事件队列，
/// 以及按脚本抛出数据库异常的数据映射器替身。
/// </remarks>
public sealed class Stage2WritePathTests
{
  /// <summary>可信组织编码，代表登录令牌携带的当前组织。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>另一个组织编码，用于验证越组织标识被拒绝。</summary>
  private const string OtherOrganization = "ORG-B";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>命令携带的操作时间，用于核对事件时间来源于命令而不是数据库当前时间。</summary>
  private static readonly DateTimeOffset CommandOperTime = new(2026, 9, 15, 8, 30, 0, TimeSpan.Zero);

  /// <summary>可互认时间天数在测试中的合法取值。</summary>
  private const int ValidDurationDays = 30;

  /// <summary>构建写入口，注入领域管理器、互认配置读写仓储端口、外部组织服务与外部用户服务。</summary>
  /// <param name="repository">承载目录与配置数据的最小仓储替身。</param>
  /// <returns>可直接调用的写入口。</returns>
  private static MedicalRecognitionReportAppService CreateAppService(FakeReportRepository repository)
    => new(new MedicalRecognitionReportManager(repository, new FakeMatchQueryRepository()), repository, new StubOrganizationAppService(), new StubUserAppService(),
      new StubReportPdfFileStore(), new StubReportQueryRepository(), new StubSystemParameterAppService());

  /// <summary>构建领域管理器，用于绕过应用层直接校验领域规则。</summary>
  /// <param name="repository">承载目录与配置数据的最小仓储替身。</param>
  /// <returns>可直接调用的领域管理器。</returns>
  private static MedicalRecognitionReportManager CreateManager(FakeReportRepository repository) => new(repository, new FakeMatchQueryRepository());

  /// <summary>建立“分类、分组、标准项目均启用”的合法目录前置数据。</summary>
  /// <returns>已装载合法目录的仓储替身与其中的标准项目。</returns>
  private static (FakeReportRepository Repository, MedicalStandardItem Item) CreateEnabledCatalog()
  {
    FakeReportRepository repository = new();
    return (repository, AddEnabledCatalog(repository));
  }

  /// <summary>向仓储替身追加一棵启用的目录树，便于在同一用例中保留既有配置数据。</summary>
  /// <param name="repository">承载目录数据的最小仓储替身。</param>
  /// <param name="code">标准项目编码，同一用例内的目录树应使用不同编码。</param>
  /// <returns>新加入的标准项目。</returns>
  private static MedicalStandardItem AddEnabledCatalog(FakeReportRepository repository, string code = "P1")
  {
    MedicalStandardCategory category = new() { Id = Guid.NewGuid(), Name = "分类", IsValid = true };
    MedicalStandardGroup group = new() { Id = Guid.NewGuid(), CategoryId = category.Id, Name = "分组", IsValid = true };
    MedicalStandardItem item = new() { Id = Guid.NewGuid(), CategoryId = category.Id, GroupId = group.Id, Code = code, Name = "标准项目", IsValid = true };
    repository.Categories[category.Id] = category;
    repository.Groups[group.Id] = group;
    repository.Items[item.Id] = item;
    return item;
  }

  /// <summary>向仓储替身登记一条已存在的互认配置，用于修改与启停用例的前置数据。</summary>
  /// <param name="repository">承载配置数据的最小仓储替身。</param>
  /// <param name="item">该配置引用的标准项目。</param>
  /// <param name="organizationCode">配置归属组织编码。</param>
  /// <param name="isValid">配置当前启用状态。</param>
  /// <returns>新登记的配置。</returns>
  private static MutualRecognitionItem AddConfiguration(
    FakeReportRepository repository, MedicalStandardItem item, string organizationCode, bool isValid)
  {
    MutualRecognitionItem configuration = new()
    {
      Id = Guid.NewGuid(),
      OrganizationCode = organizationCode,
      StandardItemId = item.Id,
      StandardProjectCode = item.Code,
      RecognitionDurationDays = ValidDurationDays,
      IsValid = isValid,
      // 夹具直接放置已存在的配置，不经过写入口；因此操作字段留空：OperId = Guid.Empty 只用于约束验证（防御用途），
      // 不是正常业务可达的写入结果（写入口始终写入登录令牌的用户标识）。
      OperId = Guid.Empty,
      OperTime = CommandOperTime
    };
    repository.Configurations[configuration.Id] = configuration;
    return configuration;
  }

  /// <summary>构建新增命令，字段与写入口解析后的命令一致。</summary>
  /// <param name="item">服务端按编码解析出的标准项目。</param>
  /// <returns>可直接交给领域管理器的新增命令。</returns>
  private static CreateMutualRecognitionItemCommand CreateCommand(MedicalStandardItem item) => new()
  {
    OrganizationCode = TrustedOrganization,
    StandardItemId = item.Id,
    StandardProjectCode = item.Code,
    RecognitionDurationDays = ValidDurationDays,
    OperId = TrustedOperId,
    OperTime = CommandOperTime
  };

  /// <summary>构建修改可互认时间命令。</summary>
  /// <param name="configuration">已存在的配置。</param>
  /// <param name="durationDays">本次可互认时间天数。</param>
  /// <param name="organizationCode">命令携带的可信组织编码。</param>
  /// <returns>可直接交给领域管理器的修改命令。</returns>
  private static UpdateMutualRecognitionItemConfigurationCommand UpdateCommand(
    MutualRecognitionItem configuration, int durationDays, string organizationCode = TrustedOrganization) => new()
    {
      Id = configuration.Id,
      OrganizationCode = organizationCode,
      RecognitionDurationDays = durationDays,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };

  /// <summary>构建启用命令。</summary>
  /// <param name="configuration">目标配置。</param>
  /// <param name="organizationCode">命令携带的可信组织编码。</param>
  /// <returns>可直接交给领域管理器的启用命令。</returns>
  private static EnableMutualRecognitionItemCommand EnableCommand(
    MutualRecognitionItem configuration, string organizationCode = TrustedOrganization) => new()
    {
      Id = configuration.Id,
      OrganizationCode = organizationCode,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };

  /// <summary>构建停用命令。</summary>
  /// <param name="configuration">目标配置。</param>
  /// <param name="organizationCode">命令携带的可信组织编码。</param>
  /// <returns>可直接交给领域管理器的停用命令。</returns>
  private static DisableMutualRecognitionItemCommand DisableCommand(
    MutualRecognitionItem configuration, string organizationCode = TrustedOrganization) => new()
    {
      Id = configuration.Id,
      OrganizationCode = organizationCode,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };

  /// <summary>
  /// 取标准目录某一层停用时领域层应给出的业务拒绝完整文案。
  /// </summary>
  /// <remarks>三层停用是三种不同事实，只断言"已停用"子串时无法区分是哪一层被误判或被漏判。</remarks>
  /// <param name="disabledLayer">被停用的目录层级：<c>Item</c>、<c>Category</c> 或 <c>Group</c>。</param>
  /// <returns>该层级对应的业务拒绝完整文案。</returns>
  private static string ExpectedDisabledCatalogRejection(string disabledLayer) => disabledLayer switch
  {
    "Item" => "业务拒绝：标准项目已停用。",
    "Category" => "业务拒绝：所属分类已停用。",
    _ => "业务拒绝：所属分组已停用。"
  };

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

  /// <summary>在捕获领域事件的前提下顺序执行一组写操作与断言，避免用例中的多次调用缺少事件队列。</summary>
  /// <param name="operation">待执行的操作与断言。</param>
  /// <returns>操作完成后的异步任务。</returns>
  private static async Task WithCapturedEventsAsync(Func<Task> operation)
  {
    InstallRecordingEventQueue();
    try
    {
      await operation();
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
  /// 原因：框架事件队列由请求管道按请求绑定，测试进程没有请求管道，只能通过框架公开工厂之外的内部属性注入。
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

  /// <summary>
  /// V1：有效目录下新增配置成功、默认启用，并登记创建事件；事件的配置标识、组织、标准项目编码、
  /// 可互认时间与启用状态均来自命令与仓储生成的主键。
  /// </summary>
  [Fact]
  public async Task Create_with_valid_catalog_registers_creation_event_and_defaults_to_enabled()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (RecordingEventQueue events, bool result) = await ExecuteAsync(() => CreateManager(repository).CreateMutualRecognitionItemAsync(CreateCommand(item)));

    Assert.True(result);
    MutualRecognitionItem configuration = Assert.IsType<MutualRecognitionItem>(repository.LastCreated);
    Assert.True(configuration.IsValid);
    Assert.Equal(TrustedOrganization, configuration.OrganizationCode);
    Assert.Equal(item.Id, configuration.StandardItemId);
    Assert.Equal(item.Code, configuration.StandardProjectCode);
    Assert.Equal(ValidDurationDays, configuration.RecognitionDurationDays);

    MutualRecognitionItemCreatedEvent createdEvent = Assert.Single(events.Events.OfType<MutualRecognitionItemCreatedEvent>());
    Assert.Equal(configuration.Id, createdEvent.Id);
    Assert.Equal(TrustedOrganization, createdEvent.OrganizationCode);
    Assert.Equal(item.Code, createdEvent.StandardProjectCode);
    Assert.Equal(ValidDurationDays, createdEvent.RecognitionDurationDays);
    Assert.True(createdEvent.IsValid);
    Assert.Equal(TrustedOperId, createdEvent.EventCreator);
    Assert.Equal(CommandOperTime, createdEvent.EventCreatedTime);
    Assert.Equal(MedicalRecognitionReportConst.AggregateId, createdEvent.AggregateId);
    Assert.Equal(nameof(MutualRecognitionItemCreatedEvent), createdEvent.EventType);
  }

  /// <summary>写入口新增配置时按调用方提交的编码解析服务端目录标识，不接受调用方提交内部标识。</summary>
  [Fact]
  public async Task Create_resolves_project_code_to_server_side_item_id()
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (RecordingEventQueue events, bool result) = await ExecuteAsync(() => CreateAppService(repository).CreateMutualRecognitionItemAsync(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));

    Assert.True(result);
    Assert.Single(events.Events.OfType<MutualRecognitionItemCreatedEvent>());
    Assert.Equal(item.Id, repository.LastCreated!.StandardItemId);
    Assert.Equal(item.Code, repository.LastCreated.StandardProjectCode);
  }

  /// <summary>写入口提交未知标准项目编码时拒绝，不写入配置、不登记事件。</summary>
  [Fact]
  public async Task Create_with_unknown_project_code_is_rejected_without_write()
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedOperId.ToString());
    (FakeReportRepository repository, _) = CreateEnabledCatalog();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateAppService(repository).CreateMutualRecognitionItemAsync(
        new CreateMutualRecognitionItemRequest { StandardProjectCode = "NOT-EXIST", RecognitionDurationDays = ValidDurationDays }));

    Assert.Contains("标准项目不存在", error.Message);
    Assert.Equal(0, repository.CreateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V3：标准项目、所属分类或所属分组任一停用时，新增被拒绝、无事件、无写入。</summary>
  /// <param name="disabledLayer">被停用的目录层级。</param>
  [Theory]
  [InlineData("Item")]
  [InlineData("Category")]
  [InlineData("Group")]
  public async Task Create_is_rejected_without_event_when_any_catalog_layer_is_disabled(string disabledLayer)
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    switch (disabledLayer)
    {
      case "Item":
        item.IsValid = false;
        break;
      case "Category":
        repository.Categories[item.CategoryId].IsValid = false;
        break;
      default:
        repository.Groups[item.GroupId].IsValid = false;
        break;
    }

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).CreateMutualRecognitionItemAsync(CreateCommand(item)));

    // 按被停用的层级断言完整文案：三层停用是三种不同事实，用"已停用"子串断言无法区分是哪一层被误判或被漏判。
    Assert.Equal(ExpectedDisabledCatalogRejection(disabledLayer), error.Message);
    Assert.Equal(0, repository.CreateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V4：可互认时间只允许正整数，创建与修改请求都按同一天数规则拒绝 0 与负数，并拒绝缺失标准项目编码。</summary>
  [Fact]
  public void Duration_days_must_be_a_positive_integer_on_create_and_update_requests()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new CreateMutualRecognitionItemRequest { StandardProjectCode = "P1", RecognitionDurationDays = 0 }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new CreateMutualRecognitionItemRequest { StandardProjectCode = "P1", RecognitionDurationDays = -1 }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new CreateMutualRecognitionItemRequest { StandardProjectCode = null!, RecognitionDurationDays = 1 }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new UpdateMutualRecognitionItemConfigurationRequest { Id = Guid.NewGuid(), RecognitionDurationDays = 0 }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new UpdateMutualRecognitionItemConfigurationRequest { Id = Guid.Empty, RecognitionDurationDays = 1 }));

    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = "P1", RecognitionDurationDays = 1 })));
    // 可构造上界：int 契约的最大值必须被接受，避免范围校验的上界把合法输入拒掉。
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = "P1", RecognitionDurationDays = int.MaxValue })));
  }

  /// <summary>
  /// V4 的可构造边界：可互认时间下界 1 与上界 <see cref="int.MaxValue"/> 都必须通过请求校验并走完写链路，
  /// 使"边界值可达"有行为证据，而不只是校验层的断言。
  /// 矩阵中的"永久有效"与"小数"在 <c>int</c> 契约上不可构造：没有表示"永久有效"的专用取值（0 与负数被拒绝），
  /// 小数不能由整型字段提交；这两项无法构造边界用例，处置结论由阶段文档侧记录。
  /// </summary>
  /// <param name="durationDays">本次提交的可互认时间天数。</param>
  [Theory]
  [InlineData(1)]
  [InlineData(int.MaxValue)]
  public async Task Duration_days_boundaries_are_reachable_through_the_write_path(int durationDays)
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (RecordingEventQueue events, bool result) = await ExecuteAsync(() => CreateAppService(repository).CreateMutualRecognitionItemAsync(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = durationDays }));

    Assert.True(result);
    Assert.Equal(durationDays, repository.LastCreated!.RecognitionDurationDays);
    Assert.Equal(durationDays, Assert.Single(events.Events.OfType<MutualRecognitionItemCreatedEvent>()).RecognitionDurationDays);
  }

  /// <summary>V6：停用后的配置仍可修改可互认时间，状态保持停用，并登记修改事件。</summary>
  [Fact]
  public async Task Update_after_disable_keeps_state_and_registers_update_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).UpdateMutualRecognitionItemConfigurationAsync(UpdateCommand(configuration, 45)));

    Assert.True(result);
    Assert.Equal(45, repository.Configurations[configuration.Id].RecognitionDurationDays);
    Assert.False(repository.Configurations[configuration.Id].IsValid);

    MutualRecognitionItemConfigurationUpdatedEvent updatedEvent =
      Assert.Single(events.Events.OfType<MutualRecognitionItemConfigurationUpdatedEvent>());
    Assert.Equal(configuration.Id, updatedEvent.Id);
    Assert.Equal(TrustedOrganization, updatedEvent.OrganizationCode);
    Assert.Equal(item.Code, updatedEvent.StandardProjectCode);
    Assert.Equal(45, updatedEvent.RecognitionDurationDays);
    Assert.Equal(TrustedOperId, updatedEvent.EventCreator);
    Assert.Equal(CommandOperTime, updatedEvent.EventCreatedTime);
  }

  /// <summary>V16：修改同值也正常保存并登记修改事件。</summary>
  [Fact]
  public async Task Update_with_unchanged_duration_still_saves_and_registers_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).UpdateMutualRecognitionItemConfigurationAsync(UpdateCommand(configuration, ValidDurationDays)));

    Assert.True(result);
    Assert.Equal(1, repository.UpdateCalls);
    Assert.Single(events.Events.OfType<MutualRecognitionItemConfigurationUpdatedEvent>());
  }

  /// <summary>修改语句影响 0 行时拒绝，不登记事件、不伪造成功。</summary>
  [Fact]
  public async Task Update_with_zero_affected_rows_is_rejected_without_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).UpdateMutualRecognitionItemConfigurationAsync(UpdateCommand(configuration, 45)));

    // 修改语句影响 0 行有专属拒绝文案：与其他拒绝共用"业务拒绝"子串时，错文案不会被发现。
    Assert.Equal("业务拒绝：互认项目配置更新失败。", error.Message);
    Assert.Equal(ValidDurationDays, repository.Configurations[configuration.Id].RecognitionDurationDays);
    Assert.Empty(events.Events);
  }

  /// <summary>V8：启用前校验标准项目、分类与分组当前有效；目录停用时拒绝、状态不变、无事件。</summary>
  /// <param name="disabledLayer">被停用的目录层级。</param>
  [Theory]
  [InlineData("Item")]
  [InlineData("Category")]
  [InlineData("Group")]
  public async Task Enable_is_rejected_without_state_change_when_catalog_is_disabled(string disabledLayer)
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);
    switch (disabledLayer)
    {
      case "Item":
        item.IsValid = false;
        break;
      case "Category":
        repository.Categories[item.CategoryId].IsValid = false;
        break;
      default:
        repository.Groups[item.GroupId].IsValid = false;
        break;
    }

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    // 启用与新增按同一口径区分三层停用文案。
    Assert.Equal(ExpectedDisabledCatalogRejection(disabledLayer), error.Message);
    Assert.Equal(0, repository.EnableCalls);
    Assert.False(repository.Configurations[configuration.Id].IsValid);
    Assert.Empty(events.Events);
  }

  /// <summary>V7 的领域面：有效目录下启用成功并登记启用事件。</summary>
  [Fact]
  public async Task Enable_with_valid_catalog_changes_state_and_registers_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.True(result);
    Assert.True(repository.Configurations[configuration.Id].IsValid);

    MutualRecognitionItemEnabledEvent enabledEvent = Assert.Single(events.Events.OfType<MutualRecognitionItemEnabledEvent>());
    Assert.Equal(configuration.Id, enabledEvent.Id);
    Assert.Equal(TrustedOrganization, enabledEvent.OrganizationCode);
    Assert.Equal(item.Code, enabledEvent.StandardProjectCode);
    Assert.True(enabledEvent.IsValid);
    Assert.Equal(TrustedOperId, enabledEvent.EventCreator);
    Assert.Equal(CommandOperTime, enabledEvent.EventCreatedTime);
  }

  /// <summary>V9：重复启用与重复停用都幂等成功，不更新、不登记事件；目录停用不影响重复启用的幂等判定。</summary>
  [Fact]
  public async Task Repeated_enable_and_disable_are_idempotent_without_events()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem enabledConfiguration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    MutualRecognitionItem disabledConfiguration = AddConfiguration(repository, AddEnabledCatalog(repository, "P2"), TrustedOrganization, isValid: false);

    (RecordingEventQueue enableEvents, bool enableResult) = await ExecuteAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(enabledConfiguration)));

    Assert.True(enableResult);
    Assert.Equal(0, repository.EnableCalls);
    Assert.Empty(enableEvents.Events);

    (RecordingEventQueue disableEvents, bool disableResult) = await ExecuteAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(disabledConfiguration)));

    Assert.True(disableResult);
    Assert.Equal(0, repository.DisableCalls);
    Assert.Empty(disableEvents.Events);
  }

  /// <summary>停用成功时登记停用事件，事件状态为停用。</summary>
  [Fact]
  public async Task Disable_changes_state_and_registers_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(configuration)));

    Assert.True(result);
    Assert.False(repository.Configurations[configuration.Id].IsValid);

    MutualRecognitionItemDisabledEvent disabledEvent = Assert.Single(events.Events.OfType<MutualRecognitionItemDisabledEvent>());
    Assert.Equal(configuration.Id, disabledEvent.Id);
    Assert.Equal(TrustedOrganization, disabledEvent.OrganizationCode);
    Assert.Equal(item.Code, disabledEvent.StandardProjectCode);
    Assert.False(disabledEvent.IsValid);
    Assert.Equal(TrustedOperId, disabledEvent.EventCreator);
    Assert.Equal(CommandOperTime, disabledEvent.EventCreatedTime);
  }

  /// <summary>V2：同一组织与同一标准项目已存在配置时，新增入口在写入前按重复配置拒绝，不触发写语句、不登记事件。</summary>
  [Fact]
  public async Task Duplicate_configuration_is_rejected_before_write_without_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    // 停用配置同样占用"组织编码 + 标准项目编码"唯一键，因此查重不因配置已停用而放行。
    AddConfiguration(repository, item, TrustedOrganization, isValid: false);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).CreateMutualRecognitionItemAsync(CreateCommand(item)));

    Assert.Equal("业务拒绝：该组织已配置此标准项目。", error.Message);
    Assert.Equal(0, repository.CreateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V16：新增语句没有写入任何行时拒绝并且不登记创建事件。</summary>
  [Fact]
  public async Task Create_with_zero_affected_rows_is_rejected_without_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).CreateMutualRecognitionItemAsync(CreateCommand(item)));

    Assert.Equal("业务拒绝：互认项目配置保存失败。", error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：启用语句影响 0 行时按同一可信组织复读一次，复读已是目标状态则幂等成功且不登记事件。</summary>
  [Fact]
  public async Task Zero_row_enable_that_became_enabled_concurrently_is_idempotent_success()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);
    // 复读到的同一行已被并发请求改为启用，用于覆盖“0 行但已是目标状态”的幂等分支。
    MutualRecognitionItem concurrentState = new()
    {
      Id = configuration.Id,
      OrganizationCode = configuration.OrganizationCode,
      StandardItemId = configuration.StandardItemId,
      StandardProjectCode = configuration.StandardProjectCode,
      RecognitionDurationDays = configuration.RecognitionDurationDays,
      IsValid = true
    };
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(concurrentState);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.True(result);
    Assert.Equal(1, repository.EnableCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：启用语句影响 0 行且复读仍为停用时返回并发冲突，提示刷新后重试，不自动重试、不登记事件。</summary>
  [Fact]
  public async Task Zero_row_enable_that_is_still_disabled_is_a_concurrency_conflict()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.Contains("刷新后重试", error.Message);
    Assert.Equal(1, repository.EnableCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V26/V27：启用语句影响 0 行且复读已不属于当前可信组织时统一按配置不存在拒绝。</summary>
  [Fact]
  public async Task Zero_row_enable_that_no_longer_belongs_to_organization_is_rejected()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(null);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.Contains("配置不存在", error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：条件更新影响多行属于持久化不变量异常，不返回成功、不登记事件；启用与停用各有专属文案。</summary>
  [Fact]
  public async Task Conditional_update_affecting_multiple_rows_is_rejected_without_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: false);
    repository.ScriptedConfigurationWriteRows.Enqueue(2);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.Equal("业务拒绝：互认项目配置状态更新异常，未完成启用。", error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：停用语句影响 0 行时按同一可信组织复读一次，复读已是停用状态则幂等成功且不登记事件。</summary>
  [Fact]
  public async Task Zero_row_disable_that_became_disabled_concurrently_is_idempotent_success()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    // 复读到的同一行已被并发请求改为停用，用于覆盖“0 行但已是目标状态”的幂等分支。
    MutualRecognitionItem concurrentState = new()
    {
      Id = configuration.Id,
      OrganizationCode = configuration.OrganizationCode,
      StandardItemId = configuration.StandardItemId,
      StandardProjectCode = configuration.StandardProjectCode,
      RecognitionDurationDays = configuration.RecognitionDurationDays,
      IsValid = false
    };
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(concurrentState);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(configuration)));

    Assert.True(result);
    Assert.Equal(1, repository.DisableCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：停用语句影响 0 行且复读仍为启用时返回并发冲突，提示刷新后重试，不自动重试、不登记事件。</summary>
  [Fact]
  public async Task Zero_row_disable_that_is_still_enabled_is_a_concurrency_conflict()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(configuration)));

    Assert.Contains("刷新后重试", error.Message);
    Assert.Equal(1, repository.DisableCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V26/V27：停用语句影响 0 行且复读已不属于当前可信组织时统一按配置不存在拒绝。</summary>
  [Fact]
  public async Task Zero_row_disable_that_no_longer_belongs_to_organization_is_rejected()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    repository.ScriptedConfigurationReads.Enqueue(configuration);
    repository.ScriptedConfigurationReads.Enqueue(null);
    repository.ScriptedConfigurationWriteRows.Enqueue(0);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(configuration)));

    Assert.Equal("业务拒绝：互认项目配置不存在。", error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>V26：停用条件更新影响多行属于持久化不变量异常，不返回成功、不登记事件。</summary>
  [Fact]
  public async Task Conditional_disable_affecting_multiple_rows_is_rejected_without_event()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    repository.ScriptedConfigurationWriteRows.Enqueue(2);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).DisableMutualRecognitionItemAsync(DisableCommand(configuration)));

    Assert.Equal("业务拒绝：互认项目配置状态更新异常，未完成停用。", error.Message);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V9：配置已启用且标准目录三层都已停用时，重复启用仍按幂等成功返回。
  /// 被守护分支在读取配置后立即按"已是启用状态"返回，此时既不校验标准目录、也不更新、也不登记事件，
  /// 因此目录停用不会把重复启用变成目录校验失败。
  /// </summary>
  [Fact]
  public async Task Repeated_enable_is_idempotent_even_when_the_whole_catalog_is_disabled()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem configuration = AddConfiguration(repository, item, TrustedOrganization, isValid: true);
    item.IsValid = false;
    repository.Categories[item.CategoryId].IsValid = false;
    repository.Groups[item.GroupId].IsValid = false;

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).EnableMutualRecognitionItemAsync(EnableCommand(configuration)));

    Assert.True(result);
    Assert.Equal(0, repository.EnableCalls);
    Assert.True(repository.Configurations[configuration.Id].IsValid);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// 写入口的操作时间取自服务端 UTC 当前时间，操作人取自登录令牌的用户标识；
  /// 该断言只能证明写入时间的时区偏移为 0（UTC 主机上本地时间同样满足），不使用等待当前时间的方式。
  /// </summary>
  [Fact]
  public async Task Write_entrypoints_use_utc_operation_time()
  {
    DateTimeOffset before = DateTimeOffset.UtcNow;
    TrustedRequestContext.Use(TrustedOrganization, TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (_, bool result) = await ExecuteAsync(() => CreateAppService(repository).CreateMutualRecognitionItemAsync(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));

    Assert.True(result);
    DateTimeOffset operTime = repository.LastCreated!.OperTime;
    Assert.Equal(TimeSpan.Zero, operTime.Offset);
    Assert.InRange(operTime, before.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
  }

  /// <summary>V27：组织 A 修改、启用或停用组织 B 的配置均被拒绝，B 的配置与操作字段不变且无事件。</summary>
  [Fact]
  public async Task Cross_organization_configuration_id_is_rejected_for_every_write()
  {
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MutualRecognitionItem foreignConfiguration = AddConfiguration(repository, item, OtherOrganization, isValid: true);
    DateTimeOffset originalOperTime = foreignConfiguration.OperTime;
    Guid originalOperId = foreignConfiguration.OperId;

    MedicalRecognitionReportManager manager = CreateManager(repository);
    (RecordingEventQueue updateEvents, InvalidOperationException updateError) = await ExecuteExpectingRejectionAsync(
      () => manager.UpdateMutualRecognitionItemConfigurationAsync(UpdateCommand(foreignConfiguration, 45)));
    (RecordingEventQueue enableEvents, InvalidOperationException enableError) = await ExecuteExpectingRejectionAsync(
      () => manager.EnableMutualRecognitionItemAsync(EnableCommand(foreignConfiguration)));
    (RecordingEventQueue disableEvents, InvalidOperationException disableError) = await ExecuteExpectingRejectionAsync(
      () => manager.DisableMutualRecognitionItemAsync(DisableCommand(foreignConfiguration)));

    Assert.Contains("配置不存在", updateError.Message);
    Assert.Contains("配置不存在", enableError.Message);
    Assert.Contains("配置不存在", disableError.Message);
    Assert.Equal(0, repository.UpdateCalls);
    Assert.Equal(0, repository.EnableCalls);
    Assert.Equal(0, repository.DisableCalls);
    Assert.Equal(ValidDurationDays, foreignConfiguration.RecognitionDurationDays);
    Assert.True(foreignConfiguration.IsValid);
    Assert.Equal(originalOperTime, foreignConfiguration.OperTime);
    Assert.Equal(originalOperId, foreignConfiguration.OperId);
    Assert.Empty(updateEvents.Events);
    Assert.Empty(enableEvents.Events);
    Assert.Empty(disableEvents.Events);
  }

  /// <summary>V11：四个互认写入口的组织编码只来自登录令牌的组织声明，公共请求不提供可覆盖的组织字段。</summary>
  [Fact]
  public async Task Write_entrypoints_take_organization_from_trusted_context_only()
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    // 前置配置挂在第二个标准项目上：新增入口按组织与标准项目查重，与新增目标同项目会让新增命中重复拒绝。
    MutualRecognitionItem configuration = AddConfiguration(repository, AddEnabledCatalog(repository, "P2"), TrustedOrganization, isValid: false);
    MedicalRecognitionReportAppService appService = CreateAppService(repository);

    Assert.Null(typeof(CreateMutualRecognitionItemRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(UpdateMutualRecognitionItemConfigurationRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(EnableMutualRecognitionItemRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(DisableMutualRecognitionItemRequest).GetProperty("OrganizationCode"));

    await WithCapturedEventsAsync(async () =>
    {
      Assert.True(await appService.CreateMutualRecognitionItemAsync(
        new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));
      Assert.Equal(TrustedOrganization, repository.LastCreated!.OrganizationCode);
      Assert.Equal(TrustedOperId, repository.LastCreated.OperId);

      Assert.True(await appService.UpdateMutualRecognitionItemConfigurationAsync(
        new UpdateMutualRecognitionItemConfigurationRequest { Id = configuration.Id, RecognitionDurationDays = 45 }));
      Assert.Equal(TrustedOrganization, repository.LastUpdated!.OrganizationCode);
      Assert.Equal(TrustedOperId, repository.LastUpdated.OperId);

      Assert.True(await appService.EnableMutualRecognitionItemAsync(new EnableMutualRecognitionItemRequest { Id = configuration.Id }));
      Assert.Equal(TrustedOrganization, repository.LastEnableCommand!.OrganizationCode);
      Assert.Equal(TrustedOperId, repository.LastEnableCommand.OperId);

      Assert.True(await appService.DisableMutualRecognitionItemAsync(new DisableMutualRecognitionItemRequest { Id = configuration.Id }));
      Assert.Equal(TrustedOrganization, repository.LastDisableCommand!.OrganizationCode);
      Assert.Equal(TrustedOperId, repository.LastDisableCommand.OperId);
    });
  }

  /// <summary>
  /// 可信组织带首尾空白时按去除空白后的编码写入，配置归属与事件都取去除空白后的值；
  /// 该口径与查询入口共用同一个解析点，避免两侧对同一令牌组织得出不同结论。
  /// </summary>
  [Fact]
  public async Task Write_entrypoints_trim_the_trusted_organization()
  {
    TrustedRequestContext.Use($"  {TrustedOrganization}  ", TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (RecordingEventQueue events, bool result) = await ExecuteAsync(() => CreateAppService(repository).CreateMutualRecognitionItemAsync(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));

    Assert.True(result);
    Assert.Equal(TrustedOrganization, repository.LastCreated!.OrganizationCode);
    Assert.Equal(TrustedOrganization, Assert.Single(events.Events.OfType<MutualRecognitionItemCreatedEvent>()).OrganizationCode);
  }

  /// <summary>V11/V22：登录令牌的组织声明取不到组织编码时写入口拒绝，不写入、不登记事件。</summary>
  [Fact]
  public async Task Write_entrypoints_are_rejected_when_trusted_organization_is_missing()
  {
    TrustedRequestContext.Use("   ", TrustedOperId.ToString());
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();
    MedicalRecognitionReportAppService appService = CreateAppService(repository);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => appService.CreateMutualRecognitionItemAsync(
        new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));

    Assert.Contains("可信组织", error.Message);
    Assert.Equal(0, repository.CreateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>V11/V22：操作人不能解析为非空 Guid 时拒绝，不静默沿用默认操作人。</summary>
  [Fact]
  public async Task Write_entrypoints_are_rejected_when_operator_cannot_be_resolved()
  {
    TrustedRequestContext.Use(TrustedOrganization, "not-a-guid");
    (FakeReportRepository repository, MedicalStandardItem item) = CreateEnabledCatalog();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateAppService(repository).CreateMutualRecognitionItemAsync(
        new CreateMutualRecognitionItemRequest { StandardProjectCode = item.Code, RecognitionDurationDays = ValidDurationDays }));

    Assert.Contains("操作人", error.Message);
    Assert.Equal(0, repository.CreateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>四个互认写入口各保持最多一条写语句，不声明 <c>WorkUnitAttribute</c>。</summary>
  [Fact]
  public void Mutual_write_entrypoints_declare_no_work_unit()
  {
    Type appService = typeof(MedicalRecognitionReportAppService);
    foreach (string name in new[]
    {
      "CreateMutualRecognitionItemAsync", "UpdateMutualRecognitionItemConfigurationAsync",
      "EnableMutualRecognitionItemAsync", "DisableMutualRecognitionItemAsync"
    })
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }
  }

  /// <summary>
  /// 四个互认写入口先执行公共请求校验，再映射领域命令。
  /// 探针按完整签名绑定到唯一的方法声明，并用花括号配平切出方法体，不再按方法名文本宽松命中。
  /// </summary>
  [Fact]
  public void Mutual_write_entrypoints_validate_request_before_mapping()
  {
    string source = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Application", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportAppService.cs"));

    Assert.Empty(FindValidationOrderViolations(source));

    // 探针自校验：证明它按完整声明定位，改名或不存在的声明不会被误命中。
    string renamed = source.Replace(nameof(MedicalRecognitionReportAppService.CreateMutualRecognitionItemAsync), "CreateMutualRecognitionItemsAsync", StringComparison.Ordinal);
    Assert.NotEqual(source, renamed);
    Assert.Throws<InvalidOperationException>(() => FindValidationOrderViolations(renamed));

    // 探针自校验：移除公共请求校验后必须报“缺少校验”，证明断言不是恒真。
    string validationRemoved = source.Replace(
      "MedicalRecognitionRequestValidator.Validate(request);", "/* 变更：移除公共请求校验 */", StringComparison.Ordinal);
    Assert.NotEqual(source, validationRemoved);
    Assert.Contains(
      "CreateMutualRecognitionItemAsync: 缺少公共请求校验调用",
      FindValidationOrderViolations(validationRemoved));

    // 探针自校验：让命令映射先于公共请求校验出现时必须报“校验晚于映射”，证明顺序判定有效。
    string mappingBeforeValidation = validationRemoved
      .Replace("/* 变更：移除公共请求校验 */", "_ = request.MapToProbeMutation();\n    MedicalRecognitionRequestValidator.Validate(request);", StringComparison.Ordinal);
    Assert.Contains(
      "CreateMutualRecognitionItemAsync: 公共请求校验晚于命令映射",
      FindValidationOrderViolations(mappingBeforeValidation));
  }

  /// <summary>
  /// 四个互认写入口的完整声明签名；探针按签名绑定声明，避免同名方法或方法名文本被误命中。
  /// </summary>
  private static readonly (string Name, string Signature)[] MutualWriteEntrypointDeclarations =
  [
    (nameof(MedicalRecognitionReportAppService.CreateMutualRecognitionItemAsync),
      "public async Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemRequest request)"),
    (nameof(MedicalRecognitionReportAppService.UpdateMutualRecognitionItemConfigurationAsync),
      "public async Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationRequest request)"),
    (nameof(MedicalRecognitionReportAppService.EnableMutualRecognitionItemAsync),
      "public async Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemRequest request)"),
    (nameof(MedicalRecognitionReportAppService.DisableMutualRecognitionItemAsync),
      "public async Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemRequest request)")
  ];

  /// <summary>
  /// 检查四个互认写入口是否都先执行公共请求校验、再映射领域命令。
  /// </summary>
  /// <param name="source">应用服务的 C# 源码文本；可以是用例构造的变更副本。</param>
  /// <returns>违规描述集合；全部满足时为空的集合。</returns>
  /// <exception cref="InvalidOperationException">完整签名没有命中唯一方法声明，或方法体花括号无法配平时抛出。</exception>
  private static List<string> FindValidationOrderViolations(string source)
  {
    List<string> violations = [];
    foreach ((string name, string signature) in MutualWriteEntrypointDeclarations)
    {
      string body = ExtractDeclaredMethodBody(source, signature, name);
      int validateIndex = body.IndexOf("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal);
      int mapIndex = body.IndexOf(".MapTo", StringComparison.Ordinal);
      if (validateIndex < 0) violations.Add($"{name}: 缺少公共请求校验调用");
      else if (mapIndex >= 0 && validateIndex > mapIndex) violations.Add($"{name}: 公共请求校验晚于命令映射");
    }

    return violations;
  }

  /// <summary>
  /// 按完整签名定位唯一的方法声明，并用花括号配平提取该方法体。
  /// </summary>
  /// <param name="source">待检查的 C# 源码文本。</param>
  /// <param name="signature">含访问修饰符、返回类型、方法名与参数列表的完整声明文本。</param>
  /// <param name="entrypointName">方法名，用于定位违规与异常描述。</param>
  /// <returns>该方法声明的方法体文本，包含首尾花括号。</returns>
  /// <exception cref="InvalidOperationException">完整签名未命中、命中多个声明，或方法体花括号未配平时抛出。</exception>
  private static string ExtractDeclaredMethodBody(string source, string signature, string entrypointName)
  {
    int declarationIndex = source.IndexOf(signature, StringComparison.Ordinal);
    if (declarationIndex < 0) throw new InvalidOperationException($"{entrypointName}: 未找到与完整签名一致的方法声明 {signature}");
    if (source.IndexOf(signature, declarationIndex + signature.Length, StringComparison.Ordinal) >= 0)
      throw new InvalidOperationException($"{entrypointName}: 完整签名命中多个方法声明，探针无法绑定到唯一声明");

    int bodyStart = source.IndexOf('{', declarationIndex + signature.Length);
    if (bodyStart < 0) throw new InvalidOperationException($"{entrypointName}: 方法声明后未找到方法体起始花括号");

    int depth = 0;
    for (int index = bodyStart; index < source.Length; index++)
    {
      switch (source[index])
      {
        case '{':
          depth++;
          break;
        case '}':
          depth--;
          if (depth == 0) return source[bodyStart..(index + 1)];
          break;
        case '"':
          index = SkipQuotedText(source, index, '"');
          break;
        case '\'':
          index = SkipQuotedText(source, index, '\'');
          break;
        case '/':
          // 跳过注释，避免注释里的花括号影响配平。
          if (index + 1 < source.Length && source[index + 1] == '/') index = SkipToLineEnd(source, index);
          else if (index + 1 < source.Length && source[index + 1] == '*') index = SkipBlockComment(source, index);
          break;
      }
    }

    throw new InvalidOperationException($"{entrypointName}: 方法体花括号未配平，无法提取唯一声明的方法体");
  }

  /// <summary>跳过普通字符串或字符字面量，返回其结束引号的下标。</summary>
  /// <param name="source">待检查的 C# 源码文本。</param>
  /// <param name="quoteIndex">起始引号的下标。</param>
  /// <param name="quote">引号字符，普通字符串为双引号、字符字面量为单引号。</param>
  /// <returns>字面量结束引号的下标；未闭合时返回源码末位下标。</returns>
  private static int SkipQuotedText(string source, int quoteIndex, char quote)
  {
    for (int index = quoteIndex + 1; index < source.Length; index++)
    {
      if (source[index] == '\\')
      {
        index++;
        continue;
      }

      if (source[index] == quote) return index;
    }

    return source.Length - 1;
  }

  /// <summary>跳过单行注释，返回该行换行符的下标。</summary>
  /// <param name="source">待检查的 C# 源码文本。</param>
  /// <param name="commentIndex">注释起始双斜杠的下标。</param>
  /// <returns>该行换行符的下标；文件末尾没有换行时返回源码末位下标。</returns>
  private static int SkipToLineEnd(string source, int commentIndex)
  {
    int lineEnd = source.IndexOf('\n', commentIndex);
    return lineEnd < 0 ? source.Length - 1 : lineEnd;
  }

  /// <summary>跳过块注释，返回块结束符中斜杠的下标。</summary>
  /// <param name="source">待检查的 C# 源码文本。</param>
  /// <param name="commentIndex">注释起始斜杠星号的下标。</param>
  /// <returns>块注释结束符 `*/` 中斜杠的下标；未闭合时返回源码末位下标。</returns>
  private static int SkipBlockComment(string source, int commentIndex)
  {
    int commentEnd = source.IndexOf("*/", commentIndex + 2, StringComparison.Ordinal);
    return commentEnd < 0 ? source.Length - 1 : commentEnd + 1;
  }

  /// <summary>
  /// V17 的静态面：互认配置 SqlMap 使用显式作用域、只指向 <c>mrec_</c> 物理表、显式绑定命令操作时间，
  /// 且修改与启停语句都带可信组织与目标原状态条件。
  /// </summary>
  [Fact]
  public void Mutual_sql_map_targets_physical_table_and_guards_organization_and_state()
  {
    XDocument document = XDocument.Load(FindRepositorySqlMap("MutualRecognitionItem.xml"));
    Assert.Equal("MutualRecognitionItem", (string?)document.Root!.Attribute("Scope"));

    Dictionary<string, string> statements = document.Descendants()
      .Where(element => element.Attribute("Id") is not null)
      .ToDictionary(element => (string)element.Attribute("Id")!, element => element.Value);

    foreach (string statement in statements.Values)
    {
      Assert.DoesNotContain(" current_timestamp", statement, StringComparison.OrdinalIgnoreCase);
      // 无前缀表名判据不能只匹配"空格 + 表名"：换行、左括号与 join 之后的表名同样必须被认出。
      Assert.Equal(0, CountUnprefixedTableNames(statement));
    }

    // 变异证据一：把创建语句副本里的 mrec_ 前缀去掉后，同一条判据必须报出违规（该表名出现在换行与左括号之后）。
    string createStatement = statements["CreateMutualRecognitionItem"];
    string unprefixedCreateStatement = createStatement.Replace(
      "mrec_mutual_recognition_item", "mutual_recognition_item", StringComparison.Ordinal);
    Assert.NotEqual(createStatement, unprefixedCreateStatement);
    Assert.Equal(1, CountUnprefixedTableNames(unprefixedCreateStatement));

    // 判据的形态覆盖：换行之后、左括号之后与 join 之后的无前缀表名都必须计入，带 mrec_ 前缀的不计入。
    Assert.Equal(1, CountUnprefixedTableNames("select id\nfrom mutual_recognition_item"));
    Assert.Equal(1, CountUnprefixedTableNames("select count(1) from (mutual_recognition_item) t"));
    Assert.Equal(1, CountUnprefixedTableNames(
      "select i.id from mrec_medical_standard_item i join mutual_recognition_item m on m.standard_item_id = i.id"));
    Assert.Equal(0, CountUnprefixedTableNames("select id from mrec_mutual_recognition_item"));

    // 目标表名精确等值：写语句只能指向互认配置的物理表，表名被改名或写成别的表都必须被发现。
    Assert.Equal("mrec_mutual_recognition_item", StatementTargetTable(statements["CreateMutualRecognitionItem"], "insert into"));
    Assert.Equal("mrec_mutual_recognition_item", StatementTargetTable(statements["UpdateMutualRecognitionItemConfiguration"], "update"));
    Assert.Equal("mrec_mutual_recognition_item", StatementTargetTable(statements["EnableMutualRecognitionItem"], "update"));
    Assert.Equal("mrec_mutual_recognition_item", StatementTargetTable(statements["DisableMutualRecognitionItem"], "update"));

    // 变异证据二：把创建语句副本的目标表换成别的表后，等值判据必须判出不相等。
    string retargetedCreateStatement = createStatement.Replace(
      "mrec_mutual_recognition_item", "mrec_other_item", StringComparison.Ordinal);
    Assert.NotEqual(
      "mrec_mutual_recognition_item",
      StatementTargetTable(retargetedCreateStatement, "insert into"));

    Assert.Contains("$OperTime", statements["CreateMutualRecognitionItem"], StringComparison.Ordinal);
    Assert.Contains("is_valid,", statements["CreateMutualRecognitionItem"], StringComparison.OrdinalIgnoreCase);
    // 创建语句必须把启用状态写成常量 true：只断言列名与"不引用请求参数"时，把 true 改成 false 不会让任何用例失败。
    Assert.True(CreateStatementWritesEnabledConstant(statements["CreateMutualRecognitionItem"]));
    // 变异证据三：把创建语句副本里的常量 true 改成 false 后，同一条判据必须判为"没有固定写入启用状态"。
    string disabledCreateStatement = createStatement.Replace("true", "false", StringComparison.OrdinalIgnoreCase);
    Assert.NotEqual(createStatement, disabledCreateStatement);
    Assert.False(CreateStatementWritesEnabledConstant(disabledCreateStatement));
    Assert.DoesNotContain("$IsValid", statements["CreateMutualRecognitionItem"], StringComparison.Ordinal);
    Assert.Contains("organization_code = $OrganizationCode", statements["UpdateMutualRecognitionItemConfiguration"], StringComparison.Ordinal);
    Assert.Contains("id = $Id", statements["UpdateMutualRecognitionItemConfiguration"], StringComparison.Ordinal);
    Assert.Contains("organization_code = $OrganizationCode", statements["EnableMutualRecognitionItem"], StringComparison.Ordinal);
    Assert.Contains("is_valid = false", statements["EnableMutualRecognitionItem"], StringComparison.OrdinalIgnoreCase);
    Assert.Contains("organization_code = $OrganizationCode", statements["DisableMutualRecognitionItem"], StringComparison.Ordinal);
    Assert.Contains("is_valid = true", statements["DisableMutualRecognitionItem"], StringComparison.OrdinalIgnoreCase);
    Assert.Contains("organization_code = $OrganizationCode", statements["GetMutualRecognitionItemById"], StringComparison.Ordinal);
    Assert.Equal("mrec_mutual_recognition_item", StatementTargetTable(statements["GetMutualRecognitionItemById"], "select", "from"));
  }

  /// <summary>
  /// 统计语句中未加 <c>mrec_</c> 前缀的互认配置表名出现次数。
  /// </summary>
  /// <remarks>
  /// 判据按"表名出现且其前面不是 <c>mrec_</c>"判定，而不是匹配"空格 + 表名"的子串：
  /// 表名可能紧跟在换行、左括号或 join 之后，带前导空格的子串判定会把这三种形态全部漏掉。
  /// </remarks>
  /// <param name="statement">SqlMap 语句文本。</param>
  /// <returns>未加前缀的表名出现次数；只出现带 <c>mrec_</c> 前缀的物理表名时为 0。</returns>
  private static int CountUnprefixedTableNames(string statement)
  {
    const string tableName = "mutual_recognition_item";
    const string prefix = "mrec_";
    int count = 0;
    for (int index = statement.IndexOf(tableName, StringComparison.Ordinal); index >= 0; index = statement.IndexOf(tableName, index + tableName.Length, StringComparison.Ordinal))
    {
      bool prefixed = index >= prefix.Length && string.CompareOrdinal(statement, index - prefix.Length, prefix, 0, prefix.Length) == 0;
      if (!prefixed) count++;
    }

    return count;
  }

  /// <summary>
  /// 取写语句或读取语句的目标表名，用于按等值断言冻结物理表。
  /// </summary>
  /// <param name="statement">语句文本。</param>
  /// <param name="verb">语句起始动词，例如 <c>insert into</c>、<c>update</c>、<c>select</c>。</param>
  /// <param name="tableMarker">取表名前的定位标记：写语句为动词本身，读取语句为 <c>from</c>。</param>
  /// <returns>动词（或 <c>from</c>）之后的表名。</returns>
  /// <exception cref="InvalidOperationException">语句不以该动词开头，或该动词之后找不到定位标记时抛出，避免判据在别的语句形态上静默取到空值。</exception>
  private static string StatementTargetTable(string statement, string verb, string? tableMarker = null)
  {
    string trimmed = statement.Trim();
    if (!trimmed.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException($"语句应以 {verb} 开头，实际为：{trimmed[..Math.Min(40, trimmed.Length)]}。");
    }

    string remainder = trimmed[verb.Length..];
    string marker = tableMarker ?? string.Empty;
    if (marker.Length > 0)
    {
      int markerIndex = remainder.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      if (markerIndex < 0) throw new InvalidOperationException($"语句中未找到表名定位标记 {marker}。");
      remainder = remainder[(markerIndex + marker.Length)..];
    }

    return remainder.TrimStart().Split(' ')[0];
  }

  /// <summary>
  /// 判断创建语句是否把启用状态固定写成常量 <c>true</c> 并且不引用请求参数。
  /// </summary>
  /// <param name="statement">创建语句文本。</param>
  /// <returns>包含启用列、写入常量 true 且不引用 <c>$IsValid</c> 时为 <see langword="true"/>。</returns>
  private static bool CreateStatementWritesEnabledConstant(string statement) =>
    statement.Contains("is_valid,", StringComparison.OrdinalIgnoreCase) &&
    statement.Contains("true", StringComparison.OrdinalIgnoreCase) &&
    !statement.Contains("$IsValid", StringComparison.Ordinal);

  /// <summary>互认配置 SqlMap 的每条语句都有说明职责的 XML 注释。</summary>
  [Fact]
  public void Mutual_sql_map_statements_are_documented()
  {
    XDocument document = XDocument.Load(FindRepositorySqlMap("MutualRecognitionItem.xml"));
    IEnumerable<XElement> statements = document.Descendants().Where(element => element.Attribute("Id") is not null);

    Assert.All(statements, statement => Assert.True(statement.PreviousNode is XComment, $"语句 {statement.Attribute("Id")?.Value} 缺少 XML 注释"));
  }

  /// <summary>标准项目 SqlMap 新增按编码读取语句，供应用层把业务编码解析为服务端目录标识。</summary>
  [Fact]
  public void Standard_item_sql_map_exposes_code_lookup()
  {
    XDocument document = XDocument.Load(FindRepositorySqlMap("MedicalStandardItem.xml"));
    XElement statement = document.Descendants().Single(element => (string?)element.Attribute("Id") == "GetMedicalStandardItemByCode");

    Assert.Contains("mrec_medical_standard_item", statement.Value, StringComparison.Ordinal);
    Assert.Contains("code = $Code", statement.Value, StringComparison.Ordinal);
    Assert.True(statement.PreviousNode is XComment);
  }

  /// <summary>
  /// 本用例读取的映射文件及其固定分组目录，相对 <c>server/Dy.MedicalRecognition.Repository</c>。
  /// </summary>
  private static readonly Dictionary<string, string> SqlMapDirectories = new(StringComparer.Ordinal)
  {
    ["MutualRecognitionItem.xml"] = "MedicalRecognitionReportAggregate",
    ["MedicalStandardItem.xml"] = "MedicalRecognitionReportAggregate"
  };

  /// <summary>在映射文件的固定分组目录内定位 SqlMap 文件。</summary>
  /// <param name="fileName">SqlMap 文件名，例如 <c>MutualRecognitionItem.xml</c>。</param>
  /// <returns>该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="InvalidOperationException">未冻结该文件的分组目录时抛出。</exception>
  /// <exception cref="FileNotFoundException">冻结的分组目录内不存在该文件时抛出。</exception>
  private static string FindRepositorySqlMap(string fileName) =>
    SourceSyntaxGuard.FindRepositoryFile(
      SqlMapDirectories.TryGetValue(fileName, out string? directory)
        ? directory
        : throw new InvalidOperationException($"未冻结映射文件 '{fileName}' 的分组目录。"),
      fileName);

  /// <summary>
  /// 记录领域层登记事件的测试替身，用于断言哪些成功路径登记了事件、哪些幂等或失败路径没有登记。
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
  /// 互认配置读写的内存测试替身：提供目录与配置数据、可信组织范围过滤、可脚本化的读取序列与影响行数，
  /// 并记录写语句调用次数与最后一次写入内容，用于断言归属、幂等与影响行数判定。
  /// </summary>
  private sealed class FakeReportRepository : IMedicalRecognitionReportRepository
  {
    /// <summary>内存分类目录。</summary>
    public Dictionary<Guid, MedicalStandardCategory> Categories { get; } = [];

    /// <summary>内存分组目录。</summary>
    public Dictionary<Guid, MedicalStandardGroup> Groups { get; } = [];

    /// <summary>内存标准项目目录。</summary>
    public Dictionary<Guid, MedicalStandardItem> Items { get; } = [];

    /// <summary>内存互认配置数据。</summary>
    public Dictionary<Guid, MutualRecognitionItem> Configurations { get; } = [];

    /// <summary>按顺序脚本化配置读取结果，用于模拟两次读取之间发生的并发状态变化；为空时按内存数据读取。</summary>
    public Queue<MutualRecognitionItem?> ScriptedConfigurationReads { get; } = [];

    /// <summary>按顺序脚本化写语句影响行数；为空时按内存数据写出并返回 1。</summary>
    public Queue<int> ScriptedConfigurationWriteRows { get; } = [];

    /// <summary>新增语句调用次数。</summary>
    public int CreateCalls { get; private set; }

    /// <summary>修改语句调用次数。</summary>
    public int UpdateCalls { get; private set; }

    /// <summary>启用语句调用次数。</summary>
    public int EnableCalls { get; private set; }

    /// <summary>停用语句调用次数。</summary>
    public int DisableCalls { get; private set; }

    /// <summary>最后一次新增写入的配置。</summary>
    public MutualRecognitionItem? LastCreated { get; private set; }

    /// <summary>最后一次修改写入的配置。</summary>
    public MutualRecognitionItem? LastUpdated { get; private set; }

    /// <summary>最后一次启用命令。</summary>
    public EnableMutualRecognitionItemCommand? LastEnableCommand { get; private set; }

    /// <summary>最后一次停用命令。</summary>
    public DisableMutualRecognitionItemCommand? LastDisableCommand { get; private set; }

    /// <summary>生成一个新的配置主键。</summary>
    /// <returns>新的主键标识。</returns>
    public Guid CreateGuid() => Guid.NewGuid();

    /// <summary>按标准项目主键读取标准项目。</summary>
    /// <param name="id">标准项目标识。</param>
    /// <returns>标准项目；不存在时返回 <see langword="null"/>。</returns>
    public Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id)
      => Task.FromResult(Items.TryGetValue(id, out MedicalStandardItem? item) ? item : null);

    /// <summary>按标准项目编码读取标准项目，用于应用层解析服务端目录标识。</summary>
    /// <param name="code">标准项目编码。</param>
    /// <returns>标准项目；不存在时返回 <see langword="null"/>。</returns>
    public Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code)
      => Task.FromResult(Items.Values.SingleOrDefault(item => item.Code == code));

    /// <summary>按主键读取分类。</summary>
    /// <param name="id">分类标识。</param>
    /// <returns>分类；不存在时返回 <see langword="null"/>。</returns>
    public Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id)
      => Task.FromResult(Categories.TryGetValue(id, out MedicalStandardCategory? category) ? category : null);

    /// <summary>按主键读取分组。</summary>
    /// <param name="id">分组标识。</param>
    /// <returns>分组；不存在时返回 <see langword="null"/>。</returns>
    public Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id)
      => Task.FromResult(Groups.TryGetValue(id, out MedicalStandardGroup? group) ? group : null);

    /// <summary>在可信组织范围内按主键读取互认配置，模拟数据库的组织范围过滤。</summary>
    /// <param name="id">配置标识。</param>
    /// <param name="organizationCode">可信组织编码。</param>
    /// <returns>配置；不存在或不属于该组织时返回 <see langword="null"/>。</returns>
    public Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode)
    {
      if (ScriptedConfigurationReads.Count > 0) return Task.FromResult(ScriptedConfigurationReads.Dequeue());
      return Task.FromResult(
        Configurations.TryGetValue(id, out MutualRecognitionItem? configuration) && configuration.OrganizationCode == organizationCode
          ? configuration
          : null);
    }

    /// <summary>插入一条互认配置，模拟影响行数。</summary>
    /// <param name="mutualRecognitionItem">待插入配置。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem mutualRecognitionItem)
    {
      CreateCalls++;
      LastCreated = mutualRecognitionItem;
      if (ScriptedConfigurationWriteRows.Count > 0)
      {
        int rows = ScriptedConfigurationWriteRows.Dequeue();
        if (rows > 0) Configurations[mutualRecognitionItem.Id] = mutualRecognitionItem;
        return Task.FromResult(rows);
      }

      Configurations[mutualRecognitionItem.Id] = mutualRecognitionItem;
      return Task.FromResult(1);
    }

    /// <summary>按标识与组织更新可互认时间，模拟影响行数与组织范围条件。</summary>
    /// <param name="mutualRecognitionItem">待更新配置。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem mutualRecognitionItem)
    {
      UpdateCalls++;
      LastUpdated = mutualRecognitionItem;
      if (ScriptedConfigurationWriteRows.Count > 0) return Task.FromResult(ScriptedConfigurationWriteRows.Dequeue());
      if (!Configurations.TryGetValue(mutualRecognitionItem.Id, out MutualRecognitionItem? configuration)
        || configuration.OrganizationCode != mutualRecognitionItem.OrganizationCode) return Task.FromResult(0);

      configuration.RecognitionDurationDays = mutualRecognitionItem.RecognitionDurationDays;
      configuration.OperId = mutualRecognitionItem.OperId;
      configuration.OperTime = mutualRecognitionItem.OperTime;
      return Task.FromResult(1);
    }

    /// <summary>按标识、组织与原状态条件启用配置，模拟条件更新影响行数。</summary>
    /// <param name="enableMutualRecognitionItemCommand">启用命令。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand enableMutualRecognitionItemCommand)
    {
      EnableCalls++;
      LastEnableCommand = enableMutualRecognitionItemCommand;
      return Task.FromResult(ApplyConditionalStateChange(enableMutualRecognitionItemCommand.Id, enableMutualRecognitionItemCommand.OrganizationCode, targetIsValid: true));
    }

    /// <summary>按标识、组织与原状态条件停用配置，模拟条件更新影响行数。</summary>
    /// <param name="disableMutualRecognitionItemCommand">停用命令。</param>
    /// <returns>受影响行数。</returns>
    public Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand disableMutualRecognitionItemCommand)
    {
      DisableCalls++;
      LastDisableCommand = disableMutualRecognitionItemCommand;
      return Task.FromResult(ApplyConditionalStateChange(disableMutualRecognitionItemCommand.Id, disableMutualRecognitionItemCommand.OrganizationCode, targetIsValid: false));
    }

    /// <summary>模拟带原状态条件的更新：仅当前状态与目标状态相反时更新 1 行。</summary>
    /// <param name="id">配置标识。</param>
    /// <param name="organizationCode">可信组织编码。</param>
    /// <param name="targetIsValid">本次目标启用状态。</param>
    /// <returns>受影响行数。</returns>
    private int ApplyConditionalStateChange(Guid id, string organizationCode, bool targetIsValid)
    {
      if (ScriptedConfigurationWriteRows.Count > 0) return ScriptedConfigurationWriteRows.Dequeue();
      if (!Configurations.TryGetValue(id, out MutualRecognitionItem? configuration)
        || configuration.OrganizationCode != organizationCode
        || configuration.IsValid == targetIsValid) return 0;

      configuration.IsValid = targetIsValid;
      return 1;
    }

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

    /// <summary>按组织与标准项目编码读取互认配置，模拟新增配置的重复查重。</summary>
    /// <param name="organizationCode">组织编码。</param>
    /// <param name="standardProjectCode">标准项目编码。</param>
    /// <returns>已存在的配置；不存在时返回 <see langword="null"/>。</returns>
    public Task<MutualRecognitionItem?> GetMutualRecognitionItemByOrganizationAndProjectAsync(string organizationCode, string standardProjectCode) =>
      Task.FromResult(Configurations.Values.SingleOrDefault(configuration =>
        configuration.OrganizationCode == organizationCode && configuration.StandardProjectCode == standardProjectCode));

    /// <summary>本测试未使用：按四个业务键读取金额记录。</summary>
    /// <param name="organizationCode">组织编码。</param>
    /// <param name="hospitalCode">医院编码。</param>
    /// <param name="branchCode">院区编码。</param>
    /// <param name="standardProjectCode">标准项目编码。</param>
    /// <returns>不返回结果。</returns>
    public Task<OrganizationHospitalBranchRecognitionAmount?> GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
      string organizationCode, string hospitalCode, string branchCode, string standardProjectCode) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增金额记录。</summary>
    /// <param name="organizationHospitalBranchRecognitionAmount">金额记录实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：更新金额记录。</summary>
    /// <param name="organizationHospitalBranchRecognitionAmount">金额记录实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateOrganizationHospitalBranchRecognitionAmountAsync(OrganizationHospitalBranchRecognitionAmount organizationHospitalBranchRecognitionAmount) => throw new NotSupportedException(UnusedMember);

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

    /// <summary>本测试未使用：新增互认匹配记录。</summary>
    /// <param name="recognitionMatchRecord">互认匹配记录实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateRecognitionMatchRecordAsync(RecognitionMatchRecord recognitionMatchRecord) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增互认匹配项。</summary>
    /// <param name="recognitionMatchItem">互认匹配项实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateRecognitionMatchItemAsync(RecognitionMatchItem recognitionMatchItem) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按标识集合读取互认匹配项。</summary>
    /// <param name="recognitionMatchItemIds">互认匹配项标识集合。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按标识集合读取互认匹配记录。</summary>
    /// <param name="recognitionMatchRecordIds">互认匹配记录标识集合。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionMatchRecord>> QueryRecognitionMatchRecordsByIdsAsync(IReadOnlyList<Guid> recognitionMatchRecordIds) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按匹配项标识集合读取互认处理结果。</summary>
    /// <param name="recognitionMatchItemIds">互认匹配项标识集合。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按匹配项标识集合读取互认引用事实。</summary>
    /// <param name="recognitionMatchItemIds">互认匹配项标识集合。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionReference>> QueryRecognitionReferencesByMatchItemIdsAsync(IReadOnlyList<Guid> recognitionMatchItemIds) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增互认引用事实。</summary>
    /// <param name="recognitionReference">互认引用事实实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateRecognitionReferenceAsync(RecognitionReference recognitionReference) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按所属记录读取处理结果。</summary>
    /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionProcessingResult>> QueryRecognitionProcessingResultsByRecordAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按主键读取互认匹配记录。</summary>
    /// <param name="id">互认匹配记录标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<RecognitionMatchRecord?> GetRecognitionMatchRecordByIdAsync(Guid id) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：按所属记录读取互认匹配项。</summary>
    /// <param name="recognitionMatchRecordId">所属互认匹配记录标识。</param>
    /// <returns>不返回结果。</returns>
    public Task<IReadOnlyList<RecognitionMatchItem>> QueryRecognitionMatchItemsByRecordAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：新增互认处理结果。</summary>
    /// <param name="recognitionProcessingResult">互认处理结果实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> CreateRecognitionProcessingResultAsync(RecognitionProcessingResult recognitionProcessingResult) => throw new NotSupportedException(UnusedMember);

    /// <summary>本测试未使用：写入匹配记录的处理结果保存时间。</summary>
    /// <param name="recognitionMatchRecord">互认匹配记录实体。</param>
    /// <returns>不返回结果。</returns>
    public Task<int> UpdateRecognitionMatchRecordDecisionSavedTimeAsync(RecognitionMatchRecord recognitionMatchRecord) => throw new NotSupportedException(UnusedMember);

    /// <summary>未使用的仓储成员统一提示，避免测试静默走过未被覆盖的写入路径。</summary>
    private const string UnusedMember = "本测试未使用该仓储成员。";
  }

}
