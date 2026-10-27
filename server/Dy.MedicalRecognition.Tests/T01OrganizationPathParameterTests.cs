using Dy.MedicalRecognition.Application.OrganizationScope;
using Dy.MedicalRecognition.Application.Validation;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 组织路径构造参数的字段对应关系与解析模式回归（本阶段后端矩阵 S01“三层字段映射”、S02“缺失层与解析模式”）。
/// </summary>
/// <remarks>
/// <para>
/// 三层使用互不相同的标识、名称与编码：任一字段被写到相邻层的位置，断言都会失败，字段混用因此可被识别。
/// 提交编码解析的两处构造按设计不为数据用途挑节点，三层实例标识一律为空 Guid，
/// 该事实见 <see cref="OrganizationPath"/> 类型说明与 <see cref="OrganizationPathResolver"/> 类型说明，
/// 因此该两处的字段对应由名称与编码承担。
/// </para>
/// <para>
/// 外部组织数据由 <see cref="FakeIamScope"/> 手写替身提供，不发起真实 HTTP 调用，也不写库；
/// 解析点只消费 <see cref="IamOrganizationDirectoryReader"/> 与 <see cref="IamOrganizationBusinessCodeReader"/>，
/// 这两个读取点只有读取成员，取名模式因此没有可以新增的写路径。
/// </para>
/// </remarks>
public sealed class T01OrganizationPathParameterTests
{
  /// <summary>医共体实例标识，用于 FromScope 的字段对应识别。</summary>
  private static readonly Guid OrganizationInstanceId = Guid.Parse("b1000000-0000-0000-0000-000000000001");

  /// <summary>医院实例标识，与医共体、院区的标识互不相同。</summary>
  private static readonly Guid HospitalInstanceId = Guid.Parse("b1000000-0000-0000-0000-000000000002");

  /// <summary>院区实例标识，与医共体、医院的标识互不相同。</summary>
  private static readonly Guid BranchInstanceId = Guid.Parse("b1000000-0000-0000-0000-000000000003");

  /// <summary>医共体名称，与其余两层的名称互不相同。</summary>
  private const string OrganizationLayerName = "字段对应医共体";

  /// <summary>医院名称，与其余两层的名称互不相同。</summary>
  private const string HospitalLayerName = "字段对应医院";

  /// <summary>院区名称，与其余两层的名称互不相同。</summary>
  private const string BranchLayerName = "字段对应院区";

  /// <summary>医共体业务编码，与其余两层的编码互不相同。</summary>
  private const string OrganizationLayerCode = "CODE-ORG-LAYER";

  /// <summary>医院业务编码，与其余两层的编码互不相同。</summary>
  private const string HospitalLayerCode = "CODE-HOS-LAYER";

  /// <summary>院区业务编码，与其余两层的编码互不相同。</summary>
  private const string BranchLayerCode = "CODE-BRH-LAYER";

  /// <summary>组织层不成立时的业务拒绝文案。</summary>
  private const string MissingOrganizationMessage = "组织不可用。";

  /// <summary>医院层不成立时的业务拒绝文案。</summary>
  private const string MissingHospitalMessage = "医院不可用。";

  /// <summary>院区层不成立时的业务拒绝文案。</summary>
  private const string MissingBranchMessage = "院区不可用。";

  /// <summary>
  /// S01 的 IAM 范围装配面：三层各不相同的实例标识、名称与编码分别进入本层的三个字段。
  /// </summary>
  [Fact]
  public void From_scope_maps_each_layer_identifier_name_and_code_to_its_own_field()
  {
    OrganizationPath path = OrganizationPath.FromScope(new IamOrganizationScope(
      OrganizationInstanceId, OrganizationLayerName, OrganizationLayerCode,
      HospitalInstanceId, HospitalLayerName, HospitalLayerCode,
      BranchInstanceId, BranchLayerName, BranchLayerCode));

    Assert.Equal(OrganizationInstanceId, path.OrganizationId);
    Assert.Equal(OrganizationLayerName, path.OrganizationName);
    Assert.Equal(OrganizationLayerCode, path.OrganizationCode);
    Assert.Equal(HospitalInstanceId, path.HospitalId);
    Assert.Equal(HospitalLayerName, path.HospitalName);
    Assert.Equal(HospitalLayerCode, path.HospitalCode);
    Assert.Equal(BranchInstanceId, path.BranchId);
    Assert.Equal(BranchLayerName, path.BranchName);
    Assert.Equal(BranchLayerCode, path.BranchCode);
  }

  /// <summary>
  /// S01 的提交编码校验面：三层各不相同的目录名称与提交编码分别进入本层的名称与编码字段，实例标识一律为空。
  /// </summary>
  [Fact]
  public async Task Resolve_throw_entry_maps_each_layer_name_and_code_to_its_own_field()
  {
    OrganizationPath path = Assert.Single(await CreateIamResolver(FakeIamScope.Default).ResolveOrThrow(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)],
      MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));

    Assert.Equal(Guid.Empty, path.OrganizationId);
    Assert.Equal("示范医共体", path.OrganizationName);
    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    Assert.Equal(Guid.Empty, path.HospitalId);
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    Assert.Equal(Guid.Empty, path.BranchId);
    Assert.Equal("示范院区", path.BranchName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
  }

  /// <summary>
  /// S01 的取名模式面：三层各不相同的名称与编码分别进入本层的名称与编码字段，实例标识一律为空。
  /// </summary>
  [Fact]
  public async Task Resolve_names_or_empty_maps_each_layer_name_and_code_to_its_own_field()
  {
    OrganizationPath path = Assert.Single(await CreateIamResolver(FakeIamScope.Default).ResolveNamesOrEmptyAsync(
      [new OrganizationPathTarget(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode)]));

    Assert.Equal(Guid.Empty, path.OrganizationId);
    Assert.Equal("示范医共体", path.OrganizationName);
    Assert.Equal(FakeIamScope.OrganizationCode, path.OrganizationCode);
    Assert.Equal(Guid.Empty, path.HospitalId);
    Assert.Equal("示范医院", path.HospitalName);
    Assert.Equal(FakeIamScope.HospitalCode, path.HospitalCode);
    Assert.Equal(Guid.Empty, path.BranchId);
    Assert.Equal("示范院区", path.BranchName);
    Assert.Equal(FakeIamScope.BranchCode, path.BranchCode);
  }

  /// <summary>
  /// S02 的缺失值面：IAM 范围未解析出的层级，其实例标识为 <see cref="Guid.Empty"/>，名称与编码为空串。
  /// </summary>
  /// <remarks>
  /// 三类缺失分开考察：三层全缺、只有医共体、缺院区，缺失的层一律按空值承载，不由相邻层补齐。
  /// </remarks>
  [Fact]
  public void From_scope_maps_missing_layers_to_empty_identifier_name_and_code()
  {
    OrganizationPath empty = OrganizationPath.FromScope(IamOrganizationScope.Empty);
    AssertPath(empty, Guid.Empty, string.Empty, string.Empty, Guid.Empty, string.Empty, string.Empty, Guid.Empty, string.Empty, string.Empty);

    OrganizationPath organizationOnly = OrganizationPath.FromScope(
      new IamOrganizationScope(OrganizationInstanceId, OrganizationLayerName, OrganizationLayerCode, null, null, null, null, null, null));
    AssertPath(
      organizationOnly,
      OrganizationInstanceId, OrganizationLayerName, OrganizationLayerCode,
      Guid.Empty, string.Empty, string.Empty,
      Guid.Empty, string.Empty, string.Empty);

    OrganizationPath withoutBranch = OrganizationPath.FromScope(
      new IamOrganizationScope(
        OrganizationInstanceId, OrganizationLayerName, OrganizationLayerCode,
        HospitalInstanceId, HospitalLayerName, HospitalLayerCode,
        null, null, null));
    AssertPath(
      withoutBranch,
      OrganizationInstanceId, OrganizationLayerName, OrganizationLayerCode,
      HospitalInstanceId, HospitalLayerName, HospitalLayerCode,
      Guid.Empty, string.Empty, string.Empty);
  }

  /// <summary>
  /// S02 的解析模式面：组织单层、组织医院两层、完整三层三个入口只填写参与解析的层，其余层为空值。
  /// </summary>
  [Fact]
  public async Task Single_two_and_three_layer_entries_fill_only_the_participating_layers()
  {
    OrganizationPathResolver resolver = CreateIamResolver(FakeIamScope.Default);
    OrganizationPathTarget target = new(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode);

    OrganizationPath organizationOnly = Assert.Single(await resolver.ResolveOrganizationOrThrow([target], MissingOrganizationMessage));
    AssertPath(
      organizationOnly,
      Guid.Empty, "示范医共体", FakeIamScope.OrganizationCode,
      Guid.Empty, string.Empty, string.Empty,
      Guid.Empty, string.Empty, string.Empty);

    OrganizationPath organizationHospital = Assert.Single(
      await resolver.ResolveOrganizationHospitalOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage));
    AssertPath(
      organizationHospital,
      Guid.Empty, "示范医共体", FakeIamScope.OrganizationCode,
      Guid.Empty, "示范医院", FakeIamScope.HospitalCode,
      Guid.Empty, string.Empty, string.Empty);

    OrganizationPath wholePath = Assert.Single(
      await resolver.ResolveOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));
    AssertPath(
      wholePath,
      Guid.Empty, "示范医共体", FakeIamScope.OrganizationCode,
      Guid.Empty, "示范医院", FakeIamScope.HospitalCode,
      Guid.Empty, "示范院区", FakeIamScope.BranchCode);
  }

  /// <summary>
  /// S02 的取名模式面：取名入口与校验入口对同一路径给出同一条记录，三层实例标识保持为空。
  /// </summary>
  [Fact]
  public async Task Name_entries_return_the_same_path_as_the_validating_entries()
  {
    OrganizationPathResolver resolver = CreateIamResolver(FakeIamScope.Default);
    OrganizationPathTarget target = new(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode);

    OrganizationPath validatedWholePath = Assert.Single(
      await resolver.ResolveOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));
    OrganizationPath namedWholePath = Assert.Single(
      await resolver.ResolveNamesOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage));
    Assert.Equal(validatedWholePath, namedWholePath);

    OrganizationPath validatedTwoLayers = Assert.Single(
      await resolver.ResolveOrganizationHospitalOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage));
    OrganizationPath namedTwoLayers = Assert.Single(
      await resolver.ResolveOrganizationHospitalNamesOrThrow([target], MissingOrganizationMessage, MissingHospitalMessage));
    Assert.Equal(validatedTwoLayers, namedTwoLayers);
  }

  /// <summary>
  /// S02 的读取次数面：取名模式整批目标只读一次组织编码树与一次目录，不随取名增加其他读取。
  /// </summary>
  [Fact]
  public async Task Name_entry_reads_the_business_code_tree_and_the_directory_once()
  {
    FakeIamScope scope = new();
    OrganizationPathTarget[] targets =
    [
      new(FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode),
      new(FakeIamScope.OrganizationCode, FakeIamScope.SecondHospitalCode, FakeIamScope.SecondBranchCode)
    ];

    IReadOnlyList<OrganizationPath> paths = await CreateIamResolver(scope).ResolveNamesOrThrow(
      targets, MissingOrganizationMessage, MissingHospitalMessage, MissingBranchMessage);

    Assert.Equal(2, paths.Count);
    Assert.Equal(1, scope.BusinessCodeQueryCount);
    Assert.Equal(1, scope.BatchReadCount);
  }

  /// <summary>用同一个 IAM 替身同时充当目录读取、业务编码读取与当前上下文，构造组织路径解析点。</summary>
  /// <param name="scope">IAM 范围替身，同时提供目录、业务编码与当前身份。</param>
  /// <returns>解析点。</returns>
  private static OrganizationPathResolver CreateIamResolver(FakeIamScope scope) =>
    new(new IamOrganizationDirectoryReader(scope), new IamOrganizationBusinessCodeReader(scope), scope);

  /// <summary>逐字段断言一条组织路径的九个字段。</summary>
  /// <param name="path">待断言的组织路径。</param>
  /// <param name="organizationId">医共体实例标识期望值。</param>
  /// <param name="organizationName">医共体名称期望值。</param>
  /// <param name="organizationCode">医共体业务编码期望值。</param>
  /// <param name="hospitalId">医院实例标识期望值。</param>
  /// <param name="hospitalName">医院名称期望值。</param>
  /// <param name="hospitalCode">医院业务编码期望值。</param>
  /// <param name="branchId">院区实例标识期望值。</param>
  /// <param name="branchName">院区名称期望值。</param>
  /// <param name="branchCode">院区业务编码期望值。</param>
  private static void AssertPath(
    OrganizationPath path,
    Guid organizationId,
    string organizationName,
    string organizationCode,
    Guid hospitalId,
    string hospitalName,
    string hospitalCode,
    Guid branchId,
    string branchName,
    string branchCode)
  {
    Assert.Equal(organizationId, path.OrganizationId);
    Assert.Equal(organizationName, path.OrganizationName);
    Assert.Equal(organizationCode, path.OrganizationCode);
    Assert.Equal(hospitalId, path.HospitalId);
    Assert.Equal(hospitalName, path.HospitalName);
    Assert.Equal(hospitalCode, path.HospitalCode);
    Assert.Equal(branchId, path.BranchId);
    Assert.Equal(branchName, path.BranchName);
    Assert.Equal(branchCode, path.BranchCode);
  }
}
