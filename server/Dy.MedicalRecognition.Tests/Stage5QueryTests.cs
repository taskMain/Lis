using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Core.Abstractions.EventBus;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 引用详情查询的应用编排与读模型校验（矩阵 V46-V58 含 V58b、V84 的引用详情面，以及 V88 的查询链面）。
/// </summary>
/// <remarks>
/// 用例把手写查询端口替身、外部组织服务替身、系统参数服务共享替身与可信请求上下文替身注入公开查询应用服务入口，
/// 验证可观察结果：返回的引用详情内容、报告公共上下文的来源医疗机构名称、失败时的业务拒绝文案、
/// 系统参数的严格失败语义、有效期边界与部分可用语义，以及外部组织服务的读取次数不随返回行数增长。
/// 负向用例同时断言"无写入"与"无事件"：本链路不产生任何写入、事件或状态变化。
/// 真实数据库上的语句执行、有效期起点列的时区口径与真实库上的部分可用行为由票 08 取证，不在本文件内验证。
/// 时间用固定输入与相对当前时点的偏移构造，不依赖等待。
/// </remarks>
public sealed class Stage5QueryTests
{
  /// <summary>可信组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>可信医院编码。</summary>
  private const string TrustedHospital = "HOS-A";

  /// <summary>可信院区编码。</summary>
  private const string TrustedBranch = "BRH-A";

  /// <summary>同组织下的第二家医院编码，用于构造来源医疗机构名称按不同医院分别读取的前置。</summary>
  private const string SecondHospital = "HOS-B";

  /// <summary>第二家医院下的院区编码。</summary>
  private const string SecondBranch = "BRH-B";

  /// <summary>可信操作人标识；本链路不读取操作人，仅用于构造可信请求上下文。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>证件类型代码。</summary>
  private const string IdentityDocumentTypeCode = "01";

  /// <summary>证件号码；用例内按需要加入首尾空白与小写差异。</summary>
  private const string IdentityDocumentNo = "110101199001011234";

  /// <summary>本次来源就诊流水号。</summary>
  private const string VisitSerialNo = "V-2026-0001";

  /// <summary>引用详情有效时长参数编码。</summary>
  private const string CitationDetailValidHours = "citation_detail_valid_hours";

  /// <summary>第一条标准项目编码。</summary>
  private const string FirstProjectCode = "SP-1";

  /// <summary>第二条标准项目编码。</summary>
  private const string SecondProjectCode = "SP-2";

  /// <summary>第三条标准项目编码，用于构造同一份报告下未被采纳的项目。</summary>
  private const string ThirdProjectCode = "SP-3";

  /// <summary>第一条互认匹配记录标识。</summary>
  private static readonly Guid FirstRecordId = Guid.Parse("11f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f70");

  /// <summary>第二条互认匹配记录标识，用于构造同次就诊的多个组。</summary>
  private static readonly Guid SecondRecordId = Guid.Parse("12f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f71");

  /// <summary>第一条互认匹配项标识。</summary>
  private static readonly Guid FirstMatchItemId = Guid.Parse("21f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f71");

  /// <summary>第二条互认匹配项标识。</summary>
  private static readonly Guid SecondMatchItemId = Guid.Parse("31f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f72");

  /// <summary>第三条互认匹配项标识。</summary>
  private static readonly Guid ThirdMatchItemId = Guid.Parse("41f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f73");

  /// <summary>第四条互认匹配项标识，用于构造同版本同项目的第二条采纳事实。</summary>
  private static readonly Guid FourthMatchItemId = Guid.Parse("51f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f74");

  /// <summary>第一条报告标识。</summary>
  private static readonly Guid FirstReportId = Guid.Parse("61f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f75");

  /// <summary>第二条报告标识。</summary>
  private static readonly Guid SecondReportId = Guid.Parse("62f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f76");

  /// <summary>第一条报告版本标识。</summary>
  private static readonly Guid FirstReportVersionId = Guid.Parse("71f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f77");

  /// <summary>第二条报告版本标识。</summary>
  private static readonly Guid SecondReportVersionId = Guid.Parse("72f0a2b3-c4d5-4e6f-8a10-2b3c4d5e6f78");

  /// <summary>
  /// V46 与 V84 的引用详情面：基本返回含匹配项标识、报告与版本标识、项目内容、文件信息与报告公共上下文，
  /// 来源医疗机构名称由来源三值经组织路径解析点批量回填，检查所见与检查结论随报告公共上下文返回。
  /// </summary>
  [Fact]
  public async Task Citation_detail_returns_item_content_and_report_context()
  {
    // 时间取值由同一次参照派生：断言逐字比较检查或检验时间与报告时间，两侧必须来自同一时点。
    DateTime now = NowReference();
    DateTime decisionSavedTime = now.AddHours(-1);
    DateTime clinicalTime = now.AddHours(-3);
    DateTime reportTime = now.AddHours(-2);
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId,
        FirstMatchItemId,
        FirstReportId,
        FirstReportVersionId,
        FirstProjectCode,
        decisionSavedTime: decisionSavedTime,
        reportType: MedicalReportType.Laboratory,
        reportNo: "LAB-2026-0001",
        reportName: "血常规报告",
        clinicalTime: clinicalTime,
        reportTime: reportTime,
        pdfFileId: "pdf-key-1",
        pdfFileName: "血常规报告.pdf",
        laboratoryResultNameForProjectCode: SourceResultName));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    RecognitionCitationItemReadModel item = Assert.Single(detail.MatchItems);
    Assert.Equal(FirstMatchItemId, item.RecognitionMatchItemId);
    Assert.Equal(FirstReportId, item.ReportId);
    Assert.Equal(FirstReportVersionId, item.ReportVersionId);
    Assert.Equal(FirstProjectCode, item.StandardProjectCode);
    Assert.Equal($"{FirstProjectCode} 名称", item.StandardProjectName);

    // 检验项目返回本项目的检验结果明细，检查部位为空集合。
    RecognitionCitationLaboratoryResultReadModel result = Assert.Single(item.LaboratoryResults);
    Assert.Equal(SourceResultName, result.ResultItemName);
    Assert.Equal("9.8", result.SourceResultContent);
    Assert.Equal("10^9/L", result.Unit);
    Assert.Equal("4.0-10.0", result.SourceReferenceRange);
    Assert.Equal("偏高", result.SourceAbnormalFlag);
    Assert.Equal("否", result.SourceCriticalValueFlag);
    Assert.Empty(item.ExaminationSites);

    // 报告公共上下文按报告归并：来源三值、名称、业务时间、来源申请与审核医生、文件入口都随项目返回。
    RecognitionReportContextReadModel context = Assert.Single(detail.ReportContext);
    Assert.Equal(FirstReportId, context.ReportId);
    Assert.Equal(FirstReportVersionId, context.ReportVersionId);
    Assert.Equal(MedicalReportType.Laboratory, context.ReportType);
    Assert.Equal("检验报告", context.ReportTypeText);
    Assert.Equal("LAB-2026-0001", context.ReportNo);
    Assert.Equal("血常规报告", context.ReportName);
    Assert.Equal(TrustedOrganization, context.SourceOrganizationCode);
    Assert.Equal("示范组织-ORG-A", context.SourceOrganizationName);
    Assert.Equal(TrustedHospital, context.SourceHospitalCode);
    Assert.Equal("示范医院-HOS-A", context.SourceHospitalName);
    Assert.Equal(TrustedBranch, context.SourceBranchCode);
    Assert.Equal("示范院区-BRH-A", context.SourceBranchName);
    Assert.Equal(clinicalTime, context.ClinicalTime);
    Assert.Equal(reportTime, context.ReportTime);
    Assert.Equal("DOC-APP", context.SourceApplicantDoctorId);
    Assert.Equal("申请医生", context.SourceApplicantDoctorName);
    Assert.Equal("DOC-REV", context.SourceReviewerDoctorId);
    Assert.Equal("审核医生", context.SourceReviewerDoctorName);
    Assert.Null(context.ExaminationFindings);
    Assert.Null(context.ExaminationConclusion);
    Assert.Equal("pdf-key-1", context.File.PdfFileId);
    Assert.Equal("血常规报告.pdf", context.File.PdfOriginalFileName);
    Assert.Equal($"api/v1/report-pdf/{FirstReportId}/versions/{FirstReportVersionId}/pdf", context.File.PdfDownloadUrl);
    Assert.Null(context.File.SourceImageStatus);
    Assert.Null(context.File.SourceImageStatusText);
    Assert.Null(context.File.ImageAccessUrl);
  }

  /// <summary>
  /// V46 的检查侧引用详情面：检查项目返回命中项目的检查部位，检查所见与检查结论随报告公共上下文返回，
  /// 来源影像状态与调阅地址随文件入口返回并按枚举描述派生中文。
  /// </summary>
  [Fact]
  public async Task Examination_citation_detail_returns_sites_findings_and_conclusion()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId,
        FirstMatchItemId,
        FirstReportId,
        FirstReportVersionId,
        FirstProjectCode,
        decisionSavedTime: DecisionSavedTime,
        reportType: MedicalReportType.Examination,
        reportNo: "EXM-2026-0001",
        reportName: "胸部 CT 报告",
        pdfFileId: "pdf-key-2",
        pdfFileName: "胸部CT报告.pdf",
        examinationSiteNames: ["胸部", "纵隔"],
        findings: "双肺纹理清晰",
        conclusion: "未见明显异常",
        sourceImageStatus: SourceImageStatus.Available,
        imageAccessUrl: "http://image/1"));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    RecognitionCitationItemReadModel item = Assert.Single(detail.MatchItems);
    Assert.Empty(item.LaboratoryResults);
    Assert.Equal(["胸部", "纵隔"], item.ExaminationSites.Select(site => site.SiteName));
    Assert.Equal(["SITE-1", "SITE-2"], item.ExaminationSites.Select(site => site.SourceSiteCode));

    RecognitionReportContextReadModel context = Assert.Single(detail.ReportContext);
    Assert.Equal(MedicalReportType.Examination, context.ReportType);
    Assert.Equal("检查报告", context.ReportTypeText);
    Assert.Equal("双肺纹理清晰", context.ExaminationFindings);
    Assert.Equal("未见明显异常", context.ExaminationConclusion);
    Assert.Equal(SourceImageStatus.Available, context.File.SourceImageStatus);
    Assert.Equal("有影像", context.File.SourceImageStatusText);
    Assert.Equal("http://image/1", context.File.ImageAccessUrl);
  }

  /// <summary>
  /// V46 的批量读取面：报告公共上下文、项目内容与检查部位都按集合一次读回，
  /// 来源医疗机构名称的读取次数只随本批不同组织与医院数量增长，不随返回行数增长。
  /// </summary>
  /// <remarks>
  /// 同一份报告下两个项目各自成一个匹配项，报告版本与检查项目都只有一个，
  /// 因此四条内容读取都只应各发生一次；来源只有一家医院，组织、医院与院区各读取一次。
  /// 按行读取时返回内容仍然正确，只有读取次数会随返回行数增长，因此该约束必须由读取次数断言承担。
  /// </remarks>
  [Fact]
  public async Task Citation_content_and_source_names_are_read_once_for_the_whole_batch()
  {
    CitationFixture fixture = CreateFixture();
    Guid matchRecordId = Guid.NewGuid();
    SeedCitation(
      fixture,
      BuildSeed(
        matchRecordId,
        FirstMatchItemId,
        FirstReportId,
        FirstReportVersionId,
        FirstProjectCode,
        decisionSavedTime: DecisionSavedTime,
        reportType: MedicalReportType.Examination,
        examinationSiteNames: ["胸部"]));
    // 同一份报告的第二个互认项目：与第一条匹配项共用同一报告与报告版本。
    SeedCitation(
      fixture,
      BuildSeed(
        matchRecordId,
        SecondMatchItemId,
        FirstReportId,
        FirstReportVersionId,
        SecondProjectCode,
        decisionSavedTime: DecisionSavedTime,
        reportType: MedicalReportType.Examination,
        examinationSiteNames: ["腹部"]));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal(2, detail.MatchItems.Count);
    Assert.Single(detail.ReportContext);
    Assert.Equal(1, fixture.OrganizationAppService.OrganizationReadCount);
    Assert.Equal(1, fixture.OrganizationAppService.HospitalReadCount);
    Assert.Equal(1, fixture.OrganizationAppService.BranchReadCount);
    Assert.Equal(1, fixture.QueryRepository.CitationQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.CitationContextQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.CitationStandardProjectNameQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ValidReportVersionQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationItemQueryCalls);
    Assert.Equal(1, fixture.QueryRepository.ExaminationSiteQueryCalls);
  }

  /// <summary>
  /// V46 的批量读取面：来源涉及同一组织下的两家不同医院时，院区读取按不同医院数量进行。
  /// </summary>
  [Fact]
  public async Task Citation_source_name_reads_branches_once_per_distinct_hospital()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId,
        SecondMatchItemId,
        SecondReportId,
        SecondReportVersionId,
        SecondProjectCode,
        decisionSavedTime: DecisionSavedTime,
        sourceHospitalCode: SecondHospital,
        sourceBranchCode: SecondBranch));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal(2, detail.ReportContext.Count);
    // 组织读取一次；该组织的医院读取一次（按组织去重）；两家不同医院各读取一次院区。
    Assert.Equal(1, fixture.OrganizationAppService.OrganizationReadCount);
    Assert.Equal(1, fixture.OrganizationAppService.HospitalReadCount);
    Assert.Equal(2, fixture.OrganizationAppService.BranchReadCount);
    Assert.Equal("示范医院-HOS-B", detail.ReportContext.Single(context => context.ReportId == SecondReportId).SourceHospitalName);
  }

  /// <summary>
  /// V47：未匹配到该医院、院区、患者与本次就诊下的采纳记录时拒绝返回，只描述平台可确认的事实，不泄露其他患者记录。
  /// </summary>
  /// <remarks>
  /// 前置中存在一条已采纳记录，但它的证件号码与本次请求不同：请求必须整次失败，
  /// 拒绝文案只说明该次就诊没有可返回的采纳记录，既不指出该患者是否存在记录，也不返回任何受保护的报告内容。
  /// </remarks>
  [Fact]
  public async Task Unmatched_visit_fails_without_disclosing_other_patients()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(
      fixture,
      new RecognitionCitationDetailRequest
      {
        IdentityDocumentTypeCode = IdentityDocumentTypeCode,
        IdentityDocumentNo = "110101198001011234",
        VisitType = VisitType.Outpatient,
        VisitSerialNo = VisitSerialNo
      });

    Assert.Contains("未查询到", error.Message, StringComparison.Ordinal);
    Assert.Contains("采纳", error.Message, StringComparison.Ordinal);
    // 不泄露其他患者记录：拒绝文案不含前置记录的证件号码与报告标识。
    Assert.DoesNotContain(IdentityDocumentNo, error.Message, StringComparison.Ordinal);
    Assert.DoesNotContain(FirstReportId.ToString(), error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V47：患者证件与本次就诊都命中、但该次就诊下没有任何采纳记录时同样拒绝，不返回成功空集合。
  /// </summary>
  [Fact]
  public async Task Visit_without_adopted_records_fails_instead_of_returning_empty_result()
  {
    CitationFixture fixture = CreateFixture();
    // 记录命中本次就诊且仍在有效期内，但其决定为不采纳：不采纳项目不参与引用详情。
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime, recognitionResult: RecognitionResult.NotAdopted));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Contains("无已采纳的互认项目", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V48：有效期边界按包含处理，起点为该组处理结果保存时间。
  /// </summary>
  /// <remarks>
  /// 参数取六小时；经过时间落在六小时以内（含下界一侧）的记录允许返回，超过六小时的记录不返回。
  /// 两个起点的耗时差都是整整一分钟，因此判定不受用例执行耗时落在边界那一瞬的影响。
  /// </remarks>
  [Fact]
  public async Task Validity_boundary_is_inclusive_from_the_decision_saved_time()
  {
    const int validHours = 6;
    DateTime now = NowReference();

    // 落在参数值以内：经过时间为六小时差一秒，按包含处理允许返回。
    CitationFixture withinFixture = CreateFixture(validHours);
    SeedCitation(
      withinFixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: now.AddHours(-validHours).AddSeconds(1)));

    RecognitionCitationDetailReadModel withinDetail = await withinFixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());
    Assert.Equal(FirstMatchItemId, Assert.Single(withinDetail.MatchItems).RecognitionMatchItemId);

    // 超过参数值：经过时间为六小时多一秒，不返回该项目且整次失败。
    CitationFixture beyondFixture = CreateFixture(validHours);
    SeedCitation(
      beyondFixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: now.AddHours(-validHours).AddSeconds(-1)));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(beyondFixture, BuildRequest());
    Assert.Contains("有效时长", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(beyondFixture, events);
  }

  /// <summary>
  /// V49：有效时长参数不存在时拒绝返回，不使用兜底默认值。
  /// </summary>
  [Fact]
  public async Task Missing_validity_parameter_fails_without_fallback_default()
  {
    CitationFixture fixture = CreateFixture(parameterMissing: true);
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Equal("业务拒绝：未配置引用详情有效时长。", error.Message);
    // 参数不合法时在任何候选读取之前终止：不使用兜底值，也不产生任何写入与事件。
    Assert.Equal(0, fixture.QueryRepository.CitationQueryCalls);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V50：有效时长参数为非正整数时拒绝返回；零、负数与非整数都判为非法取值。
  /// </summary>
  /// <param name="validHoursValue">参数服务返回的取值文本。</param>
  [Theory]
  [InlineData("0")]
  [InlineData("-1")]
  [InlineData("1.5")]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("abc")]
  [InlineData("true")]
  public async Task Non_positive_integer_validity_parameter_fails(string validHoursValue)
  {
    CitationFixture fixture = CreateFixture(validHoursValue: validHoursValue);
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Equal("业务拒绝：引用详情有效时长取值非法。", error.Message);
    Assert.Equal(0, fixture.QueryRepository.CitationQueryCalls);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V51：参数服务读取失败时拒绝返回，不退化为永久有效。
  /// </summary>
  [Fact]
  public async Task Parameter_service_failure_fails_the_whole_query_without_degrading_to_permanent_validity()
  {
    CitationFixture fixture = CreateFixture(parameterServiceFailure: new InvalidOperationException("参数服务不可用。"));
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Equal("参数服务不可用。", error.Message);
    // 参数服务失败时读取已经发生，但候选读取、写入与事件登记都没有发生。
    Assert.Equal(1, fixture.SystemParameterAppService.ReadCount);
    Assert.Equal(CitationDetailValidHours, Assert.Single(fixture.SystemParameterAppService.ReadCodes));
    Assert.Equal(0, fixture.QueryRepository.CitationQueryCalls);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V52：参数调整后立即按原起点生效；每次请求读取当前参数值，不保存截止时间。
  /// </summary>
  /// <remarks>
  /// 同一采纳记录、两次不同参数值：五小时前保存的记录在八小时参数下可获取，
  /// 参数缩短为两小时后同一记录立即不可获取，未发生任何写入或事件；
  /// 两次判定都离边界至少一小时，因此不依赖用例执行耗时。
  /// </remarks>
  [Fact]
  public async Task Parameter_change_takes_effect_immediately_from_the_same_start_point()
  {
    CitationFixture fixture = CreateFixture(validHours: 8);
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime.AddHours(-5)));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());
    Assert.Equal(FirstMatchItemId, Assert.Single(detail.MatchItems).RecognitionMatchItemId);

    // 参数缩短为两小时：同一采纳记录按原起点立即超期，两次请求各自读取当前参数值。
    fixture.SystemParameterAppService.ValuesByCode[CitationDetailValidHours] = "2";
    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Contains("有效时长", error.Message, StringComparison.Ordinal);
    Assert.Equal(2, fixture.SystemParameterAppService.ReadCount);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V53：绑定报告版本已非当前有效版本时不返回该项目；全部相关项目均失效时拒绝返回。
  /// </summary>
  [Fact]
  public async Task Invalidated_report_version_is_not_returned()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    // 报告已形成后续版本：该绑定版本不再有效。
    fixture.QueryRepository.ValidReportVersionIds.Clear();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Contains("报告版本", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V53、V54：同一份报告的多个项目中只有部分项目绑定版本仍有效时，只返回仍有效的那部分。
  /// </summary>
  /// <remarks>
  /// 同一报告版本上的两个项目，一个已被后续版本替代、一个仍有效；
  /// 版本失效只影响绑定该版本的项目，不因部分项目失效整次失败。
  /// </remarks>
  [Fact]
  public async Task Only_items_with_a_valid_report_version_are_returned()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId, SecondMatchItemId, SecondReportId, SecondReportVersionId, SecondProjectCode, decisionSavedTime: DecisionSavedTime));
    // 报告已形成后续版本：只让第一条匹配项绑定的版本失效。
    fixture.QueryRepository.ValidReportVersionIds.Remove(FirstReportVersionId);

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    RecognitionCitationItemReadModel item = Assert.Single(detail.MatchItems);
    Assert.Equal(SecondMatchItemId, item.RecognitionMatchItemId);
    Assert.Equal(SecondReportVersionId, Assert.Single(detail.ReportContext).ReportVersionId);
  }

  /// <summary>
  /// V53、V54：报告版本已失效与尚未超期并存时，失效与超期两种原因都说明，且只列出平台可确认的原因。
  /// </summary>
  [Fact]
  public async Task Expired_and_invalidated_items_report_both_platform_confirmed_reasons()
  {
    const int validHours = 6;
    CitationFixture fixture = CreateFixture(validHours);
    // 超期且版本有效。
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime.AddHours(-validHours).AddSeconds(-1)));
    // 版本失效且在有效期内。
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId, SecondMatchItemId, SecondReportId, SecondReportVersionId, SecondProjectCode, decisionSavedTime: DecisionSavedTime));
    // 报告已形成后续版本：第二条匹配项绑定的版本失效。
    fixture.QueryRepository.ValidReportVersionIds.Remove(SecondReportVersionId);

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Contains("有效时长", error.Message, StringComparison.Ordinal);
    Assert.Contains("报告版本", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V53：已采纳项目全部因绑定报告版本失效而不可返回时，只说明版本失效这一原因，不混入超期表述。
  /// </summary>
  /// <remarks>
  /// 与超期和失效并存的情形区分开：本用例的两条采纳记录都在有效期内，唯一不可返回的原因是绑定版本不再是当前有效版本，
  /// 因此拒绝文案与 SRS F06 备选流 2 的"全部相关项目均失效时拒绝返回"一致，不出现有效时长的表述。
  /// </remarks>
  [Fact]
  public async Task All_adopted_items_with_invalidated_versions_report_the_version_reason_alone()
  {
    const int validHours = 6;
    CitationFixture fixture = CreateFixture(validHours);
    // 两条采纳记录都在有效期内：没有任何项目因超期不可返回。
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId, SecondMatchItemId, SecondReportId, SecondReportVersionId, SecondProjectCode, decisionSavedTime: DecisionSavedTime));
    // 报告已形成后续版本：两条采纳记录绑定的版本都不再是当前有效版本。
    fixture.QueryRepository.ValidReportVersionIds.Clear();

    (RecordingEventQueue events, InvalidOperationException error) = await ExecuteExpectingRejectionAsync(fixture, BuildRequest());

    Assert.Equal("业务拒绝：该就诊的互认采纳记录绑定的报告版本均已失效，无可返回的引用内容。", error.Message);
    Assert.DoesNotContain("有效时长", error.Message, StringComparison.Ordinal);
    AssertNothingWritten(fixture, events);
  }

  /// <summary>
  /// V55：决定为不采纳的项目不返回。
  /// </summary>
  /// <remarks>
  /// 同一报告版本上的两个项目，一个采纳、一个不采纳：只返回采纳的那一项。
  /// </remarks>
  [Fact]
  public async Task Non_adopted_items_are_not_returned()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, SecondMatchItemId, FirstReportId, FirstReportVersionId, SecondProjectCode,
        decisionSavedTime: DecisionSavedTime, recognitionResult: RecognitionResult.NotAdopted));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal(FirstProjectCode, Assert.Single(detail.MatchItems).StandardProjectCode);
  }

  /// <summary>
  /// V56：自然超过可互认时间但仍在引用详情有效期内的已采纳项目正常返回。
  /// </summary>
  /// <remarks>
  /// 报告时间取三十天前，远超匹配查询使用的可互认时间；本链路不读取互认配置，也不按可互认时间判定。
  /// </remarks>
  [Fact]
  public async Task Naturally_expired_recognition_time_does_not_block_citation_detail()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime, reportTime: ReportTime.AddDays(-30), clinicalTime: ClinicalTime.AddDays(-30)));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal(FirstMatchItemId, Assert.Single(detail.MatchItems).RecognitionMatchItemId);
    // 引用详情链不读取互认配置：可互认时间只约束新的匹配查询。
    Assert.Equal(0, fixture.QueryRepository.ConfigurationQueryCalls);
    // 来源名称读取次数仍只随不同组织与医院数量增长。
    Assert.Equal(1, fixture.OrganizationAppService.OrganizationReadCount);
    Assert.Equal(1, fixture.OrganizationAppService.HospitalReadCount);
    Assert.Equal(1, fixture.OrganizationAppService.BranchReadCount);
  }

  /// <summary>
  /// V57：同一报告版本与同一互认项目存在多条采纳时只返回保存时间最近的一条，匹配项标识升序兜底。
  /// </summary>
  /// <remarks>
  /// 两条采纳绑定同一报告版本与同一标准项目编码：保存时间较晚的一条胜出。
  /// 保存时间相同时按匹配项标识升序取第一条，因此期望值在用例侧按同一条排序口径复算。
  /// </remarks>
  [Fact]
  public async Task Multiple_adoptions_of_the_same_version_and_project_return_only_the_latest_saved()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime.AddHours(-2)));
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId, SecondMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime.AddHours(-1)));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    RecognitionCitationItemReadModel item = Assert.Single(detail.MatchItems);
    Assert.Equal(SecondMatchItemId, item.RecognitionMatchItemId);
    // 同一报告版本只返回一次报告公共上下文。
    Assert.Single(detail.ReportContext);
  }

  /// <summary>
  /// V57 的兜底键面：保存时间相同时按匹配项标识升序取第一条。
  /// </summary>
  [Fact]
  public async Task Same_saved_time_is_broken_by_match_item_id_ascending()
  {
    CitationFixture fixture = CreateFixture();
    DateTime sameSavedTime = DecisionSavedTime.AddHours(-1);
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, SecondMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: sameSavedTime));
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: sameSavedTime));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Guid expectedMatchItemId = new[] { FirstMatchItemId, SecondMatchItemId }.Order().First();
    Assert.Equal(expectedMatchItemId, Assert.Single(detail.MatchItems).RecognitionMatchItemId);
  }

  /// <summary>
  /// V58：同次就诊中部分组超期或失效时返回有效部分，不因其他项目失效整次失败。
  /// </summary>
  /// <remarks>
  /// 两组各有自己的处理结果保存时间：一组在有效期内、一组已超期；两条记录的报告版本各自有效。
  /// </remarks>
  [Fact]
  public async Task Partially_available_groups_return_the_available_items()
  {
    const int validHours = 6;
    CitationFixture fixture = CreateFixture(validHours);
    SeedCitation(
      fixture,
      BuildSeed(
        FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode,
        decisionSavedTime: DecisionSavedTime));
    // 同次就诊的第二个组：该组处理结果保存时间已超出引用详情有效时长。
    SeedCitation(
      fixture,
      BuildSeed(
        SecondRecordId, SecondMatchItemId, SecondReportId, SecondReportVersionId, SecondProjectCode,
        decisionSavedTime: DecisionSavedTime.AddHours(-validHours).AddSeconds(-1)));

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    RecognitionCitationItemReadModel item = Assert.Single(detail.MatchItems);
    Assert.Equal(FirstMatchItemId, item.RecognitionMatchItemId);
    Assert.Equal(FirstProjectCode, item.StandardProjectCode);
    Assert.Equal(FirstReportVersionId, Assert.Single(detail.ReportContext).ReportVersionId);
  }

  /// <summary>
  /// V58b：已经提交引用结果的采纳项目在引用详情有效期内仍可获取详情。
  /// </summary>
  /// <remarks>
  /// 前置为该匹配项已存在一条引用事实；引用详情是只读查询，不因已提交引用结果而提前关闭有效期。
  /// </remarks>
  [Fact]
  public async Task Item_with_submitted_reference_result_is_still_available_within_the_validity_window()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    fixture.ReferenceFacts[FirstMatchItemId] = new RecognitionReference
    {
      Id = Guid.NewGuid(),
      RecognitionMatchItemId = FirstMatchItemId,
      ReferencedTime = DecisionSavedTime.AddMinutes(30),
      ReferenceDeptId = "DEPT-01",
      ReferenceDeptName = "心内科",
      ReferenceDoctorId = "DOC-01",
      ReferenceDoctorName = "李医生",
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };

    RecognitionCitationDetailReadModel detail = await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal(FirstMatchItemId, Assert.Single(detail.MatchItems).RecognitionMatchItemId);
    // 已经提交引用结果不提前关闭有效期，也不产生任何写入与事件。
    Assert.Single(fixture.ReferenceFacts);
  }

  /// <summary>
  /// V88 的查询链面：引用详情链不产生任何写入、事件或状态变化。
  /// </summary>
  /// <remarks>
  /// 成功路径与失败路径各执行一次，两次都不得产生匹配记录、匹配项、处理结果、引用事实或任何领域事件；
  /// 处理结果保存时间与匹配生成时间同样保持不变。请求校验先于任何读取：参数与候选读取次数都为零。
  /// </remarks>
  [Fact]
  public async Task Citation_detail_chain_produces_no_write_event_or_state_change()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));
    Dictionary<Guid, RecognitionMatchRecord> recordsBefore = new(fixture.MatchRecords);
    Dictionary<Guid, RecognitionProcessingResult> resultsBefore = new(fixture.ProcessingResults);
    Dictionary<Guid, RecognitionMatchItem> matchItemsBefore = new(fixture.MatchItems);

    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      // 成功路径一次。
      await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

      // 失败路径一次：请求校验失败发生在任何读取与写入之前。
      await Assert.ThrowsAsync<ValidationException>(() => fixture.AppService.QueryRecognitionCitationDetailAsync(
        new RecognitionCitationDetailRequest
        {
          IdentityDocumentTypeCode = string.Empty,
          IdentityDocumentNo = IdentityDocumentNo,
          VisitType = VisitType.Outpatient,
          VisitSerialNo = VisitSerialNo
        }));

      Assert.Empty(events.Events);
      Assert.Equal(recordsBefore, fixture.MatchRecords);
      Assert.Equal(resultsBefore, fixture.ProcessingResults);
      Assert.Equal(matchItemsBefore, fixture.MatchItems);
      Assert.Empty(fixture.ReferenceFacts);
      Assert.Equal(1, fixture.QueryRepository.CitationQueryCalls);
      Assert.Equal(1, fixture.QueryRepository.CitationContextQueryCalls);
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>
  /// V90 的读取维度面：引用详情有效时长按平台级编码读取，读取请求不携带组织、医院与院区维度。
  /// </summary>
  [Fact]
  public async Task Validity_parameter_is_read_by_platform_level_code_only()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    await fixture.AppService.QueryRecognitionCitationDetailAsync(BuildRequest());

    Assert.Equal([CitationDetailValidHours], fixture.SystemParameterAppService.ReadCodes);
    Assert.Null(fixture.SystemParameterAppService.LastRequest!.GetType().GetProperty("OrgId"));
    Assert.Null(fixture.SystemParameterAppService.LastRequest.GetType().GetProperty("HosId"));
    Assert.Null(fixture.SystemParameterAppService.LastRequest.GetType().GetProperty("BranchId"));
  }

  /// <summary>
  /// 请求校验先于任何读取与写入：证件类型为空时既不读取参数，也不读取候选记录，更不产生任何写入与事件。
  /// </summary>
  [Fact]
  public async Task Request_validation_precedes_parameter_and_candidate_reads()
  {
    CitationFixture fixture = CreateFixture();
    SeedCitation(
      fixture,
      BuildSeed(FirstRecordId, FirstMatchItemId, FirstReportId, FirstReportVersionId, FirstProjectCode, decisionSavedTime: DecisionSavedTime));

    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      await Assert.ThrowsAsync<ValidationException>(() => fixture.AppService.QueryRecognitionCitationDetailAsync(
        new RecognitionCitationDetailRequest
        {
          IdentityDocumentTypeCode = "   ",
          IdentityDocumentNo = IdentityDocumentNo,
          VisitType = VisitType.Outpatient,
          VisitSerialNo = VisitSerialNo
        }));

      Assert.Equal(0, fixture.SystemParameterAppService.ReadCount);
      Assert.Equal(0, fixture.QueryRepository.CitationQueryCalls);
      Assert.Empty(events.Events);
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>本次请求接收时点的固定参照；相对当前时点构造，使超过有效期的用例不依赖执行耗时。</summary>
  private static DateTime DecisionSavedTime => DateTime.Now.AddHours(-1);

  /// <summary>报告公共上下文中的检查或检验时间。</summary>
  private static DateTime ClinicalTime => DateTime.Now.AddHours(-3);

  /// <summary>报告公共上下文中的报告时间。</summary>
  private static DateTime ReportTime => DateTime.Now.AddHours(-2);

  /// <summary>
  /// 本次用例的接收时点参照；在用例内取一次后派生出全部时间取值。
  /// </summary>
  /// <remarks>
  /// 属性形式的取值每次读取都会重新取当前时间，断言两侧因此会相差若干时钟刻度；
  /// 因此需要逐字比较时间的用例改用本方法取一次参照，再由减法派生全部时间。
  /// </remarks>
  /// <returns>本次用例的接收时点参照。</returns>
  private static DateTime NowReference() => DateTime.Now;

  /// <summary>来源报告明细上的项目名称。</summary>
  private const string SourceResultName = "白细胞计数";

  /// <summary>
  /// 引用详情查询用例的全部测试替身与入口。
  /// </summary>
  private sealed class CitationFixture
  {
    /// <summary>查询侧只读投影替身：提供引用详情候选、报告公共上下文与报告版本有效性。</summary>
    public FakeMatchQueryRepository QueryRepository { get; init; } = new();
    /// <summary>外部组织服务替身，按层级记录组织、医院与院区读取次数。</summary>
    public StubOrganizationAppService OrganizationAppService { get; init; } = new();
    /// <summary>系统参数服务共享替身，按编码预置返回值并记录读取过的编码。</summary>
    public StubSystemParameterAppService SystemParameterAppService { get; init; } = new();
    /// <summary>匹配记录集合；本链路只读，用例用它断言调用前后完全一致。</summary>
    public Dictionary<Guid, RecognitionMatchRecord> MatchRecords { get; } = [];
    /// <summary>匹配项集合；本链路只读，用例用它断言调用前后完全一致。</summary>
    public Dictionary<Guid, RecognitionMatchItem> MatchItems { get; } = [];
    /// <summary>处理结果集合；本链路只读，用例用它断言调用前后完全一致。</summary>
    public Dictionary<Guid, RecognitionProcessingResult> ProcessingResults { get; } = [];
    /// <summary>引用事实集合；本链路只读，用例用它断言调用前后完全一致。</summary>
    public Dictionary<Guid, RecognitionReference> ReferenceFacts { get; } = [];
    /// <summary>引用详情查询入口。</summary>
    public MedicalRecognitionReportQueryAppService AppService { get; init; } = null!;
  }

  /// <summary>
  /// 构建一次引用详情用例的替身集合与入口，并预置三层齐全的可信请求上下文。
  /// </summary>
  /// <param name="validHours">引用详情有效时长参数取值；为零表示取用例默认的六小时。</param>
  /// <param name="validHoursValue">参数服务返回的取值文本；非空时按该文本预置，用于构造非法取值前置。</param>
  /// <param name="parameterMissing">为真时有效时长参数不预置，用于构造"参数不存在"前置。</param>
  /// <param name="parameterServiceFailure">非空时表示参数服务读取失败。</param>
  /// <returns>可直接调用的引用详情查询用例夹具。</returns>
  private static CitationFixture CreateFixture(
    int validHours = 0,
    string? validHoursValue = null,
    bool parameterMissing = false,
    Exception? parameterServiceFailure = null)
  {
    StubSystemParameterAppService systemParameterAppService = new() { Failure = parameterServiceFailure };
    if (validHoursValue is not null) systemParameterAppService.ValuesByCode[CitationDetailValidHours] = validHoursValue;
    else if (!parameterMissing) systemParameterAppService.ValuesByCode[CitationDetailValidHours] = (validHours == 0 ? 6 : validHours).ToString();

    // 来源侧涉及两家医院：名称回填按不同组织与医院批量读取，读取次数由替身按层级记录。
    StubOrganizationAppService organizationAppService = new()
    {
      Organizations = [new OrganizationDto { Id = TrustedOrganization, Name = "示范组织-ORG-A", IsValid = true }],
      HospitalsByOrganization =
      {
        [TrustedOrganization] =
        [
          new HospitalDto { Id = TrustedHospital, Name = "示范医院-HOS-A", OrgId = TrustedOrganization, IsValid = true },
          new HospitalDto { Id = SecondHospital, Name = "示范医院-HOS-B", OrgId = TrustedOrganization, IsValid = true }
        ]
      },
      BranchesByHospital =
      {
        [TrustedHospital] = [new BranchDto { Id = TrustedBranch, Name = "示范院区-BRH-A", HosId = TrustedHospital, OrgId = TrustedOrganization, IsValid = true }],
        [SecondHospital] = [new BranchDto { Id = SecondBranch, Name = "示范院区-BRH-B", HosId = SecondHospital, OrgId = TrustedOrganization, IsValid = true }]
      }
    };

    // 令牌三层齐全：可信范围解析不读取用户档案，也不向组织服务核对归属。
    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());

    // 引用详情链只使用查询侧的只读投影：应用服务与用例夹具共用同一个替身，用例登记的数据即入口读到的数据。
    FakeMatchQueryRepository queryRepository = new();

    return new CitationFixture
    {
      QueryRepository = queryRepository,
      OrganizationAppService = organizationAppService,
      SystemParameterAppService = systemParameterAppService,
      AppService = new MedicalRecognitionReportQueryAppService(
        queryRepository,
        organizationAppService,
        new StubUserAppService(),
        systemParameterAppService)
    };
  }

  /// <summary>构建一次引用详情请求；证件类型、号码、就诊类型与流水号取用例固定取值。</summary>
  /// <returns>字段齐全的引用详情请求。</returns>
  private static RecognitionCitationDetailRequest BuildRequest() => new()
  {
    IdentityDocumentTypeCode = IdentityDocumentTypeCode,
    IdentityDocumentNo = IdentityDocumentNo,
    VisitType = VisitType.Outpatient,
    VisitSerialNo = VisitSerialNo
  };

  /// <summary>引用详情用例的种子：一条已保存处理结果的匹配项与它绑定的报告。</summary>
  /// <param name="MatchRecordId">所属互认匹配记录标识。</param>
  /// <param name="MatchItemId">互认匹配项标识。</param>
  /// <param name="ReportId">报告标识。</param>
  /// <param name="ReportVersionId">报告版本标识。</param>
  /// <param name="StandardProjectCode">标准项目编码。</param>
  /// <param name="DecisionSavedTime">处理结果保存时间；引用详情有效期的起点。</param>
  /// <param name="RecognitionResult">该匹配项的处理结果。</param>
  /// <param name="ReportType">报告类型。</param>
  /// <param name="ReportNo">来源报告单号。</param>
  /// <param name="ReportName">来源报告名称。</param>
  /// <param name="SourceHospitalCode">来源医院编码。</param>
  /// <param name="SourceBranchCode">来源院区编码。</param>
  /// <param name="ClinicalTime">检查或检验时间。</param>
  /// <param name="ReportTime">报告时间。</param>
  /// <param name="PdfFileId">PDF 文件标识。</param>
  /// <param name="PdfFileName">PDF 原始文件名。</param>
  /// <param name="LaboratoryResultNameForProjectCode">检验结果明细的项目名称；为空时由标准项目编码派生。</param>
  /// <param name="ExaminationSiteNames">检查部位名称集合；为空表示该报告没有检查部位。</param>
  /// <param name="Findings">检查所见。</param>
  /// <param name="Conclusion">检查结论。</param>
  /// <param name="SourceImageStatus">来源影像状态。</param>
  /// <param name="ImageAccessUrl">影像调阅地址。</param>
  private sealed record CitationSeed(
    Guid MatchRecordId,
    Guid MatchItemId,
    Guid ReportId,
    Guid ReportVersionId,
    string StandardProjectCode,
    DateTime DecisionSavedTime,
    RecognitionResult RecognitionResult,
    MedicalReportType ReportType,
    string ReportNo,
    string ReportName,
    string SourceHospitalCode,
    string SourceBranchCode,
    DateTime ClinicalTime,
    DateTime ReportTime,
    string PdfFileId,
    string PdfFileName,
    string? LaboratoryResultNameForProjectCode,
    IReadOnlyList<string> ExaminationSiteNames,
    string? Findings,
    string? Conclusion,
    SourceImageStatus? SourceImageStatus,
    string? ImageAccessUrl);

  /// <summary>构建一条引用详情用例种子；未指定的字段取用例默认取值。</summary>
  /// <param name="matchRecordId">所属互认匹配记录标识。</param>
  /// <param name="matchItemId">互认匹配项标识。</param>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="decisionSavedTime">处理结果保存时间。</param>
  /// <param name="recognitionResult">该匹配项的处理结果；默认采纳。</param>
  /// <param name="reportType">报告类型；默认检验报告，用于构造检查侧的引用详情时需要显式指定。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="reportName">来源报告名称。</param>
  /// <param name="sourceHospitalCode">来源医院编码。</param>
  /// <param name="sourceBranchCode">来源院区编码。</param>
  /// <param name="clinicalTime">检查或检验时间。</param>
  /// <param name="reportTime">报告时间。</param>
  /// <param name="pdfFileId">PDF 文件标识。</param>
  /// <param name="pdfFileName">PDF 原始文件名。</param>
  /// <param name="laboratoryResultNameForProjectCode">检验结果明细的项目名称。</param>
  /// <param name="examinationSiteNames">检查部位名称集合。</param>
  /// <param name="findings">检查所见。</param>
  /// <param name="conclusion">检查结论。</param>
  /// <param name="sourceImageStatus">来源影像状态。</param>
  /// <param name="imageAccessUrl">影像调阅地址。</param>
  /// <returns>一条引用详情用例种子。</returns>
  private static CitationSeed BuildSeed(
    Guid matchRecordId,
    Guid matchItemId,
    Guid reportId,
    Guid reportVersionId,
    string standardProjectCode,
    DateTime decisionSavedTime,
    RecognitionResult recognitionResult = RecognitionResult.Adopted,
    MedicalReportType reportType = MedicalReportType.Laboratory,
    string reportNo = "LAB-2026-0001",
    string reportName = "血常规报告",
    string sourceHospitalCode = TrustedHospital,
    string sourceBranchCode = TrustedBranch,
    DateTime? clinicalTime = null,
    DateTime? reportTime = null,
    string pdfFileId = "pdf-key",
    string pdfFileName = "报告.pdf",
    string? laboratoryResultNameForProjectCode = null,
    IReadOnlyList<string>? examinationSiteNames = null,
    string? findings = null,
    string? conclusion = null,
    SourceImageStatus? sourceImageStatus = null,
    string? imageAccessUrl = null) =>
    new(
      matchRecordId,
      matchItemId,
      reportId,
      reportVersionId,
      standardProjectCode,
      decisionSavedTime,
      recognitionResult,
      reportType,
      reportNo,
      reportName,
      sourceHospitalCode,
      sourceBranchCode,
      clinicalTime ?? ClinicalTime,
      reportTime ?? ReportTime,
      pdfFileId,
      pdfFileName,
      laboratoryResultNameForProjectCode,
      examinationSiteNames ?? [],
      findings,
      conclusion,
      sourceImageStatus,
      imageAccessUrl);

  /// <summary>
  /// 把一条种子登记进夹具：匹配记录、匹配项、处理结果、报告版本有效性与报告公共上下文。
  /// </summary>
  /// <remarks>
  /// 登记面只表达数据库里已存在的读取素材：本链路不写入任何一张表，因此种子只进入查询投影替身，
  /// 匹配记录、匹配项与处理结果同时登记在夹具的只读集合中，供用例断言调用前后完全一致。  /// </remarks>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="seed">本次登记的一条引用详情素材。</param>
  private static void SeedCitation(CitationFixture fixture, CitationSeed seed)
  {
    if (!fixture.MatchRecords.ContainsKey(seed.MatchRecordId))
    {
      fixture.MatchRecords[seed.MatchRecordId] = new RecognitionMatchRecord
      {
        Id = seed.MatchRecordId,
        ReceiverOrganizationCode = TrustedOrganization,
        ReceiverHospitalCode = TrustedHospital,
        ReceiverBranchCode = TrustedBranch,
        IdentityDocumentTypeCode = IdentityDocumentTypeCode,
        IdentityDocumentNo = IdentityDocumentNo,
        VisitType = VisitType.Outpatient,
        VisitSerialNo = VisitSerialNo,
        MatchCreatedTime = ReportTime,
        DecisionSavedTime = seed.DecisionSavedTime,
        OperId = TrustedOperId,
        OperTime = DateTimeOffset.UtcNow
      };
    }

    fixture.MatchItems[seed.MatchItemId] = new RecognitionMatchItem
    {
      Id = seed.MatchItemId,
      RecognitionMatchRecordId = seed.MatchRecordId,
      ItemType = seed.ReportType == MedicalReportType.Laboratory ? MedicalItemType.Laboratory : MedicalItemType.Examination,
      StandardProjectCode = seed.StandardProjectCode,
      ReportId = seed.ReportId,
      ReportVersionId = seed.ReportVersionId,
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };

    fixture.ProcessingResults[seed.MatchItemId] = new RecognitionProcessingResult
    {
      Id = Guid.NewGuid(),
      RecognitionMatchRecordId = seed.MatchRecordId,
      RecognitionMatchItemId = seed.MatchItemId,
      RecognitionTime = seed.DecisionSavedTime,
      RecognitionResult = seed.RecognitionResult,
      RecognitionDeptId = "DEPT-01",
      RecognitionDeptName = "心内科",
      RecognitionDoctorId = "DOC-01",
      RecognitionDoctorName = "李医生",
      OperId = TrustedOperId,
      OperTime = DateTimeOffset.UtcNow
    };

    // 绑定的报告版本默认登记为当前有效版本；需要构造版本失效前置的用例在登记完成后再除名。
    fixture.QueryRepository.ValidReportVersionIds.Add(seed.ReportVersionId);

    // 标准项目名称取自标准目录，与来源报告明细上的来源项目名称是两个来源。
    fixture.QueryRepository.StandardProjectNamesByCode[seed.StandardProjectCode] = $"{seed.StandardProjectCode} 名称";

    fixture.QueryRepository.CitationCandidates.Add(new RecognitionCitationCandidateItem
    {
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      IdentityDocumentTypeCode = IdentityDocumentTypeCode,
      IdentityDocumentNo = IdentityDocumentNo,
      VisitType = VisitType.Outpatient,
      VisitSerialNo = VisitSerialNo,
      DecisionSavedTime = seed.DecisionSavedTime,
      RecognitionMatchRecordId = seed.MatchRecordId,
      RecognitionMatchItemId = seed.MatchItemId,
      RecognitionResult = seed.RecognitionResult,
      StandardProjectCode = seed.StandardProjectCode,
      ReportId = seed.ReportId,
      ReportVersionId = seed.ReportVersionId
    });

    fixture.QueryRepository.CitationReportContextsByVersion[seed.ReportVersionId] = new RecognitionCitationReportContextItem
    {
      ReportId = seed.ReportId,
      ReportVersionId = seed.ReportVersionId,
      ReportType = seed.ReportType,
      ReportNo = seed.ReportNo,
      ReportName = seed.ReportName,
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = seed.SourceHospitalCode,
      SourceBranchCode = seed.SourceBranchCode,
      ClinicalTime = seed.ClinicalTime,
      ReportTime = seed.ReportTime,
      SourceApplicantDoctorId = "DOC-APP",
      SourceApplicantDoctorName = "申请医生",
      SourceReviewerDoctorId = "DOC-REV",
      SourceReviewerDoctorName = "审核医生",
      PdfFileId = seed.PdfFileId,
      PdfFileName = seed.PdfFileName,
      ExaminationFindings = seed.Findings,
      ExaminationConclusion = seed.Conclusion,
      SourceImageStatus = seed.SourceImageStatus,
      ImageAccessUrl = seed.ImageAccessUrl
    };

    if (seed.ReportType == MedicalReportType.Laboratory)
    {
      fixture.QueryRepository.LaboratoryResultsByVersion[seed.ReportVersionId] =
      [
        new LaboratoryResultItemView
        {
          // 普通检验结果必须带上报告版本归属：应用层按本列把一次读回的结果归到对应的报告与项目下。
          ReportVersionId = seed.ReportVersionId,
          SourceProjectName = seed.LaboratoryResultNameForProjectCode ?? $"{seed.StandardProjectCode} 结果",
          StandardProjectCode = seed.StandardProjectCode,
          SourceResultText = "9.8",
          Unit = "10^9/L",
          ReferenceRange = "4.0-10.0",
          DisplayOrder = 1,
          AbnormalFlag = LaboratoryAbnormalFlag.High,
          CriticalValueFlag = false
        }
      ];

      return;
    }

    Guid examinationItemId = Guid.NewGuid();
    fixture.QueryRepository.ExaminationItemsByVersion[seed.ReportVersionId] =
    [
      new ExaminationItemView
      {
        ReportVersionId = seed.ReportVersionId,
        ItemId = examinationItemId,
        SourceProjectName = $"{seed.StandardProjectCode} 检查项目",
        StandardProjectCode = seed.StandardProjectCode
      }
    ];
    fixture.QueryRepository.ExaminationSitesByItem[examinationItemId] =
    [
      .. seed.ExaminationSiteNames.Select((siteName, index) => new ExaminationSiteView
      {
        ExaminationItemId = examinationItemId,
        SiteName = siteName,
        SourceSiteCode = $"SITE-{index + 1}"
      })
    ];
  }

  /// <summary>在捕获事件的前提下执行一次引用详情请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">引用详情请求。</param>
  /// <returns>捕获到的事件集合与引用详情。</returns>
  private static async Task<(RecordingEventQueue Events, RecognitionCitationDetailReadModel Detail)> ExecuteAsync(
    CitationFixture fixture, RecognitionCitationDetailRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await fixture.AppService.QueryRecognitionCitationDetailAsync(request));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>在捕获事件的前提下执行一次预期被业务拒绝的引用详情请求。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="request">引用详情请求。</param>
  /// <returns>捕获到的事件集合与抛出的业务拒绝异常。</returns>
  private static async Task<(RecordingEventQueue Events, InvalidOperationException Error)> ExecuteExpectingRejectionAsync(
    CitationFixture fixture, RecognitionCitationDetailRequest request)
  {
    RecordingEventQueue events = InstallRecordingEventQueue();
    try
    {
      return (events, await Assert.ThrowsAsync<InvalidOperationException>(
        () => fixture.AppService.QueryRecognitionCitationDetailAsync(request)));
    }
    finally
    {
      UninstallRecordingEventQueue();
    }
  }

  /// <summary>断言一次失败的引用详情请求没有留下任何业务写入与事件。</summary>
  /// <param name="fixture">用例夹具。</param>
  /// <param name="events">捕获到的事件集合。</param>
  private static void AssertNothingWritten(CitationFixture fixture, RecordingEventQueue events)
  {
    Assert.Empty(events.Events);
    Assert.Empty(fixture.ReferenceFacts);
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
  /// 记录领域层登记事件的测试替身，用于断言引用详情链不登记任何事件。
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
