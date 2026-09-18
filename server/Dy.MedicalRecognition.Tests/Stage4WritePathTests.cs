using System.Reflection;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Events;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 报告提交与作废的领域与应用编排层写路径校验（矩阵 V1 至 V16、V19b、V25、V26、V28 至 V35、V38、V41、V43、V45、V48、V50、V53、V54、V58、V60、V85、V86、V87、V89 的层级可测部分）。
/// </summary>
/// <remarks>
/// 全部用例使用手写仓储替身与文件存储替身隔离数据库与文件系统，验证可观察的业务结果：
/// 写入的报告、版本与明细内容、三个检索列、登记的领域事件、失败时的零写入与拒绝文案。
/// 真实数据库与真实文件系统上的完整链路证据由阶段 12 的宿主验收负责，不在本文件内验证。
/// 时间用固定输入构造，不依赖当前时间。
/// </remarks>
public sealed class Stage4WritePathTests
{
  /// <summary>固定操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>固定操作时间。</summary>
  private static readonly DateTimeOffset CommandOperTime = new(2026, 9, 20, 3, 30, 0, TimeSpan.Zero);

  /// <summary>固定平台接收时间；取值晚于全部来源业务时间，避免与来源时间顺序校验冲突。</summary>
  private static readonly DateTime ReceivedTime = new(2026, 10, 1, 11, 30, 0, DateTimeKind.Unspecified);

  /// <summary>固定报告时间。</summary>
  private static readonly DateTime ReportTime = new(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified);

  /// <summary>固定申请时间。</summary>
  private static readonly DateTime ApplicationTime = new(2026, 9, 20, 8, 0, 0, DateTimeKind.Unspecified);

  /// <summary>固定审核时间；晚于报告时间，使时间顺序校验成立。</summary>
  private static readonly DateTime ReviewTime = new(2026, 9, 20, 10, 30, 0, DateTimeKind.Unspecified);

  /// <summary>固定来源报告修改时间。</summary>
  private static readonly DateTime SourceModifiedTime = new(2026, 9, 20, 10, 5, 0, DateTimeKind.Unspecified);

  /// <summary>固定可信组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>固定可信医院编码。</summary>
  private const string TrustedHospital = "HOS-A";

  /// <summary>固定可信院区编码。</summary>
  private const string TrustedBranch = "BRH-A";

  /// <summary>固定来源报告单号。</summary>
  private const string ReportNo = "LAB-20260920-001";

  /// <summary>检查链路的固定来源报告单号；与检验链路分列，使两条链路的报告定位互不干扰。</summary>
  private const string ExaminationReportNo = "EXM-20260920-001";

  /// <summary>固定证件号码；用例内按需要加入首尾空白与小写差异。</summary>
  private const string IdentityDocumentNo = "110101199001011234";

  /// <summary>
  /// V1：首次提交创建报告与首个版本，当前版本指向该版本、版本序号为 1、三个检索列写入本次版本取值，
  /// 并登记报告已创建事件、版本追加事件与提交事件。
  /// </summary>
  [Fact]
  public async Task First_submission_creates_report_and_first_version_with_events()
  {
    FakeReportRepository repository = new();

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.True(result);

    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(TrustedOrganization, report.OrganizationCode);
    Assert.Equal(TrustedHospital, report.HospitalCode);
    Assert.Equal(TrustedBranch, report.BranchCode);
    Assert.Equal(MedicalReportType.Laboratory, report.ReportType);
    Assert.Equal(ReportNo, report.ReportNo);
    Assert.Equal(MedicalReportLifecycleStatus.Effective, report.Status);
    Assert.Null(report.VoidedTime);
    Assert.Null(report.VoidReason);
    // 三个检索列写入规范形式取值，且报告时间取本次版本的报告时间。
    Assert.Equal(ReportTime, report.ReportTime);
    Assert.Equal("张三", report.PatientName);
    Assert.Equal(IdentityDocumentNo, report.IdentityDocumentNo);

    MedicalReportVersion version = Assert.Single(repository.Versions.Values);
    Assert.Equal(1, version.VersionNumber);
    Assert.Equal(report.Id, version.ReportId);
    Assert.Equal(report.CurrentVersionId, version.Id);
    Assert.Equal(report.PatientId, version.PatientId);
    Assert.Equal(ReportTime, version.ReportTime);
    // 平台接收时间由服务端取，不采用请求值：断言它落在本次执行时点附近，而不等于命令里的请求接收时间。
    Assert.True(
      Math.Abs((version.ReceivedTime - DateTime.Now).TotalMinutes) < 5,
      $"平台接收时间应由服务端取当前时点，实际 {version.ReceivedTime:O}。");
    Assert.NotEqual(ReceivedTime, version.ReceivedTime);
    Assert.Equal("lab-1.pdf", version.PdfFileName);
    Assert.Equal("key/one.pdf", version.PdfFileId);

    // 报告已创建事件只在新建报告时登记；版本追加事件与提交事件各一条。
    MedicalRecognitionReportCreatedEvent created = Assert.Single(events.Events.OfType<MedicalRecognitionReportCreatedEvent>());
    Assert.Equal(report.Id, created.Id);
    Assert.Equal(ReportNo, created.ReportNo);
    MedicalReportVersionAppendedAsCurrentEvent appended = Assert.Single(events.Events.OfType<MedicalReportVersionAppendedAsCurrentEvent>());
    Assert.Equal(version.Id, appended.ReportVersionId);
    Assert.Equal(1, appended.VersionNumber);
    Assert.Equal(version.ReceivedTime, appended.ReceivedTime);
    CompleteLaboratoryReportSubmittedEvent submitted = Assert.Single(events.Events.OfType<CompleteLaboratoryReportSubmittedEvent>());
    Assert.Equal(report.Id, submitted.ReportId);
    Assert.Equal(version.Id, submitted.ReportVersionId);
    Assert.Equal(MedicalReportType.Laboratory, submitted.ReportType);
    Assert.Equal(TrustedOperId, submitted.EventCreator);
    Assert.Equal(CommandOperTime, submitted.EventCreatedTime);
    Assert.Equal(MedicalRecognitionReportConst.AggregateId, submitted.AggregateId);

    // 明细写入：一条检验专项内容、两条普通结果。
    Assert.Single(repository.LaboratoryContents);
    Assert.Equal(2, repository.LaboratoryResultItems.Count);
  }

  /// <summary>
  /// V2：同一报告再次提交追加新版本并成为当前版本，旧版本不被改写、版本序号递增，
  /// 报告主体上的报告时间、患者姓名与证件号码同步更新为本次版本取值。
  /// </summary>
  [Fact]
  public async Task Second_submission_appends_version_and_refreshes_search_columns()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);

    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Guid firstVersionId = report.CurrentVersionId;
    MedicalReportVersion firstVersion = repository.Versions[firstVersionId];

    // 第二次提交保持来源报告时间不变，用于区分「报告主体三列随当前版本更新」与「版本顺序由平台形成顺序决定」。
    DateTime secondReportTime = ReportTime;
    LaboratoryReportVersionRequest second = LaboratoryDocument() with
    {
      Version = CommonVersion() with
      {
        ReportTime = secondReportTime,
        PatientName = "张三 ",
        ReportDoctorName = "李医生"
      }
    };

    (RecordingEventQueue events, bool result) = await ExecuteAsync(() => manager.SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(document: second, receivedTime: ReceivedTime)));

    Assert.True(result);
    Assert.Equal(2, repository.Versions.Count);
    MedicalReportVersion appended = repository.Versions.Values.Single(version => version.Id != firstVersionId);
    Assert.Equal(2, appended.VersionNumber);
    Assert.Equal(appended.Id, report.CurrentVersionId);
    Assert.Equal(secondReportTime, report.ReportTime);
    Assert.Equal("张三", report.PatientName);
    Assert.Equal(IdentityDocumentNo, report.IdentityDocumentNo);
    // 旧版本不被改写：序号、报告时间与文件键保持首次提交时的取值。
    Assert.Equal(1, firstVersion.VersionNumber);
    Assert.Equal(ReportTime, firstVersion.ReportTime);
    Assert.Equal("key/one.pdf", firstVersion.PdfFileId);
    // 两个版本各有一条检验专项内容，明细按版本分别保存。
    Assert.Equal(2, repository.LaboratoryContents.Count);
    Assert.Equal(4, repository.LaboratoryResultItems.Count);

    // 第二次提交时报告已存在，因此不再登记报告已创建事件。
    Assert.Empty(events.Events.OfType<MedicalRecognitionReportCreatedEvent>());
    Assert.Single(events.Events.OfType<MedicalReportVersionAppendedAsCurrentEvent>());
    Assert.Single(events.Events.OfType<CompleteLaboratoryReportSubmittedEvent>());
  }

  /// <summary>
  /// V3：报告业务标识各分量不同即视为不同报告。
  /// </summary>
  /// <remarks>报告单号、报告类型与院区三类分量各取一组差异，分别得到独立报告。</remarks>
  [Fact]
  public async Task Different_business_key_components_create_independent_reports()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);

    await WithCapturedEventsAsync(async () =>
    {
      await manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand());
      await manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(document: LaboratoryDocument() with { ReportNo = "LAB-20260920-002" }));
      await manager.SubmitCompleteExaminationReportAsync(ExaminationCommand());
      await manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(branchCode: "BRH-B"));
    });

    Assert.Equal(4, repository.Reports.Count);
    Assert.Equal(4, repository.Versions.Count);
    Assert.Contains(repository.Reports.Values, report => report.ReportNo == "LAB-20260920-002");
    Assert.Contains(repository.Reports.Values, report => report.ReportType == MedicalReportType.Examination);
    Assert.Contains(repository.Reports.Values, report => report.BranchCode == "BRH-B");
  }

  /// <summary>
  /// V9：首次解析患者创建平台患者，患者与报告主体检索列按规范形式保存，报告版本表按来源原值保存。
  /// </summary>
  /// <remarks>输入带首尾空白与小写证件号码，规范形式用于定位与检索列，版本表保留来源原值。</remarks>
  [Fact]
  public async Task First_submission_creates_patient_with_normalized_identity()
  {
    FakeReportRepository repository = new();

    LaboratoryReportVersionRequest document = LaboratoryDocument() with
    {
      Version = CommonVersion() with
      {
        PatientName = " 张三 ",
        IdentityDocumentNo = " 110101199001011234x ",
        IdentityDocumentTypeCode = " 01 "
      }
    };

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(document: document)));

    Assert.True(result);
    PlatformPatient patient = Assert.Single(repository.Patients.Values);
    Assert.Equal("01", patient.IdentityDocumentTypeCode);
    Assert.Equal("110101199001011234X", patient.IdentityDocumentNo);
    Assert.Equal("张三", patient.PatientName);

    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(patient.Id, report.PatientId);
    Assert.Equal("110101199001011234X", report.IdentityDocumentNo);

    MedicalReportVersion version = Assert.Single(repository.Versions.Values);
    Assert.Equal(" 110101199001011234x ", version.IdentityDocumentNo);
    Assert.Equal(" 01 ", version.IdentityDocumentTypeCode);
    Assert.Equal(" 张三 ", version.PatientName);
    Assert.Equal(patient.Id, version.PatientId);
    Assert.Single(events.Events.OfType<MedicalRecognitionReportCreatedEvent>());
  }

  /// <summary>
  /// V10：后续版本命中既有患者，证件号码大小写或首尾空白差异都命中同一患者，不新建患者。
  /// </summary>
  [Fact]
  public async Task Subsequent_submission_matches_existing_patient_despite_document_formatting()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);

    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    LaboratoryReportVersionRequest second = LaboratoryDocument() with
    {
      Version = CommonVersion() with { IdentityDocumentNo = $" {IdentityDocumentNo.ToLowerInvariant()} " }
    };
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(document: second, receivedTime: ReceivedTime)));

    Assert.Single(repository.Patients);
    Assert.Equal(2, repository.Versions.Count);
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(repository.Patients.Values.Single().Id, report.PatientId);
  }

  /// <summary>
  /// V11：证件已存在但姓名、性别或出生日期不一致时按患者身份信息冲突拒绝，不覆盖既有患者、不创建第二个患者。
  /// </summary>
  /// <remarks>三项核心身份各取一组差异，逐项验证拒绝文案与零写入。</remarks>
  [Theory]
  [InlineData("name")]
  [InlineData("gender")]
  [InlineData("birthDate")]
  public async Task Patient_identity_conflict_is_rejected_without_overwriting_existing_patient(string changedField)
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    PlatformPatient existing = Assert.Single(repository.Patients.Values);
    string existingName = existing.PatientName;
    string existingGender = existing.PatientGenderCode;
    DateTime existingBirthDate = existing.PatientBirthDate;
    int versionCountBefore = repository.Versions.Count;

    MedicalReportVersionRequest conflicting = changedField switch
    {
      "name" => CommonVersion() with { PatientName = "李四" },
      "gender" => CommonVersion() with { PatientGenderCode = "2" },
      _ => CommonVersion() with { PatientBirthDate = PatientBirthDate.AddDays(1) }
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(document: LaboratoryDocument() with { Version = conflicting })));

    Assert.Equal("业务拒绝：证件已存在但患者身份信息不一致。", error.Message);
    Assert.Empty(events.Events);
    Assert.Single(repository.Patients);
    Assert.Equal(existingName, existing.PatientName);
    Assert.Equal(existingGender, existing.PatientGenderCode);
    Assert.Equal(existingBirthDate, existing.PatientBirthDate);
    // 本次提交在患者核对之后已经没有新增版本，因此失败没有留下第二个版本。
    Assert.Equal(versionCountBefore, repository.Versions.Count);
  }

  /// <summary>
  /// V12：后续版本解析到其他患者时拒绝追加，不允许报告跨患者迁移。
  /// </summary>
  [Fact]
  public async Task Report_cannot_migrate_to_another_patient()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    MedicalReportVersionRequest otherPatient = CommonVersion() with
    {
      IdentityDocumentNo = "220202198002022345",
      PatientName = "王五"
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(document: LaboratoryDocument() with { Version = otherPatient })));

    Assert.Equal("业务拒绝：本次提交解析到的患者与报告已关联的患者不一致。", error.Message);
    Assert.Empty(events.Events);
    // 报告仍绑定首次提交解析到的患者，没有追加版本，也没有把报告迁到新患者。
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    PlatformPatient originalPatient = repository.Patients.Values.Single(patient => patient.IdentityDocumentNo == IdentityDocumentNo);
    Assert.Equal(originalPatient.Id, report.PatientId);
    Assert.Single(repository.Versions);
  }

  /// <summary>
  /// V5、V6、V7：必填字段缺失、成对字段单边提供与必填时间缺失或零值都拒绝整份报告且零写入。
  /// </summary>
  /// <remarks>每组构造一份非法文档，逐组验证拒绝文案与"未创建报告、未创建版本、未写入明细"。</remarks>
  [Theory]
  [InlineData("reportNo", "业务拒绝：报告单号不能为空或空白。")]
  [InlineData("sourceReportName", "业务拒绝：来源报告名称不能为空或空白。")]
  [InlineData("identityDocumentNo", "业务拒绝：证件号码不能为空或空白。")]
  [InlineData("visitSerialNo", "业务拒绝：就诊流水号不能为空或空白。")]
  [InlineData("applicationDeptName", "业务拒绝：申请科室的标识与名称必须成对提供。")]
  [InlineData("applicationDoctorId", "业务拒绝：申请医生的标识与名称必须成对提供。")]
  [InlineData("executionDeptName", "业务拒绝：执行科室的标识与名称必须成对提供。")]
  [InlineData("reportDoctorId", "业务拒绝：报告医生的标识与名称必须成对提供。")]
  [InlineData("reviewDoctorName", "业务拒绝：审核医生的标识与名称必须成对提供。")]
  [InlineData("applicationTime", "业务拒绝：申请时间不能缺失。")]
  [InlineData("patientBirthDate", "业务拒绝：患者出生日期不能缺失。")]
  [InlineData("sourceModifiedTime", "业务拒绝：源端报告修改时间不能缺失。")]
  public async Task Required_and_paired_field_violations_reject_the_whole_report(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    MedicalReportVersionRequest version = changedField switch
    {
      "reportNo" => CommonVersion(),
      "sourceReportName" => CommonVersion() with { SourceReportName = "  " },
      "identityDocumentNo" => CommonVersion() with { IdentityDocumentNo = string.Empty },
      "visitSerialNo" => CommonVersion() with { VisitSerialNo = string.Empty },
      "applicationDeptName" => CommonVersion() with { ApplicationDeptName = string.Empty },
      "applicationDoctorId" => CommonVersion() with { ApplicationDoctorId = string.Empty },
      "executionDeptName" => CommonVersion() with { ExecutionDeptName = string.Empty },
      "reportDoctorId" => CommonVersion() with { ReportDoctorId = string.Empty },
      "reviewDoctorName" => CommonVersion() with { ReviewDoctorName = string.Empty },
      "applicationTime" => CommonVersion() with { ApplicationTime = default },
      "patientBirthDate" => CommonVersion() with { PatientBirthDate = default },
      _ => CommonVersion() with { SourceModifiedTime = default }
    };

    SubmitCompleteLaboratoryReportCommand command = LaboratoryCommand(document: LaboratoryDocument() with { Version = version }) with
    {
      ReportNo = changedField == "reportNo" ? "  " : ReportNo
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(command));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
    Assert.Empty(repository.Patients);
    Assert.Empty(repository.LaboratoryContents);
    Assert.Empty(repository.LaboratoryResultItems);
  }

  /// <summary>
  /// V8：业务时间逆序拒绝整份报告。
  /// </summary>
  /// <remarks>
  /// 两类逆序各一组：申请时间晚于报告时间，以及报告时间晚于审核时间；
  /// 来源报告时间晚于平台接收时点由作废边界与 V8 的第二类覆盖，因此不另设第三组。
  /// </remarks>
  [Theory]
  [InlineData("applicationAfterReport", "业务拒绝：申请时间不得晚于报告时间。")]
  [InlineData("reportAfterReview", "业务拒绝：报告时间不得晚于审核时间。")]
  public async Task Reversed_business_time_order_rejects_the_whole_report(string reversedCase, string expectedMessage)
  {
    FakeReportRepository repository = new();

    MedicalReportVersionRequest version = reversedCase switch
    {
      "applicationAfterReport" => CommonVersion() with { ApplicationTime = ReportTime.AddMinutes(1) },
      _ => CommonVersion() with { ReportTime = ReviewTime.AddMinutes(30), ReviewTime = ReviewTime.AddMinutes(10) }
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
        LaboratoryCommand(document: LaboratoryDocument() with { Version = version })));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V8：审核时间为空时不参与时间顺序校验，报告时间晚于审核时间点以外的取值仍可接收。
  /// </summary>
  [Fact]
  public async Task Missing_review_time_is_accepted_without_order_check()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(document: LaboratoryDocument() with { Version = CommonVersion() with { ReviewTime = null } })));

    Assert.True(result);
    MedicalReportVersion version = Assert.Single(repository.Versions.Values);
    Assert.Null(version.ReviewTime);
  }

  /// <summary>
  /// V87：就诊类型仅接受门诊、急诊、住院、体检、其他五值，未知值与其他非法值均拒绝且零写入。
  /// </summary>
  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(99)]
  public async Task Invalid_visit_type_is_rejected(int visitTypeValue)
  {
    FakeReportRepository repository = new();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        document: LaboratoryDocument() with { Version = CommonVersion() with { VisitType = (VisitType)visitTypeValue } })));

    Assert.Equal("业务拒绝：就诊类型不是已定义的取值。", error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
  }

  /// <summary>
  /// V87：五个已定义的就诊类型取值全部可接收。
  /// </summary>
  [Theory]
  [InlineData(VisitType.Outpatient)]
  [InlineData(VisitType.Emergency)]
  [InlineData(VisitType.Inpatient)]
  [InlineData(VisitType.PhysicalExam)]
  [InlineData(VisitType.Other)]
  public async Task Every_defined_visit_type_is_accepted(VisitType visitType)
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(document: LaboratoryDocument() with { Version = CommonVersion() with { VisitType = visitType } })));

    Assert.True(result);
    Assert.Equal(visitType, Assert.Single(repository.Versions.Values).VisitType);
  }

  /// <summary>
  /// V21、V22：检验专项内容必填字段缺失即拒绝；应填字段缺省时仍接收成功。
  /// </summary>
  [Theory]
  [InlineData("sourceSpecimenNo", "业务拒绝：院内标本号不能为空或空白。")]
  [InlineData("specimenTypeCode", "业务拒绝：标本类型编码不能为空或空白。")]
  [InlineData("specimenTypeName", "业务拒绝：标本类型名称不能为空或空白。")]
  [InlineData("testingCompletedTime", "业务拒绝：检测完成时间不能缺失。")]
  [InlineData("inspectorId", "业务拒绝：检验人的标识与名称必须成对提供。")]
  public async Task Laboratory_content_required_fields_reject_the_report(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    LaboratoryReportContentRequest content = changedField switch
    {
      "sourceSpecimenNo" => LaboratoryContent() with { SourceSpecimenNo = "  " },
      "specimenTypeCode" => LaboratoryContent() with { SpecimenTypeCode = string.Empty },
      "specimenTypeName" => LaboratoryContent() with { SpecimenTypeName = string.Empty },
      "testingCompletedTime" => LaboratoryContent() with { TestingCompletedTime = default },
      _ => LaboratoryContent() with { InspectorId = string.Empty }
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(content: content)));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V22：报告类别、备注、整体异常标识、来源医嘱流水号与标本时间线缺省时仍接收成功。
  /// </summary>
  [Fact]
  public async Task Laboratory_content_optional_fields_may_be_missing()
  {
    FakeReportRepository repository = new();

    LaboratoryReportContentRequest content = LaboratoryContent() with
    {
      ReportCategoryCode = null,
      ReportCategoryName = null,
      ReportRemark = null,
      OverallAbnormalFlag = null,
      SourceOrderSerialNo = null,
      SpecimenCollectedTime = null,
      SpecimenSubmittedTime = null,
      LaboratoryReceivedTime = null
    };

    (_, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(content: content)));

    Assert.True(result);
    LaboratoryReportContent stored = Assert.Single(repository.LaboratoryContents);
    Assert.Null(stored.ReportCategoryCode);
    Assert.Null(stored.SpecimenCollectedTime);
    Assert.Equal(content.SourceSpecimenNo, stored.SourceSpecimenNo);
  }

  /// <summary>
  /// V23：普通结果集合为空即拒绝整份报告。
  /// </summary>
  [Fact]
  public async Task Empty_result_collection_rejects_the_report()
  {
    FakeReportRepository repository = new();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [])));

    Assert.Equal("业务拒绝：普通检验结果至少一条。", error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
  }

  /// <summary>
  /// V24、V42：普通结果必填字段缺失即拒绝；结果类型仅接受三种取值。
  /// </summary>
  [Theory]
  [InlineData("sourceProjectName", "业务拒绝：来源项目名称不能为空或空白。")]
  [InlineData("sourceResultText", "业务拒绝：来源结果原文不能为空或空白。")]
  [InlineData("displayOrder", "业务拒绝：展示序号必须是正整数。")]
  [InlineData("resultType", "业务拒绝：结果类型不是已定义的取值。")]
  [InlineData("inspectorId", "业务拒绝：检测人的标识与名称必须成对提供。")]
  public async Task Result_item_required_fields_and_result_type_are_validated(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    LaboratoryResultItemRequest result = changedField switch
    {
      "sourceProjectName" => ResultItem() with { SourceProjectName = string.Empty },
      "sourceResultText" => ResultItem() with { SourceResultText = "  " },
      "displayOrder" => ResultItem() with { DisplayOrder = 0 },
      "resultType" => ResultItem() with { ResultType = (LaboratoryResultType)9 },
      _ => ResultItem() with { InspectorId = null }
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [result])));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.LaboratoryResultItems);
  }

  /// <summary>
  /// V25：普通结果来源明细标识提供且重复即拒绝；全部缺失时仍接收。
  /// </summary>
  [Fact]
  public async Task Duplicate_source_detail_key_rejects_and_all_missing_is_accepted()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        results: [ResultItem() with { SourceDetailKey = "D-1", DisplayOrder = 1 }, ResultItem() with { SourceDetailKey = "D-1", DisplayOrder = 2 }])));

    Assert.Equal("业务拒绝：同一报告版本内普通检验结果来源明细标识重复。", error.Message);
    Assert.Empty(repository.Versions);

    FakeReportRepository secondRepository = new();
    (_, bool result) = await ExecuteAsync(() => CreateManager(secondRepository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
      results: [ResultItem() with { SourceDetailKey = null, DisplayOrder = 1 }, ResultItem() with { SourceDetailKey = "   ", DisplayOrder = 2 }])));

    Assert.True(result);
    Assert.Equal(2, secondRepository.LaboratoryResultItems.Count);
  }

  /// <summary>
  /// V26：普通结果展示序号在同一版本内重复即拒绝。
  /// </summary>
  [Fact]
  public async Task Duplicate_display_order_rejects_the_report()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        results:
        [
          ResultItem() with { SourceDetailKey = "D-1", DisplayOrder = 1 },
          ResultItem() with { SourceDetailKey = "D-2", DisplayOrder = 1 }
        ])));

    Assert.Equal("业务拒绝：同一报告版本内普通检验结果展示序号重复。", error.Message);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V35：药敏展示序号在同一版本、同一细菌鉴定结果下重复即拒绝；不同细菌下相同序号可接受。
  /// </summary>
  [Fact]
  public async Task Susceptibility_display_order_is_unique_within_one_bacteria_result()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        bacteriaResults: [BacteriaResult(susceptibilities:
        [
          Susceptibility() with { SourceDetailKey = "SD-1", DisplayOrder = 1 },
          Susceptibility() with { SourceDetailKey = "SD-2", DisplayOrder = 1 }
        ])])));

    Assert.Equal("业务拒绝：同一报告版本内药敏结果展示序号重复。", error.Message);
    Assert.Empty(repository.Versions);

    // 不同细菌鉴定结果下相同展示序号可接受。
    FakeReportRepository secondRepository = new();
    (_, bool result) = await ExecuteAsync(() => CreateManager(secondRepository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
      bacteriaResults:
      [
        BacteriaResult(susceptibilities: [Susceptibility() with { SourceDetailKey = "SD-1", DisplayOrder = 1 }]),
        BacteriaResult(susceptibilities: [Susceptibility() with { SourceDetailKey = "SD-1", DisplayOrder = 1 }])
      ])));

    Assert.True(result);
    Assert.Equal(2, secondRepository.Susceptibilities.Count);
  }

  /// <summary>
  /// V27：互认项目编码未提供、提供但不存在或停用都仍接收并原样保存。
  /// </summary>
  [Fact]
  public async Task Missing_or_unavailable_standard_project_code_is_accepted_as_is()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
      results:
      [
        ResultItem() with { SourceDetailKey = "D-1", StandardProjectCode = null, DisplayOrder = 1 },
        ResultItem() with { SourceDetailKey = "D-2", StandardProjectCode = "NOT-EXIST", DisplayOrder = 2 },
        ResultItem() with { SourceDetailKey = "D-3", StandardProjectCode = "DISABLED-CODE", DisplayOrder = 3 }
      ])));

    Assert.True(result);
    // 按展示序号回读三条明细的互认项目编码：未提供的保持为空，提供但不可用的原样保存。
    List<string?> storedCodes = [.. repository.LaboratoryResultItems.OrderBy(item => item.DisplayOrder).Select(item => item.StandardProjectCode)];
    Assert.Equal(3, storedCodes.Count);
    Assert.Null(storedCodes[0]);
    Assert.Equal("NOT-EXIST", storedCodes[1]);
    Assert.Equal("DISABLED-CODE", storedCodes[2]);
  }

  /// <summary>
  /// V28：明细检测人是应填且成对的可选字段：标识与名称单边提供即拒绝，两侧同时为空时接收。
  /// </summary>
  /// <remarks>判定依据是填报要求里的"应填且成对"：可选字段的两侧都为空表示来源未提供该信息，不构成缺失拒绝。</remarks>
  [Theory]
  [InlineData("onlyId")]
  [InlineData("onlyName")]
  public async Task Partially_provided_detail_inspector_is_rejected(string pairedCase)
  {
    FakeReportRepository repository = new();

    LaboratoryResultItemRequest result = pairedCase == "onlyId"
      ? ResultItem() with { InspectorId = "U-1", InspectorName = null }
      : ResultItem() with { InspectorId = null, InspectorName = "检测人" };

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [result])));

    Assert.Equal("业务拒绝：检测人的标识与名称必须成对提供。", error.Message);
    Assert.Empty(repository.LaboratoryResultItems);
  }

  /// <summary>
  /// V28：明细检测人两侧同时为空时提交成功，明细按来源未提供落库。
  /// </summary>
  [Fact]
  public async Task Missing_optional_detail_inspector_is_accepted()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(results: [ResultItem() with { InspectorId = null, InspectorName = "   " }])));

    Assert.True(result);
    LaboratoryResultItem stored = Assert.Single(repository.LaboratoryResultItems);
    Assert.Null(stored.InspectorId);
    // 报告版本表按来源原值保存，因此纯空白名称原样落库，不做归一化。
    Assert.Equal("   ", stored.InspectorName);
  }

  /// <summary>
  /// V28：细菌鉴定结果与药敏结果的可选检测人两侧同时为空时同样接收。
  /// </summary>
  [Fact]
  public async Task Missing_optional_inspector_on_bacteria_and_susceptibility_is_accepted()
  {
    FakeReportRepository repository = new();

    LaboratoryBacteriaResultRequest bacteria = BacteriaResult(susceptibilities:
    [
      Susceptibility() with { InspectorId = null, InspectorName = null }
    ]) with
    {
      InspectorId = null,
      InspectorName = null
    };

    (_, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(bacteriaResults: [bacteria])));

    Assert.True(result);
    LaboratoryBacteriaResult storedBacteria = Assert.Single(repository.BacteriaResults);
    Assert.Null(storedBacteria.InspectorId);
    Assert.Null(storedBacteria.InspectorName);
    LaboratoryAntimicrobialSusceptibility storedSusceptibility = Assert.Single(repository.Susceptibilities);
    Assert.Null(storedSusceptibility.InspectorId);
    Assert.Null(storedSusceptibility.InspectorName);
  }

  /// <summary>
  /// V28：明细检测人标识与名称同时提供时接收，且保存的就是提交的取值。
  /// </summary>
  [Fact]
  public async Task Provided_detail_inspector_is_accepted_and_saved()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(results: [ResultItem() with { InspectorId = "U-9", InspectorName = "检测人甲" }])));

    Assert.True(result);
    LaboratoryResultItem stored = Assert.Single(repository.LaboratoryResultItems);
    Assert.Equal("U-9", stored.InspectorId);
    Assert.Equal("检测人甲", stored.InspectorName);
  }

  /// <summary>
  /// V29：细菌鉴定结果的结论与来源结果原文缺失即拒绝。
  /// </summary>
  [Theory]
  [InlineData("sourceResultText", "业务拒绝：来源结果原文不能为空或空白。")]
  [InlineData("detectionConclusion", "业务拒绝：检测结论不能为空或空白。")]
  public async Task Bacteria_result_required_fields_reject_the_report(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    LaboratoryBacteriaResultRequest bacteria = changedField == "sourceResultText"
      ? BacteriaResult() with { SourceResultText = "  " }
      : BacteriaResult() with { DetectionConclusion = string.Empty };

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(bacteriaResults: [bacteria])));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(repository.BacteriaResults);
  }

  /// <summary>
  /// V30、V31：培养未检出时仍接收一条细菌结果且不要求菌种编码与名称；检出具体菌种时菌种名称缺失即拒绝。
  /// </summary>
  [Fact]
  public async Task Not_detected_bacteria_is_accepted_without_organism_name_and_detected_requires_it()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
      bacteriaResults: [BacteriaResult() with { DetectionConclusion = "未检出", SourceOrganismCode = null, SourceOrganismName = null }])));

    Assert.True(result);
    Assert.Null(Assert.Single(repository.BacteriaResults).SourceOrganismName);

    FakeReportRepository secondRepository = new();
    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(secondRepository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        bacteriaResults: [BacteriaResult() with { DetectionConclusion = "检出大肠埃希菌", SourceOrganismName = null }])));

    Assert.Equal("业务拒绝：来源菌种名称不能为空或空白。", error.Message);
    Assert.Empty(secondRepository.BacteriaResults);
  }

  /// <summary>
  /// V32：细菌结果的培养信息与仪器缺省时仍接收。
  /// </summary>
  [Fact]
  public async Task Missing_culture_information_and_instrument_is_accepted()
  {
    FakeReportRepository repository = new();

    LaboratoryBacteriaResultRequest bacteria = BacteriaResult() with
    {
      ColonyCount = null,
      CultureMedium = null,
      CultureTime = null,
      CultureCondition = null,
      DiscoveryMethod = null,
      DetectionMethod = null,
      Description = null,
      InstrumentCode = null,
      InstrumentName = null,
      TestPanelCode = null,
      TestPanelName = null
    };

    (_, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(bacteriaResults: [bacteria])));

    Assert.True(result);
    LaboratoryBacteriaResult stored = Assert.Single(repository.BacteriaResults);
    Assert.Null(stored.CultureMedium);
    Assert.Null(stored.InstrumentCode);
  }

  /// <summary>
  /// V33：药敏结果的受试药物名称、来源结论与展示序号缺失即拒绝。
  /// </summary>
  [Theory]
  [InlineData("drugName", "业务拒绝：受试药物名称不能为空或空白。")]
  [InlineData("sourceConclusionText", "业务拒绝：来源结论不能为空或空白。")]
  [InlineData("displayOrder", "业务拒绝：展示序号必须是正整数。")]
  public async Task Susceptibility_required_fields_reject_the_report(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    LaboratoryAntimicrobialSusceptibilityRequest susceptibility = changedField switch
    {
      "drugName" => Susceptibility() with { DrugName = string.Empty },
      "sourceConclusionText" => Susceptibility() with { SourceConclusionText = "  " },
      _ => Susceptibility() with { DisplayOrder = 0 }
    };

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(
        bacteriaResults: [BacteriaResult(susceptibilities: [susceptibility])])));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(repository.Susceptibilities);
  }

  /// <summary>
  /// V36：药敏量值按来源文本保存，不解析、不换算。
  /// </summary>
  [Fact]
  public async Task Susceptibility_measurement_values_are_saved_as_source_text()
  {
    FakeReportRepository repository = new();

    LaboratoryAntimicrobialSusceptibilityRequest susceptibility = Susceptibility() with
    {
      DiskContent = "30μg",
      MicValue = "≤0.25 mg/L",
      InhibitionZoneDiameter = ">20mm",
      ReferenceValue = "≤1"
    };

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(bacteriaResults: [BacteriaResult(susceptibilities: [susceptibility])])));

    Assert.True(result);
    LaboratoryAntimicrobialSusceptibility stored = Assert.Single(repository.Susceptibilities);
    Assert.Equal("30μg", stored.DiskContent);
    Assert.Equal("≤0.25 mg/L", stored.MicValue);
    Assert.Equal(">20mm", stored.InhibitionZoneDiameter);
    Assert.Equal("≤1", stored.ReferenceValue);
    // 药敏结果固定归属刚写入的细菌鉴定结果，不会产生无法归属的记录。
    Assert.Equal(Assert.Single(repository.BacteriaResults).Id, stored.BacteriaResultId);
  }

  /// <summary>
  /// V44：检验报告备注与整体异常标识不参与接收判断。
  /// </summary>
  [Fact]
  public async Task Laboratory_remark_and_overall_flag_do_not_affect_acceptance()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(
        content: LaboratoryContent() with { ReportRemark = null, OverallAbnormalFlag = "A", InspectorName = "检验人" },
        results: [ResultItem() with { AbnormalFlag = LaboratoryAbnormalFlag.Normal }])));

    Assert.True(result);
    Assert.Null(Assert.Single(repository.LaboratoryContents).ReportRemark);
  }

  /// <summary>
  /// V46 至 V49：检查报告专项内容与项目的必填字段、项目集合非空与部位名称必填。
  /// </summary>
  [Theory]
  [InlineData("findings", "业务拒绝：检查所见不能为空或空白。")]
  [InlineData("conclusion", "业务拒绝：检查结论不能为空或空白。")]
  [InlineData("sourceDiagnosisName", "业务拒绝：来源诊断名称不能为空或空白。")]
  [InlineData("examinationTime", "业务拒绝：实际检查时间不能缺失。")]
  [InlineData("examinerName", "业务拒绝：检查医生的标识与名称必须成对提供。")]
  [InlineData("sourceProjectName", "业务拒绝：来源项目名称不能为空或空白。")]
  [InlineData("siteName", "业务拒绝：部位名称不能为空或空白。")]
  public async Task Examination_content_required_fields_reject_the_report(string changedField, string expectedMessage)
  {
    FakeReportRepository repository = new();

    ExaminationReportContentRequest content = changedField switch
    {
      "findings" => ExaminationContent() with { Findings = "  " },
      "conclusion" => ExaminationContent() with { Conclusion = string.Empty },
      "sourceDiagnosisName" => ExaminationContent() with { SourceDiagnosisName = string.Empty },
      "examinationTime" => ExaminationContent() with { ExaminationTime = default },
      "examinerName" => ExaminationContent() with { ExaminerName = string.Empty },
      _ => ExaminationContent()
    };

    IReadOnlyList<ExaminationItemRequest> items = changedField switch
    {
      "sourceProjectName" => [ExaminationItemRequestFixture() with { SourceProjectName = string.Empty }],
      "siteName" => [ExaminationItemRequestFixture() with { Sites = [new ExaminationSiteRequest { SiteName = "  " }] }],
      _ => [ExaminationItemRequestFixture()]
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteExaminationReportAsync(ExaminationCommand(content: content, items: items)));

    Assert.Equal(expectedMessage, error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
    Assert.Empty(repository.ExaminationContents);
  }

  /// <summary>
  /// V47：检查项目集合为空即拒绝整份报告。
  /// </summary>
  [Fact]
  public async Task Empty_examination_item_collection_rejects_the_report()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteExaminationReportAsync(ExaminationCommand(items: [])));

    Assert.Equal("业务拒绝：检查项目至少一条。", error.Message);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V49：项目没有明确部位时部位集合为空仍接收。
  /// </summary>
  [Fact]
  public async Task Examination_item_without_sites_is_accepted()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(items: [ExaminationItemRequestFixture() with { Sites = [] }])));

    Assert.True(result);
    Assert.Single(repository.ExaminationItems);
    Assert.Empty(repository.ExaminationSites);
  }

  /// <summary>
  /// V51：来源影像状态与调阅地址一致性；无影像时地址必须为空，有影像无地址仍接收，未知时地址可有可无。
  /// </summary>
  [Fact]
  public async Task Source_image_status_and_access_url_consistency_is_validated()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteExaminationReportAsync(ExaminationCommand(
        content: ExaminationContent() with { SourceImageStatus = SourceImageStatus.None, ImageAccessUrl = "http://image" })));

    Assert.Equal("业务拒绝：来源影像状态为无影像时影像调阅地址必须为空。", error.Message);
    Assert.Empty(repository.ExaminationContents);

    FakeReportRepository secondRepository = new();
    (_, bool result) = await ExecuteAsync(() => CreateManager(secondRepository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(content: ExaminationContent() with { SourceImageStatus = SourceImageStatus.Available, ImageAccessUrl = null })));

    Assert.True(result);
    Assert.Equal(SourceImageStatus.Available, Assert.Single(secondRepository.ExaminationContents).SourceImageStatus);
    Assert.Null(Assert.Single(secondRepository.ExaminationContents).ImageAccessUrl);
  }

  /// <summary>
  /// V52：来源影像状态仅接受三值；字段未提供时按未知处理并保存，不拒绝整份报告。
  /// </summary>
  [Fact]
  public async Task Source_image_status_only_accepts_three_values_and_missing_becomes_unknown()
  {
    FakeReportRepository repository = new();

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteExaminationReportAsync(ExaminationCommand(
        content: ExaminationContent() with { SourceImageStatus = (SourceImageStatus)9 })));

    Assert.Equal("业务拒绝：来源影像状态不是已定义的取值。", error.Message);
    Assert.Empty(repository.ExaminationContents);

    FakeReportRepository secondRepository = new();
    (_, bool result) = await ExecuteAsync(() => CreateManager(secondRepository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(content: ExaminationContent() with { SourceImageStatus = null })));

    Assert.True(result);
    Assert.Equal(SourceImageStatus.Unknown, Assert.Single(secondRepository.ExaminationContents).SourceImageStatus);
  }

  /// <summary>
  /// V55：检查医生与报告医生为同一人时接收成功。
  /// </summary>
  [Fact]
  public async Task Examination_doctor_may_be_the_same_person_as_report_doctor()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(() => CreateManager(repository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(content: ExaminationContent() with { ExaminerId = "DOC-1", ExaminerName = "王医生" },
        document: ExaminationDocument() with { Version = CommonVersion() with { ReportDoctorId = "DOC-1", ReportDoctorName = "王医生" } })));

    Assert.True(result);
    Assert.Equal("DOC-1", Assert.Single(repository.ExaminationContents).ExaminerId);
    Assert.Equal("DOC-1", Assert.Single(repository.Versions.Values).ReportDoctorId);
  }

  /// <summary>
  /// V4：已作废报告再次提交被拒绝，且不保存任何内容、不登记事件。
  /// </summary>
  [Fact]
  public async Task Voided_report_cannot_be_submitted_again()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));
    MedicalReportVersion version = Assert.Single(repository.Versions.Values);
    await WithCapturedEventsAsync(() => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: version.ReceivedTime)));

    // 作废后提交同一来源报告时间的新版本，用于把拒绝原因定在"报告已作废"而不是时间顺序校验。
    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：报告已作废，不能再次提交。", error.Message);
    Assert.Empty(events.Events);
    Assert.Single(repository.Versions);
  }

  /// <summary>
  /// V13：作废首次生效并写入作废事实，不生成内容版本。
  /// </summary>
  [Fact]
  public async Task First_void_takes_effect_without_content_version()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    DateTime voidedTime = ReceivedTime;
    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: voidedTime)));

    Assert.True(result);
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(MedicalReportLifecycleStatus.Voided, report.Status);
    Assert.Equal(voidedTime, report.VoidedTime);
    Assert.Equal("重复报告", report.VoidReason);
    // 作废不生成内容版本，也不物理删除既有版本与明细。
    Assert.Single(repository.Versions);
    Assert.Single(repository.LaboratoryContents);

    LaboratoryReportVoidedEvent voided = Assert.Single(events.Events.OfType<LaboratoryReportVoidedEvent>());
    Assert.Equal(report.Id, voided.ReportId);
    Assert.Equal(ReportNo, voided.ReportNo);
    Assert.Equal(voidedTime, voided.VoidedTime);
    Assert.Equal("重复报告", voided.VoidReason);
  }

  /// <summary>
  /// V14：作废时间不得早于当前版本平台接收时间，也不得晚于本次请求接收时间，两端相等时接受。
  /// </summary>
  /// <remarks>当前版本的平台接收时间由服务端取，因此用例从写入结果读取它作为边界基准。</remarks>
  [Fact]
  public async Task Void_time_boundaries_are_enforced()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));
    MedicalReportVersion version = Assert.Single(repository.Versions.Values);

    (_, InvalidOperationException tooEarly) = await ExecuteExpectingRejectionAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: version.ReceivedTime.AddSeconds(-1), requestReceivedTime: version.ReceivedTime)));
    Assert.Equal("业务拒绝：作废时间不得早于当前版本的平台接收时间。", tooEarly.Message);

    (_, InvalidOperationException tooLate) = await ExecuteExpectingRejectionAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: version.ReceivedTime.AddSeconds(1), requestReceivedTime: version.ReceivedTime)));
    Assert.Equal("业务拒绝：作废时间不得晚于本次请求的接收时间。", tooLate.Message);

    // 两端相等时接受。
    (_, bool result) = await ExecuteAsync(() => manager.VoidLaboratoryReportAsync(
      VoidCommand(voidedTime: version.ReceivedTime, requestReceivedTime: version.ReceivedTime)));
    Assert.True(result);
    Assert.Equal(MedicalReportLifecycleStatus.Voided, Assert.Single(repository.Reports.Values).Status);
  }

  /// <summary>
  /// V15：重复作废在时间与原因完全一致时幂等成功且不重复登记事件；任一不同返回作废信息冲突且不覆盖。
  /// </summary>
  [Fact]
  public async Task Repeated_void_is_idempotent_only_for_identical_void_facts()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));
    MedicalReportVersion baseline = Assert.Single(repository.Versions.Values);
    await WithCapturedEventsAsync(() => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: baseline.ReceivedTime)));

    (RecordingEventQueue idempotentEvents, bool result) = await ExecuteAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: baseline.ReceivedTime)));
    Assert.True(result);
    Assert.Empty(idempotentEvents.Events);

    (RecordingEventQueue conflictEvents, InvalidOperationException wrongReason) = await ExecuteExpectingRejectionAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: baseline.ReceivedTime, voidReason: "另一个原因")));
    Assert.Equal("业务拒绝：报告作废信息与已保存的作废事实不一致。", wrongReason.Message);
    Assert.Empty(conflictEvents.Events);

    (_, InvalidOperationException wrongTime) = await ExecuteExpectingRejectionAsync(
      () => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: baseline.ReceivedTime.AddSeconds(-1))));
    Assert.Equal("业务拒绝：报告作废信息与已保存的作废事实不一致。", wrongTime.Message);

    // 首次保存的作废事实未被覆盖。
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(baseline.ReceivedTime, report.VoidedTime);
    Assert.Equal("重复报告", report.VoidReason);
  }

  /// <summary>
  /// V16：报告不存在时作废拒绝并表达业务事实，不创建占位记录。
  /// </summary>
  [Fact]
  public async Task Void_of_missing_report_is_rejected_without_placeholder()
  {
    FakeReportRepository repository = new();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).VoidLaboratoryReportAsync(VoidCommand()));

    Assert.Equal("业务拒绝：报告不存在。", error.Message);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
  }

  /// <summary>
  /// V85：并发首次提交同一报告业务标识时唯一约束兜底，翻译为业务拒绝，且库中不出现版本。
  /// </summary>
  /// <remarks>
  /// 唯一约束冲突由仓储识别为持久化事实并翻译为内部异常，管理器把它翻译为业务拒绝并提示刷新后重试；
  /// 报告已创建事件在此路径上不登记，本次提交整体失败。
  /// </remarks>
  [Fact]
  public async Task Concurrent_first_submission_is_translated_to_business_rejection()
  {
    FakeReportRepository repository = new() { ThrowDuplicateReportOnCreate = true };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：该报告已被并发提交，请刷新后重试。", error.Message);
    Assert.IsType<DuplicateMedicalRecognitionReportException>(error.InnerException);
    Assert.Empty(events.Events);
    Assert.Empty(repository.Reports);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V86：并发首次解析同一证件时复读一次，核心身份一致则继续追加版本。
  /// </summary>
  [Fact]
  public async Task Concurrent_patient_resolution_rereads_once_and_continues_when_identity_matches()
  {
    PlatformPatient concurrentPatient = new()
    {
      Id = Guid.NewGuid(),
      IdentityDocumentTypeCode = "01",
      IdentityDocumentNo = IdentityDocumentNo,
      PatientName = "张三",
      PatientGenderCode = ProtocolGenderCode,
      PatientBirthDate = PatientBirthDate,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };
    FakeReportRepository repository = new()
    {
      ThrowDuplicatePatientOnCreate = true,
      PatientVisibleAfterDuplicate = concurrentPatient
    };

    (RecordingEventQueue events, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.True(result);
    // 复读命中并发写入的患者，因此本次没有插入新的患者行，报告与版本关联该患者。
    Assert.True(repository.PatientCreateAttempted);
    Assert.Empty(repository.Patients);
    MedicalRecognitionReport report = Assert.Single(repository.Reports.Values);
    Assert.Equal(concurrentPatient.Id, report.PatientId);
    Assert.Equal(concurrentPatient.Id, Assert.Single(repository.Versions.Values).PatientId);
    Assert.Single(events.Events.OfType<MedicalRecognitionReportCreatedEvent>());
  }

  /// <summary>
  /// V86：并发首次解析同一证件时复读一次，核心身份不一致按患者身份信息冲突拒绝。
  /// </summary>
  [Fact]
  public async Task Concurrent_patient_resolution_rereads_once_and_rejects_identity_conflict()
  {
    PlatformPatient concurrentPatient = new()
    {
      Id = Guid.NewGuid(),
      IdentityDocumentTypeCode = "01",
      IdentityDocumentNo = IdentityDocumentNo,
      PatientName = "另一名患者",
      PatientGenderCode = ProtocolGenderCode,
      PatientBirthDate = PatientBirthDate,
      OperId = TrustedOperId,
      OperTime = CommandOperTime
    };
    FakeReportRepository repository = new()
    {
      ThrowDuplicatePatientOnCreate = true,
      PatientVisibleAfterDuplicate = concurrentPatient
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：证件已存在但患者身份信息不一致。", error.Message);
    // 报告主体与版本已登记，因此这里核对的是本次提交没有新增患者行，也没有登记提交完成事件。
    Assert.Empty(repository.Patients);
    Assert.Empty(repository.Versions);
    Assert.DoesNotContain(events.Events, item => item is CompleteLaboratoryReportSubmittedEvent or CompleteExaminationReportSubmittedEvent);
  }

  /// <summary>
  /// V86：并发首次解析同一证件且复读仍读不到患者时按并发冲突拒绝。
  /// </summary>
  [Fact]
  public async Task Concurrent_patient_resolution_rejects_when_reread_finds_nothing()
  {
    FakeReportRepository repository = new() { ThrowDuplicatePatientOnCreate = true };

    (_, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：患者身份信息并发冲突，未完成提交。", error.Message);
    // 复读按同一证件键进行，因此既没有新增患者，也没有新增版本。
    Assert.Empty(repository.Patients);
    Assert.Empty(repository.Versions);
  }

  /// <summary>
  /// V85 的版本序号面：并发追加同一版本序号时唯一约束兜底，翻译为业务拒绝并提示重试。
  /// </summary>
  [Fact]
  public async Task Concurrent_version_append_is_translated_to_business_rejection()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    repository.ThrowDuplicateVersionOnCreate = true;
    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：报告版本已被并发提交，请刷新后重试。", error.Message);
    Assert.IsType<DuplicateMedicalReportVersionException>(error.InnerException);
    Assert.Empty(events.Events);
    // 版本没有被追加，当前版本指向仍是首个版本。
    Assert.Single(repository.Versions);
  }

  /// <summary>
  /// V45、V60、V18：检验与检查两条链路的写库失败都使整次提交不保存任何内容、不登记事件。
  /// </summary>
  /// <remarks>
  /// 失败注入点选在专项内容写入之后，用于验证"部分步骤已执行、后续步骤失败"时不会留下半成品；
  /// 事务回滚由框架承担，本用例只覆盖本项目自己的代码路径，因此按"替身未收到后续写入"判定。
  /// </remarks>
  [Theory]
  [InlineData("laboratory")]
  [InlineData("examination")]
  public async Task Write_failure_leaves_no_version_content_or_detail(string reportType)
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);

    if (reportType == "laboratory") repository.FailLaboratoryContentWrite = true;
    else repository.FailExaminationContentWrite = true;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      () => reportType == "laboratory"
        ? manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand())
        : manager.SubmitCompleteExaminationReportAsync(ExaminationCommand()));

    Assert.Equal($"业务拒绝：{(reportType == "laboratory" ? "检验" : "检查")}专项内容保存影响的行数异常，未完成提交。", error.Message);
    // 失败发生在专项内容写入之后：报告已创建事件与版本追加事件已登记，但提交完成事件不登记；
    // 失败点之后的明细一条都没有写入。
    Assert.DoesNotContain(events.Events, item => item is CompleteLaboratoryReportSubmittedEvent or CompleteExaminationReportSubmittedEvent);
    Assert.Empty(repository.LaboratoryResultItems);
    Assert.Empty(repository.BacteriaResults);
    Assert.Empty(repository.Susceptibilities);
    Assert.Empty(repository.ExaminationItems);
    Assert.Empty(repository.ExaminationSites);
  }

  /// <summary>
  /// V45、V60：作废入口只改状态与作废事实，历史版本与明细保持可读。
  /// </summary>
  [Fact]
  public async Task Void_keeps_versions_and_details_readable()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    MedicalReportVersion beforeVoid = Assert.Single(repository.Versions.Values);
    await WithCapturedEventsAsync(() => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: ReceivedTime)));

    MedicalReportVersion afterVoid = Assert.Single(repository.Versions.Values);
    Assert.Equal(beforeVoid.Id, afterVoid.Id);
    Assert.Equal(beforeVoid.PdfFileId, afterVoid.PdfFileId);
    Assert.Equal(beforeVoid.ReportTime, afterVoid.ReportTime);
    Assert.Single(repository.LaboratoryContents);
    Assert.Equal(2, repository.LaboratoryResultItems.Count);
  }

  /// <summary>
  /// V17：提交接口成功返回布尔真，且写入的报告、版本与明细可回读一致。
  /// </summary>
  /// <remarks>回读按业务标识定位报告并逐字段核对，不只验证返回值为真。</remarks>
  [Fact]
  public async Task Successful_submission_writes_readable_report_version_and_content()
  {
    FakeReportRepository repository = new();

    (_, bool result) = await ExecuteAsync(
      () => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.True(result);

    MedicalRecognitionReport readBack = repository.Reports.Values
      .Single(report => report.OrganizationCode == TrustedOrganization && report.HospitalCode == TrustedHospital
        && report.BranchCode == TrustedBranch && report.ReportType == MedicalReportType.Laboratory && report.ReportNo == ReportNo);
    Assert.Equal("张三", readBack.PatientName);
    Assert.Equal(IdentityDocumentNo, readBack.IdentityDocumentNo);

    MedicalReportVersion version = repository.Versions[readBack.CurrentVersionId];
    Assert.Equal("报告名称", version.SourceReportName);
    Assert.Equal("李医生", version.ReportDoctorName);
    Assert.Equal("王医生", version.ReviewDoctorName);
    Assert.Equal(VisitType.Outpatient, version.VisitType);
    Assert.Equal("V-1", version.VisitSerialNo);
    Assert.Equal("lab-1.pdf", version.PdfFileName);

    LaboratoryReportContent content = Assert.Single(repository.LaboratoryContents);
    Assert.Equal(version.Id, content.ReportVersionId);
    Assert.Equal("S-1", content.SourceSpecimenNo);
    Assert.Equal("检验人", content.InspectorName);

    LaboratoryResultItem item = repository.LaboratoryResultItems.OrderBy(result => result.DisplayOrder).First();
    Assert.Equal(version.Id, item.ReportVersionId);
    Assert.Equal("白细胞计数", item.SourceProjectName);
    Assert.Equal("6.5", item.SourceResultText);
  }

  /// <summary>
  /// V38：检验报告作废成功后再次提交同一报告被拒绝，且不追加版本、不登记事件。
  /// </summary>
  /// <remarks>
  /// 与 V4 的拒绝语义同源，但本用例把断言落在「作废后不追加版本」这一事实面上：
  /// 版本行数、明细行数与作废后的当前版本指向都不因再次提交而改变。
  /// </remarks>
  [Fact]
  public async Task Voided_laboratory_report_stops_appending_versions()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    MedicalRecognitionReport beforeVoid = Assert.Single(repository.Reports.Values);
    Guid versionBeforeVoid = beforeVoid.CurrentVersionId;
    await WithCapturedEventsAsync(() => manager.VoidLaboratoryReportAsync(VoidCommand(voidedTime: ReceivedTime)));

    int versionsBefore = repository.Versions.Count;
    int itemsBefore = repository.LaboratoryResultItems.Count;

    // 再次提交同一业务标识的完整报告：按报告已作废拒绝。
    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => manager.SubmitCompleteLaboratoryReportAsync(LaboratoryCommand()));

    Assert.Equal("业务拒绝：报告已作废，不能再次提交。", error.Message);
    Assert.Equal(versionsBefore, repository.Versions.Count);
    Assert.Equal(itemsBefore, repository.LaboratoryResultItems.Count);
    Assert.Equal(versionBeforeVoid, Assert.Single(repository.Reports.Values).CurrentVersionId);
  }

  /// <summary>
  /// V58：检查报告作废成功后再次提交同一报告被拒绝，且不追加版本、不登记事件。
  /// </summary>
  /// <remarks>与 V38 同一口径，落在检查链路，使两条报告类型的「作废后停止追加」都有独立断言。</remarks>
  [Fact]
  public async Task Voided_examination_report_stops_appending_versions()
  {
    FakeReportRepository repository = new();
    MedicalRecognitionReportManager manager = CreateManager(repository);
    await WithCapturedEventsAsync(() => manager.SubmitCompleteExaminationReportAsync(ExaminationCommand()));

    await WithCapturedEventsAsync(() => manager.VoidExaminationReportAsync(ExaminationVoidCommand()));
    int versionsBefore = repository.Versions.Count;
    int itemsBefore = repository.ExaminationItems.Count;

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => manager.SubmitCompleteExaminationReportAsync(ExaminationCommand()));

    Assert.Equal("业务拒绝：报告已作废，不能再次提交。", error.Message);
    Assert.Equal(versionsBefore, repository.Versions.Count);
    Assert.Equal(itemsBefore, repository.ExaminationItems.Count);
  }

  /// <summary>
  /// V41：普通检验明细的可选来源字段按来源原值保存，不做解析、归一或裁切。
  /// </summary>
  /// <remarks>
  /// 判据是持久化后的实体取值与请求取值逐字段相等，而不是只验证请求被接收；
  /// 单位与参考范围含特殊字符，用于确认保存的是来源原文。
  /// </remarks>
  [Fact]
  public async Task Laboratory_result_source_fields_are_saved_verbatim()
  {
    FakeReportRepository repository = new();
    LaboratoryResultItemRequest request = ResultItem() with
    {
      Unit = "×10^9/L(±)",
      ReferenceRange = "4.0 - 10.0 参考",
      TestingMethod = "仪器法/复检",
      InstrumentCode = "I-1",
      InstrumentName = "全自动分析仪",
      LaboratoryChargeItemCode = "CH-1",
      MedicalInsuranceChargeItemCode = "MI-1",
      AbnormalFlag = LaboratoryAbnormalFlag.High,
      CriticalValueFlag = true
    };

    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [request])));

    LaboratoryResultItem saved = Assert.Single(repository.LaboratoryResultItems);
    Assert.Equal(request.Unit, saved.Unit);
    Assert.Equal(request.ReferenceRange, saved.ReferenceRange);
    Assert.Equal(request.TestingMethod, saved.TestingMethod);
    Assert.Equal(request.InstrumentCode, saved.InstrumentCode);
    Assert.Equal(request.InstrumentName, saved.InstrumentName);
    Assert.Equal(request.LaboratoryChargeItemCode, saved.LaboratoryChargeItemCode);
    Assert.Equal(request.MedicalInsuranceChargeItemCode, saved.InsuranceChargeItemCode);
    Assert.Equal(request.AbnormalFlag, saved.AbnormalFlag);
    Assert.Equal(request.CriticalValueFlag, saved.CriticalValueFlag);
    Assert.Equal(request.SourceResultText, saved.SourceResultText);
  }

  /// <summary>
  /// V43：异常标志只接受受控四值，危急值标志缺失时仍接收。
  /// </summary>
  /// <remarks>
  /// 四值逐一提交确认可保存；缺失危急值标志时接收并把该列保存为无业务值。
  /// </remarks>
  [Theory]
  [InlineData(LaboratoryAbnormalFlag.Normal)]
  [InlineData(LaboratoryAbnormalFlag.High)]
  [InlineData(LaboratoryAbnormalFlag.Low)]
  [InlineData(LaboratoryAbnormalFlag.OtherAbnormal)]
  public async Task Abnormal_flag_accepts_the_four_defined_values(LaboratoryAbnormalFlag flag)
  {
    FakeReportRepository repository = new();
    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [ResultItem() with { AbnormalFlag = flag }])));

    Assert.Equal(flag, Assert.Single(repository.LaboratoryResultItems).AbnormalFlag);
  }

  /// <summary>V43：危急值标志缺失时仍接收报告，保存为无业务值。</summary>
  [Fact]
  public async Task Missing_critical_value_flag_is_accepted()
  {
    FakeReportRepository repository = new();
    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteLaboratoryReportAsync(LaboratoryCommand(results: [ResultItem() with { CriticalValueFlag = null }])));

    Assert.Null(Assert.Single(repository.LaboratoryResultItems).CriticalValueFlag);
  }

  /// <summary>
  /// V48：检查项目来源项目名称缺失即拒绝；互认项目编码缺失时仍接收。
  /// </summary>
  /// <remarks>两组对照把必填与可空区分开：名称是必填，互认项目编码缺失只影响匹配资格。</remarks>
  [Fact]
  public async Task Examination_item_requires_source_project_name_but_not_standard_code()
  {
    FakeReportRepository repository = new();

    // 名称缺失即拒绝，且不写入任何内容。
    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => CreateManager(repository).SubmitCompleteExaminationReportAsync(
        ExaminationCommand(items: [ExaminationItemRequestFixture() with { SourceProjectName = "  " }])));
    Assert.Equal("业务拒绝：来源项目名称不能为空或空白。", error.Message);
    Assert.Empty(repository.ExaminationItems);

    // 互认项目编码缺失仍接收，且按缺失保存。
    await WithCapturedEventsAsync(() => CreateManager(repository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(items: [ExaminationItemRequestFixture() with { StandardProjectCode = null }])));
    Assert.Null(Assert.Single(repository.ExaminationItems).StandardProjectCode);
  }

  /// <summary>
  /// V50：检查部位随所属项目保存与回读，不在报告中重复归属。
  /// </summary>
  /// <remarks>
  /// 两个项目各带不同部位：部位的所属项目标识必须分别指向各自的检查项目，
  /// 且报告级内容不承载部位字段，避免同一部位在两级重复归属。
  /// </remarks>
  [Fact]
  public async Task Examination_sites_belong_to_their_own_item_only()
  {
    FakeReportRepository repository = new();
    ExaminationItemRequest first = ExaminationItemRequestFixture() with
    {
      SourceProjectCode = "P-1",
      Sites = [new ExaminationSiteRequest { SiteName = "右肺上叶" }, new ExaminationSiteRequest { SiteName = "纵隔" }]
    };
    ExaminationItemRequest second = ExaminationItemRequestFixture() with
    {
      SourceProjectCode = "P-2",
      Sites = [new ExaminationSiteRequest { SiteName = "左肺下叶" }]
    };

    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteExaminationReportAsync(ExaminationCommand(items: [first, second])));

    List<ExaminationItem> items = [.. repository.ExaminationItems.OrderBy(item => item.SourceProjectCode)];
    Assert.Equal(2, items.Count);
    Assert.Equal(2, repository.ExaminationSites.Count(site => site.ExaminationItemId == items[0].Id));
    Assert.Single(repository.ExaminationSites.Where(site => site.ExaminationItemId == items[1].Id));
    // 部位集合分别归属各自项目，不存在跨项目归属。
    Assert.DoesNotContain(repository.ExaminationSites, site => site.ExaminationItemId != items[0].Id && site.ExaminationItemId != items[1].Id);
  }

  /// <summary>
  /// V53：检查报告的报告级内容按来源原文保存，不拆分到检查项目。
  /// </summary>
  /// <remarks>检查所见与结论为长文本，逐字比对确认未裁切、未归一。</remarks>
  [Fact]
  public async Task Examination_report_level_content_is_saved_verbatim()
  {
    FakeReportRepository repository = new();
    const string findings = "双肺纹理增粗、紊乱，可见散在斑片状高密度影（长约 200 字的长文本占位）。";
    const string conclusion = "考虑双肺感染性病变，建议结合临床及实验室检查进一步明确。";
    ExaminationReportContentRequest content = ExaminationContent() with
    {
      Findings = findings,
      Conclusion = conclusion,
      ConditionDescription = "咳嗽三天",
      ExaminationPurpose = "明确肺部病变性质",
      ExaminationMethod = "平扫加增强",
      DeviceCode = "CT-1",
      DeviceName = "64 排螺旋 CT"
    };

    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteExaminationReportAsync(ExaminationCommand(content: content)));

    ExaminationReportContent saved = Assert.Single(repository.ExaminationContents);
    Assert.Equal(findings, saved.Findings);
    Assert.Equal(conclusion, saved.Conclusion);
    Assert.Equal(content.ConditionDescription, saved.ConditionDescription);
    Assert.Equal(content.ExaminationPurpose, saved.ExaminationPurpose);
    Assert.Equal(content.ExaminationMethod, saved.ExaminationMethod);
    Assert.Equal(content.DeviceCode, saved.DeviceCode);
    Assert.Equal(content.DeviceName, saved.DeviceName);
  }

  /// <summary>
  /// V54：来源检查类型与来源诊断的编码、名称按来源值保存；两类字段缺失时仍接收。
  /// </summary>
  /// <remarks>两组对照覆盖「应填字段提供」与「应填字段缺失」：提供时逐字保存，缺失时保存为无业务值。</remarks>
  [Fact]
  public async Task Examination_source_type_and_diagnosis_are_saved_verbatim_or_null()
  {
    FakeReportRepository repository = new();
    ExaminationReportContentRequest provided = ExaminationContent() with
    {
      SourceExaminationTypeCode = "T-CT",
      SourceExaminationTypeName = "CT 检查",
      SourceDiagnosisCode = "DG-1",
      SourceDiagnosisName = "肺部感染"
    };

    await WithCapturedEventsAsync(() => CreateManager(repository)
      .SubmitCompleteExaminationReportAsync(ExaminationCommand(content: provided)));

    ExaminationReportContent saved = Assert.Single(repository.ExaminationContents);
    Assert.Equal("T-CT", saved.SourceExaminationTypeCode);
    Assert.Equal("CT 检查", saved.SourceExaminationTypeName);
    Assert.Equal("DG-1", saved.SourceDiagnosisCode);
    Assert.Equal("肺部感染", saved.SourceDiagnosisName);

    // 缺失一组：来源检查类型与诊断编码、名称都留空时仍接收，并按缺失保存。
    FakeReportRepository missingRepository = new();
    await WithCapturedEventsAsync(() => CreateManager(missingRepository).SubmitCompleteExaminationReportAsync(
      ExaminationCommand(content: ExaminationContent() with
      {
        SourceExaminationTypeCode = null,
        SourceExaminationTypeName = null,
        SourceDiagnosisCode = null
      })));

    ExaminationReportContent missing = Assert.Single(missingRepository.ExaminationContents);
    Assert.Null(missing.SourceExaminationTypeCode);
    Assert.Null(missing.SourceExaminationTypeName);
    Assert.Null(missing.SourceDiagnosisCode);
  }

  /// <summary>
  /// V34：药敏记录的归属字段不可为空，持久化后不产生无法归属的记录。
  /// </summary>
  /// <remarks>
  /// 药敏本身不带报告版本标识，归属靠所属细菌鉴定结果；因此判据是每一条已保存药敏的
  /// <c>BacteriaResultId</c> 都是非空 Guid，且能在已保存的细菌鉴定结果里找到对应归属。
  /// </remarks>
  [Fact]
  public async Task Susceptibility_records_are_always_attributable()
  {
    FakeReportRepository repository = new();
    await WithCapturedEventsAsync(() => CreateManager(repository).SubmitCompleteLaboratoryReportAsync(
      LaboratoryCommand(bacteriaResults:
      [
        BacteriaResult(
          [
            Susceptibility() with { SourceDetailKey = "SD-1" },
            Susceptibility() with { SourceDetailKey = "SD-2", DisplayOrder = 2, DrugCode = "DRUG-2" }
          ], "B-1"),
        BacteriaResult(
          [
            Susceptibility() with { SourceDetailKey = "SD-3" },
            Susceptibility() with { SourceDetailKey = "SD-4", DisplayOrder = 2, DrugCode = "DRUG-3" }
          ], "B-2")
      ])));

    Assert.NotEmpty(repository.Susceptibilities);
    HashSet<Guid> bacteriaIds = [.. repository.BacteriaResults.Select(bacteria => bacteria.Id)];
    foreach (LaboratoryAntimicrobialSusceptibility saved in repository.Susceptibilities)
    {
      Assert.NotEqual(Guid.Empty, saved.BacteriaResultId);
      Assert.Contains(saved.BacteriaResultId, bacteriaIds);
    }

    // 归属按所属细菌分组，不存在跨细菌或未归属的记录。
    Assert.Equal(
      repository.BacteriaResults.Count,
      repository.Susceptibilities.Select(item => item.BacteriaResultId).Distinct().Count());
  }

  /// <summary>
  /// 在捕获领域事件的前提下执行写操作。
  /// </summary>
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

  /// <summary>在捕获领域事件的前提下顺序执行一组写操作与断言。</summary>
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

  /// <summary>构建报告领域管理器，注入内存仓储替身。</summary>
  /// <param name="repository">报告读写的内存替身。</param>
  /// <returns>可直接调用的领域管理器。</returns>
  private static MedicalRecognitionReportManager CreateManager(FakeReportRepository repository) => new(repository);

  /// <summary>固定的患者性别代码。</summary>
  private const string ProtocolGenderCode = "1";

  /// <summary>固定的患者出生日期。</summary>
  private static readonly DateTime PatientBirthDate = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

  /// <summary>构建公共版本信息。</summary>
  /// <returns>与设计填报要求一致的合法公共版本信息。</returns>
  private static MedicalReportVersionRequest CommonVersion() => new()
  {
    SourceReportName = "报告名称",
    PatientName = "张三",
    PatientGenderCode = ProtocolGenderCode,
    PatientBirthDate = PatientBirthDate,
    PatientPhoneNumber = "13800000000",
    AgeAtReport = "36岁",
    IdentityDocumentTypeCode = "01",
    IdentityDocumentNo = IdentityDocumentNo,
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
    ReportDoctorName = "李医生",
    ReviewDoctorId = "DOC-C",
    ReviewDoctorName = "王医生",
    ApplicationTime = ApplicationTime,
    ReportTime = ReportTime,
    ReviewTime = ReviewTime,
    SourceModifiedTime = SourceModifiedTime,
    SourceConfidentialFlag = "0"
  };

  /// <summary>构建检验专项内容。</summary>
  /// <returns>与设计填报要求一致的合法检验专项内容。</returns>
  private static LaboratoryReportContentRequest LaboratoryContent() => new()
  {
    ReportCategoryCode = "C-1",
    ReportCategoryName = "生化",
    ReportRemark = "备注",
    OverallAbnormalFlag = "N",
    SourceOrderSerialNo = "O-1",
    SpecimenCollectedTime = ApplicationTime,
    SpecimenSubmittedTime = ApplicationTime.AddMinutes(10),
    LaboratoryReceivedTime = ApplicationTime.AddMinutes(20),
    SourceSpecimenNo = "S-1",
    SpecimenTypeCode = "T-1",
    SpecimenTypeName = "血清",
    TestingCompletedTime = ReportTime,
    InspectorId = "U-1",
    InspectorName = "检验人"
  };

  /// <summary>构建一条普通检验结果。</summary>
  /// <returns>与设计填报要求一致的合法普通结果。</returns>
  private static LaboratoryResultItemRequest ResultItem() => new()
  {
    SourceDetailKey = "D-1",
    SourceProjectName = "白细胞计数",
    SourceProjectCode = "P-1",
    StandardProjectCode = "SP-1",
    SourceResultText = "6.5",
    ResultType = LaboratoryResultType.Numeric,
    LoincCode = "LOINC-1",
    Unit = "10^9/L",
    ReferenceRange = "4.0-10.0",
    TestingMethod = "仪器法",
    InstrumentCode = "I-1",
    InstrumentName = "分析仪",
    DisplayOrder = 1,
    AbnormalFlag = LaboratoryAbnormalFlag.Normal,
    CriticalValueFlag = false,
    LaboratoryChargeItemCode = "CH-1",
    MedicalInsuranceChargeItemCode = "MI-1",
    InspectorId = "U-1",
    InspectorName = "检测人"
  };

  /// <summary>构建一条药敏结果。</summary>
  /// <returns>与设计填报要求一致的合法药敏结果。</returns>
  private static LaboratoryAntimicrobialSusceptibilityRequest Susceptibility() => new()
  {
    SourceDetailKey = "SD-1",
    DrugCode = "DRUG-1",
    DrugName = "青霉素",
    SusceptibilityCode = "S",
    SourceConclusionText = "敏感",
    ResistanceResultCode = "R-0",
    DiskContent = "30μg",
    MicValue = "≤0.25",
    InhibitionZoneDiameter = "20mm",
    ReferenceValue = "≤1",
    DisplayOrder = 1,
    InspectorId = "U-1",
    InspectorName = "检测人",
    TestingMethod = "纸片法",
    TestPanelOrder = "1"
  };

  /// <summary>构建一条细菌鉴定结果。</summary>
  /// <param name="susceptibilities">该条细菌下的药敏结果集合。</param>
  /// <param name="sourceDetailKey">来源明细标识；同一报告版本内必须唯一。</param>
  /// <returns>与设计填报要求一致的合法细菌鉴定结果。</returns>
  private static LaboratoryBacteriaResultRequest BacteriaResult(
    IReadOnlyList<LaboratoryAntimicrobialSusceptibilityRequest>? susceptibilities = null,
    string sourceDetailKey = "B-1") => new()
    {
      SourceDetailKey = sourceDetailKey,
      SourceOrganismCode = "ORG-1",
      SourceOrganismName = "大肠埃希菌",
      SourceResultText = "检出大肠埃希菌",
      DetectionConclusion = "检出大肠埃希菌",
      ColonyCount = "1+",
      CultureMedium = "血平板",
      CultureTime = "24h",
      CultureCondition = "35℃",
      DiscoveryMethod = "培养",
      DetectionMethod = "生化",
      Description = "描述",
      InstrumentCode = "I-2",
      InstrumentName = "鉴定仪",
      TestPanelCode = "TP-1",
      TestPanelName = "鉴定板",
      InspectorId = "U-1",
      InspectorName = "检测人",
      Susceptibilities = susceptibilities ?? []
    };

  /// <summary>构建检查专项内容。</summary>
  /// <returns>与设计填报要求一致的合法检查专项内容。</returns>
  private static ExaminationReportContentRequest ExaminationContent() => new()
  {
    SourceExaminationTypeCode = "ET-1",
    SourceExaminationTypeName = "CT",
    ReportRemark = "备注",
    OverallAbnormalFlag = "N",
    Findings = "所见原文",
    Conclusion = "结论原文",
    ConditionDescription = "病情描述",
    ExaminationPurpose = "检查目的",
    SourceDiagnosisCode = "DG-1",
    SourceDiagnosisName = "诊断名称",
    ExaminationTime = ReportTime,
    ExaminerId = "DOC-1",
    ExaminerName = "检查医生",
    SourceImageStatus = SourceImageStatus.Available,
    ImageAccessUrl = "http://image/1",
    ExaminationMethod = "平扫",
    DeviceCode = "DEV-1",
    DeviceName = "CT 机"
  };

  /// <summary>构建一条检查项目。</summary>
  /// <returns>与设计填报要求一致的合法检查项目。</returns>
  private static ExaminationItemRequest ExaminationItemRequestFixture() => new()
  {
    SourceProjectName = "胸部 CT",
    SourceProjectCode = "EP-1",
    StandardProjectCode = "SP-2",
    Sites = [new ExaminationSiteRequest { SourceSiteCode = "ST-1", SiteName = "胸部" }]
  };

  /// <summary>构建完整检验报告文档。</summary>
  /// <returns>两份普通结果、一条细菌结果与一条药敏结果的完整文档。</returns>
  private static LaboratoryReportVersionRequest LaboratoryDocument() => new()
  {
    ReportNo = ReportNo,
    Version = CommonVersion(),
    Content = LaboratoryContent(),
    Results = [ResultItem(), ResultItem() with { SourceDetailKey = "D-2", SourceProjectName = "红细胞计数", DisplayOrder = 2 }],
    BacteriaResults = [BacteriaResult(susceptibilities: [Susceptibility()])]
  };

  /// <summary>构建完整检查报告文档。</summary>
  /// <returns>一条项目与一条部位的完整文档。</returns>
  private static ExaminationReportVersionRequest ExaminationDocument() => new()
  {
    ReportNo = ExaminationReportNo,
    Version = CommonVersion(),
    Content = ExaminationContent(),
    Items = [ExaminationItemRequestFixture()]
  };

  /// <summary>构建检验报告提交命令。</summary>
  /// <param name="document">报告文档；缺省为合法检验文档。</param>
  /// <param name="content">检验专项内容；缺省为文档自带内容。</param>
  /// <param name="results">普通结果集合；缺省为文档自带集合。</param>
  /// <param name="bacteriaResults">细菌结果集合；缺省为文档自带集合。</param>
  /// <param name="receivedTime">本次请求的接收时间；缺省为固定值。</param>
  /// <param name="branchCode">院区编码；缺省为可信院区。</param>
  /// <returns>可直接交给领域管理器的提交命令。</returns>
  private static SubmitCompleteLaboratoryReportCommand LaboratoryCommand(
    LaboratoryReportVersionRequest? document = null,
    LaboratoryReportContentRequest? content = null,
    IReadOnlyList<LaboratoryResultItemRequest>? results = null,
    IReadOnlyList<LaboratoryBacteriaResultRequest>? bacteriaResults = null,
    DateTime? receivedTime = null,
    string branchCode = TrustedBranch)
  {
    LaboratoryReportVersionRequest source = document ?? LaboratoryDocument();
    return new SubmitCompleteLaboratoryReportCommand
    {
      ReportNo = source.ReportNo,
      Version = source.Version,
      Content = content ?? source.Content,
      Results = results ?? source.Results,
      BacteriaResults = bacteriaResults ?? source.BacteriaResults,
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = branchCode,
      PdfFileId = "key/one.pdf",
      PdfFileName = "lab-1.pdf",
      OperId = TrustedOperId,
      OperTime = CommandOperTime,
      ReceivedTime = receivedTime ?? ReceivedTime
    };
  }

  /// <summary>构建检查报告提交命令。</summary>
  /// <param name="document">报告文档；缺省为合法检查文档。</param>
  /// <param name="content">检查专项内容；缺省为文档自带内容。</param>
  /// <param name="items">检查项目集合；缺省为文档自带集合。</param>
  /// <param name="receivedTime">本次请求的接收时间；缺省为固定值。</param>
  /// <returns>可直接交给领域管理器的提交命令。</returns>
  private static SubmitCompleteExaminationReportCommand ExaminationCommand(
    ExaminationReportVersionRequest? document = null,
    ExaminationReportContentRequest? content = null,
    IReadOnlyList<ExaminationItemRequest>? items = null,
    DateTime? receivedTime = null)
  {
    ExaminationReportVersionRequest source = document ?? ExaminationDocument();
    return new SubmitCompleteExaminationReportCommand
    {
      ReportNo = source.ReportNo,
      Version = source.Version,
      Content = content ?? source.Content,
      Items = items ?? source.Items,
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      PdfFileId = "key/two.pdf",
      PdfFileName = "exm-1.pdf",
      OperId = TrustedOperId,
      OperTime = CommandOperTime,
      ReceivedTime = receivedTime ?? ReceivedTime
    };
  }

  /// <summary>构建检验报告作废命令。</summary>
  /// <param name="voidedTime">作废时间；缺省为当前版本平台接收时间。</param>
  /// <param name="voidReason">作废原因；缺省为固定文案。</param>
  /// <param name="requestReceivedTime">本次请求的接收时间；缺省为固定值。</param>
  /// <returns>可直接交给领域管理器的作废命令。</returns>
  private static VoidLaboratoryReportCommand VoidCommand(
    DateTime? voidedTime = null,
    string voidReason = "重复报告",
    DateTime? requestReceivedTime = null) => new()
    {
      ReportNo = ReportNo,
      VoidedTime = voidedTime ?? ReceivedTime,
      VoidReason = voidReason,
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      OperId = TrustedOperId,
      OperTime = CommandOperTime,
      ReceivedTime = requestReceivedTime ?? ReceivedTime
    };

  /// <summary>构建检查报告作废命令；报告单号取检查链路的固定单号。</summary>
  /// <param name="voidedTime">作废时间；缺省为当前版本平台接收时间。</param>
  /// <param name="voidReason">作废原因；缺省为固定文案。</param>
  /// <param name="requestReceivedTime">本次请求的接收时间；缺省为固定值。</param>
  /// <returns>可直接交给领域管理器的检查报告作废命令。</returns>
  private static VoidExaminationReportCommand ExaminationVoidCommand(
    DateTime? voidedTime = null,
    string voidReason = "重复报告",
    DateTime? requestReceivedTime = null) => new()
    {
      ReportNo = ExaminationReportNo,
      VoidedTime = voidedTime ?? ReceivedTime,
      VoidReason = voidReason,
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      OperId = TrustedOperId,
      OperTime = CommandOperTime,
      ReceivedTime = requestReceivedTime ?? ReceivedTime
    };

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
}
