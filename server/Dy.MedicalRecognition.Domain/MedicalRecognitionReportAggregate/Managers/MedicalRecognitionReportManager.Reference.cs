using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;

/// <summary>
/// 引用结果提交：按互认匹配项逐项反查所属匹配记录与已保存处理结果，整批校验归属、决定、引用主体与时间序，
/// 识别幂等与冲突后整批原子保存引用事实，并只登记一次引用结果已记录事件。
/// </summary>
/// <remarks>
/// 步骤顺序固定：读取既有引用事实 → 逐项反查匹配项、处理结果与所属匹配记录 → 匹配项存在与反查完整性
/// → 决定为采纳 → 接收三值归属一致 → 引用科室与医生成对 → 实际引用时间边界 → 幂等识别与冲突判定
/// → 整批写入 → 登记一次事件。
/// 幂等命中在幂等识别处直接返回成功，不新增记录、不重复统计、不登记事件；
/// 引用结果有意不校验绑定报告版本在提交时是否仍为当前有效版本，也不受引用详情有效期限制：
/// 报告在引用之后更正或作废不阻止提交该引用事实，已经发生的引用事实不因提交时报告已失效而被拒绝；
/// 本类不控制事务生命周期，用例的事务边界声明在公开应用服务入口的方法上。
/// </remarks>
public partial class MedicalRecognitionReportManager
{
  /// <summary>
  /// 相同互认匹配项下参与比较的事实不一致时的对外拒绝文案。
  /// </summary>
  private const string ReferenceConflictMessage = "业务拒绝：互认引用结果冲突，该互认匹配项已保存不同的引用事实，既有引用事实不可覆盖。";

  /// <summary>
  /// 引用科室或引用医生的标识与名称缺失、为空白或不成对时的对外拒绝文案。
  /// </summary>
  private const string ReferenceSubjectMessage = "业务拒绝：引用科室与引用医生的标识与名称必须成对提供，不得缺失、空白或只提供其一。";

  /// <summary>
  /// 提交实际引用事实：全部引用项目整批校验后原子保存，成功后登记一次引用结果已记录事件。
  /// </summary>
  /// <remarks>
  /// 只使用互认匹配项标识定位，不要求提交匹配记录标识、本次来源就诊或互认时间；一次请求允许包含来自不同匹配记录的项目，
  /// 因此逐项按其自身匹配项反查所属匹配记录与已保存处理结果，再按反查结果整批判定。
  /// 任一项目失败整批不保存、不提供逐条部分结果、不登记事件。
  /// 引用结果不校验绑定报告版本有效性，也不受引用详情有效期限制；引用只形成引用事实，
  /// 不增加采纳次数、来源医院被认次数或预计节省金额，也不更新任何既有行的处理结果保存时间。
  /// </remarks>
  /// <param name="command">引用结果提交命令，携带可信接收三值、实际引用项目集合与操作字段。</param>
  /// <returns>整批保存成功或命中幂等时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 匹配项不存在、该匹配项的处理结果不存在或决定不是采纳、接收三值与本次调用归属不一致、
  /// 引用科室或引用医生的标识与名称不成对、实际引用时间早于已保存的互认时间或晚于本次请求接收时间、
  /// 相同匹配项下参与比较的事实不一致，或引用事实写入影响的行数不为 1 时抛出；
  /// 以上情况都不保存任何引用事实、不登记事件。
  /// </exception>
  public async Task<bool> SubmitRecognitionReferencesAsync(SubmitRecognitionReferencesCommand command)
  {
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems = SortedReferenceItems(command);

    // 既有引用事实、匹配项与处理结果各按标识集合一次读回：读取次数不随提交项数增长。
    // 匹配项的绑定报告版本随匹配项一并读回，但引用结果链不校验它当前是否仍有效，因此报告版本标识不参与后续判定。
    Guid[] matchItemIds = [.. submittedItems.Select(item => item.RecognitionMatchItemId)];
    IReadOnlyList<RecognitionReference> existingReferences =
      await repository.QueryRecognitionReferencesByMatchItemIdsAsync(matchItemIds);
    IReadOnlyList<RecognitionMatchItem> matchItems = await repository.QueryRecognitionMatchItemsByIdsAsync(matchItemIds);
    IReadOnlyList<RecognitionProcessingResult> processingResults =
      await repository.QueryRecognitionProcessingResultsByMatchItemIdsAsync(matchItemIds);

    Dictionary<Guid, RecognitionMatchItem> matchItemsById = matchItems.ToDictionary(item => item.Id);
    Dictionary<Guid, RecognitionProcessingResult> processingResultsByItemId = processingResults.ToDictionary(result => result.RecognitionMatchItemId);

    string? lookupViolation = FindReferenceLookupViolation(submittedItems, matchItemsById, processingResultsByItemId);
    if (lookupViolation is not null) throw new InvalidOperationException(lookupViolation);

    // 接收三值只保存在匹配记录上：按本次提交涉及的全部记录一次读回，读取次数不随提交项数增长。
    Guid[] matchRecordIds = [.. matchItemsById.Values.Select(item => item.RecognitionMatchRecordId).Distinct()];
    Dictionary<Guid, RecognitionMatchRecord> matchRecordsById =
      (await repository.QueryRecognitionMatchRecordsByIdsAsync(matchRecordIds)).ToDictionary(record => record.Id);

    ValidateReferenceOwnership(command, submittedItems, matchItemsById, matchRecordsById);
    ValidateReferenceSubjectPairing(submittedItems);
    ValidateReferencedTime(submittedItems, processingResultsByItemId, command.ReceivedTime);

    // 相同匹配项下参与比较的事实完全一致时按幂等成功返回：不新增记录、不重复统计、不登记事件。
    if (existingReferences.Count > 0)
    {
      string? conflictViolation = FindReferenceViolation(submittedItems, existingReferences);
      if (conflictViolation is not null) throw new InvalidOperationException(conflictViolation);

      return true;
    }

    return await SaveRecognitionReferencesAsync(command, submittedItems);
  }

  /// <summary>
  /// 按互认匹配项标识升序排列本次提交的引用项目；幂等比对与逐项校验都以该顺序为基准。
  /// </summary>
  /// <param name="command">引用结果提交命令。</param>
  /// <returns>按匹配项标识升序排列的引用项目集合。</returns>
  private static IReadOnlyList<SubmitRecognitionReferenceCommandItem> SortedReferenceItems(SubmitRecognitionReferencesCommand command) =>
    [.. command.ReferenceItems.OrderBy(item => item.RecognitionMatchItemId)];

  /// <summary>
  /// 判定每个引用项目是否都能反查到匹配项、所属匹配记录与已保存的处理结果。
  /// </summary>
  /// <remarks>
  /// 匹配项不存在、该匹配项没有处理结果、该匹配项的决定不是采纳，三种情况都整次失败并逐条指出相关匹配项标识；
  /// 文案只描述平台可确认的事实，不泄露其他医院或患者是否存在对应记录。
  /// </remarks>
  /// <param name="submittedItems">本次提交的引用项目集合，按匹配项标识升序排列。</param>
  /// <param name="matchItemsById">本次读回的匹配项，按匹配项标识索引。</param>
  /// <param name="processingResultsByItemId">本次读回的处理结果，按匹配项标识索引。</param>
  /// <returns>不成立时的拒绝文案，含相关匹配项标识；全部反查成功时返回 <see langword="null"/>。</returns>
  private static string? FindReferenceLookupViolation(
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems,
    IReadOnlyDictionary<Guid, RecognitionMatchItem> matchItemsById,
    IReadOnlyDictionary<Guid, RecognitionProcessingResult> processingResultsByItemId)
  {
    Guid[] unknownItemIds = [.. submittedItems.Select(item => item.RecognitionMatchItemId).Where(id => !matchItemsById.ContainsKey(id))];
    if (unknownItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：互认匹配项不存在", unknownItemIds);

    Guid[] missingResultItemIds =
    [
      .. submittedItems
        .Where(item => !processingResultsByItemId.ContainsKey(item.RecognitionMatchItemId))
        .Select(item => item.RecognitionMatchItemId)
    ];
    if (missingResultItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：互认匹配项对应的处理结果不存在", missingResultItemIds);

    Guid[] notAdoptedItemIds =
    [
      .. submittedItems
        .Where(item => processingResultsByItemId[item.RecognitionMatchItemId].RecognitionResult != RecognitionResult.Adopted)
        .Select(item => item.RecognitionMatchItemId)
    ];
    if (notAdoptedItemIds.Length > 0) return BuildMatchItemIdsMessage("业务拒绝：互认匹配项对应的处理结果不是采纳", notAdoptedItemIds);

    return null;
  }

  /// <summary>
  /// 校验每个引用项目所属匹配记录的接收三值与本次调用的可信组织、医院、院区一致。
  /// </summary>
  /// <remarks>
  /// 归属校验只用于确认记录归属一致性，不构成操作权限判断；一条匹配记录不被其他医院的操作提走。
  /// 任一项目不一致都整次失败、不保存任何引用事实。
  /// </remarks>
  /// <param name="command">引用结果提交命令，提供本次调用的可信接收三值。</param>
  /// <param name="submittedItems">本次提交的引用项目集合。</param>
  /// <param name="matchItemsById">本次读回的匹配项，按匹配项标识索引。</param>
  /// <param name="matchRecordsById">本次读回的匹配记录，按记录标识索引。</param>
  /// <exception cref="InvalidOperationException">接收三值中任一值与所属匹配记录不一致时抛出，文案指出相关匹配项标识。</exception>
  private static void ValidateReferenceOwnership(
    SubmitRecognitionReferencesCommand command,
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems,
    IReadOnlyDictionary<Guid, RecognitionMatchItem> matchItemsById,
    IReadOnlyDictionary<Guid, RecognitionMatchRecord> matchRecordsById)
  {
    Guid[] mismatchedItemIds =
    [
      .. submittedItems
        .Where(item => !IsReferenceOwnershipMatched(command, matchItemsById[item.RecognitionMatchItemId], matchRecordsById))
        .Select(item => item.RecognitionMatchItemId)
    ];
    if (mismatchedItemIds.Length > 0)
      throw new InvalidOperationException(BuildMatchItemIdsMessage("业务拒绝：互认匹配项所属匹配记录不属于当前调用归属", mismatchedItemIds));
  }

  /// <summary>
  /// 判定一个引用项目所属匹配记录的接收三值是否与本次调用的可信三值完全一致。
  /// </summary>
  /// <param name="command">引用结果提交命令，提供本次调用的可信接收三值。</param>
  /// <param name="matchItem">该引用项目对应的匹配项，提供所属匹配记录标识。</param>
  /// <param name="matchRecordsById">本次读回的匹配记录，按记录标识索引。</param>
  /// <returns>所属记录存在且接收三值全部一致时为 <see langword="true"/>。</returns>
  private static bool IsReferenceOwnershipMatched(
    SubmitRecognitionReferencesCommand command,
    RecognitionMatchItem matchItem,
    IReadOnlyDictionary<Guid, RecognitionMatchRecord> matchRecordsById)
  {
    if (!matchRecordsById.TryGetValue(matchItem.RecognitionMatchRecordId, out RecognitionMatchRecord? matchRecord)) return false;

    return string.Equals(command.OrganizationCode, matchRecord.ReceiverOrganizationCode, StringComparison.Ordinal)
      && string.Equals(command.HospitalCode, matchRecord.ReceiverHospitalCode, StringComparison.Ordinal)
      && string.Equals(command.BranchCode, matchRecord.ReceiverBranchCode, StringComparison.Ordinal);
  }

  /// <summary>
  /// 校验每个引用项目的引用科室与引用医生的标识与名称成对提供。
  /// </summary>
  /// <remarks>
  /// 这是请求校验面之外的防御性约束：公开入口的请求校验已按必填与非空白逐项拒绝缺失、空串与纯空白的取值。
  /// 管理器仍自行判定，使“引用科室与引用医生的标识与名称成对”这一业务规则不依赖调用方的前置校验；
  /// 平台不校验引用科室或引用医生是否存在于权限系统、是否已停用或是否与调用账号一致。
  /// </remarks>
  /// <param name="submittedItems">本次提交的引用项目集合。</param>
  /// <exception cref="InvalidOperationException">任一项目的引用科室或引用医生标识与名称不成对时抛出，文案指出相关匹配项标识。</exception>
  private static void ValidateReferenceSubjectPairing(IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems)
  {
    Guid[] unpairedItemIds =
    [
      .. submittedItems
        .Where(item => string.IsNullOrWhiteSpace(item.ReferenceDeptId) || string.IsNullOrWhiteSpace(item.ReferenceDeptName)
          || string.IsNullOrWhiteSpace(item.ReferenceDoctorId) || string.IsNullOrWhiteSpace(item.ReferenceDoctorName))
        .Select(item => item.RecognitionMatchItemId)
    ];
    if (unpairedItemIds.Length > 0) throw new InvalidOperationException(BuildMatchItemIdsMessage(ReferenceSubjectMessage, unpairedItemIds));
  }

  /// <summary>
  /// 校验每个项目的实际引用时间落在该组已保存的互认时间与本次请求接收时间之间，两端都包含。
  /// </summary>
  /// <remarks>
  /// 下界取平台已保存的该组互认时间，即医院明确反馈的处理结果时间，不是处理结果保存时间；
  /// 上界取本次请求接收时间，早于下界属业务时间顺序错误，晚于上界属业务上不可能的时间并提示检查系统时钟；
  /// 该校验只比较业务时间，不使用接口接收时间代替互认时间，因此医院延迟提交引用结果不影响判定。
  /// </remarks>
  /// <param name="submittedItems">本次提交的引用项目集合。</param>
  /// <param name="processingResultsByItemId">本次读回的处理结果，按匹配项标识索引，提供该组已保存的互认时间。</param>
  /// <param name="receivedTime">本次请求的接收时间，取服务端本地墙上时间，与实际引用时间同为无时区业务时间。</param>
  /// <exception cref="InvalidOperationException">任一项目的实际引用时间早于互认时间或晚于请求接收时间时抛出，文案指出相关匹配项标识。</exception>
  private static void ValidateReferencedTime(
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems,
    IReadOnlyDictionary<Guid, RecognitionProcessingResult> processingResultsByItemId,
    DateTime receivedTime)
  {
    Guid[] outOfOrderItemIds =
    [
      .. submittedItems
        .Where(item => item.ReferencedTime < processingResultsByItemId[item.RecognitionMatchItemId].RecognitionTime)
        .Select(item => item.RecognitionMatchItemId)
    ];
    if (outOfOrderItemIds.Length > 0)
      throw new InvalidOperationException(BuildMatchItemIdsMessage("业务拒绝：实际引用时间早于该组已保存的互认时间", outOfOrderItemIds));

    Guid[] futureItemIds = [.. submittedItems.Where(item => item.ReferencedTime > receivedTime).Select(item => item.RecognitionMatchItemId)];
    if (futureItemIds.Length > 0)
      throw new InvalidOperationException(BuildMatchItemIdsMessage("业务拒绝：实际引用时间晚于本次请求接收时间，请检查系统时钟", futureItemIds));
  }

  /// <summary>
  /// 比对既有引用事实与本次提交，判定完全相同、字段不一致与既有事实不完整三类结果。
  /// </summary>
  /// <remarks>
  /// 一致性比较字段为匹配项标识、实际引用时间、引用科室标识与引用医生标识；
  /// 引用科室与引用医生名称只保存不参与比较，因此仅名称不同的重试按幂等成功处理。
  /// 既有引用事实数量与本次提交项数不同，或任一匹配项在既有事实中缺失，说明该批并非同一事实的重试，按冲突处理。
  /// </remarks>
  /// <param name="submittedItems">本次提交的引用项目集合，按匹配项标识升序排列。</param>
  /// <param name="existingReferences">本次读回的既有引用事实，按匹配项标识升序排列。</param>
  /// <returns>不一致的说明文案；完全相同（或仅名称不同）时返回 <see langword="null"/>。</returns>
  private static string? FindReferenceViolation(
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems,
    IReadOnlyList<RecognitionReference> existingReferences)
  {
    if (submittedItems.Count != existingReferences.Count) return ReferenceConflictMessage;

    for (int index = 0; index < submittedItems.Count; index++)
    {
      SubmitRecognitionReferenceCommandItem submitted = submittedItems[index];
      RecognitionReference existing = existingReferences[index];
      if (existing.RecognitionMatchItemId != submitted.RecognitionMatchItemId) return ReferenceConflictMessage;
      if (existing.ReferencedTime != submitted.ReferencedTime) return ReferenceConflictMessage;
      if (!string.Equals(existing.ReferenceDeptId, submitted.ReferenceDeptId, StringComparison.Ordinal)) return ReferenceConflictMessage;
      if (!string.Equals(existing.ReferenceDoctorId, submitted.ReferenceDoctorId, StringComparison.Ordinal)) return ReferenceConflictMessage;
    }

    return null;
  }

  /// <summary>
  /// 逐项写入引用事实并比对影响行数，全部写入成功后只登记一次引用结果已记录事件。
  /// </summary>
  /// <remarks>
  /// 任一行的写入影响行数不为 1 即失败并整批不保存；同一匹配项被并发提交时由引用事实表的唯一索引兜底，
  /// 撞键的数据库异常按原样向外传播，本层不识别也不翻译，不自动重试、不改用更新路径；
  /// 事件只承载本次形成引用事实的匹配项标识集合与逐项实际引用事实，不承载采纳与金额口径。
  /// </remarks>
  /// <param name="command">引用结果提交命令。</param>
  /// <param name="submittedItems">本次提交的引用项目集合，按匹配项标识升序排列。</param>
  /// <returns>整批保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">任一行引用事实的影响行数不为 1 时抛出，此时不登记事件。</exception>
  private async Task<bool> SaveRecognitionReferencesAsync(
    SubmitRecognitionReferencesCommand command,
    IReadOnlyList<SubmitRecognitionReferenceCommandItem> submittedItems)
  {
    List<RecognitionReferenceFact> referenceFacts = [];
    foreach (SubmitRecognitionReferenceCommandItem submittedItem in submittedItems)
    {
      // 引用事实表不保存标准项目编码、匹配记录标识与本次来源就诊：它们由互认匹配项标识反查取得。
      RecognitionReference recognitionReference = new()
      {
        Id = repository.CreateGuid(),
        RecognitionMatchItemId = submittedItem.RecognitionMatchItemId,
        ReferencedTime = submittedItem.ReferencedTime,
        // 引用科室与医生的标识与名称按请求直接保存，不向权限系统补查、不以调用账号替代。
        ReferenceDeptId = submittedItem.ReferenceDeptId,
        ReferenceDeptName = submittedItem.ReferenceDeptName,
        ReferenceDoctorId = submittedItem.ReferenceDoctorId,
        ReferenceDoctorName = submittedItem.ReferenceDoctorName,
        OperId = command.OperId,
        OperTime = command.OperTime
      };

      if (await repository.CreateRecognitionReferenceAsync(recognitionReference) != 1)
        throw new InvalidOperationException("业务拒绝：互认引用事实保存影响的行数异常，未完成提交。");

      referenceFacts.Add(new RecognitionReferenceFact
      {
        RecognitionMatchItemId = submittedItem.RecognitionMatchItemId,
        ReferencedTime = submittedItem.ReferencedTime,
        ReferenceDeptId = submittedItem.ReferenceDeptId,
        ReferenceDeptName = submittedItem.ReferenceDeptName,
        ReferenceDoctorId = submittedItem.ReferenceDoctorId,
        ReferenceDoctorName = submittedItem.ReferenceDoctorName
      });
    }

    // 整批写入完成后只登记一次事件：事件承载本次匹配项标识集合与逐项实际引用事实。
    AddEvent(command.CreateRecognitionReferencesRecordedEvent(
      [.. submittedItems.Select(item => item.RecognitionMatchItemId)], referenceFacts));

    return true;
  }
}
