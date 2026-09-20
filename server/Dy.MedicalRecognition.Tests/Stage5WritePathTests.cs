using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;
using Dy.MedicalRecognition.Domain.Queries;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 互认匹配请求、互认处理结果提交与引用结果提交的应用编排与领域写入校验（矩阵 V1-V13、V17-V22 含 V20b 与 V22b、V23-V45 含 V35b 与 V45b、V59-V72 含 V70b 与 V72b、V89-V94 的应用层可测部分，以及 V95 的引用侧与处理结果侧面）。
/// </summary>
/// <remarks>
/// 用例把手写仓储替身、查询端口替身、外部组织与用户服务替身与系统参数服务替身注入公开应用服务入口，
/// 验证可观察结果：匹配响应内容、处理结果内容与保存时间、引用事实内容、写入的匹配记录与匹配项、登记的领域事件、失败时的零写入与拒绝文案；
/// 空匹配与负向用例同时断言"无写入"与"无事件"。
/// V14（报告已作废或报告版本非当前有效）、V15（报告已形成后续版本）与 V16（来源组织与接收组织不一致）三条由候选语句的
/// 生命周期状态筛选、当前版本内联连接与来源组织范围三条判据在 <see cref="Stage5SqlMapTests"/> 的静态面承接，不在本文件内构造行为用例。
/// V92（未认证请求被拒绝）的取证面是框架认证失败路径的对外响应，属真实宿主取证，不在本文件内承接。
/// V33、V35b 与 V36 三条在本文件内只取得请求校验面的拒绝证据：三条的取值与成对规则先由公开请求类型上的枚举、必填与非空白校验拦截，
/// 拒绝发生在任何领域读取与写入之前；领域层的同口径判定为防御性分支、经公开入口不可达，本文件不为其提供行为证据，
/// 因此这三条用例不构成矩阵所声明的 Domain 层覆盖。
/// V65 的引用侧另有域名层证据：引用科室与引用医生的成对判定在管理器内以 <see cref="MedicalRecognitionReportManager"/> 直接调用取证，
/// 该判定经公开入口同样不可达（请求契约的非空白校验先拦截），因此两条路径分别取证、互不替代。
/// V59、V60、V68、V69、V70、V70b、V71、V72 与 V72b 的"真实 WorkUnit"列为"是"，本文件只取证写入成功与写路径零写入这两个子面，
/// 整批零提交的真实数据库失败注入由票 08 取证；本文件不构造真实库前置，也不把需要真实库的条目写成通过。
/// 时间用固定输入与相对当前时点的偏移构造，不依赖等待。
/// </remarks>
public sealed class Stage5WritePathTests
{
  /// <summary>可信组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>可信医院编码。</summary>
  private const string TrustedHospital = "HOS-A";

  /// <summary>可信院区编码。</summary>
  private const string TrustedBranch = "BRH-A";

  /// <summary>另一家医院编码，用于构造本院报告与他院报告两种来源。</summary>
  private const string OtherHospital = "HOS-B";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>平台患者标识。</summary>
  private static readonly Guid PatientId = Guid.Parse("8b1e6d4a-2c33-4f57-9a10-5d6e7f8a9b0c");

  /// <summary>证件类型代码。</summary>
  private const string IdentityDocumentTypeCode = "01";

  /// <summary>证件号码；用例内按需要加入首尾空白与小写差异。</summary>
  private const string IdentityDocumentNo = "110101199001011234";

  /// <summary>本次来源就诊流水号。</summary>
  private const string VisitSerialNo = "V-2026-0001";

  /// <summary>本院报告匹配开关参数编码。</summary>
  private const string OwnHospitalMatchSwitch = "own_hospital_match_switch";

  /// <summary>本院报告排除时长参数编码。</summary>
  private const string OwnHospitalExcludeHours = "own_hospital_exclude_hours";

  /// <summary>第一条标准项目编码。</summary>
  private const string FirstProjectCode = "SP-1";

  /// <summary>第二条标准项目编码。</summary>
  private const string SecondProjectCode = "SP-2";

  /// <summary>第三条标准项目编码，用于构造部分命中与未命中场景。</summary>
  private const string ThirdProjectCode = "SP-3";

  /// <summary>第一份报告明细的来源项目名称；与标准目录名称是两个来源，用于判定标准项目名称的取值来源。</summary>
  /// <remarks>该取值按来源项目名称的统一形态由第一个标准项目编码派生，见 <see cref="SourceProjectNameFor"/>。</remarks>
  private const string SourceProjectNameOfFirstResult = "血常规-白细胞计数";

  /// <summary>来源报告明细的项目名称形态：按该明细自身的标准项目编码命名，使不同报告明细的名称可分辨。</summary>
  /// <param name="standardProjectCode">该明细的标准项目编码。</param>
  /// <returns>该明细的来源项目名称。</returns>
  private static string SourceProjectNameFor(string standardProjectCode) => $"{standardProjectCode} 结果";

  /// <summary>
  /// V1 与 V21：非空匹配生成一条匹配记录与若干匹配项，返回报告公共信息与项目级内容，并登记一次匹配已返回事件。
  /// </summary>
  /// <remarks>同一份联合报告命中两个互认项目时报告公共信息只返回一次，两个项目分别成项并关联同一报告版本。</remarks>
  [Fact]
  public async Task Non_empty_match_writes_record_and_items_and_returns_report_content()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);
    DateTime clinicalTime = ClinicalTime;
    DateTime reportTime = ReportTime;
    Guid reportVersionId = AddLaboratoryReportFacts(
      fixture,
      FirstProjectCode,
      TrustedHospital,
      matchBaselineTime: clinicalTime,
      reportTime: reportTime,
      sourceProjectName: SourceProjectNameOfFirstResult,
      additionalResultProjectCodes: new[] { SecondProjectCode });
    // 同一份联合报告同时成为第二个项目的候选：联合报告命中多个互认项目时报告公共信息只返回一次。
    fixture.QueryRepository.AddLaboratoryCandidate(
      SecondProjectCode, Assert.Single(fixture.QueryRepository.CandidateReports).ReportId, reportVersionId, TrustedHospital, clinicalTime, reportTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, SecondProjectCode]);

    Assert.True(response.HasMatches);
    Assert.NotNull(response.RecognitionMatchRecordId);
    Assert.NotNull(response.MatchCreatedTime);

    // 匹配记录：接收三值、患者证件、就诊标识与匹配生成时间都已写入，处理结果保存时间留空表示尚待处理。
    RecognitionMatchRecord record = Assert.Single(fixture.Repository.MatchRecords.Values);
    Assert.Equal(response.RecognitionMatchRecordId, record.Id);
    Assert.Equal(TrustedOrganization, record.ReceiverOrganizationCode);
    Assert.Equal(TrustedHospital, record.ReceiverHospitalCode);
    Assert.Equal(TrustedBranch, record.ReceiverBranchCode);
    Assert.Equal(IdentityDocumentTypeCode, record.IdentityDocumentTypeCode);
    Assert.Equal(IdentityDocumentNo, record.IdentityDocumentNo);
    Assert.Equal(VisitType.Outpatient, record.VisitType);
    Assert.Equal(VisitSerialNo, record.VisitSerialNo);
    Assert.Equal(response.MatchCreatedTime, record.MatchCreatedTime);
    Assert.Null(record.DecisionSavedTime);
    Assert.Equal(TrustedOperId, record.OperId);
    // 匹配项：两个项目各成一项并关联同一报告版本，项目编码与项目类型取自标准项目所属分类。
    Assert.Equal(2, fixture.Repository.MatchItems.Count);
    Assert.All(fixture.Repository.MatchItems.Values, item =>
    {
      Assert.Equal(record.Id, item.RecognitionMatchRecordId);
      Assert.Equal(MedicalItemType.Laboratory, item.ItemType);
      Assert.Equal(reportVersionId, item.ReportVersionId);
    });
    Assert.Equal(
      [.. fixture.Repository.MatchItems.Values.Select(item => item.StandardProjectCode).Order(StringComparer.Ordinal)],
      [FirstProjectCode, SecondProjectCode]);

    // 报告公共信息只返回一次，两个命中项目在同一报告对象下逐项返回。
    RecognitionMatchReportReadModel report = Assert.Single(response.Reports);
    Assert.Equal(reportVersionId, report.ReportVersionId);
    Assert.Equal(MedicalReportType.Laboratory, report.ReportType);
    Assert.Equal("LAB-2026-0001", report.ReportNo);
    Assert.Equal("血常规报告", report.ReportName);
    Assert.Equal(TrustedOrganization, report.SourceOrganizationCode);
    Assert.Equal("示范组织", report.SourceOrganizationName);
    Assert.Equal(TrustedHospital, report.SourceHospitalCode);
    Assert.Equal("示范医院", report.SourceHospitalName);
    Assert.Equal(TrustedBranch, report.SourceBranchCode);
    Assert.Equal("示范院区", report.SourceBranchName);
    Assert.Equal(clinicalTime, report.ClinicalTime);
    Assert.Equal(reportTime, report.ReportTime);
    Assert.Equal("pdf-key-1", report.File.PdfFileId);
    Assert.Equal("血常规报告.pdf", report.File.PdfOriginalFileName);
    Assert.Null(report.File.SourceImageStatus);
    Assert.Equal(2, report.MatchItems.Count);
    Assert.Equal(
      [FirstProjectCode, SecondProjectCode],
      report.MatchItems.Select(item => item.StandardProjectCode));

    // 每个命中项目返回本项目的检验结果明细与报告级标本类型名称与整体异常标识。
    // 标准项目名称实时取自本次组织的标准目录互认配置，与来源报告明细上的来源项目名称是两个来源，因此取值必须与本用例预置的来源项目名称不同。
    RecognitionMatchItemReadModel firstItem = report.MatchItems.Single(item => item.StandardProjectCode == FirstProjectCode);
    string firstStandardProjectName = fixture.QueryRepository.GetStandardItemName(TrustedOrganization, FirstProjectCode);
    Assert.NotEqual(SourceProjectNameOfFirstResult, firstStandardProjectName);
    Assert.Equal(firstStandardProjectName, firstItem.StandardProjectName);

    // 候选查询收到的是本次按证件解析到的平台患者标识：候选范围按报告主体上的平台患者限定，解析结果必须交到候选查询。
    Assert.Equal(PatientId, fixture.QueryRepository.CandidateQueryPatientId);
    // 项目级内容按本次绑定的报告版本集合一次读回：报告版本集合去重后只有一个版本，四条内容读取各一次。
    Assert.Equal(1, fixture.QueryRepository.ReportFactsQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.LaboratoryResultQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationItemQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationSiteQueryCalls);
    Assert.Equal("全血", firstItem.SpecimenTypeName);
    Assert.Equal("1", firstItem.OverallAbnormalFlag);
    RecognitionMatchLaboratoryResultReadModel result = Assert.Single(firstItem.LaboratoryResults);
    Assert.Equal(SourceProjectNameOfFirstResult, result.ResultItemName);
    Assert.Equal("9.8", result.SourceResultContent);
    Assert.Equal("10^9/L", result.Unit);
    Assert.Equal("4.0-10.0", result.SourceReferenceRange);
    Assert.Equal("偏高", result.SourceAbnormalFlag);
    Assert.Empty(firstItem.ExaminationSites);

    // 项目级内容按本次命中的标准项目编码分别取数：第二个项目只返回它自己的结果明细。
    RecognitionMatchItemReadModel secondItem = report.MatchItems.Single(item => item.StandardProjectCode == SecondProjectCode);
    RecognitionMatchLaboratoryResultReadModel secondResult = Assert.Single(secondItem.LaboratoryResults);
    Assert.Equal("SP-2 结果", secondResult.ResultItemName);
    Assert.Equal("正常", secondResult.SourceResultContent);

    // 只登记一次匹配已返回事件，事件承载记录标识、生成时间、接收三值与匹配项标识集合。
    RecognitionMatchesReturnedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
    Assert.Equal(record.Id, recordedEvent.RecognitionMatchRecordId);
    Assert.Equal(record.MatchCreatedTime, recordedEvent.MatchCreatedTime);
    Assert.Equal(TrustedOrganization, recordedEvent.ReceiverOrganizationCode);
    Assert.Equal(TrustedHospital, recordedEvent.ReceiverHospitalCode);
    Assert.Equal(TrustedBranch, recordedEvent.ReceiverBranchCode);
    Assert.Equal(2, recordedEvent.MatchedItemCount);
    Assert.Equal(
      [.. recordedEvent.MatchItemIds.OrderBy(id => id)],
      [.. fixture.Repository.MatchItems.Keys.OrderBy(id => id)]);
  }

  /// <summary>
  /// V1 的读取次数面：本次绑定的报告版本集合各自的内容都按集合一次读回，读取次数不随返回的报告数增长。
  /// </summary>
  /// <remarks>
  /// 两份各自独立的报告分属两个标准项目，因此报告事实、普通检验结果、检查项目与检查部位四条读取都只应各发生一次；
  /// 逐份报告各读一次时返回内容仍然正确，只有读取次数会随报告数增长，因此该约束必须由读取次数断言承担。
  /// </remarks>
  [Fact]
  public async Task Report_content_is_read_once_for_the_whole_batch_of_versions()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);

    Guid firstVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, sourceProjectName: SourceProjectNameOfFirstResult);
    Guid secondVersionId = AddLaboratoryReportFacts(fixture, SecondProjectCode, OtherHospital);
    Assert.NotEqual(firstVersionId, secondVersionId);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, SecondProjectCode]);

    Assert.True(response.HasMatches);
    Assert.Equal(2, response.Reports.Count);
    Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());

    // 按本次绑定的报告版本集合一次读回：两份报告分属两个版本，四条内容读取仍各一次，读取次数与报告数无关。
    Guid[] reportVersionIds = [.. response.Reports.Select(report => report.ReportVersionId).Distinct()];
    Assert.Equal(2, reportVersionIds.Length);
    Assert.Equal(1, fixture.QueryRepository.ReportFactsQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.LaboratoryResultQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationItemQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationSiteQueryCalls);

    // 一次读回的内容仍按报告版本与标准项目编码分别归位：两份报告各自只取到自己的结果明细。
    RecognitionMatchReportReadModel firstReport = response.Reports.Single(report => report.ReportVersionId == firstVersionId);
    RecognitionMatchReportReadModel secondReport = response.Reports.Single(report => report.ReportVersionId == secondVersionId);
    RecognitionMatchItemReadModel firstReportItem = Assert.Single(firstReport.MatchItems);
    RecognitionMatchItemReadModel secondReportItem = Assert.Single(secondReport.MatchItems);
    Assert.Equal(FirstProjectCode, firstReportItem.StandardProjectCode);
    Assert.Equal(SecondProjectCode, secondReportItem.StandardProjectCode);
    Assert.Equal(SourceProjectNameOfFirstResult, Assert.Single(firstReportItem.LaboratoryResults).ResultItemName);
    Assert.Equal($"{SecondProjectCode} 结果", Assert.Single(secondReportItem.LaboratoryResults).ResultItemName);
  }

  /// <summary>
  /// V2：没有任何报告命中时返回成功空结果，不创建记录、不登记事件。
  /// </summary>
  [Fact]
  public async Task Empty_match_returns_success_empty_result_without_writes()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    Assert.Null(response.RecognitionMatchRecordId);
    Assert.Null(response.MatchCreatedTime);
    Assert.Empty(response.Reports);
    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V8：患者不存在时返回成功空结果，不创建记录，也不读取候选报告。
  /// </summary>
  [Fact]
  public async Task Missing_patient_returns_success_empty_result_without_reading_candidates()
  {
    MatchFixture fixture = CreateFixture(patientExists: false);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    Assert.Empty(response.Reports);
    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Empty(events.Events);
    // 患者不可解析时不再读取候选报告：读取次数为零，避免为不存在的患者做无意义查询。
    Assert.Equal(0, fixture.QueryRepository.CandidateQueryCalls);
  }

  /// <summary>
  /// V3：项目编码不存在时整次失败，指出该标准项目编码，不创建记录、不登记事件。
  /// </summary>
  [Fact]
  public async Task Unknown_project_code_fails_the_whole_query_and_names_the_code()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode, ThirdProjectCode]);

    Assert.Contains(ThirdProjectCode, error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V4：项目编码未纳入当前组织互认范围时整次失败，指出该标准项目编码。
  /// </summary>
  [Fact]
  public async Task Project_code_outside_the_organization_scope_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    // 该编码只在其他组织建立了互认配置，当前组织读取不到。
    fixture.QueryRepository.AddConfiguration("ORG-B", ThirdProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode, ThirdProjectCode]);

    Assert.Contains(ThirdProjectCode, error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V5：项目编码对应配置已停用时整次失败，指出该标准项目编码。
  /// </summary>
  [Fact]
  public async Task Disabled_project_configuration_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, ThirdProjectCode, isValid: false);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode, ThirdProjectCode]);

    Assert.Contains(ThirdProjectCode, error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V6：项目编码标准目录层级不可用时整次失败，指出该标准项目编码；分类、分组与标准项目任一层停用都判为不可用。
  /// </summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  [Theory]
  [InlineData(false, true, true)]
  [InlineData(true, false, true)]
  [InlineData(true, true, false)]
  public async Task Unavailable_catalog_level_fails_the_whole_query(bool categoryIsValid, bool groupIsValid, bool itemIsValid)
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(
      TrustedOrganization, FirstProjectCode, categoryIsValid: categoryIsValid, groupIsValid: groupIsValid, itemIsValid: itemIsValid);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    Assert.Contains(FirstProjectCode, error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V7：同一次查询重复提供相同编码只处理一次，只形成一个匹配项。
  /// </summary>
  [Fact]
  public async Task Duplicate_project_code_is_processed_once()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, FirstProjectCode, FirstProjectCode]);

    Assert.True(response.HasMatches);
    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(FirstProjectCode, item.StandardProjectCode);
    RecognitionMatchesReturnedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
    Assert.Equal(1, recordedEvent.MatchedItemCount);
  }

  /// <summary>
  /// V9：同一项目下先取匹配基准时间最晚者。
  /// </summary>
  [Fact]
  public async Task Candidate_selection_prefers_the_latest_match_baseline_time()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    Guid earlierBaselineVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: ClinicalTime.AddHours(-2));
    Guid laterBaselineVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: ClinicalTime);

    await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(laterBaselineVersionId, item.ReportVersionId);
    Assert.NotEqual(earlierBaselineVersionId, item.ReportVersionId);
  }

  /// <summary>
  /// V10：匹配基准时间相同时取报告时间最晚者。
  /// </summary>
  [Fact]
  public async Task Candidate_selection_uses_report_time_when_baseline_times_are_equal()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    Guid earlierReportVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, reportTime: ReportTime.AddHours(-3));
    Guid laterReportVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, reportTime: ReportTime);

    await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(laterReportVersionId, item.ReportVersionId);
    Assert.NotEqual(earlierReportVersionId, item.ReportVersionId);
  }

  /// <summary>
  /// V11：匹配基准时间与报告时间均相同时以报告版本标识升序稳定兜底，重复查询返回同一份。
  /// </summary>
  /// <remarks>
  /// 期望值按同一条排序键口径在用例侧复算：匹配基准时间倒序、报告时间倒序、报告版本标识升序取第一条；
  /// 报告版本标识按 <see cref="Guid"/> 自身的顺序比较，不按其在文本形式下的字面顺序比较。
  /// </remarks>
  [Fact]
  public async Task Candidate_selection_breaks_ties_by_report_version_id_ascending()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    Guid firstVersionId = AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, reportTime: ReportTime, matchBaselineTime: ClinicalTime);
    Guid secondVersionId = AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, reportTime: ReportTime, matchBaselineTime: ClinicalTime);
    Assert.NotEqual(firstVersionId, secondVersionId);

    Guid expectedVersionId = fixture.QueryRepository.CandidateReports
      .Where(candidate => candidate.StandardProjectCode == FirstProjectCode)
      .OrderByDescending(candidate => candidate.MatchBaselineTime)
      .ThenByDescending(candidate => candidate.ReportTime)
      .ThenBy(candidate => candidate.ReportVersionId)
      .First().ReportVersionId;

    await ExecuteMatchAsync(fixture, [FirstProjectCode]);
    Assert.Equal(expectedVersionId, Assert.Single(fixture.Repository.MatchItems.Values).ReportVersionId);

    // 重复查询稳定返回同一份：两次命中都绑定同一个版本标识。
    await ExecuteMatchAsync(fixture, [FirstProjectCode]);
    Assert.All(fixture.Repository.MatchItems.Values, item => Assert.Equal(expectedVersionId, item.ReportVersionId));
  }

  /// <summary>
  /// V12：可互认时间边界按包含处理，经过时间等于该项目当前可互认时间时仍满足；超过时不返回。
  /// </summary>
  /// <remarks>
  /// 边界取证从应用服务入口进行：匹配生成时间取平台收到本次查询的时间，用例把可互认时间固定为十天，
  /// 报告时间放在第五天前（满足）与第十一天前（不满足），两种情况都不依赖用例执行耗时落在边界的那一瞬。
  /// </remarks>
  [Fact]
  public async Task Recognition_duration_boundary_is_inclusive()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 10);

    // 经过时间取五天，落在十天可互认时间内；报告时间晚于更新的一份，确认它替代更早的报告成为命中项。
    DateTime withinDuration = DateTime.Now.AddDays(-5);
    Guid withinDurationVersionId = AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: withinDuration, reportTime: withinDuration);
    AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: withinDuration.AddDays(-1), reportTime: withinDuration.AddDays(-1));

    await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(withinDurationVersionId, item.ReportVersionId);
  }

  /// <summary>
  /// V12 的负向面：经过时间超过可互认时间时不返回该项目，整体仍按成功空结果返回。
  /// </summary>
  [Fact]
  public async Task Report_beyond_recognition_duration_is_not_returned()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 10);

    // 经过时间取十一天，超过十天可互认时间。
    DateTime expiredReportTime = DateTime.Now.AddDays(-11);
    AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: expiredReportTime, reportTime: expiredReportTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V13：匹配基准时间晚于本次查询时点的报告不作为候选。
  /// </summary>
  [Fact]
  public async Task Report_with_future_baseline_time_is_not_a_candidate()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 30);

    DateTime futureTime = DateTime.Now.AddDays(1);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: futureTime, reportTime: futureTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V20b：排除时长大于该项目可互认时间时该项目不返回本院报告，查询整体成功，不作为配置错误。
  /// </summary>
  [Fact]
  public async Task Exclusion_hours_greater_than_recognition_duration_is_not_a_configuration_error()
  {
    MatchFixture fixture = CreateFixture(ownHospitalMatchEnabled: true, ownHospitalExcludeHours: 800);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 10);

    // 报告时间取 30 天前：落在该项目 10 天可互认时间之外，且远未达到 800 小时排除时长。
    DateTime olderReportTime = DateTime.Now.AddDays(-30);
    AddLaboratoryReportFacts(
      fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: olderReportTime, reportTime: olderReportTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V17：本院报告开关关闭时来源医院与接收医院相同的报告不参与匹配，不区分院区；他院报告照常参与。
  /// </summary>
  [Fact]
  public async Task Own_hospital_reports_do_not_match_when_the_switch_is_off()
  {
    MatchFixture fixture = CreateFixture(ownHospitalMatchEnabled: false);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);

    // 同一家医院、不同院区的报告同样按本院报告处理，因此也不参与匹配。
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, sourceBranchCode: "BRH-OTHER");
    // 他院报告照常参与匹配。
    Guid otherHospitalVersionId = AddLaboratoryReportFacts(fixture, SecondProjectCode, OtherHospital);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, SecondProjectCode]);

    Assert.True(response.HasMatches);
    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(SecondProjectCode, item.StandardProjectCode);
    Assert.Equal(otherHospitalVersionId, item.ReportVersionId);
    Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
  }

  /// <summary>
  /// V18：本院报告开关开启且经过时间未达排除时长时本院报告不参与匹配。
  /// </summary>
  [Fact]
  public async Task Own_hospital_report_before_the_exclusion_duration_does_not_match()
  {
    MatchFixture fixture = CreateFixture(ownHospitalMatchEnabled: true, ownHospitalExcludeHours: 24);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 30);

    // 经过时间取 23 小时，小于排除时长 24 小时。
    DateTime reportTime = DateTime.Now.AddHours(-23);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: reportTime, reportTime: reportTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.False(response.HasMatches);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V19：本院报告经过时间达到排除时长且落在可互认时间内时参与匹配，两个边界都包含。
  /// </summary>
  [Fact]
  public async Task Own_hospital_report_matches_at_the_exclusion_duration_boundary()
  {
    MatchFixture fixture = CreateFixture(ownHospitalMatchEnabled: true, ownHospitalExcludeHours: 6);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 10);

    // 经过时间取 6 小时，恰好等于排除时长，且远小于 10 天可互认时间。
    DateTime reportTime = DateTime.Now.AddHours(-6);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital, matchBaselineTime: reportTime, reportTime: reportTime);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.True(response.HasMatches);
    Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
  }

  /// <summary>
  /// V20：排除时长为零时不增加额外等待，本院报告按可互认时间判断。
  /// </summary>
  [Fact]
  public async Task Zero_exclusion_hours_accepts_own_hospital_reports_within_recognition_duration()
  {
    MatchFixture fixture = CreateFixture(ownHospitalMatchEnabled: true, ownHospitalExcludeHours: 0);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, recognitionDurationDays: 30);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.True(response.HasMatches);
    Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
  }

  /// <summary>
  /// V22：联合报告部分项目命中时只有命中的项目形成匹配项。
  /// </summary>
  [Fact]
  public async Task Only_matched_projects_form_match_items()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, ThirdProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, SecondProjectCode, ThirdProjectCode]);

    Assert.True(response.HasMatches);
    RecognitionMatchItem item = Assert.Single(fixture.Repository.MatchItems.Values);
    Assert.Equal(FirstProjectCode, item.StandardProjectCode);
    Assert.Single(response.Reports);
    Assert.Single(Assert.Single(response.Reports).MatchItems);
    RecognitionMatchesReturnedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
    Assert.Equal(1, recordedEvent.MatchedItemCount);
  }

  /// <summary>
  /// V22b：匹配查询不形成处理结果与引用事实，登记的事件仅为匹配已返回。
  /// </summary>
  /// <remarks>空匹配与非空匹配各查一次：两次都只登记匹配已返回事件，不产生处理结果或引用状态。</remarks>
  [Fact]
  public async Task Match_query_forms_no_processing_result_or_reference_fact()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      // 非空匹配一次。
      await fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest([FirstProjectCode]));
      // 空匹配一次：该项目编码有效但没有任何报告命中。
      await fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest([SecondProjectCode]));

      Assert.All(events.Events, domainEvent => Assert.IsType<RecognitionMatchesReturnedEvent>(domainEvent));
      Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
      Assert.Single(fixture.Repository.MatchRecords);
      Assert.Single(fixture.Repository.MatchItems);
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>
  /// V93：提醒事实即互认匹配项记录，其数量等于命中项目数；空匹配不形成提醒事实，重复编码不重复计数。
  /// </summary>
  [Fact]
  public async Task Reminder_facts_equal_match_items_of_the_current_query()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, SecondProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    // 空匹配：该项目编码有效但没有任何报告命中，不形成提醒事实。
    (RecordingEventQueue emptyEvents, RecognitionMatchesResponseReadModel emptyResponse) =
      await ExecuteMatchAsync(fixture, [SecondProjectCode]);
    Assert.False(emptyResponse.HasMatches);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Empty(emptyEvents.Events);

    // 非空匹配且含重复编码：提醒事实数等于去重后的命中项目数，且不重复计数。
    (RecordingEventQueue events, RecognitionMatchesResponseReadModel response) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode, SecondProjectCode, FirstProjectCode]);
    Assert.True(response.HasMatches);
    Assert.Single(fixture.Repository.MatchItems);
    RecognitionMatchesReturnedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
    Assert.Equal(fixture.Repository.MatchItems.Count, recordedEvent.MatchedItemCount);
  }

  /// <summary>
  /// V94：同一输入连续查询两次生成不同的记录标识与匹配项标识，两次互不影响，旧集合不被复用。
  /// </summary>
  [Fact]
  public async Task Repeated_query_creates_new_identifiers_without_reusing_the_previous_set()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, RecognitionMatchesResponseReadModel first) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);
    Guid firstRecordId = Assert.Single(fixture.Repository.MatchRecords.Keys);
    Guid firstItemId = Assert.Single(fixture.Repository.MatchItems.Keys);

    (RecordingEventQueue secondEvents, RecognitionMatchesResponseReadModel second) =
      await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    Assert.True(first.HasMatches);
    Assert.True(second.HasMatches);
    Assert.NotEqual(first.RecognitionMatchRecordId, second.RecognitionMatchRecordId);
    Assert.Equal(2, fixture.Repository.MatchRecords.Count);
    Assert.Equal(2, fixture.Repository.MatchItems.Count);
    Assert.NotEqual(firstItemId, fixture.Repository.MatchItems.Keys.Single(id => id != firstItemId));
    Assert.Contains(firstRecordId, fixture.Repository.MatchRecords.Keys);
    Assert.Single(events.Events.OfType<RecognitionMatchesReturnedEvent>());
    Assert.Single(secondEvents.Events.OfType<RecognitionMatchesReturnedEvent>());
  }

  /// <summary>
  /// V89：本院报告匹配开关取值缺失、取值非法或参数服务读取失败都整次失败、零写入、不登记事件。
  /// </summary>
  /// <param name="switchValue">预置的开关返回值；为 <see langword="null"/> 表示参数不存在。</param>
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("true")]
  [InlineData("2")]
  public async Task Missing_or_invalid_match_switch_fails_the_whole_query(string switchValue)
  {
    MatchFixture fixture = CreateFixture(switchValue: switchValue);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    Assert.StartsWith("业务拒绝：", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V89：本院报告匹配开关参数不存在时整次失败、零写入、不登记事件。
  /// </summary>
  [Fact]
  public async Task Missing_match_switch_parameter_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture(switchParameterMissing: true);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    Assert.Equal("业务拒绝：未配置本院报告匹配开关。", error.Message);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V89：本院报告排除时长取值缺失或取值非法（小数、负数、非整数）都整次失败、零写入、不登记事件。
  /// </summary>
  /// <param name="hoursValue">预置的排除时长返回值。</param>
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("1.5")]
  [InlineData("-1")]
  [InlineData("abc")]
  [InlineData("true")]
  public async Task Missing_or_invalid_exclusion_hours_fails_the_whole_query(string hoursValue)
  {
    MatchFixture fixture = CreateFixture(excludeHoursValue: hoursValue);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    Assert.StartsWith("业务拒绝：", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V89：本院报告排除时长参数不存在时整次失败、零写入、不登记事件。
  /// </summary>
  [Fact]
  public async Task Missing_exclusion_hours_parameter_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture(excludeHoursParameterMissing: true);
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    Assert.Equal("业务拒绝：未配置本院报告排除时长。", error.Message);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V89：参数服务读取失败时整次失败、零写入、不登记事件，不降级为兜底默认值。
  /// </summary>
  [Fact]
  public async Task System_parameter_service_failure_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture(parameterServiceFailure: new InvalidOperationException("参数服务不可用。"));
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => WithEventQueueAsync(() => fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest([FirstProjectCode]))));

    Assert.Equal("参数服务不可用。", error.Message);

    // 参数服务抛出时读取已经发生，但领域写入与事件登记都没有发生。
    Assert.Equal(1, fixture.SystemParameterAppService.ReadCount);
    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Equal(0, fixture.QueryRepository.CandidateQueryCalls);
  }

  /// <summary>
  /// V90 的读取维度面：两个参数都按平台级编码读取，读取请求不携带组织、医院与院区维度。
  /// </summary>
  [Fact]
  public async Task Parameter_reads_use_platform_level_codes_only()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);

    await ExecuteMatchAsync(fixture, [FirstProjectCode]);

    // 读取请求类型上没有组织、医院与院区属性：读取形态从类型上就不可能携带这三个维度。
    Type requestType = typeof(Dy.Base.Application.Contracts.SystemParameterAggregate.GetSystemParameterByCodeRequest);
    Assert.Null(requestType.GetProperty("OrgId"));
    Assert.Null(requestType.GetProperty("HosId"));
    Assert.Null(requestType.GetProperty("BranchId"));

    // 两个参数都按平台级编码读取，读取顺序与常量声明一致。
    Assert.Equal([OwnHospitalMatchSwitch, OwnHospitalExcludeHours], fixture.SystemParameterAppService.ReadCodes);
    // 读取请求形态上不携带组织、医院与院区维度。
    Assert.Null(fixture.SystemParameterAppService.LastRequest!.GetType().GetProperty("OrgId"));
  }
  /// <summary>
  /// V91：项目编码数组为空时整次失败，不创建记录、不登记事件。
  /// </summary>
  /// <remarks>
  /// 公开入口的公共请求校验已经把空数组拒绝在应用层之外，因此这里的取证面是公共请求校验先于领域读取与写入：
  /// 空数组既不读取参数，也不读取互认配置与候选报告，更不创建记录。
  /// </remarks>
  [Fact]
  public async Task Empty_proposed_item_array_fails_the_whole_query()
  {
    MatchFixture fixture = CreateFixture();

    await Assert.ThrowsAsync<ValidationException>(
      () => WithEventQueueAsync(() => fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest([]))));

    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Equal(0, fixture.SystemParameterAppService.ReadCount);
    Assert.Equal(0, fixture.QueryRepository.ConfigurationQueryCalls);
    Assert.Equal(0, fixture.QueryRepository.CandidateQueryCalls);
  }

  /// <summary>
  /// 可信上下文不可解析时本入口整次失败，零写入、零事件。
  /// </summary>
  /// <remarks>
  /// 可信上下文的组织、医院与院区都取不到时，匹配请求在进入领域写入之前终止：
  /// 拒绝文案由可信范围解析点的通用文案给出，匹配记录、匹配项与领域事件都不产生。
  /// 未认证请求被拒绝的对外响应属框架认证失败路径，由真实宿主取证，不在本用例内承接。
  /// </remarks>
  [Fact]
  public async Task Unresolvable_trusted_context_fails_the_whole_request_without_writes()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);
    AddLaboratoryReportFacts(fixture, FirstProjectCode, TrustedHospital);
    TrustedRequestContext.Use(null, null, null, string.Empty);

    (RecordingEventQueue events, InvalidOperationException error) =
      await ExecuteMatchExpectingRejectionAsync(fixture, [FirstProjectCode]);

    // 可信上下文为 null 时三层都视为缺失；拒绝文案由可信范围解析点的通用文案给出。
    Assert.Contains("无法确定当前", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// README 约定 13：写路径上的真实入口断言请求校验先于领域写入。
  /// </summary>
  /// <remarks>
  /// 请求校验失败时既不应写入任何业务数据，也不应读取参数、互认配置或候选报告。
  /// </remarks>
  [Fact]
  public async Task Request_validation_precedes_domain_reads_and_writes()
  {
    MatchFixture fixture = CreateFixture();
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode);

    await Assert.ThrowsAsync<ValidationException>(() => WithEventQueueAsync(() => fixture.AppService.RequestRecognitionMatchesAsync(
      new RecognitionMatchQueryRequest
      {
        IdentityDocumentTypeCode = string.Empty,
        IdentityDocumentNo = IdentityDocumentNo,
        VisitType = VisitType.Outpatient,
        VisitSerialNo = VisitSerialNo,
        ProposedItems = [new RecognitionMatchProposedItemRequest { ItemType = MedicalItemType.Laboratory, StandardProjectCode = FirstProjectCode }]
      })));

    Assert.Equal(0, fixture.SystemParameterAppService.ReadCount);
    Assert.Equal(0, fixture.QueryRepository.ConfigurationQueryCalls);
    Assert.Equal(0, fixture.QueryRepository.CandidateQueryCalls);
    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
  }

  /// <summary>匹配响应用例的固定临床时间；相对当前时点构造，使报告落在可互认时间内且确实早于本次查询时点。</summary>
  private static DateTime ClinicalTime => DateTime.Now.AddHours(-2);

  /// <summary>匹配响应用例的固定报告时间；晚于临床时间且早于本次查询时点。</summary>
  private static DateTime ReportTime => DateTime.Now.AddHours(-1);

  /// <summary>
  /// 一次匹配用例的全部测试替身与入口。
  /// </summary>
  private sealed class MatchFixture
  {
    /// <summary>写侧仓储替身，保存匹配记录与匹配项，并提供平台患者。</summary>
    public FakeReportRepository Repository { get; init; } = new();
    /// <summary>互认匹配所需只读投影替身。</summary>
    public FakeMatchQueryRepository QueryRepository { get; init; } = new();
    /// <summary>外部组织服务替身，提供组织、医院与院区名称。</summary>
    public StubOrganizationAppService OrganizationAppService { get; init; } = new();
    /// <summary>系统参数服务替身，按编码预置返回值并记录读取过的编码。</summary>
    public StubSystemParameterAppService SystemParameterAppService { get; init; } = new();
    /// <summary>互认匹配请求入口。</summary>
    public MedicalRecognitionReportAppService AppService { get; init; } = null!;
  }

  /// <summary>
  /// 构建一次匹配用例的替身集合与入口，并预置可信请求上下文。
  /// </summary>
  /// <param name="patientExists">是否预置本次证件对应的平台患者。</param>
  /// <param name="ownHospitalMatchEnabled">本院报告匹配开关取值；默认开启，使来源医院与接收医院相同的报告能参与匹配。</param>
  /// <param name="ownHospitalExcludeHours">本院报告排除时长取值。</param>
  /// <param name="switchValue">预置的开关返回值；为 <see langword="null"/> 表示按布尔开关换算。</param>  /// <param name="excludeHoursValue">预置的排除时长返回值；为 <see langword="null"/> 表示按整数时长换算。</param>
  /// <param name="parameterServiceFailure">非空时表示参数服务读取失败。</param>
  /// <param name="switchParameterMissing">为真时开关参数不预置，用于构造"参数不存在"前置。</param>
  /// <param name="excludeHoursParameterMissing">为真时排除时长参数不预置，用于构造"参数不存在"前置。</param>
  /// <returns>可直接调用的互认匹配请求用例夹具。</returns>
  private static MatchFixture CreateFixture(
    bool patientExists = true,
    bool ownHospitalMatchEnabled = true,
    int ownHospitalExcludeHours = 0,
    string? switchValue = null,
    string? excludeHoursValue = null,
    Exception? parameterServiceFailure = null,
    bool switchParameterMissing = false,
    bool excludeHoursParameterMissing = false)
  {
    FakeReportRepository repository = new();
    FakeMatchQueryRepository queryRepository = new()
    {
      // 候选报告按平台患者归属筛选，用例登记候选时默认取本次已解析到的患者。
      CandidatePatientId = PatientId
    };

    if (patientExists)
    {
      repository.Patients[PatientId] = new PlatformPatient
      {
        Id = PatientId,
        IdentityDocumentTypeCode = IdentityDocumentTypeCode,
        IdentityDocumentNo = IdentityDocumentNo,
        PatientName = "张三",
        PatientGenderCode = "1",
        PatientBirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
        OperId = TrustedOperId,
        OperTime = DateTimeOffset.UtcNow
      };
    }

    // 参数服务替身按编码预置返回值：显式传入的取值原样预置（含空串与纯空白，用于构造非法取值），
    // 未显式传入时用布尔开关与整数时长换算成平台配置里的字符串形态；不预置的编码按参数不存在返回空值。
    StubSystemParameterAppService systemParameterAppService = new() { Failure = parameterServiceFailure };
    if (switchValue is not null) systemParameterAppService.ValuesByCode[OwnHospitalMatchSwitch] = switchValue;
    else if (!switchParameterMissing) systemParameterAppService.ValuesByCode[OwnHospitalMatchSwitch] = ownHospitalMatchEnabled ? "1" : "0";
    if (excludeHoursValue is not null) systemParameterAppService.ValuesByCode[OwnHospitalExcludeHours] = excludeHoursValue;
    else if (!excludeHoursParameterMissing) systemParameterAppService.ValuesByCode[OwnHospitalExcludeHours] = ownHospitalExcludeHours.ToString();

    StubOrganizationAppService organizationAppService = new()
    {
      Organizations = [new Dy.Base.Application.Contracts.OrganizationAggregate.OrganizationDto { Id = TrustedOrganization, Name = "示范组织", IsValid = true }],
      HospitalsByOrganization =
      {
        [TrustedOrganization] =
        [
          new Dy.Base.Application.Contracts.OrganizationAggregate.HospitalDto { Id = TrustedHospital, Name = "示范医院", OrgId = TrustedOrganization, IsValid = true },
          new Dy.Base.Application.Contracts.OrganizationAggregate.HospitalDto { Id = OtherHospital, Name = "他院", OrgId = TrustedOrganization, IsValid = true }
        ]
      },
      BranchesByHospital =
      {
        [TrustedHospital] = [new Dy.Base.Application.Contracts.OrganizationAggregate.BranchDto { Id = TrustedBranch, Name = "示范院区", HosId = TrustedHospital, OrgId = TrustedOrganization, IsValid = true }],
        [OtherHospital] = [new Dy.Base.Application.Contracts.OrganizationAggregate.BranchDto { Id = TrustedBranch, Name = "他院院区", HosId = OtherHospital, OrgId = TrustedOrganization, IsValid = true }]
      }
    };

    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());

    return new MatchFixture
    {
      Repository = repository,
      QueryRepository = queryRepository,
      OrganizationAppService = organizationAppService,
      SystemParameterAppService = systemParameterAppService,
      AppService = new MedicalRecognitionReportAppService(
        new MedicalRecognitionReportManager(repository, queryRepository),
        repository,
        organizationAppService,
        new StubUserAppService(),
        new StubReportPdfFileStore(),
        queryRepository,
        systemParameterAppService)
    };
  }

  /// <summary>
  /// 为一个标准项目登记一份检验报告的候选行、报告事实与项目级内容。
  /// </summary>
  /// <remarks>
  /// 报告标识与报告版本标识每次生成，保证不同报告不共用版本标识；返回的版本标识供用例登记候选行与断言绑定结果。
  /// </remarks>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="sourceHospitalCode">来源医院编码。</param>
  /// <param name="recognitionDurationDays">可互认时间天数，用于把报告时间放在该项目的可互认范围内。</param>
  /// <param name="matchBaselineTime">匹配基准时间；为空时取固定临床时间。</param>
  /// <param name="reportTime">报告时间；为空时取固定报告时间。</param>
  /// <param name="sourceBranchCode">来源院区编码；为空时取可信院区。</param>
  /// <param name="sourceProjectName">该报告主明细的来源项目名称；为空时取该明细标准项目编码对应的统一形态。</param>
  /// <param name="additionalResultProjectCodes">同一份报告下另外登记检验结果明细的标准项目编码；只登记明细，不改变报告绑定。</param>
  /// <returns>本次登记的报告版本标识。</returns>
  private static Guid AddLaboratoryReportFacts(
    MatchFixture fixture,
    string standardProjectCode,
    string sourceHospitalCode,
    int recognitionDurationDays = 30,
    DateTime? matchBaselineTime = null,
    DateTime? reportTime = null,
    string? sourceBranchCode = null,
    string? sourceProjectName = null,
    params string[] additionalResultProjectCodes)
  {
    Guid reportId = Guid.NewGuid();
    Guid reportVersionId = Guid.NewGuid();
    DateTime effectiveReportTime = reportTime ?? ReportTime;
    fixture.QueryRepository.AddLaboratoryCandidate(
      standardProjectCode, reportId, reportVersionId, sourceHospitalCode, matchBaselineTime ?? ClinicalTime, effectiveReportTime);
    fixture.QueryRepository.ReportFactsByVersion[reportVersionId] = new RecognitionMatchReportFactsItem
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId,
      ReportType = MedicalReportType.Laboratory,
      ReportNo = "LAB-2026-0001",
      ReportName = "血常规报告",
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = sourceHospitalCode,
      SourceBranchCode = sourceBranchCode ?? TrustedBranch,
      ClinicalTime = matchBaselineTime ?? ClinicalTime,
      ReportTime = effectiveReportTime,
      PdfFileId = "pdf-key-1",
      PdfFileName = "血常规报告.pdf",
      SpecimenTypeName = "全血",
      OverallAbnormalFlag = "1",
      SourceImageStatus = null,
      ImageAccessUrl = null
    };

    List<LaboratoryResultItemView> results =
    [
      new LaboratoryResultItemView
      {
        // 普通检验结果必须带上报告版本归属：应用层按本列把一次读回的结果归到对应的报告下，归属缺失时该报告取不到任何明细。
        ReportVersionId = reportVersionId,
        SourceProjectName = sourceProjectName ?? SourceProjectNameFor(standardProjectCode),
        StandardProjectCode = standardProjectCode,
        SourceResultText = "9.8",
        Unit = "10^9/L",
        ReferenceRange = "4.0-10.0",
        DisplayOrder = 1,
        AbnormalFlag = LaboratoryAbnormalFlag.High,
        CriticalValueFlag = false
      }
    ];
    foreach (string additionalProjectCode in additionalResultProjectCodes)
    {
      results.Add(new LaboratoryResultItemView
      {
        // 同一份报告下另外登记的结果明细与首条结果共用同一个报告版本归属。
        ReportVersionId = reportVersionId,
        SourceProjectName = SourceProjectNameFor(additionalProjectCode),
        StandardProjectCode = additionalProjectCode,
        SourceResultText = "正常",
        DisplayOrder = results.Count + 1
      });
    }

    fixture.QueryRepository.LaboratoryResultsByVersion[reportVersionId] = results;
    return reportVersionId;
  }

  /// <summary>构建一次互认匹配请求。</summary>
  /// <param name="projectCodes">本次拟开的标准项目编码，全部按检验项目提交。</param>
  /// <returns>字段齐全的互认匹配请求。</returns>
  private static RecognitionMatchQueryRequest BuildRequest(IReadOnlyList<string> projectCodes) => new()
  {
    IdentityDocumentTypeCode = IdentityDocumentTypeCode,
    IdentityDocumentNo = IdentityDocumentNo,
    VisitType = VisitType.Outpatient,
    VisitSerialNo = VisitSerialNo,
    ProposedItems = [.. projectCodes.Select(code => new RecognitionMatchProposedItemRequest
    {
      ItemType = MedicalItemType.Laboratory,
      StandardProjectCode = code
    })]
  };

  /// <summary>在捕获事件的前提下执行一次互认匹配请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="projectCodes">本次拟开的标准项目编码。</param>
  /// <returns>捕获到的事件集合与匹配响应。</returns>
  private static async Task<(RecordingEventQueue Events, RecognitionMatchesResponseReadModel Response)> ExecuteMatchAsync(
    MatchFixture fixture, IReadOnlyList<string> projectCodes)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest(projectCodes)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被拒绝的互认匹配请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="projectCodes">本次拟开的标准项目编码。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static async Task<(RecordingEventQueue Events, InvalidOperationException Error)> ExecuteMatchExpectingRejectionAsync(
    MatchFixture fixture, IReadOnlyList<string> projectCodes)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<InvalidOperationException>(
        () => fixture.AppService.RequestRecognitionMatchesAsync(BuildRequest(projectCodes))));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在安装记录用事件队列的前提下执行一次互认匹配请求调用。</summary>
  /// <param name="operation">待执行的互认匹配请求调用。</param>
  /// <returns>互认匹配请求的返回值。</returns>
  private static async Task WithEventQueueAsync(Func<Task> operation)
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

  /// <summary>断言一次失败的匹配没有留下任何业务写入与事件。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="events">捕获到的事件集合。</param>
  private static void AssertNothingWritten(MatchFixture fixture, RecordingEventQueue events)
  {
    Assert.Empty(fixture.Repository.MatchRecords);
    Assert.Empty(fixture.Repository.MatchItems);
    Assert.Empty(events.Events);
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

  /// <summary>处理结果用例的互认匹配记录标识。</summary>
  private static readonly Guid ProcessingRecordId = Guid.Parse("11f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f70");

  /// <summary>处理结果用例的第一条互认匹配项标识；组内标识按升序构造，使匹配项顺序与提交顺序一致。</summary>
  private static readonly Guid FirstMatchItemId = Guid.Parse("21f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f71");

  /// <summary>处理结果用例的第二条互认匹配项标识。</summary>
  private static readonly Guid SecondMatchItemId = Guid.Parse("31f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f72");

  /// <summary>其他互认匹配记录的匹配项标识，用于构造夹带其他组项目的请求。</summary>
  private static readonly Guid ForeignMatchItemId = Guid.Parse("41f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f73");

  /// <summary>处理结果用例的互认科室标识。</summary>
  private const string RecognitionDeptId = "DEPT-01";

  /// <summary>处理结果用例的互认科室名称。</summary>
  private const string RecognitionDeptName = "心内科";

  /// <summary>处理结果用例的互认医生标识。</summary>
  private const string RecognitionDoctorId = "DOC-01";

  /// <summary>处理结果用例的互认医生名称。</summary>
  private const string RecognitionDoctorName = "李医生";

  /// <summary>互认匹配记录的生成时间；互认时间取该值之后且不晚于请求接收时间。</summary>
  /// <remarks>取值在用例夹具类型初始化时冻结一次，使同一条用例内多次读取得到同一时点，不随时钟推进漂移。</remarks>
  private static readonly DateTime ProcessingMatchCreatedTime = DateTime.Now.AddHours(-2);

  /// <summary>本次提交的互认时间；晚于匹配生成时间且早于请求接收时间。</summary>
  /// <remarks>同 <see cref="ProcessingMatchCreatedTime"/>，冻结一次以避免同一用例内的读取得到不同取值。</remarks>
  private static readonly DateTime ProcessingRecognitionTime = DateTime.Now.AddHours(-1);

  /// <summary>
  /// V23：整组采纳提交保存全部匹配项的采纳决定、形成预计节省金额、写入处理结果保存时间并登记一次事件。
  /// </summary>
  /// <remarks>
  /// 预计节省金额只出现在互认处理结果上：来源组织与来源医院侧不存在承载该金额的记录或列，
  /// 因此本用例断言采纳项目的金额只写入处理结果行，且合计等于两项金额之和。
  /// </remarks>
  [Fact]
  public async Task Whole_group_adoption_saves_decisions_amount_and_saved_time()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 12.34m);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, SecondProjectCode, 5.66m);

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(
      fixture,
      BuildProcessingRequest(
        [Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)]));

    Assert.True(success);

    // 全部匹配项保存采纳决定：不采纳原因、补充说明与采纳项金额以外的字段都按组级事实写入。
    Assert.Equal(2, fixture.Repository.ProcessingResults.Count);
    RecognitionProcessingResult firstResult = fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == FirstMatchItemId);
    RecognitionProcessingResult secondResult = fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == SecondMatchItemId);
    Assert.All(fixture.Repository.ProcessingResults.Values, result =>
    {
      Assert.Equal(ProcessingRecordId, result.RecognitionMatchRecordId);
      Assert.Equal(RecognitionResult.Adopted, result.RecognitionResult);
      Assert.Equal(ProcessingRecognitionTime, result.RecognitionTime);
      Assert.Equal(RecognitionDeptId, result.RecognitionDeptId);
      Assert.Equal(RecognitionDeptName, result.RecognitionDeptName);
      Assert.Equal(RecognitionDoctorId, result.RecognitionDoctorId);
      Assert.Equal(RecognitionDoctorName, result.RecognitionDoctorName);
      Assert.Null(result.NonAdoptionReason);
      Assert.Null(result.NonAdoptionDescription);
      Assert.Equal(TrustedOperId, result.OperId);
    });

    // 采纳项目按接收组织、医院、院区与标准项目编码读取当前金额并形成预计节省金额。
    Assert.Equal(12.34m, firstResult.EstimatedSavingAmount);
    Assert.Equal(5.66m, secondResult.EstimatedSavingAmount);
    Assert.Equal(2, fixture.Repository.AmountReadCalls);

    // 首次成功保存写入处理结果保存时间。
    Assert.Equal(1, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.NotNull(fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);

    // 只登记一次处理结果已保存事件，事件承载匹配项标识、逐项决定与金额合计。
    RecognitionProcessingResultsSavedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    Assert.Equal(ProcessingRecordId, recordedEvent.RecognitionMatchRecordId);
    Assert.Equal([FirstMatchItemId, SecondMatchItemId], recordedEvent.RecognitionMatchItemIds);
    Assert.Equal([RecognitionResult.Adopted, RecognitionResult.Adopted], recordedEvent.Results);
    Assert.Equal(18.00m, recordedEvent.EstimatedSavingAmount);
    Assert.Equal(ProcessingRecognitionTime, recordedEvent.RecognitionTime);
    Assert.Equal(RecognitionDeptId, recordedEvent.RecognitionDeptId);
    Assert.Equal(RecognitionDoctorId, recordedEvent.RecognitionDoctorId);
  }

  /// <summary>
  /// V24：整组不采纳提交保存不采纳决定与原因，不读取金额、不形成预计节省金额。
  /// </summary>
  [Fact]
  public async Task Whole_group_non_adoption_saves_reasons_without_reading_amounts()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 12.34m);

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(
      fixture,
      BuildProcessingRequest(
        [
          NotAdopted(FirstMatchItemId, RecognitionNonAdoptionReason.EmergencyCare),
          NotAdopted(SecondMatchItemId, RecognitionNonAdoptionReason.RapidDiseaseProgression, "变化较快")
        ]));

    Assert.True(success);
    Assert.Equal(2, fixture.Repository.ProcessingResults.Count);
    Assert.All(fixture.Repository.ProcessingResults.Values, result =>
    {
      Assert.Equal(RecognitionResult.NotAdopted, result.RecognitionResult);
      // 不采纳不形成预计节省金额：该列写入空值而不是零元。
      Assert.Null(result.EstimatedSavingAmount);
    });
    Assert.Equal(
      RecognitionNonAdoptionReason.EmergencyCare,
      fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == FirstMatchItemId).NonAdoptionReason);
    Assert.Equal(
      "变化较快",
      fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == SecondMatchItemId).NonAdoptionDescription);

    // 不采纳项目不读取金额：一条金额都不会被读取，即使该业务键已配置金额。
    Assert.Equal(0, fixture.Repository.AmountReadCalls);
    RecognitionProcessingResultsSavedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    Assert.Equal(decimal.Zero, recordedEvent.EstimatedSavingAmount);
  }

  /// <summary>
  /// V25：组内项目分别作出不同决定时按各自决定保存。
  /// </summary>
  [Fact]
  public async Task Items_in_the_same_group_keep_their_own_decisions()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 8m);

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(
      fixture,
      BuildProcessingRequest(
        [Adopted(FirstMatchItemId), NotAdopted(SecondMatchItemId, RecognitionNonAdoptionReason.BeforeMajorMedicalMeasure)]));

    Assert.True(success);
    Assert.Equal(RecognitionResult.Adopted, fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == FirstMatchItemId).RecognitionResult);
    Assert.Equal(RecognitionResult.NotAdopted, fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == SecondMatchItemId).RecognitionResult);
    // 只读取采纳项目的金额。
    Assert.Equal(1, fixture.Repository.AmountReadCalls);
    RecognitionProcessingResultsSavedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    Assert.Equal([RecognitionResult.Adopted, RecognitionResult.NotAdopted], recordedEvent.Results);
    Assert.Equal(8m, recordedEvent.EstimatedSavingAmount);
  }

  /// <summary>
  /// V26：项目数组缺少组内匹配项时整次失败，指出相关互认匹配项标识，不保存任何项目。
  /// </summary>
  [Fact]
  public async Task Missing_item_in_the_array_fails_and_names_the_item()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.Contains(SecondMatchItemId.ToString(), error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V27：项目数组重复提交同一匹配项时整次失败，指出相关互认匹配项标识。
  /// </summary>
  [Fact]
  public async Task Duplicate_item_in_the_array_fails_and_names_the_item()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)]));

    Assert.Contains(FirstMatchItemId.ToString(), error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V28：项目数组夹带其他匹配记录的匹配项时整次失败，指出相关互认匹配项标识。
  /// </summary>
  [Fact]
  public async Task Foreign_item_from_another_record_fails_and_names_the_item()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.MatchItems[ForeignMatchItemId] = BuildMatchItem(ForeignMatchItemId, Guid.NewGuid(), FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId), Adopted(ForeignMatchItemId)]));

    Assert.Contains(ForeignMatchItemId.ToString(), error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V29：互认时间早于匹配记录生成时间时整次失败。
  /// </summary>
  [Fact]
  public async Task Recognition_time_before_match_creation_fails()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture,
      BuildProcessingRequest(Adopted(FirstMatchItemId), recognitionTime: ProcessingMatchCreatedTime.AddSeconds(-1)));

    Assert.Contains("互认时间", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V30：互认时间晚于请求接收时间时整次失败。
  /// </summary>
  [Fact]
  public async Task Recognition_time_after_request_receipt_fails()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture,
      BuildProcessingRequest(Adopted(FirstMatchItemId), recognitionTime: DateTime.Now.AddDays(1)));

    Assert.Contains("互认时间", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V31：互认时间等于两端边界时接受，两端边界都包含。
  /// </summary>
  /// <remarks>
  /// 下界取匹配记录生成时间本身；上界由用例在入口调用前取当前时点并把互认时间放在该时点之前一秒，
  /// 使"等于请求接收时间"这一端的判定落在服务端取到的接收时间上而不依赖用例执行耗时。
  /// </remarks>
  [Fact]
  public async Task Recognition_time_equal_to_both_boundaries_is_accepted()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    // 下界边界：互认时间恰好等于匹配记录生成时间。
    (RecordingEventQueue lowerEvents, bool lowerSuccess) = await ExecuteProcessingResultAsync(
      fixture, BuildProcessingRequest(Adopted(FirstMatchItemId), recognitionTime: ProcessingMatchCreatedTime));
    Assert.True(lowerSuccess);
    Assert.Single(lowerEvents.Events.OfType<RecognitionProcessingResultsSavedEvent>());

    // 上界边界：互认时间取调用前一刻的当前时点，服务端取到的接收时间不早于该取值。
    DateTime beforeCall = DateTime.Now;
    (RecordingEventQueue upperEvents, bool upperSuccess) = await ExecuteProcessingResultAsync(
      CreateProcessingFixtureWithSavedGroup(FirstProjectCode), BuildProcessingRequest(Adopted(FirstMatchItemId), recognitionTime: beforeCall));
    Assert.True(upperSuccess);
    Assert.Single(upperEvents.Events.OfType<RecognitionProcessingResultsSavedEvent>());
  }

  /// <summary>
  /// V32：不采纳未提供原因时整次失败。
  /// </summary>
  [Fact]
  public async Task Non_adoption_without_reason_fails()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(NotAdopted(FirstMatchItemId, nonAdoptionReason: null)));

    Assert.Contains("不采纳", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V33：不采纳原因取值不在值域内时被请求校验拒绝，拒绝发生在任何领域读取与写入之前。
  /// </summary>
  /// <remarks>
  /// 本用例取证的是请求校验面：原因取值由请求类型上的枚举校验拒绝，请求校验先于领域读取，因此整批零写入。
  /// 领域层的同口径判定是防御性分支，经公开入口不可达，本用例不为其提供行为证据。
  /// </remarks>
  [Fact]
  public async Task Non_adoption_with_undefined_reason_is_rejected_by_request_validation()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, ValidationException error) = await ExecuteProcessingResultExpectingValidationRejectionAsync(
      fixture, BuildProcessingRequest(NotAdopted(FirstMatchItemId, (RecognitionNonAdoptionReason)99)));

    Assert.Contains("不采纳原因", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V34：选择其他情形确需复查但缺少补充说明时整次失败。
  /// </summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task Other_reason_without_description_fails(string? description)
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(NotAdopted(FirstMatchItemId, RecognitionNonAdoptionReason.OtherReviewRequired, description)));

    Assert.Contains("补充说明", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V35：采纳时带有不采纳原因时整次失败。
  /// </summary>
  [Fact]
  public async Task Adoption_with_non_adoption_reason_fails()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture,
      BuildProcessingRequest(new RecognitionProcessingResultItemRequest
      {
        RecognitionMatchItemId = FirstMatchItemId,
        Result = RecognitionResult.Adopted,
        NonAdoptionReason = RecognitionNonAdoptionReason.EmergencyCare
      }));

    Assert.Contains("采纳", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V35b：处理结果值域非法时被请求校验拒绝，不保存任何项目，拒绝发生在任何领域读取与写入之前。
  /// </summary>
  /// <remarks>
  /// 本用例取证的是请求校验面：结果取值由请求类型上的枚举校验拒绝，请求校验先于领域读取，因此整批零写入。
  /// 领域层的同口径判定是防御性分支，经公开入口不可达，本用例不为其提供行为证据。
  /// </remarks>
  [Fact]
  public async Task Undefined_processing_result_is_rejected_by_request_validation()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, ValidationException error) = await ExecuteProcessingResultExpectingValidationRejectionAsync(
      fixture,
      BuildProcessingRequest(new RecognitionProcessingResultItemRequest
      {
        RecognitionMatchItemId = FirstMatchItemId,
        Result = (RecognitionResult)99
      }));

    Assert.Contains("互认结果", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V36：互认科室或互认医生的标识与名称不成对时被请求校验拒绝，不保存任何项目。
  /// </summary>
  /// <remarks>
  /// 本用例取证的是请求校验面：标识与名称的成对规则由请求类型上的必填与非空白校验拒绝，请求校验先于领域读取，因此整批零写入。
  /// 领域层的同口径判定是防御性分支，经公开入口不可达，本用例不为其提供行为证据。
  /// </remarks>
  /// <param name="deptName">互认科室名称。</param>
  /// <param name="doctorName">互认医生名称。</param>
  [Theory]
  [InlineData("", RecognitionDoctorName)]
  [InlineData("   ", RecognitionDoctorName)]
  [InlineData(RecognitionDeptName, "")]
  [InlineData(RecognitionDeptName, "   ")]
  public async Task Unpaired_department_or_doctor_is_rejected_by_request_validation(string deptName, string doctorName)
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, ValidationException error) = await ExecuteProcessingResultExpectingValidationRejectionAsync(
      fixture,
      BuildProcessingRequest(
        [Adopted(FirstMatchItemId)],
        recognitionDeptName: deptName,
        recognitionDoctorName: doctorName));

    Assert.Contains("参数校验失败", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V36b：提交的组织、医院、院区与匹配记录接收三值不一致时整次失败，不保存任何项目。
  /// </summary>
  /// <param name="recordOrganization">匹配记录的接收组织编码。</param>
  /// <param name="recordHospital">匹配记录的接收医院编码。</param>
  /// <param name="recordBranch">匹配记录的接收院区编码。</param>
  [Theory]
  [InlineData("ORG-B", TrustedHospital, TrustedBranch)]
  [InlineData(TrustedOrganization, OtherHospital, TrustedBranch)]
  [InlineData(TrustedOrganization, TrustedHospital, "BRH-B")]
  public async Task Mismatched_receiver_scope_fails(string recordOrganization, string recordHospital, string recordBranch)
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, recordOrganization, recordHospital, recordBranch);
    int auditCallsBefore = fixture.OrganizationAppService.OrganizationReadCount;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.Contains("不属于当前", error.Message, StringComparison.Ordinal);
    AssertProcessingNothingWritten(fixture, events);
    // 归属校验只比较匹配记录的接收三值与可信上下文，不向组织服务核对存在、启用与父子归属。
    Assert.Equal(auditCallsBefore, fixture.OrganizationAppService.OrganizationReadCount);
  }

  /// <summary>
  /// V37：幂等重试命中完全相同既有事实时返回成功，不新增记录、不重复统计、不读金额，处理结果保存时间不变。
  /// </summary>
  /// <remarks>金额在两次提交之间发生变化，命中幂等时不重新取价，因此既有预计节省金额保持不变。</remarks>
  [Fact]
  public async Task Idempotent_retry_succeeds_without_new_writes_or_amount_reads()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 10m);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, SecondProjectCode, 20m);

    RecognitionProcessingResultSubmissionRequest request = BuildProcessingRequest(
      [Adopted(FirstMatchItemId), NotAdopted(SecondMatchItemId, RecognitionNonAdoptionReason.EmergencyCare)]);
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(request));
    DateTime? savedTimeAfterFirstSave = fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime;
    int amountReadsAfterFirstSave = fixture.Repository.AmountReadCalls;

    // 金额已变化：命中幂等时不得重新取价、不得重算金额。
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 999m);

    (RecordingEventQueue retryEvents, bool retrySuccess) = await ExecuteProcessingResultAsync(fixture, request);

    Assert.True(retrySuccess);
    Assert.Equal(2, fixture.Repository.ProcessingResults.Count);
    Assert.Equal(10m, fixture.Repository.ProcessingResults.Values.Single(result => result.RecognitionMatchItemId == FirstMatchItemId).EstimatedSavingAmount);
    // 不读金额、不重复统计、处理结果保存时间不变。
    Assert.Equal(amountReadsAfterFirstSave, fixture.Repository.AmountReadCalls);
    Assert.Equal(1, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.Equal(savedTimeAfterFirstSave, fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);
    Assert.Empty(retryEvents.Events);
  }

  /// <summary>
  /// V38：幂等命中时绑定报告版本已失效仍按幂等成功返回，不改为失败。
  /// </summary>
  [Fact]
  public async Task Idempotent_retry_ignores_invalidated_report_version()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    Guid reportVersionId = SeedMatchGroup(fixture, FirstProjectCode);
    RecognitionProcessingResultSubmissionRequest request = BuildProcessingRequest(Adopted(FirstMatchItemId));
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(request));
    int validVersionReadsAfterFirstSave = fixture.QueryRepository.ValidReportVersionQueryCalls;

    // 报告已形成后续版本：该绑定版本不再有效。
    fixture.QueryRepository.ValidReportVersionIds.Remove(reportVersionId);

    (RecordingEventQueue retryEvents, bool retrySuccess) = await ExecuteProcessingResultAsync(fixture, request);

    Assert.True(retrySuccess);
    Assert.Single(fixture.Repository.ProcessingResults);
    // 幂等命中不校验报告版本有效性：不会发起有效性读取。
    Assert.Equal(validVersionReadsAfterFirstSave, fixture.QueryRepository.ValidReportVersionQueryCalls);
    Assert.Empty(retryEvents.Events);
  }

  /// <summary>
  /// V39：相同业务键但互认时间不同时返回冲突，不覆盖原决定、不登记事件。
  /// </summary>
  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public async Task Conflict_when_recognition_time_decision_or_scope_differs(int difference)
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    // 先保存一次整组决定。
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(
      BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)])));
    DateTime? savedTimeBeforeConflict = fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime;

    // 四项差异分别构造：互认时间、组级科室标识、组级医生标识与某一项目的决定。
    RecognitionProcessingResultSubmissionRequest conflictingRequest = difference switch
    {
      0 => BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)], recognitionTime: ProcessingRecognitionTime.AddSeconds(-1)),
      1 => BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)], recognitionDeptId: "DEPT-99"),
      2 => BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)], recognitionDoctorId: "DOC-99"),
      _ => BuildProcessingRequest([Adopted(FirstMatchItemId), NotAdopted(SecondMatchItemId, RecognitionNonAdoptionReason.EmergencyCare)])
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(fixture, conflictingRequest);

    Assert.Contains("冲突", error.Message, StringComparison.Ordinal);
    // 不覆盖原决定：记录数不变、金额与保存时间都不变、不登记事件。
    Assert.Equal(2, fixture.Repository.ProcessingResults.Count);
    Assert.All(fixture.Repository.ProcessingResults.Values, result => Assert.Equal(RecognitionResult.Adopted, result.RecognitionResult));
    Assert.Equal(savedTimeBeforeConflict, fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V40 的不采纳原因与补充说明差异面：任一项目的不采纳原因或补充说明不同也返回冲突。
  /// </summary>
  [Fact]
  public async Task Conflict_when_non_adoption_reason_or_description_differs()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(
      BuildProcessingRequest(NotAdopted(FirstMatchItemId, RecognitionNonAdoptionReason.EmergencyCare, "急救"))));

    (RecordingEventQueue reasonEvents, InvalidOperationException reasonError) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(NotAdopted(FirstMatchItemId, RecognitionNonAdoptionReason.RapidDiseaseProgression, "急救")));
    Assert.Contains("冲突", reasonError.Message, StringComparison.Ordinal);
    Assert.Empty(reasonEvents.Events);

    (RecordingEventQueue descriptionEvents, InvalidOperationException descriptionError) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(NotAdopted(FirstMatchItemId, RecognitionNonAdoptionReason.EmergencyCare, "另一份说明")));
    Assert.Contains("冲突", descriptionError.Message, StringComparison.Ordinal);
    Assert.Empty(descriptionEvents.Events);

    Assert.Single(fixture.Repository.ProcessingResults);
  }

  /// <summary>
  /// V42：相同业务键仅科室或医生名称不同时按幂等成功处理，名称不参与一致性判断。
  /// </summary>
  [Fact]
  public async Task Idempotent_success_when_only_names_differ()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(
      BuildProcessingRequest(Adopted(FirstMatchItemId))));

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(
      fixture,
      BuildProcessingRequest([Adopted(FirstMatchItemId)], recognitionDeptName: "改名后的科室", recognitionDoctorName: "改名后的医生"));

    Assert.True(success);
    Assert.Single(fixture.Repository.ProcessingResults);
    // 名称只保存不参与比较：既有行保留首次保存的名称，不因本次改名而被改写。
    Assert.Equal(RecognitionDeptName, fixture.Repository.ProcessingResults.Values.Single().RecognitionDeptName);
    Assert.Equal(RecognitionDoctorName, fixture.Repository.ProcessingResults.Values.Single().RecognitionDoctorName);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V43：项目数组顺序不同但内容一致时按幂等成功处理，顺序不参与一致性判断。
  /// </summary>
  [Fact]
  public async Task Idempotent_success_when_array_order_differs()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(
      BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)])));

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(
      fixture,
      BuildProcessingRequest([Adopted(SecondMatchItemId), Adopted(FirstMatchItemId)]));

    Assert.True(success);
    Assert.Equal(2, fixture.Repository.ProcessingResults.Count);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V44：未命中幂等时绑定报告版本已失效时整次失败，不保存任何项目。
  /// </summary>
  [Fact]
  public async Task Invalidated_report_version_fails_the_whole_submission()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    Guid reportVersionId = SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 10m);
    // 报告已形成后续版本：该绑定版本不再有效。
    fixture.QueryRepository.ValidReportVersionIds.Remove(reportVersionId);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)]));

    Assert.Contains(reportVersionId.ToString(), error.Message, StringComparison.Ordinal);
    // 版本校验先于金额读取与任何写入。
    Assert.Equal(0, fixture.Repository.AmountReadCalls);
    Assert.Equal(1, fixture.QueryRepository.ValidReportVersionQueryCalls);
    AssertProcessingNothingWritten(fixture, events);
  }

  /// <summary>
  /// V45：采纳项目未配置当前金额时按零元形成预计节省金额，不阻碍保存。
  /// </summary>
  [Fact]
  public async Task Missing_current_amount_forms_zero_and_does_not_block_saving()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);
    // 该业务键未配置金额：金额读取返回空值。

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.True(success);
    RecognitionProcessingResult result = Assert.Single(fixture.Repository.ProcessingResults.Values);
    Assert.Equal(RecognitionResult.Adopted, result.RecognitionResult);
    Assert.Equal(decimal.Zero, result.EstimatedSavingAmount);
    Assert.Equal(1, fixture.Repository.AmountReadCalls);
    RecognitionProcessingResultsSavedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    Assert.Equal(decimal.Zero, recordedEvent.EstimatedSavingAmount);
  }

  /// <summary>
  /// V45b：互认配置或标准目录当前停用但绑定报告版本仍有效时处理结果保存成功。
  /// </summary>
  /// <remarks>
  /// 处理结果链不读取互认配置的启用状态，也不读取标准目录层级；匹配项绑定的报告版本仍为当前有效版本即可保存。
  /// 本用例把该项目的互认配置置为停用，并断言保存仍然成功且金额照常按业务键读取。
  /// </remarks>
  [Fact]
  public async Task Disabled_configuration_does_not_block_saving_when_report_version_is_valid()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);
    // 该项目在当前组织的互认配置已停用，标准目录层级同样停用。
    fixture.QueryRepository.AddConfiguration(TrustedOrganization, FirstProjectCode, isValid: false, categoryIsValid: false, groupIsValid: false, itemIsValid: false);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 6.5m);

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.True(success);
    Assert.Equal(6.5m, Assert.Single(fixture.Repository.ProcessingResults.Values).EstimatedSavingAmount);
    Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    // 处理结果链不读取互认配置列表，也不读取标准目录。
    Assert.Equal(0, fixture.QueryRepository.ConfigurationQueryCalls);
  }

  /// <summary>
  /// V95：业务科室与人员的负向要求成立。
  /// </summary>
  /// <remarks>
  /// 互认科室与医生的标识与名称按请求直接保存：不向权限系统补查名称、不以调用账号替代、不校验医生当前所属科室；
  /// 权限系统替身在本入口上零调用，且保存下来的科室与医生标识不是调用账号的操作人标识。
  /// 本用例同时取证"权限系统中不存在或已停用不导致拒绝"：请求提供的科室与医生标识从未在权限系统登记过。
  /// </remarks>
  [Fact]
  public async Task Business_department_and_doctor_are_saved_as_submitted_without_permission_lookups()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, bool success) = await ExecuteProcessingResultAsync(fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.True(success);
    RecognitionProcessingResult result = Assert.Single(fixture.Repository.ProcessingResults.Values);
    Assert.Equal(RecognitionDeptId, result.RecognitionDeptId);
    Assert.Equal(RecognitionDeptName, result.RecognitionDeptName);
    Assert.Equal(RecognitionDoctorId, result.RecognitionDoctorId);
    Assert.Equal(RecognitionDoctorName, result.RecognitionDoctorName);

    // 不得以调用账号替代互认科室与互认医生：保存的主体是请求提供的业务主体，不是认证账号对应的操作人。
    Assert.NotEqual(TrustedOperId.ToString(), result.RecognitionDeptId);
    Assert.NotEqual(TrustedOperId.ToString(), result.RecognitionDoctorId);

    // 不向权限系统补查名称、不校验其存在或停用：本入口在权限系统替身上零调用。
    Assert.Equal(0, fixture.UserAppService.GetUserByIdCallCount);
    Assert.Equal(0, fixture.OrganizationAppService.OrganizationReadCount);

    // 事件同样承载请求提供的业务主体。
    RecognitionProcessingResultsSavedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionProcessingResultsSavedEvent>());
    Assert.Equal(RecognitionDeptId, recordedEvent.RecognitionDeptId);
    Assert.Equal(RecognitionDoctorId, recordedEvent.RecognitionDoctorId);
  }

  /// <summary>
  /// README 约定 13：写路径上的真实入口断言请求校验先于领域写入。
  /// </summary>
  /// <remarks>请求校验失败时既不读取处理结果与匹配项，也不写入任何内容、不登记事件。</remarks>
  [Fact]
  public async Task Processing_result_request_validation_precedes_domain_reads_and_writes()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    await Assert.ThrowsAsync<ValidationException>(() => WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionProcessingResultsAsync(
      new RecognitionProcessingResultSubmissionRequest
      {
        RecognitionMatchRecordId = ProcessingRecordId,
        RecognitionTime = ProcessingRecognitionTime,
        RecognitionDeptId = string.Empty,
        RecognitionDeptName = RecognitionDeptName,
        RecognitionDoctorId = RecognitionDoctorId,
        RecognitionDoctorName = RecognitionDoctorName,
        ProcessingResults = [Adopted(FirstMatchItemId)]
      })));

    Assert.Equal(0, fixture.Repository.ProcessingResultQueryCalls);
    Assert.Equal(0, fixture.Repository.MatchItemQueryCalls);
    Assert.Equal(0, fixture.Repository.AmountReadCalls);
    Assert.Equal(0, fixture.QueryRepository.ValidReportVersionQueryCalls);
    Assert.Empty(fixture.Repository.ProcessingResults);
    Assert.Null(fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);
  }

  /// <summary>
  /// 不相干组织、医院或院区发起提交时匹配记录不存在，整次失败，零写入、零事件。
  /// </summary>
  [Fact]
  public async Task Unknown_match_record_fails_without_writes()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest([Adopted(FirstMatchItemId)], recognitionMatchRecordId: Guid.NewGuid()));

    Assert.Contains("业务拒绝", error.Message, StringComparison.Ordinal);
    Assert.Empty(fixture.Repository.ProcessingResults);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// 行数守卫：处理结果写入影响行数不为 1 时整次失败，不写入处理结果保存时间、不登记事件。
  /// </summary>
  /// <remarks>整批原子性由显式事务保证，本用例只取证行数守卫本身；真实入口的零提交面由票 08 的失败注入取证。</remarks>
  [Fact]
  public async Task Row_count_guard_prevents_saved_time_update_and_event()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode, SecondProjectCode);
    fixture.Repository.FailProcessingResultWrite = true;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest([Adopted(FirstMatchItemId), Adopted(SecondMatchItemId)]));

    Assert.Contains("影响的行数", error.Message, StringComparison.Ordinal);
    Assert.Empty(fixture.Repository.ProcessingResults);
    Assert.Equal(0, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.Null(fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// 行数守卫：处理结果保存时间写入影响行数不为 1 时整次失败且不登记事件。
  /// </summary>
  [Fact]
  public async Task Row_count_guard_on_saved_time_prevents_event()
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, FirstProjectCode);
    fixture.Repository.FailDecisionSavedTimeWrite = true;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteProcessingResultExpectingRejectionAsync(
      fixture, BuildProcessingRequest(Adopted(FirstMatchItemId)));

    Assert.Contains("影响的行数", error.Message, StringComparison.Ordinal);
    Assert.Equal(1, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.Empty(events.Events);
  }

  /// <summary>引用结果用例的第一条互认匹配项标识；组内标识按升序构造，使匹配项顺序与提交顺序一致。</summary>
  private static readonly Guid FirstReferenceMatchItemId = Guid.Parse("51f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f81");

  /// <summary>引用结果用例的第二条互认匹配项标识，用于构造一次请求包含不同匹配记录项目的场景。</summary>
  private static readonly Guid SecondReferenceMatchItemId = Guid.Parse("61f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f82");

  /// <summary>引用结果用例中第二个匹配记录标识；第二个匹配项归属该记录。</summary>
  private static readonly Guid SecondReferenceRecordId = Guid.Parse("71f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f83");

  /// <summary>不存在的互认匹配项标识，用于构造匹配项不存在前置。</summary>
  private static readonly Guid UnknownReferenceMatchItemId = Guid.Parse("81f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f84");

  /// <summary>引用结果用例的引用科室标识。</summary>
  private const string ReferenceDeptId = "RDEPT-01";

  /// <summary>引用结果用例的引用科室名称。</summary>
  private const string ReferenceDeptName = "呼吸内科";

  /// <summary>引用结果用例的引用医生标识。</summary>
  private const string ReferenceDoctorId = "RDOC-01";

  /// <summary>引用结果用例的引用医生名称。</summary>
  private const string ReferenceDoctorName = "王医生";

  /// <summary>该组已保存的互认时间；实际引用时间取该值之后且不晚于请求接收时间。</summary>
  /// <remarks>取值在用例夹具类型初始化时冻结一次，使同一条用例内多次读取得到同一时点，不随时钟推进漂移。</remarks>
  private static readonly DateTime ReferenceRecognitionTime = DateTime.Now.AddHours(-3);

  /// <summary>本次提交的实际引用时间；晚于已保存的互认时间且早于请求接收时间。</summary>
  /// <remarks>同 <see cref="ReferenceRecognitionTime"/>，冻结一次以避免同一用例内的读取得到不同取值。</remarks>
  private static readonly DateTime ReferenceTime = DateTime.Now.AddHours(-1);

  /// <summary>
  /// V59：引用结果基本提交保存引用科室与医生的标识与名称、实际引用时间，登记一次引用结果已记录事件。
  /// </summary>
  /// <remarks>
  /// 逐项反查通过后整批保存：引用事实只写入匹配项标识、实际引用时间、引用科室与医生与操作字段，
  /// 不写入匹配记录标识、本次来源就诊与互认时间，也不写入采纳次数、来源医院被认次数或预计节省金额。
  /// </remarks>
  [Fact]
  public async Task Reference_submission_saves_facts_and_records_one_event()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);

    // 引用事实按请求原样保存项目级事实，并按服务端操作字段写入操作人与操作时间。
    RecognitionReference reference = Assert.Single(fixture.Repository.References.Values);
    Assert.Equal(FirstReferenceMatchItemId, reference.RecognitionMatchItemId);
    Assert.Equal(ReferenceTime, reference.ReferencedTime);
    Assert.Equal(ReferenceDeptId, reference.ReferenceDeptId);
    Assert.Equal(ReferenceDeptName, reference.ReferenceDeptName);
    Assert.Equal(ReferenceDoctorId, reference.ReferenceDoctorId);
    Assert.Equal(ReferenceDoctorName, reference.ReferenceDoctorName);
    Assert.Equal(TrustedOperId, reference.OperId);

    // 只登记一次引用结果已记录事件，事件承载匹配项标识集合与逐项引用事实。
    RecognitionReferencesRecordedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
    Assert.Equal([FirstReferenceMatchItemId], recordedEvent.ReferencedMatchItemIds);
    RecognitionReferenceFact recordedFact = Assert.Single(recordedEvent.ReferenceFacts);
    Assert.Equal(FirstReferenceMatchItemId, recordedFact.RecognitionMatchItemId);
    Assert.Equal(ReferenceTime, recordedFact.ReferencedTime);
    Assert.Equal(ReferenceDeptId, recordedFact.ReferenceDeptId);
    Assert.Equal(ReferenceDoctorId, recordedFact.ReferenceDoctorId);
    // 引用链不校验绑定报告版本有效性，也不读取任何金额或处理结果保存时间。
    Assert.Equal(0, fixture.QueryRepository.ValidReportVersionQueryCalls);
    Assert.Equal(0, fixture.Repository.AmountReadCalls);
    Assert.Equal(0, fixture.Repository.DecisionSavedTimeUpdateCalls);
  }

  /// <summary>
  /// V59 的读取次数面：既有引用事实、匹配项、处理结果与所属匹配记录各按标识集合一次读回，读取次数不随提交项数增长。
  /// </summary>
  /// <remarks>
  /// 逐项读取时返回内容仍然正确，只有读取次数会随提交项数增长，因此该约束必须由读取次数断言承担。
  /// </remarks>
  [Fact]
  public async Task Reference_lookups_are_read_once_for_the_whole_batch()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(
      fixture,
      FirstReferenceMatchItemId,
      ProcessingRecordId,
      AdoptedProcessingResult(FirstReferenceMatchItemId),
      AdoptedProcessingResult(SecondReferenceMatchItemId, SecondReferenceRecordId));

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture,
      BuildReferenceRequest(
        ReferenceItem(FirstReferenceMatchItemId, ReferenceTime),
        ReferenceItem(SecondReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    Assert.Equal(2, fixture.Repository.References.Count);
    Assert.Equal(1, fixture.Repository.ReferenceQueryCalls);
    Assert.Equal(1, fixture.Repository.ReferenceMatchItemsByIdsQueryCalls);
    Assert.Equal(1, fixture.Repository.ReferenceProcessingResultQueryCalls);
    Assert.Equal(1, fixture.Repository.ReferenceMatchRecordsByIdsQueryCalls);
    Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
  }

  /// <summary>
  /// V60：一次请求包含来自不同互认匹配记录的项目时逐项反查归属后整批原子保存。
  /// </summary>
  [Fact]
  public async Task Reference_submission_accepts_items_from_different_match_records()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(
      fixture,
      FirstReferenceMatchItemId,
      ProcessingRecordId,
      AdoptedProcessingResult(FirstReferenceMatchItemId),
      AdoptedProcessingResult(SecondReferenceMatchItemId, SecondReferenceRecordId));

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture,
      BuildReferenceRequest(
        ReferenceItem(FirstReferenceMatchItemId, ReferenceTime),
        ReferenceItem(SecondReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    Assert.Equal(2, fixture.Repository.References.Count);
    Assert.Equal(
      [FirstReferenceMatchItemId, SecondReferenceMatchItemId],
      [.. fixture.Repository.References.Values.Select(reference => reference.RecognitionMatchItemId).Order()]);
    RecognitionReferencesRecordedEvent recordedEvent = Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
    Assert.Equal([FirstReferenceMatchItemId, SecondReferenceMatchItemId], recordedEvent.ReferencedMatchItemIds);
  }

  /// <summary>
  /// V61：互认匹配项不存在时整次失败、整批不保存、不登记事件。
  /// </summary>
  [Fact]
  public async Task Unknown_match_item_fails_the_whole_reference_submission()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture,
      BuildReferenceRequest(
        ReferenceItem(FirstReferenceMatchItemId, ReferenceTime),
        ReferenceItem(UnknownReferenceMatchItemId, ReferenceTime)));

    Assert.Contains("互认匹配项不存在", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V62：匹配项对应的处理结果不存在时整次失败。
  /// </summary>
  [Fact]
  public async Task Missing_processing_result_fails_the_whole_reference_submission()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    // 匹配项与匹配记录都存在，但该匹配项没有任何处理结果。
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.Contains("处理结果不存在", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V63：匹配项对应的决定为不采纳时整次失败。
  /// </summary>
  [Fact]
  public async Task Non_adopted_processing_result_fails_the_whole_reference_submission()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(
      fixture,
      FirstReferenceMatchItemId,
      ProcessingRecordId,
      NotAdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.Contains("不是采纳", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V64：当前可信三值与匹配记录接收三值不一致时整次失败、不保存任何项目。
  /// </summary>
  /// <param name="recordOrganization">匹配记录的接收组织编码。</param>
  /// <param name="recordHospital">匹配记录的接收医院编码。</param>
  /// <param name="recordBranch">匹配记录的接收院区编码。</param>
  [Theory]
  [InlineData("ORG-B", TrustedHospital, TrustedBranch)]
  [InlineData(TrustedOrganization, OtherHospital, TrustedBranch)]
  [InlineData(TrustedOrganization, TrustedHospital, "BRH-B")]
  public async Task Mismatched_reference_ownership_fails(string recordOrganization, string recordHospital, string recordBranch)
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(
      fixture,
      FirstReferenceMatchItemId,
      ProcessingRecordId,
      AdoptedProcessingResult(FirstReferenceMatchItemId));
    fixture.Repository.MatchRecords[ProcessingRecordId].ReceiverOrganizationCode = recordOrganization;
    fixture.Repository.MatchRecords[ProcessingRecordId].ReceiverHospitalCode = recordHospital;
    fixture.Repository.MatchRecords[ProcessingRecordId].ReceiverBranchCode = recordBranch;
    int organizationReadsBefore = fixture.OrganizationAppService.OrganizationReadCount;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.Contains("不属于当前调用归属", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
    // 归属校验只比较匹配记录的接收三值与可信上下文，不向组织服务核对存在、启用与父子归属。
    Assert.Equal(organizationReadsBefore, fixture.OrganizationAppService.OrganizationReadCount);
  }

  /// <summary>
  /// V65 的请求面：引用科室或引用医生的标识与名称不成对时被请求校验拒绝，拒绝发生在任何领域读写之前。
  /// </summary>
  /// <param name="deptName">引用科室名称。</param>
  /// <param name="doctorName">引用医生名称。</param>
  [Theory]
  [InlineData("", ReferenceDoctorName)]
  [InlineData("   ", ReferenceDoctorName)]
  [InlineData(ReferenceDeptName, "")]
  [InlineData(ReferenceDeptName, "   ")]
  public async Task Unpaired_reference_subject_is_rejected_by_request_validation(string deptName, string doctorName)
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, ValidationException error) = await ExecuteReferenceExpectingValidationRejectionAsync(
      fixture,
      BuildReferenceRequest(new RecognitionReferenceItemRequest
      {
        RecognitionMatchItemId = FirstReferenceMatchItemId,
        ReferencedTime = ReferenceTime,
        ReferenceDeptId = ReferenceDeptId,
        ReferenceDeptName = deptName,
        ReferenceDoctorId = ReferenceDoctorId,
        ReferenceDoctorName = doctorName
      }));

    Assert.Contains("参数校验失败", error.Message, StringComparison.Ordinal);
    // 请求校验先于一切领域读写：既不读取既有引用事实与匹配项，也不写入任何内容、不登记事件。
    Assert.Equal(0, fixture.Repository.ReferenceQueryCalls);
    Assert.Equal(0, fixture.Repository.ReferenceMatchItemsByIdsQueryCalls);
    Assert.Empty(fixture.Repository.References);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V65 的领域面：引用科室或引用医生的标识与名称不成对时管理器整次失败且不写入任何内容。
  /// </summary>
  /// <remarks>
  /// 本判定是请求校验面之外的防御性约束，经公开入口不可达（请求契约的非空白校验先拦截），因此直接调用管理器取证；
  /// 用例同时证明该判定先于写入：拒绝时既没有引用事实，也没有登记事件。
  /// </remarks>
  /// <param name="deptId">引用科室标识。</param>
  /// <param name="doctorId">引用医生标识。</param>
  [Theory]
  [InlineData("", ReferenceDoctorId)]
  [InlineData("   ", ReferenceDoctorId)]
  [InlineData(ReferenceDeptId, "")]
  [InlineData(ReferenceDeptId, "   ")]
  public async Task Unpaired_reference_subject_fails_the_domain_validation(string deptId, string doctorId)
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
        () => fixture.Manager.SubmitRecognitionReferencesAsync(new SubmitRecognitionReferencesCommand
        {
          OrganizationCode = TrustedOrganization,
          HospitalCode = TrustedHospital,
          BranchCode = TrustedBranch,
          ReferenceItems =
          [
            new SubmitRecognitionReferenceCommandItem
            {
              RecognitionMatchItemId = FirstReferenceMatchItemId,
              ReferencedTime = ReferenceTime,
              ReferenceDeptId = deptId,
              ReferenceDeptName = ReferenceDeptName,
              ReferenceDoctorId = doctorId,
              ReferenceDoctorName = ReferenceDoctorName
            }
          ],
          // 与渠道应用服务的取值口径一致：操作字段取协调世界时，请求接收时点取本地墙上时间。
          OperTime = DateTimeOffset.UtcNow,
          ReceivedTime = DateTime.Now,
          OperId = TrustedOperId
        }));

      Assert.Contains("成对提供", error.Message, StringComparison.Ordinal);
      Assert.Empty(fixture.Repository.References);
      Assert.Empty(events.Events);
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>
  /// V66：实际引用时间早于该组已保存的互认时间时整次失败、不保存任何项目。
  /// </summary>
  [Fact]
  public async Task Referenced_time_before_recognition_time_fails()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceRecognitionTime.AddSeconds(-1))));

    Assert.Contains("早于该组已保存的互认时间", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V67：实际引用时间晚于本次请求接收时间时整次失败并提示检查系统时钟。
  /// </summary>
  [Fact]
  public async Task Referenced_time_after_request_receipt_fails_and_asks_to_check_the_clock()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, DateTime.Now.AddDays(1))));

    Assert.Contains("请检查系统时钟", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// V68：实际引用时间等于两端边界时接受，两端边界都包含。
  /// </summary>
  /// <remarks>
  /// 下界取该组已保存的互认时间本身；上界由用例在入口调用前取当前时点并把实际引用时间放在该时点之前一秒，
  /// 使"等于请求接收时间"这一端的判定落在服务端取到的接收时间上而不依赖用例执行耗时。
  /// 等边界接受与边界外一档拒绝在本用例内成对取证：忽略实际引用时间的管理器会让边界外一档的拒绝判定失败。
  /// </remarks>
  [Fact]
  public async Task Referenced_time_equal_to_both_boundaries_is_accepted()
  {
    // 下界边界：实际引用时间恰好等于该组已保存的互认时间。
    ReferenceFixture lowerFixture = CreateReferenceFixture();
    SeedReferenceGroup(lowerFixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    (RecordingEventQueue lowerEvents, bool lowerSuccess) = await ExecuteReferenceAsync(
      lowerFixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceRecognitionTime)));
    Assert.True(lowerSuccess);
    Assert.Single(lowerEvents.Events.OfType<RecognitionReferencesRecordedEvent>());

    // 上界边界：实际引用时间取调用前一刻的当前时点，服务端取到的接收时间不早于该取值。
    ReferenceFixture upperFixture = CreateReferenceFixture();
    SeedReferenceGroup(upperFixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    DateTime beforeCall = DateTime.Now;
    (RecordingEventQueue upperEvents, bool upperSuccess) = await ExecuteReferenceAsync(
      upperFixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, beforeCall)));
    Assert.True(upperSuccess);
    Assert.Single(upperEvents.Events.OfType<RecognitionReferencesRecordedEvent>());

    // 边界外一档：下界下方一秒与上界上方一秒都必须整次失败且零写入、零事件。
    ReferenceFixture belowLowerFixture = CreateReferenceFixture();
    SeedReferenceGroup(belowLowerFixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    (RecordingEventQueue belowLowerEvents, InvalidOperationException belowLowerError) = await ExecuteReferenceExpectingRejectionAsync(
      belowLowerFixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceRecognitionTime.AddSeconds(-1))));
    Assert.Contains("早于该组已保存的互认时间", belowLowerError.Message, StringComparison.Ordinal);
    AssertNothingReferenced(belowLowerFixture, belowLowerEvents);

    ReferenceFixture aboveUpperFixture = CreateReferenceFixture();
    SeedReferenceGroup(aboveUpperFixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    (RecordingEventQueue aboveUpperEvents, InvalidOperationException aboveUpperError) = await ExecuteReferenceExpectingRejectionAsync(
      aboveUpperFixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, DateTime.Now.AddDays(1))));
    Assert.Contains("请检查系统时钟", aboveUpperError.Message, StringComparison.Ordinal);
    AssertNothingReferenced(aboveUpperFixture, aboveUpperEvents);
  }

  /// <summary>
  /// V69：幂等重试完全一致时返回成功，不新增记录、不重复统计、不登记事件。
  /// </summary>
  [Fact]
  public async Task Idempotent_reference_retry_succeeds_without_new_writes()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    RecognitionReferenceSubmissionRequest request = BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime));
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionReferencesAsync(request));
    Guid firstReferenceId = Assert.Single(fixture.Repository.References.Keys);

    (RecordingEventQueue retryEvents, bool retrySuccess) = await ExecuteReferenceAsync(fixture, request);

    Assert.True(retrySuccess);
    Assert.Single(fixture.Repository.References);
    Assert.Equal(firstReferenceId, Assert.Single(fixture.Repository.References.Keys));
    Assert.Empty(retryEvents.Events);
  }

  /// <summary>
  /// V70：相同匹配项下参与比较的事实字段不同时返回冲突，不覆盖既有引用事实、不登记事件。
  /// </summary>
  /// <param name="difference">差异种类：实际引用时间、引用科室标识或引用医生标识。</param>
  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  public async Task Conflict_when_reference_facts_differ(int difference)
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionReferencesAsync(
      BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime))));
    RecognitionReference savedReference = Assert.Single(fixture.Repository.References.Values);

    RecognitionReferenceItemRequest conflictingItem = difference switch
    {
      0 => ReferenceItem(FirstReferenceMatchItemId, ReferenceTime.AddSeconds(-1), referenceDeptId: ReferenceDeptId, referenceDoctorId: ReferenceDoctorId),
      1 => ReferenceItem(FirstReferenceMatchItemId, ReferenceTime, referenceDeptId: "RDEPT-99", referenceDoctorId: ReferenceDoctorId),
      _ => ReferenceItem(FirstReferenceMatchItemId, ReferenceTime, referenceDeptId: ReferenceDeptId, referenceDoctorId: "RDOC-99")
    };

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(conflictingItem));

    Assert.Contains("冲突", error.Message, StringComparison.Ordinal);
    // 不覆盖既有引用事实：条数不变，且首次保存的引用时间与主体原样保留。
    Assert.Single(fixture.Repository.References);
    Assert.Equal(savedReference.ReferencedTime, Assert.Single(fixture.Repository.References.Values).ReferencedTime);
    Assert.Equal(ReferenceDeptId, Assert.Single(fixture.Repository.References.Values).ReferenceDeptId);
    Assert.Equal(ReferenceDoctorId, Assert.Single(fixture.Repository.References.Values).ReferenceDoctorId);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V70b：相同业务键仅引用科室或引用医生名称不同时按幂等成功处理，名称不参与一致性判断。
  /// </summary>
  [Fact]
  public async Task Idempotent_success_when_only_reference_names_differ()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    await WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionReferencesAsync(
      BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime))));

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture,
      BuildReferenceRequest(ReferenceItem(
        FirstReferenceMatchItemId, ReferenceTime, referenceDeptName: "改名后的引用科室", referenceDoctorName: "改名后的引用医生")));

    Assert.True(success);
    // 名称只保存不参与比较：既有行保留首次保存的名称，不因本次改名而被改写。
    Assert.Equal(ReferenceDeptName, Assert.Single(fixture.Repository.References.Values).ReferenceDeptName);
    Assert.Equal(ReferenceDoctorName, Assert.Single(fixture.Repository.References.Values).ReferenceDoctorName);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// V71：提交时绑定报告版本已非当前有效版本（报告已更正或作废）仍接受该引用事实。
  /// </summary>
  /// <remarks>
  /// 引用结果链有意不校验报告版本有效性，也不读取有效版本标识：断言读取次数为零，
  /// 证明接受的依据是"不校验"，而不是"恰好该版本仍然有效"。
  /// </remarks>
  [Fact]
  public async Task Reference_submission_accepts_an_invalidated_report_version()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    Guid reportVersionId = SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    // 报告已形成后续版本或已作废：该绑定版本不再出现在当前有效版本集合中。
    fixture.QueryRepository.ValidReportVersionIds.Remove(reportVersionId);

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    Assert.Single(fixture.Repository.References);
    Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
    Assert.Equal(0, fixture.QueryRepository.ValidReportVersionQueryCalls);
  }

  /// <summary>
  /// V72：引用只增加引用次数；采纳次数、来源医院被认次数与预计节省金额不变。
  /// </summary>
  /// <remarks>
  /// 三条派生统计在本阶段不落库，因此观察口径为：提交引用结果既不读取任何当前金额，也不改写任何既有处理结果，
  /// 也不更新处理结果保存时间，且事件不承载金额口径。
  /// </remarks>
  [Fact]
  public async Task Reference_submission_does_not_touch_adoption_amount_or_saved_time()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    RecognitionProcessingResult savedResult = Assert.Single(fixture.Repository.ProcessingResults.Values);
    fixture.Repository.AddAmount(TrustedOrganization, TrustedHospital, TrustedBranch, FirstProjectCode, 12.34m);

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    Assert.Single(fixture.Repository.References);
    // 不读取也不形成新的预计节省金额：采纳项目的既有处理结果原样保留。
    Assert.Equal(0, fixture.Repository.AmountReadCalls);
    Assert.Single(fixture.Repository.ProcessingResults);
    Assert.Equal(savedResult.EstimatedSavingAmount, Assert.Single(fixture.Repository.ProcessingResults.Values).EstimatedSavingAmount);
    // 不更新处理结果保存时间，采纳次数与来源医院被认次数在本阶段不落库、不被写入。
    Assert.Equal(0, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
  }

  /// <summary>
  /// V72b：超过引用详情有效期仍可提交引用结果，提交不由任何有效期参数限制。
  /// </summary>
  /// <remarks>
  /// 本入口不读取系统参数：断言参数服务零调用，证明接受与拒绝都不依赖引用详情有效时长；
  /// 前置为处理结果保存时间已远早于当前时点，即该组引用详情已超期。
  /// </remarks>
  [Fact]
  public async Task Reference_submission_is_not_limited_by_the_citation_detail_validity()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    // 引用详情有效期在阶段内最长以小时计：把处理结果保存时间推到很久以前，表达该组引用详情早已超期。
    fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime = DateTime.Now.AddYears(-1);

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    Assert.Single(fixture.Repository.References);
    Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>());
    Assert.Equal(0, fixture.SystemParameterAppService.ReadCount);
  }

  /// <summary>
  /// V95 的引用侧面：引用科室与引用医生按请求直接保存，不向权限系统补查、不以调用账号替代。
  /// </summary>
  /// <remarks>
  /// 权限系统替身在本入口上零调用；保存下来的引用科室与引用医生标识不是调用账号对应的操作人标识；
  /// 请求提供的引用科室与引用医生标识从未在权限系统登记过，因此本用例同时取证"权限系统中不存在或已停用不导致拒绝"。
  /// </remarks>
  [Fact]
  public async Task Reference_subject_is_saved_as_submitted_without_permission_lookups()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    (RecordingEventQueue events, bool success) = await ExecuteReferenceAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.True(success);
    RecognitionReference reference = Assert.Single(fixture.Repository.References.Values);
    Assert.Equal(ReferenceDeptId, reference.ReferenceDeptId);
    Assert.Equal(ReferenceDeptName, reference.ReferenceDeptName);
    Assert.Equal(ReferenceDoctorId, reference.ReferenceDoctorId);
    Assert.Equal(ReferenceDoctorName, reference.ReferenceDoctorName);

    // 不得以调用账号替代引用科室与引用医生：保存的主体是请求提供的业务主体，不是认证账号对应的操作人。
    Assert.NotEqual(TrustedOperId.ToString(), reference.ReferenceDeptId);
    Assert.NotEqual(TrustedOperId.ToString(), reference.ReferenceDoctorId);

    // 不向权限系统补查名称、不校验其存在或停用：本入口在权限系统与组织服务替身上零调用。
    Assert.Equal(0, fixture.UserAppService.GetUserByIdCallCount);
    Assert.Equal(0, fixture.OrganizationAppService.OrganizationReadCount);

    // 事件同样承载请求提供的业务主体。
    RecognitionReferenceFact recordedFact = Assert.Single(Assert.Single(events.Events.OfType<RecognitionReferencesRecordedEvent>()).ReferenceFacts);
    Assert.Equal(ReferenceDeptId, recordedFact.ReferenceDeptId);
    Assert.Equal(ReferenceDoctorId, recordedFact.ReferenceDoctorId);
  }

  /// <summary>
  /// README 约定 13：写路径上的真实入口断言请求校验先于领域写入。
  /// </summary>
  /// <remarks>请求校验失败时既不读取既有引用事实、匹配项、处理结果与匹配记录，也不写入任何内容、不登记事件。</remarks>
  [Fact]
  public async Task Reference_request_validation_precedes_domain_reads_and_writes()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));

    await Assert.ThrowsAsync<ValidationException>(() => WithEventQueueAsync(() => fixture.AppService.SubmitRecognitionReferencesAsync(
      new RecognitionReferenceSubmissionRequest { ReferenceItems = [] })));

    Assert.Equal(0, fixture.Repository.ReferenceQueryCalls);
    Assert.Equal(0, fixture.Repository.ReferenceMatchItemsByIdsQueryCalls);
    Assert.Equal(0, fixture.Repository.ReferenceProcessingResultQueryCalls);
    Assert.Equal(0, fixture.Repository.ReferenceMatchRecordsByIdsQueryCalls);
    Assert.Empty(fixture.Repository.References);
  }

  /// <summary>
  /// 写入行数守卫：引用事实写入影响行数不为 1 时整次失败、不登记事件。
  /// </summary>
  /// <remarks>整批原子性由显式事务保证，本用例只取证行数守卫本身；真实入口的零提交面由票 08 的失败注入取证。</remarks>
  [Fact]
  public async Task Reference_row_count_guard_prevents_the_event()
  {
    ReferenceFixture fixture = CreateReferenceFixture();
    SeedReferenceGroup(fixture, FirstReferenceMatchItemId, ProcessingRecordId, AdoptedProcessingResult(FirstReferenceMatchItemId));
    fixture.Repository.FailReferenceWrite = true;

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteReferenceExpectingRejectionAsync(
      fixture, BuildReferenceRequest(ReferenceItem(FirstReferenceMatchItemId, ReferenceTime)));

    Assert.Contains("影响的行数", error.Message, StringComparison.Ordinal);
    AssertNothingReferenced(fixture, events);
  }

  /// <summary>
  /// 引用结果提交用例的全部测试替身与入口。
  /// </summary>
  private sealed class ReferenceFixture
  {
    /// <summary>写侧仓储替身，保存匹配记录、匹配项、处理结果与引用事实。</summary>
    public FakeReportRepository Repository { get; init; } = new();
    /// <summary>报告版本有效性等只读投影替身；引用结果链不读取有效性，调用次数用于取证零调用。</summary>
    public FakeMatchQueryRepository QueryRepository { get; init; } = new();
    /// <summary>外部组织服务替身；本入口不读取组织信息，调用次数用于取证零调用。</summary>
    public StubOrganizationAppService OrganizationAppService { get; init; } = new();
    /// <summary>外部用户服务替身；本入口的可信三层由令牌提供，调用次数用于取证零调用。</summary>
    public StubUserAppService UserAppService { get; init; } = new();
    /// <summary>系统参数服务替身；引用结果链不读取参数，调用次数用于取证零调用。</summary>
    public StubSystemParameterAppService SystemParameterAppService { get; init; } = new();
    /// <summary>引用结果提交入口。</summary>
    public MedicalRecognitionReportAppService AppService { get; init; } = null!;
    /// <summary>领域管理器；用于直接调用公开领域方法取证经公开入口不可达的防御性判定。</summary>
    public MedicalRecognitionReportManager Manager { get; init; } = null!;
  }

  /// <summary>
  /// 构建一次引用结果用例的替身集合与入口，并预置三层齐全的可信请求上下文。
  /// </summary>
  /// <returns>可直接调用的引用结果提交用例夹具。</returns>
  private static ReferenceFixture CreateReferenceFixture()
  {
    FakeReportRepository repository = new();
    FakeMatchQueryRepository queryRepository = new();
    StubOrganizationAppService organizationAppService = new();
    StubUserAppService userAppService = new();
    StubSystemParameterAppService systemParameterAppService = new();

    // 令牌三层齐全：可信范围解析不读取用户档案，也不向组织服务核对归属。
    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());

    MedicalRecognitionReportManager manager = new(repository, queryRepository);
    return new ReferenceFixture
    {
      Repository = repository,
      QueryRepository = queryRepository,
      OrganizationAppService = organizationAppService,
      UserAppService = userAppService,
      SystemParameterAppService = systemParameterAppService,
      Manager = manager,
      AppService = new MedicalRecognitionReportAppService(
        manager,
        repository,
        organizationAppService,
        userAppService,
        new StubReportPdfFileStore(),
        queryRepository,
        systemParameterAppService)
    };
  }

  /// <summary>
  /// 预置引用结果用例的前置：一条匹配记录、若干匹配项与逐项已保存的处理结果。
  /// </summary>
  /// <remarks>
  /// 匹配项按 <paramref name="matchItemId"/> 归属 <paramref name="matchRecordId"/>，第二个匹配项归属另一个匹配记录，
  /// 使"一次请求包含不同匹配记录的项目"这一场景有真实反查目标；处理结果按 <paramref name="processingResults"/> 原样登记。
  /// </remarks>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="matchItemId">第一条匹配项标识，归属 <paramref name="matchRecordId"/>。</param>
  /// <param name="matchRecordId">第一条匹配记录的标识。</param>
  /// <param name="processingResults">逐项已保存的处理结果；不属于第一条匹配项的结果按第二条匹配记录归属。</param>
  /// <returns>第一条匹配项绑定的报告版本标识。</returns>
  private static Guid SeedReferenceGroup(
    ReferenceFixture fixture,
    Guid matchItemId,
    Guid matchRecordId,
    params RecognitionProcessingResult[] processingResults)
  {
    Guid reportVersionId = Guid.NewGuid();
    fixture.Repository.MatchRecords[matchRecordId] = new RecognitionMatchRecord
    {
      Id = matchRecordId,
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      IdentityDocumentTypeCode = IdentityDocumentTypeCode,
      IdentityDocumentNo = IdentityDocumentNo,
      VisitType = VisitType.Outpatient,
      VisitSerialNo = VisitSerialNo,
      MatchCreatedTime = ReferenceRecognitionTime.AddDays(-1),
      DecisionSavedTime = ReferenceRecognitionTime.AddSeconds(1),
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };
    fixture.Repository.MatchItems[matchItemId] = BuildMatchItem(matchItemId, matchRecordId, FirstProjectCode, reportVersionId);

    foreach (RecognitionProcessingResult processingResult in processingResults)
    {
      Guid ownerRecordId = processingResult.RecognitionMatchItemId == matchItemId ? matchRecordId : SecondReferenceRecordId;
      fixture.Repository.MatchRecords.TryAdd(ownerRecordId, new RecognitionMatchRecord
      {
        Id = ownerRecordId,
        ReceiverOrganizationCode = TrustedOrganization,
        ReceiverHospitalCode = TrustedHospital,
        ReceiverBranchCode = TrustedBranch,
        IdentityDocumentTypeCode = IdentityDocumentTypeCode,
        IdentityDocumentNo = IdentityDocumentNo,
        VisitType = VisitType.Outpatient,
        VisitSerialNo = VisitSerialNo,
        MatchCreatedTime = ReferenceRecognitionTime.AddDays(-1),
        DecisionSavedTime = ReferenceRecognitionTime.AddSeconds(1),
        OperId = TrustedOperId,
        OperTime = DateTimeOffset.UtcNow
      });
      fixture.Repository.MatchItems.TryAdd(
        processingResult.RecognitionMatchItemId,
        BuildMatchItem(processingResult.RecognitionMatchItemId, ownerRecordId, FirstProjectCode));
      fixture.Repository.ProcessingResults[processingResult.Id] = processingResult;
    }

    return reportVersionId;
  }

  /// <summary>构建一条已采纳的处理结果。</summary>
  /// <param name="matchItemId">匹配项标识。</param>
  /// <param name="matchRecordId">所属匹配记录标识；传空表示第一条匹配记录。</param>
  /// <returns>已采纳的处理结果实体。</returns>
  private static RecognitionProcessingResult AdoptedProcessingResult(Guid matchItemId, Guid? matchRecordId = null) => new()
  {
    Id = Guid.NewGuid(),
    RecognitionMatchRecordId = matchRecordId ?? ProcessingRecordId,
    RecognitionMatchItemId = matchItemId,
    RecognitionTime = ReferenceRecognitionTime,
    RecognitionResult = RecognitionResult.Adopted,
    RecognitionDeptId = RecognitionDeptId,
    RecognitionDeptName = RecognitionDeptName,
    RecognitionDoctorId = RecognitionDoctorId,
    RecognitionDoctorName = RecognitionDoctorName,
    EstimatedSavingAmount = 10m,
    OperId = TrustedOperId,
    OperTime = DateTimeOffset.UtcNow
  };

  /// <summary>构建一条不采纳的处理结果。</summary>
  /// <param name="matchItemId">匹配项标识。</param>
  /// <returns>不采纳的处理结果实体。</returns>
  private static RecognitionProcessingResult NotAdoptedProcessingResult(Guid matchItemId) => new()
  {
    Id = Guid.NewGuid(),
    RecognitionMatchRecordId = ProcessingRecordId,
    RecognitionMatchItemId = matchItemId,
    RecognitionTime = ReferenceRecognitionTime,
    RecognitionResult = RecognitionResult.NotAdopted,
    RecognitionDeptId = RecognitionDeptId,
    RecognitionDeptName = RecognitionDeptName,
    RecognitionDoctorId = RecognitionDoctorId,
    RecognitionDoctorName = RecognitionDoctorName,
    NonAdoptionReason = RecognitionNonAdoptionReason.EmergencyCare,
    OperId = TrustedOperId,
    OperTime = DateTimeOffset.UtcNow
  };

  /// <summary>
  /// 构建一个实际引用项目；项目级字段默认取用例固定取值，可按需要覆盖以构造差异前置。
  /// </summary>
  /// <param name="matchItemId">被引用的互认匹配项标识。</param>
  /// <param name="referencedTime">项目级的实际引用时间。</param>
  /// <param name="referenceDeptId">引用科室标识。</param>
  /// <param name="referenceDeptName">引用科室名称。</param>
  /// <param name="referenceDoctorId">引用医生标识。</param>
  /// <param name="referenceDoctorName">引用医生名称。</param>
  /// <returns>字段齐全的实际引用项目。</returns>
  private static RecognitionReferenceItemRequest ReferenceItem(
    Guid matchItemId,
    DateTime referencedTime,
    string referenceDeptId = ReferenceDeptId,
    string referenceDeptName = ReferenceDeptName,
    string referenceDoctorId = ReferenceDoctorId,
    string referenceDoctorName = ReferenceDoctorName) => new()
  {
    RecognitionMatchItemId = matchItemId,
    ReferencedTime = referencedTime,
    ReferenceDeptId = referenceDeptId,
    ReferenceDeptName = referenceDeptName,
    ReferenceDoctorId = referenceDoctorId,
    ReferenceDoctorName = referenceDoctorName
  };

  /// <summary>构建一次引用结果提交请求，项目按提交顺序原样放入请求。</summary>
  /// <param name="referenceItems">本次已实际写入病历的引用项目。</param>
  /// <returns>字段齐全的引用结果提交请求。</returns>
  private static RecognitionReferenceSubmissionRequest BuildReferenceRequest(params RecognitionReferenceItemRequest[] referenceItems) =>
    new() { ReferenceItems = referenceItems };

  /// <summary>在捕获事件的前提下执行一次引用结果提交请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">引用结果提交请求。</param>
  /// <returns>捕获到的事件集合与提交结果。</returns>
  private static async Task<(RecordingEventQueue Events, bool Success)> ExecuteReferenceAsync(
    ReferenceFixture fixture, RecognitionReferenceSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await fixture.AppService.SubmitRecognitionReferencesAsync(request));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被拒绝的引用结果提交请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">引用结果提交请求。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static async Task<(RecordingEventQueue Events, InvalidOperationException Error)> ExecuteReferenceExpectingRejectionAsync(
    ReferenceFixture fixture, RecognitionReferenceSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<InvalidOperationException>(
        () => fixture.AppService.SubmitRecognitionReferencesAsync(request)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被请求校验拒绝的引用结果提交请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">引用结果提交请求。</param>
  /// <returns>捕获到的事件集合与抛出的请求校验异常。</returns>
  private static async Task<(RecordingEventQueue Events, ValidationException Error)> ExecuteReferenceExpectingValidationRejectionAsync(
    ReferenceFixture fixture, RecognitionReferenceSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<ValidationException>(
        () => fixture.AppService.SubmitRecognitionReferencesAsync(request)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>断言一次失败的引用结果提交没有留下任何引用事实与事件。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="events">捕获到的事件集合。</param>
  private static void AssertNothingReferenced(ReferenceFixture fixture, RecordingEventQueue events)
  {
    Assert.Empty(fixture.Repository.References);
    Assert.Empty(events.Events);
  }

  /// <summary>
  /// 处理结果提交用例的全部测试替身与入口。
  /// </summary>
  private sealed class ProcessingFixture  {
    /// <summary>写侧仓储替身，保存匹配记录、匹配项、处理结果与业务键金额。</summary>
    public FakeReportRepository Repository { get; init; } = new();
    /// <summary>互认匹配、处理结果与报告有效性所需只读投影替身。</summary>
    public FakeMatchQueryRepository QueryRepository { get; init; } = new();
    /// <summary>外部组织服务替身；本入口不读取组织信息，调用次数用于取证零调用。</summary>
    public StubOrganizationAppService OrganizationAppService { get; init; } = new();
    /// <summary>外部用户服务替身；本入口的可信三层由令牌提供，调用次数用于取证零调用。</summary>
    public StubUserAppService UserAppService { get; init; } = new();
    /// <summary>处理结果提交入口。</summary>
    public MedicalRecognitionReportAppService AppService { get; init; } = null!;
  }

  /// <summary>
  /// 构建一次处理结果用例的替身集合与入口，并预置三层齐全的可信请求上下文。
  /// </summary>
  /// <returns>可直接调用的处理结果提交用例夹具。</returns>
  private static ProcessingFixture CreateProcessingFixture()
  {
    FakeReportRepository repository = new();
    FakeMatchQueryRepository queryRepository = new();
    StubOrganizationAppService organizationAppService = new();
    StubUserAppService userAppService = new();

    // 令牌三层齐全：可信范围解析不读取用户档案，也不向组织服务核对归属。
    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());

    return new ProcessingFixture
    {
      Repository = repository,
      QueryRepository = queryRepository,
      OrganizationAppService = organizationAppService,
      UserAppService = userAppService,
      AppService = new MedicalRecognitionReportAppService(
        new MedicalRecognitionReportManager(repository, queryRepository),
        repository,
        organizationAppService,
        userAppService,
        new StubReportPdfFileStore(),
        queryRepository,
        new StubSystemParameterAppService())
    };
  }

  /// <summary>
  /// 构建并预置一个已保存处理结果用例夹具：一个匹配记录与一个匹配项。
  /// </summary>
  /// <param name="standardProjectCode">匹配项的标准项目编码。</param>
  /// <returns>夹具。</returns>
  private static ProcessingFixture CreateProcessingFixtureWithSavedGroup(string standardProjectCode)
  {
    ProcessingFixture fixture = CreateProcessingFixture();
    SeedMatchGroup(fixture, standardProjectCode);
    return fixture;
  }

  /// <summary>
  /// 预置一条互认匹配记录与若干匹配项，并把绑定报告版本登记为当前有效。
  /// </summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="standardProjectCodes">组内匹配项的标准项目编码，按提交顺序各成一项。</param>
  /// <param name="recordOrganization">匹配记录的接收组织编码；默认取可信组织。</param>
  /// <param name="recordHospital">匹配记录的接收医院编码；默认取可信医院。</param>
  /// <param name="recordBranch">匹配记录的接收院区编码；默认取可信院区。</param>
  /// <returns>第一条匹配项绑定的报告版本标识。</returns>
  private static Guid SeedMatchGroup(
    ProcessingFixture fixture,
    params string[] standardProjectCodes)
  {
    fixture.Repository.MatchRecords[ProcessingRecordId] = new RecognitionMatchRecord
    {
      Id = ProcessingRecordId,
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      IdentityDocumentTypeCode = IdentityDocumentTypeCode,
      IdentityDocumentNo = IdentityDocumentNo,
      VisitType = VisitType.Outpatient,
      VisitSerialNo = VisitSerialNo,
      MatchCreatedTime = ProcessingMatchCreatedTime,
      DecisionSavedTime = null,
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };

    // 组内两个匹配项绑定同一个报告版本：联合报告命中多个互认项目时各项目共用同一报告版本。
    Guid reportVersionId = Guid.NewGuid();
    fixture.QueryRepository.ValidReportVersionIds.Add(reportVersionId);
    Guid[] matchItemIds = [FirstMatchItemId, SecondMatchItemId];
    for (int index = 0; index < standardProjectCodes.Length; index++)
      fixture.Repository.MatchItems[matchItemIds[index]] = BuildMatchItem(matchItemIds[index], ProcessingRecordId, standardProjectCodes[index], reportVersionId);

    return reportVersionId;
  }

  /// <summary>
  /// 预置一条接收三值不同于可信上下文的互认匹配记录，用于构造归属不一致前置。
  /// </summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="standardProjectCode">组内匹配项的标准项目编码。</param>
  /// <param name="recordOrganization">匹配记录的接收组织编码。</param>
  /// <param name="recordHospital">匹配记录的接收医院编码。</param>
  /// <param name="recordBranch">匹配记录的接收院区编码。</param>
  /// <returns>第一条匹配项绑定的报告版本标识。</returns>
  private static Guid SeedMatchGroup(
    ProcessingFixture fixture,
    string standardProjectCode,
    string recordOrganization,
    string recordHospital,
    string recordBranch)
  {
    fixture.Repository.MatchRecords[ProcessingRecordId] = new RecognitionMatchRecord
    {
      Id = ProcessingRecordId,
      ReceiverOrganizationCode = recordOrganization,
      ReceiverHospitalCode = recordHospital,
      ReceiverBranchCode = recordBranch,
      IdentityDocumentTypeCode = IdentityDocumentTypeCode,
      IdentityDocumentNo = IdentityDocumentNo,
      VisitType = VisitType.Outpatient,
      VisitSerialNo = VisitSerialNo,
      MatchCreatedTime = ProcessingMatchCreatedTime,
      DecisionSavedTime = null,
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };

    // 匹配项归属本次预置的记录，但其接收三值与可信上下文不同，用于取证归属校验。
    Guid reportVersionId = Guid.NewGuid();
    fixture.QueryRepository.ValidReportVersionIds.Add(reportVersionId);
    fixture.Repository.MatchItems[FirstMatchItemId] = BuildMatchItem(FirstMatchItemId, ProcessingRecordId, standardProjectCode, reportVersionId);
    return reportVersionId;
  }

  /// <summary>构建一条互认匹配项。</summary>
  /// <param name="matchItemId">匹配项标识。</param>
  /// <param name="matchRecordId">所属匹配记录标识。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="reportVersionId">绑定的报告版本标识；传空时新建一个。</param>
  /// <returns>互认匹配项实体。</returns>
  private static RecognitionMatchItem BuildMatchItem(Guid matchItemId, Guid matchRecordId, string standardProjectCode, Guid? reportVersionId = null) => new()
  {
    Id = matchItemId,
    RecognitionMatchRecordId = matchRecordId,
    ItemType = MedicalItemType.Laboratory,
    StandardProjectCode = standardProjectCode,
    ReportId = Guid.NewGuid(),
    ReportVersionId = reportVersionId ?? Guid.NewGuid(),
    OperId = TrustedOperId,
    OperTime = DateTimeOffset.UtcNow
  };

  /// <summary>构建一个采纳决定。</summary>
  /// <param name="matchItemId">匹配项标识。</param>
  /// <returns>采纳的项目决定。</returns>
  private static RecognitionProcessingResultItemRequest Adopted(Guid matchItemId) => new()
  {
    RecognitionMatchItemId = matchItemId,
    Result = RecognitionResult.Adopted
  };

  /// <summary>构建一个不采纳决定。</summary>
  /// <param name="matchItemId">匹配项标识。</param>
  /// <param name="nonAdoptionReason">不采纳原因；传空用于构造"未提供原因"前置。</param>
  /// <param name="nonAdoptionDescription">不采纳补充说明。</param>
  /// <returns>不采纳的项目决定。</returns>
  private static RecognitionProcessingResultItemRequest NotAdopted(
    Guid matchItemId, RecognitionNonAdoptionReason? nonAdoptionReason, string? nonAdoptionDescription = null) => new()
  {
    RecognitionMatchItemId = matchItemId,
    Result = RecognitionResult.NotAdopted,
    NonAdoptionReason = nonAdoptionReason,
    NonAdoptionDescription = nonAdoptionDescription
  };

  /// <summary>
  /// 构建一次处理结果提交请求；组级字段默认取用例固定取值，可按需要覆盖以构造差异前置。
  /// </summary>
  /// <param name="resultItems">组内各匹配项的决定，原样按提交顺序放入请求。</param>
  /// <param name="recognitionTime">互认时间；传空取用例默认取值。</param>
  /// <param name="recognitionDeptId">互认科室标识。</param>
  /// <param name="recognitionDeptName">互认科室名称。</param>
  /// <param name="recognitionDoctorId">互认医生标识。</param>
  /// <param name="recognitionDoctorName">互认医生名称。</param>
  /// <param name="recognitionMatchRecordId">互认匹配记录标识；传空取用例固定取值。</param>
  /// <returns>字段齐全的处理结果提交请求。</returns>
  private static RecognitionProcessingResultSubmissionRequest BuildProcessingRequest(
    RecognitionProcessingResultItemRequest resultItems,
    DateTime? recognitionTime = null,
    string recognitionDeptId = RecognitionDeptId,
    string recognitionDeptName = RecognitionDeptName,
    string recognitionDoctorId = RecognitionDoctorId,
    string recognitionDoctorName = RecognitionDoctorName,
    Guid? recognitionMatchRecordId = null) =>
    BuildProcessingRequest(
      [resultItems], recognitionTime, recognitionDeptId, recognitionDeptName, recognitionDoctorId, recognitionDoctorName, recognitionMatchRecordId);

  /// <summary>
  /// 构建一次处理结果提交请求；组级字段默认取用例固定取值，可按需要覆盖以构造差异前置。
  /// </summary>
  /// <param name="resultItems">组内各匹配项的决定，原样按提交顺序放入请求。</param>
  /// <param name="recognitionTime">互认时间；传空取用例默认取值。</param>
  /// <param name="recognitionDeptId">互认科室标识。</param>
  /// <param name="recognitionDeptName">互认科室名称。</param>
  /// <param name="recognitionDoctorId">互认医生标识。</param>
  /// <param name="recognitionDoctorName">互认医生名称。</param>
  /// <param name="recognitionMatchRecordId">互认匹配记录标识；传空取用例固定取值。</param>
  /// <returns>字段齐全的处理结果提交请求。</returns>
  private static RecognitionProcessingResultSubmissionRequest BuildProcessingRequest(
    IReadOnlyList<RecognitionProcessingResultItemRequest> resultItems,
    DateTime? recognitionTime = null,
    string recognitionDeptId = RecognitionDeptId,
    string recognitionDeptName = RecognitionDeptName,
    string recognitionDoctorId = RecognitionDoctorId,
    string recognitionDoctorName = RecognitionDoctorName,
    Guid? recognitionMatchRecordId = null) => new()
  {
    RecognitionMatchRecordId = recognitionMatchRecordId ?? ProcessingRecordId,
    RecognitionTime = recognitionTime ?? ProcessingRecognitionTime,
    RecognitionDeptId = recognitionDeptId,
    RecognitionDeptName = recognitionDeptName,
    RecognitionDoctorId = recognitionDoctorId,
    RecognitionDoctorName = recognitionDoctorName,
    ProcessingResults = resultItems
  };

  /// <summary>在捕获事件的前提下执行一次处理结果提交请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">处理结果提交请求。</param>
  /// <returns>捕获到的事件集合与提交结果。</returns>
  private static async Task<(RecordingEventQueue Events, bool Success)> ExecuteProcessingResultAsync(
    ProcessingFixture fixture, RecognitionProcessingResultSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await fixture.AppService.SubmitRecognitionProcessingResultsAsync(request));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被拒绝的处理结果提交请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">处理结果提交请求。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static async Task<(RecordingEventQueue Events, InvalidOperationException Error)> ExecuteProcessingResultExpectingRejectionAsync(
    ProcessingFixture fixture, RecognitionProcessingResultSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<InvalidOperationException>(
        () => fixture.AppService.SubmitRecognitionProcessingResultsAsync(request)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被请求校验拒绝的处理结果提交请求。</summary>
  /// <remarks>请求校验失败发生在任何领域读取与写入之前，因此不产生任何处理结果、保存时间更新或事件。</remarks>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">处理结果提交请求。</param>
  /// <returns>捕获到的事件集合与抛出的请求校验异常。</returns>
  private static async Task<(RecordingEventQueue Events, ValidationException Error)> ExecuteProcessingResultExpectingValidationRejectionAsync(
    ProcessingFixture fixture, RecognitionProcessingResultSubmissionRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<ValidationException>(
        () => fixture.AppService.SubmitRecognitionProcessingResultsAsync(request)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>断言一次失败的处理结果提交没有留下任何业务写入与事件。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="events">捕获到的事件集合。</param>
  private static void AssertProcessingNothingWritten(ProcessingFixture fixture, RecordingEventQueue events)
  {
    Assert.Empty(fixture.Repository.ProcessingResults);
    Assert.Equal(0, fixture.Repository.DecisionSavedTimeUpdateCalls);
    Assert.Null(fixture.Repository.MatchRecords[ProcessingRecordId].DecisionSavedTime);
    Assert.Empty(events.Events);
  }
}
