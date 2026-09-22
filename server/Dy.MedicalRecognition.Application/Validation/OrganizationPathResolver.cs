using Dy.Base.Application.Contracts.OrganizationAggregate;

namespace Dy.MedicalRecognition.Application.Validation;

/// <summary>
/// 一次性批量读取外部组织服务的组织、医院与院区，在内存中校验三层存在、启用与父子归属，并返回三层名称三元组。
/// </summary>
/// <remarks>
/// 保存与查询入口共用同一解析点，因此两层入口对同一批目标路径给出一致的归属结论与名称。
/// 读取次数只随目标涉及的组织与医院数量增长，不随目标条数增长：组织全量只读一次，每个不同组织读取一次医院，每家不同医院读取一次院区。
/// 解析过程只读外部组织服务，不写库、不发事件、不做角色授权判断，也不按行查询。
/// 归属不成立时的拒绝文案由调用方传入，各入口因此保留各自的对外错误消息。
/// </remarks>
internal sealed class OrganizationPathResolver
{
  /// <summary>
  /// 读取组织、医院与院区的外部组织服务，经框架 HTTP 服务代理注入。
  /// </summary>
  private readonly IOrganizationAppService organizationAppService;

  /// <summary>
  /// 接收外部组织服务作为组织路径的数据来源。
  /// </summary>
  /// <param name="organizationAppService">提供组织、医院与院区主数据的外部组织服务。</param>
  public OrganizationPathResolver(IOrganizationAppService organizationAppService) => this.organizationAppService = organizationAppService;

  /// <summary>
  /// 解析一批目标组织路径，校验每一条的组织、医院与院区存在、启用与父子归属，并按输入顺序返回三层业务编码与名称。
  /// </summary>
  /// <remarks>
  /// 三层业务编码与名称都取自外部组织服务：编码取业务编码属性 <c>Id</c> 去空白后的值（与匹配所用值同源），
  /// 名称取名称属性 <c>Name</c> 的原值；启用状态由启用属性 <c>IsValid</c> 承载。
  /// 校验按组织、医院、院区自外向内进行，任一层不成立即拒绝该条路径，不返回部分结果。
  /// 外部读取按目标路径去重后进行：组织全量读取一次，每个不同组织读取一次医院，每家不同医院读取一次院区，
  /// 因此同一批路径的读取次数只随目标涉及的组织与医院数量增长，不随目标条数增长。
  /// </remarks>
  /// <param name="targets">本批待校验的目标路径，至少一条；同一路径重复出现时按同一结果返回。</param>
  /// <param name="missingOrganizationMessage">组织不存在或已停用时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在或已停用，或不属于该组织时对外抛出的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区不存在或已停用，或不属于该医院时对外抛出的业务拒绝文案。</param>
  /// <returns>与 <paramref name="targets"/> 等长且同序的已校验路径集合。</returns>
  /// <exception cref="ArgumentNullException"><paramref name="targets"/> 为 null 时抛出，此时无法确定校验范围。</exception>
  /// <exception cref="ArgumentException"><paramref name="targets"/> 为空集合时抛出，空批次不代表“无需校验”。</exception>
  /// <exception cref="InvalidOperationException">任一条目标路径的组织、医院或院区不存在、已停用或父子归属不匹配时抛出。</exception>
  public async Task<IReadOnlyList<OrganizationPath>> ResolveOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条待校验的组织路径。", nameof(targets));

    // 去重后的目标组织：本批所有目标路径共用同一份组织全量快照。
    List<TargetOrganization> targetOrganizations = BuildTargetOrganizations(targets);

    // 组织全量只读一次，供本批全部目标路径共用。
    IEnumerable<OrganizationDto> organizations = await organizationAppService.QueryAllOrganizationAsync();

    Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode), OrganizationPath> resolvedPaths = [];
    foreach (TargetOrganization targetOrganization in targetOrganizations)
    {
      OrganizationDto organization = FindOrganization(organizations, targetOrganization.OrganizationCode)
        ?? throw new InvalidOperationException(missingOrganizationMessage);

      // 同一目标组织的医院只读一次，供该组织下的全部目标路径共用。
      IEnumerable<HospitalDto> hospitals = await organizationAppService.QueryAllValidHospitalByOrgIdAsync(new QueryAllValidHospitalByOrgIdRequest { OrgId = targetOrganization.OrganizationCode });
      foreach (string hospitalCode in targetOrganization.HospitalCodes)
      {
        HospitalDto hospital = FindHospital(hospitals, targetOrganization.OrganizationCode, hospitalCode)
          ?? throw new InvalidOperationException(missingHospitalMessage);

        // 同一目标医院的院区只读一次，供该医院下的全部目标路径共用。
        IEnumerable<BranchDto> branches = await organizationAppService.QueryAllValidBranchByHosIdAsync(new QueryAllValidBranchByHosIdRequest { HosId = hospitalCode });
        foreach (string branchCode in targetOrganization.BranchCodesByHospital[hospitalCode])
        {
          BranchDto branch = FindBranch(branches, targetOrganization.OrganizationCode, hospitalCode, branchCode)
            ?? throw new InvalidOperationException(missingBranchMessage);

          resolvedPaths[(targetOrganization.OrganizationCode, hospitalCode, branchCode)] =
            // 返回值必须与匹配口径同源：查找命中的是外部服务 Id 去空白后的值，因此返回的编码也一律去空白，
            // 否则外部主数据带首尾空白时，写入与查询会拿到与互认配置表不同源的编码，表现为误报未建立配置或查出空表。
            new OrganizationPath(
              TrimCode(organization.Id), organization.Name,
              TrimCode(hospital.Id), hospital.Name,
              TrimCode(branch.Id), branch.Name);
        }
      }
    }

    // 结果按输入顺序返回，同一路径重复出现时复用同一份已校验结果。
    // 查找键必须与登记键同源：登记用的是去空白后的值，因此这里也复用同一套 TrimCode 结果，
    // 否则带首尾空白的目标编码在查找时会抛 KeyNotFoundException，把业务拒绝变成未处理异常。
    return
    [
      .. targets.Select(target =>
      {
        string organizationCode = TrimCode(target.OrganizationCode);
        string hospitalCode = TrimCode(target.HospitalCode);
        string branchCode = TrimCode(target.BranchCode);

        return resolvedPaths[(organizationCode, hospitalCode, branchCode)];
      })
    ];
  }

  /// <summary>
  /// 解析一批只到组织与医院两层的目标路径，校验组织与医院存在、启用与父子归属，并按输入顺序返回两层业务编码与名称。
  /// </summary>
  /// <remarks>
  /// 供"院区可选、为空按可信医院全院范围"的统计入口使用：院区层不参与解析，也不读取院区数据，
  /// 返回路径的院区编码与名称为空串。三层目标路径的校验仍走 <see cref="ResolveOrThrow"/>。
  /// 外部读取按目标路径去重后进行：组织全量读取一次，每个不同组织读取一次医院，
  /// 读取次数只随目标涉及的组织与医院数量增长，不随目标条数增长。
  /// </remarks>
  /// <param name="targets">本批待校验的两层目标路径，至少一条；同一路径重复出现时按同一结果返回。</param>
  /// <param name="missingOrganizationMessage">组织不存在或已停用时对外抛出的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在或已停用，或不属于该组织时对外抛出的业务拒绝文案。</param>
  /// <returns>与 <paramref name="targets"/> 等长且同序的已校验路径集合，路径院区编码与名称为空串。</returns>
  /// <exception cref="ArgumentNullException"><paramref name="targets"/> 为 null 时抛出，此时无法确定校验范围。</exception>
  /// <exception cref="ArgumentException"><paramref name="targets"/> 为空集合时抛出，空批次不代表“无需校验”。</exception>
  /// <exception cref="InvalidOperationException">任一条目标路径的组织或医院不存在、已停用或父子归属不匹配时抛出。</exception>
  public async Task<IReadOnlyList<OrganizationPath>> ResolveOrganizationHospitalOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage,
    string missingHospitalMessage)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条待校验的组织路径。", nameof(targets));

    // 去重后的目标组织：本批所有目标路径共用同一份组织全量快照。
    List<TargetOrganization> targetOrganizations = BuildTargetOrganizations(targets);

    // 组织全量只读一次，供本批全部目标路径共用。
    IEnumerable<OrganizationDto> organizations = await organizationAppService.QueryAllOrganizationAsync();

    Dictionary<(string OrganizationCode, string HospitalCode), OrganizationPath> resolvedPaths = [];
    foreach (TargetOrganization targetOrganization in targetOrganizations)
    {
      OrganizationDto organization = FindOrganization(organizations, targetOrganization.OrganizationCode)
        ?? throw new InvalidOperationException(missingOrganizationMessage);

      // 同一目标组织的医院只读一次，供该组织下的全部目标路径共用；院区层不在本方法职责内。
      IEnumerable<HospitalDto> hospitals = await organizationAppService.QueryAllValidHospitalByOrgIdAsync(new QueryAllValidHospitalByOrgIdRequest { OrgId = targetOrganization.OrganizationCode });
      foreach (string hospitalCode in targetOrganization.HospitalCodes)
      {
        HospitalDto hospital = FindHospital(hospitals, targetOrganization.OrganizationCode, hospitalCode)
          ?? throw new InvalidOperationException(missingHospitalMessage);

        resolvedPaths[(targetOrganization.OrganizationCode, hospitalCode)] =
          new OrganizationPath(
            TrimCode(organization.Id), organization.Name,
            TrimCode(hospital.Id), hospital.Name,
            string.Empty, string.Empty);
      }
    }

    // 结果按输入顺序返回，同一路径重复出现时复用同一份已校验结果；查找键与登记键同用去空白后的值。
    return
    [
      .. targets.Select(target => resolvedPaths[(
        TrimCode(target.OrganizationCode),
        TrimCode(target.HospitalCode))])
    ];
  }

  /// <summary>
  /// 解析一批只到组织一层的目标路径，校验组织存在与启用，并按输入顺序返回组织业务编码与名称。
  /// </summary>
  /// <remarks>
  /// 供"任一范围条件为空不附加该层过滤"的统计范围入口使用：医院与院区层未提供取值时只有组织层需要校验，
  /// 医院与院区层不读取也不校验，返回路径的医院、院区编码与名称为空串。
  /// 只提交医院或院区取值的目标路径分别走 <see cref="ResolveOrganizationHospitalOrThrow"/> 与 <see cref="ResolveOrThrow"/>，
  /// 由上级链路逐层完成存在、启用与父子归属校验。
  /// 组织全量只读取一次，读取次数不随目标条数增长。
  /// </remarks>
  /// <param name="targets">本批待校验的组织目标路径，至少一条；同一路径重复出现时按同一结果返回。</param>
  /// <param name="missingOrganizationMessage">组织不存在或已停用时对外抛出的业务拒绝文案。</param>
  /// <returns>与 <paramref name="targets"/> 等长且同序的已校验路径集合，路径医院与院区编码、名称为空串。</returns>
  /// <exception cref="ArgumentNullException"><paramref name="targets"/> 为 null 时抛出，此时无法确定校验范围。</exception>
  /// <exception cref="ArgumentException"><paramref name="targets"/> 为空集合时抛出，空批次不代表“无需校验”。</exception>
  /// <exception cref="InvalidOperationException">任一条目标路径的组织不存在或已停用时抛出。</exception>
  public async Task<IReadOnlyList<OrganizationPath>> ResolveOrganizationOrThrow(
    IReadOnlyList<OrganizationPathTarget> targets,
    string missingOrganizationMessage)
  {
    ArgumentNullException.ThrowIfNull(targets);
    if (targets.Count == 0) throw new ArgumentException("至少需要一条待校验的组织路径。", nameof(targets));

    // 组织全量只读一次，供本批全部目标路径共用。
    IEnumerable<OrganizationDto> organizations = await organizationAppService.QueryAllOrganizationAsync();

    Dictionary<string, OrganizationPath> resolvedPaths = [];
    foreach (string organizationCode in targets.Select(target => TrimCode(target.OrganizationCode)).Distinct())
    {
      OrganizationDto organization = FindOrganization(organizations, organizationCode)
        ?? throw new InvalidOperationException(missingOrganizationMessage);

      resolvedPaths[organizationCode] = new OrganizationPath(
        TrimCode(organization.Id), organization.Name,
        string.Empty, string.Empty,
        string.Empty, string.Empty);
    }

    // 结果按输入顺序返回，同一路径重复出现时复用同一份已校验结果；查找键与登记键同用去空白后的值。
    return [.. targets.Select(target => resolvedPaths[TrimCode(target.OrganizationCode)])];
  }

  /// <summary>
  /// 一个目标组织及其下已去重的医院、院区，用于把本批目标路径折叠成最少的对外读取次数。
  /// </summary>
  private sealed class TargetOrganization
  {
    /// <summary>
    /// 按医院分组的院区编码，医院按首次出现顺序排列。
    /// </summary>
    private readonly Dictionary<string, List<string>> branchCodesByHospital = [];

    /// <summary>
    /// 建立该组织的目标分组。
    /// </summary>
    /// <param name="organizationCode">已去除两端空白的目标组织业务编码。</param>
    public TargetOrganization(string organizationCode) => OrganizationCode = organizationCode;

    /// <summary>已去除两端空白的目标组织业务编码。</summary>
    public string OrganizationCode { get; }

    /// <summary>该组织下按首次出现顺序排列的医院编码，同一医院只出现一次。</summary>
    public IReadOnlyCollection<string> HospitalCodes => branchCodesByHospital.Keys;

    /// <summary>按医院分组的院区编码，组内已去重。</summary>
    public IReadOnlyDictionary<string, List<string>> BranchCodesByHospital => branchCodesByHospital;

    /// <summary>
    /// 把一条目标医院、院区加入该组织分组，重复出现的组合只保留一次。
    /// </summary>
    /// <param name="hospitalCode">已去除两端空白的目标医院业务编码。</param>
    /// <param name="branchCode">已去除两端空白的目标院区业务编码。</param>
    public void AddTarget(string hospitalCode, string branchCode)
    {
      if (!branchCodesByHospital.TryGetValue(hospitalCode, out List<string>? branchCodes))
      {
        branchCodes = [];
        branchCodesByHospital[hospitalCode] = branchCodes;
      }

      if (!branchCodes.Contains(branchCode)) branchCodes.Add(branchCode);
    }
  }

  /// <summary>
  /// 按组织去重并把同一组织下的医院、同一医院下的院区去重，形成外部读取分组。
  /// </summary>
  /// <param name="targets">本批待校验的目标路径。</param>
  /// <returns>按目标出现顺序排列的组织分组；每个组织下的医院与院区都已去重。</returns>
  private static List<TargetOrganization> BuildTargetOrganizations(IReadOnlyList<OrganizationPathTarget> targets)
  {
    List<TargetOrganization> targetOrganizations = [];
    Dictionary<string, TargetOrganization> organizationsByCode = [];
    foreach (OrganizationPathTarget target in targets)
    {
      // 三层编码一律去除首尾空白：外部配置、可信上下文与请求提交值都可能带空白。
      string organizationCode = TrimCode(target.OrganizationCode);
      string hospitalCode = TrimCode(target.HospitalCode);
      string branchCode = TrimCode(target.BranchCode);

      if (!organizationsByCode.TryGetValue(organizationCode, out TargetOrganization? targetOrganization))
      {
        targetOrganization = new TargetOrganization(organizationCode);
        organizationsByCode[organizationCode] = targetOrganization;
        targetOrganizations.Add(targetOrganization);
      }

      targetOrganization.AddTarget(hospitalCode, branchCode);
    }

    return targetOrganizations;
  }

  /// <summary>
  /// 去除业务编码两端空白，使带空白的输入与配置文件、可信上下文中的原始值按同一口径比较。
  /// </summary>
  /// <param name="code">待处理的业务编码；可能为 null。</param>
  /// <returns>去除两端空白后的编码；输入为 null 时返回空串。</returns>
  private static string TrimCode(string? code) => code?.Trim() ?? string.Empty;

  /// <summary>
  /// 在组织全量读取结果中查找业务编码匹配且处于启用状态的组织。
  /// </summary>
  /// <param name="organizations">一次组织全量读取返回的组织集合。</param>
  /// <param name="organizationCode">已去除两端空白的目标组织业务编码。</param>
  /// <returns>匹配到的启用组织；不存在或已停用时返回 null。</returns>
  private static OrganizationDto? FindOrganization(IEnumerable<OrganizationDto> organizations, string organizationCode)
  {
    foreach (OrganizationDto organization in organizations)
    {
      // 业务编码与启用状态必须同时成立：编码命中但已停用的组织按不可用处理。
      if (string.Equals(organization.Id?.Trim(), organizationCode, StringComparison.Ordinal) && organization.IsValid) return organization;
    }

    return null;
  }

  /// <summary>
  /// 在该组织的启用医院中查找业务编码匹配、启用且父组织归属正确的医院。
  /// </summary>
  /// <param name="hospitals">该组织的启用医院读取结果。</param>
  /// <param name="organizationCode">已去除两端空白的目标组织业务编码，作为医院的父归属。</param>
  /// <param name="hospitalCode">已去除两端空白的目标医院业务编码。</param>
  /// <returns>匹配到的医院；不存在、已停用或不属于该组织时返回 null。</returns>
  private static HospitalDto? FindHospital(IEnumerable<HospitalDto> hospitals, string organizationCode, string hospitalCode)
  {
    foreach (HospitalDto hospital in hospitals)
    {
      // 编码、启用状态与父组织三者必须同时成立：外部服务返回了同一编码但父级不同的医院时按越界归属拒绝。
      if (string.Equals(hospital.Id?.Trim(), hospitalCode, StringComparison.Ordinal)
        && hospital.IsValid
        && string.Equals(hospital.OrgId?.Trim(), organizationCode, StringComparison.Ordinal)) return hospital;
    }

    return null;
  }

  /// <summary>
  /// 在该医院的启用院区中查找业务编码匹配、启用且父医院、父组织归属都正确的院区。
  /// </summary>
  /// <param name="branches">该医院的启用院区读取结果。</param>
  /// <param name="organizationCode">已去除两端空白的目标组织业务编码，作为院区的父组织归属。</param>
  /// <param name="hospitalCode">已去除两端空白的目标医院业务编码，作为院区的父医院归属。</param>
  /// <param name="branchCode">已去除两端空白的目标院区业务编码。</param>
  /// <returns>匹配到的院区；不存在、已停用或不属于该医院、该组织时返回 null。</returns>
  private static BranchDto? FindBranch(IEnumerable<BranchDto> branches, string organizationCode, string hospitalCode, string branchCode)
  {
    foreach (BranchDto branch in branches)
    {
      // 父医院、父组织与启用状态必须同时成立：院区编码在不同医院之间不保证唯一，只比对编码会放进越权院区。
      if (string.Equals(branch.Id?.Trim(), branchCode, StringComparison.Ordinal)
        && branch.IsValid
        && string.Equals(branch.HosId?.Trim(), hospitalCode, StringComparison.Ordinal)
        && string.Equals(branch.OrgId?.Trim(), organizationCode, StringComparison.Ordinal)) return branch;
    }

    return null;
  }
}
