using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application;
using Dy.MedicalRecognition.Application.OrganizationScope;
using Dy.MedicalRecognition.Application.Validation;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 组织路径解析的应用层行为校验（矩阵 V8-V10、V13、V19）：
/// 组织、医院、院区三层的存在、启用与类型校验，带首尾空白与取不到该层的拒绝口径，
/// 每层各唯一命中时的父子归属校验，以及外部读取次数与目标行数无关。
/// </summary>
/// <remarks>
/// 编码是业务键、组织节点标识不是：同一业务编码在 IAM 目录中命中多个节点时不构成业务拒绝，
/// 多候选时跳过父子校验，名称按容错口径回填，返回路径的实例标识一律留空。
/// </remarks>
/// <remarks>
/// 外部组织服务使用手写替身 <see cref="FakeOrganizationAppService"/>，不发起任何真实 HTTP 调用，也不写库。
/// 替身记录组织、医院、院区三类读取的次数与收到的父级编码，供调用次数断言使用。
/// </remarks>
public sealed class Stage3OrganizationPathTests
{
  /// <summary>用例使用的组织业务编码。</summary>
  private const string OrganizationCode = "11111111-1111-1111-1111-111111111111";
  /// <summary>用例使用的医院业务编码。</summary>
  private const string HospitalCode = "22222222-2222-2222-2222-222222222222";
  /// <summary>用例使用的院区业务编码。</summary>
  private const string BranchCode = "33333333-3333-3333-3333-333333333333";
  /// <summary>另一个组织的业务编码，用于构造父级归属不匹配的数据。</summary>
  private const string OtherOrganizationCode = "44444444-4444-4444-4444-444444444444";
  /// <summary>另一家医院的业务编码，用于构造父级归属不匹配与越权院区数据。</summary>
  private const string OtherHospitalCode = "55555555-5555-5555-5555-555555555555";
  /// <summary>校验通过时返回的组织名称。</summary>
  private const string OrganizationName = "示例组织";
  /// <summary>校验通过时返回的医院名称。</summary>
  private const string HospitalName = "示例医院";
  /// <summary>校验通过时返回的院区名称。</summary>
  private const string BranchName = "示例院区";
  /// <summary>组织不可用时的对外拒绝文案。</summary>
  private const string MissingOrganizationMessage = "组织不可用。";
  /// <summary>医院不可用时的对外拒绝文案。</summary>
  private const string MissingHospitalMessage = "医院不可用。";
  /// <summary>院区不可用时的对外拒绝文案。</summary>
  private const string MissingBranchMessage = "院区不可用。";
  /// <summary>目录里只挂在非医院类型节点上的自定义医院编码。</summary>
  private const string NonHospitalCode = "CSYY9";
  /// <summary>与默认医院编码共用同一业务编码的院区类型节点标识。</summary>
  private static readonly Guid DuplicateHospitalCodeNodeId = Guid.Parse("90000000-0000-0000-0000-000000000001");
  /// <summary>与默认组织编码共用同一业务编码的医共体节点标识。</summary>
  private static readonly Guid DuplicateOrganizationCodeNodeId = Guid.Parse("90000000-0000-0000-0000-000000000002");
  /// <summary>只挂自定义医院编码的院区类型节点标识。</summary>
  private static readonly Guid BranchTypeOnlyNodeId = Guid.Parse("90000000-0000-0000-0000-000000000003");
  /// <summary>只挂自定义医院编码的医共体类型节点标识。</summary>
  private static readonly Guid CommunityTypeOnlyNodeId = Guid.Parse("90000000-0000-0000-0000-000000000004");

  /// <summary>组织、医院、院区三层都命中时返回三层的业务编码与名称，且不产生任何写库动作。</summary>
  [Fact]
  public async Task Resolve_returns_three_level_names_when_the_whole_path_is_valid()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, HospitalCode, OrganizationCode)] }
    };

    OrganizationPath path = Assert.Single(await Resolve(CreateResolver(service)));

    Assert.Equal(OrganizationCode, path.OrganizationCode);
    Assert.Equal(OrganizationName, path.OrganizationName);
    Assert.Equal(HospitalCode, path.HospitalCode);
    Assert.Equal(HospitalName, path.HospitalName);
    Assert.Equal(BranchCode, path.BranchCode);
    Assert.Equal(BranchName, path.BranchName);
    Assert.Equal(0, service.WriteCount);
  }

  /// <summary>平台入口提交三层业务编码时按 IAM 目录校验，并返回提交的编码与目录名称。</summary>
  /// <remarks>
  /// 编码是业务键、组织节点标识不是：校验只判定存在性、类型与可定论的父子归属，
  /// 返回路径不为数据用途挑选节点，因此三层实例标识一律留空。
  /// </remarks>
  [Fact]
  public async Task Resolve_validates_submitted_business_codes_and_returns_their_names()
  {
    FakeIamScope scope = FakeIamScope.Default;
    OrganizationPathResolver resolver = BuildResolver(scope, scope);

    OrganizationPath path = Assert.Single(await resolver.ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    Assert.Equal("示范医共体", path.OrganizationName);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
    Assert.Equal("示范院区", path.BranchName);
    Assert.Equal(Guid.Empty, path.OrganizationId);
    Assert.Equal(Guid.Empty, path.HospitalId);
    Assert.Equal(Guid.Empty, path.BranchId);
  }

  /// <summary>平台入口提交目录中不存在的组织业务编码时按组织文案拒绝。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_unknown_organization_code()
  {
    FakeIamScope scope = FakeIamScope.Default;
    OrganizationPathResolver resolver = BuildResolver(scope, scope);

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveOrThrow(
      [new OrganizationPathTarget("ORG-NOT-CONFIGURED", FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingOrganizationMessage, exception.Message);
  }

  /// <summary>平台入口提交已停用组织的业务编码时按组织文案拒绝。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_disabled_organization_code()
  {
    FakeIamScope scope = FakeIamScope.DisabledOrganization();
    OrganizationPathResolver resolver = BuildResolver(scope, scope);

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingOrganizationMessage, exception.Message);
  }

  /// <summary>平台入口提交父子不匹配的医院时按医院文案拒绝。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_hospital_outside_the_submitted_organization()
  {
    FakeIamScope scope = FakeIamScope.Default;
    OrganizationPathResolver resolver = BuildResolver(scope, scope);

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.OtherHospitalCode, FakeIamScope.OtherBranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>组织读取返回的启用组织里没有该编码时拒绝保存或查询（V8）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_organization_does_not_exist()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OtherOrganizationCode, OrganizationName)]
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingOrganizationMessage, exception.Message);
  }

  /// <summary>组织存在但已停用时拒绝保存或查询（V8）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_organization_is_disabled()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName, isValid: false)]
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingOrganizationMessage, exception.Message);
  }

  /// <summary>医院读取返回的启用医院里没有该编码时拒绝（V9）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_hospital_does_not_exist()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(OtherHospitalCode, HospitalName, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>医院存在但已停用时拒绝，即使外部服务把它放进了启用医院集合（V9）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_hospital_is_disabled()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode, isValid: false)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>医院编码在组织下的启用医院里存在，但父组织不是请求组织时拒绝（V9）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_hospital_belongs_to_another_organization()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OtherOrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>院区读取返回的启用院区里没有该编码时拒绝（V10）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_branch_does_not_exist()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch("66666666-6666-6666-6666-666666666666", BranchName, HospitalCode, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>院区存在但已停用时拒绝，即使外部服务把它放进了启用院区集合（V10）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_branch_is_disabled()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, HospitalCode, OrganizationCode, isValid: false)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>医院管理员入口提交的院区不属于可信医院时拒绝，不降级为空值（V13）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_branch_org_differs_from_selected_org()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, OtherHospitalCode, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>院区属于其他医院时按院区文案拒绝（V13）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_requested_branch_belongs_to_another_hospital()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, OtherHospitalCode, OtherOrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(CreateResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>
  /// 目标路径三层编码带首尾空白、且外部组织服务返回的业务编码同样带首尾空白时，
  /// 按去空白后的编码校验成功，并且返回的三层编码一律是去空白值。
  /// </summary>
  /// <remarks>
  /// 请求提交值与可信上下文值都可能带首尾空白：解析器承诺三层一律去空白后比较，
  /// 因此带空白的目标路径必须与不带空白的目标路径得到完全相同的结果，而不是在校验通过后查找失败。
  /// 外部主数据也可能带首尾空白：匹配用的是 <c>Id</c> 去空白后的值，返回值必须与匹配口径同源，
  /// 否则保存与查询会拿到未去空白的编码写入或过滤，与按去空白令牌写入的互认配置表不同源，
  /// 表现为误报"未建立互认配置"或查询出空表；带空白的目标路径用例的返回值若回退成外部原值，本用例必须失败。
  /// </remarks>
  [Fact]
  public async Task Resolve_accepts_codes_with_surrounding_whitespace_and_returns_trimmed_codes()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization($"  {OrganizationCode}  ", OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital($"  {HospitalCode}  ", HospitalName, $"  {OrganizationCode}  ")] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch($"  {BranchCode}  ", BranchName, $"  {HospitalCode}  ", $"  {OrganizationCode}  ")] }
    };

    OrganizationPath path = Assert.Single(
      await ResolveTargets(service, [new OrganizationPathTarget($" {OrganizationCode} ", $" {HospitalCode} ", $" {BranchCode} ")]));

    // 三层编码与匹配口径同源：外部主数据带空白时返回值仍必须是去空白值。
    Assert.Equal(OrganizationCode, path.OrganizationCode);
    Assert.Equal(HospitalCode, path.HospitalCode);
    Assert.Equal(BranchCode, path.BranchCode);
    // 名称保持外部服务原值，不受编码去空白影响。
    Assert.Equal(OrganizationName, path.OrganizationName);
    Assert.Equal(HospitalName, path.HospitalName);
    Assert.Equal(BranchName, path.BranchName);
    Assert.Equal(0, service.WriteCount);
  }

  /// <summary>目标路径某一层编码为 null、空串或纯空白时按该层文案拒绝，不抛未处理异常。</summary>
  /// <remarks>
  /// 三层编码都先去除首尾空白再判定：取不到该层时是业务拒绝（<see cref="InvalidOperationException"/> 且消息为调用方传入的该层文案），
  /// 不是查找失败类异常；已成立的上层不影响该判定，判定顺序为组织、医院、院区。
  /// </remarks>
  /// <param name="organizationCode">目标组织编码；null、空串与纯空白表示该层取不到值。</param>
  /// <param name="hospitalCode">目标医院编码；null、空串与纯空白表示该层取不到值。</param>
  /// <param name="branchCode">目标院区编码；null、空串与纯空白表示该层取不到值。</param>
  /// <param name="expectedMessage">期望的该层对外拒绝文案。</param>
  [Theory]
  [InlineData(null, HospitalCode, BranchCode, MissingOrganizationMessage)]
  [InlineData("", HospitalCode, BranchCode, MissingOrganizationMessage)]
  [InlineData("   ", HospitalCode, BranchCode, MissingOrganizationMessage)]
  [InlineData(OrganizationCode, null, BranchCode, MissingHospitalMessage)]
  [InlineData(OrganizationCode, "", BranchCode, MissingHospitalMessage)]
  [InlineData(OrganizationCode, "   ", BranchCode, MissingHospitalMessage)]
  [InlineData(OrganizationCode, HospitalCode, null, MissingBranchMessage)]
  [InlineData(OrganizationCode, HospitalCode, "", MissingBranchMessage)]
  [InlineData(OrganizationCode, HospitalCode, "   ", MissingBranchMessage)]
  public async Task Resolve_rejects_the_missing_layer_with_that_layer_message(string? organizationCode, string? hospitalCode, string? branchCode, string expectedMessage)
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, HospitalCode, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
      () => ResolveTargets(service, [new OrganizationPathTarget(organizationCode, hospitalCode, branchCode)]));

    Assert.Equal(expectedMessage, exception.Message);
  }

  /// <summary>同一批目标路径只读一次组织、按不同组织各读一次医院、按不同医院各读一次院区，10 行与 100 行的读取次数必须相等（V19）。</summary>
  /// <remarks>
  /// 两条目标路径在 10 行与 100 行两组数据里都只涉及 1 个组织、2 家医院、2 个院区，因此读取次数必须与目标行数无关。
  /// 若解析退化为逐行读取，两组计数会分别放大到 10 与 100，本用例据此失败。
  /// </remarks>
  [Fact]
  public async Task External_read_counts_do_not_grow_with_the_number_of_target_rows()
  {
    FakeOrganizationAppService tenRowService = BuildPathService();
    FakeOrganizationAppService hundredRowService = BuildPathService();

    await ResolveBatch(tenRowService, 10);
    await ResolveBatch(hundredRowService, 100);

    (int Organizations, int Hospitals, int Branches) tenRowReads = (tenRowService.OrganizationReadCount, tenRowService.HospitalReadCount, tenRowService.BranchReadCount);
    (int Organizations, int Hospitals, int Branches) hundredRowReads = (hundredRowService.OrganizationReadCount, hundredRowService.HospitalReadCount, hundredRowService.BranchReadCount);

    Assert.Equal(tenRowReads, hundredRowReads);
    Assert.Equal(1, tenRowReads.Organizations);
    Assert.Equal(1, tenRowReads.Hospitals);
    Assert.Equal(0, tenRowReads.Branches);
  }

  /// <summary>
  /// 一次调用处理 N 条目标时，IAM 目录与组织编码树的读取次数与 N 无关：组织与医院两层入口把整批目标合成一次读取（V19）。
  /// </summary>
  /// <remarks>
  /// 两条目标轮转复用的始终是 1 个组织与 2 家医院；若入口退化为逐条校验，目录读取次数会等于目标条数，本用例据此失败。
  /// 编码读取含「业务编码到实例标识」与「实例标识到业务编码」两次整树读取，同样不随目标条数增长。
  /// </remarks>
  [Fact]
  public async Task Organization_hospital_path_reads_do_not_grow_with_the_number_of_target_rows()
  {
    FakeIamScope tenRows = new();
    FakeIamScope hundredRows = new();

    await ResolveOrganizationHospitalBatch(tenRows, 10);
    await ResolveOrganizationHospitalBatch(hundredRows, 100);

    // 目录读取一次即覆盖整批目标：多一条目标不再多读一次目录。
    Assert.Equal(1, tenRows.BatchReadCount);
    Assert.Equal(tenRows.BatchReadCount, hundredRows.BatchReadCount);
    Assert.Equal(tenRows.BusinessCodeQueryCount, hundredRows.BusinessCodeQueryCount);
  }

  /// <summary>提交的医院编码对应两个节点、其中一个符合医院类型时校验通过，不因编码不唯一拒绝。</summary>
  /// <remarks>
  /// 编码是业务键，组织节点标识不是：同一编码挂在两个节点上不影响业务表与查询过滤（两者都用编码文本），
  /// 因此校验只判定「存在启用节点」与「存在符合该层类型的节点」。旧实现要求编码唯一命中，
  /// 本用例在旧实现下抛「对应多个组织节点」的业务拒绝。
  /// </remarks>
  [Fact]
  public async Task Resolve_iam_accepts_a_hospital_code_matching_two_nodes_when_one_matches_the_hospital_type()
  {
    FakeIamScope scope = new();
    // 同一医院编码同时挂在一个院区类型的节点上：两个候选里只有默认医院节点符合医院类型。
    scope.Organizations[DuplicateHospitalCodeNodeId] =
      BuildDirectoryNode(DuplicateHospitalCodeNodeId, "示范院区九", FakeIamScope.OrganizationId, "Branch");
    scope.Codes[DuplicateHospitalCodeNodeId] = FakeIamScope.HospitalCode;

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    // 名称仍按容错口径取值：取会话组织链上那个候选的名称。
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
    Assert.Equal("示范院区", path.BranchName);
    // 不为数据用途挑节点：即使类型判定后只剩一个候选，实例标识也不回填。
    Assert.Equal(Guid.Empty, path.OrganizationId);
    Assert.Equal(Guid.Empty, path.HospitalId);
    Assert.Equal(Guid.Empty, path.BranchId);
  }

  /// <summary>组织编码命中两个医共体节点时校验通过；父子归属因无法定论而跳过。</summary>
  /// <remarks>
  /// 组织层有两个候选时，编码本身无法说明提交方指向哪一个，父子关系也就无从定论；
  /// 父子校验服务于写入归属，无法定论时不能据此拒绝，因此本用例要求校验通过。
  /// </remarks>
  [Fact]
  public async Task Resolve_iam_accepts_an_organization_code_matching_two_nodes_and_skips_parent_child_check()
  {
    FakeIamScope scope = new();
    // 第二个医共体节点与该编码共用同一个业务编码，也是医共体类型：类型判定成立，父子归属无法定论。
    scope.Organizations[DuplicateOrganizationCodeNodeId] =
      BuildDirectoryNode(DuplicateOrganizationCodeNodeId, "示范医共体二", null, "CountyMedicalCommunity");
    scope.Codes[DuplicateOrganizationCodeNodeId] = FakeIamScope.OrganizationCode;

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    // 会话组织链上只有一个候选，名称取该候选的名称。
    Assert.Equal("示范医共体", path.OrganizationName);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
    Assert.Equal("示范院区", path.BranchName);
    Assert.Equal(Guid.Empty, path.OrganizationId);
  }

  /// <summary>编码命中若干节点但没有任何一个符合该层类型时，仍按该层既有文案业务拒绝。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_a_hospital_code_whose_nodes_are_not_hospitals()
  {
    FakeIamScope scope = new();
    // 两个节点都挂该医院编码，但类型分别是院区与医共体：没有任何一个符合医院层。
    scope.Organizations[BranchTypeOnlyNodeId] =
      BuildDirectoryNode(BranchTypeOnlyNodeId, "示范院区七", FakeIamScope.OrganizationId, "Branch");
    scope.Codes[BranchTypeOnlyNodeId] = NonHospitalCode;
    scope.Organizations[CommunityTypeOnlyNodeId] =
      BuildDirectoryNode(CommunityTypeOnlyNodeId, "示范医共体七", null, "CountyMedicalCommunity");
    scope.Codes[CommunityTypeOnlyNodeId] = NonHospitalCode;

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, NonHospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>编码命中 0 个节点时仍按该层既有文案拒绝，即使其他层存在多个候选。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_a_layer_code_matching_no_node_even_when_another_layer_is_ambiguous()
  {
    FakeIamScope scope = new();
    scope.Organizations[DuplicateOrganizationCodeNodeId] =
      BuildDirectoryNode(DuplicateOrganizationCodeNodeId, "示范医共体二", null, "CountyMedicalCommunity");
    scope.Codes[DuplicateOrganizationCodeNodeId] = FakeIamScope.OrganizationCode;

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, "CSYY-NOT-CONFIGURED", FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingHospitalMessage, exception.Message);
  }

  /// <summary>每层各唯一命中时父子归属仍在校验：院区不属于所选医院时按院区文案拒绝。</summary>
  [Fact]
  public async Task Resolve_iam_rejects_a_branch_that_does_not_belong_to_the_unique_hospital()
  {
    FakeIamScope scope = new();

    // 三层各命中一个候选：院区挂在默认医院下，提交的却是同一组织下的第二家医院。
    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.SecondHospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>编码命中多个节点时名称取会话组织链上那个候选的名称，实例标识不因取名而回填。</summary>
  /// <remarks>
  /// 会话组织链是名称回填唯一可观察的可信依据：本用例把当前会话放在「其他医共体 → 其他医院 → 其他院区」这条链上，
  /// 同一编码同时挂在两条链的医共体节点上，只有链上候选的名称可以被确定。
  /// </remarks>
  [Fact]
  public async Task Resolve_names_take_the_candidate_on_the_current_session_organization_chain()
  {
    FakeIamScope scope = new()
    {
      Identity = new(
        FakeIamScope.OtherHospitalId,
        FakeIamScope.WorkIdentityId,
        FakeIamScope.TenantId,
        FakeIamScope.OtherBranchId,
        null,
        "medical-recognition",
        Guid.NewGuid(),
        1,
        1,
        1)
    };
    scope.Codes[FakeIamScope.OtherOrganizationId] = FakeIamScope.OrganizationCode;

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveNamesOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.OtherHospitalCode, FakeIamScope.OtherBranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    Assert.Equal("其他医共体", path.OrganizationName);
    Assert.Equal("其他医院", path.HospitalName);
    Assert.Equal("其他院区", path.BranchName);
    // 名称取自会话组织链，但实例标识不因此被填成链上节点：不为数据用途挑节点。
    Assert.Equal(Guid.Empty, path.OrganizationId);
    Assert.Equal(Guid.Empty, path.HospitalId);
    Assert.Equal(Guid.Empty, path.BranchId);
  }

  /// <summary>编码命中的多个节点名称一致时按该名称回填，整批校验通过。</summary>
  [Fact]
  public async Task Resolve_names_use_the_shared_name_when_two_candidates_of_one_code_agree()
  {
    FakeIamScope scope = FakeIamScope.WithDuplicateOtherHospitalCode(name: "其他医院");

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveNamesOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OtherOrganizationCode, FakeIamScope.OtherHospitalCode, FakeIamScope.OtherBranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal("其他医共体", path.OrganizationName);
    Assert.Equal("其他医院", path.HospitalName);
    Assert.Equal("其他院区", path.BranchName);
    Assert.Equal(Guid.Empty, path.HospitalId);
  }

  /// <summary>编码命中的多个节点名称不同且都不在会话组织链上时名称留空，不抛异常、不回落编码文本。</summary>
  [Fact]
  public async Task Resolve_names_leave_the_name_empty_when_candidates_of_one_code_disagree()
  {
    FakeIamScope scope = FakeIamScope.WithDuplicateOtherHospitalCode(name: "其他医院二");

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveNamesOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OtherOrganizationCode, FakeIamScope.OtherHospitalCode, FakeIamScope.OtherBranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OtherHospitalCode, path.HospitalCode);
    Assert.Equal(string.Empty, path.HospitalName);
    Assert.Equal("其他医共体", path.OrganizationName);
    Assert.Equal("其他院区", path.BranchName);
  }

  /// <summary>编码命中的多个节点都不在会话组织链上时校验照常通过，名称留空而不是取其中一个候选。</summary>
  /// <remarks>
  /// 归属校验路径不再因编码不唯一拒绝：两个候选名称不同且都不在会话组织链上，名称按容错规则留空，
  /// 行本身照常返回。旧实现把这种情况整次业务拒绝，本用例在旧实现下失败。
  /// </remarks>
  [Fact]
  public async Task Resolve_accepts_a_business_code_matching_multiple_organization_nodes_and_leaves_the_name_empty()
  {
    FakeIamScope scope = new();
    // 会话链上的医共体不配置该编码，编码落在链外的两个节点上（一个是医共体、一个是医院类型）。
    scope.Codes.Remove(FakeIamScope.OrganizationId);
    scope.Codes[FakeIamScope.OtherOrganizationId] = FakeIamScope.OrganizationCode;
    scope.Codes[FakeIamScope.SecondHospitalId] = FakeIamScope.OrganizationCode;

    OrganizationPath path = Assert.Single(await CreateIamResolver(scope).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    // 两个候选名称不同且都不在会话组织链上：名称留空，不猜、不回落编码文本。
    Assert.Equal(string.Empty, path.OrganizationName);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
    Assert.Equal(Guid.Empty, path.OrganizationId);
  }

  /// <summary>把一条目标路径交给解析器校验，供各用例共用同一入参构造。</summary>
  /// <param name="resolver">待验证的组织路径解析器。</param>
  /// <returns>校验通过时的三层业务编码与名称。</returns>
  private static Task<IReadOnlyList<OrganizationPath>> Resolve(OrganizationPathResolver resolver) =>
    resolver.ResolveOrThrow(
      [new OrganizationPathTarget(OrganizationCode, HospitalCode, BranchCode)],
      MissingOrganizationMessage,
      MissingHospitalMessage,
      MissingBranchMessage);

  /// <summary>构造路径解析用例的替身：1 个组织、2 家医院各 1 个院区。</summary>
  /// <returns>只含 2 条可解析目标路径的替身；行数由调用方在同一路径上重复以放大行数。</returns>
  private static FakeOrganizationAppService BuildPathService()
  {
    FakeOrganizationAppService service = new() { Organizations = [BuildOrganization(OrganizationCode, OrganizationName)] };
    service.HospitalsByOrganization[OrganizationCode] = [];
    service.HospitalsByOrganization[OrganizationCode].Add(BuildHospital(HospitalCode, HospitalName, OrganizationCode));
    service.HospitalsByOrganization[OrganizationCode].Add(BuildHospital(OtherHospitalCode, HospitalName, OrganizationCode));
    service.BranchesByHospital[HospitalCode] = [BuildBranch("33333333-3333-3333-3333-333333333333", BranchName, HospitalCode, OrganizationCode)];
    service.BranchesByHospital[OtherHospitalCode] = [BuildBranch("77777777-7777-7777-7777-777777777777", BranchName, OtherHospitalCode, OrganizationCode)];

    return service;
  }

  /// <summary>把一批目标路径交给解析器校验，供各用例共用同一入参构造。</summary>
  /// <param name="service">提供组织、医院与院区的替身。</param>
  /// <param name="targets">本批待校验的目标路径。</param>
  /// <returns>与目标路径等长且同序的已校验路径集合。</returns>
  private static Task<IReadOnlyList<OrganizationPath>> ResolveTargets(FakeOrganizationAppService service, IReadOnlyList<OrganizationPathTarget> targets) =>
    CreateResolver(service).ResolveOrThrow(targets, MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage);

  /// <summary>在两条固定目标路径之间轮转形成指定行数，模拟同一批请求里包含大量重复路径的目标行。</summary>
  /// <param name="service">提供组织、医院与院区的替身。</param>
  /// <param name="rowCount">本次目标行数；只影响返回条数，不改变涉及的组织、医院与院区数量。</param>
  private static async Task ResolveBatch(FakeOrganizationAppService service, int rowCount)
  {
    OrganizationPathTarget[] targets =
      [.. Enumerable.Range(1, rowCount).Select(index => index % 2 == 0
        ? new OrganizationPathTarget(OrganizationCode, HospitalCode, "33333333-3333-3333-3333-333333333333")
        : new OrganizationPathTarget(OrganizationCode, OtherHospitalCode, "77777777-7777-7777-7777-777777777777"))];

    IReadOnlyList<OrganizationPath> paths = await ResolveTargets(service, targets);

    Assert.Equal(targets.Length, paths.Count);
  }

  /// <summary>构造外部组织服务返回的组织。</summary>
  /// <param name="id">组织业务编码，由外部服务的 <c>Id</c> 承载。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="isValid">组织是否启用。</param>
  /// <returns>组织返回对象。</returns>
  private static OrganizationDto BuildOrganization(string id, string name, bool isValid = true) => new() { Id = id, Name = name, IsValid = isValid };

  /// <summary>构造外部组织服务返回的医院。</summary>
  /// <param name="id">医院业务编码，由外部服务的 <c>Id</c> 承载。</param>
  /// <param name="name">医院名称。</param>
  /// <param name="organizationId">医院所属组织编码。</param>
  /// <param name="isValid">医院是否启用。</param>
  /// <returns>医院返回对象。</returns>
  private static HospitalDto BuildHospital(string id, string name, string organizationId, bool isValid = true) =>
    new() { Id = id, Name = name, OrgId = organizationId, IsValid = isValid };

  /// <summary>构造外部组织服务返回的院区。</summary>
  /// <param name="id">院区业务编码，由外部服务的 <c>Id</c> 承载。</param>
  /// <param name="name">院区名称。</param>
  /// <param name="hospitalId">院区所属医院编码。</param>
  /// <param name="organizationId">院区所属组织编码。</param>
  /// <param name="isValid">院区是否启用。</param>
  /// <returns>院区返回对象。</returns>
  private static BranchDto BuildBranch(string id, string name, string hospitalId, string organizationId, bool isValid = true) =>
    new() { Id = id, Name = name, HosId = hospitalId, OrgId = organizationId, IsValid = isValid };

  /// <summary>构造 IAM 目录节点，供「同一业务编码命中多个节点」的用例写入替身。</summary>
  /// <param name="id">组织实例标识。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="parentId">父组织标识；根节点为空。</param>
  /// <param name="typeCode">组织类型编码。</param>
  /// <returns>启用状态的组织详情。</returns>
  private static IamDirectoryOrganization BuildDirectoryNode(Guid id, string name, Guid? parentId, string typeCode) =>
    new(id, name, parentId, true, [.. (parentId is { } parent ? new[] { parent, id } : new[] { id })
      .Select(item => new IamDirectoryPathItem(item, $"组织-{item:N}"))])
    {
      OrganizationType = new IamDirectoryOrganizationType(Guid.NewGuid(), typeCode, typeCode, true)
    };

  /// <summary>把旧组织服务替身适配为 IAM 目录客户端，保持原用例的组织、医院、院区数据与读取计数口径。</summary>
  private sealed class FakeOrganizationDirectoryClient : IIamDirectoryClient
  {
    private readonly FakeOrganizationAppService service;

    public FakeOrganizationDirectoryClient(FakeOrganizationAppService service) => this.service = service;

    public async Task<IamDirectoryReadResult> BatchReadDirectoryAsync(IamDirectoryReadRequest request, CancellationToken cancellationToken)
    {
      Dictionary<Guid, IamDirectoryOrganization> nodes = [];
      IReadOnlyList<OrganizationDto> organizations = [.. await service.QueryAllOrganizationAsync()];
      foreach (OrganizationDto organization in organizations)
      {
        Guid id = Guid.Parse(organization.Id);
        nodes[id] = Node(id, organization.Name, null, "CountyMedicalCommunity", organization.IsValid);
      }
      List<HospitalDto> allHospitals = [];
      foreach (OrganizationDto organization in organizations)
      {
        allHospitals.AddRange(await service.QueryAllValidHospitalByOrgIdAsync(
          new QueryAllValidHospitalByOrgIdRequest { OrgId = organization.Id.Trim() }));
      }
      foreach (HospitalDto hospital in allHospitals)
      {
        Guid id = Guid.Parse(hospital.Id);
        nodes[id] = Node(id, hospital.Name, Guid.Parse(hospital.OrgId), "Hospital", hospital.IsValid);
      }
      foreach ((string _, List<BranchDto> branches) in service.BranchesByHospital)
      {
        foreach (BranchDto branch in branches)
        {
          Guid id = Guid.Parse(branch.Id);
          nodes[id] = Node(id, branch.Name, Guid.Parse(branch.HosId.Trim()), "Branch", branch.IsValid);
        }
      }
      IamDirectoryReadItem[] items = [.. request.Ids.Select(id => nodes.TryGetValue(id, out IamDirectoryOrganization? organization)
        ? new IamDirectoryReadItem(id, true, new IamDirectoryData { Organization = organization })
        : new IamDirectoryReadItem(id, false, null!))];
      return new IamDirectoryReadResult(items, DateTimeOffset.UtcNow);
    }

    public Task<IamEmploymentValidationResult> BatchValidateEmploymentsAsync(IamEmploymentValidationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IamOrganizationDirectoryResult> QueryOrganizationDirectoryAsync(IamOrganizationDirectoryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public bool IsAuthenticated => true;
    public IamIdentityContext? Current => null;
    public IamIdentityContext RequireCurrent() => throw new NotSupportedException();

    private static IamDirectoryOrganization Node(Guid id, string name, Guid? parentId, string typeCode, bool isEnabled) =>
      new(id, name, parentId, isEnabled, [new IamDirectoryPathItem(id, $"组织-{id:N}")])
      {
        OrganizationType = new IamDirectoryOrganizationType(Guid.NewGuid(), typeCode, typeCode, true)
      };
  }

  /// <summary>用旧组织服务替身的数据构造 IAM 目录解析点。</summary>
  private static OrganizationPathResolver CreateResolver(FakeOrganizationAppService service) =>
    BuildResolver(new FakeOrganizationDirectoryClient(service), new FakeOrganizationCodeQueryClient(service));

  /// <summary>用 IAM 目录替身与业务编码替身构造解析点。</summary>
  /// <param name="directoryClient">IAM 目录读取替身。</param>
  /// <param name="codeQueryClient">组织业务编码读取替身。</param>
  /// <returns>解析点。</returns>
  private static OrganizationPathResolver BuildResolver(
    IIamDirectoryClient directoryClient, IOrganizationBusinessCodeQueryClient codeQueryClient)
  {
    IamOrganizationDirectoryReader reader = new(directoryClient);
    IamOrganizationBusinessCodeReader codeReader = new(codeQueryClient);
    return new OrganizationPathResolver(reader, codeReader, FakeIamScope.Default);
  }

  /// <summary>用同一个 IAM 替身同时充当目录读取、业务编码读取与当前上下文，构造计数口径自洽的解析点。</summary>
  /// <param name="scope">IAM 范围替身，同时提供目录、业务编码与当前身份。</param>
  /// <returns>解析点。</returns>
  private static OrganizationPathResolver CreateIamResolver(FakeIamScope scope)
  {
    IamOrganizationDirectoryReader reader = new(scope);
    IamOrganizationBusinessCodeReader codeReader = new(scope);
    return new OrganizationPathResolver(reader, codeReader, scope);
  }

  /// <summary>把 1 个组织、2 家医院的路径轮转成指定行数，交给组织与医院两层入口校验。</summary>
  /// <param name="scope">IAM 范围替身。</param>
  /// <param name="rowCount">本次目标行数；只影响返回条数，不改变涉及的组织与医院数量。</param>
  private static async Task ResolveOrganizationHospitalBatch(FakeIamScope scope, int rowCount)
  {
    string[] hospitalCodes = [FakeIamScope.HospitalCode, FakeIamScope.SecondHospitalCode];
    OrganizationPathTarget[] targets =
      [.. Enumerable.Range(0, rowCount).Select(index =>
        new OrganizationPathTarget(FakeIamScope.OrganizationCode, hospitalCodes[index % hospitalCodes.Length], null))];

    IReadOnlyList<OrganizationPath> paths = await CreateIamResolver(scope).ResolveOrganizationHospitalOrThrow(
      targets, MissingOrganizationMessage, MissingHospitalMessage);

    // 返回等长同序：第 i 条路径的组织与医院必须对应第 i 条目标，院区层不参与该入口。
    Assert.Equal(targets.Length, paths.Count);
    Assert.All(paths, path => Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode));
    // 编码是业务键、实例标识不是：校验不为数据用途挑节点，实例标识一律留空。
    Assert.All(paths, path => Assert.Equal(Guid.Empty, path.OrganizationId));
    Assert.Equal(targets.Select(target => target.HospitalCode), paths.Select(path => path.HospitalCode));
    Assert.All(paths, path => Assert.Equal(Guid.Empty, path.BranchId));
  }

  /// <summary>
  /// 组织业务编码读取替身：按目录替身的数据返回组织树节点，节点编码取用例预置的编码文本。
  /// </summary>
  /// <remarks>本文件的用例把编码当作不透明文本使用，替身据此登记编码与实例标识的对应关系，不推断层级。</remarks>
  private sealed class FakeOrganizationCodeQueryClient : IOrganizationBusinessCodeQueryClient
  {
    private readonly FakeOrganizationAppService service;

    public FakeOrganizationCodeQueryClient(FakeOrganizationAppService service) => this.service = service;

    public Task<OrganizationBusinessCodeTreeResponse> QueryOrganizationByTenantIdAsync(OrganizationBusinessCodeQueryRequest request)
    {
      List<OrganizationBusinessCodeTreeNode> nodes = [];
      foreach (OrganizationDto organization in service.Organizations)
      {
        Guid id = Guid.Parse(organization.Id.Trim());
        // 替身按去空白后的编码登记：编码文本以目录数据为准，首尾空白只出现在提交值一侧，由解析点统一去空白后比对。
        nodes.Add(new OrganizationBusinessCodeTreeNode { Id = id, Code = organization.Id.Trim(), IsEnabled = organization.IsValid });
      }

      foreach ((string _, List<HospitalDto> hospitals) in service.HospitalsByOrganization)
      {
        foreach (HospitalDto hospital in hospitals)
        {
          Guid id = Guid.Parse(hospital.Id.Trim());
          nodes.Add(new OrganizationBusinessCodeTreeNode { Id = id, Code = hospital.Id.Trim(), IsEnabled = hospital.IsValid });
        }
      }

      foreach ((string _, List<BranchDto> branches) in service.BranchesByHospital)
      {
        foreach (BranchDto branch in branches)
        {
          Guid id = Guid.Parse(branch.Id.Trim());
          nodes.Add(new OrganizationBusinessCodeTreeNode { Id = id, Code = branch.Id.Trim(), IsEnabled = branch.IsValid });
        }
      }

      return Task.FromResult(new OrganizationBusinessCodeTreeResponse { TenantId = request.TenantId, TenantIsEnabled = true, Nodes = [.. nodes] });
    }
  }

  /// <summary>
  /// 外部组织服务的手写替身：按组织、医院编码返回预置的启用数据，并记录三类读取的次数、入参与全部写方法的调用次数。
  /// </summary>
  private sealed class FakeOrganizationAppService : IOrganizationAppService
  {
    /// <summary>组织读取返回的启用组织集合。</summary>
    public List<OrganizationDto> Organizations { get; init; } = [];

    /// <summary>按组织编码预置的医院读取结果。</summary>
    public Dictionary<string, List<HospitalDto>> HospitalsByOrganization { get; } = [];

    /// <summary>按医院编码预置的院区读取结果。</summary>
    public Dictionary<string, List<BranchDto>> BranchesByHospital { get; } = [];

    /// <summary>组织读取次数。</summary>
    public int OrganizationReadCount { get; private set; }

    /// <summary>医院读取次数。</summary>
    public int HospitalReadCount { get; private set; }

    /// <summary>院区读取次数。</summary>
    public int BranchReadCount { get; private set; }

    /// <summary>医院读取收到的组织编码，按调用顺序记录。</summary>
    public List<string> ReceivedHospitalOrganizationCodes { get; } = [];

    /// <summary>院区读取收到的医院编码，按调用顺序记录。</summary>
    public List<string> ReceivedBranchHospitalCodes { get; } = [];

    /// <summary>全部写方法（新增、修改、删除、启用、停用）被调用的合计次数；组织路径解析不得触发任何写库动作。</summary>
    public int WriteCount { get; private set; }

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
      ReceivedHospitalOrganizationCodes.Add(queryAllValidHospitalByOrgIdRequest.OrgId);
      return Task.FromResult<IEnumerable<HospitalDto>>(HospitalsByOrganization.TryGetValue(queryAllValidHospitalByOrgIdRequest.OrgId, out List<HospitalDto>? hospitals) ? hospitals : []);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByHosIdAsync(QueryAllValidBranchByHosIdRequest queryAllValidBranchByHosIdRequest)
    {
      BranchReadCount++;
      ReceivedBranchHospitalCodes.Add(queryAllValidBranchByHosIdRequest.HosId);
      return Task.FromResult<IEnumerable<BranchDto>>(BranchesByHospital.TryGetValue(queryAllValidBranchByHosIdRequest.HosId, out List<BranchDto>? branches) ? branches : []);
    }
  }
}
