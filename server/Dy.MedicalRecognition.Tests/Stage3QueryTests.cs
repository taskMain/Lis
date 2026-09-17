using System.Reflection;
using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.Base.Application.Contracts.UserAggregate;
using Dy.Core.Extensions.Models;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 互认项目金额列表查询的应用层与静态守卫校验（矩阵 V17、V18、V19、V20、V21、V22、V26、V29、V30）：
/// 行集由互认配置驱动且未配置金额不用零元冒充、返回字段集合、名称与归属读取次数与行数无关、
/// 排序与空结果、三层停用原因优先级、医院管理员入口的院区范围拒绝、平台管理员入口按请求组织执行，
/// 以及两个入口"先校验公共请求、不声明工作单元"的静态约定。
/// </summary>
/// <remarks>
/// 查询端口与外部组织服务使用手写替身，不连接数据库、不发起真实 HTTP 调用。
/// SQL 的行集来源、左连接条件位置与排序键不依赖数据库即可由语句文本冻结，因此在本文件内以 XML 断言覆盖；
/// 真实库上的行数、空结果与金额精度属于阶段 3 的运行面，不在本文件内验证。
/// </remarks>
public sealed class Stage3QueryTests
{
  /// <summary>可信上下文中的组织编码。</summary>
  private const string TrustedOrganization = "ORG-A";
  /// <summary>另一个组织编码，用于构造平台管理员入口的跨组织查询。</summary>
  private const string OtherOrganization = "ORG-B";
  /// <summary>可信上下文中的医院编码。</summary>
  private const string TrustedHospital = "HOS-1";
  /// <summary>另一个组织下的医院编码，用于构造越权院区。</summary>
  private const string OtherHospital = "HOS-2";
  /// <summary>可信医院下的院区编码。</summary>
  private const string TrustedBranch = "BRH-1";
  /// <summary>另一个组织、另一家医院下的院区编码，用于构造请求院区不属于可信医院。</summary>
  private const string OtherBranch = "BRH-2";
  /// <summary>用例注入的令牌用户标识；查询不读取操作人，只用于构造完整的认证上下文。</summary>
  private const string TrustedOperId = "2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e";

  /// <summary>V17：行数等于该组织已建立的互认配置数，且未配置金额的行不用零元冒充、已配置的零元仍算已配置。</summary>
  [Fact]
  public async Task Amount_list_returns_one_row_per_configuration_and_distinguishes_unconfigured_from_zero()
  {
    UseTrustedContext();
    FakeQueryRepository repository = new(
    [
      BuildItem("PROBE-001", currentAmount: 125.50m),
      BuildItem("PROBE-002", currentAmount: 0.00m),
      BuildItem("PROBE-003", currentAmount: null)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel[] items = [.. await service.QueryRecognitionAmountListAsync(PlatformRequest())];

    // 三条互认配置驱动三行：金额表命中的两行与未命中的一行都必须在结果里。
    Assert.Equal(3, items.Length);
    Assert.Equal(["PROBE-001", "PROBE-002", "PROBE-003"], items.Select(item => item.StandardProjectCode).ToArray());

    Assert.True(items[0].IsAmountConfigured);
    Assert.Equal(125.50m, items[0].CurrentAmount);
    // 零元是有效配置：必须与"从未配置"区分，不能因为金额为 0 就判为未配置。
    Assert.True(items[1].IsAmountConfigured);
    Assert.Equal(0.00m, items[1].CurrentAmount);
    // 未配置行：金额无业务值，不用 0 冒充。
    Assert.False(items[2].IsAmountConfigured);
    Assert.Null(items[2].CurrentAmount);
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>V18：返回字段集合恰好 12 个，无三个编码、无最后修改字段、无三层原始状态字段，四个名称齐全。</summary>
  [Fact]
  public async Task Amount_read_model_exposes_only_the_designed_fields_with_four_names()
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel item = Assert.Single(await service.QueryRecognitionAmountListAsync(PlatformRequest()));

    Assert.Equal(
      [
        "BranchName", "CategoryName", "ConfigurationStatus", "ConfigurationStatusText", "CurrentAmount", "GroupName",
        "HospitalName", "IsAmountConfigured", "ItemType", "ItemTypeText", "OrganizationName", "StandardProjectCode",
        "StandardProjectName", "UnavailableReason"
      ],
      typeof(RecognitionAmountReadModel).GetProperties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("OrganizationCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("HospitalCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("BranchCode"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("LastModifiedTime"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("LastModifiedBy"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("CategoryIsValid"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("GroupIsValid"));
    Assert.Null(typeof(RecognitionAmountReadModel).GetProperty("ItemIsValid"));

    // 四个名称都来自查询端口与组织路径，缺一个都会让页面无法展示归属。
    Assert.Equal("示例组织", item.OrganizationName);
    Assert.Equal("示例医院", item.HospitalName);
    Assert.Equal("示例院区", item.BranchName);
    Assert.Equal("血常规", item.StandardProjectName);
  }

  /// <summary>
  /// V18 的枚举中文子面（S3-D21）：读模型必须以服务端声明交付项目类型与配置状态中文，
  /// 页面直接取这两个字段展示；缺字段或返回错误文案会让前端本地常量成为事实上的第二来源。
  /// </summary>
  /// <param name="itemType">项目类型取值。</param>
  /// <param name="configurationStatus">配置状态取值。</param>
  /// <param name="expectedItemTypeText">期望的项目类型中文。</param>
  /// <param name="expectedConfigurationStatusText">期望的配置状态中文。</param>
  [Theory]
  [InlineData(MedicalItemType.Laboratory, ConfigurationStatus.Enabled, "检验", "启用")]
  [InlineData(MedicalItemType.Examination, ConfigurationStatus.Disabled, "检查", "停用")]
  public void Amount_read_model_exposes_enum_texts_from_server_declarations(
    MedicalItemType itemType,
    ConfigurationStatus configurationStatus,
    string expectedItemTypeText,
    string expectedConfigurationStatusText)
  {
    RecognitionAmountReadModel model = new()
    {
      ItemType = itemType,
      ConfigurationStatus = configurationStatus
    };

    Assert.Equal(expectedItemTypeText, model.ItemTypeText);
    Assert.Equal(expectedConfigurationStatusText, model.ConfigurationStatusText);
  }

  /// <summary>
  /// 项目类型取自目录关联、无存储约束校验，未登记取值必须安全降级为 <see langword="null"/>：
  /// 若按严格解析抛出，一条越界数据会让整张金额列表查询失败。
  /// </summary>
  [Fact]
  public void Amount_read_model_returns_null_item_type_text_for_unregistered_value()
  {
    // 取值 99 不可由正常业务路径产生：项目类型固定为检验或检查，这里只用于验证防御行为。
    RecognitionAmountReadModel model = new() { ItemType = (MedicalItemType)99 };

    Assert.Null(model.ItemTypeText);
  }

  /// <summary>
  /// 配置状态由布尔派生、取值集合封闭，未登记取值必须抛出而不是静默降级，
  /// 否则调用链损坏会被显示成"未知状态"而长期不被发现。
  /// </summary>
  [Fact]
  public void Amount_read_model_throws_for_unregistered_configuration_status()
  {
    // 取值 99 不可由正常业务路径产生：配置状态由互认配置表的布尔启用列派生，只有启用与停用两种取值。
    RecognitionAmountReadModel model = new() { ConfigurationStatus = (ConfigurationStatus)99 };

    Assert.Throws<ExtensionException>(() => model.ConfigurationStatusText);
  }

  /// <summary>
  /// V19：外部组织服务的读取次数只随目标涉及的组织与医院数量增长，不随返回行数增长；
  /// 10 行与 100 行两组数据下三类读取次数必须完全相等且为常量。
  /// </summary>
  [Fact]
  public async Task Organization_read_counts_do_not_grow_with_the_number_of_returned_rows()
  {
    UseTrustedContext();

    (int RowCount, long Organizations, long Hospitals, long Branches) ten = await ReadCountsAsync(BuildItems(10));
    (int RowCount, long Organizations, long Hospitals, long Branches) hundred = await ReadCountsAsync(BuildItems(100));

    Assert.Equal((10, 1L, 1L, 1L), ten);
    Assert.Equal((100, 1L, 1L, 1L), hundred);
    Assert.Equal((ten.Organizations, ten.Hospitals, ten.Branches), (hundred.Organizations, hundred.Hospitals, hundred.Branches));
  }

  /// <summary>V20：结果按标准项目编码升序；该组织没有互认配置时成功返回空集合。</summary>
  [Fact]
  public async Task Amount_list_is_ordered_by_project_code_and_returns_empty_when_no_configuration_exists()
  {
    UseTrustedContext();
    MedicalRecognitionReportQueryAppService service = CreateService(new FakeQueryRepository([]), CreateTrustedOrganizationService());

    Assert.Empty(await service.QueryRecognitionAmountListAsync(PlatformRequest()));

    // 排序键由 SQL 的 order by 决定，应用层原样保持端口顺序：乱序返回时不得静默重排。
    FakeQueryRepository unordered = new(
    [
      BuildItem("PROBE-003"), BuildItem("PROBE-001"), BuildItem("PROBE-002")
    ]);
    RecognitionAmountReadModel[] items = [.. await CreateService(unordered, CreateTrustedOrganizationService()).QueryRecognitionAmountListAsync(PlatformRequest())];

    Assert.Equal(["PROBE-003", "PROBE-001", "PROBE-002"], items.Select(item => item.StandardProjectCode).ToArray());
  }

  /// <summary>
  /// V17 的语句级判据：行集由互认配置驱动、金额按四个业务键左连接、医院与院区条件只写在 ON 子句内，
  /// 且语句不含分页与方言特征。
  /// </summary>
  /// <remarks>
  /// 医院或院区条件一旦被挪进 where，左连接会退化为内连接、从未配置金额的行整体消失；
  /// 该退化不会让任何编译或应用层用例失败，因此必须在这里按语句文本冻结。
  /// </remarks>
  [Fact]
  public void Amount_query_drives_rows_from_configuration_and_keeps_join_conditions_in_the_on_clause()
  {
    System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Load(FindRepositoryFile("MedicalRecognitionReportQuery.xml"));
    System.Xml.Linq.XNamespace ns = "http://dysoft.vip/schemas/EarthraceSqlMap.xsd";
    string statement = document.Descendants(ns + "Statement")
      .Single(element => (string?)element.Attribute("Id") == "QueryRecognitionAmountList").Value;
    string sql = NormalizeSqlText(statement);

    Assert.True(AmountQueryKeepsJoinConditionsOnTheAmountSide(sql));

    // 变异证据：把医院条件从 ON 子句挪进 where 后，同一条判据必须判为不成立。
    string hospitalMovedIntoWhere = sql.Replace("and a.hospital_code = $HospitalCode", "", StringComparison.Ordinal)
      .Replace("where 1 = 1", "where 1 = 1 and a.hospital_code = $HospitalCode", StringComparison.Ordinal);
    Assert.NotEqual(sql, hospitalMovedIntoWhere);
    Assert.False(AmountQueryKeepsJoinConditionsOnTheAmountSide(hospitalMovedIntoWhere));

    // 行集表与左连接目标表：行集换成金额表会让未配置金额的标准项目整体消失。
    Assert.Contains("from mrec_mutual_recognition_item m", sql, StringComparison.Ordinal);
    Assert.Contains("left join mrec_organization_hospital_branch_recognition_amount a", sql, StringComparison.Ordinal);
    // 组织范围谓词：缺失时一个组织的调用方会读到其他组织的金额行。
    Assert.Contains("and m.organization_code = $OrganizationCode", sql, StringComparison.Ordinal);
    // 不筛配置启用状态：停用的配置仍要出现在金额维护页面上。
    Assert.DoesNotContain("m.is_valid = $IsValid", sql, StringComparison.Ordinal);
    // 排序与不分页：分页会截断结果，方言特征会让共享 SQL 绑定到单一 Provider。
    Assert.Contains("order by m.standard_project_code asc", sql, StringComparison.Ordinal);
    foreach (string marker in new[] { "limit ", "offset ", "on conflict", "nulls first", "nulls last", "count(distinct", "::" })
    {
      Assert.DoesNotContain(marker, sql, StringComparison.OrdinalIgnoreCase);
    }
  }

  /// <summary>
  /// V17 的语句级行为的变异证据：把金额表侧的医院条件或院区条件写进 where 都会让左连接退化为内连接，
  /// 同一条判据必须判红；只断言"语句里出现过这两个条件"时，条件被挪进 where 不会让任何用例失败。
  /// </summary>
  [Fact]
  public void Amount_query_join_condition_placement_detects_both_degradation_variants()
  {
    System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Load(FindRepositoryFile("MedicalRecognitionReportQuery.xml"));
    System.Xml.Linq.XNamespace ns = "http://dysoft.vip/schemas/EarthraceSqlMap.xsd";
    string sql = NormalizeSqlText(document.Descendants(ns + "Statement")
      .Single(element => (string?)element.Attribute("Id") == "QueryRecognitionAmountList").Value);
    Assert.True(AmountQueryKeepsJoinConditionsOnTheAmountSide(sql));

    foreach (string predicate in new[] { "a.hospital_code = $HospitalCode", "a.branch_code = $BranchCode" })
    {
      string degraded = sql.Replace($"and {predicate}", string.Empty, StringComparison.Ordinal)
        .Replace("where 1 = 1", $"where 1 = 1 and {predicate}", StringComparison.Ordinal);

      // 变异确实改变了语句文本：条件从 ON 子句消失、改在 where 中出现。
      Assert.NotEqual(sql, degraded);
      Assert.DoesNotContain($"and {predicate}\n", degraded, StringComparison.Ordinal);
      Assert.Contains($"where 1 = 1 and {predicate}", degraded, StringComparison.Ordinal);
      // 判据对两种退化都判红，说明"未配置行整体消失"这一缺陷不会被静默放过。
      Assert.False(AmountQueryKeepsJoinConditionsOnTheAmountSide(degraded), $"{predicate}: 条件被挪进 where 后判据仍判为通过");
    }
  }

  /// <summary>V18/V26：两个金额查询请求的字段集合与设计一致，且都不携带操作人、操作时间、内部标识或授权结论字段。</summary>
  [Fact]
  public void Amount_query_requests_expose_only_the_designed_members()
  {
    Assert.Equal(
      ["BranchCode", "HospitalCode", "OrganizationCode", "StandardProjectCode"],
      typeof(RecognitionAmountListQueryRequest).GetProperties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
    Assert.Equal(
      ["BranchCode", "StandardProjectCode"],
      typeof(BranchRecognitionAmountListQueryRequest).GetProperties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));

    // 医院管理员入口的请求不得提交组织与医院，也不得提交授权结论。
    Assert.Null(typeof(BranchRecognitionAmountListQueryRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(BranchRecognitionAmountListQueryRequest).GetProperty("HospitalCode"));
    foreach (Type requestType in new[] { typeof(RecognitionAmountListQueryRequest), typeof(BranchRecognitionAmountListQueryRequest) })
    {
      foreach (string forbidden in new[] { "OperId", "OperTime", "Id", "IsValid", "ConfigurationStatus", "IsAuthorized" })
      {
        Assert.Null(requestType.GetProperty(forbidden));
      }
    }

    // 必填字段：空值与纯空白都必须由请求校验拒绝，空白文本不代表"不过滤"。
    AssertInvalid<RecognitionAmountListQueryRequest>(new RecognitionAmountListQueryRequest { HospitalCode = TrustedHospital, BranchCode = TrustedBranch });
    AssertInvalid<RecognitionAmountListQueryRequest>(new RecognitionAmountListQueryRequest { OrganizationCode = "  ", HospitalCode = TrustedHospital, BranchCode = TrustedBranch });
    AssertInvalid<RecognitionAmountListQueryRequest>(new RecognitionAmountListQueryRequest { OrganizationCode = TrustedOrganization, HospitalCode = "  ", BranchCode = TrustedBranch });
    AssertInvalid<RecognitionAmountListQueryRequest>(new RecognitionAmountListQueryRequest { OrganizationCode = TrustedOrganization, HospitalCode = TrustedHospital, BranchCode = "  " });
    AssertInvalid<RecognitionAmountListQueryRequest>(new RecognitionAmountListQueryRequest { OrganizationCode = TrustedOrganization, HospitalCode = TrustedHospital, BranchCode = TrustedBranch, StandardProjectCode = "  " });
    AssertInvalid<BranchRecognitionAmountListQueryRequest>(new BranchRecognitionAmountListQueryRequest());
    AssertInvalid<BranchRecognitionAmountListQueryRequest>(new BranchRecognitionAmountListQueryRequest { BranchCode = "  " });
    AssertInvalid<BranchRecognitionAmountListQueryRequest>(new BranchRecognitionAmountListQueryRequest { BranchCode = TrustedBranch, StandardProjectCode = "  " });
    Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(PlatformRequest());
    Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(new BranchRecognitionAmountListQueryRequest { BranchCode = TrustedBranch });
  }

  /// <summary>V21：目录三层全部启用时原因为空，配置自身停用不改写该字段。</summary>
  [Fact]
  public async Task Amount_list_has_no_unavailable_reason_when_catalog_layers_are_enabled()
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([BuildItem(configurationIsValid: false)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel item = Assert.Single(await service.QueryRecognitionAmountListAsync(PlatformRequest()));

    Assert.Null(item.UnavailableReason);
    Assert.Equal(ConfigurationStatus.Disabled, item.ConfigurationStatus);
  }

  /// <summary>V21：只有一层停用时返回该层对应的原因文案，配置自身停用与金额未配置都不影响原因。</summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="expectedReason">期望的不可用原因文案。</param>
  [Theory]
  [InlineData(false, true, true, "所属分类已停用")]
  [InlineData(true, false, true, "所属分组已停用")]
  [InlineData(true, true, false, "标准项目已停用")]
  public async Task Amount_list_reports_the_single_disabled_catalog_layer(bool categoryIsValid, bool groupIsValid, bool itemIsValid, string expectedReason)
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([BuildItem(categoryIsValid: categoryIsValid, groupIsValid: groupIsValid, itemIsValid: itemIsValid, currentAmount: null)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel item = Assert.Single(await service.QueryRecognitionAmountListAsync(PlatformRequest()));

    Assert.Equal(expectedReason, item.UnavailableReason);
  }

  /// <summary>V21：多层同时停用时按“分类 → 分组 → 标准项目”的顺序返回第一条原因。</summary>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="expectedReason">期望的不可用原因文案。</param>
  [Theory]
  [InlineData(false, false, false, "所属分类已停用")]
  [InlineData(false, false, true, "所属分类已停用")]
  [InlineData(false, true, false, "所属分类已停用")]
  [InlineData(true, false, false, "所属分组已停用")]
  [InlineData(true, false, true, "所属分组已停用")]
  [InlineData(true, true, false, "标准项目已停用")]
  public async Task Amount_list_follows_category_group_item_priority(bool categoryIsValid, bool groupIsValid, bool itemIsValid, string expectedReason)
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([BuildItem(categoryIsValid: categoryIsValid, groupIsValid: groupIsValid, itemIsValid: itemIsValid)]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel item = Assert.Single(await service.QueryRecognitionAmountListAsync(PlatformRequest()));

    Assert.Equal(expectedReason, item.UnavailableReason);
  }

  /// <summary>V30：医院管理员入口请求属于可信医院的院区时放行，组织、医院与院区都取可信/已校验路径。</summary>
  [Fact]
  public async Task Branch_entrypoint_allows_a_branch_of_the_trusted_hospital()
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    RecognitionAmountReadModel item = Assert.Single(await service.QueryBranchRecognitionAmountListAsync(BranchRequest()));

    Assert.Equal("示例组织", item.OrganizationName);
    Assert.Equal("示例医院", item.HospitalName);
    Assert.Equal("示例院区", item.BranchName);
    Assert.Equal((TrustedOrganization, TrustedHospital, TrustedBranch, (string?)null), repository.ReceivedQuery);
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>V22/V30：医院管理员入口请求属于另一家医院的院区即拒绝，不返回其他医院数据、不降级为空集合。</summary>
  [Fact]
  public async Task Branch_entrypoint_rejects_a_branch_of_another_hospital_without_degrading_to_an_empty_collection()
  {
    UseTrustedContext();
    FakeQueryRepository repository = new([]);
    CountingOrganizationAppService organizationService = CreateTrustedOrganizationService();
    AddOtherOrganization(organizationService);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.QueryBranchRecognitionAmountListAsync(BranchRequest(branchCode: OtherBranch)));

    Assert.Contains("院区", error.Message);
    // 拒绝而不是返回空集合：查询端口一次都不能进入，否则调用方无法区分"越权"与"没有数据"。
    Assert.Equal(0, repository.CallCount);
    Assert.Null(repository.ReceivedQuery);
  }

  /// <summary>V30：可信组织与医院都由令牌与该登录用户的档案提供且都取不到时拒绝，不使用默认值、不降级为空集合。</summary>
  /// <remarks>
  /// 可信范围解析先取登录令牌声明，令牌缺失的层由当前登录用户档案补齐。两类取不到的情形都必须拒绝：
  /// 读不到用户档案（此时无法补齐），以及档案可读但该层为 null、空串或纯空白（补齐后仍为空）；
  /// 两种情况都发生在组织路径解析之前，因此组织服务一次都不被调用，也不进入查询端口。
  /// </remarks>
  [Fact]
  public async Task Branch_entrypoint_is_rejected_when_trusted_organization_or_hospital_is_missing()
  {
    FakeQueryRepository repository = new([]);
    CountingOrganizationAppService organizationService = CreateTrustedOrganizationService();

    foreach ((string? organizationCode, string? hospitalCode, string expectedMessage) in new[]
    {
      (null, TrustedHospital, "组织"),
      ("   ", TrustedHospital, "组织"),
      (TrustedOrganization, null, "医院"),
      (TrustedOrganization, "   ", "医院")
    })
    {
      TrustedRequestContext.Use(organizationCode, hospitalCode, TrustedBranch, TrustedOperId);

      // 情形一：读不到用户档案，令牌缺失层无从补齐。
      MedicalRecognitionReportQueryAppService missingProfileService =
        CreateService(repository, organizationService, new StubUserAppService());
      InvalidOperationException missingProfileError = await Assert.ThrowsAsync<InvalidOperationException>(
        () => missingProfileService.QueryBranchRecognitionAmountListAsync(BranchRequest()));
      Assert.Contains("用户", missingProfileError.Message);
      Assert.Equal(0, repository.CallCount);

      // 情形二：档案可读但组织与医院都取不到值，补齐后该层仍为空。
      StubUserAppService emptyProfileUserService = new();
      emptyProfileUserService.UserById[Guid.Parse(TrustedOperId)] =
        new UserDto { Id = Guid.Parse(TrustedOperId), OrgId = "", HosId = "", BranchId = null };
      MedicalRecognitionReportQueryAppService emptyProfileService =
        CreateService(repository, organizationService, emptyProfileUserService);
      InvalidOperationException emptyProfileError = await Assert.ThrowsAsync<InvalidOperationException>(
        () => emptyProfileService.QueryBranchRecognitionAmountListAsync(BranchRequest()));
      Assert.Contains(expectedMessage, emptyProfileError.Message);
      Assert.Equal(0, repository.CallCount);

      // 两种情形都必须在读取任何组织路径之前拒绝。
      Assert.Equal(0L, organizationService.OrganizationReadCount);
    }
  }

  /// <summary>V29（应用层）：平台管理员入口按请求的组织、医院与院区执行，不要求等于可信上下文组织。</summary>
  [Fact]
  public async Task Platform_entrypoint_queries_the_requested_organization_even_when_it_differs_from_the_trusted_context()
  {
    UseTrustedContext();
    CountingOrganizationAppService organizationService = CreateTrustedOrganizationService();
    AddOtherOrganization(organizationService);
    FakeQueryRepository repository = new([BuildItem()]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    RecognitionAmountReadModel item = Assert.Single(await service.QueryRecognitionAmountListAsync(
      PlatformRequest(organizationCode: OtherOrganization, hospitalCode: OtherHospital, branchCode: OtherBranch)));

    // 查询端口收到的组织编码必须是请求值，不是可信上下文值：收到可信值说明被静默改写成"只查自己"。
    Assert.Equal((OtherOrganization, OtherHospital, OtherBranch, (string?)null), repository.ReceivedQuery);
    Assert.Equal("另一个组织", item.OrganizationName);
    Assert.Equal("另一个组织的医院", item.HospitalName);
    Assert.Equal("另一个组织的院区", item.BranchName);
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>两个金额查询入口都先执行公共请求校验，再解析组织路径、再进入查询端口。</summary>
  [Fact]
  public void Amount_query_entrypoints_validate_request_before_any_other_step()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition.Application", "Queries", "MedicalRecognitionReportQueryAppService.cs");
    string[] entrypoints =
    [
      nameof(MedicalRecognitionReportQueryAppService.QueryRecognitionAmountListAsync),
      nameof(MedicalRecognitionReportQueryAppService.QueryBranchRecognitionAmountListAsync)
    ];

    foreach (string name in entrypoints)
    {
      Assert.True(AmountQueryValidatesBeforeRepository(SourceSyntaxGuard.FindSingleMethod(root, name)), $"{name}: 未先执行公共请求校验");
    }

    // 变异证据：把公共请求校验与查询端口调用对调后，同一条判据必须判为不成立。
    // 副本按语句位置重建，而不是用 SyntaxList.Replace 互换节点：后者要求被换入的节点本身就是该列表的成员，
    // 互换会直接抛参数异常，变异证据也就无从产生。
    foreach (string name in entrypoints)
    {
      MethodDeclarationSyntax method = SourceSyntaxGuard.FindSingleMethod(root, name);
      SyntaxList<StatementSyntax> statements = MethodStatements(method);
      int validateIndex = IndexOfStatement(statements, "MedicalRecognitionRequestValidator.Validate(request)");
      int repositoryIndex = IndexOfStatement(statements, "repository.QueryRecognitionAmountListAsync");
      List<StatementSyntax> swapped = [.. statements];
      (swapped[validateIndex], swapped[repositoryIndex]) = (swapped[repositoryIndex], swapped[validateIndex]);
      MethodDeclarationSyntax mutated = method.WithBody(method.Body!.WithStatements(SyntaxFactory.List(swapped)));
      Assert.False(AmountQueryValidatesBeforeRepository(mutated), $"{name}: 判据对调换顺序后的副本仍判为通过");
    }
  }

  /// <summary>两个金额查询入口都是纯查询：都不声明工作单元，避免查询路径引入事务或事件。</summary>
  [Fact]
  public void Amount_query_entrypoints_declare_no_work_unit()
  {
    Type appService = typeof(MedicalRecognitionReportQueryAppService);
    foreach (string name in new[] { "QueryRecognitionAmountListAsync", "QueryBranchRecognitionAmountListAsync" })
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }
  }

  /// <summary>把当前请求的可信上下文设置为可信组织与可信医院。</summary>
  private static void UseTrustedContext() => TrustedRequestContext.Use(TrustedOrganization, TrustedHospital, TrustedBranch, TrustedOperId);

  /// <summary>构建被测查询应用服务：注入查询端口替身、外部组织服务替身与外部用户服务替身。</summary>
  /// <param name="repository">查询端口替身。</param>
  /// <param name="organizationService">外部组织服务替身。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(FakeQueryRepository repository, IOrganizationAppService organizationService) =>
    new(repository, organizationService, new StubUserAppService());

  /// <summary>构建被测查询应用服务，并指定补齐令牌缺失层所用的外部用户服务替身。</summary>
  /// <param name="repository">查询端口替身。</param>
  /// <param name="organizationService">外部组织服务替身。</param>
  /// <param name="userService">提供当前登录用户档案的外部用户服务替身。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(
    FakeQueryRepository repository, IOrganizationAppService organizationService, StubUserAppService userService) =>
    new(repository, organizationService, userService);

  /// <summary>构造平台管理员入口的金额列表查询条件。</summary>
  /// <param name="organizationCode">请求组织编码。</param>
  /// <param name="hospitalCode">请求医院编码。</param>
  /// <param name="branchCode">请求院区编码。</param>
  /// <param name="standardProjectCode">可选标准项目编码。</param>
  /// <returns>可直接交给查询入口的请求。</returns>
  private static RecognitionAmountListQueryRequest PlatformRequest(
    string organizationCode = TrustedOrganization,
    string hospitalCode = TrustedHospital,
    string branchCode = TrustedBranch,
    string? standardProjectCode = null) => new()
    {
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode
    };

  /// <summary>构造医院管理员入口的金额列表查询条件；该请求不含组织与医院。</summary>
  /// <param name="standardProjectCode">可选标准项目编码。</param>
  /// <param name="branchCode">请求院区编码。</param>
  /// <returns>可直接交给查询入口的请求。</returns>
  private static BranchRecognitionAmountListQueryRequest BranchRequest(
    string? standardProjectCode = null, string branchCode = TrustedBranch) => new()
    {
      BranchCode = branchCode,
      StandardProjectCode = standardProjectCode
    };

  /// <summary>构造只含可信组织路径的替身：一个组织、一家医院、一个院区，三层都启用。</summary>
  /// <returns>已完成初始化的外部组织服务替身。</returns>
  private static CountingOrganizationAppService CreateTrustedOrganizationService() =>
    AddTrustedOrganization(new CountingOrganizationAppService());

  /// <summary>向替身填入可信组织、可信医院与可信院区三层启用的路径数据。</summary>
  /// <param name="organizationService">待填充的外部组织服务替身。</param>
  /// <returns>同一个已填充的替身，便于链式构造。</returns>
  private static CountingOrganizationAppService AddTrustedOrganization(CountingOrganizationAppService organizationService)
  {
    organizationService.Organizations.Add(new OrganizationDto { Id = TrustedOrganization, Name = "示例组织", IsValid = true });
    organizationService.HospitalsByOrganization[TrustedOrganization] =
      [new HospitalDto { Id = TrustedHospital, Name = "示例医院", OrgId = TrustedOrganization, IsValid = true }];
    organizationService.BranchesByHospital[TrustedHospital] =
      [new BranchDto { Id = TrustedBranch, Name = "示例院区", HosId = TrustedHospital, OrgId = TrustedOrganization, IsValid = true }];
    return organizationService;
  }

  /// <summary>向替身追加另一个组织的医院与院区，用于构造跨组织查询与越权院区。</summary>
  /// <param name="organizationService">已完成初始化的外部组织服务替身。</param>
  /// <returns>同一个已填充的替身，便于链式构造。</returns>
  private static CountingOrganizationAppService AddOtherOrganization(CountingOrganizationAppService organizationService)
  {
    organizationService.Organizations.Add(new OrganizationDto { Id = OtherOrganization, Name = "另一个组织", IsValid = true });
    organizationService.HospitalsByOrganization[OtherOrganization] =
      [new HospitalDto { Id = OtherHospital, Name = "另一个组织的医院", OrgId = OtherOrganization, IsValid = true }];
    organizationService.BranchesByHospital[OtherHospital] =
      [new BranchDto { Id = OtherBranch, Name = "另一个组织的院区", HosId = OtherHospital, OrgId = OtherOrganization, IsValid = true }];
    return organizationService;
  }

  /// <summary>构造指定条数的金额行投影，标准项目编码按序号递增。</summary>
  /// <param name="count">行数。</param>
  /// <returns>按标准项目编码升序排列的投影集合。</returns>
  private static List<RecognitionAmountListItem> BuildItems(int count) =>
    [.. Enumerable.Range(1, count).Select(index => BuildItem($"PROBE-{index:D3}"))];

  /// <summary>发起一次金额查询并返回返回行数与外部组织服务的三类读取次数。</summary>
  /// <param name="items">本次查询返回的金额行投影。</param>
  /// <returns>返回行数与组织、医院、院区三类读取次数。</returns>
  private static async Task<(int RowCount, long Organizations, long Hospitals, long Branches)> ReadCountsAsync(IEnumerable<RecognitionAmountListItem> items)
  {
    CountingOrganizationAppService organizationService = CreateTrustedOrganizationService();
    RecognitionAmountReadModel[] result =
      [.. await CreateService(new FakeQueryRepository(items), organizationService).QueryRecognitionAmountListAsync(PlatformRequest())];
    return (result.Length, organizationService.OrganizationReadCount, organizationService.HospitalReadCount, organizationService.BranchReadCount);
  }

  /// <summary>断言请求的声明式校验会因必填缺失或空白文本而拒绝。</summary>
  /// <typeparam name="TRequest">请求类型。</typeparam>
  /// <param name="request">待校验的请求实例。</param>
  private static void AssertInvalid<TRequest>(TRequest request) =>
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(request!));

  /// <summary>构造一条金额行投影，只把用例关心的字段参数化，其余字段取固定值。</summary>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="currentAmount">当前金额；null 表示未配置金额。</param>
  /// <param name="categoryIsValid">所属分类是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <param name="itemIsValid">标准项目是否启用。</param>
  /// <param name="configurationIsValid">配置自身是否启用。</param>
  /// <returns>查询端口返回的金额行投影。</returns>
  private static RecognitionAmountListItem BuildItem(
    string standardProjectCode = "PROBE-001",
    decimal? currentAmount = 125.50m,
    bool categoryIsValid = true,
    bool groupIsValid = true,
    bool itemIsValid = true,
    bool configurationIsValid = true) => new()
    {
      StandardProjectCode = standardProjectCode,
      StandardItemName = "血常规",
      ItemType = MedicalItemType.Laboratory,
      CategoryName = "临床检验",
      GroupName = "血液学",
      CurrentAmount = currentAmount,
      ConfigurationIsValid = configurationIsValid,
      CategoryIsValid = categoryIsValid,
      GroupIsValid = groupIsValid,
      ItemIsValid = itemIsValid
    };

  /// <summary>取方法体的语句集合；无方法体时返回空集合。</summary>
  /// <param name="method">方法声明节点。</param>
  /// <returns>方法体内的语句集合。</returns>
  private static SyntaxList<StatementSyntax> MethodStatements(MethodDeclarationSyntax method) => method.Body?.Statements ?? default;

  /// <summary>
  /// 判断金额查询入口是否在进入查询端口之前先执行了公共请求校验。
  /// </summary>
  /// <param name="method">查询入口的方法声明节点。</param>
  /// <returns>公共请求校验存在且早于查询端口调用时为 <see langword="true"/>。</returns>
  private static bool AmountQueryValidatesBeforeRepository(MethodDeclarationSyntax method)
  {
    int validateIndex = IndexOfStatement(MethodStatements(method), "MedicalRecognitionRequestValidator.Validate(request)");
    int repositoryIndex = IndexOfStatement(MethodStatements(method), "repository.QueryRecognitionAmountListAsync");
    return validateIndex >= 0 && repositoryIndex >= 0 && validateIndex < repositoryIndex;
  }

  /// <summary>
  /// 在方法体语句中定位包含指定文本的第一条语句的位置。
  /// </summary>
  /// <param name="statements">方法体语句集合。</param>
  /// <param name="marker">语句文本中必须出现的片段。</param>
  /// <returns>命中的语句下标；没有命中时为 <c>-1</c>。</returns>
  private static int IndexOfStatement(SyntaxList<StatementSyntax> statements, string marker)
  {
    for (int index = 0; index < statements.Count; index++)
    {
      if (statements[index].ToString().Contains(marker, StringComparison.Ordinal)) return index;
    }

    return -1;
  }

  /// <summary>
  /// 判断金额查询语句是否把医院与院区条件保留在金额表侧的 ON 子句内。
  /// </summary>
  /// <remarks>
  /// 判据按复合比较取子句区间：简单包含判定无法区分条件写在 ON 还是 where，而写成 where 会让左连接退化为内连接。
  /// </remarks>
  /// <param name="sql">空白归一后的金额查询语句文本。</param>
  /// <returns>两个条件都只在左连接的 ON 子句内出现时为 <see langword="true"/>。</returns>
  private static bool AmountQueryKeepsJoinConditionsOnTheAmountSide(string sql)
  {
    int joinIndex = sql.IndexOf("left join mrec_organization_hospital_branch_recognition_amount a", StringComparison.Ordinal);
    if (joinIndex < 0) return false;

    int onIndex = sql.IndexOf(" on ", joinIndex, StringComparison.Ordinal);
    int whereIndex = sql.IndexOf(" where ", joinIndex, StringComparison.Ordinal);
    if (onIndex < 0 || whereIndex < 0 || onIndex > whereIndex) return false;

    string onClause = sql[(onIndex + " on ".Length)..whereIndex];
    string whereClause = sql[whereIndex..];
    return onClause.Contains("a.hospital_code = $HospitalCode", StringComparison.Ordinal)
      && onClause.Contains("a.branch_code = $BranchCode", StringComparison.Ordinal)
      && !whereClause.Contains("a.hospital_code = $HospitalCode", StringComparison.Ordinal)
      && !whereClause.Contains("a.branch_code = $BranchCode", StringComparison.Ordinal);
  }

  /// <summary>
  /// 把 SQL 文本中的换行与连续空白归一为单个空格，使冻结判据只对语句内容敏感、不对映射文件排版敏感。
  /// </summary>
  /// <param name="text">SQL 文本或语句片段。</param>
  /// <returns>空白归一后的 SQL 文本。</returns>
  private static string NormalizeSqlText(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

  /// <summary>
  /// 在仓储工程内按文件名定位 SqlMap 文件，供语句级断言直接读取语句文本。
  /// </summary>
  /// <param name="fileName">SqlMap 文件名，例如 <c>MedicalRecognitionReportQuery.xml</c>。</param>
  /// <returns>仓储工程中该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">从测试程序集所在目录逐级向上都未唯一命中该文件时抛出。</exception>
  private static string FindRepositoryFile(string fileName)
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var repositoryDirectory = Path.Combine(directory.FullName, "Dy.MedicalRecognition.Repository");
      if (Directory.Exists(repositoryDirectory))
      {
        var matches = Directory.GetFiles(repositoryDirectory, fileName, SearchOption.AllDirectories);
        if (matches.Length == 1) return matches[0];
      }

      directory = directory.Parent;
    }

    throw new FileNotFoundException($"未找到仓储文件 '{fileName}'。");
  }

  /// <summary>
  /// 金额查询端口的手写替身：记录本次查询收到的业务键与调用次数，并按测试设置的集合返回金额行投影。
  /// </summary>
  private sealed class FakeQueryRepository : IMedicalRecognitionReportQueryRepository
  {
    /// <summary>
    /// 创建替身并设置首次金额查询返回的投影集合。
    /// </summary>
    /// <param name="items">首次金额查询返回的投影集合。</param>
    public FakeQueryRepository(IEnumerable<RecognitionAmountListItem> items) => Items = [.. items];

    /// <summary>金额查询返回的投影集合。</summary>
    public IEnumerable<RecognitionAmountListItem> Items { get; }

    /// <summary>本次金额查询收到的业务键；未进入查询端口时为 null。</summary>
    public (string OrganizationCode, string HospitalCode, string BranchCode, string? StandardProjectCode)? ReceivedQuery { get; private set; }

    /// <summary>进入金额查询端口的次数。</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode)
    {
      CallCount++;
      ReceivedQuery = (organizationCode, hospitalCode, branchCode, standardProjectCode);
      return Task.FromResult(Items);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) =>
      throw new NotSupportedException("本测试只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) =>
      throw new NotSupportedException("本测试只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) =>
      throw new NotSupportedException("本测试只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) =>
      throw new NotSupportedException("本测试只覆盖互认项目金额列表查询。");

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus) =>
      throw new NotSupportedException("本测试只覆盖互认项目金额列表查询。");
  }

  /// <summary>
  /// 外部组织服务替身：按用例预置的组织、医院与院区数据实现组织路径解析实际使用的三类读取，
  /// 并累计各类读取的调用次数，供"读取次数不随返回行数增长"的断言使用。
  /// </summary>
  /// <remarks>
  /// 三类读取之外的成员一律委托给共享替身 <see cref="StubOrganizationAppService"/>，被误用时直接抛出，
  /// 不会在未覆盖的路径上静默走过。
  /// </remarks>
  private sealed class CountingOrganizationAppService : IOrganizationAppService
  {
    /// <summary>按组织编码预置的读取结果；本替身默认为空。</summary>
    public List<OrganizationDto> Organizations { get; } = [];

    /// <summary>按组织编码预置的医院读取结果；本替身默认为空。</summary>
    public Dictionary<string, List<HospitalDto>> HospitalsByOrganization { get; } = [];

    /// <summary>按医院编码预置的院区读取结果；本替身默认为空。</summary>
    public Dictionary<string, List<BranchDto>> BranchesByHospital { get; } = [];

    /// <summary>组织全量读取的调用次数。</summary>
    public long OrganizationReadCount { get; private set; }

    /// <summary>按组织读取启用医院的调用次数。</summary>
    public long HospitalReadCount { get; private set; }

    /// <summary>按医院读取启用院区的调用次数。</summary>
    public long BranchReadCount { get; private set; }

    /// <summary>未被本替身覆盖的接口成员统一委托给它，调用即抛出。</summary>
    private static readonly IOrganizationAppService Unsupported = new StubOrganizationAppService();

    /// <inheritdoc/>
    public Task<IEnumerable<OrganizationDto>> QueryAllOrganizationAsync()
    {
      OrganizationReadCount++;
      return Task.FromResult<IEnumerable<OrganizationDto>>(Organizations);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalByOrgIdAsync(QueryAllValidHospitalByOrgIdRequest queryAllValidHospitalByOrgIdRequest)
    {
      HospitalReadCount++;
      return Task.FromResult<IEnumerable<HospitalDto>>(
        HospitalsByOrganization.TryGetValue(queryAllValidHospitalByOrgIdRequest.OrgId, out List<HospitalDto>? hospitals) ? hospitals : []);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest queryAllValidBranchByHosIdRequest)
    {
      BranchReadCount++;
      return Task.FromResult<IEnumerable<BranchDto>>(
        BranchesByHospital.TryGetValue(queryAllValidBranchByHosIdRequest.HosId, out List<BranchDto>? branches) ? branches : []);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByOrgIdAsync(QueryAllValidBranchByOrgIdRequest queryAllValidBranchByOrgIdRequest) => Unsupported.QueryAllValidBranchByOrgIdAsync(queryAllValidBranchByOrgIdRequest);

    /// <inheritdoc/>
    public Task<OrganizationDto> GetOrganizationByIdAsync(GetOrganizationByIdRequest getOrganizationByIdRequest) => Unsupported.GetOrganizationByIdAsync(getOrganizationByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllHospitalAsync() => Unsupported.QueryAllHospitalAsync();

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalAsync() => Unsupported.QueryAllValidHospitalAsync();

    /// <inheritdoc/>
    public Task<HospitalDto> GetHospitalByIdAsync(GetHospitalByIdRequest getHospitalByIdRequest) => Unsupported.GetHospitalByIdAsync(getHospitalByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllBranchAsync() => Unsupported.QueryAllBranchAsync();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchAsync() => Unsupported.QueryAllValidBranchAsync();

    /// <inheritdoc/>
    public Task<BranchDto> GetBranchByIdAsync(GetBranchByIdRequest getBranchByIdRequest) => Unsupported.GetBranchByIdAsync(getBranchByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptBySubApplicationIdAsync(QueryDeptBySubApplicationIdRequest queryDeptBySubApplicationIdRequest) => Unsupported.QueryDeptBySubApplicationIdAsync(queryDeptBySubApplicationIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByOrgIdAsync(QueryDeptByOrgIdRequest queryDeptByOrgIdRequest) => Unsupported.QueryDeptByOrgIdAsync(queryDeptByOrgIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByHosIdAsync(QueryDeptByHosIdRequest queryDeptByHosIdRequest) => Unsupported.QueryDeptByHosIdAsync(queryDeptByHosIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByBranchIdAsync(QueryDeptByBranchIdRequest queryDeptByBranchIdRequest) => Unsupported.QueryDeptByBranchIdAsync(queryDeptByBranchIdRequest);

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByBranchIdAndCodeAsync(GetDeptByBranchIdAndCodeRequest getDeptByBranchIdAndCodeRequest) => Unsupported.GetDeptByBranchIdAndCodeAsync(getDeptByBranchIdAndCodeRequest);

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByIdAsync(GetDeptByIdRequest getDeptByIdRequest) => Unsupported.GetDeptByIdAsync(getDeptByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryAllDeptAsync() => Unsupported.QueryAllDeptAsync();

    /// <inheritdoc/>
    public Task<DeptWardDto> GetDeptWardByIdAsync(GetDeptWardByIdRequest getDeptWardByIdRequest) => Unsupported.GetDeptWardByIdAsync(getDeptWardByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByDeptAsync(QueryDeptWardByDeptRequest queryDeptWardByDeptRequest) => Unsupported.QueryDeptWardByDeptAsync(queryDeptWardByDeptRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByWardAsync(QueryDeptWardByWardRequest queryDeptWardByWardRequest) => Unsupported.QueryDeptWardByWardAsync(queryDeptWardByWardRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardByBranchAsync(QueryWardByBranchRequest queryWardByBranchRequest) => Unsupported.QueryWardByBranchAsync(queryWardByBranchRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardBySubApplicationIdAsync(QueryWardBySubApplicationIdRequest queryWardBySubApplicationIdRequest) => Unsupported.QueryWardBySubApplicationIdAsync(queryWardBySubApplicationIdRequest);

    /// <inheritdoc/>
    public Task<WardDto> GetWardByIdAsync(GetWardByIdRequest getWardByIdRequest) => Unsupported.GetWardByIdAsync(getWardByIdRequest);

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryAllWardAsync() => Unsupported.QueryAllWardAsync();

    /// <inheritdoc/>
    public Task<string> CreateOrganizationAsync(CreateOrganizationRequest createOrganizationRequest) => Unsupported.CreateOrganizationAsync(createOrganizationRequest);

    /// <inheritdoc/>
    public Task<int> UpdateOrganizationAsync(UpdateOrganizationRequest updateOrganizationRequest) => Unsupported.UpdateOrganizationAsync(updateOrganizationRequest);

    /// <inheritdoc/>
    public Task<int> DeleteOrganizationAsync(DeleteOrganizationRequest deleteOrganizationRequest) => Unsupported.DeleteOrganizationAsync(deleteOrganizationRequest);

    /// <inheritdoc/>
    public Task<int> EnableOrganizationAsync(EnableOrganizationRequest enableOrganizationRequest) => Unsupported.EnableOrganizationAsync(enableOrganizationRequest);

    /// <inheritdoc/>
    public Task<int> DisableOrganizationAsync(DisableOrganizationRequest disableOrganizationRequest) => Unsupported.DisableOrganizationAsync(disableOrganizationRequest);

    /// <inheritdoc/>
    public Task<string> CreateHospitalAsync(CreateHospitalRequest createHospitalRequest) => Unsupported.CreateHospitalAsync(createHospitalRequest);

    /// <inheritdoc/>
    public Task<int> UpdateHospitalAsync(UpdateHospitalRequest updateHospitalRequest) => Unsupported.UpdateHospitalAsync(updateHospitalRequest);

    /// <inheritdoc/>
    public Task<int> DeleteHospitalAsync(DeleteHospitalRequest deleteHospitalRequest) => Unsupported.DeleteHospitalAsync(deleteHospitalRequest);

    /// <inheritdoc/>
    public Task<int> EnableHospitalAsync(EnableHospitalRequest enableHospitalRequest) => Unsupported.EnableHospitalAsync(enableHospitalRequest);

    /// <inheritdoc/>
    public Task<int> DisableHospitalAsync(DisableHospitalRequest disableHospitalRequest) => Unsupported.DisableHospitalAsync(disableHospitalRequest);

    /// <inheritdoc/>
    public Task<string> CreateBranchAsync(CreateBranchRequest createBranchRequest) => Unsupported.CreateBranchAsync(createBranchRequest);

    /// <inheritdoc/>
    public Task<int> UpdateBranchAsync(UpdateBranchRequest updateBranchRequest) => Unsupported.UpdateBranchAsync(updateBranchRequest);

    /// <inheritdoc/>
    public Task<int> DeleteBranchAsync(DeleteBranchRequest deleteBranchRequest) => Unsupported.DeleteBranchAsync(deleteBranchRequest);

    /// <inheritdoc/>
    public Task<int> EnableBranchAsync(EnableBranchRequest enableBranchRequest) => Unsupported.EnableBranchAsync(enableBranchRequest);

    /// <inheritdoc/>
    public Task<int> DisableBranchAsync(DisableBranchRequest disableBranchRequest) => Unsupported.DisableBranchAsync(disableBranchRequest);

    /// <inheritdoc/>
    public Task<Guid> CreateDeptAsync(CreateDeptRequest createDeptRequest) => Unsupported.CreateDeptAsync(createDeptRequest);

    /// <inheritdoc/>
    public Task<int> UpdateDeptAsync(UpdateDeptRequest updateDeptRequest) => Unsupported.UpdateDeptAsync(updateDeptRequest);

    /// <inheritdoc/>
    public Task<int> DeleteDeptAsync(DeleteDeptRequest deleteDeptRequest) => Unsupported.DeleteDeptAsync(deleteDeptRequest);

    /// <inheritdoc/>
    public Task<int> EnableDeptAsync(EnableDeptRequest enableDeptRequest) => Unsupported.EnableDeptAsync(enableDeptRequest);

    /// <inheritdoc/>
    public Task<int> DisableDeptAsync(DisableDeptRequest disableDeptRequest) => Unsupported.DisableDeptAsync(disableDeptRequest);

    /// <inheritdoc/>
    public Task<int> MoveDeptUpAsync(MoveDeptUpRequest moveDeptUpRequest) => Unsupported.MoveDeptUpAsync(moveDeptUpRequest);

    /// <inheritdoc/>
    public Task<int> MoveDeptDownAsync(MoveDeptDownRequest moveDeptDownRequest) => Unsupported.MoveDeptDownAsync(moveDeptDownRequest);

    /// <inheritdoc/>
    public Task<Guid> CreateDeptWardAsync(CreateDeptWardRequest createDeptWardRequest) => Unsupported.CreateDeptWardAsync(createDeptWardRequest);

    /// <inheritdoc/>
    public Task<int> UpdateDeptWardAsync(UpdateDeptWardRequest updateDeptWardRequest) => Unsupported.UpdateDeptWardAsync(updateDeptWardRequest);

    /// <inheritdoc/>
    public Task<int> DeleteDeptWardAsync(DeleteDeptWardRequest deleteDeptWardRequest) => Unsupported.DeleteDeptWardAsync(deleteDeptWardRequest);

    /// <inheritdoc/>
    public Task<int> EnableDeptWardAsync(EnableDeptWardRequest enableDeptWardRequest) => Unsupported.EnableDeptWardAsync(enableDeptWardRequest);

    /// <inheritdoc/>
    public Task<int> DisableDeptWardAsync(DisableDeptWardRequest disableDeptWardRequest) => Unsupported.DisableDeptWardAsync(disableDeptWardRequest);

    /// <inheritdoc/>
    public Task<Guid> CreateWardAsync(CreateWardRequest createWardRequest) => Unsupported.CreateWardAsync(createWardRequest);

    /// <inheritdoc/>
    public Task<int> UpdateWardAsync(UpdateWardRequest updateWardRequest) => Unsupported.UpdateWardAsync(updateWardRequest);

    /// <inheritdoc/>
    public Task<int> DeleteWardAsync(DeleteWardRequest deleteWardRequest) => Unsupported.DeleteWardAsync(deleteWardRequest);

    /// <inheritdoc/>
    public Task<int> EnableWardAsync(EnableWardRequest enableWardRequest) => Unsupported.EnableWardAsync(enableWardRequest);

    /// <inheritdoc/>
    public Task<int> DisableWardAsync(DisableWardRequest disableWardRequest) => Unsupported.DisableWardAsync(disableWardRequest);

    /// <inheritdoc/>
    public Task<int> MoveWardUpAsync(MoveWardUpRequest moveWardUpRequest) => Unsupported.MoveWardUpAsync(moveWardUpRequest);

    /// <inheritdoc/>
    public Task<int> MoveWardDownAsync(MoveWardDownRequest moveWardDownRequest) => Unsupported.MoveWardDownAsync(moveWardDownRequest);
  }
}
