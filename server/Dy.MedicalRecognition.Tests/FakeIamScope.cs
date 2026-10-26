using Dy.IAM.Sdk;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Dy.MedicalRecognition.Application.OrganizationScope;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 报告链路测试共用的 IAM 范围替身：同一实例同时提供共享目录读取、当前身份与组织业务编码读取，
/// 使按当前工作身份组织链解析范围的入口可以在不连真实 IAM 的情况下构造。
/// </summary>
/// <remarks>
/// 默认链为「医共体 → 医院 → 院区」三层，标识与业务编码固定，便于用例按同一组取值断言；
/// 业务编码不表达层级，默认链的编码只是各节点自己的取值，父子关系仍只由 <see cref="IamDirectoryOrganization.ParentOrganizationId"/> 表达。
/// 需要其他链、缺失层级或未配置业务编码的用例可自行构造 <see cref="IamDirectoryOrganization"/> 后写入 <see cref="Organizations"/>，
/// 并把该节点的编码写入 <see cref="Codes"/>（不写入表示该节点未配置业务编码）。
/// </remarks>
internal sealed class FakeIamScope : IIamDirectoryClient, ICurrentIamContext, IOrganizationBusinessCodeQueryClient
{
  /// <summary>默认链的医共体标识。</summary>
  internal static readonly Guid OrganizationId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

  /// <summary>默认链的医院标识。</summary>
  internal static readonly Guid HospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000002");

  /// <summary>默认链的院区标识。</summary>
  internal static readonly Guid BranchId = Guid.Parse("a1000000-0000-0000-0000-000000000003");

  /// <summary>默认操作人标识。</summary>
  internal static readonly Guid UserId = Guid.Parse("a1000000-0000-0000-0000-000000000004");

  /// <summary>默认工作身份标识。</summary>
  internal static readonly Guid WorkIdentityId = Guid.Parse("a1000000-0000-0000-0000-000000000005");

  /// <summary>默认租户标识。</summary>
  internal static readonly Guid TenantId = Guid.Parse("a1000000-0000-0000-0000-000000000006");

  /// <summary>另一个医共体的标识，用于平台入口跨组织场景。</summary>
  internal static readonly Guid OtherOrganizationId = Guid.Parse("a1000000-0000-0000-0000-000000000011");

  /// <summary>另一个医共体下医院的标识。</summary>
  internal static readonly Guid OtherHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000012");

  /// <summary>另一个医共体下医院的院区标识。</summary>
  internal static readonly Guid OtherBranchId = Guid.Parse("a1000000-0000-0000-0000-000000000013");

  /// <summary>同组织多医院夹具的第四家医院标识。</summary>
  internal static readonly Guid FourthHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000042");
  /// <summary>同组织多医院夹具的第五家医院标识。</summary>
  internal static readonly Guid FifthHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000043");
  /// <summary>同组织多医院夹具的第六家医院标识。</summary>
  internal static readonly Guid SixthHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000044");
  /// <summary>第四家医院下的固定院区标识。</summary>
  internal static readonly Guid FourthBranchId = Guid.Parse("a1000000-0000-0000-0000-000000000045");
  /// <summary>第五家医院下的固定院区标识。</summary>
  internal static readonly Guid FifthBranchId = Guid.Parse("a1000000-0000-0000-0000-000000000046");

  /// <summary>同一医共体下第二家医院的标识，供统计入口区分本院与同组织他院。</summary>
  internal static readonly Guid SecondHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000014");

  /// <summary>同一医共体下第二家医院所属院区的标识。</summary>
  internal static readonly Guid SecondBranchId = Guid.Parse("a1000000-0000-0000-0000-000000000015");

  /// <summary>默认链的医共体业务编码。</summary>
  internal const string OrganizationCode = "01";

  /// <summary>默认链的医院业务编码。</summary>
  internal const string HospitalCode = "CSYY1";

  /// <summary>默认链的院区业务编码。</summary>
  internal const string BranchCode = "CSYQ1-1";

  /// <summary>同一医共体下第二家医院的业务编码。</summary>
  internal const string SecondHospitalCode = "CSYY2";

  /// <summary>同一医共体下第二家医院所属院区的业务编码。</summary>
  internal const string SecondBranchCode = "CSYQ2-1";

  /// <summary>另一个医共体的业务编码。</summary>
  internal const string OtherOrganizationCode = "02";

  /// <summary>另一个医共体下医院的业务编码。</summary>
  internal const string OtherHospitalCode = "CSYY3";

  /// <summary>另一个医共体下医院的院区业务编码。</summary>
  internal const string OtherBranchCode = "CSYQ3-1";

  /// <summary>同组织多医院夹具的第四家医院业务编码。</summary>
  internal const string FourthHospitalCode = "CSYY4";

  /// <summary>同组织多医院夹具的第五家医院业务编码。</summary>
  internal const string FifthHospitalCode = "CSYY5";

  /// <summary>同组织多医院夹具的第六家医院业务编码。</summary>
  internal const string SixthHospitalCode = "CSYY6";

  /// <summary>第四家医院下的固定院区业务编码。</summary>
  internal const string FourthBranchCode = "CSYQ4-1";

  /// <summary>第五家医院下的固定院区业务编码。</summary>
  internal const string FifthBranchCode = "CSYQ5-1";

  /// <summary>默认链的组织详情，按标识索引。</summary>
  internal Dictionary<Guid, IamDirectoryOrganization> Organizations { get; } = BuildDefaultChain();

  /// <summary>组织业务编码，按组织实例标识索引；未登记的组织按未配置业务编码处理。</summary>
  internal Dictionary<Guid, string> Codes { get; } = BuildDefaultCodes();

  /// <summary>默认替身：三层齐全、身份已选工作身份。</summary>
  internal static FakeIamScope Default { get; } = new();

  /// <summary>目录批量读取次数：供局部性判据断言读取次数不随条目数增长。</summary>
  internal int BatchReadCount { get; private set; }

  /// <summary>组织业务编码整树读取次数：供局部性判据断言编码读取次数不随条目数增长。</summary>
  internal int BusinessCodeQueryCount { get; private set; }

  /// <inheritdoc/>
  public Task<IamDirectoryReadResult> BatchReadDirectoryAsync(IamDirectoryReadRequest request, CancellationToken cancellationToken)
  {
    BatchReadCount++;
    IamDirectoryReadItem[] items =
    [
      .. request.Ids.Select(id => Organizations.TryGetValue(id, out IamDirectoryOrganization? organization)
        ? new IamDirectoryReadItem(id, true, new IamDirectoryData { Organization = organization })
        : new IamDirectoryReadItem(id, false, null!))
    ];
    return Task.FromResult(new IamDirectoryReadResult(items, DateTimeOffset.UtcNow));
  }

  /// <inheritdoc/>
  public Task<IamEmploymentValidationResult> BatchValidateEmploymentsAsync(IamEmploymentValidationRequest request, CancellationToken cancellationToken) =>
    throw new NotSupportedException();

  /// <inheritdoc/>
  public Task<IamOrganizationDirectoryResult> QueryOrganizationDirectoryAsync(IamOrganizationDirectoryRequest request, CancellationToken cancellationToken) =>
    throw new NotSupportedException();

  /// <inheritdoc/>
  public Task<OrganizationBusinessCodeTreeResponse> QueryOrganizationByTenantIdAsync(OrganizationBusinessCodeQueryRequest request)
  {
    BusinessCodeQueryCount++;
    return Task.FromResult(new OrganizationBusinessCodeTreeResponse
    {
      TenantId = request.TenantId,
      TenantName = "演示租户",
      TenantIsEnabled = true,
      Nodes = [.. Codes.Select(pair =>
      {
        Organizations.TryGetValue(pair.Key, out IamDirectoryOrganization? organization);
        return new OrganizationBusinessCodeTreeNode
        {
          Id = pair.Key,
          ParentOrganizationId = organization?.ParentOrganizationId,
          Name = organization?.Name,
          IsEnabled = organization?.IsEnabled ?? false,
          Code = pair.Value
        };
      })]
    });
  }

  /// <inheritdoc/>
  public bool IsAuthenticated => true;

  /// <inheritdoc/>
  public IamIdentityContext? Current => Identity;

  /// <inheritdoc/>
  public IamIdentityContext RequireCurrent() => Identity;

  /// <summary>当前身份；默认按默认链的院区工作身份构造。</summary>
  internal IamIdentityContext Identity { get; init; } = new(
    UserId,
    WorkIdentityId,
    TenantId,
    BranchId,
    null,
    "medical-recognition",
    Guid.Parse("a1000000-0000-0000-0000-000000000007"),
    1,
    1,
    1);

  /// <summary>
  /// 构造附带另一家独立医院链的替身：默认链不变，另加一个医共体、医院与院区，用于平台入口跨组织校验。
  /// </summary>
  /// <returns>带两条完整链的替身。</returns>
  internal static FakeIamScope WithOtherHospital()
  {
    FakeIamScope scope = new();
    scope.AddNode(OtherOrganizationId, "其他医共体", null, "CountyMedicalCommunity", [OtherOrganizationId], OtherOrganizationCode);
    scope.AddNode(OtherHospitalId, "其他医院", OtherOrganizationId, "Hospital", [OtherOrganizationId, OtherHospitalId], OtherHospitalCode);
    scope.AddNode(OtherBranchId, "其他院区", OtherHospitalId, "Branch", [OtherOrganizationId, OtherHospitalId, OtherBranchId], OtherBranchCode);
    return scope;
  }

  /// <summary>
  /// 构造可信组织下附带三家额外医院的替身，供同一组织多家医院的分组排序与并列键用例使用。
  /// </summary>
  /// <returns>默认链不变、可信组织下共五家医院的替身。</returns>
  internal static FakeIamScope WithManyHospitals()
  {
    FakeIamScope scope = new();
    scope.AddNode(FourthHospitalId, "测试医院四", OrganizationId, "Hospital", [OrganizationId, FourthHospitalId], FourthHospitalCode);
    scope.AddNode(FifthHospitalId, "测试医院五", OrganizationId, "Hospital", [OrganizationId, FifthHospitalId], FifthHospitalCode);
    scope.AddNode(SixthHospitalId, "测试医院六", OrganizationId, "Hospital", [OrganizationId, SixthHospitalId], SixthHospitalCode);
    scope.AddNode(FourthBranchId, "测试院区五", FourthHospitalId, "Branch", [OrganizationId, FourthHospitalId, FourthBranchId], FourthBranchCode);
    scope.AddNode(FifthBranchId, "测试院区六", FifthHospitalId, "Branch", [OrganizationId, FifthHospitalId, FifthBranchId], FifthBranchCode);
    return scope;
  }

  /// <summary>与外来医共体下医院共用业务编码的节点标识。</summary>
  internal static readonly Guid DuplicateOtherHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000021");

  /// <summary>与默认链医院共用业务编码的节点标识。</summary>
  internal static readonly Guid DuplicateTrustedHospitalId = Guid.Parse("a1000000-0000-0000-0000-000000000022");

  /// <summary>与外来医共体共用业务编码的节点标识。</summary>
  internal static readonly Guid DuplicateOtherOrganizationId = Guid.Parse("a1000000-0000-0000-0000-000000000023");

  /// <summary>
  /// 构造附加一个与外来医院共用业务编码的医院节点的替身：同一编码命中两个节点，且两个候选都不在当前身份的组织链上。
  /// </summary>
  /// <param name="name">附加节点的名称；与原名一致时考察「名称一致即取该名称」，不一致时考察「宁可不填」。</param>
  /// <returns>该编码命中两个节点的替身。</returns>
  internal static FakeIamScope WithDuplicateOtherHospitalCode(string name) =>
    new FakeIamScope().WithIsolatedNode(DuplicateOtherHospitalId, OtherHospitalCode, name, "Hospital", OtherOrganizationId);

  /// <summary>
  /// 构造附加一个与默认链医院共用业务编码的医院节点的替身：其中一个候选落在当前身份的组织链上。
  /// </summary>
  /// <param name="name">附加节点的名称；与会话链上节点名称不同，使「取链上节点的名称」可与「名称一致」区分开。</param>
  /// <returns>该编码命中两个节点、且一个候选在会话组织链上的替身。</returns>
  internal static FakeIamScope WithDuplicateTrustedHospitalCode(string name) =>
    new FakeIamScope().WithIsolatedNode(DuplicateTrustedHospitalId, HospitalCode, name, "Hospital", OtherOrganizationId);

  /// <summary>
  /// 构造附加一个与外来医共体共用业务编码的医共体节点的替身：同一编码命中两个根节点，两个候选都不在当前身份的组织链上。
  /// </summary>
  /// <returns>该医共体编码命中两个节点的替身。</returns>
  internal static FakeIamScope WithDuplicateOtherOrganizationCode() =>
    new FakeIamScope().WithIsolatedNode(
      DuplicateOtherOrganizationId, OtherOrganizationCode, "其他医共体二", "CountyMedicalCommunity", null);

  /// <summary>
  /// 附加一个只挂在指定父节点下的独立节点：其标识不在当前身份的组织链上，因此会话组织链收敛不到它。
  /// </summary>
  /// <param name="id">节点标识。</param>
  /// <param name="code">节点业务编码。</param>
  /// <param name="name">节点名称。</param>
  /// <param name="typeCode">组织类型编码。</param>
  /// <param name="parentOrganizationId">父组织标识；为 <see langword="null"/> 时该节点是根节点。</param>
  /// <returns>同一替身，供链式构造。</returns>
  private FakeIamScope WithIsolatedNode(Guid id, string code, string name, string typeCode, Guid? parentOrganizationId)
  {
    AddNode(id, name, parentOrganizationId, typeCode, parentOrganizationId is { } parent ? [parent, id] : [id], code);
    return this;
  }

  /// <summary>
  /// 构造当前身份组织在目录中不存在的替身：身份组织标识指向的节点未登记，读取结果为未找到。
  /// </summary>
  /// <returns>目录中缺少当前组织的替身。</returns>
  internal static FakeIamScope MissingOrganization()
  {
    FakeIamScope scope = new();
    scope.Organizations.Clear();
    scope.Codes.Clear();
    scope.AddNode(OtherOrganizationId, "其他医共体", null, "CountyMedicalCommunity", [OtherOrganizationId], OtherOrganizationCode);
    return scope;
  }

  /// <summary>
  /// 构造当前身份组织在目录中已停用的替身：节点存在但未启用，不参与层级归类。
  /// </summary>
  /// <returns>当前组织已停用的替身。</returns>
  internal static FakeIamScope DisabledOrganization()
  {
    FakeIamScope scope = new();
    scope.Organizations.Clear();
    scope.Codes.Clear();
    scope.AddNode(OrganizationId, "示范医共体", null, "CountyMedicalCommunity", [OrganizationId], OrganizationCode, isEnabled: false);
    return scope;
  }

  /// <summary>
  /// 构造只有医共体、没有医院与院区的替身：当前身份落在该医共体上，用于缺失层级的拒绝用例。
  /// </summary>
  /// <returns>只含医共体节点的替身。</returns>
  internal static FakeIamScope WithoutHospital()
  {
    FakeIamScope scope = new() { Identity = new(OrganizationId, WorkIdentityId, TenantId, OrganizationId, null, "medical-recognition", Guid.NewGuid(), 1, 1, 1) };
    scope.Organizations.Clear();
    scope.Codes.Clear();
    scope.AddNode(OrganizationId, "示范医共体", null, "CountyMedicalCommunity", [OrganizationId], OrganizationCode);
    return scope;
  }

  /// <summary>
  /// 构造默认三层链：医共体、医院、院区各一个节点，类型编码分别为医共体、医院与院区。
  /// </summary>
  /// <returns>按标识索引的组织详情。</returns>
  private static Dictionary<Guid, IamDirectoryOrganization> BuildDefaultChain() => new()
  {
    [OrganizationId] = Organization(OrganizationId, "示范医共体", null, "CountyMedicalCommunity", [OrganizationId]),
    [HospitalId] = Organization(HospitalId, "示范医院", OrganizationId, "Hospital", [OrganizationId, HospitalId]),
    [BranchId] = Organization(BranchId, "示范院区", HospitalId, "Branch", [OrganizationId, HospitalId, BranchId]),
    // 同一医共体下的第二家医院与院区，供统计入口区分「本院」与「同组织他院」。
    [SecondHospitalId] = Organization(SecondHospitalId, "示范医院二", OrganizationId, "Hospital", [OrganizationId, SecondHospitalId]),
    [SecondBranchId] = Organization(SecondBranchId, "示范院区二", SecondHospitalId, "Branch", [OrganizationId, SecondHospitalId, SecondBranchId]),
    // 另一条独立医共体链，供来源侧跨组织的统计用例。
    [OtherOrganizationId] = Organization(OtherOrganizationId, "其他医共体", null, "CountyMedicalCommunity", [OtherOrganizationId]),
    [OtherHospitalId] = Organization(OtherHospitalId, "其他医院", OtherOrganizationId, "Hospital", [OtherOrganizationId, OtherHospitalId]),
    [OtherBranchId] = Organization(OtherBranchId, "其他院区", OtherHospitalId, "Branch", [OtherOrganizationId, OtherHospitalId, OtherBranchId]),
  };

  /// <summary>
  /// 构造默认链各节点的业务编码：编码不表达层级，各节点只登记自己的取值。
  /// </summary>
  /// <returns>按组织实例标识索引的业务编码。</returns>
  private static Dictionary<Guid, string> BuildDefaultCodes() => new()
  {
    [OrganizationId] = OrganizationCode,
    [HospitalId] = HospitalCode,
    [BranchId] = BranchCode,
    [SecondHospitalId] = SecondHospitalCode,
    [SecondBranchId] = SecondBranchCode,
    [OtherOrganizationId] = OtherOrganizationCode,
    [OtherHospitalId] = OtherHospitalCode,
    [OtherBranchId] = OtherBranchCode,
  };

  /// <summary>
  /// 同时登记组织节点与其业务编码：目录读取与编码读取两处保持一致，避免出现有节点无编码的夹具。
  /// </summary>
  /// <param name="id">组织标识。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="parentId">父组织标识。</param>
  /// <param name="typeCode">组织类型编码。</param>
  /// <param name="path">从根到自身的组织路径标识序列。</param>
  /// <param name="code">组织业务编码；为 <see langword="null"/> 表示该节点未配置业务编码。</param>
  /// <param name="isEnabled">组织是否启用。</param>
  private void AddNode(Guid id, string name, Guid? parentId, string typeCode, Guid[] path, string? code, bool isEnabled = true)
  {
    Organizations[id] = Organization(id, name, parentId, typeCode, path, isEnabled);
    if (code is null)
    {
      Codes.Remove(id);
      return;
    }

    Codes[id] = code;
  }

  /// <summary>
  /// 构造一个组织详情节点。
  /// </summary>
  /// <param name="id">组织标识。</param>
  /// <param name="name">组织名称。</param>
  /// <param name="parentId">父组织标识。</param>
  /// <param name="typeCode">组织类型编码。</param>
  /// <param name="path">从根到自身的组织路径标识序列，供祖先链解析读取。</param>
  /// <returns>组织详情。</returns>
  private static IamDirectoryOrganization Organization(Guid id, string name, Guid? parentId, string typeCode, Guid[] path, bool isEnabled = true) =>
    new(id, name, parentId, isEnabled, [.. path.Select(item => new IamDirectoryPathItem(item, $"组织-{item:N}"))])
    {
      OrganizationType = new IamDirectoryOrganizationType(Guid.NewGuid(), typeCode, typeCode, true)
    };
}
