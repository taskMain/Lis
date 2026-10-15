using ClosedXML.Excel;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 管理端页面导出的应用服务实现分片：复用既有列表查询取全量行，
/// 把服务端解析的文本与数值写入单个工作表并组装文件字节。
/// </summary>
/// <remarks>
/// 导出继承对应列表查询的条件口径、行集与排序，不使用页面分页；零记录与十万行上限在取回后校验，
/// 防御工作簿组装与响应传输面；导出过程不写入数据库、不登记事件、不留存文件副本。
/// 入口由手工导出控制器的互认项目、平台金额与本院区金额 POST 动作调用，以 NonAction 声明排除框架自动端点暴露。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryAppService
{
  /// <summary>互认项目导出的工作表名与文件名前缀；与页面名称一致。</summary>
  private const string RecognitionProjectExportSheetName = "互认项目";

  /// <summary>互认项目导出的列头：列集与顺序按阶段设计冻结，序号列为导出行序。</summary>
  private static readonly string[] RecognitionProjectExportHeaders =
  [
    "序号", "项目类型", "分类", "分组", "标准项目编码", "标准项目名称", "可互认时间（天）", "互认配置状态", "当前不可用原因"
  ];

  /// <summary>
  /// 按当前查询条件生成互认项目配置导出文件（由导出控制器的互认项目 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 组织范围只信服务端可信当前组织：与对应列表查询共用同一解析点与先后，请求组织不一致即拒绝；
  /// 配置状态参数恒为空，导出同时包含启用与停用的配置，不按页面页签档位裁剪；
  /// 零记录与十万行上限都在取回后校验，不生成工作簿、不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：组织编码必填，关键字可选。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织编码缺失或为空白、关键字已传入但为空白文本时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织、请求组织与可信当前组织不一致、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  [NonAction]
  public async Task<ConfigurationExportFileReadModel> GetRecognitionProjectConfigurationExportAsync(RecognitionProjectConfigurationExportRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    // 组织范围只信服务端可信当前组织：与对应列表查询共用同一解析点，先去除首尾空白再判定，空白组织一律拒绝。
    string currentOrganizationCode = TrustedOrganizationResolver.ResolveOrThrow(HttpRequestInfo?.OrgId, "无法确定有效的当前组织。");
    // 请求组织与可信组织同源，两侧都可能带首尾空白：比较与下传统一用去空白后的可信值，口径与列表查询一致。
    if (!string.Equals(request.OrganizationCode.Trim(), currentOrganizationCode, StringComparison.Ordinal))
    {
      throw new InvalidOperationException("请求组织与当前登录组织不一致，不能导出该组织的互认配置。");
    }

    // 配置状态参数恒为空：导出同时包含启用与停用的配置；关键字非空时原样下传，口径与列表查询一致。
    IReadOnlyList<RecognitionProjectConfigurationReadModel> rows =
      [.. (await repository.QueryRecognitionProjectConfigurationListAsync(currentOrganizationCode, request.Keyword, null)).Select(Map)];
    if (rows.Count == 0)
    {
      throw new InvalidOperationException("业务拒绝：当前条件下无可导出数据。");
    }

    // 上限校验在取回后执行，防御工作簿组装与响应传输面；列表查询自身的不分页读取面由对应列表口径继承。
    if (rows.Count > MaxExportRowCount)
    {
      throw new InvalidOperationException("业务拒绝：导出数据超过十万行上限，请缩小查询条件后重试。");
    }

    return CreateRecognitionProjectExportWorkbook(rows);
  }

  /// <summary>平台金额导出的工作表名与文件名前缀；与页面名称一致。</summary>
  private const string RecognitionAmountExportSheetName = "互认项目金额维护";

  /// <summary>平台金额导出的列头：列集与顺序按阶段设计冻结，序号列为导出行序。</summary>
  private static readonly string[] RecognitionAmountExportHeaders =
  [
    "序号", "项目类型", "分类", "分组", "标准项目编码", "标准项目名称", "当前金额", "金额状态", "互认配置状态", "当前不可用原因"
  ];

  /// <summary>
  /// 按当前查询条件生成平台金额页导出文件（由导出控制器的平台金额 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 组织范围按请求使用：三值经组织路径解析一次性批量校验存在、启用与父子归属，任一层不成立即整次拒绝，
  /// 不要求请求组织等于可信上下文组织，口径与平台金额列表查询一致；
  /// 标准项目编码参数恒为空（导出契约不承载该字段）；关键字先归一再进入查询语句；
  /// 零记录与十万行上限都在取回后校验，不生成工作簿、不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：组织、医院与院区编码必填，关键字可选。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织、医院或院区编码缺失或为空白文本时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">请求的组织、医院或院区不存在、已停用或父子归属不匹配、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  [NonAction]
  public async Task<ConfigurationExportFileReadModel> GetRecognitionAmountExportAsync(RecognitionAmountExportRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    // 组织范围按请求使用并整批校验：与平台金额列表查询共用同一解析点，三值任一不存在、停用或父子不匹配都整次拒绝。
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(request.OrganizationCode, request.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    // 标准项目编码参数恒为空：导出契约不承载该字段；关键字先归一再进入查询语句，空白文本不代表过滤条件。
    string? keyword = NullIfBlank(request.Keyword);
    IReadOnlyList<RecognitionAmountReadModel> rows =
      [.. (await repository.QueryRecognitionAmountListAsync(path.OrganizationCode, path.HospitalCode, path.BranchCode, null, keyword)).Select(item => Map(item, path))];
    if (rows.Count == 0)
    {
      throw new InvalidOperationException("业务拒绝：当前条件下无可导出数据。");
    }

    // 上限校验在取回后执行，防御工作簿组装与响应传输面；列表查询自身的不分页读取面由对应列表口径继承。
    if (rows.Count > MaxExportRowCount)
    {
      throw new InvalidOperationException("业务拒绝：导出数据超过十万行上限，请缩小查询条件后重试。");
    }

    return CreateRecognitionAmountExportWorkbook(rows, RecognitionAmountExportSheetName);
  }

  /// <summary>本院区金额导出的工作表名与文件名前缀；与页面名称一致。</summary>
  private const string BranchRecognitionAmountExportSheetName = "本院区互认项目金额";

  /// <summary>
  /// 按当前查询条件生成本院区金额页导出文件（由导出控制器的本院区金额 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 组织与医院由可信上下文解析（令牌优先、缺失层按登录用户档案补齐、不一致即拒绝、补齐后仍空拒绝），
  /// 解析先后与职责和本院区金额列表查询一致：可信范围解析先于公共请求校验，可信范围不成立时不读取任何业务数据；
  /// 请求院区再经组织路径解析校验存在、启用且属于可信医院，不属于即整次拒绝；
  /// 标准项目编码参数恒为空（导出契约不承载该字段）；关键字先归一再进入查询语句；
  /// 零记录与十万行上限都在取回后校验，不生成工作簿、不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：院区编码必填，关键字可选。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">院区编码缺失或为空白文本时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">可信组织或可信医院无法解析、请求院区不存在、已停用或不属于可信医院、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  [NonAction]
  public async Task<ConfigurationExportFileReadModel> GetBranchRecognitionAmountExportAsync(BranchRecognitionAmountExportRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：与本院区金额列表查询同一先后，可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);

    // 请求院区按请求使用并整批校验：可信组织与医院配合请求院区走同一组织路径解析点，任一层不成立都整次拒绝。
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    // 标准项目编码参数恒为空：导出契约不承载该字段；关键字先归一再进入查询语句，空白文本不代表过滤条件。
    string? keyword = NullIfBlank(request.Keyword);
    IReadOnlyList<RecognitionAmountReadModel> rows =
      [.. (await repository.QueryRecognitionAmountListAsync(path.OrganizationCode, path.HospitalCode, path.BranchCode, null, keyword)).Select(item => Map(item, path))];
    if (rows.Count == 0)
    {
      throw new InvalidOperationException("业务拒绝：当前条件下无可导出数据。");
    }

    // 上限校验在取回后执行，防御工作簿组装与响应传输面；列表查询自身的不分页读取面由对应列表口径继承。
    if (rows.Count > MaxExportRowCount)
    {
      throw new InvalidOperationException("业务拒绝：导出数据超过十万行上限，请缩小查询条件后重试。");
    }

    return CreateRecognitionAmountExportWorkbook(rows, BranchRecognitionAmountExportSheetName);
  }

  /// <summary>
  /// 用 ClosedXML 把金额导出行写入单个工作表并组装文件读模型。
  /// </summary>
  /// <remarks>
  /// 平台金额页与本院区金额页共用同一组装实现，仅工作表名与文件名前缀随页面名称不同；
  /// 工作表以页面名称命名，文件名为「页面名称 + 组装时刻 yyyyMMddHHmmss + .xlsx」；
  /// 表头行加粗、冻结首行、列宽按内容适配；当前金额写数值单元格并声明两位小数格式，
  /// 未配置金额不写值、单元格留空，金额状态按金额是否已配置输出两态文本；
  /// 工作簿只在内存中组装，不写入数据库、不留存文件副本。
  /// </remarks>
  /// <param name="rows">按导出行序排列的金额只读模型集合；已通过零记录与行数上限校验。</param>
  /// <param name="sheetName">工作表名兼文件名前缀；取发起导出的页面名称。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  private static ConfigurationExportFileReadModel CreateRecognitionAmountExportWorkbook(IReadOnlyList<RecognitionAmountReadModel> rows, string sheetName)
  {
    using XLWorkbook workbook = new();
    IXLWorksheet worksheet = workbook.Worksheets.Add(sheetName);
    for (int columnIndex = 0; columnIndex < RecognitionAmountExportHeaders.Length; columnIndex++)
    {
      worksheet.Cell(1, columnIndex + 1).Value = RecognitionAmountExportHeaders[columnIndex];
    }

    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
    {
      RecognitionAmountReadModel row = rows[rowIndex];
      int sheetRowNumber = rowIndex + 2;
      worksheet.Cell(sheetRowNumber, 1).Value = rowIndex + 1;
      // 项目类型取值未登记时文本为 null，单元格输出空，不伪造类型名。
      worksheet.Cell(sheetRowNumber, 2).Value = row.ItemTypeText ?? string.Empty;
      worksheet.Cell(sheetRowNumber, 3).Value = row.CategoryName;
      worksheet.Cell(sheetRowNumber, 4).Value = row.GroupName;
      worksheet.Cell(sheetRowNumber, 5).Value = row.StandardProjectCode;
      worksheet.Cell(sheetRowNumber, 6).Value = row.StandardProjectName;
      // 当前金额写数值单元格并声明两位小数格式；未配置金额不写值，单元格留空，不用零元冒充未配置。
      if (row.IsAmountConfigured)
      {
        IXLCell amountCell = worksheet.Cell(sheetRowNumber, 7);
        amountCell.Value = row.CurrentAmount!.Value;
        amountCell.Style.NumberFormat.Format = "0.00";
      }

      // 金额状态按金额是否已配置派生：已配置的零元与从未配置由此区分。
      worksheet.Cell(sheetRowNumber, 8).Value = row.IsAmountConfigured ? "已配置" : "未配置";
      worksheet.Cell(sheetRowNumber, 9).Value = row.ConfigurationStatusText;
      // 当前不可用原因为空时输出空。
      worksheet.Cell(sheetRowNumber, 10).Value = row.UnavailableReason ?? string.Empty;
    }

    worksheet.Row(1).Style.Font.Bold = true;
    worksheet.SheetView.FreezeRows(1);
    worksheet.Columns(1, RecognitionAmountExportHeaders.Length).AdjustToContents();

    using MemoryStream stream = new();
    workbook.SaveAs(stream);
    return new ConfigurationExportFileReadModel
    {
      FileName = $"{sheetName}{DateTime.Now:yyyyMMddHHmmss}.xlsx",
      FileFormat = ExcelFileFormat,
      FileStream = stream.ToArray()
    };
  }

  /// <summary>
  /// 用 ClosedXML 把互认项目导出行写入单个工作表并组装文件读模型。
  /// </summary>
  /// <remarks>
  /// 工作表以页面名称命名，文件名为「页面名称 + 组装时刻 yyyyMMddHHmmss + .xlsx」；
  /// 表头行加粗、冻结首行、列宽按内容适配；可互认时间列写整数数值并声明整数格式；
  /// 工作簿只在内存中组装，不写入数据库、不留存文件副本。
  /// </remarks>
  /// <param name="rows">按导出行序排列的互认配置只读模型集合；已通过零记录与行数上限校验。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  private static ConfigurationExportFileReadModel CreateRecognitionProjectExportWorkbook(IReadOnlyList<RecognitionProjectConfigurationReadModel> rows)
  {
    using XLWorkbook workbook = new();
    IXLWorksheet worksheet = workbook.Worksheets.Add(RecognitionProjectExportSheetName);
    for (int columnIndex = 0; columnIndex < RecognitionProjectExportHeaders.Length; columnIndex++)
    {
      worksheet.Cell(1, columnIndex + 1).Value = RecognitionProjectExportHeaders[columnIndex];
    }

    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
    {
      RecognitionProjectConfigurationReadModel row = rows[rowIndex];
      int sheetRowNumber = rowIndex + 2;
      worksheet.Cell(sheetRowNumber, 1).Value = rowIndex + 1;
      // 项目类型取值未登记时文本为 null，单元格输出空，不伪造类型名。
      worksheet.Cell(sheetRowNumber, 2).Value = row.ItemTypeText ?? string.Empty;
      worksheet.Cell(sheetRowNumber, 3).Value = row.CategoryName;
      worksheet.Cell(sheetRowNumber, 4).Value = row.GroupName;
      worksheet.Cell(sheetRowNumber, 5).Value = row.StandardProjectCode;
      worksheet.Cell(sheetRowNumber, 6).Value = row.StandardItemName;
      // 可互认时间写数值单元格并声明整数格式，单位为天。
      IXLCell durationCell = worksheet.Cell(sheetRowNumber, 7);
      durationCell.Value = row.RecognitionDurationDays;
      durationCell.Style.NumberFormat.Format = "0";
      worksheet.Cell(sheetRowNumber, 8).Value = row.ConfigurationStatusText;
      // 当前不可用原因为空时输出空。
      worksheet.Cell(sheetRowNumber, 9).Value = row.UnavailableReason ?? string.Empty;
    }

    worksheet.Row(1).Style.Font.Bold = true;
    worksheet.SheetView.FreezeRows(1);
    worksheet.Columns(1, RecognitionProjectExportHeaders.Length).AdjustToContents();

    using MemoryStream stream = new();
    workbook.SaveAs(stream);
    return new ConfigurationExportFileReadModel
    {
      FileName = $"{RecognitionProjectExportSheetName}{DateTime.Now:yyyyMMddHHmmss}.xlsx",
      FileFormat = ExcelFileFormat,
      FileStream = stream.ToArray()
    };
  }
}
