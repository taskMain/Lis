using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Application.Contracts.PlatformParameter.Requests;
using Dy.MedicalRecognition.Application.PlatformParameter;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// T02 票 10 的平台参数自管用例：读取缺失、非法与停用，维护保存的新增与更新，保存后立即生效，
/// 以及平台参数脚本与映射的静态判据（矩阵 V11～V13）。
/// </summary>
/// <remarks>
/// 读取与保存共用同一个参数值规则；保存入口使用可控的 IAM 当前上下文与仓储替身，验证操作人、操作时间和写入形态。
/// 真实数据库与真实工作单元的取证需要负责人先执行建表脚本，当前用例只冻结应用层与脚本文本。
/// </remarks>
public sealed class T02PlatformParameterTests
{
  /// <summary>本院报告匹配开关的参数编码。</summary>
  private const string MatchSwitchCode = PlatformParameterDefinition.OwnHospitalMatchSwitch;

  /// <summary>本院报告排除时长的参数编码。</summary>
  private const string ExcludeHoursCode = PlatformParameterDefinition.OwnHospitalExcludeHours;

  /// <summary>引用详情有效时长的参数编码。</summary>
  private const string CitationHoursCode = PlatformParameterDefinition.CitationDetailValidHours;

  /// <summary>
  /// V11：匹配开关缺失、停用与非法取值分别拒绝，合法值按字符串 1/0 解析。
  /// </summary>
  [Fact]
  public async Task Match_switch_read_rejects_missing_disabled_and_invalid_values()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterReader reader = new(repository);

    InvalidOperationException missing = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalMatchSwitchAsync());
    Assert.Equal("业务拒绝：未配置本院报告匹配开关。", missing.Message);

    repository.Set(MatchSwitchCode, "2");
    InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalMatchSwitchAsync());
    Assert.Equal("业务拒绝：本院报告匹配开关取值非法。", invalid.Message);

    repository.Set(MatchSwitchCode, "1", isValid: false);
    InvalidOperationException disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalMatchSwitchAsync());
    Assert.Equal("业务拒绝：未配置本院报告匹配开关。", disabled.Message);

    repository.Set(MatchSwitchCode, "1");
    Assert.True(await reader.ReadOwnHospitalMatchSwitchAsync());

    repository.Set(MatchSwitchCode, "0");
    Assert.False(await reader.ReadOwnHospitalMatchSwitchAsync());
  }

  /// <summary>
  /// V11：排除时长缺失、停用与非法取值分别拒绝，合法值允许零与非负整数。
  /// </summary>
  [Fact]
  public async Task Exclude_hours_read_rejects_missing_disabled_and_invalid_values()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterReader reader = new(repository);

    InvalidOperationException missing = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalExcludeHoursAsync());
    Assert.Equal("业务拒绝：未配置本院报告排除时长。", missing.Message);

    foreach (string invalidValue in new[] { "-1", "1.5", "abc" })
    {
      repository.Set(ExcludeHoursCode, invalidValue);
      InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalExcludeHoursAsync());
      Assert.Equal("业务拒绝：本院报告排除时长取值非法。", invalid.Message);
    }

    repository.Set(ExcludeHoursCode, "0", isValid: false);
    InvalidOperationException disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadOwnHospitalExcludeHoursAsync());
    Assert.Equal("业务拒绝：未配置本院报告排除时长。", disabled.Message);

    repository.Set(ExcludeHoursCode, "0");
    Assert.Equal(0, await reader.ReadOwnHospitalExcludeHoursAsync());

    repository.Set(ExcludeHoursCode, "24");
    Assert.Equal(24, await reader.ReadOwnHospitalExcludeHoursAsync());
  }

  /// <summary>
  /// V11：引用详情有效时长缺失、停用与非法取值分别拒绝，合法值只接受正整数。
  /// </summary>
  [Fact]
  public async Task Citation_hours_read_rejects_missing_disabled_and_invalid_values()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterReader reader = new(repository);

    InvalidOperationException missing = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadCitationDetailValidHoursAsync());
    Assert.Equal("业务拒绝：未配置引用详情有效时长。", missing.Message);

    foreach (string invalidValue in new[] { "0", "-1", "1.5", "abc" })
    {
      repository.Set(CitationHoursCode, invalidValue);
      InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadCitationDetailValidHoursAsync());
      Assert.Equal("业务拒绝：引用详情有效时长取值非法。", invalid.Message);
    }

    repository.Set(CitationHoursCode, "6", isValid: false);
    InvalidOperationException disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadCitationDetailValidHoursAsync());
    Assert.Equal("业务拒绝：未配置引用详情有效时长。", disabled.Message);

    repository.Set(CitationHoursCode, "6");
    Assert.Equal(6, await reader.ReadCitationDetailValidHoursAsync());
  }

  /// <summary>
  /// V12：每次读取都从参数存储取当前值，连续两次读取之间修改参数值会立即生效。
  /// </summary>
  [Fact]
  public async Task Reader_sees_the_current_value_on_every_read()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterReader reader = new(repository);
    repository.Set(CitationHoursCode, "6");

    Assert.Equal(6, await reader.ReadCitationDetailValidHoursAsync());

    repository.Set(CitationHoursCode, "2");

    Assert.Equal(2, await reader.ReadCitationDetailValidHoursAsync());
    Assert.Equal(2, repository.ReadCount);
  }

  /// <summary>
  /// V12：保存入口对三个参数分别执行与读取相同的取值校验，非法值在写入前被拒绝。
  /// </summary>
  /// <param name="parameterCode">参数编码。</param>
  /// <param name="invalidValue">不合法的参数值。</param>
  [Theory]
  [InlineData(MatchSwitchCode, "2")]
  [InlineData(ExcludeHoursCode, "-1")]
  [InlineData(ExcludeHoursCode, "abc")]
  [InlineData(CitationHoursCode, "0")]
  [InlineData(CitationHoursCode, "abc")]
  public async Task Save_rejects_invalid_values_before_writing(string parameterCode, string invalidValue)
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SavePlatformParameterAsync(parameterCode, new SavePlatformParameterRequest
      {
        ParameterValue = invalidValue,
        IsValid = true
      }));

    Assert.Equal($"业务拒绝：{PlatformParameterDefinition.GetDisplayName(parameterCode)}取值非法。", error.Message);
    Assert.Equal(0, repository.CreateCount);
    Assert.Equal(0, repository.UpdateCount);
  }

  /// <summary>
  /// V12：维护保存拒绝未登记的参数编码，不写入任何记录。
  /// </summary>
  [Fact]
  public async Task Save_rejects_an_unknown_parameter_code()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.SavePlatformParameterAsync("unknown_parameter", new SavePlatformParameterRequest
      {
        ParameterValue = "1",
        IsValid = true
      }));

    Assert.Equal("业务拒绝：参数编码不存在。", error.Message);
    Assert.Equal(0, repository.CreateCount);
    Assert.Equal(0, repository.UpdateCount);
  }

  /// <summary>
  /// V12：编码不存在时执行新增，操作人取 IAM 当前用户，操作时间取本次保存的服务端当前时间。
  /// </summary>
  [Fact]
  public async Task Save_creates_a_missing_parameter_with_the_current_operator_and_time()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);
    DateTimeOffset before = DateTimeOffset.UtcNow;

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(CitationHoursCode, new SavePlatformParameterRequest
    {
      ParameterValue = "8",
      Description = "引用详情有效期",
      IsValid = true
    }));
    DateTimeOffset after = DateTimeOffset.UtcNow;

    PlatformParameter created = Assert.IsType<PlatformParameter>(repository.LastCreated);
    Assert.Equal(CitationHoursCode, created.ParameterCode);
    Assert.Equal("8", created.ParameterValue);
    Assert.Equal("引用详情有效期", created.Description);
    Assert.True(created.IsValid);
    Assert.Equal(FakeIamScope.UserId, created.OperId);
    Assert.InRange(created.OperTime, before, after);
    Assert.Equal(1, repository.CreateCount);
    Assert.Equal(0, repository.UpdateCount);
    Assert.Equal(1, repository.ReadCount);
    Assert.Equal([CitationHoursCode], repository.ReadCodes);
  }

  /// <summary>
  /// V12：编码已存在时执行更新，保留原主键，并刷新参数值与操作字段。
  /// </summary>
  [Fact]
  public async Task Save_updates_an_existing_parameter_and_keeps_its_identity()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);
    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(CitationHoursCode, new SavePlatformParameterRequest
    {
      ParameterValue = "8",
      IsValid = true
    }));
    Guid originalId = repository.LastCreated!.Id;

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(CitationHoursCode, new SavePlatformParameterRequest
    {
      ParameterValue = "2",
      Description = "改为两小时",
      IsValid = false
    }));

    PlatformParameter updated = Assert.IsType<PlatformParameter>(repository.LastUpdated);
    Assert.Equal(originalId, updated.Id);
    Assert.Equal(CitationHoursCode, updated.ParameterCode);
    Assert.Equal("2", updated.ParameterValue);
    Assert.False(updated.IsValid);
    Assert.Equal(FakeIamScope.UserId, updated.OperId);
    Assert.Equal(1, repository.CreateCount);
    Assert.Equal(1, repository.UpdateCount);
  }

  /// <summary>
  /// V12：保存后同一读取入口立即看到新值，连续两次保存与读取之间没有旧值缓存。
  /// </summary>
  [Fact]
  public async Task Save_takes_effect_on_the_next_read_without_caching()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);
    PlatformParameterReader reader = new(repository);

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(MatchSwitchCode, new SavePlatformParameterRequest { ParameterValue = "0", IsValid = true }));
    Assert.False(await reader.ReadOwnHospitalMatchSwitchAsync());

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(MatchSwitchCode, new SavePlatformParameterRequest { ParameterValue = "1", IsValid = true }));
    Assert.True(await reader.ReadOwnHospitalMatchSwitchAsync());
  }

  /// <summary>
  /// V13：保存为停用后，业务读取按参数缺失拒绝，不使用旧值。
  /// </summary>
  [Fact]
  public async Task Saved_disabled_parameter_is_rejected_by_business_read()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);
    PlatformParameterReader reader = new(repository);

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(CitationHoursCode, new SavePlatformParameterRequest { ParameterValue = "6", IsValid = true }));
    Assert.Equal(6, await reader.ReadCitationDetailValidHoursAsync());

    await WithEventQueueAsync(() => service.SavePlatformParameterAsync(CitationHoursCode, new SavePlatformParameterRequest { ParameterValue = "6", IsValid = false }));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadCitationDetailValidHoursAsync());
    Assert.Equal("业务拒绝：未配置引用详情有效时长。", error.Message);
  }

  /// <summary>
  /// V12：维护列表按参数编码升序返回全部记录；空表返回空集合。
  /// </summary>
  [Fact]
  public async Task Query_list_returns_all_parameters_in_code_order()
  {
    FakePlatformParameterRepository repository = new();
    PlatformParameterMaintenanceAppService service = new(repository, FakeIamScope.Default);
    repository.Set(CitationHoursCode, "6");
    repository.Set(ExcludeHoursCode, "0");
    repository.Set(MatchSwitchCode, "1");

    IReadOnlyList<Dy.MedicalRecognition.Application.Contracts.PlatformParameter.PlatformParameterReadModel> parameters =
      await service.QueryPlatformParameterListAsync();

    Assert.Equal([CitationHoursCode, ExcludeHoursCode, MatchSwitchCode], parameters.Select(parameter => parameter.ParameterCode));
    Assert.All(parameters, parameter => Assert.True(parameter.IsValid));

    FakePlatformParameterRepository emptyRepository = new();
    PlatformParameterMaintenanceAppService emptyService = new(emptyRepository, FakeIamScope.Default);
    Assert.Empty(await emptyService.QueryPlatformParameterListAsync());
  }

  /// <summary>
  /// V12：维护控制器以版本前缀暴露列表查询与按编码保存两个动作。
  /// </summary>
  [Fact]
  public void Maintenance_controller_exposes_the_list_and_single_save_routes()
  {
    CompilationUnitSyntax controller = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition", "Controllers", "PlatformParameterController.cs");
    ClassDeclarationSyntax declaration = controller.DescendantNodes().OfType<ClassDeclarationSyntax>()
      .Single(type => type.Identifier.ValueText == "PlatformParameterController");

    string[] routes =
    [
      .. declaration.AttributeLists.SelectMany(list => list.Attributes)
        .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "Route")
        .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)
    ];
    Assert.Equal(["api/v1/platform-parameters"], routes);

    MethodDeclarationSyntax query = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(method => method.Identifier.ValueText == "QueryPlatformParameterListAsync");
    Assert.Contains(query.AttributeLists.SelectMany(list => list.Attributes),
      attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpGet");

    MethodDeclarationSyntax save = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(method => method.Identifier.ValueText == "SavePlatformParameterAsync");
    AttributeSyntax put = Assert.Single(
      save.AttributeLists.SelectMany(list => list.Attributes),
      attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpPut");
    Assert.Equal("{parameterCode}", SourceSyntaxGuard.LiteralStringArgument(put));
  }

  /// <summary>
  /// V12：建表脚本与可重复迁移脚本声明同一张表、同一唯一索引和同样的列集合。
  /// </summary>
  [Fact]
  public void Table_script_and_repeatable_migration_declare_the_same_platform_parameter_table()
  {
    string tableScript = ReadRepositoryFile("Scripts", "mrec_platform_parameter.sql");
    string migration = ReadRepositoryFile("Scripts/Migrations", "20261023_t02_platform_parameter.sql");

    string[] expectedColumns =
    [
      "id uuid not null",
      "parameter_code text not null",
      "parameter_value text not null",
      "description text",
      "is_valid boolean not null",
      "oper_time timestamptz not null",
      "oper_id uuid not null"
    ];
    foreach (string column in expectedColumns)
    {
      Assert.Contains(column, tableScript, StringComparison.Ordinal);
      Assert.Contains(column, migration, StringComparison.Ordinal);
    }

    Assert.Contains("create unique index ux_mrec_platform_parameter_code", tableScript, StringComparison.Ordinal);
    Assert.Contains("create unique index if not exists ux_mrec_platform_parameter_code", migration, StringComparison.Ordinal);
    Assert.Contains("create table if not exists mrec_platform_parameter", migration, StringComparison.Ordinal);
    Assert.DoesNotContain("drop ", migration, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// V12：映射声明按编码读取、全量读取、新增与更新四条语句，写入语句声明布尔参数处理器。
  /// </summary>
  [Fact]
  public void Sql_map_declares_the_parameter_write_and_read_statements()
  {
    XDocument document = XDocument.Load(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Repository",
      "PlatformParameterAggregate", "PlatformParameter.xml"));
    string[] statementIds =
    [
      .. document.Descendants().Where(element => element.Attribute("Id") is not null)
        .Select(element => (string)element.Attribute("Id")!)
    ];

    Assert.Contains("PlatformParameterColumns", statementIds);
    Assert.Contains("GetPlatformParameterByCode", statementIds);
    Assert.Contains("QueryAllPlatformParameter", statementIds);
    Assert.Contains("CreatePlatformParameter", statementIds);
    Assert.Contains("UpdatePlatformParameter", statementIds);
    Assert.Contains("PlatformParameterWriteParameters", document.ToString(), StringComparison.Ordinal);
    Assert.Contains("order by parameter_code asc", document.ToString(), StringComparison.Ordinal);
  }

  /// <summary>
  /// 读取仓库内文件。
  /// </summary>
  /// <param name="relativeSegments">相对仓库根的路径片段。</param>
  /// <returns>文件文本。</returns>
  private static string ReadRepositoryFile(string relativeDirectory, string fileName) =>
    File.ReadAllText(SourceSyntaxGuard.FindRepositoryFile(relativeDirectory, fileName));

  /// <summary>
  /// 在安装记录用事件队列的前提下执行一次保存调用。
  /// </summary>
  /// <param name="operation">待执行的保存调用。</param>
  private static async Task WithEventQueueAsync(Func<Task> operation)
  {
    PropertyInfo eventQueueProperty = typeof(EventBusFactory).GetProperty(
      nameof(EventBusFactory.CurrentEventQueue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
    eventQueueProperty.SetValue(null, new RecordingEventQueue());
    try
    {
      await operation();
    }
    finally
    {
      eventQueueProperty.SetValue(null, null);
    }
  }

  /// <summary>
  /// 记录领域层登记事件的测试替身，用于让保存入口的事件登记有可用的框架事件队列。
  /// </summary>
  private sealed class RecordingEventQueue : IEventQueue
  {
    /// <summary>本次测试期间登记的事件。</summary>
    public List<IEvent> Events { get; } = [];

    /// <inheritdoc/>
    public void Enqueue<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Add(eventData);

    /// <inheritdoc/>
    public void Remove<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Remove(eventData);

    /// <inheritdoc/>
    public void RemoveEvent(IEvent eventData) => Events.Remove(eventData);

    /// <inheritdoc/>
    public void Remove<TEventData>() where TEventData : IEvent => Events.RemoveAll(@event => @event is TEventData);

    /// <inheritdoc/>
    public Task FlushAsync() => Task.CompletedTask;

    /// <summary>清空已登记事件。</summary>
    public void Clear() => Events.Clear();
  }
}