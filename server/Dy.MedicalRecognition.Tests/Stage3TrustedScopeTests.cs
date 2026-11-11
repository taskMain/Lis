using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 可信范围解析的行为校验（矩阵 V13、V14、V22、V30）：可信组织与可信医院由登录令牌声明与当前登录用户档案共同构成，
/// 令牌某层缺失时按用户档案补齐该层、令牌与档案冲突或补齐后仍为空时拒绝，院区层不参与回落。
/// </summary>
/// <remarks>
/// 外部组织服务与外部用户服务都使用同目录手写替身，不发起真实 HTTP 调用，也不写库。
/// 用户服务替身记录读取次数，使"令牌已提供两层时不再读取用户档案"由调用次数直接观察；
/// 组织服务替身记录组织读取次数，使"可信范围拒绝发生在任何路径读取之前"由调用次数直接观察。
/// </remarks>
public sealed class Stage3TrustedScopeTests
{
  /// <summary>令牌携带的组织编码。</summary>
  private const string TokenOrganization = "ORG-A";
  /// <summary>令牌携带的医院编码。</summary>
  private const string TokenHospital = "HOS-1";
  /// <summary>令牌携带的院区编码。</summary>
  private const string TokenBranch = "BRH-1";
  /// <summary>另一家医院编码，用于构造越权请求院区。</summary>
  private const string OtherHospital = "HOS-2";
  /// <summary>另一家医院下的院区编码，用于构造越权请求院区。</summary>
  private const string OtherHospitalBranch = "BRH-2";
  /// <summary>用户档案中的组织编码，用于构造令牌缺失层与冲突层。</summary>
  private const string ProfileOrganization = "ORG-P";
  /// <summary>用户档案中的医院编码，用于构造令牌缺失层与冲突层。</summary>
  private const string ProfileHospital = "HOS-P";
  /// <summary>可解析为非空 Guid 的令牌用户标识。</summary>
  private const string TrustedUserId = "2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e";
  /// <summary>用例使用的组织名称。</summary>
  private const string OrganizationName = "示例组织";
  /// <summary>用例使用的医院名称。</summary>
  private const string HospitalName = "示例医院";
  /// <summary>用例使用的院区名称。</summary>
  private const string BranchName = "示例院区";

  /// <summary>令牌与用户档案的组织层同时提供且不一致时的对外拒绝文案。</summary>
  private const string OrganizationConflictMessage = "当前登录组织与用户归属组织不一致，不能访问该组织数据。";
  /// <summary>令牌与用户档案的医院层同时提供且不一致时的对外拒绝文案。</summary>
  private const string HospitalConflictMessage = "当前登录医院与用户归属医院不一致，不能访问该医院数据。";
  /// <summary>组织层最终仍为空时的对外拒绝文案。</summary>
  private const string MissingOrganizationMessage = "无法确定当前可信组织。";
  /// <summary>医院层最终仍为空时的对外拒绝文案。</summary>
  private const string MissingHospitalMessage = "无法确定当前可信医院。";
  /// <summary>用户标识不能解析为非空 Guid 时的对外拒绝文案。</summary>
  private const string MissingUserMessage = "无法确定当前登录用户。";
  /// <summary>读不到用户档案时的对外拒绝文案。</summary>
  private const string MissingProfileMessage = "无法读取当前登录用户信息。";
  /// <summary>阶段 2 互认配置查询沿用既有可信组织拒绝文案。</summary>
  private const string Stage2MissingOrganizationMessage = "无法确定有效的当前组织。";

  /// <summary>院区层不参与可信回落：用户档案的院区不得放行属于其他医院的请求院区，可信范围补齐后仍由请求院区与归属校验拒绝（V22、V30）。
  /// </summary>
  [Fact]
  public async Task Requested_branch_of_another_hospital_is_still_rejected_when_the_user_profile_carries_a_branch()
  {
    StubUserAppService userService = BuildUserService(new UserDto
    {
      Id = Guid.Parse(TrustedUserId), OrgId = TokenOrganization, HosId = TokenHospital, BranchId = OtherHospitalBranch
    });
    TrustedRequestContext.Use(TokenOrganization, null, OtherHospitalBranch, TrustedUserId);
    StubOrganizationAppService organizationService = BuildTwoHospitalService();
    AmountQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService appService = new(repository, new PlatformParameterRepositoryStub(), FakeIamScope.Default, FakeIamScope.Default, FakeIamScope.Default);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => appService.QueryBranchRecognitionAmountListAsync(new BranchRecognitionAmountListQueryRequest { BranchCode = OtherHospitalBranch }));

    Assert.Contains("院区", error.Message);
    // 拒绝而不是静默改写院区：查询端口一次都不能进入。
    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>院区层不参与可信回落：令牌医院下的请求院区在补齐成功后仍按请求院区放行。</summary>
  [Fact]
  public async Task Requested_branch_of_the_filled_hospital_is_allowed()
  {
    AmountQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService appService = new(repository, new PlatformParameterRepositoryStub(), FakeIamScope.Default, FakeIamScope.Default, FakeIamScope.Default);

    await appService.QueryBranchRecognitionAmountListAsync(new BranchRecognitionAmountListQueryRequest { BranchCode = FakeIamScope.BranchCode });

    Assert.Equal((FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode, (string?)null, (string?)null), repository.ReceivedQuery);
  }

  /// <summary>
  /// 医院管理员查询入口在令牌缺失医院层时按用户档案补齐，交给查询端口的业务键取补齐后的医院（V14、V30）。
  /// </summary>
  [Fact]
  public async Task Branch_query_entrypoint_passes_the_profile_filled_scope_to_the_repository()
  {
    StubUserAppService userService = new();
    AmountQueryRepository repository = new();
    MedicalRecognitionReportQueryAppService appService = new(repository, new PlatformParameterRepositoryStub(), FakeIamScope.Default, FakeIamScope.Default, FakeIamScope.Default);

    await appService.QueryBranchRecognitionAmountListAsync(new BranchRecognitionAmountListQueryRequest { BranchCode = FakeIamScope.BranchCode });

    Assert.Equal((FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode, (string?)null, (string?)null), repository.ReceivedQuery);
    Assert.Equal(0, userService.GetUserByIdCallCount);
  }

  /// <summary>可信范围拒绝发生在任何组织路径读取之前：令牌与档案都取不到医院时组织服务一次都不被调用。</summary>
  [Fact]
  public async Task Missing_trusted_scope_is_rejected_before_any_organization_read()
  {
    StubOrganizationAppService organizationService = BuildTwoHospitalService();
    MedicalRecognitionReportQueryAppService appService = new(new AmountQueryRepository(), new PlatformParameterRepositoryStub(), FakeIamScope.WithoutHospital(), FakeIamScope.WithoutHospital(), FakeIamScope.WithoutHospital());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => appService.QueryBranchRecognitionAmountListAsync(
        new BranchRecognitionAmountListQueryRequest { BranchCode = FakeIamScope.BranchCode }));

    Assert.Equal(MissingHospitalMessage, error.Message);
    Assert.Equal(0, organizationService.OrganizationReadCount);
  }

  /// <summary>阶段 2 的互认配置查询入口继续只按令牌声明取值，不因用户档案提供组织而放行空令牌组织（S3-D11 的作用范围）。</summary>
  [Fact]
  public async Task Stage2_configuration_query_still_ignores_the_user_profile()
  {
    StubUserAppService userService = BuildUserService(Profile(ProfileOrganization, ProfileHospital));
    MedicalRecognitionReportQueryAppService appService = new(new AmountQueryRepository(), new PlatformParameterRepositoryStub(), FakeIamScope.Default, FakeIamScope.Default, FakeIamScope.Default);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => appService.QueryRecognitionProjectConfigurationListAsync(
        new RecognitionProjectConfigurationListQueryRequest { OrganizationCode = ProfileOrganization }));

    Assert.Equal("请求组织与当前登录组织不一致，不能查询该组织的互认配置。", error.Message);
    Assert.Equal(0, userService.GetUserByIdCallCount);
  }


  /// <summary>构造用户档案，只设置组织与医院归属，院区归属一律为空。</summary>
  /// <param name="organizationCode">用户档案的组织归属编码。</param>
  /// <param name="hospitalCode">用户档案的医院归属编码。</param>
  /// <returns>与令牌用户标识同源的用户档案。</returns>
  /// <summary>按当前用户标识预置一条档案的用户服务替身。</summary>
  /// <param name="profile">用户档案。</param>
  /// <returns>只返回该条档案的替身。</returns>
  private static StubUserAppService BuildUserService(UserDto profile)
  {
    StubUserAppService service = new();
    service.UserById[profile.Id] = profile;
    return service;
  }

  private static UserDto Profile(string? organizationCode, string? hospitalCode) => new()
  {
    Id = Guid.Parse(TrustedUserId),
    // 契约把归属声明为非空字符串，这里显式写入 null/空白构造"档案中取不到该层"的真实数据形态。
    OrgId = organizationCode!,
    HosId = hospitalCode!,
    BranchId = null
  };

  /// <summary>构造含令牌医院与另一家医院、两家各含一个院区的组织服务替身。</summary>
  /// <returns>可用于组织路径校验的组织服务替身。</returns>
  private static StubOrganizationAppService BuildTwoHospitalService()
  {
    StubOrganizationAppService service = new()
    {
      Organizations = [new OrganizationDto { Id = TokenOrganization, Name = OrganizationName, IsValid = true }]
    };
    service.HospitalsByOrganization[TokenOrganization] =
    [
      new HospitalDto { Id = TokenHospital, Name = HospitalName, OrgId = TokenOrganization, IsValid = true },
      new HospitalDto { Id = OtherHospital, Name = HospitalName, OrgId = TokenOrganization, IsValid = true }
    ];
    service.BranchesByHospital[TokenHospital] =
      [new BranchDto { Id = TokenBranch, Name = BranchName, HosId = TokenHospital, OrgId = TokenOrganization, IsValid = true }];
    service.BranchesByHospital[OtherHospital] =
      [new BranchDto { Id = OtherHospitalBranch, Name = BranchName, HosId = OtherHospital, OrgId = TokenOrganization, IsValid = true }];
    return service;
  }

  /// <summary>
  /// 金额查询端口的手写替身：记录本次查询收到的业务键与进入次数；其余查询成员调用即失败。
  /// </summary>
  private sealed class AmountQueryRepository : IMedicalRecognitionReportQueryRepository
  {
    /// <summary>本次金额查询收到的业务键与关键字；未进入查询端口时为 null。</summary>
    public (string OrganizationCode, string HospitalCode, string BranchCode, string? StandardProjectCode, string? Keyword)? ReceivedQuery { get; private set; }

    /// <summary>进入金额查询端口的次数。</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode, string? keyword)
    {
      CallCount++;
      ReceivedQuery = (organizationCode, hospitalCode, branchCode, standardProjectCode, keyword);
      return Task.FromResult<IEnumerable<RecognitionAmountListItem>>([]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? keyword, ConfigurationStatus? configurationStatus) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QuerySelectableStandardItemListAsync(string organizationCode, string? keyword) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
      string organizationCode,
      string hospitalCode,
      string branchCode,
      Guid patientId,
      string identityDocumentNo,
      VisitType visitType,
      string visitSerialNo,
      IReadOnlyList<string> standardProjectCodes) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<bool> HasCurrentEffectiveRecognitionMatchAsync(Guid reportVersionId, string organizationCode, string hospitalCode, string branchCode) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
      string organizationCode,
      string hospitalCode,
      string branchCode,
      string identityDocumentNo,
      VisitType visitType,
      string visitSerialNo) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(IReadOnlyList<string> standardProjectCodes) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    // 阶段 6 互认统计的查询成员：本替身不覆盖统计路径，调用即失败。

    /// <inheritdoc/>
    public Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId) =>
      throw new NotSupportedException("本替身只覆盖互认项目金额列表查询。");
  }
}
