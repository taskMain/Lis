using Dy.Base.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application;
using Dy.MedicalRecognition.Application.Validation;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 组织路径解析的应用层行为校验（矩阵 V8-V10、V13、V19）：
/// 组织、医院、院区三层的存在、启用与父子归属校验，带首尾空白与取不到该层的拒绝口径，
/// 以及外部读取次数与目标行数无关。
/// </summary>
/// <remarks>
/// 外部组织服务使用手写替身 <see cref="FakeOrganizationAppService"/>，不发起任何真实 HTTP 调用，也不写库。
/// 替身记录组织、医院、院区三类读取的次数与收到的父级编码，供调用次数断言使用。
/// </remarks>
public sealed class Stage3OrganizationPathTests
{
  /// <summary>用例使用的组织业务编码。</summary>
  private const string OrganizationCode = "ORG-A";
  /// <summary>用例使用的医院业务编码。</summary>
  private const string HospitalCode = "HOS-1";
  /// <summary>用例使用的院区业务编码。</summary>
  private const string BranchCode = "BRH-1";
  /// <summary>另一个组织的业务编码，用于构造父级归属不匹配的数据。</summary>
  private const string OtherOrganizationCode = "ORG-B";
  /// <summary>另一家医院的业务编码，用于构造父级归属不匹配与越权院区数据。</summary>
  private const string OtherHospitalCode = "HOS-2";
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

    OrganizationPathResolver.OrganizationPath path = Assert.Single(await Resolve(new OrganizationPathResolver(service)));

    Assert.Equal(OrganizationCode, path.OrganizationCode);
    Assert.Equal(OrganizationName, path.OrganizationName);
    Assert.Equal(HospitalCode, path.HospitalCode);
    Assert.Equal(HospitalName, path.HospitalName);
    Assert.Equal(BranchCode, path.BranchCode);
    Assert.Equal(BranchName, path.BranchName);
    Assert.Equal(0, service.WriteCount);
  }

  /// <summary>组织读取返回的启用组织里没有该编码时拒绝保存或查询（V8）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_organization_does_not_exist()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OtherOrganizationCode, OrganizationName)]
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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
      BranchesByHospital = { [HospitalCode] = [BuildBranch("BRH-9", BranchName, HospitalCode, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>医院管理员入口提交的院区不属于可信医院时拒绝，不降级为空值（V13）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_requested_branch_belongs_to_another_hospital()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, OtherHospitalCode, OrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

    Assert.Equal(MissingBranchMessage, exception.Message);
  }

  /// <summary>院区父院区匹配但父组织不是可信组织时同样拒绝（V13）。</summary>
  [Fact]
  public async Task Resolve_rejects_when_the_requested_branch_belongs_to_another_organization()
  {
    FakeOrganizationAppService service = new()
    {
      Organizations = [BuildOrganization(OrganizationCode, OrganizationName)],
      HospitalsByOrganization = { [OrganizationCode] = [BuildHospital(HospitalCode, HospitalName, OrganizationCode)] },
      BranchesByHospital = { [HospitalCode] = [BuildBranch(BranchCode, BranchName, HospitalCode, OtherOrganizationCode)] }
    };

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(new OrganizationPathResolver(service)));

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

    OrganizationPathResolver.OrganizationPath path = Assert.Single(
      await ResolveTargets(service, [new OrganizationPathResolver.OrganizationPathTarget($" {OrganizationCode} ", $" {HospitalCode} ", $" {BranchCode} ")]));

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
      () => ResolveTargets(service, [new OrganizationPathResolver.OrganizationPathTarget(organizationCode, hospitalCode, branchCode)]));

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
    Assert.Equal(2, tenRowReads.Branches);
  }

  /// <summary>把一条目标路径交给解析器校验，供各用例共用同一入参构造。</summary>
  /// <param name="resolver">待验证的组织路径解析器。</param>
  /// <returns>校验通过时的三层业务编码与名称。</returns>
  private static Task<IReadOnlyList<OrganizationPathResolver.OrganizationPath>> Resolve(OrganizationPathResolver resolver) =>
    resolver.ResolveOrThrow(
      [new OrganizationPathResolver.OrganizationPathTarget(OrganizationCode, HospitalCode, BranchCode)],
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
    service.BranchesByHospital[HospitalCode] = [BuildBranch("BRH-1", BranchName, HospitalCode, OrganizationCode)];
    service.BranchesByHospital[OtherHospitalCode] = [BuildBranch("BRH-2", BranchName, OtherHospitalCode, OrganizationCode)];

    return service;
  }

  /// <summary>把一批目标路径交给解析器校验，供各用例共用同一入参构造。</summary>
  /// <param name="service">提供组织、医院与院区的替身。</param>
  /// <param name="targets">本批待校验的目标路径。</param>
  /// <returns>与目标路径等长且同序的已校验路径集合。</returns>
  private static Task<IReadOnlyList<OrganizationPathResolver.OrganizationPath>> ResolveTargets(FakeOrganizationAppService service, IReadOnlyList<OrganizationPathResolver.OrganizationPathTarget> targets) =>
    new OrganizationPathResolver(service).ResolveOrThrow(targets, MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage);

  /// <summary>在两条固定目标路径之间轮转形成指定行数，模拟同一批请求里包含大量重复路径的目标行。</summary>
  /// <param name="service">提供组织、医院与院区的替身。</param>
  /// <param name="rowCount">本次目标行数；只影响返回条数，不改变涉及的组织、医院与院区数量。</param>
  private static async Task ResolveBatch(FakeOrganizationAppService service, int rowCount)
  {
    OrganizationPathResolver.OrganizationPathTarget[] targets =
      [.. Enumerable.Range(1, rowCount).Select(index => index % 2 == 0
        ? new OrganizationPathResolver.OrganizationPathTarget(OrganizationCode, HospitalCode, "BRH-1")
        : new OrganizationPathResolver.OrganizationPathTarget(OrganizationCode, OtherHospitalCode, "BRH-2"))];

    IReadOnlyList<OrganizationPathResolver.OrganizationPath> paths = await ResolveTargets(service, targets);

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

    /// <inheritdoc/>
    public Task<OrganizationDto> GetOrganizationByIdAsync(GetOrganizationByIdRequest getOrganizationByIdRequest) =>
      throw new NotSupportedException("组织路径解析按组织全量读取，不按标识逐条读取。");

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllHospitalAsync() =>
      throw new NotSupportedException("组织路径解析只读取指定组织的启用医院。");

    /// <inheritdoc/>
    public Task<IEnumerable<HospitalDto>> QueryAllValidHospitalAsync() =>
      throw new NotSupportedException("组织路径解析只读取指定组织的启用医院。");

    /// <inheritdoc/>
    public Task<HospitalDto> GetHospitalByIdAsync(GetHospitalByIdRequest getHospitalByIdRequest) =>
      throw new NotSupportedException("组织路径解析按组织批量读取医院，不按标识逐条读取。");

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllBranchAsync() =>
      throw new NotSupportedException("组织路径解析只读取指定医院的启用院区。");

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchAsync() =>
      throw new NotSupportedException("组织路径解析只读取指定医院的启用院区。");

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDto>> QueryAllValidBranchByOrgIdAsync(QueryAllValidBranchByOrgIdRequest queryAllValidBranchByOrgIdRequest) =>
      throw new NotSupportedException("组织路径解析按医院批量读取院区，不按组织读取。");

    /// <inheritdoc/>
    public Task<BranchDto> GetBranchByIdAsync(GetBranchByIdRequest getBranchByIdRequest) =>
      throw new NotSupportedException("组织路径解析按医院批量读取院区，不按标识逐条读取。");

    /// <inheritdoc/>
    public Task<string> CreateOrganizationAsync(CreateOrganizationRequest createOrganizationRequest) => CountWrite<string>();

    /// <inheritdoc/>
    public Task<int> UpdateOrganizationAsync(UpdateOrganizationRequest updateOrganizationRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DeleteOrganizationAsync(DeleteOrganizationRequest deleteOrganizationRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableOrganizationAsync(EnableOrganizationRequest enableOrganizationRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableOrganizationAsync(DisableOrganizationRequest disableOrganizationRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<string> CreateHospitalAsync(CreateHospitalRequest createHospitalRequest) => CountWrite<string>();

    /// <inheritdoc/>
    public Task<int> UpdateHospitalAsync(UpdateHospitalRequest updateHospitalRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DeleteHospitalAsync(DeleteHospitalRequest deleteHospitalRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableHospitalAsync(EnableHospitalRequest enableHospitalRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableHospitalAsync(DisableHospitalRequest disableHospitalRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<string> CreateBranchAsync(CreateBranchRequest createBranchRequest) => CountWrite<string>();

    /// <inheritdoc/>
    public Task<int> UpdateBranchAsync(UpdateBranchRequest updateBranchRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DeleteBranchAsync(DeleteBranchRequest deleteBranchRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableBranchAsync(EnableBranchRequest enableBranchRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableBranchAsync(DisableBranchRequest disableBranchRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<Guid> CreateDeptAsync(CreateDeptRequest createDeptRequest) => CountWrite<Guid>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptAsync(UpdateDeptRequest updateDeptRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptAsync(DeleteDeptRequest deleteDeptRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptAsync(EnableDeptRequest enableDeptRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptAsync(DisableDeptRequest disableDeptRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptUpAsync(MoveDeptUpRequest moveDeptUpRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> MoveDeptDownAsync(MoveDeptDownRequest moveDeptDownRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptBySubApplicationIdAsync(QueryDeptBySubApplicationIdRequest queryDeptBySubApplicationIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByOrgIdAsync(QueryDeptByOrgIdRequest queryDeptByOrgIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByHosIdAsync(QueryDeptByHosIdRequest queryDeptByHosIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryDeptByBranchIdAsync(QueryDeptByBranchIdRequest queryDeptByBranchIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByBranchIdAndCodeAsync(GetDeptByBranchIdAndCodeRequest getDeptByBranchIdAndCodeRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<DeptDto> GetDeptByIdAsync(GetDeptByIdRequest getDeptByIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptDto>> QueryAllDeptAsync() =>
      throw new NotSupportedException("组织路径解析不读取平台科室。");

    /// <inheritdoc/>
    public Task<Guid> CreateDeptWardAsync(CreateDeptWardRequest createDeptWardRequest) => CountWrite<Guid>();

    /// <inheritdoc/>
    public Task<int> DeleteDeptWardAsync(DeleteDeptWardRequest deleteDeptWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> UpdateDeptWardAsync(UpdateDeptWardRequest updateDeptWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableDeptWardAsync(EnableDeptWardRequest enableDeptWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableDeptWardAsync(DisableDeptWardRequest disableDeptWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<DeptWardDto> GetDeptWardByIdAsync(GetDeptWardByIdRequest getDeptWardByIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取科室病区关系。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByDeptAsync(QueryDeptWardByDeptRequest queryDeptWardByDeptRequest) =>
      throw new NotSupportedException("组织路径解析不读取科室病区关系。");

    /// <inheritdoc/>
    public Task<IEnumerable<DeptWardDto>> QueryDeptWardByWardAsync(QueryDeptWardByWardRequest queryDeptWardByWardRequest) =>
      throw new NotSupportedException("组织路径解析不读取科室病区关系。");

    /// <inheritdoc/>
    public Task<Guid> CreateWardAsync(CreateWardRequest createWardRequest) => CountWrite<Guid>();

    /// <inheritdoc/>
    public Task<int> DeleteWardAsync(DeleteWardRequest deleteWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> UpdateWardAsync(UpdateWardRequest updateWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> EnableWardAsync(EnableWardRequest enableWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> DisableWardAsync(DisableWardRequest disableWardRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardUpAsync(MoveWardUpRequest moveWardUpRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<int> MoveWardDownAsync(MoveWardDownRequest moveWardDownRequest) => CountWrite<int>();

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardByBranchAsync(QueryWardByBranchRequest queryWardByBranchRequest) =>
      throw new NotSupportedException("组织路径解析不读取病区。");

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryWardBySubApplicationIdAsync(QueryWardBySubApplicationIdRequest queryWardBySubApplicationIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取病区。");

    /// <inheritdoc/>
    public Task<WardDto> GetWardByIdAsync(GetWardByIdRequest getWardByIdRequest) =>
      throw new NotSupportedException("组织路径解析不读取病区。");

    /// <inheritdoc/>
    public Task<IEnumerable<WardDto>> QueryAllWardAsync() =>
      throw new NotSupportedException("组织路径解析不读取病区。");

    /// <summary>
    /// 记录一次写方法调用并使用例失败：组织路径解析只读组织、医院与院区，不得触发任何写库动作。
    /// </summary>
    /// <typeparam name="TResult">写方法的返回类型。</typeparam>
    /// <returns>不会返回；调用即抛出异常。</returns>
    /// <exception cref="Xunit.Sdk.XunitException">写方法被调用时抛出，使对应用例立即失败。</exception>
    private Task<TResult> CountWrite<TResult>()
    {
      WriteCount++;
      throw new Xunit.Sdk.XunitException("组织路径解析不得调用外部组织服务的写方法。");
    }
  }
}
