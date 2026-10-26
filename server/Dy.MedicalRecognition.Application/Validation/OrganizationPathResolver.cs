using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application.OrganizationScope;

namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 组织路径解析点：按业务编码批量读取 IAM 目录，校验三层编码的存在、启用与类型，
/// 并在每层都能唯一确定时校验父子归属，返回携带提交编码与展示名称的组织路径。
/// </summary>
/// <remarks>
/// <para>
/// 编码是业务键，组织节点标识不是业务键。同一业务编码在 IAM 目录中对应多个组织节点时，提交值本身没有歧义：
/// 业务表与查询过滤用的是编码文本，查询链路除会话来源外不再消费组织实例标识。
/// 因此本解析点只为校验服务，不为数据用途挑节点：返回路径的实例标识一律为空，避免被误当成已定论。
/// </para>
/// <para>
/// 校验只判定三件事：该层编码是否存在启用节点、是否存在符合该层组织类型的节点、以及能否定论的父子归属。
/// 父子归属按边校验：一条父子边只在它两端的编码都恰好命中一个候选时执行；参与该边的任一层命中多个候选时跳过这条边。
/// 编码命中多个节点时编码本身无法说明提交方指向哪一个，父子关系也无从定论，
/// 而父子校验本身是为写入归属服务的，不唯一时不应据此拒绝。
/// </para>
/// <para>
/// 编码不表达层级：父子归属只取 IAM 目录的父组织标识，不按编码前缀或形态推导；节点未配置编码时该层按空值处理，由各层文案拒绝。
/// </para>
/// <para>
/// 名称是展示字段，按容错口径取值：会话组织链上唯一候选取该候选的名称，否则全部候选名称一致时取该名称，都不成立时留空。
/// </para>
/// <para>整批目标共用一次编码整树读取与一次目录批量读取，读取次数只随租户与候选节点数量增长，不随目标条数增长。</para>
/// <para>解析只读 IAM 目录，不写库、不发事件、不做角色授权判断。</para>
/// </remarks>
internal sealed class OrganizationPathResolver
{
  /// <summary>IAM 目录批量读取点：提供候选节点的当前组织类型与启停状态。</summary>
  private readonly IamOrganizationDirectoryReader iamDirectoryReader;

  /// <summary>组织业务编码读取点：按租户一次读取组织树，并按业务编码归集候选节点。</summary>
  private readonly IamOrganizationBusinessCodeReader iamBusinessCodeReader;

  /// <summary>已验证的 IAM 当前上下文，提供组织树所属租户与当前工作身份所在组织。</summary>
  private readonly ICurrentIamContext currentIamContext;

  /// <summary>
  /// 以 IAM 目录与组织业务编码为唯一数据来源初始化组织路径解析点。
  /// </summary>
  /// <param name="iamDirectoryReader">IAM 目录批量读取点。</param>
  /// <param name="iamBusinessCodeReader">组织业务编码读取点。</param>
  /// <param name="currentIamContext">已验证的 IAM 当前上下文。</param>
  public OrganizationPathResolver(
    IamOrganizationDirectoryReader iamDirectoryReader,
    IamOrganizationBusinessCodeReader iamBusinessCodeReader,
    ICurrentIamContext currentIamContext)
  {
    this.iamDirectoryReader = iamDirectoryReader;
    this.iamBusinessCodeReader = iamBusinessCodeReader;
    this.currentIamContext = currentIamContext;
  }

  /// <summary>解析一批三层目标路径，校验组织、医院、院区三层编码的存在、启用、类型与能定论的父子归属。</summary>
  /// <param name="targets">待校验目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区层不成立时的业务拒绝文案。</param>
  /// <returns>与输入等长且同序的已校验路径集合。</returns>
  /// <exception cref="InvalidOperationException">任一条目标的任一层校验不成立时抛出。</exception>
  public Task<IReadOnlyList<OrganizationPath>> ResolveOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage) =>
    ValidateAsync(targets, missingOrganizationMessage, missingHospitalMessage, missingBranchMessage);

  /// <summary>解析一批组织与医院两层目标路径，院区层留空。</summary>
  /// <remarks>整批目标经一次编码整树读取与一次目录批量读取完成，读取次数不随目标条数增长。</remarks>
  /// <param name="targets">待校验目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案。</param>
  /// <returns>与输入等长且同序的已校验路径集合，院区为空。</returns>
  /// <exception cref="InvalidOperationException">任一条目标的任一层校验不成立时抛出。</exception>
  public Task<IReadOnlyList<OrganizationPath>> ResolveOrganizationHospitalOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage) =>
    ValidateAsync(targets, missingOrganizationMessage, missingHospitalMessage, null);

  /// <summary>解析一批只到组织一层的目标路径，医院与院区层留空。</summary>
  /// <param name="targets">待校验目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <returns>与输入等长且同序的已校验路径集合，医院与院区为空。</returns>
  /// <exception cref="InvalidOperationException">任一条目标的组织层校验不成立时抛出。</exception>
  public Task<IReadOnlyList<OrganizationPath>> ResolveOrganizationOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage) =>
    ValidateAsync(targets, missingOrganizationMessage, null, null);

  /// <summary>为名称回填解析一批三层目标路径的展示名称。</summary>
  /// <remarks>
  /// 取名口径与校验入口是同一套：编码命中多个节点时按容错规则取名称，实例标识一律留空。
  /// 本入口与 <see cref="ResolveOrThrow"/> 行为一致，保留它是为了按页取名的调用点能各自表达意图。
  /// </remarks>
  /// <param name="targets">待取名的目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区层不成立时的业务拒绝文案。</param>
  /// <returns>与输入等长且同序的路径集合。</returns>
  /// <exception cref="InvalidOperationException">任一条目标的任一层校验不成立时抛出。</exception>
  public Task<IReadOnlyList<OrganizationPath>> ResolveNamesOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage) =>
    ValidateAsync(targets, missingOrganizationMessage, missingHospitalMessage, missingBranchMessage);

  /// <summary>为名称回填解析一批组织与医院两层目标路径的展示名称，院区层留空。</summary>
  /// <remarks>取名与校验口径与 <see cref="ResolveNamesOrThrow"/> 相同；院区层不参与本入口，返回时为空。</remarks>
  /// <param name="targets">待取名的目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案。</param>
  /// <returns>与输入等长且同序的路径集合，院区为空。</returns>
  /// <exception cref="InvalidOperationException">任一条目标的任一层校验不成立时抛出。</exception>
  public Task<IReadOnlyList<OrganizationPath>> ResolveOrganizationHospitalNamesOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage) =>
    ValidateAsync(targets, missingOrganizationMessage, missingHospitalMessage, null);

  /// <summary>
  /// 校验一批目标路径并装配返回路径；提供医院文案时校验医院层，提供院区文案时校验院区层。
  /// </summary>
  /// <remarks>
  /// 整批目标共用一次编码整树读取与一次目录批量读取；候选节点的类型与启停状态以 IAM 目录为准。
  /// 校验只判定存在性、类型与能定论的父子归属，不为数据用途挑选候选节点。
  /// </remarks>
  /// <param name="targets">待校验目标路径，至少一条。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案；为 <see langword="null"/> 时不校验医院层。</param>
  /// <param name="missingBranchMessage">院区层不成立时的业务拒绝文案；为 <see langword="null"/> 时不校验院区层。</param>
  /// <returns>与输入等长且同序的已校验路径集合。</returns>
  /// <exception cref="ArgumentNullException">目标集合为 null 时抛出。</exception>
  /// <exception cref="ArgumentException">目标集合为空时抛出。</exception>
  /// <exception cref="InvalidOperationException">任一条目标的任一层校验不成立时抛出。</exception>
  private async Task<IReadOnlyList<OrganizationPath>> ValidateAsync(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string? missingHospitalMessage,
    string? missingBranchMessage)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条待校验的组织路径。", nameof(targets));

    CandidateNodes candidates = await ReadCandidatesAsync(CollectCodes(targets));
    List<OrganizationPath> paths = [];
    foreach (OrganizationPathTarget target in targets)
    {
      paths.Add(ValidateTarget(target, candidates, missingOrganizationMessage, missingHospitalMessage, missingBranchMessage));
    }

    return paths;
  }

  /// <summary>
  /// 校验一条目标路径并装配返回路径：编码取提交值，名称取容错取名结果，实例标识留空。
  /// </summary>
  /// <remarks>
  /// 父子归属按边校验，且每条边只在两端编码都唯一命中时执行：编码命中多个节点时无法确定提交方指向哪一个，
  /// 父子关系也就无从定论；父子校验服务于写入归属，定不下来时不能据此拒绝，因此这些边按存在性与类型放行。
  /// 实例标识不因类型判定收敛到唯一候选而回填：本解析点不为数据用途挑节点。
  /// </remarks>
  /// <param name="target">待校验的目标路径。</param>
  /// <param name="candidates">本批编码的候选节点。</param>
  /// <param name="missingOrganizationMessage">组织层不成立时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院层不成立时的业务拒绝文案；为 <see langword="null"/> 时不校验医院层。</param>
  /// <param name="missingBranchMessage">院区层不成立时的业务拒绝文案；为 <see langword="null"/> 时不校验院区层。</param>
  /// <returns>已校验的路径。</returns>
  /// <exception cref="InvalidOperationException">任一层校验不成立时抛出。</exception>
  private static OrganizationPath ValidateTarget(
    OrganizationPathTarget target,
    CandidateNodes candidates,
    string missingOrganizationMessage,
    string? missingHospitalMessage,
    string? missingBranchMessage)
  {
    // 判定顺序固定为组织、医院、院区：上层不成立时按上层文案拒绝，与既有拒绝口径一致。
    LayerMatch organization = RequireLayer(
      target.OrganizationCode, IamOrganizationTypeCodes.CountyMedicalCommunity, missingOrganizationMessage, candidates);

    LayerMatch? hospital = null;
    if (missingHospitalMessage is not null)
    {
      hospital = RequireLayer(target.HospitalCode, IamOrganizationTypeCodes.Hospital, missingHospitalMessage, candidates);

      // 父子校验按边执行：一条父子边只取决于它两端的编码。组织层或医院层命中多个节点时，
      // 这条边无法定论，跳过它并按存在性与类型放行——父子校验服务于写入归属，定不下来时不应据此拒绝。
      if (organization.IsSoleCandidate
        && hospital.Value.IsSoleCandidate
        && hospital.Value.Node!.ParentOrganizationId != organization.Node!.Id)
      {
        throw new InvalidOperationException(missingHospitalMessage);
      }
    }

    LayerMatch? branch = null;
    if (missingBranchMessage is not null)
    {
      branch = RequireLayer(target.BranchCode, IamOrganizationTypeCodes.Branch, missingBranchMessage, candidates);
      if (hospital is { IsSoleCandidate: true } validatedHospital
        && branch.Value.IsSoleCandidate
        && branch.Value.Node!.ParentOrganizationId != validatedHospital.Node!.Id)
      {
        throw new InvalidOperationException(missingBranchMessage);
      }
    }

    return new OrganizationPath(
      Guid.Empty, organization.Name, organization.Code,
      Guid.Empty, hospital?.Name ?? string.Empty, hospital?.Code ?? string.Empty,
      Guid.Empty, branch?.Name ?? string.Empty, branch?.Code ?? string.Empty);
  }

  /// <summary>
  /// 校验一层编码：必须命中启用节点，且其中至少一个节点的类型符合该层，否则按该层文案业务拒绝。
  /// </summary>
  /// <remarks>
  /// 命中多个节点不是失败：只要有一个候选的类型符合该层，本层校验即成立。
  /// 类型与启停状态以 IAM 目录为准，编码树只负责把编码映射到候选节点标识。
  /// </remarks>
  /// <param name="code">该层提交的业务编码。</param>
  /// <param name="typeCode">该层要求的组织类型编码。</param>
  /// <param name="missingMessage">该层不成立时的业务拒绝文案。</param>
  /// <param name="candidates">本批编码的候选节点。</param>
  /// <returns>该层的校验结果。</returns>
  /// <exception cref="InvalidOperationException">编码为空、未命中任何节点，或没有候选符合该层类型时抛出。</exception>
  private static LayerMatch RequireLayer(string? code, string typeCode, string missingMessage, CandidateNodes candidates)
  {
    string? normalized = NormalizeCode(code);
    if (normalized is null || !candidates.NodesByCode.TryGetValue(normalized, out IReadOnlyList<CodeCandidate> hits))
    {
      throw new InvalidOperationException(missingMessage);
    }

    IamDirectoryOrganization[] typed =
    [
      .. hits
        .Select(hit => hit.Detail)
        .OfType<IamDirectoryOrganization>()
        .Where(node => node.IsEnabled
          && string.Equals(node.OrganizationType?.Code, typeCode, StringComparison.Ordinal))
    ];
    if (typed.Length == 0) throw new InvalidOperationException(missingMessage);

    // 唯一命中时记录该节点，供能定论时的父子归属校验使用；多候选时不留节点，避免被当成已定论的归属。
    bool soleCandidate = hits.Count == 1;
    return new LayerMatch(normalized, NameOf(candidates, normalized), soleCandidate ? typed[0] : null, soleCandidate);
  }

  /// <summary>
  /// 按本批业务编码读取候选节点：编码树给出候选标识，IAM 目录一次读回这些节点的当前名称、类型与启停状态。
  /// </summary>
  /// <param name="codes">本批需要的业务编码。</param>
  /// <returns>业务编码到候选节点与展示名称的映射。</returns>
  private async Task<CandidateNodes> ReadCandidatesAsync(IReadOnlyList<string> codes)
  {
    Dictionary<string, List<OrganizationBusinessCodeTreeNode>> hitsByCode = await ReadNodesByCodeAsync(codes);

    // 名称、类型与启停状态都取目录详情：候选节点标识一次批量读回，读取次数不随目标条数增长。
    Guid[] candidateIds = [.. hitsByCode.Values.SelectMany(hits => hits).Select(node => node.Id)];
    IReadOnlyDictionary<Guid, IamDirectoryOrganization> details = candidateIds.Length == 0
      ? new Dictionary<Guid, IamDirectoryOrganization>()
      : await iamDirectoryReader.ReadAsync(candidateIds);

    Dictionary<string, IReadOnlyList<CodeCandidate>> nodesByCode = new(StringComparer.Ordinal);
    foreach ((string code, List<OrganizationBusinessCodeTreeNode> hits) in hitsByCode)
    {
      nodesByCode[code] =
      [
        .. hits.Select(node => new CodeCandidate(node.Id, details.TryGetValue(node.Id, out IamDirectoryOrganization? detail) ? detail : null))
      ];
    }

    return new CandidateNodes(nodesByCode, await ResolveNamesAsync(nodesByCode));
  }

  /// <summary>
  /// 按当前租户一次读取组织树，并按业务编码归集候选节点。
  /// </summary>
  /// <remarks>
  /// 读取次数只随租户增长，不随编码或目标条数增长；未配置业务编码的节点不进入任何编码的候选；
  /// 同一编码命中多个节点时全部候选都保留，由校验与取名按各自口径使用。
  /// </remarks>
  /// <param name="codes">本批需要的业务编码。</param>
  /// <returns>业务编码到候选节点的映射；未命中的编码不在映射中，租户不可用时为空映射。</returns>
  private async Task<Dictionary<string, List<OrganizationBusinessCodeTreeNode>>> ReadNodesByCodeAsync(IReadOnlyList<string> codes)
  {
    Guid? tenantId = currentIamContext.Current?.TenantId;
    if (codes.Count == 0 || tenantId is not { } selectedTenantId || selectedTenantId == Guid.Empty)
    {
      return new Dictionary<string, List<OrganizationBusinessCodeTreeNode>>(StringComparer.Ordinal);
    }

    HashSet<string> wanted = [.. codes];
    IReadOnlyDictionary<Guid, OrganizationBusinessCodeTreeNode> nodes = await iamBusinessCodeReader.ReadAsync(selectedTenantId);

    Dictionary<string, List<OrganizationBusinessCodeTreeNode>> candidatesByCode = new(StringComparer.Ordinal);
    foreach (OrganizationBusinessCodeTreeNode node in nodes.Values)
    {
      string? code = NormalizeCode(node.Code);
      if (code is null || !wanted.Contains(code)) continue;
      if (!candidatesByCode.TryGetValue(code, out List<OrganizationBusinessCodeTreeNode>? candidates))
      {
        candidates = [];
        candidatesByCode[code] = candidates;
      }

      candidates.Add(node);
    }

    return candidatesByCode;
  }

  /// <summary>
  /// 按容错口径为每个命中的编码取展示名称：唯一候选取其名称，多候选按会话组织链与会名一致性取舍。
  /// </summary>
  /// <remarks>
  /// 名称是展示字段，宁可留空也不猜：既不在多个名称之间择一，也不把业务编码当名称回填。
  /// 会话组织链只在出现多候选时读取，正常路径不增加目录读取次数。
  /// </remarks>
  /// <param name="hitsByCode">业务编码到候选节点的映射。</param>
  /// <returns>业务编码到展示名称的映射；无法确定可信名称时为空串。</returns>
  private async Task<Dictionary<string, string>> ResolveNamesAsync(
    IReadOnlyDictionary<string, IReadOnlyList<CodeCandidate>> nodesByCode)
  {
    Dictionary<string, string> namesByCode = new(StringComparer.Ordinal);
    HashSet<Guid>? currentChain = null;
    foreach ((string code, IReadOnlyList<CodeCandidate> hits) in nodesByCode)
    {
      if (hits.Count == 1)
      {
        namesByCode[code] = TrimName(hits[0].Detail?.Name);
        continue;
      }

      // 会话组织链只在出现多候选时读取：单候选不需要取舍，正常路径不增加目录读取次数。
      currentChain ??= await ReadCurrentOrganizationChainAsync();
      namesByCode[code] = ConvergeName(hits, currentChain);
    }

    return namesByCode;
  }

  /// <summary>
  /// 在候选节点中取展示名称：会话组织链上恰好一个候选时取它的名称，否则全部候选名称一致时取该名称，其余留空。
  /// </summary>
  /// <param name="hits">同一业务编码命中的候选节点，至少两个。</param>
  /// <param name="currentChain">当前会话组织链上的组织标识集合；取不到时为空集合。</param>
  /// <returns>展示名称；无法确定时为空串。</returns>
  private static string ConvergeName(IReadOnlyList<CodeCandidate> hits, HashSet<Guid> currentChain)
  {
    CodeCandidate[] onChain = [.. hits.Where(hit => currentChain.Contains(hit.Id))];
    if (onChain.Length == 1) return TrimName(onChain[0].Detail?.Name);

    string[] names = [.. hits.Select(hit => TrimName(hit.Detail?.Name))];
    return names.All(name => string.Equals(name, names[0], StringComparison.Ordinal)) ? names[0] : string.Empty;
  }

  /// <summary>
  /// 读取当前工作身份所在节点的组织链：该节点在目录中的路径（祖先标识含自身）。
  /// </summary>
  /// <remarks>
  /// 会话组织链只用于取名：它由登录上下文给出，可以说明候选节点是否落在调用方实际工作的组织上。
  /// 取不到当前组织标识或目录中没有该节点时只返回当前标识本身，取名因此只剩「名称是否一致」一条依据。
  /// </remarks>
  /// <returns>当前会话组织链上的组织标识集合。</returns>
  private async Task<HashSet<Guid>> ReadCurrentOrganizationChainAsync()
  {
    Guid? organizationId = currentIamContext.Current?.OrganizationId;
    if (organizationId is not { } selectedOrganizationId || selectedOrganizationId == Guid.Empty) return [];

    IReadOnlyDictionary<Guid, IamDirectoryOrganization> current =
      await iamDirectoryReader.ReadAsync([selectedOrganizationId]);
    HashSet<Guid> chain = [selectedOrganizationId];
    if (current.TryGetValue(selectedOrganizationId, out IamDirectoryOrganization? node))
    {
      chain.UnionWith(node.Path.Select(item => item.Id));
    }

    return chain;
  }

  /// <summary>取一层编码的展示名称；编码没有候选时为空串。</summary>
  /// <param name="candidates">本批编码的候选节点。</param>
  /// <param name="code">该层已规范化的业务编码。</param>
  /// <returns>该层展示名称；无法确定时为空串。</returns>
  private static string NameOf(CandidateNodes candidates, string code) =>
    candidates.NamesByCode.TryGetValue(code, out string? name) ? name : string.Empty;

  /// <summary>
  /// 收集一批目标路径上的业务编码：三层一起去空白、去空值、去重。
  /// </summary>
  /// <param name="targets">目标路径集合。</param>
  /// <returns>本批需要的业务编码。</returns>
  private static string[] CollectCodes(IReadOnlyList<OrganizationPathTarget> targets) =>
  [
    .. targets
      .SelectMany(target => new[] { target.OrganizationCode, target.HospitalCode, target.BranchCode })
      .Select(NormalizeCode)
      .OfType<string>()
      .Distinct(StringComparer.Ordinal)
  ];

  /// <summary>把节点名称规范为展示口径：去首尾空白，未配置名称时为空串。</summary>
  /// <param name="name">节点名称。</param>
  /// <returns>去空白后的名称；空或纯空白时为空串。</returns>
  private static string TrimName(string? name) => name?.Trim() ?? string.Empty;

  /// <summary>
  /// 把提交的编码规范为比较口径：去首尾空白，空白编码视为未提交。
  /// </summary>
  /// <param name="code">提交的编码。</param>
  /// <returns>去空白后的编码；空或纯空白时为 <see langword="null"/>。</returns>
  private static string? NormalizeCode(string? code)
  {
    string normalized = code?.Trim() ?? string.Empty;
    return normalized.Length == 0 ? null : normalized;
  }

  /// <summary>一层编码的校验结果：提交的编码、容错取到的名称、唯一命中时的节点与是否唯一命中。</summary>
  /// <param name="Code">去空白后的提交编码。</param>
  /// <param name="Name">该层展示名称；无法确定时为空串。</param>
  /// <param name="Node">该层唯一命中且类型相符的节点；多候选时为空。</param>
  /// <param name="IsSoleCandidate">该层编码是否恰好命中一个候选节点。</param>
  private readonly record struct LayerMatch(string Code, string Name, IamDirectoryOrganization? Node, bool IsSoleCandidate);

  /// <summary>一个候选节点：编码树给出的节点标识与目录读回的当前详情（目录缺失时为空）。</summary>
  /// <param name="Id">组织实例标识。</param>
  /// <param name="Detail">IAM 目录中的节点详情；目录没有该节点时为空。</param>
  private readonly record struct CodeCandidate(Guid Id, IamDirectoryOrganization? Detail);

  /// <summary>本批编码的候选节点与展示名称。</summary>
  /// <param name="NodesByCode">业务编码到候选节点。</param>
  /// <param name="NamesByCode">业务编码到展示名称；无法确定可信名称时为空串。</param>
  private sealed record CandidateNodes(
    IReadOnlyDictionary<string, IReadOnlyList<CodeCandidate>> NodesByCode,
    IReadOnlyDictionary<string, string> NamesByCode);
}
