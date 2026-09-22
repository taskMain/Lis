using ClosedXML.Excel;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 互认统计导出的应用服务实现分片：先以同筛选条件的计数语句校验零记录与十万行上限，
/// 再不带分页地全量读取既有统计语句，把读模型已审字段写入单个工作表并组装文件字节。
/// </summary>
/// <remarks>
/// 导出继承发起时的全部查询条件与业务归属范围，不使用页面分页；导出过程不写入数据库、不登记事件、不留存文件副本。
/// 两个入口都由导出控制器的平台/本院 POST 动作调用，宿主不把导出入口自动暴露为端点（入口以 NonAction 声明排除）。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryAppService
{
  /// <summary>单次导出允许的最大记录数；超过即按业务拒绝并提示缩小查询条件。</summary>
  private const int MaxExportRowCount = 100_000;

  /// <summary>导出文件的统一内容类型；控制器按该类型返回工作簿文件流。</summary>
  private const string ExcelFileFormat = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>未反馈匹配项在处理结果列的展示文案。</summary>
  private const string UnprocessedResultText = "未反馈";

  /// <summary>提醒次数为零时同期互认率不计算，在导出列显示的文案。</summary>
  private const string UncomputedRateText = "未计算";

  /// <summary>
  /// 按当前查询条件生成互认统计导出文件（平台管理员入口，由导出控制器的平台 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 接收组与来源组的范围条件按请求使用并经组织路径解析校验存在、启用与父子归属，任一层为空不附加该层过滤；
  /// 两组组织编码同时提供且不一致时整次拒绝。导出继承全部查询条件与汇总维度，不使用页面分页；
  /// 零记录与超过十万行都在读取明细之前按业务拒绝，不生成工作簿。
  /// </remarks>
  /// <param name="request">导出类型、汇总维度、日期范围与全部统计查询条件；不含分页对象。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">导出类型、汇总维度或筛选条件取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">
  /// 当前条件下无可导出数据、记录数超过十万行上限，或范围条件不存在、已停用、父子归属不匹配、两组组织编码不一致时抛出。
  /// </exception>
  [NonAction]
  public async Task<StatisticsExportFileReadModel> GetStatisticsExportAsync(RecognitionStatisticsExportRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    EnsureSourceSideExportDimension(request.ExportType, request.GroupDimension);
    EnsureSameOrganization(request.ReceiverOrganizationCode, request.SourceOrganizationCode);

    // 来源侧导出类型先解析来源组再解析接收组，与来源侧查询入口的解析顺序一致。
    OrganizationPath? receiverScope;
    OrganizationPath? sourceScope;
    if (IsSourceSideExport(request.ExportType))
    {
      sourceScope = await ResolveOptionalScopeAsync(
        request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
        "业务拒绝：来源组织不存在或已停用。",
        "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
        "业务拒绝：来源院区不存在、已停用或不属于所选医院。");
      receiverScope = await ResolveOptionalScopeAsync(
        request.ReceiverOrganizationCode, request.ReceiverHospitalCode, request.ReceiverBranchCode,
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：医院不存在、已停用或不属于所选组织。",
        "业务拒绝：院区不存在、已停用或不属于所选医院。");
    }
    else
    {
      receiverScope = await ResolveOptionalScopeAsync(
        request.ReceiverOrganizationCode, request.ReceiverHospitalCode, request.ReceiverBranchCode,
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：医院不存在、已停用或不属于所选组织。",
        "业务拒绝：院区不存在、已停用或不属于所选医院。");
      sourceScope = await ResolveOptionalScopeAsync(
        request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
        "业务拒绝：来源组织不存在或已停用。",
        "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
        "业务拒绝：来源院区不存在、已停用或不属于所选医院。");
    }

    return await GenerateStatisticsExportAsync(new StatisticsExportCommand(
      request.ExportType,
      request.StartTime,
      request.EndTime,
      request.GroupDimension,
      ScopeFilterCode(receiverScope?.OrganizationCode),
      ScopeFilterCode(receiverScope?.HospitalCode),
      ScopeFilterCode(receiverScope?.BranchCode),
      ScopeFilterCode(sourceScope?.OrganizationCode),
      ScopeFilterCode(sourceScope?.HospitalCode),
      ScopeFilterCode(sourceScope?.BranchCode),
      request.RecognitionDeptId?.Trim(),
      request.RecognitionDoctorId?.Trim(),
      ResolveExportNonAdoptionReason(request.ExportType, request.NonAdoptionReasonCode),
      request.ItemType,
      request.CategoryName?.Trim(),
      request.GroupName?.Trim(),
      request.StandardProjectCode?.Trim()));
  }

  /// <summary>
  /// 按当前查询条件生成本可信范围的互认统计导出文件（医院管理员入口，由导出控制器的本院 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 本侧组织与医院取自可信上下文并按导出类型注入：接收侧导出类型注入接收组织与接收医院，
  /// 来源侧导出类型固定来源组织与来源医院；本侧院区可选，为空按可信医院全院范围导出。
  /// 对侧组的医院与院区取请求并校验属于可信组织；其余校验、上限与文件组装与平台入口共用同一导出核心。
  /// </remarks>
  /// <param name="request">导出类型、汇总维度、日期范围与筛选条件；请求不携带本侧组织与医院字段。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="InvalidOperationException">可信组织或医院不可解析，或范围条件不存在、已停用、不属于可信范围时抛出。</exception>
  [NonAction]
  public async Task<StatisticsExportFileReadModel> GetBranchStatisticsExportAsync(BranchRecognitionStatisticsExportRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    EnsureSourceSideExportDimension(request.ExportType, request.GroupDimension);

    if (IsSourceSideExport(request.ExportType))
    {
      // 来源侧导出类型：来源组织与来源医院固定为可信上下文，本侧来源院区可选；
      // 接收组织恒为可信组织，接收组医院与院区取请求并校验属于可信组织。
      OrganizationPath localSourceScope = await ResolveBranchLocalScopeAsync(trustedScope, request.SourceBranchCode);
      OrganizationPath? receiverScope = await ResolveBranchReceiverScopeAsync(trustedScope, request.ReceiverHospitalCode, request.ReceiverBranchCode);

      return await GenerateStatisticsExportAsync(new StatisticsExportCommand(
        request.ExportType,
        request.StartTime,
        request.EndTime,
        request.GroupDimension,
        ScopeFilterCode(receiverScope?.OrganizationCode),
        ScopeFilterCode(receiverScope?.HospitalCode),
        ScopeFilterCode(receiverScope?.BranchCode),
        localSourceScope.OrganizationCode,
        localSourceScope.HospitalCode,
        request.SourceBranchCode is null ? null : localSourceScope.BranchCode,
        request.RecognitionDeptId?.Trim(),
        request.RecognitionDoctorId?.Trim(),
        ResolveExportNonAdoptionReason(request.ExportType, request.NonAdoptionReasonCode),
        request.ItemType,
        request.CategoryName?.Trim(),
        request.GroupName?.Trim(),
        request.StandardProjectCode?.Trim()));
    }

    // 接收侧导出类型：接收组织与接收医院取可信上下文，本侧接收院区可选；
    // 来源组织恒为可信组织，来源组医院与院区取请求并校验属于可信组织。
    OrganizationPath localScope = await ResolveBranchLocalScopeAsync(trustedScope, request.BranchCode);
    OrganizationPath? sourceScope = await ResolveBranchSourceScopeAsync(trustedScope, request.SourceHospitalCode, request.SourceBranchCode);

    return await GenerateStatisticsExportAsync(new StatisticsExportCommand(
      request.ExportType,
      request.StartTime,
      request.EndTime,
      request.GroupDimension,
      localScope.OrganizationCode,
      localScope.HospitalCode,
      request.BranchCode is null ? null : localScope.BranchCode,
      trustedScope.OrganizationCode,
      ScopeFilterCode(sourceScope?.HospitalCode),
      ScopeFilterCode(sourceScope?.BranchCode),
      request.RecognitionDeptId?.Trim(),
      request.RecognitionDoctorId?.Trim(),
      ResolveExportNonAdoptionReason(request.ExportType, request.NonAdoptionReasonCode),
      request.ItemType,
      request.CategoryName?.Trim(),
      request.GroupName?.Trim(),
      request.StandardProjectCode?.Trim()));
  }

  /// <summary>
  /// 判断导出类型是否为来源侧导出：来源侧汇总与来源侧明细只反映本组织作为来源方的事实。
  /// </summary>
  /// <param name="exportType">请求提交的导出类型。</param>
  /// <returns>来源侧导出类型返回 <see langword="true"/>，接收侧导出类型返回 <see langword="false"/>。</returns>
  private static bool IsSourceSideExport(RecognitionStatisticsExportType exportType) =>
    exportType is RecognitionStatisticsExportType.SourceRecognitionSummary or RecognitionStatisticsExportType.SourceRecognitionDetails;

  /// <summary>
  /// 校验导出请求的汇总维度子集：来源侧导出类型不接受互认科室维度值，进入仓储之前按业务拒绝。
  /// </summary>
  /// <param name="exportType">请求提交的导出类型。</param>
  /// <param name="groupDimension">请求提交的汇总维度。</param>
  /// <exception cref="InvalidOperationException">来源侧导出类型的维度取互认科室值时抛出。</exception>
  private static void EnsureSourceSideExportDimension(RecognitionStatisticsExportType exportType, RecognitionStatisticsGroupDimension groupDimension)
  {
    if (IsSourceSideExport(exportType))
    {
      EnsureSourceGroupDimension(groupDimension);
    }
  }

  /// <summary>
  /// 解析导出请求的不采纳原因代码：只对接收侧明细导出生效，其余导出类型不施加该筛选。
  /// </summary>
  /// <param name="exportType">请求提交的导出类型。</param>
  /// <param name="reasonCode">请求提交的原因代码文本；平台统一原因代码为枚举数值的十进制文本。</param>
  /// <returns>解析后的原因枚举；不生效或输入空白时为 <see langword="null"/>。</returns>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">接收侧明细导出的代码不是数字或不在平台统一原因取值域内时抛出。</exception>
  private static RecognitionNonAdoptionReason? ResolveExportNonAdoptionReason(RecognitionStatisticsExportType exportType, string? reasonCode) =>
    IsSourceSideExport(exportType) || exportType == RecognitionStatisticsExportType.RecognitionUsageSummary
      ? null
      : ParseNonAdoptionReasonCode(reasonCode);

  /// <summary>
  /// 按导出类型分发导出生成：四个接收侧明细类型共用同一读模型列，汇总与来源侧各用自己的语句与列。
  /// </summary>
  /// <param name="command">已解析的导出条件：范围筛选取值、日期边界、汇总维度与明细筛选字段。</param>
  /// <returns>导出文件读模型。</returns>
  /// <exception cref="InvalidOperationException">当前条件下无可导出数据、记录数超过十万行上限，或导出类型不在取值范围内时抛出。</exception>
  private async Task<StatisticsExportFileReadModel> GenerateStatisticsExportAsync(StatisticsExportCommand command) =>
    command.ExportType switch
    {
      RecognitionStatisticsExportType.RecognitionUsageSummary => await ExportUsageSummaryFileAsync(command),
      RecognitionStatisticsExportType.RecognitionReminderDetails or
        RecognitionStatisticsExportType.RecognitionAdoptionDetails or
        RecognitionStatisticsExportType.RecognitionNonAdoptionDetails or
        RecognitionStatisticsExportType.RecognitionReferenceDetails => await ExportUsageDetailFileAsync(command),
      RecognitionStatisticsExportType.SourceRecognitionSummary => await ExportSourceSummaryFileAsync(command),
      RecognitionStatisticsExportType.SourceRecognitionDetails => await ExportSourceDetailFileAsync(command),
      _ => throw new InvalidOperationException("导出类型不在统计导出取值范围内。")
    };

  /// <summary>
  /// 生成接收侧汇总导出文件：先对分组行计数并校验零记录与上限，再全量读取分组行与原因行，
  /// 接收方名称按全部分组行涉及编码批量回填，每个分组行按原因逐行展开、组级字段重复。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>接收侧汇总导出文件读模型。</returns>
  /// <exception cref="InvalidOperationException">当前条件下无可导出数据、记录数超过十万行上限，或外部组织服务不可用时抛出。</exception>
  private async Task<StatisticsExportFileReadModel> ExportUsageSummaryFileAsync(StatisticsExportCommand command)
  {
    RecognitionUsageSummaryFilter filter = BuildUsageSummaryFilter(command);

    // 先计数再全量读取：分组行计数作用于聚合后的分组行，原因行不带分页独立取回。
    long totalCount = await repository.CountRecognitionUsageSummaryGroupsAsync(filter);
    EnsureExportRowCount(totalCount);
    IReadOnlyList<RecognitionUsageSummaryGroupItem> rows =
      [.. await repository.QueryRecognitionUsageSummaryPageAsync(filter, 0, (int)totalCount)];
    Dictionary<ReasonGroupKey, List<RecognitionUsageReasonItem>> reasonsByGroup =
      ClassifyReasonRows(await repository.QueryRecognitionUsageSummaryReasonsAsync(filter));

    // 接收方名称按全部分组行涉及的编码批量解析，读取次数只随组织与医院数量增长，不随行数增长。
    Dictionary<(string, string, string), OrganizationPath> receiverNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(row => (row.ReceiverOrganizationCode, row.ReceiverHospitalCode, row.ReceiverBranchCode))],
      "业务拒绝：汇总行的接收组织不存在或已停用。",
      "业务拒绝：汇总行的接收医院不存在、已停用或不属于该组织。",
      "业务拒绝：汇总行的接收院区不存在、已停用或不属于该医院。");

    IReadOnlyList<RecognitionUsageSummaryReadModel> models =
      [.. rows.Select(row => MapUsageSummaryRow(row, reasonsByGroup, receiverNames, filter))];
    List<object?[]> sheetRows = [UsageSummaryHeaders, .. BuildUsageSummaryRows(models)];
    return CreateExportWorkbook(command, sheetRows);
  }

  /// <summary>
  /// 生成接收侧明细导出文件：按导出类型先计数校验零记录与上限，再以「窗口起点 0、页容量等于计数」全量读取
  /// 对应事实语句，两侧名称按全部行涉及编码批量回填后组装工作簿。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>接收侧明细导出文件读模型。</returns>
  /// <exception cref="InvalidOperationException">当前条件下无可导出数据、记录数超过十万行上限，或外部组织服务不可用时抛出。</exception>
  private async Task<StatisticsExportFileReadModel> ExportUsageDetailFileAsync(StatisticsExportCommand command)
  {
    RecognitionUsageDetailFilter filter = BuildUsageDetailFilter(command);

    // 四类明细各自先计数再全量读取：窗口起点为 0、页容量取计数，同一语句在不截断行集的前提下取回全部记录。
    switch (command.ExportType)
    {
      case RecognitionStatisticsExportType.RecognitionReminderDetails:
      {
        long totalCount = await repository.CountRecognitionUsageReminderDetailsAsync(filter);
        EnsureExportRowCount(totalCount);
        IReadOnlyList<RecognitionReminderDetailItem> rows =
          [.. await repository.QueryRecognitionUsageReminderDetailsAsync(filter, 0, (int)totalCount)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return CreateUsageDetailWorkbook(command, rows, names, item => MapReminderDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionStatisticsExportType.RecognitionAdoptionDetails:
      {
        long totalCount = await repository.CountRecognitionUsageAdoptionDetailsAsync(filter);
        EnsureExportRowCount(totalCount);
        IReadOnlyList<RecognitionAdoptionDetailItem> rows =
          [.. await repository.QueryRecognitionUsageAdoptionDetailsAsync(filter, 0, (int)totalCount)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return CreateUsageDetailWorkbook(command, rows, names, item => MapAdoptionDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionStatisticsExportType.RecognitionNonAdoptionDetails:
      {
        long totalCount = await repository.CountRecognitionUsageNonAdoptionDetailsAsync(filter);
        EnsureExportRowCount(totalCount);
        IReadOnlyList<RecognitionNonAdoptionDetailItem> rows =
          [.. await repository.QueryRecognitionUsageNonAdoptionDetailsAsync(filter, 0, (int)totalCount)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return CreateUsageDetailWorkbook(command, rows, names, item => MapNonAdoptionDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionStatisticsExportType.RecognitionReferenceDetails:
      {
        long totalCount = await repository.CountRecognitionUsageReferenceDetailsAsync(filter);
        EnsureExportRowCount(totalCount);
        IReadOnlyList<RecognitionReferenceDetailItem> rows =
          [.. await repository.QueryRecognitionUsageReferenceDetailsAsync(filter, 0, (int)totalCount)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return CreateUsageDetailWorkbook(command, rows, names, item => MapReferenceDetail(item, names.ReceiverNames, names.SourceNames));
      }

      default:
        throw new InvalidOperationException("导出类型不在接收侧明细取值范围内。");
    }
  }

  /// <summary>
  /// 用全量明细行组装接收侧明细导出工作簿：列头按导出类型补齐类型专属列，行序保持仓储给出的顺序。
  /// </summary>
  /// <typeparam name="TRow">全量明细行投影类型。</typeparam>
  /// <param name="command">已解析的导出条件。</param>
  /// <param name="rows">全量明细行。</param>
  /// <param name="names">接收方与来源方名称字典。</param>
  /// <param name="mapRow">把行投影映射为明细读模型的映射器。</param>
  /// <returns>接收侧明细导出文件读模型。</returns>
  private static StatisticsExportFileReadModel CreateUsageDetailWorkbook<TRow>(
    StatisticsExportCommand command,
    IReadOnlyList<TRow> rows,
    (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names,
    Func<TRow, RecognitionUsageDetailReadModel> mapRow)
  {
    IReadOnlyList<object?[]> sheetRows =
    [
      GetUsageDetailHeaders(command.ExportType),
      .. rows.Select(item => BuildUsageDetailRow(command.ExportType, mapRow(item)))
    ];
    return CreateExportWorkbook(command, sheetRows);
  }

  /// <summary>
  /// 按导出类型组装接收侧明细导出的列头：基础列之后追加采纳金额、不采纳原因或引用事实的类型专属列。
  /// </summary>
  /// <param name="exportType">导出类型。</param>
  /// <returns>与行取值顺序一致的列头集合。</returns>
  private static string[] GetUsageDetailHeaders(RecognitionStatisticsExportType exportType) => exportType switch
  {
    RecognitionStatisticsExportType.RecognitionAdoptionDetails => [.. UsageDetailBaseHeaders, "预计节省金额"],
    RecognitionStatisticsExportType.RecognitionNonAdoptionDetails => [.. UsageDetailBaseHeaders, "不采纳原因代码", "不采纳原因名称", "不采纳补充说明"],
    RecognitionStatisticsExportType.RecognitionReferenceDetails => [.. UsageDetailBaseHeaders, "引用时间", "引用科室ID", "引用科室名称", "引用医生ID", "引用医生名称"],
    _ => UsageDetailBaseHeaders
  };

  /// <summary>
  /// 把已解析的导出条件组装为接收侧汇总筛选：范围取值、日期边界、汇总维度与目录条件。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>接收侧汇总筛选条件。</returns>
  private static RecognitionUsageSummaryFilter BuildUsageSummaryFilter(StatisticsExportCommand command) => new()
  {
    PeriodStart = command.StartTime.ToDateTime(TimeOnly.MinValue),
    PeriodEnd = command.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
    GroupDimension = command.GroupDimension,
    OrganizationCode = command.ReceiverOrganizationCode,
    HospitalCode = command.ReceiverHospitalCode,
    BranchCode = command.ReceiverBranchCode,
    SourceOrganizationCode = command.SourceOrganizationCode,
    SourceHospitalCode = command.SourceHospitalCode,
    SourceBranchCode = command.SourceBranchCode,
    RecognitionDeptId = command.RecognitionDeptId,
    ItemType = command.ItemType,
    CategoryName = command.CategoryName,
    GroupName = command.GroupName,
    StandardProjectCode = command.StandardProjectCode
  };

  /// <summary>
  /// 把已解析的导出条件组装为接收侧明细筛选：范围取值、日期边界与互认科室、互认医生、不采纳原因、目录条件。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>接收侧明细筛选条件。</returns>
  private static RecognitionUsageDetailFilter BuildUsageDetailFilter(StatisticsExportCommand command) => new()
  {
    PeriodStart = command.StartTime.ToDateTime(TimeOnly.MinValue),
    PeriodEnd = command.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
    OrganizationCode = command.ReceiverOrganizationCode,
    HospitalCode = command.ReceiverHospitalCode,
    BranchCode = command.ReceiverBranchCode,
    SourceOrganizationCode = command.SourceOrganizationCode,
    SourceHospitalCode = command.SourceHospitalCode,
    SourceBranchCode = command.SourceBranchCode,
    RecognitionDeptId = command.RecognitionDeptId,
    RecognitionDoctorId = command.RecognitionDoctorId,
    NonAdoptionReason = command.NonAdoptionReason,
    ItemType = command.ItemType,
    CategoryName = command.CategoryName,
    GroupName = command.GroupName,
    StandardProjectCode = command.StandardProjectCode
  };

  /// <summary>
  /// 把不采纳原因行按汇总分组键归并：与汇总分组语句同套的维度取值构成分组键，
  /// 页面查询与导出共用同一装配口径。
  /// </summary>
  /// <param name="reasons">仓储返回的原因行集合。</param>
  /// <returns>按分组键索引的原因行集合。</returns>
  private static Dictionary<ReasonGroupKey, List<RecognitionUsageReasonItem>> ClassifyReasonRows(
    IEnumerable<RecognitionUsageReasonItem> reasons)
  {
    Dictionary<ReasonGroupKey, List<RecognitionUsageReasonItem>> reasonsByGroup = [];
    foreach (RecognitionUsageReasonItem reason in reasons)
    {
      ReasonGroupKey key = new(
        reason.ReceiverOrganizationCode, reason.ReceiverHospitalCode, reason.ReceiverBranchCode,
        reason.RecognitionDeptId, reason.ItemType, reason.StandardProjectCode);
      if (!reasonsByGroup.TryGetValue(key, out List<RecognitionUsageReasonItem>? groupReasons))
      {
        groupReasons = [];
        reasonsByGroup[key] = groupReasons;
      }

      groupReasons.Add(reason);
    }

    return reasonsByGroup;
  }

  /// <summary>
  /// 生成来源侧汇总导出文件：先对分组行计数并校验零记录与上限，再全量读取分组行，
  /// 来源方名称按全部分组行涉及编码批量回填后按页面汇总行同结构导出。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>来源侧汇总导出文件读模型。</returns>
  /// <exception cref="InvalidOperationException">当前条件下无可导出数据、记录数超过十万行上限，或外部组织服务不可用时抛出。</exception>
  private async Task<StatisticsExportFileReadModel> ExportSourceSummaryFileAsync(StatisticsExportCommand command)
  {
    SourceRecognitionSummaryFilter filter = new()
    {
      PeriodStart = command.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = command.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      GroupDimension = command.GroupDimension,
      SourceOrganizationCode = command.SourceOrganizationCode,
      SourceHospitalCode = command.SourceHospitalCode,
      SourceBranchCode = command.SourceBranchCode,
      ReceiverOrganizationCode = command.ReceiverOrganizationCode,
      ReceiverHospitalCode = command.ReceiverHospitalCode,
      ReceiverBranchCode = command.ReceiverBranchCode,
      ItemType = command.ItemType,
      CategoryName = command.CategoryName,
      GroupName = command.GroupName,
      StandardProjectCode = command.StandardProjectCode
    };

    // 先计数再全量读取：分组行计数作用于聚合后的分组行，窗口起点为 0、页容量取计数。
    long totalCount = await repository.CountSourceRecognitionSummaryGroupsAsync(filter);
    EnsureExportRowCount(totalCount);
    IReadOnlyList<SourceRecognitionSummaryGroupItem> rows =
      [.. await repository.QuerySourceRecognitionSummaryPageAsync(filter, 0, (int)totalCount)];

    // 来源方名称按全部分组行涉及的编码批量解析，读取次数只随组织与医院数量增长，不随行数增长。
    Dictionary<(string, string, string), OrganizationPath> sourceNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(row => (row.SourceOrganizationCode, row.SourceHospitalCode, row.SourceBranchCode))],
      "业务拒绝：汇总行的来源组织不存在或已停用。",
      "业务拒绝：汇总行的来源医院不存在、已停用或不属于该组织。",
      "业务拒绝：汇总行的来源院区不存在、已停用或不属于该医院。");

    IReadOnlyList<object?[]> sheetRows =
    [
      SourceSummaryHeaders,
      .. rows.Select(row => BuildSourceSummaryRow(MapSourceSummaryRow(row, sourceNames, filter)))
    ];
    return CreateExportWorkbook(command, sheetRows);
  }

  /// <summary>
  /// 生成来源侧明细导出文件：先计数校验零记录与上限，再全量读取来源侧被互认明细，
  /// 两侧名称按全部行涉及编码批量回填后按匹配项一行展开导出。
  /// </summary>
  /// <param name="command">已解析的导出条件。</param>
  /// <returns>来源侧明细导出文件读模型。</returns>
  /// <exception cref="InvalidOperationException">当前条件下无可导出数据、记录数超过十万行上限，或外部组织服务不可用时抛出。</exception>
  private async Task<StatisticsExportFileReadModel> ExportSourceDetailFileAsync(StatisticsExportCommand command)
  {
    SourceRecognitionDetailFilter filter = new()
    {
      PeriodStart = command.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = command.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      SourceOrganizationCode = command.SourceOrganizationCode,
      SourceHospitalCode = command.SourceHospitalCode,
      SourceBranchCode = command.SourceBranchCode,
      ReceiverOrganizationCode = command.ReceiverOrganizationCode,
      ReceiverHospitalCode = command.ReceiverHospitalCode,
      ReceiverBranchCode = command.ReceiverBranchCode,
      ItemType = command.ItemType,
      CategoryName = command.CategoryName,
      GroupName = command.GroupName,
      StandardProjectCode = command.StandardProjectCode
    };

    // 先计数再全量读取：窗口起点为 0、页容量取计数，同一语句在不截断行集的前提下取回全部记录。
    long totalCount = await repository.CountSourceRecognitionDetailsAsync(filter);
    EnsureExportRowCount(totalCount);
    IReadOnlyList<SourceRecognitionDetailItem> rows =
      [.. await repository.QuerySourceRecognitionDetailsAsync(filter, 0, (int)totalCount)];

    (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
      await ResolveUsageDetailNamesAsync(
        rows,
        item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
        item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));

    IReadOnlyList<object?[]> sheetRows =
    [
      SourceDetailHeaders,
      .. rows.Select(item => BuildSourceDetailRow(MapSourceDetail(item, names.ReceiverNames, names.SourceNames)))
    ];
    return CreateExportWorkbook(command, sheetRows);
  }

  /// <summary>
  /// 来源侧汇总导出的列头：列结构与口径同页面来源侧汇总行。
  /// </summary>
  private static readonly string[] SourceSummaryHeaders =
  [
    "来源组织编码", "来源组织名称", "来源医院编码", "来源医院名称", "来源院区编码", "来源院区名称",
    "项目类型", "标准项目编码", "标准项目名称", "分类名称", "分组名称",
    "统计开始日期", "统计结束日期", "被互认次数"
  ];

  /// <summary>
  /// 把一条来源侧汇总读模型写成导出行：分组维度字段未参与分组时按空值输出。
  /// </summary>
  /// <param name="model">已组装的来源侧汇总行读模型。</param>
  /// <returns>一行导出单元格取值，顺序与列头一致。</returns>
  private static object?[] BuildSourceSummaryRow(SourceRecognitionSummaryReadModel model) =>
  [
    model.SourceOrganizationCode,
    model.SourceOrganizationName,
    model.SourceHospitalCode,
    model.SourceHospitalName,
    model.SourceBranchCode,
    model.SourceBranchName,
    model.ItemTypeText,
    model.StandardProjectCode,
    model.StandardProjectName,
    model.CategoryName,
    model.GroupName,
    model.PeriodStart,
    model.PeriodEnd,
    (double)model.RecognitionCount
  ];

  /// <summary>
  /// 来源侧明细导出的列头：两侧归属、互认科室与医生、互认时间与患者字段。
  /// </summary>
  private static readonly string[] SourceDetailHeaders =
  [
    "互认匹配记录ID", "互认匹配项ID",
    "来源组织编码", "来源组织名称", "来源医院编码", "来源医院名称", "来源院区编码", "来源院区名称",
    "接收组织编码", "接收组织名称", "接收医院编码", "接收医院名称", "接收院区编码", "接收院区名称",
    "标准项目编码",
    "互认科室ID", "互认科室名称", "互认医生ID", "互认医生名称",
    "互认时间", "患者姓名", "证件号码"
  ];

  /// <summary>
  /// 把一条来源侧明细读模型写成导出行：携带互认匹配记录与匹配项标识，两侧归属逐行重复。
  /// </summary>
  /// <param name="model">已组装的来源侧明细读模型。</param>
  /// <returns>一行导出单元格取值，顺序与列头一致。</returns>
  private static object?[] BuildSourceDetailRow(SourceRecognitionDetailReadModel model) =>
  [
    model.RecognitionMatchRecordId.ToString(),
    model.RecognitionMatchItemId.ToString(),
    model.SourceOrganizationCode,
    model.SourceOrganizationName,
    model.SourceHospitalCode,
    model.SourceHospitalName,
    model.SourceBranchCode,
    model.SourceBranchName,
    model.ReceiverOrganizationCode,
    model.ReceiverOrganizationName,
    model.ReceiverHospitalCode,
    model.ReceiverHospitalName,
    model.ReceiverBranchCode,
    model.ReceiverBranchName,
    model.StandardProjectCode,
    model.RecognitionDeptId,
    model.RecognitionDeptName,
    model.RecognitionDoctorId,
    model.RecognitionDoctorName,
    model.RecognitionTime,
    model.PatientName,
    model.IdentityDocumentNo
  ];

  /// <summary>
  /// 校验导出记录数：零记录与超过上限都在读取明细之前按业务拒绝，不生成工作簿、不返回文件响应。
  /// </summary>
  /// <param name="totalCount">同筛选条件的计数语句结果。</param>
  /// <exception cref="InvalidOperationException">零记录或记录数超过十万行上限时抛出。</exception>
  private static void EnsureExportRowCount(long totalCount)
  {
    if (totalCount == 0)
    {
      throw new InvalidOperationException("业务拒绝：当前条件下无可导出数据。");
    }

    if (totalCount > MaxExportRowCount)
    {
      throw new InvalidOperationException("业务拒绝：导出数据超过十万行上限，请缩小查询条件后重试。");
    }
  }

  /// <summary>
  /// 接收侧明细导出的基础列头：互认匹配记录与匹配项标识、组级归属与项目资料按匹配项一行展开并逐行重复。
  /// </summary>
  private static readonly string[] UsageDetailBaseHeaders =
  [
    "互认匹配记录ID", "互认匹配项ID", "匹配生成时间", "就诊类型", "就诊流水号",
    "来源组织编码", "来源组织名称", "来源医院编码", "来源医院名称", "来源院区编码", "来源院区名称",
    "接收组织编码", "接收组织名称", "接收医院编码", "接收医院名称", "接收院区编码", "接收院区名称",
    "项目类型", "分类名称", "分组名称", "标准项目编码", "标准项目名称",
    "业务时间", "患者姓名", "证件号码",
    "互认科室ID", "互认科室名称", "互认医生ID", "互认医生名称", "处理结果", "互认时间"
  ];

  /// <summary>
  /// 把一条明细读模型写成导出行：组级字段逐行重复，处理结果列对未反馈项显示「未反馈」。
  /// </summary>
  /// <param name="exportType">导出类型；决定基础列之后的类型专属列。</param>
  /// <param name="model">已组装的明细读模型。</param>
  /// <returns>一行导出单元格取值，顺序与列头一致。</returns>
  private static object?[] BuildUsageDetailRow(RecognitionStatisticsExportType exportType, RecognitionUsageDetailReadModel model)
  {
    List<object?> cells =
    [
      model.RecognitionMatchRecordId.ToString(),
      model.RecognitionMatchItemId.ToString(),
      model.MatchCreatedTime,
      model.VisitTypeText,
      model.VisitSerialNo,
      model.Source.OrganizationCode,
      model.Source.OrganizationName,
      model.Source.HospitalCode,
      model.Source.HospitalName,
      model.Source.BranchCode,
      model.Source.BranchName,
      model.Receiver.OrganizationCode,
      model.Receiver.OrganizationName,
      model.Receiver.HospitalCode,
      model.Receiver.HospitalName,
      model.Receiver.BranchCode,
      model.Receiver.BranchName,
      model.Item.ItemTypeText,
      model.Item.CategoryName,
      model.Item.GroupName,
      model.Item.StandardProjectCode,
      model.Item.StandardProjectName,
      model.BusinessTime,
      model.PatientName,
      model.IdentityDocumentNo,
      model.RecognitionDeptId,
      model.RecognitionDeptName,
      model.RecognitionDoctorId,
      model.RecognitionDoctorName,
      model.ProcessingResult?.DecisionText ?? UnprocessedResultText,
      model.ProcessingResult?.RecognitionTime
    ];
    AppendExportTypeTailCells(exportType, model, cells);
    return [.. cells];
  }

  /// <summary>
  /// 按导出类型追加基础列之后的类型专属列取值：采纳随预计节省金额，不采纳随原因代码、名称与补充说明，
  /// 引用随实际引用时间与引用科室、医生。
  /// </summary>
  /// <param name="exportType">导出类型。</param>
  /// <param name="model">已组装的明细读模型。</param>
  /// <param name="cells">正在组装的行取值集合；类型专属列追加在其后。</param>
  /// <exception cref="InvalidOperationException">导出类型不在接收侧明细取值范围内时抛出。</exception>
  private static void AppendExportTypeTailCells(
    RecognitionStatisticsExportType exportType,
    RecognitionUsageDetailReadModel model,
    List<object?> cells)
  {
    switch (exportType)
    {
      case RecognitionStatisticsExportType.RecognitionReminderDetails:
        break;
      case RecognitionStatisticsExportType.RecognitionAdoptionDetails:
        cells.Add((double)(model.ProcessingResult?.EstimatedSavingAmount ?? 0m));
        break;
      case RecognitionStatisticsExportType.RecognitionNonAdoptionDetails:
        cells.Add(model.ProcessingResult?.NonAdoptionReasonCode);
        cells.Add(model.ProcessingResult?.NonAdoptionReasonName);
        cells.Add(model.ProcessingResult?.NonAdoptionSupplementDescription);
        break;
      case RecognitionStatisticsExportType.RecognitionReferenceDetails:
        cells.Add(model.Reference?.ReferenceTime);
        cells.Add(model.Reference?.ReferenceDeptId);
        cells.Add(model.Reference?.ReferenceDeptName);
        cells.Add(model.Reference?.ReferenceDoctorId);
        cells.Add(model.Reference?.ReferenceDoctorName);
        break;
      default:
        throw new InvalidOperationException("导出类型不在接收侧明细取值范围内。");
    }
  }

  /// <summary>
  /// 接收侧汇总导出的列头：列结构与口径同页面汇总行，并附不采纳原因的代码、名称、次数与占比。
  /// </summary>
  private static readonly string[] UsageSummaryHeaders =
  [
    "接收组织编码", "接收组织名称", "接收医院编码", "接收医院名称", "接收院区编码", "接收院区名称",
    "互认科室ID", "互认科室名称", "项目类型", "标准项目编码", "标准项目名称", "分类名称", "分组名称",
    "统计开始日期", "统计结束日期",
    "提醒次数", "采纳次数", "不采纳次数", "引用次数", "同期互认率", "预计节省金额",
    "不采纳原因代码", "不采纳原因名称", "原因次数", "原因占比"
  ];

  /// <summary>
  /// 把汇总读模型按不采纳原因逐行展开：无原因的分组行输出一行、原因列为空值，
  /// 有原因的分组行每个原因输出一行、组级字段逐行重复。
  /// </summary>
  /// <param name="models">已组装的汇总行读模型集合。</param>
  /// <returns>汇总导出数据行集合。</returns>
  private static IReadOnlyList<object?[]> BuildUsageSummaryRows(IReadOnlyList<RecognitionUsageSummaryReadModel> models)
  {
    List<object?[]> rows = [];
    foreach (RecognitionUsageSummaryReadModel model in models)
    {
      if (model.NonAdoptionReasons.Count == 0)
      {
        rows.Add(BuildUsageSummaryRow(model, null));
      }
      else
      {
        rows.AddRange(model.NonAdoptionReasons.Select(reason => BuildUsageSummaryRow(model, reason)));
      }
    }

    return rows;
  }

  /// <summary>
  /// 把一条汇总读模型与所属原因写成导出行：六项指标与金额来自分组行，同期互认率未计算时显示「未计算」，
  /// 原因次数与占比来自行内装配的原因汇总。
  /// </summary>
  /// <param name="model">已组装的汇总行读模型。</param>
  /// <param name="reason">本行展开的不采纳原因汇总；分组行无原因时为 <see langword="null"/>。</param>
  /// <returns>一行导出单元格取值，顺序与列头一致。</returns>
  private static object?[] BuildUsageSummaryRow(RecognitionUsageSummaryReadModel model, NonAdoptionReasonSummaryReadModel? reason) =>
  [
    model.ReceiverOrganizationCode,
    model.ReceiverOrganizationName,
    model.ReceiverHospitalCode,
    model.ReceiverHospitalName,
    model.ReceiverBranchCode,
    model.ReceiverBranchName,
    model.RecognitionDeptId,
    model.RecognitionDeptName,
    model.ItemTypeText,
    model.StandardProjectCode,
    model.StandardProjectName,
    model.CategoryName,
    model.GroupName,
    model.PeriodStart,
    model.PeriodEnd,
    (double)model.ReminderCount,
    (double)model.AdoptionCount,
    (double)model.NonAdoptionCount,
    (double)model.ReferenceCount,
    model.SamePeriodRecognitionRateCalculated ? (double)model.SamePeriodRecognitionRate : UncomputedRateText,
    (double)model.EstimatedSavingAmount,
    reason?.ReasonCode,
    reason?.ReasonName,
    reason is null ? null : (double)reason.Count,
    reason is null ? null : (double)reason.Ratio
  ];

  /// <summary>
  /// 用 ClosedXML 把导出行写入单个工作表并组装文件读模型。
  /// </summary>
  /// <remarks>
  /// 工作表以导出类型中文名称命名，文件名为「导出类型名称-起止日期.xlsx」；
  /// 工作簿只在内存中组装，不写入数据库、不留存文件副本。
  /// </remarks>
  /// <param name="command">已解析的导出条件，提供导出类型名称与起止日期。</param>
  /// <param name="sheetRows">首行为列头、其余为数据行的导出行集合。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  private static StatisticsExportFileReadModel CreateExportWorkbook(StatisticsExportCommand command, IReadOnlyList<object?[]> sheetRows)
  {
    string exportTypeName = ResolveEnumDescription((RecognitionStatisticsExportType?)command.ExportType, RecognitionStatisticsExportTypeDescriptorList.List) ?? string.Empty;
    using XLWorkbook workbook = new();
    IXLWorksheet worksheet = workbook.Worksheets.Add(exportTypeName);
    for (int rowIndex = 0; rowIndex < sheetRows.Count; rowIndex++)
    {
      object?[] cells = sheetRows[rowIndex];
      for (int columnIndex = 0; columnIndex < cells.Length; columnIndex++)
      {
        WriteExportCell(worksheet.Cell(rowIndex + 1, columnIndex + 1), cells[columnIndex]);
      }
    }

    using MemoryStream stream = new();
    workbook.SaveAs(stream);
    return new StatisticsExportFileReadModel
    {
      FileName = $"{exportTypeName}-{command.StartTime:yyyyMMdd}-{command.EndTime:yyyyMMdd}.xlsx",
      FileFormat = ExcelFileFormat,
      FileStream = stream.ToArray()
    };
  }

  /// <summary>
  /// 把导出单元格取值写入工作表单元格：文本、日期与数值按各自类型写入，空值写空文本。
  /// </summary>
  /// <param name="cell">目标单元格。</param>
  /// <param name="value">单元格取值；只支持文本、日期与数值。</param>
  /// <exception cref="InvalidOperationException">取值类型不受支持时就地失败，避免把错误数据静默写进导出文件。</exception>
  private static void WriteExportCell(IXLCell cell, object? value)
  {
    cell.Value = value switch
    {
      null => string.Empty,
      string text => text,
      DateTime time => time,
      double number => number,
      _ => throw new InvalidOperationException($"导出单元格取值类型 {value.GetType().Name} 不受支持。")
    };
  }

  /// <summary>
  /// 导出生成的内部命令：两个入口把各自请求与解析结果归一后的导出条件。
  /// </summary>
  /// <param name="ExportType">导出类型。</param>
  /// <param name="StartTime">统计开始日期；按本地日构成闭区间起点，并进入导出文件名。</param>
  /// <param name="EndTime">统计结束日期；按本地日构成闭区间终点，并进入导出文件名。</param>
  /// <param name="GroupDimension">汇总维度；来源侧导出类型已按维度子集校验。</param>
  /// <param name="ReceiverOrganizationCode">接收组织筛选取值；未提供时为空。</param>
  /// <param name="ReceiverHospitalCode">接收医院筛选取值；未提供时为空。</param>
  /// <param name="ReceiverBranchCode">接收院区筛选取值；未提供时为空。</param>
  /// <param name="SourceOrganizationCode">来源组织筛选取值；未提供时为空。</param>
  /// <param name="SourceHospitalCode">来源医院筛选取值；未提供时为空。</param>
  /// <param name="SourceBranchCode">来源院区筛选取值；未提供时为空。</param>
  /// <param name="RecognitionDeptId">互认科室ID筛选条件。</param>
  /// <param name="RecognitionDoctorId">互认医生ID筛选条件；仅对接收侧明细导出生效。</param>
  /// <param name="NonAdoptionReason">解析后的不采纳原因；仅对接收侧明细导出生效。</param>
  /// <param name="ItemType">项目类型筛选条件。</param>
  /// <param name="CategoryName">标准目录分类名称筛选条件。</param>
  /// <param name="GroupName">标准目录分组名称筛选条件。</param>
  /// <param name="StandardProjectCode">标准项目编码筛选条件。</param>
  private sealed record StatisticsExportCommand(
    RecognitionStatisticsExportType ExportType,
    DateOnly StartTime,
    DateOnly EndTime,
    RecognitionStatisticsGroupDimension GroupDimension,
    string? ReceiverOrganizationCode,
    string? ReceiverHospitalCode,
    string? ReceiverBranchCode,
    string? SourceOrganizationCode,
    string? SourceHospitalCode,
    string? SourceBranchCode,
    string? RecognitionDeptId,
    string? RecognitionDoctorId,
    RecognitionNonAdoptionReason? NonAdoptionReason,
    MedicalItemType? ItemType,
    string? CategoryName,
    string? GroupName,
    string? StandardProjectCode);
}
