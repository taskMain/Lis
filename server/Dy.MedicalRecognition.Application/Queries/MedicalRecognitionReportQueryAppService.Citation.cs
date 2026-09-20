using Dy.Base.Application.Contracts.SystemParameterAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 获取引用详情：医生采纳互认匹配后按本次就诊取回可写入病历的项目内容与报告公共上下文。
/// </summary>
/// <remarks>
/// 本链路是只读查询：不产生任何写入、事件或状态变化，不声明显式事务。
/// 组织、医院与院区三层都取自可信上下文，请求只提供医院必然掌握的患者证件与本次来源就诊；
/// 引用详情有效时长按平台级编码读取后在本分片内联校验，参数不存在、取值非法或参数服务读取失败都整次失败，不使用任何兜底默认值；
/// 有效期按各匹配记录的处理结果保存时间分别计算，每次请求读取当前参数值，不为采纳记录保存独立有效时长或截止时间；
/// 自然超过可互认时间不影响已采纳项目获取详情：本链路不读取互认配置，可互认时间只约束新的匹配查询。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 按患者证件与本次来源就诊获取仍可引用的已采纳项目内容与报告公共上下文。
  /// </summary>
  /// <remarks>
  /// 校验与读取顺序固定：公共请求校验 → 可信三层解析 → 引用详情有效时长读取与校验 → 候选采纳记录读取 →
  /// 逐项有效期与决定校验 → 同版本同项目取保存时间最近的一条 → 报告版本有效性校验 → 报告公共上下文与项目内容按集合一次读回 →
  /// 来源医疗机构名称批量回填。
  /// 参数读取失败发生在任何候选读取之前，因此取值异常不会产生任何数据库往返，也不会产生写入或事件；
  /// 同一报告版本与同一互认项目存在多条采纳时只返回保存时间最近的一条，保存时间相同时以匹配项标识升序兜底，较早事实仍保留并参与既有统计；
  /// 报告公共上下文按报告归并，同一报告命中多个互认项目时只返回一次；
  /// 部分项目超期或失效时返回仍可用的部分，全部项目均不可返回时按平台实际确认的原因抛出业务拒绝，不返回成功空集合；
  /// 检查所见与检查结论属于整份报告的公共内容，随报告公共上下文返回。
  /// </remarks>
  /// <param name="request">引用详情请求，携带患者证件类型、证件号码、本次来源就诊类型与就诊流水号。</param>
  /// <returns>仍可引用的已采纳项目与其所属报告的必要公共上下文。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">请求字段不满足契约声明的必填或值域约束时抛出，此时不读取参数与任何业务数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，引用详情有效时长参数不存在、取值非法或参数服务读取失败时抛出；
  /// 本次就诊在该医院、院区与证件下没有已采纳记录、已采纳项目全部超出有效时长、绑定报告版本全部失效，
  /// 以及超出有效时长与版本失效两类原因同时存在时同样抛出，
  /// 异常文案只说明平台能够确认的事实，不披露其他患者的记录是否存在。
  /// </exception>
  public async Task<RecognitionCitationDetailReadModel> QueryRecognitionCitationDetailAsync(RecognitionCitationDetailRequest request)
  {
    // 请求校验先于任何读取：请求不成立时既不解析可信范围，也不读取参数与业务数据。
    MedicalRecognitionRequestValidator.Validate(request);
    // 可信范围先于业务数据读取解析：可信上下文不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");

    // 每次请求读取当前参数值：参数调整后立即按原起点影响既有采纳，因此不保存截止时间。
    int validHours = await ResolveCitationDetailValidHoursAsync();

    IReadOnlyList<RecognitionCitationCandidateItem> candidates = [.. await repository.QueryRecognitionCitationCandidatesAsync(
      trustedScope.OrganizationCode,
      trustedScope.HospitalCode,
      trustedScope.BranchCode,
      NormalizeCode(request.IdentityDocumentTypeCode),
      NormalizeDocumentNo(request.IdentityDocumentNo),
      request.VisitType,
      request.VisitSerialNo)];
    if (candidates.Count == 0) throw new InvalidOperationException("业务拒绝：未查询到该就诊的互认采纳记录。");

    // 有效期按各匹配记录分别计算，边界按包含处理；记录尚未保存处理结果时没有有效期起点，按不可返回处理。
    DateTime receivedTime = DateTime.Now;
    List<RecognitionCitationCandidateItem> withinValidity = [.. candidates.Where(candidate => IsWithinValidity(candidate.DecisionSavedTime, validHours, receivedTime))];
    // 不采纳项目不返回；决定为采纳是引用详情的逐项校验条件之一。
    List<RecognitionCitationCandidateItem> adopted = [.. candidates.Where(candidate => candidate.RecognitionResult == RecognitionResult.Adopted)];
    List<RecognitionCitationCandidateItem> adoptedWithinValidity =
      [.. withinValidity.Where(candidate => candidate.RecognitionResult == RecognitionResult.Adopted)];
    // 分组独立判断有效期与版本有效性：同一次就诊的其他组超期或失效不影响本组可用项目。
    if (adopted.Count == 0) throw new InvalidOperationException("业务拒绝：该就诊下无已采纳的互认项目可供返回。");
    if (adoptedWithinValidity.Count == 0) throw new InvalidOperationException("业务拒绝：该就诊的互认采纳记录已超过引用详情有效时长，无可返回的引用内容。");

    // 同一报告版本与同一互认项目存在多条采纳时只返回保存时间最近的一条，匹配项标识升序兜底；
    // 组键已包含报告版本，因此组内选定后的集合本身就是按版本去重的集合。
    IReadOnlyList<RecognitionCitationCandidateItem> availableItems = SelectLatestPerVersionAndProject(adoptedWithinValidity);
    IReadOnlyList<Guid> candidateVersionIds = [.. availableItems.Select(candidate => candidate.ReportVersionId)];
    HashSet<Guid> validReportVersionIds = [.. (await repository.QueryValidRecognitionReportVersionIdsAsync(candidateVersionIds)).Select(item => item.Id)];
    List<RecognitionCitationCandidateItem> available = [.. availableItems.Where(candidate => validReportVersionIds.Contains(candidate.ReportVersionId))];

    // 自然超期与报告版本失效必须分开表达：已超期的记录不参与版本判定，因此按本次是否有记录超期区分两类原因。
    if (available.Count == 0 && adoptedWithinValidity.Count == adopted.Count) throw new InvalidOperationException("业务拒绝：该就诊的互认采纳记录绑定的报告版本均已失效，无可返回的引用内容。");
    if (available.Count == 0) throw new InvalidOperationException("业务拒绝：该就诊的互认采纳记录已超过引用详情有效时长，且绑定的报告版本均已失效，无可返回的引用内容。");

    return await BuildCitationDetailAsync(available);
  }

  /// <summary>
  /// 读取并校验引用详情有效时长。
  /// </summary>
  /// <remarks>
  /// 读取请求只携带平台级参数编码：该请求类型不承载组织、医院与院区维度，取值不随调用归属变化；
  /// 只接受正整数小时，零、负数、非整数、空值与参数不存在一律整次失败，不使用兜底默认值。
  /// </remarks>
  /// <returns>引用详情有效时长的小时数。</returns>
  /// <exception cref="InvalidOperationException">参数不存在、返回值非法或参数服务读取失败时抛出。</exception>
  private async Task<int> ResolveCitationDetailValidHoursAsync()
  {
    SystemParameterDto? parameter = await systemParameterAppService.GetSystemParameterByCodeAsync(new GetSystemParameterByCodeRequest
    {
      Code = CitationDetailValidHours
    });

    string? hoursValue = parameter?.Value;
    if (hoursValue is null) throw new InvalidOperationException("业务拒绝：未配置引用详情有效时长。");
    if (!int.TryParse(hoursValue, out int hours) || hours <= 0) throw new InvalidOperationException("业务拒绝：引用详情有效时长取值非法。");

    return hours;
  }

  /// <summary>
  /// 判断一条采纳记录是否仍落在引用详情有效期内。
  /// </summary>
  /// <remarks>
  /// 起点为所属匹配记录的处理结果保存时间，经过时间小于或等于当前参数值时为真，边界包含；
  /// 尚未保存处理结果时没有有效期起点，按不可返回处理，不以下降为调用时点或匹配生成时间代替。
  /// </remarks>
  /// <param name="decisionSavedTime">该组处理结果的保存时间；未保存时为 <see langword="null"/>。</param>
  /// <param name="validHours">本次读取到的引用详情有效时长。</param>
  /// <param name="receivedTime">平台收到本次请求的时间，作为经过时间的终点。</param>
  /// <returns>仍在有效期内时为 <see langword="true"/>。</returns>
  private static bool IsWithinValidity(DateTime? decisionSavedTime, int validHours, DateTime receivedTime) =>
    decisionSavedTime is DateTime savedTime && receivedTime - savedTime <= TimeSpan.FromHours(validHours);

  /// <summary>
  /// 按报告版本与标准项目编码分组，每组只保留处理结果保存时间最近的一条采纳。
  /// </summary>
  /// <remarks>
  /// 保存时间相同时以匹配项标识升序取第一条，与候选查询的排序口径一致，因此结果不随数据库执行计划变化；
  /// 返回顺序为处理结果保存时间倒序加匹配项标识升序，使同一批内的返回顺序稳定。
  /// </remarks>
  /// <param name="candidates">已通过有效期初筛的候选采纳记录。</param>
  /// <returns>同一报告版本与同一互认项目下保存时间最近的一条采纳。</returns>
  private static IReadOnlyList<RecognitionCitationCandidateItem> SelectLatestPerVersionAndProject(
    IReadOnlyList<RecognitionCitationCandidateItem> candidates) =>
  [
    .. candidates
      .GroupBy(candidate => (candidate.ReportVersionId, candidate.StandardProjectCode))
      .Select(group => group
        .OrderByDescending(candidate => candidate.DecisionSavedTime)
        .ThenBy(candidate => candidate.RecognitionMatchItemId)
        .First())
      .OrderByDescending(candidate => candidate.DecisionSavedTime)
      .ThenBy(candidate => candidate.RecognitionMatchItemId)
  ];

  /// <summary>
  /// 把仍可用的采纳记录组装为引用详情：按报告版本集合读取报告公共上下文与项目内容，并批量回填来源医疗机构名称。
  /// </summary>
  /// <remarks>
  /// 报告公共上下文、普通检验结果、检查项目与检查部位都按本次选出的报告版本或检查项目集合各读一次，
  /// 读取次数只随本批不同的报告版本数量与检查项目数量增长，不随返回的匹配项数增长；
  /// 来源医疗机构名称按本批来源三值经组织路径解析点批量解析后回填，读取次数只随本批不同组织与医院数量增长；
  /// 报告公共上下文按报告归并，同一报告命中多个互认项目时只返回一次。
  /// </remarks>
  /// <param name="candidates">已通过有效期与版本有效性校验的采纳记录。</param>
  /// <returns>引用详情读模型。</returns>
  /// <exception cref="InvalidOperationException">本次要返回的报告版本在平台不可读，或外部组织服务不可用时抛出，此时不返回不完整的引用详情。</exception>
  private async Task<RecognitionCitationDetailReadModel> BuildCitationDetailAsync(IReadOnlyList<RecognitionCitationCandidateItem> candidates)
  {
    // 报告标识按首次出现顺序保留，使返回的报告公共上下文顺序与候选记录顺序一致。
    List<Guid> reportIds = [];
    foreach (RecognitionCitationCandidateItem candidate in candidates)
    {
      if (!reportIds.Contains(candidate.ReportId)) reportIds.Add(candidate.ReportId);
    }

    // 每个报告取一条代表记录即可取到该报告的报告版本与来源三值：报告公共上下文按报告归并，只返回一次。
    List<RecognitionCitationCandidateItem> reportRepresentatives =
      [.. reportIds.Select(reportId => candidates.First(candidate => candidate.ReportId == reportId))];
    IReadOnlyList<Guid> reportVersionIds = [.. reportRepresentatives.Select(candidate => candidate.ReportVersionId)];
    Dictionary<Guid, RecognitionCitationReportContextItem> contextsByVersion =
      (await repository.QueryRecognitionCitationReportContextsAsync(reportVersionIds)).ToDictionary(context => context.ReportVersionId);

    IReadOnlyList<OrganizationPath> resolvedPaths = await organizationPathResolver.ResolveOrThrow(
      [.. reportRepresentatives.Select(candidate =>
      {
        RecognitionCitationReportContextItem context = ResolveContext(contextsByVersion, candidate);
        return new OrganizationPathTarget(context.SourceOrganizationCode, context.SourceHospitalCode, context.SourceBranchCode);
      })],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");
    Dictionary<Guid, OrganizationPath> sourcePathsByReport = [];
    for (int index = 0; index < reportIds.Count; index++) sourcePathsByReport[reportIds[index]] = resolvedPaths[index];

    // 项目级内容按本次要返回的报告版本集合与检查项目集合各读一次，读取次数不随返回的匹配项数增长。
    Dictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> laboratoryResultsByVersion =
      GroupLaboratoryResultsByVersion(await repository.QueryLaboratoryResultItemsByVersionsAsync(reportVersionIds));
    Dictionary<Guid, IReadOnlyList<ExaminationItemView>> examinationItemsByVersion =
      GroupExaminationItemsByVersion(await repository.QueryExaminationItemsByVersionsAsync(reportVersionIds));
    List<Guid> examinationItemIds = [.. examinationItemsByVersion.Values.SelectMany(items => items).Select(item => item.ItemId)];
    Dictionary<Guid, IReadOnlyList<ExaminationSiteView>> examinationSitesByItem =
      (await repository.QueryExaminationSitesByItemsAsync(examinationItemIds))
        .GroupBy(site => site.ExaminationItemId)
        .ToDictionary(group => group.Key, group => (IReadOnlyList<ExaminationSiteView>)[.. group]);

    // 标准项目名称按本次命中的编码集合一次读回：读取次数只随本批不同编码数量增长，不随返回的匹配项数增长。
    List<string> standardProjectCodes = [.. candidates.Select(candidate => candidate.StandardProjectCode).Distinct(StringComparer.Ordinal)];
    Dictionary<string, string> standardProjectNames =
      (await repository.QueryRecognitionCitationStandardProjectNamesAsync(standardProjectCodes))
        .ToDictionary(item => item.StandardProjectCode, item => item.StandardProjectName, StringComparer.Ordinal);

    return new RecognitionCitationDetailReadModel
    {
      MatchItems =
      [
        .. candidates.Select(candidate => MapCitationItem(
          candidate, standardProjectNames, contextsByVersion, laboratoryResultsByVersion, examinationItemsByVersion, examinationSitesByItem))
      ],
      ReportContext =
      [
        .. reportIds.Select(reportId =>
        {
          RecognitionCitationCandidateItem candidate = candidates.First(item => item.ReportId == reportId);
          return MapReportContext(ResolveContext(contextsByVersion, candidate), sourcePathsByReport[reportId]);
        })
      ]
    };
  }

  /// <summary>
  /// 取某条采纳记录绑定版本的报告公共上下文行。
  /// </summary>
  /// <param name="contextsByVersion">按报告版本标识索引的报告公共上下文。</param>
  /// <param name="candidate">本次要取上下文的采纳记录。</param>
  /// <returns>该版本的报告公共上下文行。</returns>
  /// <exception cref="InvalidOperationException">该报告版本在平台不可读时抛出。</exception>
  private static RecognitionCitationReportContextItem ResolveContext(
    IReadOnlyDictionary<Guid, RecognitionCitationReportContextItem> contextsByVersion, RecognitionCitationCandidateItem candidate) =>
    contextsByVersion.TryGetValue(candidate.ReportVersionId, out RecognitionCitationReportContextItem? context)
      ? context
      : throw new InvalidOperationException("业务拒绝：本次引用绑定的报告版本不可读。");

  /// <summary>
  /// 按报告版本标识把一次读回的普通检验结果分组。
  /// </summary>
  /// <param name="laboratoryResults">本批版本下的全部普通检验结果。</param>
  /// <returns>报告版本标识到该版本结果的映射；没有结果的版本不在映射内。</returns>
  private static Dictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> GroupLaboratoryResultsByVersion(
    IEnumerable<LaboratoryResultItemView> laboratoryResults) =>
    laboratoryResults
      .GroupBy(result => result.ReportVersionId)
      .ToDictionary(group => group.Key, group => (IReadOnlyList<LaboratoryResultItemView>)[.. group]);

  /// <summary>
  /// 按报告版本标识把一次读回的检查项目分组。
  /// </summary>
  /// <param name="examinationItems">本批版本下的全部检查项目。</param>
  /// <returns>报告版本标识到该版本项目的映射；没有项目的版本不在映射内。</returns>
  private static Dictionary<Guid, IReadOnlyList<ExaminationItemView>> GroupExaminationItemsByVersion(
    IEnumerable<ExaminationItemView> examinationItems) =>
    examinationItems
      .GroupBy(item => item.ReportVersionId)
      .ToDictionary(group => group.Key, group => (IReadOnlyList<ExaminationItemView>)[.. group]);

  /// <summary>
  /// 把一条采纳记录映射为引用详情的项目级内容。
  /// </summary>
  /// <remarks>
  /// 检验报告返回本次命中项目对应的检验结果明细，检查报告返回命中项目的检查部位；
  /// 标准项目名称取自标准目录的标准项目行，与来源报告明细上的来源项目名称是两个来源。
  /// </remarks>
  /// <param name="candidate">本次要映射的采纳记录。</param>
  /// <param name="standardProjectNames">本次命中项目在标准目录中的展示名称，按标准项目编码索引。</param>
  /// <param name="contextsByVersion">按报告版本标识索引的报告公共上下文。</param>
  /// <param name="laboratoryResultsByVersion">本批版本下的普通检验结果，按报告版本标识索引。</param>
  /// <param name="examinationItemsByVersion">本批版本下的检查项目，按报告版本标识索引。</param>
  /// <param name="examinationSitesByItem">本批版本下的检查部位，按检查项目标识索引。</param>
  /// <returns>该采纳记录的项目级引用内容。</returns>
  /// <exception cref="InvalidOperationException">该报告版本在平台不可读时抛出。</exception>
  private static RecognitionCitationItemReadModel MapCitationItem(
    RecognitionCitationCandidateItem candidate,
    IReadOnlyDictionary<string, string> standardProjectNames,
    IReadOnlyDictionary<Guid, RecognitionCitationReportContextItem> contextsByVersion,
    IReadOnlyDictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> laboratoryResultsByVersion,
    IReadOnlyDictionary<Guid, IReadOnlyList<ExaminationItemView>> examinationItemsByVersion,
    IReadOnlyDictionary<Guid, IReadOnlyList<ExaminationSiteView>> examinationSitesByItem)
  {
    RecognitionCitationReportContextItem context = ResolveContext(contextsByVersion, candidate);
    bool isLaboratory = context.ReportType == MedicalReportType.Laboratory;
    IReadOnlyList<LaboratoryResultItemView> laboratoryResults =
      isLaboratory && laboratoryResultsByVersion.TryGetValue(candidate.ReportVersionId, out IReadOnlyList<LaboratoryResultItemView>? versionResults)
        ? versionResults
        : [];
    IReadOnlyList<ExaminationItemView> examinationItems =
      !isLaboratory && examinationItemsByVersion.TryGetValue(candidate.ReportVersionId, out IReadOnlyList<ExaminationItemView>? versionItems)
        ? versionItems
        : [];

    return new RecognitionCitationItemReadModel
    {
      RecognitionMatchItemId = candidate.RecognitionMatchItemId,
      ReportId = candidate.ReportId,
      ReportVersionId = candidate.ReportVersionId,
      StandardProjectCode = candidate.StandardProjectCode,
      // 标准项目名称取自标准目录；名称缺失时返回空串，不按编码拼接名称。
      StandardProjectName = standardProjectNames.TryGetValue(candidate.StandardProjectCode, out string? standardProjectName)
        ? standardProjectName
        : string.Empty,
      LaboratoryResults =
      [
        .. laboratoryResults
          .Where(result => string.Equals(result.StandardProjectCode, candidate.StandardProjectCode, StringComparison.Ordinal))
          .Select(MapCitationLaboratoryResult)
      ],
      ExaminationSites = isLaboratory
        ? []
        :
        [
          .. examinationItems
            .Where(item => string.Equals(item.StandardProjectCode, candidate.StandardProjectCode, StringComparison.Ordinal))
            .Where(item => examinationSitesByItem.ContainsKey(item.ItemId))
            .SelectMany(item => examinationSitesByItem[item.ItemId])
            .Select(site => new RecognitionExaminationSiteReadModel { SiteName = site.SiteName, SourceSiteCode = site.SourceSiteCode })
        ]
    };
  }

  /// <summary>
  /// 把一条普通检验结果映射为引用详情的检验结果明细。
  /// </summary>
  /// <remarks>异常标志与危急值标志按枚举声明解析为中文文本，只表达来源医院的判断，平台不重新计算。</remarks>
  /// <param name="result">来源报告版本下的一条普通检验结果。</param>
  /// <returns>引用详情的检验结果明细。</returns>
  private static RecognitionCitationLaboratoryResultReadModel MapCitationLaboratoryResult(LaboratoryResultItemView result) => new()
  {
    ResultItemName = result.SourceProjectName,
    SourceResultContent = result.SourceResultText,
    Unit = result.Unit,
    SourceReferenceRange = result.ReferenceRange,
    SourceAbnormalFlag = ResolveEnumDescription(result.AbnormalFlag, LaboratoryAbnormalFlagDescriptorList.List),
    SourceCriticalValueFlag = result.CriticalValueFlag is bool criticalValueFlag ? (criticalValueFlag ? "是" : "否") : null
  };

  /// <summary>
  /// 按 SourceGen 生成的枚举描述列表解析可空枚举的中文说明。
  /// </summary>
  /// <remarks>取值未提供时返回空值；取值未登记时同样返回空值，不静默替换为其他文案。</remarks>
  /// <typeparam name="TEnum">待解析的枚举类型。</typeparam>
  /// <param name="value">枚举值；未提供时为 <see langword="null"/>。</param>
  /// <param name="descriptors">该枚举自己的 SourceGen 描述列表。</param>
  /// <returns>枚举值对应的中文说明；未提供或取值未登记时返回 <see langword="null"/>。</returns>
  private static string? ResolveEnumDescription<TEnum>(TEnum? value, IEnumerable<IEnumDescriptor> descriptors)
    where TEnum : struct, Enum =>
    value is TEnum enumValue
      ? descriptors.FirstOrDefault(descriptor => descriptor.EnumValue is TEnum item && EqualityComparer<TEnum>.Default.Equals(item, enumValue))?.Description
      : null;

  /// <summary>
  /// 把报告公共上下文行与已校验的来源路径映射为引用详情的报告公共上下文。
  /// </summary>
  /// <remarks>
  /// 来源医疗机构名称取组织路径解析的批量读取结果，不按编码拼接名称、不查询平台组织表；
  /// 文件入口不携带独立有效期，也不携带一次性或消费状态，下载入口指向阶段 4 已交付的下载动作。
  /// </remarks>
  /// <param name="context">本次报告的报告公共上下文行。</param>
  /// <param name="sourcePath">已校验通过的来源组织路径，提供三层名称。</param>
  /// <returns>引用详情的报告公共上下文。</returns>
  private static RecognitionReportContextReadModel MapReportContext(
    RecognitionCitationReportContextItem context, OrganizationPath sourcePath) => new()
  {
    ReportId = context.ReportId,
    ReportVersionId = context.ReportVersionId,
    ReportType = context.ReportType,
    ReportNo = context.ReportNo,
    ReportName = context.ReportName,
    SourceOrganizationCode = context.SourceOrganizationCode,
    SourceOrganizationName = sourcePath.OrganizationName,
    SourceHospitalCode = context.SourceHospitalCode,
    SourceHospitalName = sourcePath.HospitalName,
    SourceBranchCode = context.SourceBranchCode,
    SourceBranchName = sourcePath.BranchName,
    ClinicalTime = context.ClinicalTime,
    ReportTime = context.ReportTime,
    SourceApplicantDoctorId = context.SourceApplicantDoctorId,
    SourceApplicantDoctorName = context.SourceApplicantDoctorName,
    SourceReviewerDoctorId = context.SourceReviewerDoctorId,
    SourceReviewerDoctorName = context.SourceReviewerDoctorName,
    ExaminationFindings = context.ExaminationFindings,
    ExaminationConclusion = context.ExaminationConclusion,
    File = new PdfAndImageAccessReadModel
    {
      PdfFileId = context.PdfFileId,
      PdfDownloadUrl = BuildPdfDownloadUrl(context.ReportId, context.ReportVersionId),
      PdfOriginalFileName = context.PdfFileName,
      SourceImageStatus = context.SourceImageStatus,
      ImageAccessUrl = context.ImageAccessUrl
    }
  };

  /// <summary>
  /// 按阶段 4 已交付的下载动作形态拼出 PDF 下载入口。
  /// </summary>
  /// <remarks>该入口指向既有下载能力，不新增下载接口；文件内容仍按文件流由该动作返回。</remarks>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>相对下载入口。</returns>
  private static string BuildPdfDownloadUrl(Guid reportId, Guid reportVersionId) =>
    $"api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf";

  /// <summary>
  /// 去除业务编码两端空白，使可信上下文、写入侧保存值与查询侧提交值按同一口径比较。
  /// </summary>
  /// <param name="value">待处理的业务编码；可能为 <see langword="null"/>。</param>
  /// <returns>去除两端空白后的编码；输入为 <see langword="null"/> 时返回空串。</returns>
  private static string NormalizeCode(string? value) => value?.Trim() ?? string.Empty;

  /// <summary>
  /// 按规范形式处理证件号码：去首尾空白并统一大写，与写入侧保存匹配记录时的口径一致。
  /// </summary>
  /// <param name="value">调用方提交的证件号码；可能为 <see langword="null"/>。</param>
  /// <returns>规范形式后的证件号码；输入为 <see langword="null"/> 时返回空串。</returns>
  private static string NormalizeDocumentNo(string? value) => NormalizeCode(value).ToUpperInvariant();
}
