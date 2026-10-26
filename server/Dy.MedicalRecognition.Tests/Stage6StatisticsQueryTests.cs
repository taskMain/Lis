using ClosedXML.Excel;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 6 使用方与提供方汇总、明细及匹配记录集合视图的应用层行为：分页窗口先于仓储访问、
/// 平台版两组范围解析与一致性拒绝、本院版可信注入与院区为空分支、汇总行结构、同期互认率与原因占比的行内计算、
/// 名称批量回填、明细四类型业务时间与未反馈语义、明细筛选与分页、汇总数字与明细计数的一致性，
/// 以及提供方维度子集拒绝、提供方双侧名称回填与匹配记录集合视图的单记录读取、已处理记录的记录级字段、
/// 名称回填读取次数与未知标识拒绝。
/// </summary>
/// <remarks>
/// 覆盖阶段 6 验证矩阵（docs/plans/008-阶段6-互认统计与导出/Server/design.md「验证矩阵」章）
/// V11 至 V33 与 V39 在应用层与查询组装层的替身取证部分：使用统计查询仓储替身、组织服务替身与可信请求上下文替身
/// 隔离数据库与外部服务，验证可观察结果、下推条件、调用次数与拒绝时机。
/// 真实数据库上的分组聚合、排序跨页与汇总明细对账由票 09 的真实库批次负责。
/// </remarks>
public sealed class Stage6StatisticsQueryTests
{
  /// <summary>可信组织编码。</summary>
  private const string TrustedOrganization = FakeIamScope.OrganizationCode;

  /// <summary>可信医院编码。</summary>
  private const string TrustedHospital = FakeIamScope.HospitalCode;

  /// <summary>可信院区编码。</summary>
  private const string TrustedBranch = FakeIamScope.BranchCode;

  /// <summary>同组织下的第二家医院编码。</summary>
  private const string SecondHospital = FakeIamScope.SecondHospitalCode;

  /// <summary>提供方组织编码；平台版用例的来源组取值。</summary>
  private const string SourceOrganization = FakeIamScope.OtherOrganizationCode;

  /// <summary>提供方医院编码。</summary>
  private const string SourceHospital = FakeIamScope.OtherHospitalCode;

  /// <summary>提供方院区编码。</summary>
  private const string SourceBranch = FakeIamScope.OtherBranchCode;

  /// <summary>同一医共体下第二家医院的院区编码。</summary>
  private const string SecondBranch = FakeIamScope.SecondBranchCode;

  /// <summary>组织名称前缀；替身按编码拼出唯一名称，用于核对回填来源。</summary>
  private const string OrganizationNamePrefix = "示范组织";

  /// <summary>医院名称前缀。</summary>
  private const string HospitalNamePrefix = "示范医院";

  /// <summary>院区名称前缀。</summary>
  private const string BranchNamePrefix = "示范院区";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>统计事实的业务时间基准；全部明细行的业务时间都按它偏移。</summary>
  private static readonly DateTime BusinessDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Unspecified);

  /// <summary>用例通用的统计开始日期。</summary>
  private static readonly DateOnly PeriodFrom = new(2026, 9, 1);

  /// <summary>用例通用的统计结束日期。</summary>
  private static readonly DateOnly PeriodTo = new(2026, 9, 30);

  // ---------- 平台版汇总：V11、V12、V14、V18、V19、V21、V22、V24 ----------

  /// <summary>
  /// V11：非法分页窗口在第一次仓储访问之前按业务拒绝；请求校验先拦截取值域越界的窗口，
  /// 计数、取页与原因方法一次都没有被调用。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(-1, 20, "页码不能小于 1")]
  [InlineData(1, 0, "页容量必须在 1 到 200 之间")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Usage_summary_rejects_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(pageIndex, pageSize)));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V12：接收组与来源组的组织、医院、院区按请求使用并经组织路径解析校验后，以去空白编码下推为筛选条件；
  /// 两组组织编码一致；时间范围按本地日换算为起始日零点（含）与结束日次日零点（不含）。
  /// </summary>
  [Fact]
  public async Task Usage_summary_resolves_requested_scope_groups_into_the_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      OrganizationCode = $" {TrustedOrganization} ",
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = FakeIamScope.FourthHospitalCode,
      SourceBranchCode = FakeIamScope.FourthBranchCode,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Equal(TrustedBranch, filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FourthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(FakeIamScope.FourthBranchCode, filter.SourceBranchCode);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodStart);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodEnd);
    Assert.Equal(DateTimeKind.Unspecified, filter.PeriodStart.Kind);
  }

  /// <summary>
  /// V12：来源组与接收组组织编码同时提供且不一致时按业务拒绝，且不读取任何仓储数据。
  /// </summary>
  [Fact]
  public async Task Usage_summary_rejects_mismatched_receiver_and_source_organizations()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        OrganizationCode = TrustedOrganization,
        SourceOrganizationCode = SourceOrganization,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Contains("不一致", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V12：任一范围条件为空不附加该层过滤，全部为空即按授权全量；此时不发起组织服务读取。
  /// </summary>
  [Fact]
  public async Task Usage_summary_leaves_empty_scope_conditions_unfiltered_without_organization_reads()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 20));

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Null(filter.OrganizationCode);
    Assert.Null(filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Null(filter.SourceOrganizationCode);
    Assert.Null(filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(0, organizationService.OrganizationReadCount);
  }

  /// <summary>
  /// V12：仅提交组织、医院与院区为空时按层独立处理：只校验组织层，
  /// 筛选只附加组织条件，医院与院区条件保持空值，医院层与院区层读取一次都没有发生。
  /// </summary>
  [Fact]
  public async Task Usage_summary_resolves_organization_only_scope_without_deeper_layer_filters()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      OrganizationCode = TrustedOrganization,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(string.Empty, filter.HospitalCode);
    Assert.Equal(string.Empty, filter.BranchCode);
    Assert.Null(filter.SourceOrganizationCode);
    Assert.Null(filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V12：提交组织加医院、院区为空时按层独立处理：校验组织与医院两层、不进入院区层，
  /// 筛选附加组织与医院条件、院区条件保持空值。
  /// </summary>
  [Fact]
  public async Task Usage_summary_resolves_organization_and_hospital_scope_without_branch_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      OrganizationCode = TrustedOrganization,
      HospitalCode = SecondHospital,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(SecondHospital, filter.HospitalCode);
    Assert.Equal(string.Empty, filter.BranchCode);
    Assert.Null(filter.SourceOrganizationCode);
    Assert.Null(filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V12、V45：提供的范围取值经组织路径解析校验，医院不属于所选组织时整次拒绝，不返回部分结果。
  /// </summary>
  [Fact]
  public async Task Usage_summary_rejects_scope_outside_the_organization_tree()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      }));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        OrganizationCode = SourceOrganization,
        HospitalCode = TrustedHospital,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("业务拒绝：医院不存在、已停用或不属于所选组织。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V14：每行为所选维度的一个分组值，未参与分组的维度字段为空，无总计行；
  /// 汇总维度与请求一致，读模型统计期间按本地日闭区间解释。
  /// </summary>
  [Fact]
  public async Task Usage_summary_keeps_dimension_row_shape_without_total_rows()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
    [
      BuildSummaryRow(TrustedHospital, reminder: 4, adoption: 3),
      BuildSummaryRow(SecondHospital, reminder: 1)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] }));

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    Assert.Equal(2, result.Items.Count);
    foreach (RecognitionUsageSummaryReadModel row in result.Items)
    {
      Assert.Equal(RecognitionStatisticsGroupDimension.Hospital, row.GroupDimension);
      Assert.Null(row.ReceiverBranchCode);
      Assert.Null(row.ReceiverBranchName);
      Assert.Null(row.RecognitionDeptId);
      Assert.Null(row.RecognitionDeptName);
      Assert.Null(row.ItemType);
      Assert.Null(row.StandardProjectCode);
      Assert.Null(row.StandardProjectName);
    }

    Assert.Equal(TrustedHospital, result.Items[0].ReceiverHospitalCode);
    Assert.Equal(4, result.Items[0].ReminderCount);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), result.Items[0].PeriodStart);
    Assert.Equal(new DateOnly(2026, 9, 30).ToDateTime(TimeOnly.MaxValue), result.Items[0].PeriodEnd);
  }

  /// <summary>
  /// V18：同期互认率在应用层按行内采纳次数除以提醒次数计算，提醒为零时标记未计算、数值无业务含义，
  /// 短周期率可超过百分之百按原始值展示。
  /// </summary>
  [Fact]
  public async Task Usage_summary_computes_same_period_rate_inline_and_marks_zero_reminder_uncalculated()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
    [
      BuildSummaryRow(TrustedHospital, reminder: 4, adoption: 3),
      BuildSummaryRow(SecondHospital, reminder: 0, adoption: 2),
      BuildSummaryRow(FakeIamScope.FourthHospitalCode, reminder: 2, adoption: 3)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode] }));

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 20));

    Assert.Equal(0.75m, result.Items[0].SamePeriodRecognitionRate);
    Assert.True(result.Items[0].SamePeriodRecognitionRateCalculated);

    Assert.Equal(0m, result.Items[1].SamePeriodRecognitionRate);
    Assert.False(result.Items[1].SamePeriodRecognitionRateCalculated);

    Assert.Equal(1.5m, result.Items[2].SamePeriodRecognitionRate);
    Assert.True(result.Items[2].SamePeriodRecognitionRateCalculated);
  }

  /// <summary>
  /// V19：原因汇总语句按分组键加原因代码返回全部原因行，应用层只把当页分组键所属的原因行装配进汇总行，
  /// 占比分母为行内不采纳次数，行内不采纳为零时无原因可装配；其他原因按平台统一代码归集展示。
  /// </summary>
  [Fact]
  public async Task Usage_summary_assembles_reason_rows_into_their_group_rows_with_inline_ratio()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
    [
      BuildSummaryRow(TrustedHospital, reminder: 4, adoption: 2, nonAdoption: 2),
      BuildSummaryRow(SecondHospital, reminder: 1)
    ]);
    repository.ReasonItems.AddRange(
    [
      BuildReasonRow(TrustedHospital, RecognitionNonAdoptionReason.CurrentConditionMismatch, 1),
      BuildReasonRow(TrustedHospital, RecognitionNonAdoptionReason.OtherReviewRequired, 1),
      BuildReasonRow(FakeIamScope.FourthHospitalCode, RecognitionNonAdoptionReason.OtherReviewRequired, 5)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] }));

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 20));

    Assert.Equal(2, result.Items[0].NonAdoptionReasons.Count);
    Assert.Equal("1", result.Items[0].NonAdoptionReasons[0].ReasonCode);
    Assert.Equal("病情变化致结果难以满足诊疗需求", result.Items[0].NonAdoptionReasons[0].ReasonName);
    Assert.Equal(1, result.Items[0].NonAdoptionReasons[0].Count);
    Assert.Equal(0.5m, result.Items[0].NonAdoptionReasons[0].Ratio);
    Assert.Equal("6", result.Items[0].NonAdoptionReasons[1].ReasonCode);
    Assert.Equal("其他情形确需复查", result.Items[0].NonAdoptionReasons[1].ReasonName);
    Assert.Equal(0.5m, result.Items[0].NonAdoptionReasons[1].Ratio);

    // 行内不采纳为零的分组没有原因可装配；不属于当页分组键的原因行不进入任何汇总行。
    Assert.Empty(result.Items[1].NonAdoptionReasons);
  }

  /// <summary>
  /// V24：组织、医院与院区名称按当页涉及编码批量解析回填，调用次数不随行数增长；
  /// 名称取自外部组织服务的当前值。
  /// </summary>
  [Theory]
  [InlineData(10)]
  [InlineData(50)]
  public async Task Usage_summary_backfills_names_by_page_scope_without_growing_calls(int rowCount)
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
      [.. Enumerable.Range(1, rowCount).Select(index => BuildSummaryRow(TrustedHospital, reminder: index, branchCode: TrustedBranch))]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 200 }
      });

    Assert.Equal(rowCount, result.Items.Count);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
    Assert.All(result.Items, row =>
    {
      Assert.Equal("示范医共体", row.ReceiverOrganizationName);
      Assert.Equal("示范医院", row.ReceiverHospitalName);
      Assert.Equal("示范院区", row.ReceiverBranchName);
    });
  }

  /// <summary>
  /// V22：越界页返回空集合且保留真实总数；无命中时返回空页成功，不报业务异常。
  /// </summary>
  [Fact]
  public async Task Usage_summary_returns_empty_items_with_real_total_for_out_of_range_page()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
    [
      BuildSummaryRow(TrustedHospital, reminder: 1),
      BuildSummaryRow(SecondHospital, reminder: 1),
      BuildSummaryRow(FakeIamScope.FourthHospitalCode, reminder: 1)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode] }));

    PageResultDto<RecognitionUsageSummaryReadModel> lastPartialPage = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(2, 2));
    Assert.Single(lastPartialPage.Items);
    Assert.Equal(3, lastPartialPage.Page.TotalCount);

    PageResultDto<RecognitionUsageSummaryReadModel> beyondLastPage = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(9, 2));
    Assert.Empty(beyondLastPage.Items);
    Assert.Equal(3, beyondLastPage.Page.TotalCount);

    FakeStatisticsQueryRepository emptyRepository = new();
    PageResultDto<RecognitionUsageSummaryReadModel> emptyResult = await CreateService(emptyRepository, CreateOrganizationService())
      .QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 20));
    Assert.Empty(emptyResult.Items);
    Assert.Equal(0, emptyResult.Page.TotalCount);
  }

  /// <summary>
  /// V48：使用方汇总查询与汇总导出保持仓储给出的业务量顺序——首行采纳次数最多，
  /// 采纳次数相同的行按提醒次数降序，提醒次数也相同时按分组键升序；分页与全量导出不重排。
  /// </summary>
  /// <remarks>排序子句由 <see cref="Stage6SqlMapTests"/> 的语句断言负责，本用例冻结应用层与导出层对仓储行序的保持。</remarks>
  [Fact]
  public async Task Usage_summary_query_and_export_preserve_business_volume_order_and_group_key_tiebreak()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.AddRange(
    [
      BuildSummaryRow(TrustedHospital, reminder: 8, adoption: 5),
      BuildSummaryRow(SecondHospital, reminder: 7, adoption: 5),
      BuildSummaryRow(FakeIamScope.FourthHospitalCode, reminder: 9, adoption: 4),
      BuildSummaryRow(FakeIamScope.FifthHospitalCode, reminder: 6, adoption: 3),
      BuildSummaryRow(FakeIamScope.SixthHospitalCode, reminder: 1, adoption: 2)
    ]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode, FakeIamScope.FifthHospitalCode, FakeIamScope.SixthHospitalCode] });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<RecognitionUsageSummaryReadModel> firstPage = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 2));
    PageResultDto<RecognitionUsageSummaryReadModel> middlePage = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(2, 2));
    PageResultDto<RecognitionUsageSummaryReadModel> lastPage = await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(3, 2));
    string[] pageKeys =
    [
      .. firstPage.Items.Select(row => row.ReceiverHospitalCode!),
      .. middlePage.Items.Select(row => row.ReceiverHospitalCode!),
      .. lastPage.Items.Select(row => row.ReceiverHospitalCode!)
    ];

    Assert.Equal([TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode, FakeIamScope.FifthHospitalCode, FakeIamScope.SixthHospitalCode], pageKeys);
    Assert.Equal(5, pageKeys.Distinct(StringComparer.Ordinal).Count());
    Assert.Equal(5, firstPage.Items[0].AdoptionCount);
    Assert.Equal(8, firstPage.Items[0].ReminderCount);
    Assert.Equal(5, firstPage.Items[1].AdoptionCount);
    Assert.Equal(7, firstPage.Items[1].ReminderCount);

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionUsageSummary));
    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(6, worksheet.LastRowUsed()!.RowNumber());
    Assert.Equal(TrustedHospital, worksheet.Row(2).Cell(3).GetString());
    Assert.Equal(SecondHospital, worksheet.Row(3).Cell(3).GetString());
    Assert.Equal(FakeIamScope.FourthHospitalCode, worksheet.Row(4).Cell(3).GetString());
    Assert.Equal(FakeIamScope.FifthHospitalCode, worksheet.Row(5).Cell(3).GetString());
    Assert.Equal(FakeIamScope.SixthHospitalCode, worksheet.Row(6).Cell(3).GetString());
  }

  // ---------- 本院版汇总：V13 ----------

  /// <summary>
  /// V13：本院版使用方组织与使用方医院由可信上下文注入，请求不含本侧组织与医院字段；
  /// 院区为空时按可信医院全院范围统计，筛选不附加院区条件，也不进入院区层解析。
  /// </summary>
  [Fact]
  public async Task Branch_usage_summary_injects_trusted_scope_and_skips_branch_layer_when_branch_is_empty()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    FakeIamScope scope = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService, scope);

    await service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);

    // 院区为空的分支不进入院区层解析：当页为空、无回填写入时，院区读取一次都没有发生。
    // 可信范围来自 IAM 目录：三层由一次批量读取归类，不再按层分别调用外部组织服务，
    // 原先「组织 1 次 / 医院 1 次 / 院区 0 次」的分层计数判据随之退出；此处只断言目录确实被读取过，
    // 局部性（读取次数不随条目数增长）由按报告版本的批量回填用例承接。
    Assert.True(scope.BatchReadCount > 0, "可信范围应来自 IAM 目录读取。");
  }

  /// <summary>
  /// V13：本侧院区非空时经解析校验存在、启用且属于可信医院，并以解析后的编码附加院区过滤。
  /// </summary>
  [Fact]
  public async Task Branch_usage_summary_resolves_optional_branch_when_provided()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      BranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Equal(TrustedBranch, filter.BranchCode);
  }

  /// <summary>
  /// V13：本院版仅提交提供方医院、提供方院区为空时按层独立处理：提供方医院经可信组织校验存在、启用与父子归属，
  /// 筛选附加可信组织与提供方医院条件、提供方院区条件保持空值，院区层读取一次都没有发生。
  /// </summary>
  [Fact]
  public async Task Branch_usage_summary_resolves_source_hospital_only_without_source_branch_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceHospitalCode = FakeIamScope.FourthHospitalCode,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageSummaryFilter filter = repository.LastSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FourthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(string.Empty, filter.SourceBranchCode);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V11：本院版汇总入口的非法分页窗口同样在第一次仓储访问之前按业务拒绝；
  /// 可信范围解析与请求校验先于窗口校验，计数与取页一次都没有被调用。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Branch_usage_summary_rejects_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
        {
          StartTime = PeriodFrom,
          EndTime = PeriodTo,
          GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
          Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
        }));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V13：不属于可信医院的院区、不属于可信组织的提供方医院都按业务拒绝，不返回越界数据。
  /// </summary>
  [Fact]
  public async Task Branch_usage_summary_rejects_branch_or_source_hospital_outside_trusted_scope()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException branchError = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        BranchCode = "BRH-UNKNOWN",
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));
    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", branchError.Message);

    InvalidOperationException sourceError = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        SourceHospitalCode = "HOS-UNKNOWN",
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));
    Assert.Equal("业务拒绝：提供方医院不存在、已停用或不属于可信组织。", sourceError.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V13：可信上下文取不到组织与医院时整次阻断，且不发生仓储访问。
  /// </summary>
  [Fact]
  public async Task Branch_usage_summary_blocks_when_trusted_scope_is_unavailable()
  {
    FakeStatisticsQueryRepository repository = new();
    // 当前工作身份所在组织在目录中不存在：可信范围解析不出任何层级，入口在仓储访问之前阻断。
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateOrganizationService(), FakeIamScope.MissingOrganization());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("无法确定当前可信组织。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  // ---------- 平台版明细：V11、V12、V26、V27、V28、V29、V30、V24、V25 ----------

  /// <summary>
  /// V11：明细入口的非法分页窗口同样在第一次仓储访问之前按业务拒绝。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Usage_details_reject_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(pageIndex, pageSize)));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V12：明细入口的来源组与接收组组织编码同时提供且不一致时按业务拒绝。
  /// </summary>
  [Fact]
  public async Task Usage_details_reject_mismatched_receiver_and_source_organizations()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionUsageDetailsAsync(new RecognitionUsageDetailsQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        OrganizationCode = TrustedOrganization,
        SourceOrganizationCode = SourceOrganization,
        DetailType = RecognitionUsageDetailType.Reminder,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Contains("不一致", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V12：明细入口的来源组按请求的提供方编码解析并与接收组并存；接收方与提供方落在同一组织的不同医院、院区时，
  /// 下推的提供方医院与提供方院区取请求的提供方编码，不会退化成接收方的医院与院区编码。
  /// </summary>
  [Fact]
  public async Task Usage_details_resolves_requested_source_scope_group_into_the_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    // 可信组织下并列两条医院链：CSYY1 → CSYQ1-1 与 CSYY2 → CSYQ2-1，父子归属各自独立。
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital, SecondHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SecondHospital] = [SecondBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryRecognitionUsageDetailsAsync(new RecognitionUsageDetailsQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      // 接收组取同组织的第二家医院，来源组取可信医院：两侧医院与院区编码都不相同，只有组织编码相同。
      OrganizationCode = TrustedOrganization,
      HospitalCode = SecondHospital,
      BranchCode = SecondBranch,
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = TrustedHospital,
      SourceBranchCode = TrustedBranch,
      DetailType = RecognitionUsageDetailType.Reminder,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    Assert.NotNull(repository.LastDetailFilter);
    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    // 接收组三层按使用方编码下推。
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(SecondHospital, filter.HospitalCode);
    Assert.Equal(SecondBranch, filter.BranchCode);
    // 来源组三层按提供方请求编码下推：与接收方的医院、院区互不相同。
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Equal(TrustedBranch, filter.SourceBranchCode);
    Assert.NotEqual(SecondHospital, filter.SourceHospitalCode);
    Assert.NotEqual(SecondBranch, filter.SourceBranchCode);
    Assert.NotEqual(filter.HospitalCode, filter.SourceHospitalCode);
    Assert.NotEqual(filter.BranchCode, filter.SourceBranchCode);
  }

  /// <summary>
  /// V26、V27、V30：四类明细的业务时间随行返回——提醒按匹配生成时间、采纳与不采纳按互认时间、引用按实际引用时间；
  /// 未反馈项只出现在提醒明细并显示未反馈；患者与人员字段取各业务记录自身保存值按原样返回。
  /// </summary>
  [Fact]
  public async Task Usage_details_carry_business_time_unprocessed_semantics_and_saved_fields()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.AddRange([BuildReminderItem(1, processed: false), BuildReminderItem(2, processed: true)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      }));

    PageResultDto<RecognitionUsageDetailReadModel> reminders = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Reminder));

    Assert.Equal(2, reminders.Items.Count);

    // 未反馈项：业务时间取匹配生成时间，处理结果标记未反馈，无决策与科室、医生归属。
    RecognitionUsageDetailReadModel unprocessed = reminders.Items[0];
    Assert.True(unprocessed.IsUnprocessed);
    Assert.Equal(BusinessDate.AddHours(1), unprocessed.BusinessTime);
    Assert.False(unprocessed.ProcessingResult!.IsProcessed);
    Assert.Null(unprocessed.ProcessingResult.RecognitionTime);
    Assert.Null(unprocessed.ProcessingResult.Decision);
    Assert.Null(unprocessed.ProcessingResult.DecisionText);
    Assert.Null(unprocessed.RecognitionDeptId);
    Assert.Null(unprocessed.RecognitionDoctorId);

    // 已反馈的提醒项：业务时间仍是匹配生成时间，反馈时间与决策随处理结果返回，决策中文按枚举声明解析。
    RecognitionUsageDetailReadModel processedReminder = reminders.Items[1];
    Assert.False(processedReminder.IsUnprocessed);
    Assert.Equal(BusinessDate.AddHours(2), processedReminder.BusinessTime);
    Assert.True(processedReminder.ProcessingResult!.IsProcessed);
    Assert.Equal(BusinessDate.AddHours(2), processedReminder.ProcessingResult.RecognitionTime);
    Assert.Equal(RecognitionResult.Adopted, processedReminder.ProcessingResult.Decision);
    Assert.Equal("采纳", processedReminder.ProcessingResult.DecisionText);
    Assert.Equal("DPT-1", processedReminder.RecognitionDeptId);
    Assert.Equal("检验科", processedReminder.RecognitionDeptName);
    Assert.Equal("DOC-1", processedReminder.RecognitionDoctorId);
    Assert.Equal("李医生", processedReminder.RecognitionDoctorName);

    // 患者与人员字段按原值返回。
    Assert.Equal("患者2", processedReminder.PatientName);
    Assert.Equal("110101199001011234", processedReminder.IdentityDocumentNo);
    Assert.Equal(VisitType.Outpatient, processedReminder.VisitType);

    // 采纳明细：业务时间取互认时间，决策为采纳并随金额。
    repository.AdoptionItems.Add(BuildAdoptionItem(3));
    PageResultDto<RecognitionUsageDetailReadModel> adoptions = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Adopted));
    RecognitionUsageDetailReadModel adoption = adoptions.Items[0];
    Assert.False(adoption.IsUnprocessed);
    Assert.Equal(BusinessDate.AddHours(3), adoption.BusinessTime);
    Assert.Equal(RecognitionResult.Adopted, adoption.ProcessingResult!.Decision);
    Assert.Equal(BusinessDate.AddHours(3), adoption.ProcessingResult.RecognitionTime);
    Assert.Equal(120m, adoption.ProcessingResult.EstimatedSavingAmount);

    // 不采纳明细：业务时间取互认时间，决策为不采纳并随原因代码、名称与补充说明。
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(4, RecognitionNonAdoptionReason.CurrentConditionMismatch, "病情变化"));
    PageResultDto<RecognitionUsageDetailReadModel> nonAdoptions = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.NotAdopted));
    RecognitionUsageDetailReadModel nonAdoption = nonAdoptions.Items[0];
    Assert.Equal(BusinessDate.AddHours(4), nonAdoption.BusinessTime);
    Assert.Equal(RecognitionResult.NotAdopted, nonAdoption.ProcessingResult!.Decision);
    Assert.Equal("1", nonAdoption.ProcessingResult.NonAdoptionReasonCode);
    Assert.Equal("病情变化致结果难以满足诊疗需求", nonAdoption.ProcessingResult.NonAdoptionReasonName);
    Assert.Equal("病情变化", nonAdoption.ProcessingResult.NonAdoptionSupplementDescription);

    // 引用明细：业务时间取实际引用时间，互认科室与引用科室各自保存。
    repository.ReferenceItems.Add(BuildReferenceItem(5));
    PageResultDto<RecognitionUsageDetailReadModel> references = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Referenced));
    RecognitionUsageDetailReadModel reference = references.Items[0];
    Assert.Equal(BusinessDate.AddHours(6), reference.BusinessTime);
    Assert.True(reference.Reference!.IsReferenced);
    Assert.Equal(BusinessDate.AddHours(6), reference.Reference.ReferenceTime);
    Assert.Equal("RPT-1", reference.Reference.ReferenceDeptId);
    Assert.Equal("病历科", reference.Reference.ReferenceDeptName);
    Assert.Equal(RecognitionResult.Adopted, reference.ProcessingResult!.Decision);
  }

  /// <summary>
  /// V27：未反馈项只进入提醒明细；按采纳、不采纳与引用三种类型查询时都不返回该行。
  /// </summary>
  [Fact]
  public async Task Unprocessed_items_only_enter_reminder_details()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.Add(BuildReminderItem(1, processed: false));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      }));

    PageResultDto<RecognitionUsageDetailReadModel> reminders = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Reminder));
    Assert.Single(reminders.Items);
    Assert.True(reminders.Items[0].IsUnprocessed);

    Assert.Empty((await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Adopted))).Items);
    Assert.Empty((await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20, RecognitionUsageDetailType.NotAdopted))).Items);
    Assert.Empty((await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Referenced))).Items);
  }

  /// <summary>
  /// V50、V51：明细查询把系统填充的精确分组字段原样下推——使用方传科室 ID 与标准项目编码，
  /// 提供方只传标准项目编码；字段不参与汇总筛选，也不改变用户关键字。
  /// </summary>
  [Fact]
  public async Task Detail_queries_forward_exact_group_fields()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.Add(BuildReminderItem(0, processed: true));
    repository.SourceDetailItems.Add(BuildSourceDetailItem(0));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20) with { RecognitionDeptId = "DPT-9", StandardProjectCode = "SP-9" });
    Assert.Equal("DPT-9", repository.LastDetailFilter!.RecognitionDeptId);
    Assert.Equal("SP-9", repository.LastDetailFilter.StandardProjectCode);

    await service.QuerySourceRecognitionDetailsAsync(
      BuildSourceDetailsRequest(1, 20) with { StandardProjectCode = "SP-9" });
    Assert.Equal("SP-9", repository.LastSourceDetailFilter!.StandardProjectCode);

    StatisticsExportFileReadModel usageFile = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails)
        with { RecognitionDeptId = "DPT-9", StandardProjectCode = "SP-9" });
    Assert.Equal("DPT-9", repository.LastDetailFilter!.RecognitionDeptId);
    Assert.Equal("SP-9", repository.LastDetailFilter.StandardProjectCode);
    using (XLWorkbook usageWorkbook = new(new MemoryStream(usageFile.FileStream)))
    {
      Assert.Equal(2, usageWorkbook.Worksheets.First().LastRowUsed()!.RowNumber());
    }

    StatisticsExportFileReadModel sourceFile = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionDetails)
        with { StandardProjectCode = "SP-9" });
    Assert.Equal("SP-9", repository.LastSourceDetailFilter!.StandardProjectCode);
    using XLWorkbook sourceWorkbook = new(new MemoryStream(sourceFile.FileStream));
    Assert.Equal(2, sourceWorkbook.Worksheets.First().LastRowUsed()!.RowNumber());
  }

  /// <summary>
  /// V28：统一关键字、不采纳原因代码与全部维度筛选按下推为明细条件——
  /// 关键字经空白归一（空白视为未提供、非空白去除首尾空白），
  /// 不采纳原因代码按平台统一代码解析，非法代码按参数校验拒绝。
  /// </summary>
  [Fact]
  public async Task Usage_details_push_filters_down_and_parse_reason_code()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryRecognitionUsageDetailsAsync(new RecognitionUsageDetailsQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      Keyword = " DOC-9 ",
      NonAdoptionReasonCode = "6",
      ItemType = MedicalItemType.Laboratory,
      CategoryName = "检验",
      GroupName = "血常规",
      DetailType = RecognitionUsageDetailType.NotAdopted,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Equal("DOC-9", filter.Keyword);
    Assert.Equal(RecognitionNonAdoptionReason.OtherReviewRequired, filter.NonAdoptionReason);
    Assert.Equal(MedicalItemType.Laboratory, filter.ItemType);
    Assert.Equal("检验", filter.CategoryName);
    Assert.Equal("血常规", filter.GroupName);

    System.ComponentModel.DataAnnotations.ValidationException error = await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
      () => service.QueryRecognitionUsageDetailsAsync(new RecognitionUsageDetailsQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        NonAdoptionReasonCode = "not-a-code",
        DetailType = RecognitionUsageDetailType.NotAdopted,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));
    Assert.Contains("不采纳原因代码无效", error.Message, StringComparison.Ordinal);
  }

  /// <summary>
  /// V25：同一筛选条件下，汇总行的六项指标数字与四类明细的总数逐项相等——
  /// 汇总链与明细链把同一起止日期与同一范围条件下推，明细总数来自同一筛选的计数。
  /// </summary>
  [Fact]
  public async Task Detail_counts_match_summary_row_numbers_under_the_same_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 3, adoption: 2, nonAdoption: 1, reference: 1));
    repository.ReminderItems.AddRange([BuildReminderItem(1, processed: true), BuildReminderItem(2, processed: true), BuildReminderItem(3, processed: false)]);
    repository.AdoptionItems.AddRange([BuildAdoptionItem(1), BuildAdoptionItem(2)]);
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(3));
    repository.ReferenceItems.Add(BuildReferenceItem(4));
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<RecognitionUsageSummaryReadModel> summary = await service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      OrganizationCode = TrustedOrganization,
      HospitalCode = TrustedHospital,
      BranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    long reminderTotal = (await service.QueryRecognitionUsageDetailsAsync(BuildScopedDetailsRequest(RecognitionUsageDetailType.Reminder))).Page.TotalCount;
    long adoptionTotal = (await service.QueryRecognitionUsageDetailsAsync(BuildScopedDetailsRequest(RecognitionUsageDetailType.Adopted))).Page.TotalCount;
    long nonAdoptionTotal = (await service.QueryRecognitionUsageDetailsAsync(BuildScopedDetailsRequest(RecognitionUsageDetailType.NotAdopted))).Page.TotalCount;
    long referenceTotal = (await service.QueryRecognitionUsageDetailsAsync(BuildScopedDetailsRequest(RecognitionUsageDetailType.Referenced))).Page.TotalCount;

    Assert.Equal(3, summary.Items[0].ReminderCount);
    Assert.Equal(2, summary.Items[0].AdoptionCount);
    Assert.Equal(1, summary.Items[0].NonAdoptionCount);
    Assert.Equal(1, summary.Items[0].ReferenceCount);
    Assert.Equal(summary.Items[0].ReminderCount, reminderTotal);
    Assert.Equal(summary.Items[0].AdoptionCount, adoptionTotal);
    Assert.Equal(summary.Items[0].NonAdoptionCount, nonAdoptionTotal);
    Assert.Equal(summary.Items[0].ReferenceCount, referenceTotal);

    // 两条链把同一起止日期与同一范围条件下推：各自时间列口径由语句与真实库批次负责，这里冻结下推一致。
    Assert.Equal(repository.LastSummaryFilter!.PeriodStart, repository.LastDetailFilter!.PeriodStart);
    Assert.Equal(repository.LastSummaryFilter.PeriodEnd, repository.LastDetailFilter.PeriodEnd);
    Assert.Equal(repository.LastSummaryFilter.OrganizationCode, repository.LastDetailFilter.OrganizationCode);
    Assert.Equal(repository.LastSummaryFilter.HospitalCode, repository.LastDetailFilter.HospitalCode);
    Assert.Equal(repository.LastSummaryFilter.BranchCode, repository.LastDetailFilter.BranchCode);
  }

  /// <summary>
  /// V29：明细分页在服务端窗口内完成，窗口起点按页码换算；返回行保持仓储给出的顺序，应用层不重排。
  /// </summary>
  [Fact]
  public async Task Usage_details_page_by_server_window_and_preserve_repository_order()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.AdoptionItems.AddRange([BuildAdoptionItem(1), BuildAdoptionItem(2), BuildAdoptionItem(3)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      }));

    PageResultDto<RecognitionUsageDetailReadModel> result = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(2, 2, RecognitionUsageDetailType.Adopted));

    Assert.Single(result.Items);
    Assert.Equal(3, result.Page.TotalCount);
    Assert.Equal(2, repository.LastDetailSkipCount);
    Assert.Equal(2, repository.LastDetailPageSize);
    // 计数与取页各一次：本替身按成员累计调用，采纳链两次调用即计数一次加取页一次。
    Assert.Equal(2, repository.AdoptionCalls);
    // 返回行保持仓储切片顺序：第三行的患者原值，应用层不重排。
    Assert.Equal("患者3", result.Items[0].PatientName);
  }

  /// <summary>
  /// V24、V30：明细行同时回填使用方与提供方的组织、医院、院区名称，读取次数按不同组织与医院进行、不随行数增长。
  /// </summary>
  [Fact]
  public async Task Usage_details_backfill_both_sides_names_without_row_proportional_calls()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.AddRange([.. Enumerable.Range(1, 20).Select(index => BuildReminderItem(index, processed: true))]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<RecognitionUsageDetailReadModel> result = await service.QueryRecognitionUsageDetailsAsync(
      BuildDetailsRequest(1, 20, RecognitionUsageDetailType.Reminder));

    Assert.Equal(20, result.Items.Count);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
    Assert.All(result.Items, row =>
    {
      Assert.Equal(TrustedOrganization, row.Receiver.OrganizationCode);
      Assert.Equal("示范医共体", row.Receiver.OrganizationName);
      Assert.Equal("示范医院", row.Receiver.HospitalName);
      Assert.Equal("示范院区", row.Receiver.BranchName);
      Assert.Equal(SourceOrganization, row.Source.OrganizationCode);
      Assert.Equal("其他医共体", row.Source.OrganizationName);
      Assert.Equal("其他医院", row.Source.HospitalName);
    });
  }

  // ---------- 本院版明细：V13 ----------

  /// <summary>
  /// V13：本院版明细的使用方组织与使用方医院由可信上下文注入，院区为空时筛选不附加院区条件，提供方组织恒为可信组织。
  /// </summary>
  [Fact]
  public async Task Branch_usage_details_inject_trusted_scope_into_the_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryBranchRecognitionUsageDetailsAsync(new BranchRecognitionUsageDetailsQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      DetailType = RecognitionUsageDetailType.Reminder,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(RecognitionUsageDetailType.Reminder, repository.LastDetailType);
  }

  /// <summary>
  /// V11：本院版明细入口的非法分页窗口同样在第一次仓储访问之前按业务拒绝，
  /// 计数与取页一次都没有被调用，与平台版明细入口的窗口校验同构。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Branch_usage_details_reject_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryBranchRecognitionUsageDetailsAsync(new BranchRecognitionUsageDetailsQueryRequest
        {
          StartTime = PeriodFrom,
          EndTime = PeriodTo,
          DetailType = RecognitionUsageDetailType.Reminder,
          Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
        }));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  // ---------- 平台版提供方汇总：V31、V32、V33、S6-D18 ----------

  /// <summary>
  /// V33：平台版提供方汇总入口的非法分页窗口在第一次仓储访问之前按业务拒绝，
  /// 计数与取页一次都没有被调用，与提供方明细及本院版提供方入口的窗口校验同构。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Source_summary_rejects_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(pageIndex, pageSize)));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V32：互认科室维度值在提供方汇总查询按业务拒绝，校验先于第一次仓储访问，
  /// 组织服务三层读取一次都没有发生；平台版与本院版入口同口径（本院版由专门用例再取证）。
  /// </summary>
  [Fact]
  public async Task Source_summary_rejects_recognition_department_dimension_before_any_repository_access()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(1, 20, RecognitionStatisticsGroupDimension.RecognitionDepartment)));

    Assert.Contains("互认科室", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V31、V12：来源组与接收组的组织、医院、院区按请求使用并经组织路径解析校验后，以去空白编码下推为筛选条件；
  /// 两组组织编码一致；时间范围按本地日换算为起始日零点（含）与结束日次日零点（不含）。
  /// </summary>
  [Fact]
  public async Task Source_summary_resolves_requested_scope_groups_into_the_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QuerySourceRecognitionSummaryAsync(new SourceRecognitionSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceOrganizationCode = $" {TrustedOrganization} ",
      SourceHospitalCode = FakeIamScope.FourthHospitalCode,
      SourceBranchCode = FakeIamScope.FourthBranchCode,
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FourthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(FakeIamScope.FourthBranchCode, filter.SourceBranchCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Equal(TrustedHospital, filter.ReceiverHospitalCode);
    Assert.Equal(TrustedBranch, filter.ReceiverBranchCode);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodStart);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodEnd);
    Assert.Equal(DateTimeKind.Unspecified, filter.PeriodStart.Kind);
  }

  /// <summary>
  /// V31、V12：来源组与接收组组织编码同时提供且不一致时按业务拒绝，且不读取任何仓储数据。
  /// </summary>
  [Fact]
  public async Task Source_summary_rejects_mismatched_source_and_receiver_organizations()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QuerySourceRecognitionSummaryAsync(new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        SourceOrganizationCode = SourceOrganization,
        ReceiverOrganizationCode = TrustedOrganization,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Contains("不一致", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V31、S6-D18：任一范围条件为空不附加该层过滤，全部为空即按授权全量；此时不发起组织服务读取。
  /// </summary>
  [Fact]
  public async Task Source_summary_leaves_empty_scope_conditions_unfiltered_without_organization_reads()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(1, 20));

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Null(filter.SourceOrganizationCode);
    Assert.Null(filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Null(filter.ReceiverOrganizationCode);
    Assert.Null(filter.ReceiverHospitalCode);
    Assert.Null(filter.ReceiverBranchCode);
    Assert.Equal(0, organizationService.OrganizationReadCount);
  }

  /// <summary>
  /// V31：提供的提供方医院取值经组织路径解析校验，医院不属于所选组织时整次拒绝，不返回部分结果。
  /// </summary>
  [Fact]
  public async Task Source_summary_rejects_scope_outside_the_organization_tree()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      }));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QuerySourceRecognitionSummaryAsync(new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        SourceOrganizationCode = SourceOrganization,
        SourceHospitalCode = TrustedHospital,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("业务拒绝：提供方医院不存在、已停用或不属于所选组织。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V32、V14：每行为所选提供方维度的一个分组值，未参与分组的维度字段为空，无总计行；
  /// 被互认次数随行返回，读模型统计期间按本地日闭区间解释。
  /// </summary>
  [Fact]
  public async Task Source_summary_keeps_dimension_row_shape_without_total_rows()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.AddRange(
    [
      BuildSourceSummaryRow(TrustedHospital, count: 7),
      BuildSourceStandardItemRow("STD-001", count: 3)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital] }));

    PageResultDto<SourceRecognitionSummaryReadModel> result = await service.QuerySourceRecognitionSummaryAsync(
      new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    Assert.Equal(2, result.Items.Count);

    SourceRecognitionSummaryReadModel hospitalRow = result.Items[0];
    Assert.Equal(RecognitionStatisticsGroupDimension.Hospital, hospitalRow.GroupDimension);
    Assert.Equal(TrustedOrganization, hospitalRow.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, hospitalRow.SourceHospitalCode);
    Assert.Null(hospitalRow.SourceBranchCode);
    Assert.Null(hospitalRow.SourceBranchName);
    Assert.Null(hospitalRow.ItemType);
    Assert.Null(hospitalRow.StandardProjectCode);
    Assert.Null(hospitalRow.StandardProjectName);
    Assert.Null(hospitalRow.CategoryName);
    Assert.Null(hospitalRow.GroupName);
    Assert.Equal(7, hospitalRow.RecognitionCount);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), hospitalRow.PeriodStart);
    Assert.Equal(new DateOnly(2026, 9, 30).ToDateTime(TimeOnly.MaxValue), hospitalRow.PeriodEnd);

    SourceRecognitionSummaryReadModel standardRow = result.Items[1];
    Assert.Equal(RecognitionStatisticsGroupDimension.Hospital, standardRow.GroupDimension);
    Assert.Null(standardRow.SourceOrganizationCode);
    Assert.Null(standardRow.SourceOrganizationName);
    Assert.Null(standardRow.SourceHospitalCode);
    Assert.Null(standardRow.SourceHospitalName);
    Assert.Equal(MedicalItemType.Laboratory, standardRow.ItemType);
    Assert.Equal("STD-001", standardRow.StandardProjectCode);
    Assert.Equal("血常规", standardRow.StandardProjectName);
    Assert.Equal("检验", standardRow.CategoryName);
    Assert.Equal("血常规", standardRow.GroupName);
    Assert.Equal(3, standardRow.RecognitionCount);
  }

  /// <summary>
  /// V32 修订面：提供方院区维度必须同时保留组织与医院键，名称解析才能完成；
  /// 只按院区分组会让医院编码为空，页面读取被业务拒绝。
  /// </summary>
  [Fact]
  public async Task Source_branch_summary_keeps_hospital_key_and_resolves_names()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(
      BuildSourceSummaryRow(TrustedHospital, count: 4, branchCode: TrustedBranch));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital] },
      branches: new Dictionary<string, string[]> { [TrustedHospital] = [TrustedBranch] }));

    PageResultDto<SourceRecognitionSummaryReadModel> result = await service.QuerySourceRecognitionSummaryAsync(
      new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    SourceRecognitionSummaryReadModel row = Assert.Single(result.Items);
    Assert.Equal(TrustedOrganization, row.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, row.SourceHospitalCode);
    Assert.Equal(TrustedBranch, row.SourceBranchCode);
    Assert.False(string.IsNullOrWhiteSpace(row.SourceOrganizationName));
    Assert.False(string.IsNullOrWhiteSpace(row.SourceHospitalName));
    Assert.False(string.IsNullOrWhiteSpace(row.SourceBranchName));
    Assert.Equal(4, row.RecognitionCount);
  }

  /// <summary>
  /// V33：提供方汇总分页在服务端窗口内完成；越界页返回空集合且保留真实总数；无命中时返回空页成功。
  /// </summary>
  [Fact]
  public async Task Source_summary_pages_by_server_window_and_keeps_real_total()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.AddRange(
    [
      BuildSourceSummaryRow(TrustedHospital, count: 1),
      BuildSourceSummaryRow(SecondHospital, count: 1),
      BuildSourceSummaryRow(FakeIamScope.FourthHospitalCode, count: 1)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode] }));

    PageResultDto<SourceRecognitionSummaryReadModel> lastPartialPage = await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(2, 2));
    Assert.Single(lastPartialPage.Items);
    Assert.Equal(3, lastPartialPage.Page.TotalCount);
    Assert.Equal(2, repository.LastSourceSummarySkipCount);
    Assert.Equal(2, repository.LastSourceSummaryPageSize);
    Assert.Equal(1, repository.SourceSummaryCountCalls);
    Assert.Equal(1, repository.SourceSummaryPageCalls);

    PageResultDto<SourceRecognitionSummaryReadModel> beyondLastPage = await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(9, 2));
    Assert.Empty(beyondLastPage.Items);
    Assert.Equal(3, beyondLastPage.Page.TotalCount);

    FakeStatisticsQueryRepository emptyRepository = new();
    PageResultDto<SourceRecognitionSummaryReadModel> emptyResult = await CreateService(emptyRepository, CreateOrganizationService())
      .QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(1, 20));
    Assert.Empty(emptyResult.Items);
    Assert.Equal(0, emptyResult.Page.TotalCount);
  }

  /// <summary>
  /// V49：提供方汇总查询与汇总导出保持仓储给出的业务量顺序——首行被互认次数最多，
  /// 次数相同的行按提供方医院编码升序；分页与全量导出不重排。
  /// </summary>
  /// <remarks>排序子句由 <see cref="Stage6SqlMapTests"/> 的语句断言负责，本用例冻结应用层与导出层对仓储行序的保持。</remarks>
  [Fact]
  public async Task Source_summary_query_and_export_preserve_recognition_count_order_and_group_key_tiebreak()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.AddRange(
    [
      BuildSourceSummaryRow(TrustedHospital, count: 9),
      BuildSourceSummaryRow(SecondHospital, count: 7),
      BuildSourceSummaryRow(FakeIamScope.FourthHospitalCode, count: 5),
      BuildSourceSummaryRow(FakeIamScope.FifthHospitalCode, count: 5),
      BuildSourceSummaryRow(FakeIamScope.SixthHospitalCode, count: 3)
    ]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode, FakeIamScope.FifthHospitalCode, FakeIamScope.SixthHospitalCode] });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<SourceRecognitionSummaryReadModel> firstPage = await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(1, 2));
    PageResultDto<SourceRecognitionSummaryReadModel> middlePage = await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(2, 2));
    PageResultDto<SourceRecognitionSummaryReadModel> lastPage = await service.QuerySourceRecognitionSummaryAsync(BuildSourceSummaryRequest(3, 2));
    string[] pageKeys =
    [
      .. firstPage.Items.Select(row => row.SourceHospitalCode!),
      .. middlePage.Items.Select(row => row.SourceHospitalCode!),
      .. lastPage.Items.Select(row => row.SourceHospitalCode!)
    ];

    Assert.Equal([TrustedHospital, SecondHospital, FakeIamScope.FourthHospitalCode, FakeIamScope.FifthHospitalCode, FakeIamScope.SixthHospitalCode], pageKeys);
    Assert.Equal(5, pageKeys.Distinct(StringComparer.Ordinal).Count());
    Assert.Equal(9, firstPage.Items[0].RecognitionCount);
    Assert.Equal(7, firstPage.Items[1].RecognitionCount);

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionSummary));
    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(6, worksheet.LastRowUsed()!.RowNumber());
    Assert.Equal(TrustedHospital, worksheet.Row(2).Cell(3).GetString());
    Assert.Equal(SecondHospital, worksheet.Row(3).Cell(3).GetString());
    Assert.Equal(FakeIamScope.FourthHospitalCode, worksheet.Row(4).Cell(3).GetString());
    Assert.Equal(FakeIamScope.FifthHospitalCode, worksheet.Row(5).Cell(3).GetString());
    Assert.Equal(FakeIamScope.SixthHospitalCode, worksheet.Row(6).Cell(3).GetString());
  }

  /// <summary>
  /// V24：提供方汇总行的组织、医院与院区名称按当页分组行涉及的编码批量解析回填，
  /// 读取次数不随行数增长；标准项目维度行没有来源归属，名称保持空值。
  /// </summary>
  [Theory]
  [InlineData(10)]
  [InlineData(50)]
  public async Task Source_summary_backfills_source_names_by_page_scope_without_growing_calls(int rowCount)
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.AddRange(
      [.. Enumerable.Range(1, rowCount).Select(index => BuildSourceSummaryRow(TrustedHospital, count: index, branchCode: TrustedBranch))]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<SourceRecognitionSummaryReadModel> result = await service.QuerySourceRecognitionSummaryAsync(
      new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 200 }
      });

    Assert.Equal(rowCount, result.Items.Count);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
    Assert.All(result.Items, row =>
    {
      Assert.Equal("示范医共体", row.SourceOrganizationName);
      Assert.Equal("示范医院", row.SourceHospitalName);
      Assert.Equal("示范院区", row.SourceBranchName);
    });
  }

  // ---------- 名称回填容错：同一业务编码在 IAM 目录中命中多个节点 ----------

  /// <summary>
  /// 名称回填容错：某编码命中两个节点但两者名称一致时按该一致名称回填，整页不因歧义拒绝。
  /// </summary>
  /// <remarks>
  /// 名称是展示字段：候选名称一致时名称本身可以确定，属于容错取舍的第一种可取值；
  /// 该用例在名称回填切到容错入口之前按业务拒绝整页失败，因此构成 RED 证据。
  /// </remarks>
  [Fact]
  public async Task Source_summary_backfills_the_shared_name_of_two_nodes_on_the_same_code()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(SourceHospital, count: 3, branchCode: SourceBranch));
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateOrganizationService(), FakeIamScope.WithDuplicateOtherHospitalCode(name: "其他医院"));

    PageResultDto<SourceRecognitionSummaryReadModel> result = await service.QuerySourceRecognitionSummaryAsync(
      new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    SourceRecognitionSummaryReadModel row = Assert.Single(result.Items);
    Assert.Equal("其他医共体", row.SourceOrganizationName);
    Assert.Equal("其他医院", row.SourceHospitalName);
    Assert.Equal("其他院区", row.SourceBranchName);
  }

  /// <summary>
  /// 名称回填容错：某编码命中两个节点、名称不同且都不在会话组织链上时名称留空，整页仍返回成功。
  /// </summary>
  /// <remarks>
  /// 名称留空是「宁可留空也不猜」的取舍：不抛异常、不在两个名称之间择一、也不回落编码文本；
  /// 行本身、编码与总数都照常返回，只有该层名称缺失。
  /// </remarks>
  [Fact]
  public async Task Source_summary_leaves_the_name_empty_when_a_code_hits_two_nodes_with_different_names()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(SourceHospital, count: 3, branchCode: SourceBranch));
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateOrganizationService(), FakeIamScope.WithDuplicateOtherHospitalCode(name: "其他医院二"));

    PageResultDto<SourceRecognitionSummaryReadModel> result = await service.QuerySourceRecognitionSummaryAsync(
      new SourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    SourceRecognitionSummaryReadModel row = Assert.Single(result.Items);
    Assert.Equal(1, result.Page.TotalCount);
    Assert.Equal(SourceOrganization, row.SourceOrganizationCode);
    Assert.Equal(SourceHospital, row.SourceHospitalCode);
    Assert.Equal("其他医共体", row.SourceOrganizationName);
    // 留空：既不用编码回填，也不在两个候选名称之间猜测。
    Assert.Equal(string.Empty, row.SourceHospitalName);
    Assert.Equal("其他院区", row.SourceBranchName);
  }

  /// <summary>
  /// 名称回填容错：候选之一落在当前会话组织链上时，取链上那个节点的名称。
  /// </summary>
  /// <remarks>
  /// 会话组织链是名称回填唯一可观察的可信依据：链上的候选说明该行就指向调用方实际工作的组织，
  /// 因此优先于另一条链上同名编码的节点。严格入口本身已有同一收敛规则，本用例是它的行为契约。
  /// </remarks>
  [Fact]
  public async Task Usage_summary_takes_the_name_of_the_candidate_on_the_current_organization_chain()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 1, branchCode: TrustedBranch));
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateOrganizationService(), FakeIamScope.WithDuplicateTrustedHospitalCode(name: "其他地方示范医院"));

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        GroupDimension = RecognitionStatisticsGroupDimension.Branch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    RecognitionUsageSummaryReadModel row = Assert.Single(result.Items);
    Assert.Equal("示范医共体", row.ReceiverOrganizationName);
    Assert.Equal("示范医院", row.ReceiverHospitalName);
    Assert.Equal("示范院区", row.ReceiverBranchName);
  }

  /// <summary>
  /// 回归保护：提交的编码命中多个节点时归属校验照常通过，查询按编码文本下推筛选。
  /// </summary>
  /// <remarks>
  /// 编码是业务键，组织节点标识不是：两个候选都符合组织类型，校验只判存在性与类型即通过；
  /// 父子归属因组织层命中多个候选而无法定论，按规则跳过。筛选值取提交的编码文本，
  /// 因此「编码不唯一」不再是整页失败的理由；旧实现把这种情况整次业务拒绝，本用例在旧实现下失败。
  /// </remarks>
  [Fact]
  public async Task Ambiguous_business_code_passes_scope_validation_and_filters_by_the_code()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateOrganizationService(), FakeIamScope.WithDuplicateOtherOrganizationCode());

    PageResultDto<RecognitionUsageSummaryReadModel> result = await service.QueryRecognitionUsageSummaryAsync(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        OrganizationCode = SourceOrganization,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      });

    Assert.Empty(result.Items);
    Assert.Equal(0, result.Page.TotalCount);
    // 筛选按提交的编码文本下推：编码是业务键，实例标识不参与该层过滤。
    Assert.Equal(SourceOrganization, repository.LastSummaryFilter!.OrganizationCode);
    Assert.True(repository.TotalCalls > 0);
  }

  // ---------- 平台版提供方明细：V32、V33、V30、V24 ----------

  /// <summary>
  /// V33：提供方明细入口的非法分页窗口在第一次仓储访问之前按业务拒绝，计数与取页一次都没有被调用。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Source_details_reject_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QuerySourceRecognitionDetailsAsync(BuildSourceDetailsRequest(pageIndex, pageSize)));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V33、V30（S6-D19 修订面）：来源组与接收组筛选及统一关键字按下推为明细条件，分页在服务端窗口内完成，
  /// 计数与取页各一次且共用同一套筛选条件，返回行保持仓储给出的顺序。
  /// </summary>
  [Fact]
  public async Task Source_details_push_filters_down_and_page_by_server_window()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceDetailItems.AddRange(
      [BuildSourceDetailItem(1, SourceOrganization), BuildSourceDetailItem(2, SourceOrganization), BuildSourceDetailItem(3, SourceOrganization)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      }));

    PageResultDto<SourceRecognitionDetailReadModel> result = await service.QuerySourceRecognitionDetailsAsync(new SourceRecognitionDetailsQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = FakeIamScope.FourthHospitalCode,
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ItemType = MedicalItemType.Laboratory,
      CategoryName = "检验",
      GroupName = "血常规",
      Keyword = "STD-1",
      Page = new PageRequestDto { PageIndex = 2, PageSize = 2 }
    });

    SourceRecognitionDetailFilter filter = repository.LastSourceDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FourthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Equal(TrustedHospital, filter.ReceiverHospitalCode);
    Assert.Equal(MedicalItemType.Laboratory, filter.ItemType);
    Assert.Equal("检验", filter.CategoryName);
    Assert.Equal("血常规", filter.GroupName);
    Assert.Equal("STD-1", filter.Keyword);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodStart);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodEnd);

    Assert.Single(result.Items);
    Assert.Equal(3, result.Page.TotalCount);
    Assert.Equal(2, repository.LastSourceDetailSkipCount);
    Assert.Equal(2, repository.LastSourceDetailPageSize);
    Assert.Equal(2, repository.SourceDetailCalls);
    Assert.Equal("患者3", result.Items[0].PatientName);
  }

  /// <summary>
  /// V32、V30、V24：提供方明细只反映被采纳事实——互认时间随行返回为业务时间，
  /// 互认科室与医生取处理结果自身保存值，患者姓名与证件号码按业务原值返回；
  /// 使用方与提供方名称按双侧编码批量回填，读取次数按不同组织与医院进行、不随行数增长。
  /// </summary>
  [Fact]
  public async Task Source_details_map_adopted_fact_fields_and_backfill_both_sides_names()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceDetailItems.AddRange([BuildSourceDetailItem(1), BuildSourceDetailItem(2)]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<SourceRecognitionDetailReadModel> result = await service.QuerySourceRecognitionDetailsAsync(BuildSourceDetailsRequest(1, 20));

    Assert.Equal(2, result.Items.Count);
    Assert.All(result.Items, row =>
    {
      Assert.Equal("DPT-1", row.RecognitionDeptId);
      Assert.Equal("检验科", row.RecognitionDeptName);
      Assert.Equal("DOC-1", row.RecognitionDoctorId);
      Assert.Equal("李医生", row.RecognitionDoctorName);
      Assert.Equal("110101199001011234", row.IdentityDocumentNo);
      Assert.Equal("STD-001", row.StandardProjectCode);
      Assert.Equal("示范医共体", row.ReceiverOrganizationName);
      Assert.Equal("示范医院", row.ReceiverHospitalName);
      Assert.Equal("示范院区", row.ReceiverBranchName);
      Assert.Equal("其他医共体", row.SourceOrganizationName);
      Assert.Equal("其他医院", row.SourceHospitalName);
      Assert.Equal("其他院区", row.SourceBranchName);
    });
    Assert.Equal(BusinessDate.AddHours(2), result.Items[1].RecognitionTime);
    Assert.Equal("患者2", result.Items[1].PatientName);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V54、S6-D25：提供方明细每行随行返回标准项目名称、项目类型取值与文本、标准目录分类与分组名称；
  /// 标准目录缺失时四项为空、标准项目编码原样保留，页面按缺失规则显示，不回查标准目录。
  /// </summary>
  [Fact]
  public async Task Source_details_map_standard_catalog_fields_of_each_row()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceDetailItems.AddRange(
    [
      BuildSourceDetailItem(1),
      BuildSourceDetailItem(2) with { StandardProjectName = null, CategoryName = null, GroupName = null }
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    PageResultDto<SourceRecognitionDetailReadModel> result = await service.QuerySourceRecognitionDetailsAsync(BuildSourceDetailsRequest(1, 20));

    Assert.Equal(2, result.Items.Count);
    Assert.Equal("血常规", result.Items[0].StandardProjectName);
    Assert.Equal(MedicalItemType.Laboratory, result.Items[0].ItemType);
    Assert.Equal("检验", result.Items[0].ItemTypeText);
    Assert.Equal("检验分类", result.Items[0].CategoryName);
    Assert.Equal("血液", result.Items[0].GroupName);
    Assert.Equal("STD-001", result.Items[0].StandardProjectCode);

    Assert.Null(result.Items[1].StandardProjectName);
    Assert.Equal(MedicalItemType.Laboratory, result.Items[1].ItemType);
    Assert.Equal("检验", result.Items[1].ItemTypeText);
    Assert.Null(result.Items[1].CategoryName);
    Assert.Null(result.Items[1].GroupName);
    Assert.Equal("STD-001", result.Items[1].StandardProjectCode);
  }

  // ---------- 本院版提供方：V31、V32、V33 ----------

  /// <summary>
  /// V31：本院版提供方的提供方组织与提供方医院由可信上下文注入，请求无法指定其他提供方医院；
  /// 本侧提供方院区为空时按可信医院全部提供方院区统计，筛选不附加院区条件，也不进入院区层解析；
  /// 使用方组织恒为可信组织，接收组筛选为空时不附加该层条件。
  /// </summary>
  [Fact]
  public async Task Branch_source_summary_injects_trusted_source_scope_and_skips_branch_layer_when_branch_is_empty()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    FakeIamScope scope = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService, scope);

    await service.QueryBranchSourceRecognitionSummaryAsync(BuildBranchSourceSummaryRequest(1, 20));

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Null(filter.ReceiverHospitalCode);
    Assert.Null(filter.ReceiverBranchCode);
    // 可信范围来自 IAM 目录：三层由一次批量读取归类，不再按层分别调用外部组织服务，
    // 原先「组织 1 次 / 医院 1 次 / 院区 0 次」的分层计数判据随之退出；此处只断言目录确实被读取过，
    // 局部性（读取次数不随条目数增长）由按报告版本的批量回填用例承接。
    Assert.True(scope.BatchReadCount > 0, "可信范围应来自 IAM 目录读取。");
  }

  /// <summary>
  /// V31：本侧提供方院区非空时经解析校验存在、启用且属于可信医院，并以解析后的编码附加院区过滤。
  /// </summary>
  [Fact]
  public async Task Branch_source_summary_resolves_optional_source_branch_when_provided()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryBranchSourceRecognitionSummaryAsync(new BranchSourceRecognitionSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceBranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Equal(TrustedBranch, filter.SourceBranchCode);
  }

  /// <summary>
  /// V31：不属于可信医院的院区按业务拒绝，不返回越界数据，且不发生仓储访问。
  /// </summary>
  [Fact]
  public async Task Branch_source_summary_rejects_source_branch_outside_trusted_hospital()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchSourceRecognitionSummaryAsync(new BranchSourceRecognitionSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        SourceBranchCode = "BRH-UNKNOWN",
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V31：接收组医院与院区筛选限可信组织——取值经解析校验属于可信组织与可信医院后按下推，
  /// 使用方组织恒为可信组织。
  /// </summary>
  [Fact]
  public async Task Branch_source_summary_resolves_receiver_filters_limited_to_trusted_organization()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SecondHospital] = [SecondBranch]
      }));

    await service.QueryBranchSourceRecognitionSummaryAsync(new BranchSourceRecognitionSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      ReceiverHospitalCode = SecondHospital,
      ReceiverBranchCode = SecondBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Equal(SecondHospital, filter.ReceiverHospitalCode);
    Assert.Equal(SecondBranch, filter.ReceiverBranchCode);
  }

  /// <summary>
  /// V32：本院版提供方汇总同样拒绝互认科室维度值，校验先于第一次仓储访问，
  /// 组织服务三层读取一次都没有发生。
  /// </summary>
  [Fact]
  public async Task Branch_source_summary_rejects_recognition_department_dimension_before_any_repository_access()
  {
    FakeStatisticsQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchSourceRecognitionSummaryAsync(
        BuildBranchSourceSummaryRequest(1, 20, RecognitionStatisticsGroupDimension.RecognitionDepartment)));

    Assert.Contains("互认科室", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V33：本院版提供方汇总入口的非法分页窗口在第一次仓储访问之前按业务拒绝；
  /// 可信范围解析与请求校验先于窗口校验，计数与取页一次都没有被调用。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Branch_source_summary_rejects_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryBranchSourceRecognitionSummaryAsync(new BranchSourceRecognitionSummaryQueryRequest
        {
          StartTime = PeriodFrom,
          EndTime = PeriodTo,
          GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
          Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
        }));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V31：本院版提供方明细的提供方组织与提供方医院由可信上下文注入，本侧提供方院区为空不附加院区条件；
  /// 接收组医院筛选限可信组织并按下推。
  /// </summary>
  [Fact]
  public async Task Branch_source_details_inject_trusted_source_scope_and_push_receiver_filters()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] }));

    await service.QueryBranchSourceRecognitionDetailsAsync(new BranchSourceRecognitionDetailsQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      ReceiverHospitalCode = SecondHospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    SourceRecognitionDetailFilter filter = repository.LastSourceDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Equal(SecondHospital, filter.ReceiverHospitalCode);
    Assert.Equal(string.Empty, filter.ReceiverBranchCode);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodStart);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodEnd);
    Assert.Equal(2, repository.SourceDetailCalls);
  }

  /// <summary>
  /// V33：本院版提供方明细入口的非法分页窗口同样在第一次仓储访问之前按业务拒绝。
  /// </summary>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Branch_source_details_reject_invalid_window_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryBranchSourceRecognitionDetailsAsync(new BranchSourceRecognitionDetailsQueryRequest
        {
          StartTime = PeriodFrom,
          EndTime = PeriodTo,
          Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
        }));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  // ---------- 匹配记录集合视图：V39 ----------

  /// <summary>
  /// V39：未知匹配记录标识按业务拒绝——先查询组级信息，未命中即拒绝，不再读取匹配项集合，
  /// 组织服务三层读取一次都没有发生。
  /// </summary>
  [Fact]
  public async Task Match_record_view_rejects_unknown_id_as_business_error_without_reading_items()
  {
    FakeStatisticsQueryRepository repository = new();
    Guid unknownId = Guid.Parse("3f2d1c0b-9a8e-4d7c-b6a5-1e2f3a4b5c6d");
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryRecognitionMatchRecordAsync(new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = unknownId }));

    Assert.Contains("不存在", error.Message, StringComparison.Ordinal);
    Assert.Equal(unknownId, repository.LastMatchRecordViewId);
    Assert.Equal(1, repository.MatchRecordViewCalls);
    Assert.Equal(0, repository.MatchRecordViewItemsCalls);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V39：匹配记录标识为空 Guid 按参数校验拒绝，且不发生仓储访问。
  /// </summary>
  [Fact]
  public async Task Match_record_view_rejects_empty_guid_before_any_repository_access()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryRecognitionMatchRecordAsync(new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = Guid.Empty }));

    Assert.Contains("互认匹配记录标识", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V39：按标识取单记录返回组级信息、患者字段、就诊、反馈状态、决定主体与全部匹配项；
  /// 未反馈记录没有记录级互认时间与科室、医生归属；各项的反馈状态与决策取自身处理结果，
  /// 未反馈项不推断为不采纳；使用方与提供方名称按编码批量回填。
  /// </summary>
  [Fact]
  public async Task Match_record_view_maps_group_info_feedback_state_items_and_backfills_names()
  {
    Guid recordId = Guid.Parse("4a5b6c7d-8e9f-4a1b-2c3d-5e6f7a8b9c0d");
    FakeStatisticsQueryRepository repository = new();
    repository.MatchRecordViews.Add(BuildMatchRecordView(recordId, isProcessed: false));
    repository.MatchRecordViewItems.AddRange(
    [
      BuildMatchRecordViewItem(processed: true, RecognitionResult.Adopted),
      BuildMatchRecordViewItem(processed: true, RecognitionResult.NotAdopted, RecognitionNonAdoptionReason.CurrentConditionMismatch),
      BuildMatchRecordViewItem(processed: false)
    ]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    RecognitionMatchRecordReadModel view = await service.QueryRecognitionMatchRecordAsync(
      new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = recordId });

    Assert.Equal(recordId, view.RecognitionMatchRecordId);
    Assert.Equal(BusinessDate.AddHours(-2), view.MatchCreatedTime);
    Assert.Equal(TrustedOrganization, view.Receiver.OrganizationCode);
    Assert.Equal("示范医共体", view.Receiver.OrganizationName);
    Assert.Equal("示范医院", view.Receiver.HospitalName);
    Assert.Equal("示范院区", view.Receiver.BranchName);
    Assert.Equal("患者1", view.PatientName);
    Assert.Equal("110101199001011234", view.IdentityDocumentNo);
    Assert.Equal(VisitType.Outpatient, view.VisitType);
    Assert.Equal("V-1", view.VisitSerialNo);
    Assert.False(view.IsProcessed);
    Assert.Null(view.RecognitionTime);
    Assert.Null(view.RecognitionDeptId);
    Assert.Null(view.RecognitionDeptName);
    Assert.Null(view.RecognitionDoctorId);
    Assert.Null(view.RecognitionDoctorName);

    Assert.Equal(3, view.MatchItems.Count);

    RecognitionMatchRecordItemReadModel adopted = view.MatchItems[0];
    Assert.Equal("STD-001", adopted.Item.StandardProjectCode);
    Assert.Equal("血常规", adopted.Item.StandardProjectName);
    Assert.Equal("检验", adopted.Item.CategoryName);
    Assert.Equal(SourceOrganization, adopted.Source.OrganizationCode);
    Assert.Equal("其他医共体", adopted.Source.OrganizationName);
    Assert.Equal("其他医院", adopted.Source.HospitalName);
    Assert.True(adopted.IsProcessed);
    Assert.Equal(RecognitionResult.Adopted, adopted.Decision);
    Assert.Equal("采纳", adopted.DecisionText);
    Assert.Null(adopted.NonAdoptionReasonCode);

    RecognitionMatchRecordItemReadModel notAdopted = view.MatchItems[1];
    Assert.True(notAdopted.IsProcessed);
    Assert.Equal(RecognitionResult.NotAdopted, notAdopted.Decision);
    Assert.Equal("不采纳", notAdopted.DecisionText);
    Assert.Equal("1", notAdopted.NonAdoptionReasonCode);
    Assert.Equal("病情变化致结果难以满足诊疗需求", notAdopted.NonAdoptionReasonName);

    RecognitionMatchRecordItemReadModel unprocessed = view.MatchItems[2];
    Assert.False(unprocessed.IsProcessed);
    Assert.Null(unprocessed.Decision);
    Assert.Null(unprocessed.DecisionText);
    Assert.Null(unprocessed.NonAdoptionReasonCode);
  }

  /// <summary>
  /// V39：组级行已反馈时记录级互认时间、互认科室与互认医生随行返回，
  /// 各记录级取值与组级聚合数据逐项一致；记录反馈状态保持组级聚合值。
  /// </summary>
  [Fact]
  public async Task Match_record_view_returns_processed_record_level_fields_from_group_row()
  {
    Guid recordId = Guid.Parse("7e8f9a0b-1c2d-4e3f-4a5b-6c7d8e9f0a1b");
    DateTime recognitionTime = BusinessDate.AddHours(3);
    FakeStatisticsQueryRepository repository = new();
    repository.MatchRecordViews.Add(BuildMatchRecordView(
      recordId,
      isProcessed: true,
      recognitionTime: recognitionTime,
      recognitionDeptId: "DPT-1",
      recognitionDeptName: "检验科",
      recognitionDoctorId: "DOC-1",
      recognitionDoctorName: "李医生"));
    repository.MatchRecordViewItems.AddRange(
    [
      BuildMatchRecordViewItem(processed: true, RecognitionResult.Adopted),
      BuildMatchRecordViewItem(processed: true, RecognitionResult.NotAdopted, RecognitionNonAdoptionReason.CurrentConditionMismatch)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      }));

    RecognitionMatchRecordReadModel view = await service.QueryRecognitionMatchRecordAsync(
      new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = recordId });

    Assert.True(view.IsProcessed);
    Assert.Equal(recognitionTime, view.RecognitionTime);
    Assert.Equal("DPT-1", view.RecognitionDeptId);
    Assert.Equal("检验科", view.RecognitionDeptName);
    Assert.Equal("DOC-1", view.RecognitionDoctorId);
    Assert.Equal("李医生", view.RecognitionDoctorName);
  }

  /// <summary>
  /// V39：使用方按组级单键、提供方按匹配项编码去重批量解析名称，
  /// 组织服务各层读取次数与明细链同口径地不随匹配项数量增长；各项名称取批量解析结果回填。
  /// </summary>
  [Theory]
  [InlineData(3)]
  [InlineData(50)]
  public async Task Match_record_view_backfills_names_by_deduplicated_keys_without_growing_calls(int itemCount)
  {
    Guid recordId = Guid.Parse("8f9a0b1c-2d3e-4f4a-5b6c-7d8e9f0a1b2c");
    FakeStatisticsQueryRepository repository = new();
    repository.MatchRecordViews.Add(BuildMatchRecordView(recordId, isProcessed: false));
    repository.MatchRecordViewItems.AddRange(
      [.. Enumerable.Range(1, itemCount).Select(_ => BuildMatchRecordViewItem(processed: true, RecognitionResult.Adopted))]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    RecognitionMatchRecordReadModel view = await service.QueryRecognitionMatchRecordAsync(
      new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = recordId });

    Assert.Equal(itemCount, view.MatchItems.Count);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
    Assert.Equal("示范医共体", view.Receiver.OrganizationName);
    Assert.All(view.MatchItems, item =>
    {
      Assert.Equal("其他医共体", item.Source.OrganizationName);
      Assert.Equal("其他医院", item.Source.HospitalName);
      Assert.Equal("其他院区", item.Source.BranchName);
    });
  }

  // ---------- 统计导出：V34、V35、V36、V37、V38 的应用层替身取证 ----------

  /// <summary>
  /// V38：提醒明细导出返回 Excel 工作簿文件——文件名为「导出类型名称-起止日期.xlsx」、
  /// 文件格式为工作簿内容类型、文件字节可按单工作表工作簿打开，工作表以导出类型中文名称命名。
  /// </summary>
  [Fact]
  public async Task Reminder_export_returns_xlsx_file_named_by_type_and_period()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.AddRange([BuildReminderItem(0, processed: true), BuildReminderItem(1, processed: false)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails));

    Assert.Equal("使用方提醒明细-20260901-20260930.xlsx", file.FileName);
    Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileFormat);
    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = Assert.Single(workbook.Worksheets);
    Assert.Equal("使用方提醒明细", worksheet.Name);
  }

  /// <summary>
  /// V35：提醒明细导出取当前查询条件下的全部记录、不使用页面分页——取页调用以「窗口起点 0、页容量等于计数」读取全量；
  /// 每行按匹配项展开并携带互认匹配记录与匹配项标识，未反馈项的处理结果列显示「未反馈」，已反馈项显示决策文本。
  /// </summary>
  [Fact]
  public async Task Reminder_export_reads_all_rows_without_paging_and_marks_unprocessed()
  {
    Guid recordId = Guid.Parse("3a7b2c9d-1111-4a2b-9c3d-1f0a2b3c4d5e");
    Guid itemId = Guid.Parse("4b8c3dae-2222-4b3c-ad4e-2a1b3c4d5e6f");
    FakeStatisticsQueryRepository repository = new();
    RecognitionReminderDetailItem unprocessed = BuildReminderItem(0, processed: false);
    RecognitionReminderDetailItem processed = BuildReminderItem(1, processed: true) with
    {
      RecognitionMatchRecordId = recordId,
      RecognitionMatchItemId = itemId
    };
    repository.ReminderItems.AddRange([unprocessed, processed]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails));

    Assert.Equal(0, repository.LastDetailSkipCount);
    Assert.Equal(2, repository.LastDetailPageSize);
    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(3, worksheet.LastRowUsed()!.RowNumber());
    IXLRow unprocessedRow = worksheet.Row(2);
    IXLRow processedRow = worksheet.Row(3);
    Assert.Equal(unprocessed.RecognitionMatchRecordId.ToString(), unprocessedRow.Cell(1).GetString());
    Assert.Equal(unprocessed.RecognitionMatchItemId.ToString(), unprocessedRow.Cell(2).GetString());
    Assert.Equal("未反馈", unprocessedRow.Cell(30).GetString());
    Assert.Equal(recordId.ToString(), processedRow.Cell(1).GetString());
    Assert.Equal(itemId.ToString(), processedRow.Cell(2).GetString());
    Assert.Equal("采纳", processedRow.Cell(30).GetString());
  }

  /// <summary>
  /// V35：零记录按业务拒绝「当前条件下无可导出数据」，只发生计数读取、不取明细，不生成文件。
  /// </summary>
  [Fact]
  public async Task Export_rejects_zero_records_after_counting_only()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails)));

    Assert.Equal("业务拒绝：当前条件下无可导出数据。", error.Message);
    Assert.Equal(1, repository.ReminderCalls);
  }

  /// <summary>
  /// V36：计数超过十万行上限按业务拒绝并提示缩小查询条件，只发生计数读取、不取明细。
  /// </summary>
  [Fact]
  public async Task Export_rejects_counts_beyond_the_row_limit_with_narrowing_prompt()
  {
    FakeStatisticsQueryRepository repository = new() { ReminderCountOverride = 100_001 };
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails)));

    Assert.Contains("十万行上限", error.Message, StringComparison.Ordinal);
    Assert.Contains("缩小查询条件", error.Message, StringComparison.Ordinal);
    Assert.Equal(1, repository.ReminderCalls);
  }

  /// <summary>
  /// V36：计数恰为十万行上限时允许导出——上限按「超过才拒绝」解释，正常全量读取并生成文件。
  /// </summary>
  [Fact]
  public async Task Export_allows_counts_at_the_row_limit_boundary()
  {
    FakeStatisticsQueryRepository repository = new() { ReminderCountOverride = 100_000 };
    repository.ReminderItems.Add(BuildReminderItem(0, processed: true));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails));

    Assert.Equal(2, repository.ReminderCalls);
    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
  }

  /// <summary>
  /// V35（S6-D19 修订面）：导出继承发起时的全部查询条件——接收组与来源组经组织路径解析后按去空白编码下推，
  /// 日期范围按本地日换算，统一关键字经空白归一后下推（空白视为未提供、非空白去首尾空白），
  /// 不采纳原因代码仅作用于使用方不采纳明细导出。
  /// </summary>
  [Fact]
  public async Task Export_resolves_requested_scope_groups_and_conditions_into_the_filter()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(0));
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.GetStatisticsExportAsync(new RecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.RecognitionNonAdoptionDetails,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      ReceiverOrganizationCode = $" {TrustedOrganization} ",
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      SourceOrganizationCode = TrustedOrganization,
      SourceHospitalCode = FakeIamScope.FifthHospitalCode,
      SourceBranchCode = FakeIamScope.FifthBranchCode,
      Keyword = " DPT-9 ",
      NonAdoptionReasonCode = "2",
      ItemType = MedicalItemType.Laboratory,
      CategoryName = " 检验 ",
      GroupName = "血常规",
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Equal(TrustedBranch, filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FifthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(FakeIamScope.FifthBranchCode, filter.SourceBranchCode);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodStart);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), filter.PeriodEnd);
    Assert.Equal("DPT-9", filter.Keyword);
    Assert.Equal(RecognitionNonAdoptionReason.RapidDiseaseProgression, filter.NonAdoptionReason);
    Assert.Equal("检验", filter.CategoryName);
  }

  /// <summary>
  /// V46 的应用面：统一关键字在平台版汇总入口、本院版汇总入口与提供方明细入口
  /// 均经空白归一后下推——空白输入不携带条件，非空白输入去除首尾空白（审查回改 F-01 口径）。
  /// </summary>
  [Fact]
  public async Task Keyword_normalization_applies_across_query_entries()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital] }));

    await service.QueryRecognitionUsageSummaryAsync(BuildSummaryRequest(1, 20) with { Keyword = " AbC_1 " });
    RecognitionUsageSummaryFilter platformSummaryFilter = repository.LastSummaryFilter!;
    await service.QueryBranchRecognitionUsageSummaryAsync(new BranchRecognitionUsageSummaryQueryRequest
    {
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Keyword = "\u3000",
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });
    RecognitionUsageSummaryFilter branchSummaryFilter = repository.LastSummaryFilter!;
    await service.QuerySourceRecognitionDetailsAsync(BuildSourceDetailsRequest(1, 20) with { Keyword = "X_9" });

    Assert.Equal("AbC_1", platformSummaryFilter.Keyword);
    Assert.Null(branchSummaryFilter.Keyword);
    Assert.Equal("X_9", repository.LastSourceDetailFilter!.Keyword);
  }

  /// <summary>
  /// V46 的空白语义（审查回改 F-01）：关键字为全空白文本——全角空格 U+3000 与混合空白——
  /// 视为未提供，领域筛选记录不携带关键字条件；全角空白属于 Unicode 空白，不能只按 ASCII 空白剥除判定。
  /// </summary>
  [Fact]
  public async Task Keyword_of_unicode_whitespace_only_is_treated_as_not_provided()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20) with { Keyword = "\u3000" });
    RecognitionUsageDetailFilter fullWidthBlankFilter = repository.LastDetailFilter!;
    await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20) with { Keyword = " \u3000\t " });
    RecognitionUsageDetailFilter mixedBlankFilter = repository.LastDetailFilter!;

    Assert.Null(fullWidthBlankFilter.Keyword);
    Assert.Null(mixedBlankFilter.Keyword);
  }

  /// <summary>
  /// V28 的原因作用域（审查回改 F-02）：不采纳原因代码仅在不采纳明细查询解析并下推——
  /// 提醒、采纳与引用明细查询携带原因代码时不携带该条件，非法代码在其余明细类型也不触发参数校验拒绝。
  /// </summary>
  [Theory]
  [InlineData(RecognitionUsageDetailType.Reminder)]
  [InlineData(RecognitionUsageDetailType.Adopted)]
  [InlineData(RecognitionUsageDetailType.Referenced)]
  public async Task Non_adoption_reason_code_applies_only_to_not_adopted_detail_queries(RecognitionUsageDetailType detailType)
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.Add(BuildReminderItem(0, processed: true));
    repository.AdoptionItems.Add(BuildAdoptionItem(1));
    repository.ReferenceItems.Add(BuildReferenceItem(2));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20, detailType) with { NonAdoptionReasonCode = "2" });
    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Null(filter.NonAdoptionReason);

    // 原因代码仅在不采纳明细查询解析：其余明细类型提交非法代码同样不触发参数校验拒绝。
    await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest(1, 20, detailType) with { NonAdoptionReasonCode = "not-a-code" });
  }

  /// <summary>
  /// V47（S6-D19 修订面）：汇总导出、使用方明细导出与提供方明细导出都继承统一关键字，命中口径与页面查询一致；
  /// 不采纳原因代码仍仅作用于使用方不采纳明细导出——提醒明细导出与提供方明细导出提交原因代码也不下推。
  /// </summary>
  [Fact]
  public async Task Exports_inherit_the_keyword_and_keep_reason_code_scope()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 1, adoption: 1));
    repository.ReminderItems.Add(BuildReminderItem(0, processed: true));
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(1));
    repository.SourceDetailItems.Add(BuildSourceDetailItem(2));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    await service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionUsageSummary) with { Keyword = "STD-1" });
    RecognitionUsageSummaryFilter summaryFilter = repository.LastSummaryFilter!;
    await service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionNonAdoptionDetails) with { Keyword = " DOC-9 " });
    RecognitionUsageDetailFilter nonAdoptionFilter = repository.LastDetailFilter!;
    await service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails) with
    {
      Keyword = "STD-2",
      NonAdoptionReasonCode = "2"
    });
    RecognitionUsageDetailFilter reminderFilter = repository.LastDetailFilter!;
    await service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionDetails) with
    {
      Keyword = "X_9",
      NonAdoptionReasonCode = "2"
    });
    SourceRecognitionDetailFilter sourceDetailFilter = repository.LastSourceDetailFilter!;

    Assert.Equal("STD-1", summaryFilter.Keyword);
    Assert.Equal("DOC-9", nonAdoptionFilter.Keyword);
    Assert.Equal("STD-2", reminderFilter.Keyword);
    Assert.Null(reminderFilter.NonAdoptionReason);
    Assert.Equal("X_9", sourceDetailFilter.Keyword);
  }

  /// <summary>
  /// V35：越权条件按既有范围校验拒绝——医院不属于所选组织时整次拒绝，不读取任何业务数据。
  /// </summary>
  [Fact]
  public async Task Export_rejects_out_of_scope_conditions_before_any_repository_access()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      }));

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetStatisticsExportAsync(new RecognitionStatisticsExportRequest
      {
        ExportType = RecognitionStatisticsExportType.RecognitionReminderDetails,
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        ReceiverOrganizationCode = SourceOrganization,
        ReceiverHospitalCode = TrustedHospital,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital
      }));

    Assert.Equal("业务拒绝：医院不存在、已停用或不属于所选组织。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V35：接收组与来源组组织编码同时提供且不一致时整次拒绝，不读取任何业务数据。
  /// </summary>
  [Fact]
  public async Task Export_rejects_mismatched_receiver_and_source_organizations()
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetStatisticsExportAsync(new RecognitionStatisticsExportRequest
      {
        ExportType = RecognitionStatisticsExportType.RecognitionReminderDetails,
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        ReceiverOrganizationCode = TrustedOrganization,
        SourceOrganizationCode = SourceOrganization,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital
      }));

    Assert.Contains("不一致", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V35、S6-D18：范围条件全空不附加该层过滤、不发起组织服务读取，按授权全量导出；
  /// 计数取 1 使导出进入成功路径，无数据行时名称回填也不读取组织服务。
  /// </summary>
  [Fact]
  public async Task Export_leaves_empty_scope_conditions_unfiltered_without_organization_reads()
  {
    FakeStatisticsQueryRepository repository = new() { ReminderCountOverride = 1 };
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.GetStatisticsExportAsync(BuildExportRequest(RecognitionStatisticsExportType.RecognitionReminderDetails));

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Null(filter.OrganizationCode);
    Assert.Null(filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Null(filter.SourceOrganizationCode);
    Assert.Null(filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(0, organizationService.OrganizationReadCount);
    Assert.Equal(0, organizationService.HospitalReadCount);
    Assert.Equal(0, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V34、S6-D15：提供方导出类型不接受互认科室维度值，进入仓储之前按业务拒绝。
  /// </summary>
  [Theory]
  [InlineData(RecognitionStatisticsExportType.SourceRecognitionSummary)]
  [InlineData(RecognitionStatisticsExportType.SourceRecognitionDetails)]
  public async Task Source_side_exports_reject_the_recognition_department_dimension(RecognitionStatisticsExportType exportType)
  {
    FakeStatisticsQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStatisticsExportAsync(
      BuildExportRequest(exportType, RecognitionStatisticsGroupDimension.RecognitionDepartment)));

    Assert.Equal("业务拒绝：提供方汇总不支持按互认科室维度统计。", error.Message);
    Assert.Equal(0, repository.TotalCalls);
  }

  /// <summary>
  /// V34：汇总导出忽略不采纳原因代码——该代码仅作用于使用方不采纳明细导出，汇总导出不拦截、不下推。
  /// </summary>
  [Fact]
  public async Task Usage_summary_export_ignores_the_non_adoption_reason_code()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 1, adoption: 1));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(new RecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.RecognitionUsageSummary,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      NonAdoptionReasonCode = "不采纳",
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    Assert.EndsWith(".xlsx", file.FileName, StringComparison.Ordinal);
    Assert.Equal(1, repository.SummaryCountCalls);
  }

  /// <summary>
  /// V34：采纳明细导出按匹配项一行展开，处理结果显示决策文本，类型专属列携带预计节省金额。
  /// </summary>
  [Fact]
  public async Task Adoption_export_carries_decision_and_estimated_saving_amount()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.AdoptionItems.Add(BuildAdoptionItem(0));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionAdoptionDetails));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal("预计节省金额", worksheet.Row(1).Cell(32).GetString());
    IXLRow dataRow = worksheet.Row(2);
    Assert.Equal("采纳", dataRow.Cell(30).GetString());
    Assert.Equal(120d, dataRow.Cell(32).GetDouble());
    Assert.Equal(2, repository.AdoptionCalls);
  }

  /// <summary>
  /// V34：不采纳明细导出的类型专属列携带原因代码、原因名称与补充说明。
  /// </summary>
  [Fact]
  public async Task Non_adoption_export_carries_reason_code_name_and_supplement()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(0, RecognitionNonAdoptionReason.CurrentConditionMismatch, "补充说明"));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionNonAdoptionDetails));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal("不采纳原因代码", worksheet.Row(1).Cell(32).GetString());
    IXLRow dataRow = worksheet.Row(2);
    Assert.Equal("1", dataRow.Cell(32).GetString());
    Assert.Equal("病情变化致结果难以满足诊疗需求", dataRow.Cell(33).GetString());
    Assert.Equal("补充说明", dataRow.Cell(34).GetString());
  }

  /// <summary>
  /// V34：引用明细导出的类型专属列携带实际引用时间与引用科室、医生。
  /// </summary>
  [Fact]
  public async Task Reference_export_carries_reference_time_and_reference_org_fields()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.ReferenceItems.Add(BuildReferenceItem(0));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionReferenceDetails));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal("引用时间", worksheet.Row(1).Cell(32).GetString());
    IXLRow dataRow = worksheet.Row(2);
    Assert.Equal(BusinessDate.AddHours(1), dataRow.Cell(32).GetDateTime());
    Assert.Equal("RPT-1", dataRow.Cell(33).GetString());
    Assert.Equal("病历科", dataRow.Cell(34).GetString());
    Assert.Equal("DOC-2", dataRow.Cell(35).GetString());
    Assert.Equal("王医生", dataRow.Cell(36).GetString());
  }

  /// <summary>
  /// V34：使用方汇总导出为页面汇总行同结构、按当前条件重新聚合，不采纳原因按行展开——
  /// 每个原因一行、组级字段重复，原因次数与占比来自行内装配的原因汇总，占比以行内不采纳次数为分母。
  /// </summary>
  [Fact]
  public async Task Usage_summary_export_expands_reason_rows_with_counts_and_ratio()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 4, adoption: 3, nonAdoption: 2, reference: 1));
    repository.ReasonItems.AddRange(
    [
      BuildReasonRow(TrustedHospital, RecognitionNonAdoptionReason.CurrentConditionMismatch, 1),
      BuildReasonRow(TrustedHospital, RecognitionNonAdoptionReason.RapidDiseaseProgression, 1)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.RecognitionUsageSummary));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(3, worksheet.LastRowUsed()!.RowNumber());
    IXLRow reasonRow = worksheet.Row(2);
    Assert.Equal(TrustedHospital, reasonRow.Cell(3).GetString());
    Assert.Equal(4d, reasonRow.Cell(16).GetDouble());
    Assert.Equal(2d, reasonRow.Cell(18).GetDouble());
    Assert.Equal("1", reasonRow.Cell(22).GetString());
    Assert.Equal(1d, reasonRow.Cell(24).GetDouble());
    Assert.Equal(0.5d, reasonRow.Cell(25).GetDouble());
  }

  /// <summary>
  /// V34：提供方汇总导出复用提供方语句按当前条件重新聚合，被互认次数随分组行导出。
  /// </summary>
  [Fact]
  public async Task Source_summary_export_reads_source_side_summary_statements()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(TrustedHospital, count: 7));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionSummary));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal("提供方医院被互认汇总", worksheet.Name);
    Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    IXLRow dataRow = worksheet.Row(2);
    Assert.Equal("被互认次数", worksheet.Row(1).Cell(14).GetString());
    Assert.Equal(7d, dataRow.Cell(14).GetDouble());
    Assert.Equal(1, repository.SourceSummaryCountCalls);
    Assert.Equal(1, repository.SourceSummaryPageCalls);
  }

  /// <summary>
  /// V34：提供方明细导出按匹配项一行展开并携带互认匹配记录与匹配项标识，复用提供方明细语句全量读取。
  /// </summary>
  [Fact]
  public async Task Source_detail_export_carries_both_side_ids()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceDetailItems.Add(BuildSourceDetailItem(0));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionDetails));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    IXLRow dataRow = worksheet.Row(2);
    Assert.Equal("使用方医院编码", worksheet.Row(1).Cell(11).GetString());
    Assert.Equal(TrustedHospital, dataRow.Cell(11).GetString());
    Assert.Equal("互认时间", worksheet.Row(1).Cell(20).GetString());
    Assert.Equal(2, repository.SourceDetailCalls);
  }

  /// <summary>
  /// V56、S6-D25：提供方明细导出的标准项目名称、编码、项目类型、分类与分组按页面明细列顺序输出，
  /// 四列取值与页面同源，导出总数与页面明细总数一致。
  /// </summary>
  [Fact]
  public async Task Source_detail_export_columns_follow_the_page_detail_order_and_share_the_row_values()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceDetailItems.AddRange([BuildSourceDetailItem(1), BuildSourceDetailItem(2), BuildSourceDetailItem(3)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    PageResultDto<SourceRecognitionDetailReadModel> page = await service.QuerySourceRecognitionDetailsAsync(BuildSourceDetailsRequest(1, 20));
    SourceRecognitionDetailFilter? pageFilter = repository.LastSourceDetailFilter;

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(
      BuildExportRequest(RecognitionStatisticsExportType.SourceRecognitionDetails));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    string[] headers = [.. worksheet.Row(1).CellsUsed().Select(cell => cell.GetString())];
    int projectNameColumn = Array.IndexOf(headers, "标准项目名称") + 1;
    Assert.True(projectNameColumn > 0, "提供方明细导出应包含标准项目名称列。");
    Assert.Equal(
      ["标准项目名称", "标准项目编码", "项目类型", "分类名称", "分组名称"],
      headers[(projectNameColumn - 1)..(projectNameColumn + 4)]);

    // 互认时间与患者排在科室/医生之前，与页面明细列序一致。
    int recognitionTimeColumn = Array.IndexOf(headers, "互认时间");
    Assert.True(recognitionTimeColumn > 0, "提供方明细导出应包含互认时间列。");
    string[] tail = headers[recognitionTimeColumn..];
    Assert.True(tail.Length >= 4, $"互认时间之后应还有患者与科室列，实际只有 {tail.Length} 列。");
    Assert.Equal(["互认时间", "患者姓名", "证件号码", "互认科室ID"], tail[..4]);

    IXLRow firstRow = worksheet.Row(2);
    Assert.Equal("血常规", firstRow.Cell(projectNameColumn).GetString());
    Assert.Equal("STD-001", firstRow.Cell(projectNameColumn + 1).GetString());
    Assert.Equal("检验", firstRow.Cell(projectNameColumn + 2).GetString());
    Assert.Equal("检验分类", firstRow.Cell(projectNameColumn + 3).GetString());
    Assert.Equal("血液", firstRow.Cell(projectNameColumn + 4).GetString());

    // 导出按同一筛选条件的计数取全量：行数与页面明细总数一致，筛选边界与页面查询逐项相同。
    Assert.Equal(page.Page.TotalCount, worksheet.LastRowUsed()!.RowNumber() - 1L);
    Assert.NotNull(pageFilter);
    SourceRecognitionDetailFilter exportFilter = repository.LastSourceDetailFilter!;
    Assert.Equal(pageFilter.PeriodStart, exportFilter.PeriodStart);
    Assert.Equal(pageFilter.PeriodEnd, exportFilter.PeriodEnd);
    Assert.Equal(pageFilter.SourceOrganizationCode, exportFilter.SourceOrganizationCode);
    Assert.Equal(pageFilter.ReceiverOrganizationCode, exportFilter.ReceiverOrganizationCode);
    Assert.Equal(pageFilter.StandardProjectCode, exportFilter.StandardProjectCode);
  }

  /// <summary>
  /// V37：七个导出类型的列头逐一冻结——列全部来自既有读模型已审字段，且全部单元格不出现
  /// 完整报告结构化内容、PDF 文件标识与下载入口、影像调阅地址、医院院内项目编码/名称/项目映射。
  /// </summary>
  [Theory]
  [InlineData(RecognitionStatisticsExportType.RecognitionUsageSummary)]
  [InlineData(RecognitionStatisticsExportType.RecognitionReminderDetails)]
  [InlineData(RecognitionStatisticsExportType.RecognitionAdoptionDetails)]
  [InlineData(RecognitionStatisticsExportType.RecognitionNonAdoptionDetails)]
  [InlineData(RecognitionStatisticsExportType.RecognitionReferenceDetails)]
  [InlineData(RecognitionStatisticsExportType.SourceRecognitionSummary)]
  [InlineData(RecognitionStatisticsExportType.SourceRecognitionDetails)]
  public async Task Export_headers_stay_within_approved_columns_and_never_carry_forbidden_content(
    RecognitionStatisticsExportType exportType)
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SummaryGroupItems.Add(BuildSummaryRow(TrustedHospital, reminder: 1, adoption: 1, nonAdoption: 1));
    repository.ReasonItems.Add(BuildReasonRow(TrustedHospital, RecognitionNonAdoptionReason.CurrentConditionMismatch, 1));
    repository.ReminderItems.Add(BuildReminderItem(0, processed: false));
    repository.AdoptionItems.Add(BuildAdoptionItem(0));
    repository.NonAdoptionItems.Add(BuildNonAdoptionItem(0));
    repository.ReferenceItems.Add(BuildReferenceItem(0));
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(TrustedHospital, count: 1));
    repository.SourceDetailItems.Add(BuildSourceDetailItem(0));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateReceiverAndSourceOrganizationService());

    StatisticsExportFileReadModel file = await service.GetStatisticsExportAsync(BuildExportRequest(exportType));

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    string[] actualHeaders = [.. worksheet.Row(1).CellsUsed().Select(cell => cell.GetString())];
    Assert.Equal(ExpectedExportHeaders[exportType], actualHeaders);
    foreach (IXLCell cell in worksheet.CellsUsed())
    {
      string text = cell.GetString();
      Assert.DoesNotContain("PDF", text, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("影像", text, StringComparison.Ordinal);
      Assert.DoesNotContain("院内项目", text, StringComparison.Ordinal);
      Assert.DoesNotContain("项目映射", text, StringComparison.Ordinal);
      Assert.DoesNotContain("报告内容", text, StringComparison.Ordinal);
    }
  }

  /// <summary>七个导出类型的冻结列头，键为导出类型。</summary>
  private static IReadOnlyDictionary<RecognitionStatisticsExportType, string[]> ExpectedExportHeaders =>
    new Dictionary<RecognitionStatisticsExportType, string[]>
    {
      [RecognitionStatisticsExportType.RecognitionUsageSummary] =
      [
        "使用方组织编码", "使用方组织名称", "使用方医院编码", "使用方医院名称", "使用方院区编码", "使用方院区名称",
        "互认科室ID", "互认科室名称", "项目类型", "标准项目编码", "标准项目名称", "分类名称", "分组名称",
        "统计开始日期", "统计结束日期",
        "提醒次数", "采纳次数", "不采纳次数", "引用次数", "同期互认率", "预计节省金额",
        "不采纳原因代码", "不采纳原因名称", "原因次数", "原因占比"
      ],
      [RecognitionStatisticsExportType.RecognitionReminderDetails] =
      [
        "互认匹配记录ID", "互认匹配项ID", "匹配生成时间", "就诊类型", "就诊流水号",
        "提供方组织编码", "提供方组织名称", "提供方医院编码", "提供方医院名称", "提供方院区编码", "提供方院区名称",
        "使用方组织编码", "使用方组织名称", "使用方医院编码", "使用方医院名称", "使用方院区编码", "使用方院区名称",
        "项目类型", "分类名称", "分组名称", "标准项目编码", "标准项目名称",
        "业务时间", "患者姓名", "证件号码",
        "互认科室ID", "互认科室名称", "互认医生ID", "互认医生名称", "处理结果", "互认时间"
      ],
      [RecognitionStatisticsExportType.RecognitionAdoptionDetails] =
      [
        .. ExpectedExportDetailBaseHeaders, "预计节省金额"
      ],
      [RecognitionStatisticsExportType.RecognitionNonAdoptionDetails] =
      [
        .. ExpectedExportDetailBaseHeaders, "不采纳原因代码", "不采纳原因名称", "不采纳补充说明"
      ],
      [RecognitionStatisticsExportType.RecognitionReferenceDetails] =
      [
        .. ExpectedExportDetailBaseHeaders, "引用时间", "引用科室ID", "引用科室名称", "引用医生ID", "引用医生名称"
      ],
      [RecognitionStatisticsExportType.SourceRecognitionSummary] =
      [
        "提供方组织编码", "提供方组织名称", "提供方医院编码", "提供方医院名称", "提供方院区编码", "提供方院区名称",
        "项目类型", "标准项目编码", "标准项目名称", "分类名称", "分组名称",
        "统计开始日期", "统计结束日期", "被互认次数"
      ],
      [RecognitionStatisticsExportType.SourceRecognitionDetails] =
      [
        "互认匹配记录ID", "互认匹配项ID",
        "提供方组织编码", "提供方组织名称", "提供方医院编码", "提供方医院名称", "提供方院区编码", "提供方院区名称",
        "使用方组织编码", "使用方组织名称", "使用方医院编码", "使用方医院名称", "使用方院区编码", "使用方院区名称",
        "标准项目名称", "标准项目编码", "项目类型", "分类名称", "分组名称",
        "互认时间", "患者姓名", "证件号码",
        "互认科室ID", "互认科室名称", "互认医生ID", "互认医生名称"
      ]
    };

  /// <summary>使用方明细导出的基础列头（不含类型专属列）。</summary>
  private static string[] ExpectedExportDetailBaseHeaders { get; } =
  [
    "互认匹配记录ID", "互认匹配项ID", "匹配生成时间", "就诊类型", "就诊流水号",
    "提供方组织编码", "提供方组织名称", "提供方医院编码", "提供方医院名称", "提供方院区编码", "提供方院区名称",
    "使用方组织编码", "使用方组织名称", "使用方医院编码", "使用方医院名称", "使用方院区编码", "使用方院区名称",
    "项目类型", "分类名称", "分组名称", "标准项目编码", "标准项目名称",
    "业务时间", "患者姓名", "证件号码",
    "互认科室ID", "互认科室名称", "互认医生ID", "互认医生名称", "处理结果", "互认时间"
  ];

  // ---------- 本院版统计导出：V13、V31、V35 的应用层替身取证 ----------

  /// <summary>
  /// V13：本院版使用方导出的本侧组织与医院取自可信上下文注入，院区为空按可信医院全院范围；
  /// 提供方组织恒为可信组织，来源组医院与院区取请求并经解析校验。
  /// </summary>
  [Fact]
  public async Task Branch_export_injects_trusted_receiver_scope_and_resolves_source_group()
  {
    FakeStatisticsQueryRepository repository = new() { ReminderCountOverride = 1 };
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SourceHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.GetBranchStatisticsExportAsync(new BranchRecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.RecognitionReminderDetails,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceHospitalCode = FakeIamScope.FifthHospitalCode,
      SourceBranchCode = FakeIamScope.FifthBranchCode,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Null(filter.BranchCode);
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(FakeIamScope.FifthHospitalCode, filter.SourceHospitalCode);
    Assert.Equal(FakeIamScope.FifthBranchCode, filter.SourceBranchCode);
  }

  /// <summary>
  /// V13：本院版使用方导出提交本侧院区时，筛选按可信医院下的该院区收敛。
  /// </summary>
  [Fact]
  public async Task Branch_export_injects_local_branch_filter_when_provided()
  {
    FakeStatisticsQueryRepository repository = new() { ReminderCountOverride = 1 };
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.GetBranchStatisticsExportAsync(new BranchRecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.RecognitionReminderDetails,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      BranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    RecognitionUsageDetailFilter filter = repository.LastDetailFilter!;
    Assert.Equal(TrustedOrganization, filter.OrganizationCode);
    Assert.Equal(TrustedHospital, filter.HospitalCode);
    Assert.Equal(TrustedBranch, filter.BranchCode);
  }

  /// <summary>
  /// V31：本院版提供方导出的提供方组织与提供方医院恒为可信上下文，请求无法指定其他提供方医院；
  /// 本侧提供方院区可选，接收组医院与院区取请求并限可信组织。
  /// </summary>
  [Fact]
  public async Task Branch_export_locks_source_side_to_the_trusted_scope()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(TrustedHospital, count: 1));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.GetBranchStatisticsExportAsync(new BranchRecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.SourceRecognitionSummary,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceHospitalCode = "HOS-EVIL",
      ReceiverHospitalCode = TrustedHospital,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Null(filter.SourceBranchCode);
    Assert.Equal(TrustedOrganization, filter.ReceiverOrganizationCode);
    Assert.Equal(TrustedHospital, filter.ReceiverHospitalCode);
  }

  /// <summary>
  /// V31：本院版提供方导出提交本侧提供方院区时，筛选按可信医院下的该院区收敛。
  /// </summary>
  [Fact]
  public async Task Branch_export_injects_local_source_branch_filter_when_provided()
  {
    FakeStatisticsQueryRepository repository = new();
    repository.SourceSummaryGroupItems.Add(BuildSourceSummaryRow(TrustedHospital, count: 1, branchCode: TrustedBranch));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.GetBranchStatisticsExportAsync(new BranchRecognitionStatisticsExportRequest
    {
      ExportType = RecognitionStatisticsExportType.SourceRecognitionSummary,
      StartTime = PeriodFrom,
      EndTime = PeriodTo,
      SourceBranchCode = TrustedBranch,
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital
    });

    SourceRecognitionSummaryFilter filter = repository.LastSourceSummaryFilter!;
    Assert.Equal(TrustedOrganization, filter.SourceOrganizationCode);
    Assert.Equal(TrustedHospital, filter.SourceHospitalCode);
    Assert.Equal(TrustedBranch, filter.SourceBranchCode);
  }

  // ---------- 用例辅助 ----------

  /// <summary>
  /// 构建统计查询应用服务，并把可信请求上下文设置为固定取值。
  /// </summary>
  /// <param name="repository">统计查询仓储替身。</param>
  /// <param name="organizationService">组织服务替身。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(
    FakeStatisticsQueryRepository repository, RecordingOrganizationAppService organizationService, FakeIamScope? iamScope = null)
  {
    FakeIamScope scope = iamScope ?? FakeIamScope.WithManyHospitals();
    return new MedicalRecognitionReportQueryAppService(repository, new PlatformParameterRepositoryStub(), scope, scope, scope);
  }

  /// <summary>构建平台版汇总查询请求。</summary>
  /// <param name="pageIndex">页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>按通用日期范围与医院维度构造的请求。</returns>
  private static RecognitionUsageSummaryQueryRequest BuildSummaryRequest(int pageIndex, int pageSize) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
    Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
  };

  /// <summary>构建平台版明细查询请求。</summary>
  /// <param name="pageIndex">页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <param name="detailType">明细类型。</param>
  /// <returns>按通用日期范围构造的请求。</returns>
  private static RecognitionUsageDetailsQueryRequest BuildDetailsRequest(
    int pageIndex, int pageSize, RecognitionUsageDetailType detailType = RecognitionUsageDetailType.Reminder) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    DetailType = detailType,
    Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
  };

  /// <summary>构建平台版统计导出请求。</summary>
  /// <param name="exportType">导出类型。</param>
  /// <param name="dimension">汇总维度，默认医院。</param>
  /// <returns>按通用日期范围构造的请求；范围条件全空即按授权全量。</returns>
  private static RecognitionStatisticsExportRequest BuildExportRequest(
    RecognitionStatisticsExportType exportType,
    RecognitionStatisticsGroupDimension dimension = RecognitionStatisticsGroupDimension.Hospital) => new()
  {
    ExportType = exportType,
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    GroupDimension = dimension
  };

  /// <summary>
  /// 构建同时包含可信组织与独立提供方组织的组织服务替身，供明细行的双侧名称回填使用。
  /// </summary>
  /// <returns>含两侧组织、医院与院区拓扑的组织服务替身。</returns>
  private static RecordingOrganizationAppService CreateReceiverAndSourceOrganizationService() =>
    CreateOrganizationService(
      organizations: [TrustedOrganization, SourceOrganization],
      hospitals: new Dictionary<string, string[]>
      {
        [TrustedOrganization] = [TrustedHospital],
        [SourceOrganization] = [SourceHospital]
      },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SourceHospital] = [SourceBranch]
      });

  /// <summary>构建携带接收组范围的平台版明细查询请求，供汇总与明细计数一致性用例使用。</summary>
  /// <param name="detailType">明细类型。</param>
  /// <returns>按通用日期范围与接收组范围构造的请求。</returns>
  private static RecognitionUsageDetailsQueryRequest BuildScopedDetailsRequest(RecognitionUsageDetailType detailType) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    OrganizationCode = TrustedOrganization,
    HospitalCode = TrustedHospital,
    BranchCode = TrustedBranch,
    DetailType = detailType,
    Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
  };

  /// <summary>构建平台版提供方汇总查询请求。</summary>
  /// <param name="pageIndex">页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <param name="dimension">汇总维度，默认医院。</param>
  /// <returns>按通用日期范围构造的请求。</returns>
  private static SourceRecognitionSummaryQueryRequest BuildSourceSummaryRequest(
    int pageIndex,
    int pageSize,
    RecognitionStatisticsGroupDimension dimension = RecognitionStatisticsGroupDimension.Hospital) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    GroupDimension = dimension,
    Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
  };

  /// <summary>构建平台版提供方明细查询请求。</summary>
  /// <param name="pageIndex">页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>按通用日期范围构造的请求。</returns>
  private static SourceRecognitionDetailsQueryRequest BuildSourceDetailsRequest(int pageIndex, int pageSize) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
  };

  /// <summary>构建本院版提供方汇总查询请求。</summary>
  /// <param name="pageIndex">页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <param name="dimension">汇总维度，默认医院。</param>
  /// <returns>按通用日期范围构造的请求。</returns>
  private static BranchSourceRecognitionSummaryQueryRequest BuildBranchSourceSummaryRequest(
    int pageIndex,
    int pageSize,
    RecognitionStatisticsGroupDimension dimension = RecognitionStatisticsGroupDimension.Hospital) => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    GroupDimension = dimension,
    Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize }
  };

  /// <summary>按提供方医院业务编码取该医院所属组织的业务编码。</summary>
  /// <remarks>同组织下的第二家及以后医院归属可信组织，另一条链的医院归属其自身组织，与 IAM 替身登记的父子关系一致。</remarks>
  /// <param name="hospitalCode">提供方医院业务编码。</param>
  /// <returns>该医院所属组织的业务编码。</returns>
  private static string OrganizationOfHospital(string hospitalCode) =>
    hospitalCode == FakeIamScope.OtherHospitalCode ? SourceOrganization : TrustedOrganization;

  /// <summary>构建一条提供方汇总分组行。</summary>
  /// <param name="hospitalCode">提供方医院编码（分组值）。</param>
  /// <param name="count">被互认次数。</param>
  /// <param name="branchCode">提供方院区编码；医院维度行为空。</param>
  /// <returns>提供方汇总分组行投影。</returns>
  private static SourceRecognitionSummaryGroupItem BuildSourceSummaryRow(
    string hospitalCode,
    long count = 0,
    string? branchCode = null)
  {
    string organizationCode = OrganizationOfHospital(hospitalCode);
    return new()
    {
      SourceOrganizationCode = organizationCode,
      SourceHospitalCode = hospitalCode,
      SourceBranchCode = branchCode,
      RecognitionCount = count
    };
  }

  /// <summary>构建一条标准项目维度的提供方汇总分组行。</summary>
  /// <param name="standardProjectCode">标准项目编码（分组值）。</param>
  /// <param name="count">被互认次数。</param>
  /// <returns>标准项目维度的提供方汇总分组行投影。</returns>
  private static SourceRecognitionSummaryGroupItem BuildSourceStandardItemRow(string standardProjectCode, long count) => new()
  {
    ItemType = MedicalItemType.Laboratory,
    StandardProjectCode = standardProjectCode,
    StandardItemName = "血常规",
    CategoryName = "检验",
    GroupName = "血常规",
    RecognitionCount = count
  };

  /// <summary>构建一条提供方被互认明细行。</summary>
  /// <param name="index">序号，驱动互认时间与患者姓名。</param>
  /// <param name="sourceOrganizationCode">提供方组织编码；默认取独立的提供方组织，供同组织拓扑用例改取可信组织。</param>
  /// <returns>提供方明细行投影。</returns>
  private static SourceRecognitionDetailItem BuildSourceDetailItem(int index, string? sourceOrganizationCode = null) => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionMatchItemId = Guid.NewGuid(),
    SourceOrganizationCode = sourceOrganizationCode ?? SourceOrganization,
    SourceHospitalCode = SourceHospital,
    SourceBranchCode = SourceBranch,
    ReceiverOrganizationCode = TrustedOrganization,
    ReceiverHospitalCode = TrustedHospital,
    ReceiverBranchCode = TrustedBranch,
    StandardProjectCode = "STD-001",
    StandardProjectName = "血常规",
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "检验分类",
    GroupName = "血液",
    RecognitionTime = BusinessDate.AddHours(index),
    RecognitionDeptId = "DPT-1",
    RecognitionDeptName = "检验科",
    RecognitionDoctorId = "DOC-1",
    RecognitionDoctorName = "李医生",
    PatientName = $"患者{index}",
    IdentityDocumentNo = "110101199001011234"
  };

  /// <summary>构建一条匹配记录集合视图的组级行。</summary>
  /// <param name="recordId">匹配记录标识。</param>
  /// <param name="isProcessed">记录是否已反馈。</param>
  /// <param name="recognitionTime">记录级互认时间；未反馈行为空。</param>
  /// <param name="recognitionDeptId">记录级互认科室ID；未反馈行为空。</param>
  /// <param name="recognitionDeptName">记录级互认科室名称；未反馈行为空。</param>
  /// <param name="recognitionDoctorId">记录级互认医生ID；未反馈行为空。</param>
  /// <param name="recognitionDoctorName">记录级互认医生名称；未反馈行为空。</param>
  /// <returns>匹配记录组级行投影；接收三值与患者、就诊字段按通用取值。</returns>
  private static RecognitionMatchRecordView BuildMatchRecordView(
    Guid recordId,
    bool isProcessed,
    DateTime? recognitionTime = null,
    string? recognitionDeptId = null,
    string? recognitionDeptName = null,
    string? recognitionDoctorId = null,
    string? recognitionDoctorName = null) => new()
    {
      RecognitionMatchRecordId = recordId,
      MatchCreatedTime = BusinessDate.AddHours(-2),
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      PatientName = "患者1",
      IdentityDocumentNo = "110101199001011234",
      VisitType = VisitType.Outpatient,
      VisitSerialNo = "V-1",
      IsProcessed = isProcessed,
      RecognitionTime = recognitionTime,
      RecognitionDeptId = recognitionDeptId,
      RecognitionDeptName = recognitionDeptName,
      RecognitionDoctorId = recognitionDoctorId,
      RecognitionDoctorName = recognitionDoctorName
    };

  /// <summary>构建一条匹配记录集合视图的匹配项行。</summary>
  /// <param name="processed">该项是否已反馈处理结果。</param>
  /// <param name="decision">处理结果决策；未反馈时忽略。</param>
  /// <param name="reason">不采纳原因代码；仅不采纳决策携带。</param>
  /// <returns>匹配项行投影。</returns>
  private static RecognitionMatchRecordViewItem BuildMatchRecordViewItem(
    bool processed,
    RecognitionResult decision = RecognitionResult.Adopted,
    RecognitionNonAdoptionReason? reason = null) => new()
  {
    RecognitionMatchItemId = Guid.NewGuid(),
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "检验",
    GroupName = "血常规",
    StandardProjectCode = "STD-001",
    StandardItemName = "血常规",
    SourceOrganizationCode = SourceOrganization,
    SourceHospitalCode = SourceHospital,
    SourceBranchCode = SourceBranch,
    ReportId = Guid.NewGuid(),
    ReportVersionId = Guid.NewGuid(),
    IsProcessed = processed,
    Decision = processed ? decision : null,
    NonAdoptionReason = processed && decision == RecognitionResult.NotAdopted ? reason : null
  };

  /// <summary>构建一条使用方汇总分组行。</summary>
  /// <param name="hospitalCode">使用方医院编码（分组值）。</param>
  /// <param name="reminder">提醒次数。</param>
  /// <param name="adoption">采纳次数。</param>
  /// <param name="nonAdoption">不采纳次数。</param>
  /// <param name="reference">引用次数。</param>
  /// <param name="branchCode">使用方院区编码；医院维度行为空。</param>
  /// <returns>汇总分组行投影。</returns>
  private static RecognitionUsageSummaryGroupItem BuildSummaryRow(
    string hospitalCode,
    long reminder = 0,
    long adoption = 0,
    long nonAdoption = 0,
    long reference = 0,
    string? branchCode = null) => new()
    {
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = hospitalCode,
      ReceiverBranchCode = branchCode,
      ReminderCount = reminder,
      AdoptionCount = adoption,
      NonAdoptionCount = nonAdoption,
      ReferenceCount = reference
    };

  /// <summary>平台入口的接收组织与来源组织必须一致：不一致时按精确文案拒绝。</summary>
  [Fact]
  public async Task Usage_summary_rejects_source_organization_different_from_receiver()
  {
    MedicalRecognitionReportQueryAppService service = CreateService(new FakeStatisticsQueryRepository(), CreateOrganizationService());

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.QueryRecognitionUsageSummaryAsync(new RecognitionUsageSummaryQueryRequest
      {
        StartTime = PeriodFrom,
        EndTime = PeriodTo,
        OrganizationCode = TrustedOrganization,
        SourceOrganizationCode = SourceOrganization,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("业务拒绝：使用方组织与提供方组织不一致，不能同时按两个组织统计。", exception.Message);
  }

  /// <summary>构建一条与指定医院分组键同套的原因行。</summary>
  /// <param name="hospitalCode">使用方医院编码（分组值）。</param>
  /// <param name="reason">不采纳原因代码。</param>
  /// <param name="count">该原因的次数。</param>
  /// <returns>原因汇总行投影。</returns>
  private static RecognitionUsageReasonItem BuildReasonRow(
    string hospitalCode, RecognitionNonAdoptionReason reason, long count) => new()
    {
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = hospitalCode,
      NonAdoptionReason = reason,
      ReasonCount = count
    };

  /// <summary>构建一条提醒明细行；未反馈项的处理结果相关字段为空值，已反馈项的决策为采纳。</summary>
  /// <param name="index">序号，驱动业务时间与患者姓名。</param>
  /// <param name="processed">是否已反馈处理结果。</param>
  /// <returns>提醒明细行投影。</returns>
  private static RecognitionReminderDetailItem BuildReminderItem(int index, bool processed) => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionMatchItemId = Guid.NewGuid(),
    MatchCreatedTime = BusinessDate.AddHours(index),
    VisitType = VisitType.Outpatient,
    VisitSerialNo = $"V-{index}",
    ReceiverOrganizationCode = TrustedOrganization,
    ReceiverHospitalCode = TrustedHospital,
    ReceiverBranchCode = TrustedBranch,
    SourceOrganizationCode = SourceOrganization,
    SourceHospitalCode = SourceHospital,
    SourceBranchCode = SourceBranch,
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "检验",
    GroupName = "血常规",
    StandardProjectCode = "STD-001",
    StandardItemName = "血常规",
    PatientName = $"患者{index}",
    IdentityDocumentNo = "110101199001011234",
    ProcessingTime = processed ? BusinessDate.AddHours(index) : null,
    Decision = processed ? RecognitionResult.Adopted : null,
    ProcessingDeptId = processed ? "DPT-1" : null,
    ProcessingDeptName = processed ? "检验科" : null,
    ProcessingDoctorId = processed ? "DOC-1" : null,
    ProcessingDoctorName = processed ? "李医生" : null
  };

  /// <summary>构建一条采纳明细行。</summary>
  /// <param name="index">序号，驱动业务时间与患者姓名。</param>
  /// <returns>采纳明细行投影。</returns>
  private static RecognitionAdoptionDetailItem BuildAdoptionItem(int index) => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionMatchItemId = Guid.NewGuid(),
    MatchCreatedTime = BusinessDate.AddHours(-1),
    VisitType = VisitType.Outpatient,
    VisitSerialNo = $"V-{index}",
    ReceiverOrganizationCode = TrustedOrganization,
    ReceiverHospitalCode = TrustedHospital,
    ReceiverBranchCode = TrustedBranch,
    SourceOrganizationCode = SourceOrganization,
    SourceHospitalCode = SourceHospital,
    SourceBranchCode = SourceBranch,
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "检验",
    GroupName = "血常规",
    StandardProjectCode = "STD-001",
    StandardItemName = "血常规",
    PatientName = $"患者{index}",
    IdentityDocumentNo = "110101199001011234",
    ProcessingTime = BusinessDate.AddHours(index),
    ProcessingDeptId = "DPT-1",
    ProcessingDeptName = "检验科",
    ProcessingDoctorId = "DOC-1",
    ProcessingDoctorName = "李医生",
    EstimatedSavingAmount = 120m
  };

  /// <summary>构建一条不采纳明细行。</summary>
  /// <param name="index">序号，驱动业务时间。</param>
  /// <param name="reason">不采纳原因代码。</param>
  /// <param name="description">补充说明。</param>
  /// <returns>不采纳明细行投影。</returns>
  private static RecognitionNonAdoptionDetailItem BuildNonAdoptionItem(
    int index, RecognitionNonAdoptionReason reason = RecognitionNonAdoptionReason.CurrentConditionMismatch, string? description = "补充说明") => new()
    {
      RecognitionMatchRecordId = Guid.NewGuid(),
      RecognitionMatchItemId = Guid.NewGuid(),
      MatchCreatedTime = BusinessDate.AddHours(-1),
      VisitType = VisitType.Outpatient,
      VisitSerialNo = $"V-{index}",
      ReceiverOrganizationCode = TrustedOrganization,
      ReceiverHospitalCode = TrustedHospital,
      ReceiverBranchCode = TrustedBranch,
      SourceOrganizationCode = SourceOrganization,
      SourceHospitalCode = SourceHospital,
      SourceBranchCode = SourceBranch,
      ItemType = MedicalItemType.Laboratory,
      CategoryName = "检验",
      GroupName = "血常规",
      StandardProjectCode = "STD-001",
      StandardItemName = "血常规",
      PatientName = $"患者{index}",
      IdentityDocumentNo = "110101199001011234",
      ProcessingTime = BusinessDate.AddHours(index),
      ProcessingDeptId = "DPT-1",
      ProcessingDeptName = "检验科",
      ProcessingDoctorId = "DOC-1",
      ProcessingDoctorName = "李医生",
      NonAdoptionReason = reason,
      NonAdoptionDescription = description
    };

  /// <summary>构建一条引用明细行。</summary>
  /// <param name="index">序号，驱动互认时间。</param>
  /// <returns>引用明细行投影。</returns>
  private static RecognitionReferenceDetailItem BuildReferenceItem(int index) => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionMatchItemId = Guid.NewGuid(),
    MatchCreatedTime = BusinessDate.AddHours(-2),
    VisitType = VisitType.Outpatient,
    VisitSerialNo = $"V-{index}",
    ReceiverOrganizationCode = TrustedOrganization,
    ReceiverHospitalCode = TrustedHospital,
    ReceiverBranchCode = TrustedBranch,
    SourceOrganizationCode = SourceOrganization,
    SourceHospitalCode = SourceHospital,
    SourceBranchCode = SourceBranch,
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "检验",
    GroupName = "血常规",
    StandardProjectCode = "STD-001",
    StandardItemName = "血常规",
    PatientName = $"患者{index}",
    IdentityDocumentNo = "110101199001011234",
    ProcessingTime = BusinessDate.AddHours(index),
    ProcessingDeptId = "DPT-1",
    ProcessingDeptName = "检验科",
    ProcessingDoctorId = "DOC-1",
    ProcessingDoctorName = "李医生",
    ReferenceTime = BusinessDate.AddHours(index + 1),
    ReferenceDeptId = "RPT-1",
    ReferenceDeptName = "病历科",
    ReferenceDoctorId = "DOC-2",
    ReferenceDoctorName = "王医生"
  };

  /// <summary>构建带组织、医院与院区数据的组织服务替身。</summary>
  /// <param name="organizations">组织编码集合。</param>
  /// <param name="hospitals">组织到医院的映射。</param>
  /// <param name="branches">医院到院区的映射。</param>
  /// <returns>可按层级记录调用次数的组织服务替身。</returns>
  private static RecordingOrganizationAppService CreateOrganizationService(
    IReadOnlyList<string>? organizations = null,
    IReadOnlyDictionary<string, string[]>? hospitals = null,
    IReadOnlyDictionary<string, string[]>? branches = null) =>
    new(
      organizations ?? [TrustedOrganization],
      hospitals ?? new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital] },
      branches ?? new Dictionary<string, string[]> { [TrustedHospital] = [TrustedBranch] });

  /// <summary>
  /// 按层级记录调用次数的组织服务替身：只实现组织路径解析使用的三类读取。
  /// </summary>
  private sealed class RecordingOrganizationAppService : IOrganizationAppService
  {
    /// <summary>按组织编码索引的医院编码集合。</summary>
    private readonly IReadOnlyDictionary<string, string[]> hospitals;

    /// <summary>按医院编码索引的院区编码集合。</summary>
    private readonly IReadOnlyDictionary<string, string[]> branches;

    /// <summary>启用的组织集合。</summary>
    private readonly List<OrganizationDto> organizations;

    /// <summary>
    /// 用组织、医院与院区数据构造替身。
    /// </summary>
    /// <param name="organizationCodes">启用的组织编码集合。</param>
    /// <param name="hospitals">组织到医院的映射。</param>
    /// <param name="branches">医院到院区的映射。</param>
    public RecordingOrganizationAppService(
      IReadOnlyList<string> organizationCodes,
      IReadOnlyDictionary<string, string[]> hospitals,
      IReadOnlyDictionary<string, string[]> branches)
    {
      this.hospitals = hospitals;
      this.branches = branches;
      organizations = [.. organizationCodes.Select(code => new OrganizationDto { Id = code, Name = $"{OrganizationNamePrefix}-{code}", IsValid = true })];
    }

    /// <summary>组织全量读取次数。</summary>
    public int OrganizationReadCount { get; private set; }

    /// <summary>医院读取次数；每个不同组织一次。</summary>
    public int HospitalReadCount { get; private set; }

    /// <summary>院区读取次数；每家不同医院一次。</summary>
    public int BranchReadCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync()
    {
      OrganizationReadCount++;
      return Task.FromResult<IEnumerable<OrganizationDto>>(organizations);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest request)
    {
      HospitalReadCount++;
      return Task.FromResult<IEnumerable<HospitalDto>>(hospitals.TryGetValue(request.OrgId, out string[]? codes)
        ? [.. codes.Select(code => new HospitalDto { Id = code, Name = $"{HospitalNamePrefix}-{code}", OrgId = request.OrgId, IsValid = true })]
        : []);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest request)
    {
      BranchReadCount++;
      return Task.FromResult<IEnumerable<BranchDto>>(branches.TryGetValue(request.HosId, out string[]? codes)
        ? [.. codes.Select(code => new BranchDto
        {
          Id = code,
          Name = $"{BranchNamePrefix}-{code}",
          HosId = request.HosId,
          OrgId = TrustedOrganization,
          IsValid = true
        })]
        : []);
    }
  }
}
