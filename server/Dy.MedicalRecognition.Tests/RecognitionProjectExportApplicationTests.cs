using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 互认项目页导出的应用层行为：可信组织校验与拒绝时机、关键字的校验与原样下传、配置状态参数恒空、
/// 行序保持、零记录与十万行上限的业务拒绝、九列列集与取值、工作簿组装与文件名。
/// </summary>
/// <remarks>
/// 查询端口使用手写替身，不连接数据库；行集与排序的真实 SQL 行为与宿主下载链路分别由真实库批次与宿主批次取证。
/// 可信当前组织通过框架公开工厂注入，与既有查询用例共用 <see cref="TrustedRequestContext"/>。
/// </remarks>
public sealed class RecognitionProjectExportApplicationTests
{
  /// <summary>可信当前组织编码；用例通过框架请求上下文注入该值。</summary>
  private const string TrustedOrganizationCode = "ORG-A";

  /// <summary>与可信当前组织不同的组织编码，用于越权导出用例。</summary>
  private const string OtherOrganizationCode = "ORG-B";

  /// <summary>用例注入的令牌用户标识；导出不读取操作人，只用于构造完整的认证上下文。</summary>
  private const string TrustedOperId = "2f7c1c1e-6a2f-4b1a-9c3d-1f0a2b3c4d5e";

  /// <summary>导出文件的统一内容类型，与应用层写入读模型的文件格式一致。</summary>
  private const string ExcelFileFormat = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>导出工作簿的列头：列集与顺序按阶段设计冻结。</summary>
  private static readonly string[] ExportHeaders =
  [
    "序号", "项目类型", "分类", "分组", "标准项目编码", "标准项目名称", "可互认时间（天）", "互认配置状态", "当前不可用原因"
  ];

  // ---------- 请求校验：契约声明的运行期面 ----------

  /// <summary>组织编码缺失或空白、关键字传入空白文本都按校验拒绝，合法请求通过；口径与列表请求一致。</summary>
  [Fact]
  public void Export_request_validation_rejects_blank_keyword_and_missing_organization()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = "" }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = "  " }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode, Keyword = "   " }));

    Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode });
  }

  // ---------- 范围校验与取数下传 ----------

  /// <summary>请求组织与可信当前组织不一致时按业务拒绝，不进入查询端口、不返回其他组织的数据。</summary>
  [Fact]
  public async Task Export_rejects_request_organization_that_differs_from_trusted_context()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetRecognitionProjectConfigurationExportAsync(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = OtherOrganizationCode }));

    Assert.Contains("不一致", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>可信请求上下文中取不到组织时拒绝，不把导出降级为空集合。</summary>
  [Fact]
  public async Task Export_rejects_when_trusted_organization_is_missing()
  {
    TrustedRequestContext.Use("   ", TrustedOperId);
    FakeRecognitionProjectRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetRecognitionProjectConfigurationExportAsync(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode }));

    Assert.Contains("无法确定有效的当前组织", error.Message, StringComparison.Ordinal);
    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>关键字传入空白文本按校验拒绝（与列表请求同口径），不进入查询端口。</summary>
  [Fact]
  public async Task Export_rejects_blank_keyword_before_any_repository_access()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
      () => service.GetRecognitionProjectConfigurationExportAsync(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode, Keyword = "   " }));

    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>
  /// 请求组织与可信组织同源时按去首尾空白后的可信值下传；关键字非空时原样下传；
  /// 配置状态参数恒为空——导出同时包含启用与停用的配置，不按页面页签档位裁剪。
  /// </summary>
  [Fact]
  public async Task Export_passes_keyword_as_is_and_queries_with_null_status()
  {
    TrustedRequestContext.Use($"  {TrustedOrganizationCode}  ", TrustedOperId);
    FakeRecognitionProjectRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    await service.GetRecognitionProjectConfigurationExportAsync(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode, Keyword = " PROBE-0 " });

    Assert.Equal(1, repository.CallCount);
    Assert.Equal((TrustedOrganizationCode, (string?)" PROBE-0 ", (Domain.Share.Enums.ConfigurationStatus?)null), repository.ReceivedQuery);
  }

  /// <summary>导出行集与行序由查询端口给出，应用层原样保持：不重新排序、不丢弃、不截断。</summary>
  [Fact]
  public async Task Export_preserves_the_row_order_given_by_the_query_port()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new(
    [
      BuildItem("PROBE-003", standardItemName: "第三项"),
      BuildItem("PROBE-001", standardItemName: "第一项"),
      BuildItem("PROBE-002", standardItemName: "第二项")
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    ConfigurationExportFileReadModel file = await service.GetRecognitionProjectConfigurationExportAsync(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode });

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(
      ["PROBE-003", "PROBE-001", "PROBE-002"],
      [.. Enumerable.Range(2, 3).Select(rowNumber => worksheet.Cell(rowNumber, 5).GetString())]);
  }

  // ---------- 防御口径 ----------

  /// <summary>零记录按业务拒绝「当前条件下无可导出数据」，取数发生一次、不生成文件读模型。</summary>
  [Fact]
  public async Task Export_rejects_zero_records_without_generating_a_file()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new([]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetRecognitionProjectConfigurationExportAsync(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode }));

    Assert.Equal("业务拒绝：当前条件下无可导出数据。", error.Message);
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>取回行数超过十万行上限按业务拒绝并提示缩小查询条件；上限校验在取回后执行。</summary>
  [Fact]
  public async Task Export_rejects_rows_beyond_the_limit_with_narrowing_prompt()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new(
      Enumerable.Range(0, 100_001).Select(index => BuildItem($"PROBE-{index:D6}")));
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetRecognitionProjectConfigurationExportAsync(
        new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode }));

    Assert.Contains("十万行上限", error.Message, StringComparison.Ordinal);
    Assert.Contains("缩小查询条件", error.Message, StringComparison.Ordinal);
  }

  /// <summary>取回行数恰为十万行上限时允许导出——上限按「超过才拒绝」解释，正常组装工作簿。</summary>
  [Fact]
  public async Task Export_allows_rows_at_the_limit_boundary()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new(
      Enumerable.Range(0, 100_000).Select(index => BuildItem($"PROBE-{index:D6}")));
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    ConfigurationExportFileReadModel file = await service.GetRecognitionProjectConfigurationExportAsync(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode });

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = Assert.Single(workbook.Worksheets);
    Assert.Equal(100_001, worksheet.LastRowUsed()!.RowNumber());
  }

  // ---------- 列集与取值 ----------

  /// <summary>
  /// 互认项目导出恰九列、列名与顺序符合阶段设计；序号 1..N 连续；项目类型与互认配置状态输出服务端文本，
  /// 项目类型未登记时单元格输出空；可互认时间写整数数值单元格并声明整数格式；
  /// 当前不可用原因为空输出空，配置停用行照常导出。
  /// </summary>
  [Fact]
  public async Task Export_writes_the_nine_frozen_columns_with_server_side_texts()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new(
    [
      BuildItem("PROBE-001"),
      BuildItem("PROBE-002", standardItemName: "胸部CT", itemType: Domain.Share.Enums.MedicalItemType.Examination,
        categoryName: "医学影像", groupName: "CT", recognitionDurationDays: 90, configurationIsValid: false, groupIsValid: false),
      BuildItem("PROBE-003", itemType: (Domain.Share.Enums.MedicalItemType)99)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    ConfigurationExportFileReadModel file = await service.GetRecognitionProjectConfigurationExportAsync(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode });

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(
      ExportHeaders,
      Enumerable.Range(1, 9).Select(column => worksheet.Cell(1, column).GetString()).ToArray());

    Assert.Equal(["1", "检验", "临床检验", "血液学", "PROBE-001", "血常规", "30", "启用", ""],
      [.. Enumerable.Range(1, 9).Select(column => worksheet.Cell(2, column).GetString())]);
    Assert.Equal(["2", "检查", "医学影像", "CT", "PROBE-002", "胸部CT", "90", "停用", "所属分组已停用"],
      [.. Enumerable.Range(1, 9).Select(column => worksheet.Cell(3, column).GetString())]);
    // 项目类型取值未登记时项目类型单元格输出空，不伪造类型名。
    Assert.Equal(["3", "", "临床检验", "血液学", "PROBE-003", "血常规", "30", "启用", ""],
      [.. Enumerable.Range(1, 9).Select(column => worksheet.Cell(4, column).GetString())]);

    // 可互认时间写数值单元格并声明整数格式：序号与天数为数字类型，天数列不带小数位。
    foreach (int rowNumber in new[] { 2, 3, 4 })
    {
      Assert.Equal(XLDataType.Number, worksheet.Cell(rowNumber, 1).DataType);
      Assert.Equal(XLDataType.Number, worksheet.Cell(rowNumber, 7).DataType);
      Assert.Equal("0", worksheet.Cell(rowNumber, 7).Style.NumberFormat.Format);
    }
  }

  /// <summary>
  /// 工作簿组装：单工作表、工作表名与页面名称一致、表头加粗、冻结首行、列宽按内容适配；
  /// 文件名为「互认项目 + 组装时刻 yyyyMMddHHmmss + .xlsx」，文件格式为工作簿内容类型。
  /// </summary>
  [Fact]
  public async Task Export_assembles_a_single_bold_frozen_sheet_with_adapted_columns_and_timestamped_name()
  {
    TrustedRequestContext.Use(TrustedOrganizationCode, TrustedOperId);
    FakeRecognitionProjectRepository repository = new(
    [
      BuildItem("PROBE-001", standardItemName: "血常规"),
      BuildItem("PROBE-002", standardItemName: "胸部CT扫描与影像检查项目名称样例")
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository);

    ConfigurationExportFileReadModel file = await service.GetRecognitionProjectConfigurationExportAsync(
      new RecognitionProjectConfigurationExportRequest { OrganizationCode = TrustedOrganizationCode });

    Assert.Matches(@"^互认项目\d{14}\.xlsx$", file.FileName);
    Assert.Equal(ExcelFileFormat, file.FileFormat);

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = Assert.Single(workbook.Worksheets);
    Assert.Equal("互认项目", worksheet.Name);

    Assert.All(Enumerable.Range(1, 9), column => Assert.True(worksheet.Cell(1, column).Style.Font.Bold));
    Assert.Equal(1, worksheet.SheetView.SplitRow);
    Assert.Equal(0, worksheet.SheetView.SplitColumn);

    // 列宽按内容适配：内容长的列比序号列更宽，九个使用中的列都有按内容计算的宽度。
    double indexColumnWidth = worksheet.Column(1).Width;
    double nameColumnWidth = worksheet.Column(6).Width;
    Assert.True(nameColumnWidth > indexColumnWidth,
      $"标准项目名称列宽 {nameColumnWidth} 应大于序号列宽 {indexColumnWidth}。");
    Assert.All(Enumerable.Range(1, 9), column => Assert.True(worksheet.Column(column).Width > 0));
  }

  /// <summary>构造被测查询应用服务：导出链不读取外部组织服务与系统参数，替身按缺省构造。</summary>
  /// <param name="repository">互认配置查询端口替身。</param>
  /// <returns>被测查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(FakeRecognitionProjectRepository repository)
  {
    return new MedicalRecognitionReportQueryAppService(
      repository, new StubOrganizationAppService(), new StubUserAppService(), new StubSystemParameterAppService());
  }

  /// <summary>构造一条互认配置查询投影，只把用例关心的字段参数化，其余字段取固定值。</summary>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="standardItemName">标准项目名称。</param>
  /// <param name="itemType">项目类型。</param>
  /// <param name="categoryName">所属分类名称。</param>
  /// <param name="groupName">所属分组名称。</param>
  /// <param name="recognitionDurationDays">可互认时间天数。</param>
  /// <param name="configurationIsValid">配置自身是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <returns>查询端口返回的互认配置投影。</returns>
  private static RecognitionProjectConfigurationListItem BuildItem(
    string standardProjectCode,
    string standardItemName = "血常规",
    Domain.Share.Enums.MedicalItemType itemType = Domain.Share.Enums.MedicalItemType.Laboratory,
    string categoryName = "临床检验",
    string groupName = "血液学",
    int recognitionDurationDays = 30,
    bool configurationIsValid = true,
    bool groupIsValid = true) => new()
  {
    ConfigurationId = Guid.NewGuid(),
    StandardProjectCode = standardProjectCode,
    StandardItemName = standardItemName,
    ItemType = itemType,
    CategoryName = categoryName,
    GroupName = groupName,
    RecognitionDurationDays = recognitionDurationDays,
    IsValid = configurationIsValid,
    CategoryIsValid = true,
    GroupIsValid = groupIsValid,
    ItemIsValid = true
  };

  /// <summary>
  /// 互认配置查询端口的手写替身：记录本次导出取数收到的组织、关键字与配置状态参数及调用次数，按设置的集合返回结果。
  /// </summary>
  /// <remarks>集合支持延迟序列：十万行上限用例按惰性序列构造行数，不预先物化。</remarks>
  private sealed class FakeRecognitionProjectRepository : IMedicalRecognitionReportQueryRepository
  {
    /// <summary>
    /// 创建替身并设置取数返回的投影集合。
    /// </summary>
    /// <param name="items">取数返回的互认配置投影集合。</param>
    public FakeRecognitionProjectRepository(IEnumerable<RecognitionProjectConfigurationListItem> items) => Items = items;

    /// <summary>取数返回的互认配置投影集合。</summary>
    public IEnumerable<RecognitionProjectConfigurationListItem> Items { get; set; }

    /// <summary>本次取数收到的组织编码、关键字与配置状态参数；未进入查询端口时为 null。</summary>
    public (string OrganizationCode, string? Keyword, Domain.Share.Enums.ConfigurationStatus? ConfigurationStatus)? ReceivedQuery { get; private set; }

    /// <summary>进入互认配置查询端口的次数。</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(
      string organizationCode, string? keyword, Domain.Share.Enums.ConfigurationStatus? configurationStatus)
    {
      CallCount++;
      ReceivedQuery = (organizationCode, keyword, configurationStatus);
      return Task.FromResult(Items);
    }

    /// <summary>本用例集不覆盖其余查询成员，调用即失败。</summary>
    /// <returns>不返回结果。</returns>
    public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType? itemType) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(Domain.Share.Enums.MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QuerySelectableStandardItemListAsync(string organizationCode, string? keyword) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode, string? keyword) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
      string organizationCode, string hospitalCode, string branchCode, Guid patientId, string identityDocumentTypeCode,
      string identityDocumentNo, Domain.Share.Enums.VisitType visitType, string visitSerialNo, IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
      string organizationCode, string hospitalCode, string branchCode, string identityDocumentTypeCode,
      string identityDocumentNo, Domain.Share.Enums.VisitType visitType, string visitSerialNo) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(Domain.Share.Enums.MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException("本替身只覆盖互认配置列表查询。");
  }
}
