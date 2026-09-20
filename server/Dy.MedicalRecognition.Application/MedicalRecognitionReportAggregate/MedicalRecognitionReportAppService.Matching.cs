using System.ComponentModel.DataAnnotations;
using Dy.Base.Application.Contracts.SystemParameterAggregate;
using Dy.Core.Abstractions.Http;
using Dy.Core.Extensions.Models;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 在线互认主流程的医院接入写入口：互认匹配请求。
/// </summary>
/// <remarks>
/// 组织、医院与院区三层都取自可信上下文，操作人取自登录上下文；本院报告两个参数按平台级编码读取后在本类内联校验，
/// 任一参数缺失、非法或参数服务读取失败都整次失败，不使用任何兜底默认值。
/// 本入口是事务边界：匹配记录与匹配项共同成功或共同失败；没有任何报告命中时在任何写入之前返回成功空结果。
/// </remarks>
public partial class MedicalRecognitionReportAppService
{
  /// <summary>
  /// 本院报告匹配开关的合法关闭取值。
  /// </summary>
  private const string OwnHospitalMatchSwitchOff = "0";

  /// <summary>
  /// 本院报告匹配开关的合法开启取值。
  /// </summary>
  private const string OwnHospitalMatchSwitchOn = "1";

  /// <summary>
  /// 按患者身份与本次来源就诊请求可用的互认匹配，并组装匹配响应。
  /// </summary>
  /// <remarks>
  /// 校验顺序固定：公共请求校验 → 可信三层解析 → 操作人解析 → 本院报告两个参数读取与校验 → 命令组装 → 领域匹配 → 读模型映射。
  /// 参数读取失败发生在领域匹配之前，因此取值异常不会产生任何写入，也不会登记事件；
  /// 匹配响应的报告公共信息与项目级内容按本次匹配项绑定的报告版本集合各读一次，命中项目的标准项目名称取自领域匹配时已读回的互认配置行，
  /// 来源医疗机构名称按来源三值批量回填，数据库往返次数只随本批不同的报告版本数量增长，不随返回的报告数与命中项目数增长。
  /// </remarks>
  /// <param name="request">匹配查询请求，携带患者证件、本次来源就诊与拟开标准项目集合。</param>
  /// <returns>匹配响应；没有任何报告命中时返回成功空结果。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或值域约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，本院报告匹配开关或排除时长取值缺失、非法或参数服务读取失败时抛出；
  /// 请求携带不可用项目编码、平台患者不可解析或匹配写入未影响恰好一行时同样抛出，此时不创建记录、不登记事件。
  /// </exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<RecognitionMatchesResponseReadModel> RequestRecognitionMatchesAsync(RecognitionMatchQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out Guid userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");

    bool ownHospitalMatchEnabled = await ResolveOwnHospitalMatchEnabledAsync();
    // 排除时长允许零，零表示不增加额外等待；小数、负数与非整数取值都在这里被拒绝，不使用任何兜底默认值。
    int ownHospitalExcludeHours = await ResolveOwnHospitalExcludeHoursAsync();

    QueryRecognitionMatchesCommand command = new()
    {
      ReceiverOrganizationCode = trustedScope.OrganizationCode,
      ReceiverHospitalCode = trustedScope.HospitalCode,
      ReceiverBranchCode = trustedScope.BranchCode,
      IdentityDocumentTypeCode = request.IdentityDocumentTypeCode,
      IdentityDocumentNo = request.IdentityDocumentNo,
      VisitType = request.VisitType,
      VisitSerialNo = request.VisitSerialNo,
      // 查询截止点取平台收到本次查询的时间：请求不携带挂号时间、入院时间与统一就诊时间。
      MatchCreatedTime = DateTime.Now,
      OwnHospitalMatchEnabled = ownHospitalMatchEnabled,
      OwnHospitalExcludeHours = ownHospitalExcludeHours,
      ProposedItems =
      [
        .. request.ProposedItems.Select(item => new QueryRecognitionMatchesCommandItem
        {
          ItemType = item.ItemType,
          StandardProjectCode = item.StandardProjectCode
        })
      ],
      OperId = userId,
      OperTime = DateTimeOffset.UtcNow
    };

    RecognitionMatchResult result = await manager.RequestRecognitionMatchesAsync(command);
    if (result.RecognitionMatchRecordId is null || result.MatchCreatedTime is null) return new RecognitionMatchesResponseReadModel { HasMatches = false };

    return new RecognitionMatchesResponseReadModel
    {
      HasMatches = true,
      RecognitionMatchRecordId = result.RecognitionMatchRecordId,
      MatchCreatedTime = result.MatchCreatedTime,
      Reports = await AssembleMatchReportsAsync(result.MatchItems, result.StandardProjectNames)
    };
  }

  /// <summary>
  /// 读取并校验本院报告匹配开关。
  /// </summary>
  /// <remarks>只接受字符串 1 与 0；参数不存在、取值非法或参数服务读取失败一律整次失败，不使用兜底默认值。</remarks>
  /// <returns>开关是否开启。</returns>
  /// <exception cref="InvalidOperationException">参数不存在、返回值非法或参数服务读取失败时抛出。</exception>
  private async Task<bool> ResolveOwnHospitalMatchEnabledAsync()
  {
    string? switchValue = await ReadSystemParameterValueAsync(OwnHospitalMatchSwitch);
    if (switchValue is null) throw new InvalidOperationException("业务拒绝：未配置本院报告匹配开关。");
    if (string.Equals(switchValue, OwnHospitalMatchSwitchOn, StringComparison.Ordinal)) return true;
    if (string.Equals(switchValue, OwnHospitalMatchSwitchOff, StringComparison.Ordinal)) return false;

    throw new InvalidOperationException("业务拒绝：本院报告匹配开关取值非法。");
  }

  /// <summary>
  /// 读取并校验本院报告排除时长。
  /// </summary>
  /// <remarks>只接受非负整数小时并允许零；参数不存在、取值非法或参数服务读取失败一律整次失败，不使用兜底默认值。</remarks>
  /// <returns>排除时长小时数。</returns>
  /// <exception cref="InvalidOperationException">参数不存在、返回值非法或参数服务读取失败时抛出。</exception>
  private async Task<int> ResolveOwnHospitalExcludeHoursAsync()
  {
    string? hoursValue = await ReadSystemParameterValueAsync(OwnHospitalExcludeHours);
    if (hoursValue is null) throw new InvalidOperationException("业务拒绝：未配置本院报告排除时长。");
    if (!int.TryParse(hoursValue, out int hours) || hours < 0) throw new InvalidOperationException("业务拒绝：本院报告排除时长取值非法。");

    return hours;
  }

  /// <summary>
  /// 按平台级参数编码读取系统参数值。
  /// </summary>
  /// <remarks>
  /// 读取请求只携带参数编码：该请求类型不承载组织、医院与院区维度，平台级参数的取值不随调用归属变化；
  /// 返回值为空表示参数不存在，读取失败原样向外传播、不降级为兜底值。
  /// </remarks>
  /// <param name="parameterCode">平台系统参数编码，取小写下划线形态。</param>
  /// <returns>参数值；参数不存在时返回 <see langword="null"/>。</returns>
  /// <exception cref="InvalidOperationException">参数服务不可用时由其调用链路抛出，本入口不捕获、不降级。</exception>
  private async Task<string?> ReadSystemParameterValueAsync(string parameterCode)
  {
    SystemParameterDto? parameter = await systemParameterAppService.GetSystemParameterByCodeAsync(new GetSystemParameterByCodeRequest
    {
      Code = parameterCode
    });

    return parameter?.Value;
  }

  /// <summary>
  /// 把本次匹配项组装为按报告归并的匹配响应内容。
  /// </summary>
  /// <remarks>
  /// 报告事实、普通检验结果与检查项目都按本次绑定的报告版本集合各读一次，读取次数只随本批不同的报告版本数量增长、不随报告数与命中项目数增长；
  /// 来源医疗机构名称按本批来源三值经组织路径解析点批量解析后回填，读取次数不随命中项目数增长；
  /// 报告按本次匹配项首次出现的顺序返回，同一份联合报告的多条匹配项共用同一个报告对象。
  /// </remarks>
  /// <param name="matchItems">本次形成的全部匹配项，按拟开项目提交顺序排列。</param>
  /// <param name="standardProjectNames">本次命中项目在标准目录中的展示名称，按标准项目编码索引。</param>
  /// <returns>按报告归并的匹配内容集合。</returns>
  /// <exception cref="InvalidOperationException">本次绑定的报告版本在平台不可读时抛出，此时不返回不完整的匹配响应。</exception>
  private async Task<IReadOnlyList<RecognitionMatchReportReadModel>> AssembleMatchReportsAsync(
    IReadOnlyList<RecognitionMatchItem> matchItems, IReadOnlyDictionary<string, string> standardProjectNames)
  {
    Dictionary<Guid, RecognitionMatchReportFactsItem> reportFacts = await GetReportFactsByVersionAsync(matchItems);
    IReadOnlyList<Guid> reportVersionIds = [.. matchItems.Select(item => item.ReportVersionId).Distinct()];
    Dictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> laboratoryResultsByVersion =
      GroupLaboratoryResultsByVersion(await queryReportRepository.QueryLaboratoryResultItemsByVersionsAsync(reportVersionIds));
    Dictionary<Guid, IReadOnlyList<ExaminationItemView>> examinationItemsByVersion =
      GroupExaminationItemsByVersion(await queryReportRepository.QueryExaminationItemsByVersionsAsync(reportVersionIds));
    List<Guid> examinationItemIds =
    [
      .. examinationItemsByVersion.Values.SelectMany(items => items).Select(item => item.ItemId)
    ];
    // 检查部位按本次绑定的全部检查项目一次读回：读取次数只随本批检查项目数量增长，不随报告数或命中项目数增长。
    IReadOnlyList<ExaminationSiteView> examinationSites =
      [.. await queryReportRepository.QueryExaminationSitesByItemsAsync(examinationItemIds)];
    Dictionary<Guid, IReadOnlyList<ExaminationSiteView>> examinationSitesByItem = examinationSites
      .GroupBy(site => site.ExaminationItemId)
      .ToDictionary(group => group.Key, group => (IReadOnlyList<ExaminationSiteView>)[.. group]);

    List<Guid> reportIds = [];
    List<OrganizationPathTarget> sourcePaths = [];
    foreach (RecognitionMatchItem matchItem in matchItems)
    {
      if (!reportIds.Contains(matchItem.ReportId)) reportIds.Add(matchItem.ReportId);
    }

    foreach (Guid reportId in reportIds)
    {
      RecognitionMatchReportFactsItem fact = ResolveReportFact(reportFacts, reportId, matchItems);
      sourcePaths.Add(new OrganizationPathTarget(fact.SourceOrganizationCode, fact.SourceHospitalCode, fact.SourceBranchCode));
    }

    IReadOnlyList<OrganizationPath> resolvedPaths = await organizationPathResolver.ResolveOrThrow(
      sourcePaths,
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");
    Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode), OrganizationPath> pathsBySourceKey = [];
    for (int index = 0; index < reportIds.Count; index++)
    {
      OrganizationPath path = resolvedPaths[index];
      pathsBySourceKey[(path.OrganizationCode, path.HospitalCode, path.BranchCode)] = path;
    }

    List<RecognitionMatchReportReadModel> reports = [];
    foreach (Guid reportId in reportIds)
    {
      RecognitionMatchReportFactsItem fact = ResolveReportFact(reportFacts, reportId, matchItems);
      OrganizationPath sourcePath = pathsBySourceKey[(fact.SourceOrganizationCode, fact.SourceHospitalCode, fact.SourceBranchCode)];
      reports.Add(new RecognitionMatchReportReadModel
      {
        ReportId = fact.ReportId,
        ReportVersionId = fact.ReportVersionId,
        ReportType = fact.ReportType,
        ReportNo = fact.ReportNo,
        ReportName = fact.ReportName,
        SourceOrganizationCode = fact.SourceOrganizationCode,
        SourceOrganizationName = sourcePath.OrganizationName,
        SourceHospitalCode = fact.SourceHospitalCode,
        SourceHospitalName = sourcePath.HospitalName,
        SourceBranchCode = fact.SourceBranchCode,
        SourceBranchName = sourcePath.BranchName,
        ClinicalTime = fact.ClinicalTime,
        ReportTime = fact.ReportTime,
        File = MapFileAccess(fact),
        MatchItems = MapMatchItems(
          matchItems, reportId, fact, laboratoryResultsByVersion, examinationItemsByVersion, examinationSitesByItem, standardProjectNames)
      });
    }

    return reports;
  }

  /// <summary>
  /// 按报告版本标识把一次读回的普通检验结果分组。
  /// </summary>
  /// <param name="laboratoryResults">本批绑定版本下的全部普通检验结果。</param>
  /// <returns>报告版本标识到该版本结果的映射；没有结果的版本不在映射内。</returns>
  private static Dictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> GroupLaboratoryResultsByVersion(
    IEnumerable<LaboratoryResultItemView> laboratoryResults) =>
    laboratoryResults
      .GroupBy(result => result.ReportVersionId)
      .ToDictionary(group => group.Key, group => (IReadOnlyList<LaboratoryResultItemView>)[.. group]);

  /// <summary>
  /// 按报告版本标识把一次读回的检查项目分组。
  /// </summary>
  /// <param name="examinationItems">本批绑定版本下的全部检查项目。</param>
  /// <returns>报告版本标识到该版本项目的映射；没有项目的版本不在映射内。</returns>
  private static Dictionary<Guid, IReadOnlyList<ExaminationItemView>> GroupExaminationItemsByVersion(
    IEnumerable<ExaminationItemView> examinationItems) =>
    examinationItems
      .GroupBy(item => item.ReportVersionId)
      .ToDictionary(group => group.Key, group => (IReadOnlyList<ExaminationItemView>)[.. group]);

  /// <summary>
  /// 按本次绑定的报告版本集合一次读回报告事实，并按版本标识建立索引。
  /// </summary>
  /// <param name="matchItems">本次形成的全部匹配项。</param>
  /// <returns>报告版本标识到报告事实的映射。</returns>
  private async Task<Dictionary<Guid, RecognitionMatchReportFactsItem>> GetReportFactsByVersionAsync(IReadOnlyList<RecognitionMatchItem> matchItems)
  {
    IReadOnlyList<Guid> reportVersionIds = [.. matchItems.Select(item => item.ReportVersionId).Distinct()];
    Dictionary<Guid, RecognitionMatchReportFactsItem> factsByVersion = [];
    foreach (RecognitionMatchReportFactsItem fact in await queryReportRepository.QueryRecognitionMatchReportFactsAsync(reportVersionIds))
    {
      factsByVersion[fact.ReportVersionId] = fact;
    }

    return factsByVersion;
  }

  /// <summary>
  /// 取指定报告在本批匹配项中的报告事实行。
  /// </summary>
  /// <param name="reportFacts">按版本标识索引的报告事实。</param>
  /// <param name="reportId">本次要取事实的报告标识。</param>
  /// <param name="matchItems">本次形成的全部匹配项，用于定位该报告绑定的版本。</param>
  /// <returns>该报告的报告事实行。</returns>
  /// <exception cref="InvalidOperationException">该报告绑定的版本在平台不可读时抛出。</exception>
  private static RecognitionMatchReportFactsItem ResolveReportFact(
    Dictionary<Guid, RecognitionMatchReportFactsItem> reportFacts, Guid reportId, IReadOnlyList<RecognitionMatchItem> matchItems)
  {
    Guid reportVersionId = matchItems.First(item => item.ReportId == reportId).ReportVersionId;
    return reportFacts.TryGetValue(reportVersionId, out RecognitionMatchReportFactsItem? fact)
      ? fact
      : throw new InvalidOperationException("业务拒绝：本次匹配绑定的报告版本不可读。");
  }

  /// <summary>
  /// 组装同一份报告下本次命中的项目级匹配内容。
  /// </summary>
  /// <remarks>
  /// 检验报告返回本次命中项目对应的主要检验结果明细，检查报告返回命中项目的检查部位；
  /// 同一份报告的其他未命中项目不成项，因此报告版本下的项目内容按项目编码与匹配项逐一对应；
  /// 项目级内容已在组装前按本批绑定的版本集合一次读回，这里只做内存分组，不再发起数据库往返。
  /// </remarks>
  /// <param name="matchItems">本次形成的全部匹配项。</param>
  /// <param name="reportId">本次组装的报告标识。</param>
  /// <param name="fact">该报告的报告事实行，提供报告版本标识与报告类型。</param>
  /// <param name="laboratoryResultsByVersion">本批绑定版本下的普通检验结果，按报告版本标识索引。</param>
  /// <param name="examinationItemsByVersion">本批绑定版本下的检查项目，按报告版本标识索引。</param>
  /// <param name="examinationSitesByItem">本批绑定版本下的检查部位，按检查项目标识索引。</param>
  /// <param name="standardProjectNames">本次命中项目在标准目录中的展示名称，按标准项目编码索引。</param>
  /// <returns>该报告下本次命中的项目级匹配内容。</returns>
  private static IReadOnlyList<RecognitionMatchItemReadModel> MapMatchItems(
    IReadOnlyList<RecognitionMatchItem> matchItems,
    Guid reportId,
    RecognitionMatchReportFactsItem fact,
    IReadOnlyDictionary<Guid, IReadOnlyList<LaboratoryResultItemView>> laboratoryResultsByVersion,
    IReadOnlyDictionary<Guid, IReadOnlyList<ExaminationItemView>> examinationItemsByVersion,
    IReadOnlyDictionary<Guid, IReadOnlyList<ExaminationSiteView>> examinationSitesByItem,
    IReadOnlyDictionary<string, string> standardProjectNames)
  {
    List<RecognitionMatchItem> reportMatchItems = [.. matchItems.Where(item => item.ReportId == reportId)];
    if (fact.ReportType == MedicalReportType.Laboratory)
    {
      IReadOnlyList<LaboratoryResultItemView> laboratoryResults =
        laboratoryResultsByVersion.TryGetValue(fact.ReportVersionId, out IReadOnlyList<LaboratoryResultItemView>? versionResults)
          ? versionResults
          : [];
      return
      [
        .. reportMatchItems.Select(item => new RecognitionMatchItemReadModel
        {
          RecognitionMatchItemId = item.Id,
          StandardProjectCode = item.StandardProjectCode,
          StandardProjectName = ResolveStandardProjectName(standardProjectNames, item.StandardProjectCode),
          SpecimenTypeName = fact.SpecimenTypeName,
          LaboratoryResults =
          [
            .. laboratoryResults
              .Where(result => string.Equals(result.StandardProjectCode, item.StandardProjectCode, StringComparison.Ordinal))
              .Select(MapLaboratoryResult)
          ],
          ExaminationSites = [],
          OverallAbnormalFlag = fact.OverallAbnormalFlag
        })
      ];
    }

    IReadOnlyList<ExaminationItemView> examinationItems =
      examinationItemsByVersion.TryGetValue(fact.ReportVersionId, out IReadOnlyList<ExaminationItemView>? versionItems)
        ? versionItems
        : [];
    return
    [
      .. reportMatchItems.Select(item => new RecognitionMatchItemReadModel
      {
        RecognitionMatchItemId = item.Id,
        StandardProjectCode = item.StandardProjectCode,
        StandardProjectName = ResolveStandardProjectName(standardProjectNames, item.StandardProjectCode),
        SpecimenTypeName = null,
        LaboratoryResults = [],
        ExaminationSites = MapExaminationSites(item.StandardProjectCode, examinationItems, examinationSitesByItem),
        OverallAbnormalFlag = fact.OverallAbnormalFlag
      })
    ];
  }

  /// <summary>
  /// 取本次命中项目在来源报告中的全部检查部位。
  /// </summary>
  /// <remarks>检查部位归属检查项目，因此先按标准项目编码定位本次命中的检查项目，再按检查项目标识取该项目的部位。</remarks>
  /// <param name="standardProjectCode">本次命中的标准项目编码。</param>
  /// <param name="examinationItems">该报告版本下的全部检查项目。</param>
  /// <param name="examinationSitesByItem">本批绑定版本下的检查部位，按检查项目标识索引。</param>
  /// <returns>该命中项目的检查部位集合；项目没有明确部位时为空集合。</returns>
  private static IReadOnlyList<RecognitionExaminationSiteReadModel> MapExaminationSites(
    string standardProjectCode,
    IReadOnlyList<ExaminationItemView> examinationItems,
    IReadOnlyDictionary<Guid, IReadOnlyList<ExaminationSiteView>> examinationSitesByItem)
  {
    List<Guid> matchedItemIds =
    [
      .. examinationItems
        .Where(item => string.Equals(item.StandardProjectCode, standardProjectCode, StringComparison.Ordinal))
        .Select(item => item.ItemId)
    ];

    return
    [
      .. matchedItemIds
        .Where(examinationSitesByItem.ContainsKey)
        .SelectMany(examinationItemId => examinationSitesByItem[examinationItemId])
        .Select(site => new RecognitionExaminationSiteReadModel
        {
          SiteName = site.SiteName,
          SourceSiteCode = site.SourceSiteCode
        })
    ];
  }

  /// <summary>
  /// 取本次命中项目在标准目录中的展示名称。
  /// </summary>
  /// <remarks>
  /// 名称实时取自本次组织的标准目录互认配置，与来源报告明细上的来源项目名称是两个来源；
  /// 名称缺失时返回空串，不按编码拼接名称。
  /// </remarks>
  /// <param name="standardProjectNames">本次命中项目在标准目录中的展示名称，按标准项目编码索引。</param>
  /// <param name="standardProjectCode">本次命中的标准项目编码。</param>
  /// <returns>标准目录中的项目名称。</returns>
  private static string ResolveStandardProjectName(
    IReadOnlyDictionary<string, string> standardProjectNames, string standardProjectCode) =>
    standardProjectNames.TryGetValue(standardProjectCode, out string? standardProjectName) ? standardProjectName : string.Empty;

  /// <summary>
  /// 把一条普通检验结果映射为匹配响应的检验结果明细。
  /// </summary>
  /// <remarks>异常标志与危急值标志按枚举声明解析为中文文本，只表达来源医院的判断，平台不重新计算。</remarks>
  /// <param name="result">来源报告版本下的一条普通检验结果。</param>
  /// <returns>匹配响应的检验结果明细。</returns>
  private static RecognitionMatchLaboratoryResultReadModel MapLaboratoryResult(LaboratoryResultItemView result) => new()
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
  /// 把报告事实中的文件与影像信息映射为匹配响应的文件入口。
  /// </summary>
  /// <param name="fact">本次报告的报告事实行。</param>
  /// <returns>PDF 与影像入口；检验报告不含来源影像状态与调阅地址。</returns>
  private static PdfAndImageAccessReadModel MapFileAccess(RecognitionMatchReportFactsItem fact) => new()
  {
    PdfFileId = fact.PdfFileId,
    PdfDownloadUrl = BuildPdfDownloadUrl(fact.ReportId, fact.ReportVersionId),
    PdfOriginalFileName = fact.PdfFileName,
    SourceImageStatus = fact.SourceImageStatus,
    ImageAccessUrl = fact.ImageAccessUrl
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
}
