using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;

/// <summary>
/// 互认匹配查询：项目编码整批校验、平台患者解析、候选报告筛选、逐项目选优与非空匹配写入。
/// </summary>
/// <remarks>
/// 全部步骤在单个公开方法内分步串行完成，空匹配在任何写入之前返回成功空结果。
/// 候选报告与匹配响应的报告事实分别经查询侧映射语句读取，领域写入经仓储端口完成；
/// 本类不控制事务生命周期，查询用例的事务边界声明在公开应用服务入口的方法上。
/// </remarks>
public partial class MedicalRecognitionReportManager
{
  /// <summary>
  /// 一小时对应的时间跨度；本院报告排除时长按连续二十四小时计算。
  /// </summary>
  private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

  /// <summary>
  /// 一天对应的时间跨度；可互认时间从报告时间起按连续二十四小时计算。
  /// </summary>
  private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

  /// <summary>
  /// 候选报告与匹配响应报告事实的只读查询端口。
  /// </summary>
  private readonly IMedicalRecognitionReportQueryRepository queryRepository;

  /// <summary>
  /// 初始化管理器：写入端口维护聚合数据，查询端口读取候选报告与匹配响应的报告事实。
  /// </summary>
  /// <param name="repository">互认匹配记录、互认匹配项与其他聚合数据的持久化入口。</param>
  /// <param name="queryRepository">候选报告与匹配响应报告事实的只读查询入口。</param>
  public MedicalRecognitionReportManager(IMedicalRecognitionReportRepository repository, IMedicalRecognitionReportQueryRepository queryRepository)
  {
    this.repository = repository;
    this.queryRepository = queryRepository;
  }

  /// <summary>
  /// 按患者身份与本次来源就诊查询可用的互认匹配，非空匹配时写入一条匹配记录与全部匹配项并登记一次匹配已返回事件。
  /// </summary>
  /// <remarks>
  /// 步骤顺序固定：校验并去重项目编码 → 解析平台患者 → 筛选候选报告 → 逐项目选优 → 确定非空后写入记录与匹配项、登记事件。
  /// 任一项目编码不可用时整次失败且只指出该编码，不创建记录、不返回同批其他匹配、不登记事件；
  /// 患者不存在或没有任何报告命中时返回成功空结果，记录标识与生成时间为空、匹配项集合为空，不创建记录、不登记事件；
  /// 每次非空查询都生成新的记录标识与匹配项标识，不复用上一批结果；
  /// 命中项目的标准项目名称取自本次已读回的互认配置行，随结果一并返回，应用层不再为取名称发起查询。
  /// </remarks>
  /// <param name="command">匹配查询命令，携带可信接收三值、患者证件、本次来源就诊、匹配生成时间、本院报告两个参数与拟开项目集合。</param>
  /// <returns>非空匹配的结果；空匹配时返回 <see cref="RecognitionMatchResult.Empty"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 任一项目编码不存在、未纳入当前组织互认范围、对应配置已停用、标准目录层级不可用或与本次提交的项目类型不一致时抛出，
  /// 异常文案包含该标准项目编码；匹配记录或匹配项写入影响的行数不为 1 时同样抛出，此时不登记事件。
  /// </exception>
  public async Task<RecognitionMatchResult> RequestRecognitionMatchesAsync(QueryRecognitionMatchesCommand command)
  {
    // 空项目数组没有任何可校验的编码，也不产生任何匹配：按成功空结果返回，与"编码有效但没有报告命中"同一语义。
    if (command.ProposedItems.Count == 0) return RecognitionMatchResult.Empty;

    (Dictionary<string, QueryRecognitionMatchesCommandItem> validItems, Dictionary<string, RecognitionProjectConfigurationListItem> configurationsByCode, string? invalidProjectCode) =
      await ValidateProposedItemsAsync(command.ReceiverOrganizationCode, command.ProposedItems);
    if (invalidProjectCode is not null) throw new InvalidOperationException(BuildUnavailableProjectCodeMessage(invalidProjectCode));

    PlatformPatient? patient = await repository.GetPlatformPatientByDocumentAsync(
      NormalizeCode(command.IdentityDocumentTypeCode), NormalizeDocumentNo(command.IdentityDocumentNo));
    if (patient is null) return RecognitionMatchResult.Empty;

    IReadOnlyList<RecognitionMatchCandidateReportItem> candidates =
      await ListCandidatesAsync(command, patient.Id, NormalizeCode(command.IdentityDocumentTypeCode), NormalizeDocumentNo(command.IdentityDocumentNo));

    // 每个互认项目各自按其当前可互认时间判断：可互认时间从报告时间起按连续二十四小时计算，经过时间小于或等于配置值才满足。
    List<(string StandardProjectCode, RecognitionMatchCandidateReportItem Candidate)> selectedCandidates = [];
    foreach ((string standardProjectCode, QueryRecognitionMatchesCommandItem _) in validItems)
    {
      RecognitionMatchCandidateReportItem? candidate = SelectCandidateForProject(
        candidates, standardProjectCode, configurationsByCode[standardProjectCode].RecognitionDurationDays, command);
      if (candidate is not null) selectedCandidates.Add((standardProjectCode, candidate));
    }

    if (selectedCandidates.Count == 0) return RecognitionMatchResult.Empty;

    RecognitionMatchRecord matchRecord = new()
    {
      Id = repository.CreateGuid(),
      ReceiverOrganizationCode = command.ReceiverOrganizationCode,
      ReceiverHospitalCode = command.ReceiverHospitalCode,
      ReceiverBranchCode = command.ReceiverBranchCode,
      IdentityDocumentTypeCode = NormalizeCode(command.IdentityDocumentTypeCode),
      IdentityDocumentNo = NormalizeDocumentNo(command.IdentityDocumentNo),
      VisitType = command.VisitType,
      VisitSerialNo = command.VisitSerialNo,
      MatchCreatedTime = command.MatchCreatedTime,
      // 本次查询只形成匹配事实，处理结果保存时间留空表示该组尚待作出处理决定。
      DecisionSavedTime = null,
      OperId = command.OperId,
      OperTime = command.OperTime
    };
    if (await repository.CreateRecognitionMatchRecordAsync(matchRecord) != 1)
      throw new InvalidOperationException("业务拒绝：互认匹配记录保存影响的行数异常，未完成匹配。");

    List<RecognitionMatchItem> matchItems = [];
    foreach ((string standardProjectCode, RecognitionMatchCandidateReportItem candidate) in selectedCandidates)
    {
      RecognitionMatchItem matchItem = new()
      {
        Id = repository.CreateGuid(),
        RecognitionMatchRecordId = matchRecord.Id,
        ItemType = candidate.ItemType,
        StandardProjectCode = standardProjectCode,
        ReportId = candidate.ReportId,
        ReportVersionId = candidate.ReportVersionId,
        OperId = command.OperId,
        OperTime = command.OperTime
      };
      if (await repository.CreateRecognitionMatchItemAsync(matchItem) != 1)
        throw new InvalidOperationException("业务拒绝：互认匹配项保存影响的行数异常，未完成匹配。");

      matchItems.Add(matchItem);
    }

    // 只登记一次匹配已返回事件：整批匹配项写入完成之后，事件只承载本批的记录标识与匹配项标识集合。
    AddEvent(command.CreateRecognitionMatchesReturnedEvent(matchRecord.Id, [.. matchItems.Select(item => item.Id)]));
    return new RecognitionMatchResult
    {
      RecognitionMatchRecordId = matchRecord.Id,
      MatchCreatedTime = matchRecord.MatchCreatedTime,
      MatchItems = matchItems,
      // 标准项目名称取自本次已读回的互认配置行，不另外发起查询；名称缺失时返回空串，不按编码拼接名称。
      StandardProjectNames = selectedCandidates.ToDictionary(
        selected => selected.StandardProjectCode,
        selected => configurationsByCode[selected.StandardProjectCode].StandardItemName,
        StringComparer.Ordinal)
    };
  }

  /// <summary>
  /// 校验本次提交的项目编码并去重，返回每个有效编码对应的提交项与第一个不可用的编码。
  /// </summary>
  /// <remarks>
  /// 校验按标准项目编码去重后进行：相同编码重复提交只处理一次，重复项不重复读取配置也不重复成项。
  /// 任一项的类型与该项目互认配置归属的标准目录分类不一致时同样按不可用处理，避免按错误类型选到另一侧的候选报告。
  /// 返回值第二个分量非空表示整批校验失败，调用方按整次失败处理。
  /// </remarks>
  /// <param name="organizationCode">可信接收组织编码，决定读取哪个组织的互认配置与标准目录范围。</param>
  /// <param name="proposedItems">本次提交的拟开项目集合。</param>
  /// <returns>
  /// 有效编码到提交项的映射、有效编码到互认配置行的映射（供逐项目取可互认时间），
  /// 以及第一个不可用的标准项目编码（全部可用时为 <see langword="null"/>）。
  /// </returns>
  private async Task<(
    Dictionary<string, QueryRecognitionMatchesCommandItem> ValidItems,
    Dictionary<string, RecognitionProjectConfigurationListItem> ConfigurationsByCode,
    string? InvalidProjectCode)> ValidateProposedItemsAsync(
    string organizationCode, IReadOnlyList<QueryRecognitionMatchesCommandItem> proposedItems)
  {
    // 同一组织的互认配置一次读回并在内存中按编码索引，读取次数不随提交项数增长，也包含已停用的配置。
    Dictionary<string, RecognitionProjectConfigurationListItem> configurationsByCode =
      (await queryRepository.QueryRecognitionProjectConfigurationListAsync(organizationCode, null, null))
        .ToDictionary(configuration => configuration.StandardProjectCode, StringComparer.Ordinal);

    Dictionary<string, QueryRecognitionMatchesCommandItem> validItems = new(StringComparer.Ordinal);
    foreach (QueryRecognitionMatchesCommandItem proposedItem in proposedItems)
    {
      string standardProjectCode = NormalizeCode(proposedItem.StandardProjectCode);
      if (validItems.ContainsKey(standardProjectCode)) continue;

      if (!configurationsByCode.TryGetValue(standardProjectCode, out RecognitionProjectConfigurationListItem? configuration)) return ([], [], standardProjectCode);
      if (!configuration.IsValid || !configuration.CategoryIsValid || !configuration.GroupIsValid || !configuration.ItemIsValid) return ([], [], standardProjectCode);
      if (configuration.ItemType != proposedItem.ItemType) return ([], [], standardProjectCode);

      validItems[standardProjectCode] = proposedItem;
    }

    return (validItems, configurationsByCode, null);
  }

  /// <summary>
  /// 按可信接收三值、已解析平台患者与本次来源就诊读取候选报告。
  /// </summary>
  /// <param name="command">匹配查询命令，提供可信接收三值与本次来源就诊。</param>
  /// <param name="patientId">本次按证件解析到的平台患者标识，候选报告主体必须关联该患者。</param>
  /// <param name="identityDocumentTypeCode">规范形式处理后的证件类型代码。</param>
  /// <param name="identityDocumentNo">规范形式处理后的证件号码。</param>
  /// <returns>候选报告行集合；没有任何候选时为空集合。</returns>
  private async Task<IReadOnlyList<RecognitionMatchCandidateReportItem>> ListCandidatesAsync(
    QueryRecognitionMatchesCommand command, Guid patientId, string identityDocumentTypeCode, string identityDocumentNo) =>
    [.. await queryRepository.QueryRecognitionMatchCandidateReportsAsync(
      command.ReceiverOrganizationCode,
      command.ReceiverHospitalCode,
      command.ReceiverBranchCode,
      patientId,
      identityDocumentTypeCode,
      identityDocumentNo,
      command.VisitType,
      command.VisitSerialNo,
      [.. command.ProposedItems.Select(item => NormalizeCode(item.StandardProjectCode)).Distinct(StringComparer.Ordinal)])];

  /// <summary>
  /// 在一个互认项目下选出唯一一份候选报告。
  /// </summary>
  /// <remarks>
  /// 先按匹配基准时间最晚、再按报告时间最晚、两者均相同时按报告版本标识升序取第一条；
  /// 候选是否参与匹配由匹配截止点、本院报告规则与可互认时间共同决定，全部条件都在取第一条之前完成筛选。
  /// </remarks>
  /// <param name="candidates">本次查询的全部候选行。</param>
  /// <param name="standardProjectCode">本次要选优的标准项目编码。</param>
  /// <param name="recognitionDurationDays">该项目当前配置的可互认时间天数，决定经过时间的允许上限。</param>
  /// <param name="command">匹配查询命令，提供匹配截止点与本院报告两个参数。</param>
  /// <returns>选中的候选；该项目没有任何满足条件的候选时返回 <see langword="null"/>。</returns>
  private static RecognitionMatchCandidateReportItem? SelectCandidateForProject(
    IReadOnlyList<RecognitionMatchCandidateReportItem> candidates,
    string standardProjectCode,
    int recognitionDurationDays,
    QueryRecognitionMatchesCommand command) =>
    candidates
      .Where(candidate => string.Equals(candidate.StandardProjectCode, standardProjectCode, StringComparison.Ordinal))
      .Where(candidate => IsMatchableCandidate(candidate, recognitionDurationDays, command))
      .OrderByDescending(candidate => candidate.MatchBaselineTime)
      .ThenByDescending(candidate => candidate.ReportTime)
      .ThenBy(candidate => candidate.ReportVersionId)
      .FirstOrDefault();

  /// <summary>
  /// 判断一份候选报告是否满足匹配截止点、本院报告规则与可互认时间。
  /// </summary>
  /// <remarks>
  /// 匹配基准时间缺失、匹配基准时间晚于本次查询时点、报告时间晚于本次查询时点的候选都不参与匹配；
  /// 可互认时间从报告时间起按连续二十四小时计算，经过时间小于或等于配置值才满足，边界包含；
  /// 本院报告指来源医院与接收医院相同，判定不区分院区；排除时长大于该项目可互认时间时该项目不返回本院报告，按正常成功处理而不是配置错误。
  /// </remarks>
  /// <param name="candidate">待判定的候选行。</param>
  /// <param name="recognitionDurationDays">该项目当前配置的可互认时间天数。</param>
  /// <param name="command">匹配查询命令，提供接收医院、匹配截止点与本院报告两个参数。</param>
  /// <returns>该候选参与匹配时为 <see langword="true"/>。</returns>
  private static bool IsMatchableCandidate(
    RecognitionMatchCandidateReportItem candidate, int recognitionDurationDays, QueryRecognitionMatchesCommand command)
  {
    if (candidate.MatchBaselineTime is not DateTime matchBaselineTime) return false;
    // 匹配截止点决定本次查询能看到哪些报告：检查或检验时间晚于查询时点的报告不属于本次匹配范围。
    if (matchBaselineTime > command.MatchCreatedTime) return false;
    if (candidate.ReportTime > command.MatchCreatedTime) return false;

    // 可互认时间与本院报告排除时长同源，都从报告时间起算；经过时间超过配置天数即不满足。
    TimeSpan elapsedFromReportTime = command.MatchCreatedTime - candidate.ReportTime;
    if (elapsedFromReportTime > recognitionDurationDays * OneDay) return false;

    bool isOwnHospitalReport = string.Equals(candidate.SourceHospitalCode, command.ReceiverHospitalCode, StringComparison.Ordinal);
    if (!isOwnHospitalReport) return true;
    // 本院报告开关关闭时本院报告整体不参与匹配，不区分院区；本院报告共用的值都取自本次接收三值中的医院编码。
    if (!command.OwnHospitalMatchEnabled) return false;

    return elapsedFromReportTime >= command.OwnHospitalExcludeHours * OneHour;
  }

  /// <summary>
  /// 按标准项目编码构造编码不可用的整次失败文案。
  /// </summary>
  /// <param name="standardProjectCode">不可用的标准项目编码。</param>
  /// <returns>包含该编码的对外拒绝文案。</returns>
  private static string BuildUnavailableProjectCodeMessage(string standardProjectCode) =>
    $"业务拒绝：拟开项目编码 {standardProjectCode} 在当前组织不是可用互认项目。";
}
