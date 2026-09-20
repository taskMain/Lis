using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;

/// <summary>
/// 互认处理结果提交：整组幂等识别、完整性、时间顺序、决定内容、决定主体与报告版本有效性校验，
/// 读取采纳项目的当前金额后整批原子保存，并把该组处理结果保存时间写入匹配记录。
/// </summary>
/// <remarks>
/// 步骤顺序固定：读取既有处理结果与组内匹配项 → 幂等识别 → 项目数组完整性 → 互认时间边界 → 决定内容与原因
/// → 决定主体标识与名称成对 → 绑定报告版本有效性 → 读取采纳项目当前金额 → 整批写入 → 写入处理结果保存时间 → 登记一次事件。
/// 步骤顺序中的决定主体成对与决定内容的值域判定在公开入口的请求校验之外另行判定，属防御性约束。
/// 幂等命中在任何金额读取与报告版本校验之前返回成功，因此不读金额、不校验版本、不新增记录、不重复统计、不更新保存时间；
/// 未命中幂等时任一校验或写入失败都整批不保存、不登记事件；本类不控制事务生命周期，
/// 用例的事务边界声明在公开应用服务入口的方法上。
/// </remarks>
public partial class MedicalRecognitionReportManager
{
  /// <summary>
  /// 相同业务键下参与比较的事实不一致时的对外拒绝文案。
  /// </summary>
  private const string ProcessingResultConflictMessage = "业务拒绝：互认处理结果冲突，该互认匹配记录已保存不同的处理结果，原决定不可覆盖。";

  /// <summary>
  /// 未配置当前金额的采纳项目所取的预计节省金额；金额缺失不阻碍采纳处理结果保存。
  /// </summary>
  private const decimal MissingCurrentAmount = decimal.Zero;

  /// <summary>
  /// 组级互认科室或互认医生的标识与名称缺失、为空白或不成对时的对外拒绝文案。
  /// </summary>
  private const string DecisionSubjectMessage = "业务拒绝：互认科室与互认医生的标识与名称必须成对提供，不得缺失、空白或只提供其一。";

  /// <summary>
  /// 按互认匹配记录整组提交处理结果：全部匹配项的采纳或不采纳决定整批原子保存，成功后登记一次处理结果已保存事件。
  /// </summary>
  /// <remarks>
  /// 幂等识别先于其他校验：命中完全相同的既有事实时返回成功，不读金额、不校验报告版本有效性、不新增记录、不重复统计、
  /// 处理结果保存时间不变；仅科室与医生名称不同或项目数组顺序不同同样按幂等成功处理。
  /// 未命中幂等时依序校验项目数组完整性、互认时间边界、决定内容与原因、决定主体成对与绑定报告版本有效性；
  /// 采纳项目按接收组织、医院、院区与由匹配项反查得到的标准项目编码读取当前金额，未配置金额按零元形成且不阻碍保存，
  /// 不采纳项目不读取金额；互认配置或标准目录当前停用不阻断保存。
  /// 任一校验或写入失败整批不保存、不登记事件；行数守卫不为 1 即失败。
  /// </remarks>
  /// <param name="command">处理结果提交命令，携带互认匹配记录标识、组级互认时间与决定主体、完整的组内项目决定集合与操作字段。</param>
  /// <returns>整批保存成功或命中幂等时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 互认匹配记录不存在或不属于本次提交的组织、医院与院区，提交的项目集合缺项、重复或夹带其他组的匹配项，
  /// 互认时间早于匹配记录生成时间或晚于本次请求接收时间，采纳时带有不采纳原因，不采纳时未提供原因或选择了其他原因却缺少补充说明，
  /// 互认科室或互认医生的标识与名称不成对，未命中幂等时绑定报告版本已非当前有效版本，
  /// 相同业务键下参与比较的事实不一致，或处理结果与匹配记录的写入影响行数不为 1 时抛出；以上情况都不保存任何项目、不登记事件。
  /// </exception>
  public async Task<bool> SubmitRecognitionProcessingResultsAsync(SubmitRecognitionProcessingResultsCommand command)
  {
    // 幂等识别先于其他校验：先读该组已保存的处理结果，再读组内匹配项用于完整性判定与反查标准项目编码。
    IReadOnlyList<RecognitionProcessingResult> existingResults =
      await repository.QueryRecognitionProcessingResultsByRecordAsync(command.RecognitionMatchRecordId);
    IReadOnlyList<RecognitionMatchItem> matchItems =
      await repository.QueryRecognitionMatchItemsByRecordAsync(command.RecognitionMatchRecordId);

    IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems = SortedSubmittedItems(command);
    Dictionary<Guid, RecognitionMatchItem> matchItemsById = matchItems.ToDictionary(item => item.Id);

    // 命中完全相同的既有事实时返回成功：不读金额、不校验报告版本有效性、不新增记录、不重复统计、保存时间不变。
    if (existingResults.Count > 0)
    {
      string? idempotencyViolation = FindProcessingResultViolation(command, submittedItems, existingResults, matchItemsById);
      if (idempotencyViolation is not null) throw new InvalidOperationException(idempotencyViolation);

      return true;
    }

    string? completenessViolation = FindSubmittedItemsViolation(submittedItems, matchItems, matchItemsById);
    if (completenessViolation is not null) throw new InvalidOperationException(completenessViolation);

    // 匹配记录承载接收三值与匹配生成时间：未命中幂等后才读取，且必须在完整性校验之后，避免对不成立的组发起读取。
    RecognitionMatchRecord matchRecord = await repository.GetRecognitionMatchRecordByIdAsync(command.RecognitionMatchRecordId)
      ?? throw new InvalidOperationException("业务拒绝：互认匹配记录不存在。");
    ValidateMatchRecordOwnership(command, matchRecord);
    ValidateRecognitionTime(command.RecognitionTime, matchRecord, command.ReceivedTime);
    ValidateDecisionContent(submittedItems);
    ValidateDecisionSubjectPairing(command);

    await ValidateReportVersionsAsync(matchItems);
    decimal[] amounts = await ReadAdoptedProjectAmountsAsync(command, submittedItems, matchItemsById);

    return await SaveProcessingResultsAsync(command, submittedItems, amounts);
  }

  /// <summary>
  /// 按匹配项标识升序排列本次提交的项目集合；幂等比对与逐项校验都以该顺序为基准。
  /// </summary>
  /// <param name="command">处理结果提交命令。</param>
  /// <returns>按匹配项标识升序排列的项目集合。</returns>
  private static IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> SortedSubmittedItems(
    SubmitRecognitionProcessingResultsCommand command) =>
    [.. command.ProcessingResults.OrderBy(item => item.RecognitionMatchItemId)];

  /// <summary>
  /// 比对既有处理结果与本次提交，判定幂等命中、字段不一致与项目集合不一致。
  /// </summary>
  /// <remarks>
  /// 一致性比较字段为互认时间、互认科室标识、互认医生标识、各匹配项决定、不采纳原因与补充说明；
  /// 科室与医生名称只保存不参与比较，项目数组顺序不参与比较（两侧都已按匹配项标识升序排列）。
  /// 项目集合不一致与任一参与比较的事实不同都返回冲突，表示该组已保存不同的处理结果。
  /// </remarks>
  /// <param name="command">处理结果提交命令，提供组级互认时间与决定主体。</param>
  /// <param name="submittedItems">本次提交的项目集合，按匹配项标识升序排列。</param>
  /// <param name="existingResults">该组已保存的处理结果，按匹配项标识升序排列。</param>
  /// <param name="matchItemsById">该组匹配项，按匹配项标识索引。</param>
  /// <returns>不一致的说明文案；完全相同（或仅名称、顺序不同）时返回 <see langword="null"/>。</returns>
  private static string? FindProcessingResultViolation(
    SubmitRecognitionProcessingResultsCommand command,
    IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems,
    IReadOnlyList<RecognitionProcessingResult> existingResults,
    IReadOnlyDictionary<Guid, RecognitionMatchItem> matchItemsById)
  {
    if (FindSubmittedItemsViolation(submittedItems, [.. matchItemsById.Values], matchItemsById) is not null)
      return ProcessingResultConflictMessage;

    if (submittedItems.Count != existingResults.Count) return ProcessingResultConflictMessage;
    if (command.RecognitionTime != existingResults[0].RecognitionTime) return ProcessingResultConflictMessage;
    // 组级决定主体与组级互认时间对本次提交的全部匹配项共同适用，因此只与任一既有行比较即可。
    if (!string.Equals(command.RecognitionDeptId, existingResults[0].RecognitionDeptId, StringComparison.Ordinal)) return ProcessingResultConflictMessage;
    if (!string.Equals(command.RecognitionDoctorId, existingResults[0].RecognitionDoctorId, StringComparison.Ordinal)) return ProcessingResultConflictMessage;

    for (int index = 0; index < submittedItems.Count; index++)
    {
      SubmitRecognitionProcessingResultCommandItem submitted = submittedItems[index];
      RecognitionProcessingResult existing = existingResults[index];
      if (existing.RecognitionMatchItemId != submitted.RecognitionMatchItemId) return ProcessingResultConflictMessage;
      if (existing.RecognitionResult != submitted.Result) return ProcessingResultConflictMessage;
      if (existing.NonAdoptionReason != submitted.NonAdoptionReason) return ProcessingResultConflictMessage;
      if (!string.Equals(existing.NonAdoptionDescription, submitted.NonAdoptionDescription, StringComparison.Ordinal)) return ProcessingResultConflictMessage;
    }

    return null;
  }

  /// <summary>
  /// 判定本次提交的项目集合是否完整覆盖该组全部匹配项，且无重复、无其他组的匹配项。
  /// </summary>
  /// <remarks>
  /// 提交项在互认匹配项标识上重复时指出被重复提交的匹配项标识；
  /// 夹带其他组的匹配项与组内缺项同样分别指出相关的互认匹配项标识。
  /// </remarks>
  /// <param name="submittedItems">本次提交的项目集合。</param>
  /// <param name="matchItems">该组全部匹配项。</param>
  /// <param name="matchItemsById">该组匹配项，按匹配项标识索引。</param>
  /// <returns>不成立时的拒绝文案，含相关的互认匹配项标识；成立时返回 <see langword="null"/>。</returns>
  private static string? FindSubmittedItemsViolation(
    IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems,
    IReadOnlyList<RecognitionMatchItem> matchItems,
    IReadOnlyDictionary<Guid, RecognitionMatchItem> matchItemsById)
  {
    Guid[] duplicatedItemIds =
    [
      .. submittedItems
        .GroupBy(item => item.RecognitionMatchItemId)
        .Where(group => group.Count() > 1)
        .Select(group => group.Key)
    ];
    if (duplicatedItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：项目数组重复提交同一互认匹配项", duplicatedItemIds);

    Guid[] foreignItemIds = [.. submittedItems.Select(item => item.RecognitionMatchItemId).Where(id => !matchItemsById.ContainsKey(id))];
    if (foreignItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：项目数组包含不属于本互认匹配记录的互认匹配项", foreignItemIds);

    Guid[] missingItemIds = [.. matchItems.Select(item => item.Id).Where(id => !submittedItems.Any(submitted => submitted.RecognitionMatchItemId == id))];
    if (missingItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：项目数组缺少本互认匹配记录的互认匹配项", missingItemIds);

    return null;
  }

  /// <summary>
  /// 按互认匹配项标识集合构造包含相关标识的拒绝文案；标识按 Guid 自身顺序稳定排列。
  /// </summary>
  /// <param name="reason">拒绝原因，作为文案前缀。</param>
  /// <param name="matchItemIds">相关的互认匹配项标识。</param>
  /// <returns>包含全部相关标识的对外拒绝文案。</returns>
  private static string BuildMatchItemIdsMessage(string reason, IReadOnlyList<Guid> matchItemIds) =>
    $"{reason}：{string.Join('、', matchItemIds.Order())}。";

  /// <summary>
  /// 校验匹配记录的接收三值与本次提交的组织、医院、院区一致。
  /// </summary>
  /// <remarks>归属校验只用于确认记录归属一致性，不构成操作权限判断；不一致时整次失败且不保存任何项目。</remarks>
  /// <param name="command">处理结果提交命令，提供本次提交的接收组织、医院与院区。</param>
  /// <param name="matchRecord">本次提交对应的匹配记录。</param>
  /// <exception cref="InvalidOperationException">接收三值中任一值与匹配记录不一致时抛出。</exception>
  private static void ValidateMatchRecordOwnership(SubmitRecognitionProcessingResultsCommand command, RecognitionMatchRecord matchRecord)
  {
    if (!string.Equals(command.OrganizationCode, matchRecord.ReceiverOrganizationCode, StringComparison.Ordinal))
      throw new InvalidOperationException("业务拒绝：互认匹配记录不属于当前组织。");
    if (!string.Equals(command.HospitalCode, matchRecord.ReceiverHospitalCode, StringComparison.Ordinal))
      throw new InvalidOperationException("业务拒绝：互认匹配记录不属于当前医院。");
    if (!string.Equals(command.BranchCode, matchRecord.ReceiverBranchCode, StringComparison.Ordinal))
      throw new InvalidOperationException("业务拒绝：互认匹配记录不属于当前院区。");
  }

  /// <summary>
  /// 校验互认时间落在匹配记录生成时间与本次请求接收时间之间，两端都包含。
  /// </summary>
  /// <param name="recognitionTime">本次提交的互认时间。</param>
  /// <param name="matchRecord">本次提交对应的匹配记录，提供匹配生成时间。</param>
  /// <param name="receivedTime">本次请求的接收时间，取服务端本地墙上时间，与互认时间同为无时区业务时间。</param>
  /// <exception cref="InvalidOperationException">互认时间早于匹配生成时间或晚于请求接收时间时抛出。</exception>
  private static void ValidateRecognitionTime(DateTime recognitionTime, RecognitionMatchRecord matchRecord, DateTime receivedTime)
  {
    if (recognitionTime < matchRecord.MatchCreatedTime) throw new InvalidOperationException("业务拒绝：互认时间早于互认匹配记录生成时间。");
    if (recognitionTime > receivedTime) throw new InvalidOperationException("业务拒绝：互认时间晚于本次请求接收时间。");
  }

  /// <summary>
  /// 校验每个匹配项的决定与不采纳原因合规。
  /// </summary>
  /// <remarks>
  /// 处理结果值域只包括采纳与不采纳；采纳时不得带不采纳原因，不采纳时必须提供平台统一原因代码，
  /// 选择其他情形确需复查时补充说明必填；平台不审查不采纳的临床理由。
  /// </remarks>
  /// <param name="submittedItems">本次提交的项目集合。</param>
  /// <exception cref="InvalidOperationException">决定不在值域内、采纳带原因、不采纳缺原因或选择了其他原因却缺少补充说明时抛出。</exception>
  private static void ValidateDecisionContent(IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems)
  {
    foreach (SubmitRecognitionProcessingResultCommandItem submittedItem in submittedItems)
    {
      if (submittedItem.Result == RecognitionResult.Adopted)
      {
        if (submittedItem.NonAdoptionReason is not null) throw new InvalidOperationException("业务拒绝：采纳的处理结果不得填写不采纳原因。");
        continue;
      }

      if (submittedItem.Result != RecognitionResult.NotAdopted) throw new InvalidOperationException("业务拒绝：处理结果值域非法。");
      if (submittedItem.NonAdoptionReason is null) throw new InvalidOperationException("业务拒绝：不采纳的处理结果必须填写不采纳原因。");
      if (!Enum.IsDefined(submittedItem.NonAdoptionReason.Value)) throw new InvalidOperationException("业务拒绝：不采纳原因取值非法。");
      if (submittedItem.NonAdoptionReason == RecognitionNonAdoptionReason.OtherReviewRequired && string.IsNullOrWhiteSpace(submittedItem.NonAdoptionDescription))
        throw new InvalidOperationException("业务拒绝：选择其他情形确需复查时必须填写补充说明。");
    }
  }

  /// <summary>
  /// 校验互认科室与互认医生的标识与名称成对提供。
  /// </summary>
  /// <remarks>
  /// 这是请求校验面之外的防御性约束：公开入口的请求校验已按必填与非空白拒绝缺失、空串与纯空白的取值，
  /// 因此在正常业务链路上本判定不可达。管理器仍自行判定，使互认科室与互认医生的标识与名称成对这一业务规则
  /// 不依赖调用方的前置校验；标识与名称任一为空、为空白或只提供其一时整次失败，且不保存任何项目、不登记事件。
  /// 平台不校验互认科室或互认医生是否存在于权限系统、是否已停用或是否属于当前调用医院与院区。
  /// </remarks>
  /// <param name="command">处理结果提交命令，提供组级互认科室与互认医生的标识与名称。</param>
  /// <exception cref="InvalidOperationException">
  /// 互认科室标识、互认科室名称、互认医生标识或互认医生名称任一为空、为空白，或标识与名称不成对时抛出。
  /// </exception>
  private static void ValidateDecisionSubjectPairing(SubmitRecognitionProcessingResultsCommand command)
  {
    if (string.IsNullOrWhiteSpace(command.RecognitionDeptId) || string.IsNullOrWhiteSpace(command.RecognitionDeptName))
      throw new InvalidOperationException(DecisionSubjectMessage);
    if (string.IsNullOrWhiteSpace(command.RecognitionDoctorId) || string.IsNullOrWhiteSpace(command.RecognitionDoctorName))
      throw new InvalidOperationException(DecisionSubjectMessage);
  }

  /// <summary>
  /// 校验本次匹配项绑定的报告版本仍为当前有效版本。
  /// </summary>
  /// <remarks>
  /// 报告已形成后续版本或已作废时该版本不再有效，整次请求失败且不保存任何项目；
  /// 本校验只在未命中幂等时执行，幂等命中不因绑定报告版本后续失效改为失败。
  /// </remarks>
  /// <param name="matchItems">该组全部匹配项，提供各自绑定的报告版本标识。</param>
  /// <returns>全部绑定版本仍为当前有效版本时完成的异步操作。</returns>
  /// <exception cref="InvalidOperationException">任一绑定报告版本已非当前有效版本时抛出，文案指出相关报告版本标识。</exception>
  private async Task ValidateReportVersionsAsync(IReadOnlyList<RecognitionMatchItem> matchItems)
  {
    Guid[] reportVersionIds = [.. matchItems.Select(item => item.ReportVersionId).Distinct()];
    HashSet<Guid> validReportVersionIds = [.. (await queryRepository.QueryValidRecognitionReportVersionIdsAsync(reportVersionIds)).Select(item => item.Id)];
    Guid[] invalidReportVersionIds = [.. reportVersionIds.Where(reportVersionId => !validReportVersionIds.Contains(reportVersionId))];
    if (invalidReportVersionIds.Length > 0)
      throw new InvalidOperationException($"业务拒绝：互认匹配项绑定的报告版本已非当前有效版本：{string.Join('、', invalidReportVersionIds.Order())}。");
  }

  /// <summary>
  /// 读取采纳项目的当前金额，逐项按下标与提交项对应返回。
  /// </summary>
  /// <remarks>
  /// 项目编码由互认匹配项标识反查匹配项取得；金额按接收组织、医院、院区与该项目编码读取，不采纳项目不去读取也不返回金额；
  /// 未配置当前金额时按零元形成，且不因互认配置停用或标准目录不可用而拒绝。
  /// </remarks>
  /// <param name="command">处理结果提交命令，提供可信接收组织、医院与院区。</param>
  /// <param name="submittedItems">本次提交的项目集合，按匹配项标识升序排列。</param>
  /// <param name="matchItemsById">该组匹配项，按匹配项标识索引，用于反查标准项目编码。</param>
  /// <returns>与提交项同下标的金额数组；不采纳项对应位置为零。</returns>
  private async Task<decimal[]> ReadAdoptedProjectAmountsAsync(
    SubmitRecognitionProcessingResultsCommand command,
    IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems,
    IReadOnlyDictionary<Guid, RecognitionMatchItem> matchItemsById)
  {
    decimal[] amounts = new decimal[submittedItems.Count];
    for (int index = 0; index < submittedItems.Count; index++)
    {
      if (submittedItems[index].Result != RecognitionResult.Adopted) continue;

      OrganizationHospitalBranchRecognitionAmount? amount = await repository.GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync(
        command.OrganizationCode, command.HospitalCode, command.BranchCode, matchItemsById[submittedItems[index].RecognitionMatchItemId].StandardProjectCode);
      amounts[index] = amount?.CurrentAmount ?? MissingCurrentAmount;
    }

    return amounts;
  }

  /// <summary>
  /// 逐行写入处理结果并比对影响行数，随后把该组处理结果保存时间写入匹配记录，最后只登记一次处理结果已保存事件。
  /// </summary>
  /// <remarks>
  /// 任一行的写入影响行数不为 1 即失败并整批不保存；处理结果保存时间按主键命中且该列当前为空值时更新 1 行，
  /// 每次成功保存都在命令携带的接收时间上推进该时间，加载既有处理结果的读取发生在本次写入之前；
  /// 事件只承载本次写入的匹配项标识、逐项决定与逐项不采纳原因，以及采纳项目形成的预计节省金额合计。
  /// </remarks>
  /// <param name="command">处理结果提交命令。</param>
  /// <param name="submittedItems">本次提交的项目集合，按匹配项标识升序排列。</param>
  /// <param name="amounts">与提交项同下标的预计节省金额，采纳项为读取到的当前金额、不采纳项为零。</param>
  /// <returns>整批保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">任一行处理结果或匹配记录处理结果保存时间的影响行数不为 1 时抛出，此时不登记事件。</exception>
  private async Task<bool> SaveProcessingResultsAsync(
    SubmitRecognitionProcessingResultsCommand command,
    IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> submittedItems,
    IReadOnlyList<decimal> amounts)
  {
    decimal estimatedSavingAmount = decimal.Zero;
    RecognitionNonAdoptionReason?[] nonAdoptionReasons = new RecognitionNonAdoptionReason?[submittedItems.Count];
    for (int index = 0; index < submittedItems.Count; index++)
    {
      SubmitRecognitionProcessingResultCommandItem submittedItem = submittedItems[index];
      bool adopted = submittedItem.Result == RecognitionResult.Adopted;

      // 处理结果表不保存标准项目编码：项目编码由互认匹配项标识反查匹配项取得，金额按该编码读取。
      RecognitionProcessingResult processingResult = new()
      {
        Id = repository.CreateGuid(),
        RecognitionMatchRecordId = command.RecognitionMatchRecordId,
        RecognitionMatchItemId = submittedItem.RecognitionMatchItemId,
        RecognitionTime = command.RecognitionTime,
        RecognitionResult = submittedItem.Result,
        // 互认科室与医生的标识与名称按请求直接保存，不向权限系统补查、不以调用账号替代。
        RecognitionDeptId = command.RecognitionDeptId,
        RecognitionDeptName = command.RecognitionDeptName,
        RecognitionDoctorId = command.RecognitionDoctorId,
        RecognitionDoctorName = command.RecognitionDoctorName,
        NonAdoptionReason = adopted ? null : submittedItem.NonAdoptionReason,
        NonAdoptionDescription = adopted ? null : submittedItem.NonAdoptionDescription,
        // 不采纳项目不形成预计节省金额，也不读取当前金额。
        EstimatedSavingAmount = adopted ? amounts[index] : null,
        OperId = command.OperId,
        OperTime = command.OperTime
      };
      if (await repository.CreateRecognitionProcessingResultAsync(processingResult) != 1)
        throw new InvalidOperationException("业务拒绝：互认处理结果保存影响的行数异常，未完成处理。");

      if (adopted) estimatedSavingAmount += amounts[index];
      nonAdoptionReasons[index] = processingResult.NonAdoptionReason;
    }

    RecognitionMatchRecord savedTimeRecord = new()
    {
      Id = command.RecognitionMatchRecordId,
      DecisionSavedTime = command.ReceivedTime,
      OperId = command.OperId,
      OperTime = command.OperTime
    };
    if (await repository.UpdateRecognitionMatchRecordDecisionSavedTimeAsync(savedTimeRecord) != 1)
      throw new InvalidOperationException("业务拒绝：处理结果保存时间更新影响的行数异常，未完成处理。");

    // 整批写入完成后只登记一次事件：事件承载本次匹配项标识集合、逐项决定、逐项不采纳原因与金额合计。
    AddEvent(command.CreateRecognitionProcessingResultsSavedEvent(
      [.. submittedItems.Select(item => item.RecognitionMatchItemId)],
      [.. submittedItems.Select(item => item.Result)],
      nonAdoptionReasons,
      estimatedSavingAmount));

    return true;
  }
}
