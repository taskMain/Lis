using System.Reflection;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 报告管理端查询的应用与查询映射层校验
/// （矩阵 V61 至 V74、V90 在这些层级的可测部分）。
/// </summary>
/// <remarks>
/// 全部用例使用查询仓储替身、组织服务替身与可信请求上下文替身隔离数据库与外部服务，
/// 验证可观察结果：返回的读模型内容、分页信息、下推给仓储的筛选条件、外部服务调用次数与拒绝时机。
/// 真实数据库上的排序、分页与执行计划证据由阶段 12 的宿主验收负责，不在本文件内验证。
/// </remarks>
public sealed class Stage4QueryTests
{
  /// <summary>可信组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";

  /// <summary>可信医院编码。</summary>
  private const string TrustedHospital = "HOS-A";

  /// <summary>可信院区编码。</summary>
  private const string TrustedBranch = "BRH-A";

  /// <summary>同组织下的第二家医院编码。</summary>
  private const string SecondHospital = "HOS-B";

  /// <summary>第二家医院下的院区编码。</summary>
  private const string SecondBranch = "BRH-B";

  /// <summary>另一院区编码；用于验证院区必须属于可信医院。</summary>
  private const string OtherBranch = "BRH-C";

  /// <summary>可信操作人标识。</summary>
  private static readonly Guid TrustedOperId = Guid.Parse("2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e");

  /// <summary>
  /// V90：分页窗口校验在第一次仓储访问之前完成，非法窗口按业务拒绝且零仓储调用。
  /// </summary>
  /// <remarks>
  /// 四条非法窗口各验一次：页码小于 1 与大于 1 的两组、页容量小于 1 与超过 200；
  /// 断言仓储的计数与取页方法都没有被调用，使失败时机可核对。
  /// </remarks>
  [Theory]
  [InlineData(0, 20, "页码不能小于 1")]
  [InlineData(-1, 20, "页码不能小于 1")]
  [InlineData(1, 0, "页容量必须在 1 到 200 之间")]
  [InlineData(1, 201, "页容量必须在 1 到 200 之间")]
  public async Task Invalid_page_window_is_rejected_before_any_repository_access(int pageIndex, int pageSize, string expectedMessage)
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    System.ComponentModel.DataAnnotations.ValidationException error =
      await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
        () => service.QueryMedicalReportListAsync(new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize } }));

    Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    // 窗口校验必须发生在第一次仓储访问之前：计数与取页方法一次都没有被调用。
    Assert.Equal(0, repository.CountCalls);
    Assert.Equal(0, repository.PageCalls);
  }

  /// <summary>
  /// V90：取值域内的窗口放行并写入仓储，窗口起点按页码换算。
  /// </summary>
  [Theory]
  [InlineData(1, 1)]
  [InlineData(200, 200)]
  public async Task Boundary_page_window_is_accepted(int pageIndex, int pageSize)
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    PageResultDto<MedicalReportListReadModel> result = await service.QueryMedicalReportListAsync(
      new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = pageIndex, PageSize = pageSize } });

    Assert.Equal(pageIndex, result.Page.PageIndex);
    Assert.Equal(pageSize, result.Page.PageSize);
    Assert.Equal((pageIndex - 1) * pageSize, repository.LastSkipCount);
    Assert.Equal(pageSize, repository.LastPageSize);
  }

  /// <summary>
  /// V61、V68：合法窗口先取总数再取当页，分页信息与请求一致。
  /// </summary>
  [Fact]
  public async Task Valid_page_window_reads_total_then_page_and_echoes_request()
  {
    FakeQueryRepository repository = new();
    repository.ReportItems.AddRange([.. Enumerable.Range(1, 5).Select(index => BuildReportItem(index))]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    PageResultDto<MedicalReportListReadModel> result = await service.QueryMedicalReportListAsync(
      new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = 2, PageSize = 2 } });

    Assert.Equal(1, repository.CountCalls);
    Assert.Equal(1, repository.PageCalls);
    // 页码二与页容量二对应窗口起点 2。
    Assert.Equal(2, repository.LastSkipCount);
    Assert.Equal(2, repository.LastPageSize);
    Assert.Equal(2, result.Items.Count);
    Assert.Equal(2, result.Page.PageIndex);
    Assert.Equal(2, result.Page.PageSize);
    Assert.Equal(5, result.Page.TotalCount);
  }

  /// <summary>
  /// V63：时间范围按左闭右开下推：起始日含边界，结束日取次日零点作为不含上界。
  /// </summary>
  [Fact]
  public async Task Report_time_range_is_pushed_down_as_half_open_interval()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest
    {
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 },
      ReportDateFrom = new DateOnly(2026, 9, 1),
      ReportDateTo = new DateOnly(2026, 9, 30)
    });

    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified), repository.LastFilter!.ReportTimeFrom);
    // 结束日按次日零点下推，因此 9 月 30 日当天的数据全部命中、10 月 1 日零点起被排除。
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified), repository.LastFilter.ReportTimeTo);
  }

  /// <summary>
  /// V63：下推的两个时间边界不携带时区，避免数据库驱动按本机时区再次换算。
  /// </summary>
  /// <remarks>
  /// 请求侧提交的是日历日；边界值若带 <see cref="DateTimeKind.Utc"/> 或 <see cref="DateTimeKind.Local"/>，
  /// 数据库驱动写入 <c>timestamp without time zone</c> 列时会按本机时区换算，使结束日的上界取到当天 08:00 而不是次日零点。
  /// 因此边界的时刻值与 <see cref="DateTimeKind"/> 都必须正确。
  /// </remarks>
  [Fact]
  public async Task Report_time_range_bounds_carry_no_time_zone()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest
    {
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 },
      ReportDateFrom = new DateOnly(2026, 9, 1),
      ReportDateTo = new DateOnly(2026, 9, 30)
    });

    Assert.Equal(DateTimeKind.Unspecified, repository.LastFilter!.ReportTimeFrom!.Value.Kind);
    Assert.Equal(DateTimeKind.Unspecified, repository.LastFilter.ReportTimeTo!.Value.Kind);
    Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0), repository.LastFilter.ReportTimeFrom!.Value);
    Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0), repository.LastFilter.ReportTimeTo!.Value);
  }

  /// <summary>
  /// V64、V65：患者证件号码与姓名按规范形式处理成前缀条件；输入缺失或处理后为空时不施加该条件。
  /// </summary>
  [Theory]
  [InlineData(" 110101199001011234x ", null, "110101199001011234X", null)]
  [InlineData(null, " 张三 ", null, "张三")]
  [InlineData("   ", "   ", null, null)]
  [InlineData("110101", null, "110101", null)]
  public async Task Patient_filters_are_normalized_and_blank_input_is_dropped(
    string? documentNo, string? patientName, string? expectedDocumentNo, string? expectedPatientName)
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest
    {
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 },
      IdentityDocumentNo = documentNo,
      PatientName = patientName
    });

    Assert.Equal(expectedDocumentNo, repository.LastFilter!.IdentityDocumentNoPrefix);
    Assert.Equal(expectedPatientName, repository.LastFilter.PatientNamePrefix);
  }

  /// <summary>
  /// V66：报告类型与报告单号筛选按请求下推，报告单号去除首尾空白。
  /// </summary>
  [Fact]
  public async Task Report_type_and_number_filters_are_pushed_down()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest
    {
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 },
      ReportType = MedicalReportType.Examination,
      ReportNo = " EXM-1 "
    });

    Assert.Equal(MedicalReportType.Examination, repository.LastFilter!.ReportType);
    Assert.Equal("EXM-1", repository.LastFilter.ReportNo);
  }

  /// <summary>
  /// V61：平台管理员入口的组织、医院与院区按请求使用，并下推为筛选条件。
  /// </summary>
  [Fact]
  public async Task Platform_entry_pushes_requested_scope_into_the_filter()
  {
    FakeQueryRepository repository = new();
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization],
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SecondHospital] = [SecondBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest
    {
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 },
      OrganizationCode = TrustedOrganization,
      HospitalCode = SecondHospital,
      BranchCode = SecondBranch
    });

    Assert.Equal(TrustedOrganization, repository.LastFilter!.OrganizationCode);
    Assert.Equal(SecondHospital, repository.LastFilter.HospitalCode);
    Assert.Equal(SecondBranch, repository.LastFilter.BranchCode);
  }

  /// <summary>
  /// V69：名称回填一次读取的组织与医院数量不随返回行数增长。
  /// </summary>
  /// <remarks>
  /// 同一组织同一医院上的多行数据，无论 10 行还是 100 行，组织读取一次、医院读取一次、院区读取一次；
  /// 替身按层级记录调用次数，因此"按行读取"会直接体现在计数上。
  /// </remarks>
  [Theory]
  [InlineData(10)]
  [InlineData(100)]
  public async Task Name_backfill_call_counts_do_not_grow_with_row_count(int rowCount)
  {
    FakeQueryRepository repository = new();
    repository.ReportItems.AddRange([.. Enumerable.Range(1, rowCount).Select(index => BuildReportItem(index))]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    PageResultDto<MedicalReportListReadModel> result = await service.QueryMedicalReportListAsync(
      new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = 1, PageSize = 200 } });

    Assert.Equal(rowCount, result.Items.Count);
    Assert.Equal(1, organizationService.OrganizationReadCount);
    Assert.Equal(1, organizationService.HospitalReadCount);
    Assert.Equal(1, organizationService.BranchReadCount);
    // 名称由服务端按外部组织服务的当前值回填：读模型返回的名称必须与外部服务返回的名称逐字一致。
    Assert.All(result.Items, item =>
    {
      Assert.Equal($"{OrganizationName}-{item.OrganizationCode}", item.OrganizationName);
      Assert.Equal($"{HospitalName}-{item.HospitalCode}", item.HospitalName);
      Assert.Equal($"{BranchName}-{item.BranchCode}", item.BranchName);
    });
  }

  /// <summary>
  /// V69：当页涉及同一组织下的两家不同医院时，医院与院区读取各按不同医院数量进行。
  /// </summary>
  [Fact]
  public async Task Name_backfill_reads_branches_once_per_distinct_hospital()
  {
    FakeQueryRepository repository = new();
    repository.ReportItems.AddRange(
    [
      BuildReportItem(1),
      BuildReportItem(2, hospitalCode: SecondHospital, branchCode: SecondBranch),
      BuildReportItem(3, hospitalCode: SecondHospital, branchCode: SecondBranch)
    ]);
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization],
      hospitals: new Dictionary<string, string[]> { [TrustedOrganization] = [TrustedHospital, SecondHospital] },
      branches: new Dictionary<string, string[]>
      {
        [TrustedHospital] = [TrustedBranch],
        [SecondHospital] = [SecondBranch]
      });
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    await service.QueryMedicalReportListAsync(new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = 1, PageSize = 20 } });

    // 组织一次；该组织的医院一次（按组织去重）；两家不同医院各一次院区读取。
    Assert.Equal(1, organizationService.OrganizationReadCount);
    Assert.Equal(1, organizationService.HospitalReadCount);
    Assert.Equal(2, organizationService.BranchReadCount);
  }

  /// <summary>
  /// V69：外部组织服务不可用时整次查询失败，不降级为空名称或编码。
  /// </summary>
  [Fact]
  public async Task Name_backfill_failure_fails_the_whole_query()
  {
    FakeQueryRepository repository = new();
    repository.ReportItems.Add(BuildReportItem(1));
    RecordingOrganizationAppService organizationService = CreateOrganizationService(
      organizations: [TrustedOrganization],
      hospitals: new Dictionary<string, string[]>());
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryMedicalReportListAsync(new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = 1, PageSize = 20 } }));

    Assert.Equal("业务拒绝：报告来源医院不存在、已停用或不属于该组织。", error.Message);
  }

  /// <summary>
  /// V70：无匹配时返回空集合且成功，分页信息保留，不报业务异常。
  /// </summary>
  [Fact]
  public async Task No_match_returns_empty_page_with_total_count()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    PageResultDto<MedicalReportListReadModel> result = await service.QueryMedicalReportListAsync(
      new ReportListQueryRequest { Page = new PageRequestDto { PageIndex = 1, PageSize = 20 } });

    Assert.Empty(result.Items);
    Assert.Equal(0, result.Page.TotalCount);
    // 当页为空时不读取外部组织服务，也不产生任何归属解析开销。
    Assert.Equal(1, repository.CountCalls);
  }

  /// <summary>
  /// V62：医院管理员入口的组织与医院取自可信上下文，院区必须属于可信医院。
  /// </summary>
  [Fact]
  public async Task Branch_entry_uses_trusted_scope_and_rejects_branch_outside_trusted_hospital()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    await service.QueryBranchMedicalReportListAsync(new BranchReportListQueryRequest
    {
      BranchCode = TrustedBranch,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    });

    Assert.Equal(TrustedOrganization, repository.LastFilter!.OrganizationCode);
    Assert.Equal(TrustedHospital, repository.LastFilter.HospitalCode);
    Assert.Equal(TrustedBranch, repository.LastFilter.BranchCode);

    // 不属于可信医院的院区拒绝，且不返回其他医院的数据、也不降级为空集合。
    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchMedicalReportListAsync(new BranchReportListQueryRequest
      {
        BranchCode = "BRH-UNKNOWN",
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));
    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", error.Message);
  }

  /// <summary>
  /// V62：医院管理员入口在可信上下文缺失时阻断，且不发生仓储访问。
  /// </summary>
  [Fact]
  public async Task Branch_entry_blocks_when_trusted_scope_is_unavailable()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    // 把可信上下文置为组织与医院都缺失，页面与入口都应阻断在可信范围解析处。
    TrustedRequestContext.Use(null, null, null, TrustedOperId.ToString());
    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchMedicalReportListAsync(new BranchReportListQueryRequest
      {
        BranchCode = TrustedBranch,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      }));

    Assert.Equal("无法读取当前登录用户信息。", error.Message);
    Assert.Equal(0, repository.CountCalls);
  }

  /// <summary>
  /// V71：版本列表按版本序号返回全部版本，并区分当前有效版本、已被后续版本替代与报告已作废状态。
  /// </summary>
  [Fact]
  public async Task Version_list_returns_all_versions_with_the_two_version_flags()
  {
    Guid reportId = Guid.NewGuid();
    Guid currentVersionId = Guid.NewGuid();
    FakeQueryRepository repository = new();
    repository.ReportScopes[reportId] = new MedicalReportScopeItem
    {
      ReportId = reportId, OrganizationCode = TrustedOrganization, HospitalCode = TrustedHospital, BranchCode = TrustedBranch
    };
    repository.VersionItems.AddRange(
    [
      new MedicalReportVersionItem
      {
        ReportVersionId = Guid.NewGuid(), ReportId = reportId, ReportType = MedicalReportType.Laboratory, VersionSequence = 1,
        SourceModifiedTime = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Unspecified),
        PlatformReceivedTime = new DateTime(2026, 9, 20, 11, 0, 0, DateTimeKind.Unspecified),
        ReportDoctorName = "李医生", ReviewDoctorName = "王医生", PdfFileName = "v1.pdf",
        IsCurrentVersion = false, IsSuperseded = true, ReportStatus = MedicalReportLifecycleStatus.Effective
      },
      new MedicalReportVersionItem
      {
        ReportVersionId = currentVersionId, ReportId = reportId, ReportType = MedicalReportType.Laboratory, VersionSequence = 2,
        SourceModifiedTime = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Unspecified),
        PlatformReceivedTime = new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Unspecified),
        ReportDoctorName = "李医生", ReviewDoctorName = "王医生", PdfFileName = "v2.pdf",
        IsCurrentVersion = true, IsSuperseded = false, ReportStatus = MedicalReportLifecycleStatus.Voided
      }
    ]);
    repository.LaboratoryContents[repository.VersionItems[0].ReportVersionId] = new LaboratoryReportContentItem
    {
      ContentId = Guid.NewGuid(), SourceSpecimenNo = "S-1", SpecimenTypeCode = "T-1", SpecimenTypeName = "血清",
      TestingCompletedTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified),
      InspectorId = "U-1", InspectorName = "检验人甲", ReportRemark = "版本一备注"
    };
    repository.LaboratoryResultItems[repository.VersionItems[0].ReportVersionId] =
    [
      BuildResultItem(1, "检测人甲"), BuildResultItem(2, "检测人乙"), BuildResultItem(3, "检测人甲")
    ];

    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());
    IReadOnlyList<MedicalReportVersionListReadModel> versions = await service.QueryMedicalReportVersionListAsync(
      new MedicalReportVersionListQueryRequest { ReportId = reportId });

    Assert.Equal(2, versions.Count);
    Assert.Equal([1, 2], versions.Select(version => version.VersionSequence).ToArray());
    Assert.False(versions[0].IsCurrentVersion);
    Assert.True(versions[0].IsSuperseded);
    Assert.True(versions[1].IsCurrentVersion);
    Assert.False(versions[1].IsSuperseded);
    Assert.Equal(MedicalReportLifecycleStatus.Voided, versions[1].ReportStatus);
    Assert.Equal("已作废", versions[1].ReportStatusText);
    // 责任人员为结构化字段：报告医生与审核医生来自版本自身；检验版本另带检验人名称与去重后的明细检测人。
    Assert.Equal("李医生", versions[0].ReportDoctorName);
    Assert.Equal("王医生", versions[0].ReviewDoctorName);
    Assert.Equal("检验人甲", versions[0].InspectorName);
    Assert.Equal("检测人甲、检测人乙", versions[0].DetailInspectors);
    Assert.Equal("版本一备注", versions[0].SourceReportRemark);
    Assert.Null(versions[0].ExaminerName);
    Assert.Equal("v1.pdf", versions[0].PdfFileName);
  }

  /// <summary>
  /// V71：报告不存在时报业务事实，不返回空集合冒充不存在。
  /// </summary>
  [Fact]
  public async Task Version_list_rejects_when_report_is_missing()
  {
    FakeQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryMedicalReportVersionListAsync(new MedicalReportVersionListQueryRequest { ReportId = Guid.NewGuid() }));

    Assert.Equal("业务拒绝：报告不存在。", error.Message);
  }

  /// <summary>
  /// V71：报告不在可信范围内时拒绝，不泄漏其他医院或院区的数据。
  /// </summary>
  [Fact]
  public async Task Version_list_rejects_report_outside_trusted_scope()
  {
    Guid reportId = Guid.NewGuid();
    FakeQueryRepository repository = new();
    repository.ReportScopes[reportId] = new MedicalReportScopeItem
    {
      ReportId = reportId, OrganizationCode = TrustedOrganization, HospitalCode = TrustedHospital, BranchCode = OtherBranch
    };
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryMedicalReportVersionListAsync(new MedicalReportVersionListQueryRequest { ReportId = reportId }));

    Assert.Equal("业务拒绝：报告不在当前可信组织、医院与院区范围内。", error.Message);
  }

  /// <summary>
  /// V72、V73、V74：版本详情按报告类型返回检验或检查内容之一，枚举文本由服务端派生，联系电话脱敏。
  /// </summary>
  [Theory]
  [InlineData(MedicalReportType.Laboratory)]
  [InlineData(MedicalReportType.Examination)]
  public async Task Version_detail_returns_content_for_the_report_type_with_masked_phone(MedicalReportType reportType)
  {
    (Guid reportId, Guid versionId, FakeQueryRepository repository) = BuildDetailRepository(reportType);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    MedicalReportVersionDetailQueryReadModel detail = await service.QueryMedicalReportVersionDetailAsync(
      new MedicalReportVersionDetailQueryRequest { ReportId = reportId, ReportVersionId = versionId });

    Assert.Equal(reportType, detail.ReportType);
    Assert.Equal(
      reportType == MedicalReportType.Laboratory ? "检验报告" : "检查报告",
      detail.ReportTypeText);
    Assert.Equal("R-1", detail.ReportNo);
    Assert.Equal(1, detail.VersionSequence);
    Assert.Equal("张医生", detail.ReportDoctorName);
    Assert.Equal("孙医生", detail.ReviewDoctorName);
    Assert.Equal("v1.pdf", detail.File.FileName);

    // 公共信息：姓名与证件号码完整返回，联系电话脱敏。
    Assert.Equal("张三", detail.Content.Common.PatientName);
    Assert.Equal("110101199001011234", detail.Content.Common.IdentityDocumentNo);
    Assert.Equal("*******1234", detail.Content.Common.PatientPhoneNumber);
    Assert.Equal(VisitType.Outpatient, detail.Content.Common.VisitType);
    Assert.Equal("门诊", detail.Content.Common.VisitTypeText);

    // 内容按报告类型二选一。
    if (reportType == MedicalReportType.Laboratory)
    {
      Assert.NotNull(detail.Content.LaboratoryContent);
      Assert.Null(detail.Content.ExaminationContent);
      Assert.Equal("检验人甲", detail.InspectorName);
      Assert.Equal("S-1", detail.Content.LaboratoryContent!.SourceSpecimenNo);
      LaboratoryResultItemReadModel item = Assert.Single(detail.Content.LaboratoryContent.Results);
      Assert.Equal(LaboratoryResultType.Numeric, item.ResultType);
      Assert.Equal("数值型", item.ResultTypeText);
      LaboratoryBacteriaResultReadModel bacteria = Assert.Single(detail.Content.LaboratoryContent.BacteriaResults);
      Assert.Equal("SD-1", Assert.Single(bacteria.Susceptibilities).SourceDetailKey);
    }
    else
    {
      Assert.NotNull(detail.Content.ExaminationContent);
      Assert.Null(detail.Content.LaboratoryContent);
      Assert.Equal("检查医生甲", detail.ExaminerName);
      Assert.Equal("所见原文", detail.Content.ExaminationContent!.Findings);
      Assert.Equal("有影像", detail.Content.ExaminationContent.SourceImageStatusText);
      ExaminationItemReadModel item = Assert.Single(detail.Content.ExaminationContent.Items);
      Assert.Equal("胸部 CT", item.SourceProjectName);
      Assert.Equal("胸部", Assert.Single(item.Sites).SiteName);
    }
  }

  /// <summary>
  /// V72：版本不存在或不属于该报告时拒绝，不返回另一支内容冒充存在。
  /// </summary>
  [Fact]
  public async Task Version_detail_rejects_version_not_belonging_to_report()
  {
    (Guid reportId, _, FakeQueryRepository repository) = BuildDetailRepository(MedicalReportType.Laboratory);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryMedicalReportVersionDetailAsync(new MedicalReportVersionDetailQueryRequest
      {
        ReportId = reportId,
        ReportVersionId = Guid.NewGuid()
      }));

    Assert.Equal("业务拒绝：报告版本不存在或不属于该报告。", error.Message);
  }

  /// <summary>
  /// V74：来源未提供联系电话时返回空值，不补造脱敏串。
  /// </summary>
  [Fact]
  public async Task Missing_phone_number_stays_null()
  {
    (Guid reportId, Guid versionId, FakeQueryRepository repository) = BuildDetailRepository(MedicalReportType.Laboratory);
    repository.VersionCommons[versionId] = repository.VersionCommons[versionId] with { PatientPhoneNumber = "  " };
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateOrganizationService());

    MedicalReportVersionDetailQueryReadModel detail = await service.QueryMedicalReportVersionDetailAsync(
      new MedicalReportVersionDetailQueryRequest { ReportId = reportId, ReportVersionId = versionId });

    Assert.Null(detail.Content.Common.PatientPhoneNumber);
  }

  /// <summary>
  /// 构建查询应用服务，并把可信请求上下文设置为固定取值。
  /// </summary>
  /// <param name="repository">查询仓储替身。</param>
  /// <param name="organizationService">组织服务替身。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(
    FakeQueryRepository repository, RecordingOrganizationAppService organizationService)
  {
    TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId.ToString());
    return new MedicalRecognitionReportQueryAppService(repository, organizationService, new StubUserAppService());
  }

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

  /// <summary>构建一条报告列表行。</summary>
  /// <param name="index">序号，用于生成稳定标识与报告单号。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <returns>报告列表行投影。</returns>
  private static MedicalReportListItem BuildReportItem(
    int index, string hospitalCode = TrustedHospital, string branchCode = TrustedBranch) => new()
    {
      ReportId = Guid.NewGuid(),
      OrganizationCode = TrustedOrganization,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      ReportType = MedicalReportType.Laboratory,
      ReportNo = $"R-{index:D3}",
      ReportTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified).AddMinutes(index),
      CurrentVersionSequence = 1,
      PatientName = "张三",
      IdentityDocumentNo = "110101199001011234",
      Status = MedicalReportLifecycleStatus.Effective
    };

  /// <summary>构建一条普通检验结果行。</summary>
  /// <param name="displayOrder">展示序号。</param>
  /// <param name="inspectorName">检测人名称。</param>
  /// <returns>普通检验结果投影。</returns>
  private static LaboratoryResultItemView BuildResultItem(int displayOrder, string inspectorName) => new()
  {
    SourceDetailKey = $"D-{displayOrder}",
    SourceProjectName = $"项目{displayOrder}",
    SourceResultText = "1.0",
    ResultType = LaboratoryResultType.Numeric,
    DisplayOrder = displayOrder,
    InspectorName = inspectorName
  };

  /// <summary>
  /// 构建版本详情用例所需的仓储数据，按报告类型只准备该类型的专项内容。
  /// </summary>
  /// <param name="reportType">报告类型。</param>
  /// <returns>报告标识、版本标识与已装载数据的仓储替身。</returns>
  private static (Guid ReportId, Guid VersionId, FakeQueryRepository Repository) BuildDetailRepository(MedicalReportType reportType)
  {
    Guid reportId = Guid.NewGuid();
    Guid versionId = Guid.NewGuid();
    FakeQueryRepository repository = new();
    repository.ReportScopes[reportId] = new MedicalReportScopeItem
    {
      ReportId = reportId, OrganizationCode = TrustedOrganization, HospitalCode = TrustedHospital, BranchCode = TrustedBranch
    };
    repository.VersionDetails[(reportId, versionId)] = new MedicalReportVersionDetailItem
    {
      ReportVersionId = versionId,
      ReportId = reportId,
      ReportType = reportType,
      ReportNo = "R-1",
      VersionSequence = 1,
      SourceModifiedTime = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Unspecified),
      PlatformReceivedTime = new DateTime(2026, 9, 20, 11, 0, 0, DateTimeKind.Unspecified),
      ReportDoctorName = "张医生",
      ReviewDoctorName = "孙医生",
      PdfFileName = "v1.pdf"
    };
    repository.VersionCommons[versionId] = new MedicalRecognitionReportDetailCommon
    {
      PatientName = "张三",
      PatientGenderCode = "1",
      PatientBirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
      PatientPhoneNumber = "13800001234",
      IdentityDocumentTypeCode = "01",
      IdentityDocumentNo = "110101199001011234",
      VisitType = VisitType.Outpatient,
      VisitSerialNo = "V-1",
      SourceReportName = "报告名称",
      ApplicationTime = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Unspecified),
      ReportTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified)
    };

    if (reportType == MedicalReportType.Laboratory)
    {
      repository.LaboratoryContents[versionId] = new LaboratoryReportContentItem
      {
        ContentId = Guid.NewGuid(),
        SourceSpecimenNo = "S-1",
        SpecimenTypeCode = "T-1",
        SpecimenTypeName = "血清",
        TestingCompletedTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified),
        InspectorId = "U-1",
        InspectorName = "检验人甲"
      };
      repository.LaboratoryResultItems[versionId] = [BuildResultItem(1, "检测人甲")];
      Guid bacteriaResultId = Guid.NewGuid();
      repository.BacteriaItems[versionId] =
      [
        new LaboratoryBacteriaResultItem
        {
          BacteriaResultId = bacteriaResultId,
          SourceResultText = "检出",
          DetectionConclusion = "检出大肠埃希菌",
          SourceOrganismName = "大肠埃希菌"
        }
      ];
      repository.SusceptibilityItems[bacteriaResultId] =
      [
        new LaboratorySusceptibilityItem { SourceDetailKey = "SD-1", DrugName = "青霉素", SourceConclusionText = "敏感", DisplayOrder = 1 }
      ];
    }
    else
    {
      repository.ExaminationContents[versionId] = new ExaminationReportContentItem
      {
        ContentId = Guid.NewGuid(),
        Findings = "所见原文",
        Conclusion = "结论原文",
        SourceDiagnosisName = "诊断名称",
        ExaminationTime = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Unspecified),
        ExaminerId = "DOC-1",
        ExaminerName = "检查医生甲",
        SourceImageStatus = SourceImageStatus.Available,
        ImageAccessUrl = "http://image/1"
      };
      Guid itemId = Guid.NewGuid();
      repository.ExaminationItemViews[versionId] =
      [
        new ExaminationItemView { ItemId = itemId, SourceProjectName = "胸部 CT", SourceProjectCode = "EP-1" }
      ];
      repository.ExaminationSiteViews[itemId] = [new ExaminationSiteView { SiteName = "胸部", SourceSiteCode = "ST-1" }];
    }

    return (reportId, versionId, repository);
  }

  /// <summary>组织名称前缀；替身按编码拼出唯一名称，用于核对回填来源。</summary>
  private const string OrganizationName = "示范组织";

  /// <summary>医院名称前缀。</summary>
  private const string HospitalName = "示范医院";

  /// <summary>院区名称前缀。</summary>
  private const string BranchName = "示范院区";

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
      organizations = [.. organizationCodes.Select(code => new OrganizationDto { Id = code, Name = $"{OrganizationName}-{code}", IsValid = true })];
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
        ? [.. codes.Select(code => new HospitalDto { Id = code, Name = $"{HospitalName}-{code}", OrgId = request.OrgId, IsValid = true })]
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
          Name = $"{BranchName}-{code}",
          HosId = request.HosId,
          OrgId = FindOrganizationCodeOfHospital(request.HosId),
          IsValid = true
        })]
        : []);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByOrgIdAsync(QueryAllValidBranchByOrgIdRequest request) => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<OrganizationDto> GetOrganizationByIdAsync(GetOrganizationByIdRequest request) => Unsupported<OrganizationDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalAsync() => Unsupported<IEnumerable<HospitalDto>>();

    /// <inheritdoc/>
    public Task<HospitalDto> GetHospitalByIdAsync(GetHospitalByIdRequest request) => Unsupported<HospitalDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchAsync() => Unsupported<IEnumerable<BranchDto>>();

    /// <inheritdoc/>
    public Task<BranchDto> GetBranchByIdAsync(GetBranchByIdRequest request) => Unsupported<BranchDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptBySubApplicationIdAsync(QueryDeptBySubApplicationIdRequest request) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByOrgIdAsync(QueryDeptByOrgIdRequest request) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByHosIdAsync(QueryDeptByHosIdRequest request) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByBranchIdAsync(QueryDeptByBranchIdRequest request) => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByBranchIdAndCodeAsync(GetDeptByBranchIdAndCodeRequest request) => Unsupported<DeptDto>();

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByIdAsync(GetDeptByIdRequest request) => Unsupported<DeptDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryAllDeptAsync() => Unsupported<IEnumerable<DeptDto>>();

    /// <inheritdoc/>
    public Task<DeptWardDto> GetDeptWardByIdAsync(GetDeptWardByIdRequest request) => Unsupported<DeptWardDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByDeptAsync(QueryDeptWardByDeptRequest request) => Unsupported<IEnumerable<DeptWardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByWardAsync(QueryDeptWardByWardRequest request) => Unsupported<IEnumerable<DeptWardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardByBranchAsync(QueryWardByBranchRequest request) => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardBySubApplicationIdAsync(QueryWardBySubApplicationIdRequest request) => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<WardDto> GetWardByIdAsync(GetWardByIdRequest request) => Unsupported<WardDto>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryAllWardAsync() => Unsupported<IEnumerable<WardDto>>();

    /// <inheritdoc/>
    public Task<string> CreateOrganizationAsync(CreateOrganizationRequest request) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateOrganizationAsync(UpdateOrganizationRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteOrganizationAsync(DeleteOrganizationRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableOrganizationAsync(EnableOrganizationRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableOrganizationAsync(DisableOrganizationRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<string> CreateHospitalAsync(CreateHospitalRequest request) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateHospitalAsync(UpdateHospitalRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteHospitalAsync(DeleteHospitalRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableHospitalAsync(EnableHospitalRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableHospitalAsync(DisableHospitalRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<string> CreateBranchAsync(CreateBranchRequest request) => Unsupported<string>();

    /// <inheritdoc/>
    public Task<int> UpdateBranchAsync(UpdateBranchRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteBranchAsync(DeleteBranchRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableBranchAsync(EnableBranchRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableBranchAsync(DisableBranchRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateDeptAsync(CreateDeptRequest request) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptAsync(UpdateDeptRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptAsync(DeleteDeptRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptAsync(EnableDeptRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptAsync(DisableDeptRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptUpAsync(MoveDeptUpRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptDownAsync(MoveDeptDownRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateDeptWardAsync(CreateDeptWardRequest request) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptWardAsync(UpdateDeptWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptWardAsync(DeleteDeptWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptWardAsync(EnableDeptWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptWardAsync(DisableDeptWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateWardAsync(CreateWardRequest request) => Unsupported<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateWardAsync(UpdateWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DeleteWardAsync(DeleteWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> EnableWardAsync(EnableWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> DisableWardAsync(DisableWardRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardUpAsync(MoveWardUpRequest request) => Unsupported<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardDownAsync(MoveWardDownRequest request) => Unsupported<int>();

    /// <summary>
    /// 按医院编码反查其所属组织编码，使院区数据的父组织与医院数据一致。
    /// </summary>
    /// <param name="hospitalCode">医院编码。</param>
    /// <returns>该医院所属的组织编码；找不到时返回可信组织编码。</returns>
    private string FindOrganizationCodeOfHospital(string hospitalCode) =>
      hospitals.FirstOrDefault(pair => pair.Value.Contains(hospitalCode, StringComparer.Ordinal)).Key ?? TrustedOrganization;

    /// <summary>本替身未实现该外部组织服务成员。</summary>
    /// <typeparam name="TResult">成员返回类型。</typeparam>
    /// <returns>不会返回。</returns>
    /// <exception cref="NotSupportedException">该成员被调用时抛出。</exception>
    private static Task<TResult> Unsupported<TResult>() => throw new NotSupportedException("本替身只实现报告名称回填使用的组织、医院与院区读取。");
  }
}
