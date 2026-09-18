using System.Reflection;
using System.Text.Json;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 报告提交入口与下载入口的应用编排层校验：文件键可读性、下载文件缺失与已作废报告历史版本下载、
/// 成功与失败的可观察结果（矩阵 V89、V77、V78 的应用层，V17 与 V18 在应用层的可测部分）。
/// </summary>
/// <remarks>
/// 用例把手写仓储替身、组织服务替身与文件存储替身注入公开应用服务入口，验证可观察结果：
/// 文件键不可读时拒绝且零业务写入、成功时报告与版本落库、失败时不登记提交完成事件。
/// 真实存储目录、真实数据库与真实宿主入口的完整链路证据由阶段 12 的宿主验收负责，不在本文件内验证。
/// </remarks>
public sealed class Stage4SubmissionPathTests
{
  /// <summary>可信组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>可信医院编码。</summary>
  private const string TrustedHospital = "HOS-A";

  /// <summary>可信院区编码。</summary>
  private const string TrustedBranch = "BRH-A";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>
  /// V79：写库失败后既有版本与其文件不受影响。
  /// </summary>
  /// <remarks>
  /// 矩阵预期含三面：失败后库中无版本记录、本次新文件已删除、既有版本与其文件不受影响。
  /// 第一面的「失败后库中无版本记录」由工作单元的事务回滚保证，本用例注入的是内存替身、不承载事务，
  /// 因此该面的证据是真实入口取证（缺陷八修复后的复测），不在本文件覆盖；第二面（新文件删除）由宿主控制器的失败补偿负责，
  /// 同样以真实入口取证为准。本用例只补第三面：先建立一份既有报告与版本作为基线，再让一次追加版本的提交在写库阶段失败，
  /// 断言既有版本行逐字段未变、且其文件键仍可读，未被失败路径的清理误伤。
  /// </remarks>
  [Fact]
  public async Task Failed_submission_leaves_existing_version_and_its_file_untouched()
  {
    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: ["existing/key.pdf"]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    // 基线：一份既有报告与其既有版本，文件键可读。
    fileStore.NextSavedFileKey = "existing/key.pdf";
    await SubmitAsync(appService, "existing/key.pdf");
    MedicalReportVersion existingVersion = Assert.Single(repository.Versions.Values);
    Guid existingVersionId = existingVersion.Id;
    string existingFileKey = existingVersion.PdfFileId;
    int contentCountBefore = repository.LaboratoryContents.Count;

    // 让追加版本的提交在写库阶段失败：版本行写入之后的内容写入被拒绝。
    repository.FailLaboratoryContentWrite = true;

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => SubmitAsync(appService, "existing/key.pdf"));

    Assert.StartsWith("业务拒绝：", error.Message, StringComparison.Ordinal);

    // 失败路径没有写入专项内容。
    Assert.Equal(contentCountBefore, repository.LaboratoryContents.Count);

    // 第三面：既有版本行未被改写，其文件键仍可读，未被失败路径的清理误伤。
    MedicalReportVersion afterFailure = repository.Versions[existingVersionId];
    Assert.Equal(existingVersion.VersionNumber, afterFailure.VersionNumber);
    Assert.Equal(existingVersion.ReportTime, afterFailure.ReportTime);
    Assert.Equal(existingFileKey, afterFailure.PdfFileId);
    Assert.Contains(existingFileKey, fileStore.ReadableFileKeys);
  }

  /// <summary>
  /// V89：提交请求携带不可读的文件键时拒绝，不产生版本与业务数据，既有报告与文件不受影响。
  /// </summary>
  /// <remarks>
  /// 文件键可读性由应用入口在进入领域写入之前确认；因此用例断言仓储没有收到任何写入调用，
  /// 且已保存的既有报告与版本保持不变。
  /// </remarks>
  [Fact]
  public async Task Submit_with_unreadable_file_key_is_rejected_without_business_writes()
  {    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: []);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    // 先用一个可读文件键建立既有报告与版本，作为"既有数据不受影响"的基线。
    fileStore.ReadableFileKeys.Add("existing/key.pdf");
    await SubmitAsync(appService, "existing/key.pdf");

    int reportCountBefore = repository.Reports.Count;
    int versionCountBefore = repository.Versions.Count;

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => SubmitAsync(appService, "missing/key.pdf"));

    Assert.Equal("业务拒绝：报告 PDF 文件不存在。", error.Message);
    // 不可读文件键在进入领域写入之前就被拒绝，因此没有新增报告、版本或患者。
    Assert.Equal(reportCountBefore, repository.Reports.Count);
    Assert.Equal(versionCountBefore, repository.Versions.Count);
    Assert.Single(repository.Patients);
  }

  /// <summary>
  /// V17 的应用层：提交成功时报告、版本与专项内容都已写入，且返回真。
  /// </summary>
  [Fact]
  public async Task Submit_writes_report_version_and_content_when_file_key_is_readable()
  {
    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: ["key/one.pdf"]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    bool result = await SubmitAsync(appService, "key/one.pdf");

    Assert.True(result);
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(TrustedOrganization, report.OrganizationCode);
    Assert.Equal(TrustedHospital, report.HospitalCode);
    Assert.Equal(TrustedBranch, report.BranchCode);
    Assert.Equal(MedicalReportLifecycleStatus.Effective, report.Status);
    MedicalReportVersion version = Assert.Single(repository.Versions.Values);
    Assert.Equal(report.CurrentVersionId, version.Id);
    Assert.Equal(1, version.VersionNumber);
    Assert.Single(repository.LaboratoryContents);
    Assert.Equal(2, repository.LaboratoryResultItems.Count);
    // 提交入口把文件键与下载名交给命令，版本保存的就是这两个取值。
    Assert.Equal("key/one.pdf", version.PdfFileId);
    Assert.Equal("lab-1.pdf", version.PdfFileName);
  }

  /// <summary>
  /// V18 的应用层：请求校验失败时不进入领域写入，也不读取任何业务数据。
  /// </summary>
  /// <remarks>
  /// 文档原文缺失、文件键缺失与下载名缺失都在公共请求校验处被拒绝。
  /// </remarks>
  [Theory]
  [InlineData("", "key/one.pdf", "lab-1.pdf")]
  [InlineData("   ", "key/one.pdf", "lab-1.pdf")]
  [InlineData("{\"reportNo\":\"R-1\"}", "", "lab-1.pdf")]
  [InlineData("{\"reportNo\":\"R-1\"}", "key/one.pdf", "")]
  public async Task Submit_request_validation_rejects_before_domain_writes(string reportJson, string fileKey, string fileName)
  {
    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: [fileKey]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
      () => WithEventQueueAsync(() => appService.SubmitCompleteLaboratoryReportAsync(new CompleteReportSubmissionRequest
      {
        ReportJson = reportJson,
        PdfFileId = fileKey,
        PdfFileName = fileName
      })));

    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
    Assert.Empty(repository.Patients);
  }

  /// <summary>
  /// 提交入口：报告文档不能反序列化为合法报告结构时拒绝，此时不读取文件、也不写入业务数据。
  /// </summary>
  /// <remarks>
  /// 未知字段与非法 JSON 都在反序列化阶段被拒绝，因此文件存储的读取调用次数为零，业务数据零写入。
  /// 结构合法但缺少业务必填字段的文档由领域校验拒绝，其拒绝时机另行覆盖。
  /// </remarks>
  [Theory]
  [InlineData("{\"reportNo\":\"R-1\",\"unknownField\":1}")]
  [InlineData("not-json")]
  public async Task Submit_rejects_invalid_report_document_before_file_access(string reportJson)
  {
    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: ["key/one.pdf"]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    await Assert.ThrowsAnyAsync<Exception>(
      () => appService.SubmitCompleteLaboratoryReportAsync(new CompleteReportSubmissionRequest
      {
        ReportJson = reportJson,
        PdfFileId = "key/one.pdf",
        PdfFileName = "lab-1.pdf"
      }));

    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
    // 文档非法时不读取文件：文件存储的读取调用次数为零。
    Assert.Equal(0, fileStore.OpenReadCalls);
  }

  /// <summary>
  /// 提交入口：文档可反序列化但缺少业务必填字段时由领域校验拒绝，且不产生任何业务数据。
  /// </summary>
  [Fact]
  public async Task Submit_rejects_structurally_empty_report_document_without_business_writes()
  {
    FakeReportRepository repository = new();
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: ["key/one.pdf"]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => WithEventQueueAsync(() => appService.SubmitCompleteLaboratoryReportAsync(new CompleteReportSubmissionRequest
      {
        ReportJson = "{}",
        PdfFileId = "key/one.pdf",
        PdfFileName = "lab-1.pdf"
      })));

    Assert.StartsWith("业务拒绝：", error.Message, StringComparison.Ordinal);
    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
    Assert.Empty(repository.Patients);
  }

  /// <summary>
  /// V77：下载时文件缺失按业务拒绝表达，不返回文件内容。
  /// </summary>
  /// <remarks>
  /// 归属与版本定位都已成立、只有存储中的文件不存在，因此该拒绝只表达文件缺失这一事实。
  /// 文件缺失由存储端口抛出，应用层不把它转成空流或成功。
  /// </remarks>
  [Fact]
  public async Task Download_with_missing_file_is_rejected_without_returning_content()
  {
    FakeReportRepository repository = new();
    Guid reportId = Guid.NewGuid();
    Guid reportVersionId = Guid.NewGuid();
    SyntheticReportPorts ports = new()
    {
      VersionFile = new MedicalReportVersionFileItem
      {
        ReportId = reportId,
        ReportVersionId = reportVersionId,
        OrganizationCode = TrustedOrganization,
        HospitalCode = TrustedHospital,
        BranchCode = TrustedBranch,
        PdfFileId = "key/deleted.pdf",
        PdfFileName = "v1.pdf",
        ReportStatus = MedicalReportLifecycleStatus.Effective
      }
    };
    // 存储端口视为可读文件键为空：该版本记录存在，但存储里没有对应文件。
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: []);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore, ports);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => appService.OpenReportVersionPdfAsync(new MedicalReportVersionPdfQueryRequest
      {
        ReportId = reportId,
        ReportVersionId = reportVersionId
      }));

    Assert.Equal("业务拒绝：报告 PDF 文件不存在。", error.Message);
  }

  /// <summary>
  /// V78：已作废报告的历史版本仍可下载，作废不改变文件键与下载名。
  /// </summary>
  /// <remarks>
  /// 下载路径只校验可信归属与版本定位，不按报告生命周期状态拦截；
  /// 因此报告作废后其历史版本的文件流与下载名与作废前一致。
  /// </remarks>
  [Fact]
  public async Task Download_of_voided_report_version_is_still_allowed()
  {
    FakeReportRepository repository = new();
    Guid reportId = Guid.NewGuid();
    Guid reportVersionId = Guid.NewGuid();
    const string fileKey = "key/voided.pdf";
    SyntheticReportPorts ports = new()
    {
      // 报告已作废，但版本与文件记录仍存在。
      VersionFile = new MedicalReportVersionFileItem
      {
        ReportId = reportId,
        ReportVersionId = reportVersionId,
        OrganizationCode = TrustedOrganization,
        HospitalCode = TrustedHospital,
        BranchCode = TrustedBranch,
        PdfFileId = fileKey,
        PdfFileName = "作废前版本.pdf",
        ReportStatus = MedicalReportLifecycleStatus.Voided
      }
    };
    RecordingReportPdfFileStore fileStore = new(readableFileKeys: [fileKey]);
    MedicalRecognitionReportAppService appService = CreateAppService(repository, fileStore, ports);

    ReportPdfDownload download = await appService.OpenReportVersionPdfAsync(new MedicalReportVersionPdfQueryRequest
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId
    });

    using Stream content = download.Content;
    Assert.Equal("作废前版本.pdf", download.FileName);
    Assert.NotNull(content);
  }

  /// <summary>
  /// 构建提交入口，注入仓储替身、文件存储替身、组织服务替身与可信请求上下文。
  /// </summary>
  /// <param name="repository">报告读写内存替身。</param>
  /// <param name="fileStore">报告 PDF 存储替身。</param>
  /// <returns>可直接调用的提交入口。</returns>
  private static MedicalRecognitionReportAppService CreateAppService(FakeReportRepository repository, RecordingReportPdfFileStore fileStore) =>
    CreateAppService(repository, fileStore, new SyntheticReportPorts());

  /// <summary>
  /// 构建应用入口，并允许注入只读查询端口替身以覆盖下载路径。
  /// </summary>
  /// <param name="repository">报告读写内存替身。</param>
  /// <param name="fileStore">报告 PDF 存储替身。</param>
  /// <param name="ports">只读查询端口替身，提供版本文件信息与报告来源归属。</param>
  /// <returns>可直接调用的应用入口。</returns>
  private static MedicalRecognitionReportAppService CreateAppService(
    FakeReportRepository repository, RecordingReportPdfFileStore fileStore, SyntheticReportPorts ports)
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());
    return new MedicalRecognitionReportAppService(
      new MedicalRecognitionReportManager(repository),
      repository,
      new StubOrganizationAppService
      {
        Organizations =
        [
          new OrganizationDto { Id = TrustedOrganization, Name = "示范组织", IsValid = true }
        ],
        HospitalsByOrganization = { [TrustedOrganization] = [new HospitalDto { Id = TrustedHospital, Name = "示范医院", OrgId = TrustedOrganization, IsValid = true }] },
        BranchesByHospital = { [TrustedHospital] = [new BranchDto { Id = TrustedBranch, Name = "示范院区", HosId = TrustedHospital, OrgId = TrustedOrganization, IsValid = true }] }
      },
      new StubUserAppService(),
      fileStore,
      ports);
  }

  /// <summary>
  /// 用一份合法的完整检验报告文档调用提交入口，并在提交期间安装记录用事件队列。
  /// </summary>
  /// <param name="appService">提交入口。</param>
  /// <param name="fileKey">本次提交使用的文件键。</param>
  /// <returns>提交入口的返回值。</returns>
  private static async Task<bool> SubmitAsync(MedicalRecognitionReportAppService appService, string fileKey)
  {
    InstallEventQueue();
    try
    {
      return await appService.SubmitCompleteLaboratoryReportAsync(new CompleteReportSubmissionRequest
      {
        ReportJson = BuildReportJson(),
        PdfFileId = fileKey,
        PdfFileName = "lab-1.pdf"
      });
    }
    finally
    {
      UninstallEventQueue();
    }
  }

  /// <summary>
  /// 在安装记录用事件队列的前提下执行一次提交入口调用。
  /// </summary>
  /// <remarks>
  /// 领域层登记事件依赖框架的事件队列，测试进程没有请求管道，因此按既有测试的做法注入记录用队列。
  /// </remarks>
  /// <param name="operation">待执行的提交调用。</param>
  /// <returns>提交入口的返回值。</returns>
  private static async Task<bool> WithEventQueueAsync(Func<Task<bool>> operation)
  {
    InstallEventQueue();
    try
    {
      return await operation();
    }
    finally
    {
      UninstallEventQueue();
    }
  }

  /// <summary>把记录用事件队列挂到框架的当前事件队列位置。</summary>
  private static void InstallEventQueue() => EventQueueProperty.SetValue(null, new RecordingEventQueue());

  /// <summary>清空测试进程的当前事件队列，避免用例之间相互影响。</summary>
  private static void UninstallEventQueue() => EventQueueProperty.SetValue(null, null);

  /// <summary>框架当前事件队列属性；测试需要写入其内部 setter 才能观察领域事件登记。</summary>
  private static readonly PropertyInfo EventQueueProperty = typeof(EventBusFactory)
    .GetProperty(nameof(EventBusFactory.CurrentEventQueue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

  /// <summary>
  /// 记录领域层登记事件的测试替身；本文件只用于让事件登记可用，不校验事件内容。
  /// </summary>
  private sealed class RecordingEventQueue : IEventQueue
  {
    /// <summary>本次测试期间登记的事件，按登记顺序保存。</summary>
    public List<IEvent> Events { get; } = [];

    /// <summary>记录一个待发布事件。</summary>
    /// <param name="eventData">登记的领域事件。</param>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Enqueue<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Add(eventData);

    /// <summary>按对象移除事件。</summary>
    /// <param name="eventData">待移除事件。</param>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Remove<TEventData>(TEventData eventData) where TEventData : IEvent => Events.Remove(eventData);

    /// <summary>按接口移除事件。</summary>
    /// <param name="eventData">待移除事件。</param>
    public void RemoveEvent(IEvent eventData) => Events.Remove(eventData);

    /// <summary>按类型批量移除事件。</summary>
    /// <typeparam name="TEventData">事件类型。</typeparam>
    public void Remove<TEventData>() where TEventData : IEvent => Events.RemoveAll(@event => @event is TEventData);

    /// <summary>记录队列不在测试中发布事件，仅观察登记结果。</summary>
    /// <returns>已完成的任务。</returns>
    public Task FlushAsync() => Task.CompletedTask;

    /// <summary>清空已登记事件。</summary>
    public void Clear() => Events.Clear();
  }

  /// <summary>
  /// 构造一份满足必填与成对要求的完整检验报告 JSON 文档。
  /// </summary>
  /// <returns>camelCase 的完整报告文档原文。</returns>
  private static string BuildReportJson()
  {
    DateTime reportTime = new(2020, 9, 20, 10, 0, 0, DateTimeKind.Unspecified);
    LaboratoryReportVersionRequest document = new()
    {
      ReportNo = "LAB-1",
      Version = new MedicalReportVersionRequest
      {
        SourceReportName = "报告名称",
        PatientName = "张三",
        PatientGenderCode = "1",
        PatientBirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
        IdentityDocumentTypeCode = "01",
        IdentityDocumentNo = "110101199001011234",
        VisitType = VisitType.Outpatient,
        VisitSerialNo = "V-1",
        ApplicationDeptId = "D-1",
        ApplicationDeptName = "检验科",
        ApplicationDoctorId = "DOC-A",
        ApplicationDoctorName = "申请医生",
        ExecutionDeptId = "D-2",
        ExecutionDeptName = "检验科",
        ReportDeptId = "D-3",
        ReportDeptName = "检验科",
        ReportDoctorId = "DOC-B",
        ReportDoctorName = "报告医生",
        ReviewDoctorId = "DOC-C",
        ReviewDoctorName = "审核医生",
        ApplicationTime = new DateTime(2020, 9, 20, 8, 0, 0, DateTimeKind.Unspecified),
        ReportTime = reportTime,
        ReviewTime = new DateTime(2020, 9, 20, 10, 30, 0, DateTimeKind.Unspecified),
        SourceModifiedTime = new DateTime(2020, 9, 20, 10, 5, 0, DateTimeKind.Unspecified)
      },
      Content = new LaboratoryReportContentRequest
      {
        SourceSpecimenNo = "S-1",
        SpecimenTypeCode = "T-1",
        SpecimenTypeName = "血清",
        TestingCompletedTime = reportTime,
        InspectorId = "U-1",
        InspectorName = "检验人"
      },
      Results =
      [
        new LaboratoryResultItemRequest
        {
          SourceDetailKey = "D-1",
          SourceProjectName = "白细胞计数",
          SourceResultText = "6.5",
          ResultType = LaboratoryResultType.Numeric,
          DisplayOrder = 1,
          InspectorId = "U-1",
          InspectorName = "检测人"
        },
        new LaboratoryResultItemRequest
        {
          SourceDetailKey = "D-2",
          SourceProjectName = "红细胞计数",
          SourceResultText = "4.5",
          ResultType = LaboratoryResultType.Numeric,
          DisplayOrder = 2
        }
      ]
    };

    // 字段名按公开约定使用 camelCase，反序列化端拒绝未知字段。
    return JsonSerializer.Serialize(document, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
  }

  /// <summary>
  /// 报告 PDF 存储端口的手写替身：只接受预置的可读文件键，并记录读取调用次数。
  /// </summary>
  private sealed class RecordingReportPdfFileStore : IReportPdfFileStore
  {
    /// <summary>
    /// 用可读文件键集合构造替身。
    /// </summary>
    /// <param name="readableFileKeys">本替身视为可读的文件键。</param>
    public RecordingReportPdfFileStore(IEnumerable<string> readableFileKeys) => ReadableFileKeys = [.. readableFileKeys];

    /// <summary>本替身视为可读的文件键集合；用例可追加既有文件键。</summary>
    public List<string> ReadableFileKeys { get; }

    /// <summary>按键读取的调用次数。</summary>
    public int OpenReadCalls { get; private set; }

    /// <summary>本替身已写入（落盘）的文件键；删除会把键从可读集合移除。</summary>
    public List<string> SavedFileKeys { get; } = [];

    /// <summary>本次提交使用的文件键；非空时 <see cref="SaveAsync"/> 复用它，使用例可以预置落盘键。</summary>
    public string? NextSavedFileKey { get; set; }

    /// <inheritdoc/>
    public Task<string> SaveAsync(Stream content)
    {
      string fileKey = NextSavedFileKey ?? $"new/{Guid.NewGuid():N}.pdf";
      SavedFileKeys.Add(fileKey);
      ReadableFileKeys.Add(fileKey);
      return Task.FromResult(fileKey);
    }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(string fileKey)
    {
      OpenReadCalls++;
      if (!ReadableFileKeys.Contains(fileKey, StringComparer.Ordinal))
        throw new InvalidOperationException("业务拒绝：报告 PDF 文件不存在。");

      return Task.FromResult<Stream>(new MemoryStream("%PDF-1.4"u8.ToArray()));
    }

    /// <summary>
    /// 按键删除：把键从可读集合移除，使「补偿删除本次新文件」成为可观察事实。
    /// </summary>
    /// <param name="fileKey">待删除的文件键。</param>
    /// <returns>已完成的任务。</returns>
    public Task DeleteAsync(string fileKey)
    {
      ReadableFileKeys.RemoveAll(key => string.Equals(key, fileKey, StringComparison.Ordinal));
      return Task.CompletedTask;
    }
  }
}
