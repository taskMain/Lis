using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 本院区金额页导出的应用层行为：组织与医院由当前 IAM 工作身份范围解析、请求院区经组织路径校验存在启用且属于可信医院、
/// 关键字空白视为未提供与非空原样下传、行序保持、零记录与十万行上限的业务拒绝、十列列集与金额取值、工作簿组装与文件名。
/// </summary>
/// <remarks>
/// 查询端口、外部组织服务与外部用户服务使用手写替身，不连接数据库；行集与排序的真实 SQL 行为与宿主下载链路分别由真实库批次与宿主批次取证。
/// 解析先后与职责和本院区金额列表查询完全一致：可信范围解析先于公共请求校验，可信范围不成立时不读取任何业务数据；
/// 可信范围只来自 <see cref="FakeIamScope"/>，不再安装旧的 TrustedRequestContext。
/// </remarks>
public sealed class BranchRecognitionAmountExportApplicationTests
{
  /// <summary>FakeIamScope 默认链的医共体业务编码；导出组织层只取该值。</summary>
  private const string TrustedOrganizationCode = FakeIamScope.OrganizationCode;

  /// <summary>FakeIamScope 默认链的医院业务编码；导出医院层只取该值。</summary>
  private const string TrustedHospitalCode = FakeIamScope.HospitalCode;

  /// <summary>FakeIamScope 默认链的院区业务编码；请求导出的目标院区。</summary>
  private const string RequestBranchCode = FakeIamScope.BranchCode;

  /// <summary>另一个医共体下医院的实例标识文本；其院区不属于可信医院，用于构造越权院区。</summary>
  private const string OtherHospitalCode = FakeIamScope.OtherHospitalCode;

  /// <summary>另一家医院下的院区实例标识文本；请求该值时按越权拒绝。</summary>
  private const string OtherHospitalBranchCode = FakeIamScope.OtherBranchCode;

  /// <summary>导出文件的统一内容类型，与应用层写入读模型的文件格式一致。</summary>
  private const string ExcelFileFormat = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>导出工作簿的列头：列集与顺序按阶段设计冻结。</summary>
  private static readonly string[] ExportHeaders =
  [
    "序号", "项目类型", "分类", "分组", "标准项目编码", "标准项目名称", "当前金额", "金额状态", "互认配置状态", "当前不可用原因"
  ];

  // ---------- 请求校验：契约声明的运行期面 ----------

  /// <summary>院区编码缺失或空白按校验拒绝；关键字缺失或空白视为未提供、不拒绝；合法请求通过。</summary>
  [Fact]
  public void Export_request_validation_rejects_blank_branch_and_allows_blank_keyword()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
        new BranchRecognitionAmountExportRequest()));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
        new BranchRecognitionAmountExportRequest { BranchCode = "   " }));

    // 关键字缺失或空白都按未提供处理，不触发校验拒绝：口径与本院区金额列表请求一致。
    Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
      new BranchRecognitionAmountExportRequest { BranchCode = RequestBranchCode, Keyword = "   " });
  }

  // ---------- 可信范围解析：两段解析的第一段 ----------

  /// <summary>
  /// 可信范围解析先于公共请求校验：请求院区空白且当前身份缺少医院层时，按可信范围拒绝而不是请求校验拒绝，
  /// 与本院区金额列表查询的先后一致；此时不读取任何业务数据。
  /// </summary>
  [Fact]
  public async Task Export_resolves_the_trusted_scope_before_request_validation()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(
      repository, CreateTrustedOrganizationService(), new StubUserAppService(), FakeIamScope.WithoutHospital());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest(branchCode: "   ")));

    Assert.Equal("无法确定当前可信医院。", error.Message);
    Assert.Equal(0, repository.CallCount);
  }

  /// <summary>
  /// 当前身份的组织或医院范围缺失、或组织已停用时按对应层文案拒绝；不进入组织路径解析与查询端口。
  /// </summary>
  [Fact]
  public async Task Export_rejects_when_the_trusted_scope_is_missing_or_disabled()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    StubOrganizationAppService organizationService = CreateTrustedOrganizationService();

    foreach ((FakeIamScope scope, string expectedMessage) in new[]
    {
      (FakeIamScope.MissingOrganization(), "无法确定当前可信组织。"),
      (FakeIamScope.DisabledOrganization(), "无法确定当前可信组织。"),
      (FakeIamScope.WithoutHospital(), "无法确定当前可信医院。")
    })
    {
      MedicalRecognitionReportQueryAppService service = CreateService(repository, new StubOrganizationAppService(), scope);
      InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.GetBranchRecognitionAmountExportAsync(ExportRequest()));

      Assert.Equal(expectedMessage, error.Message);
      Assert.Equal(0, repository.CallCount);
      Assert.Equal(0, organizationService.OrganizationReadCount);
    }
  }

  /// <summary>
  /// 导出直接使用当前 IAM 工作身份的组织与医院实例标识，不读取用户档案补齐旧令牌层。
  /// </summary>
  [Fact]
  public async Task Export_uses_the_iam_scope_without_reading_the_user_profile()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    StubUserAppService userService = new();
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService(), userService);

    await service.GetBranchRecognitionAmountExportAsync(ExportRequest());

    Assert.Equal(1, repository.CallCount);
    Assert.Equal(
      (FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode, (string?)null, (string?)null),
      repository.ReceivedQuery);
    Assert.Equal(0, userService.GetUserByIdCallCount);
  }

  // ---------- 组织路径校验：两段解析的第二段 ----------

  /// <summary>请求院区不存在、已停用或不属于可信医院时按业务拒绝，不进入查询端口。</summary>
  [Fact]
  public async Task Export_rejects_a_branch_outside_the_trusted_hospital_before_repository_access()
  {
    StubOrganizationAppService organizationService = CreateTrustedOrganizationService();
    // 可信医院下登记一个停用院区；再登记可信组织下另一家医院及其院区，父子归属不成立时同样拒绝。
    organizationService.BranchesByHospital[TrustedHospitalCode].Add(
      new BranchDto { Id = "BRH-DISABLED", Name = "停用院区", HosId = TrustedHospitalCode, OrgId = TrustedOrganizationCode, IsValid = false });
    organizationService.HospitalsByOrganization[TrustedOrganizationCode].Add(
      new HospitalDto { Id = OtherHospitalCode, Name = "另一家医院", OrgId = TrustedOrganizationCode, IsValid = true });
    organizationService.BranchesByHospital[OtherHospitalCode] =
      [new BranchDto { Id = OtherHospitalBranchCode, Name = "另一家医院的院区", HosId = OtherHospitalCode, OrgId = TrustedOrganizationCode, IsValid = true }];
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, organizationService);

    InvalidOperationException missingError = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest(branchCode: "BRH-NOTSEEDED")));
    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", missingError.Message);

    InvalidOperationException disabledError = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest(branchCode: "BRH-DISABLED")));
    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", disabledError.Message);

    InvalidOperationException elsewhereError = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest(branchCode: OtherHospitalBranchCode)));
    Assert.Equal("业务拒绝：院区不存在、已停用或不属于所选医院。", elsewhereError.Message);

    Assert.Equal(0, repository.CallCount);
  }

  // ---------- 关键字下传 ----------

  /// <summary>关键字缺失或去首尾空白后为空都视为未提供：不附加过滤，仓储收到 null；标准项目编码参数恒为空。</summary>
  [Fact]
  public async Task Export_treats_missing_or_blank_keyword_as_not_provided()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    await service.GetBranchRecognitionAmountExportAsync(ExportRequest());
    await service.GetBranchRecognitionAmountExportAsync(ExportRequest(keyword: "   "));

    Assert.Equal(2, repository.CallCount);
    Assert.Null(repository.ReceivedQueries[0].Keyword);
    Assert.Null(repository.ReceivedQueries[1].Keyword);
    Assert.All(repository.ReceivedQueries, query => Assert.Null(query.StandardProjectCode));
  }

  /// <summary>关键字非空时按归一后的值下传：去首尾空白后原样交由仓储过滤、不再加工，口径与本院区金额列表查询一致。</summary>
  [Fact]
  public async Task Export_passes_normalized_non_blank_keyword_as_is()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    await service.GetBranchRecognitionAmountExportAsync(ExportRequest(keyword: "  PROBE-0  "));

    Assert.Equal("PROBE-0", repository.ReceivedQuery!.Value.Keyword);
  }

  /// <summary>导出行集与行序由查询端口给出，应用层原样保持：不重新排序、不丢弃、不截断。</summary>
  [Fact]
  public async Task Export_preserves_the_row_order_given_by_the_query_port()
  {
    FakeRecognitionAmountRepository repository = new(
    [
      BuildItem("PROBE-003", standardItemName: "第三项"),
      BuildItem("PROBE-001", standardItemName: "第一项"),
      BuildItem("PROBE-002", standardItemName: "第二项")
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    ConfigurationExportFileReadModel file = await service.GetBranchRecognitionAmountExportAsync(ExportRequest());

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
    FakeRecognitionAmountRepository repository = new([]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest()));

    Assert.Equal("业务拒绝：当前条件下无可导出数据。", error.Message);
    Assert.Equal(1, repository.CallCount);
  }

  /// <summary>取回行数超过十万行上限按业务拒绝并提示缩小查询条件；上限校验在取回后执行。</summary>
  [Fact]
  public async Task Export_rejects_rows_beyond_the_limit_with_narrowing_prompt()
  {
    FakeRecognitionAmountRepository repository = new(
      Enumerable.Range(0, 100_001).Select(index => BuildItem($"PROBE-{index:D6}")));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.GetBranchRecognitionAmountExportAsync(ExportRequest()));

    Assert.Contains("十万行上限", error.Message, StringComparison.Ordinal);
    Assert.Contains("缩小查询条件", error.Message, StringComparison.Ordinal);
  }

  /// <summary>取回行数恰为十万行上限时允许导出——上限按「超过才拒绝」解释，正常组装工作簿。</summary>
  [Fact]
  public async Task Export_allows_rows_at_the_limit_boundary()
  {
    FakeRecognitionAmountRepository repository = new(
      Enumerable.Range(0, 100_000).Select(index => BuildItem($"PROBE-{index:D6}")));
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    ConfigurationExportFileReadModel file = await service.GetBranchRecognitionAmountExportAsync(ExportRequest());

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = Assert.Single(workbook.Worksheets);
    Assert.Equal(100_001, worksheet.LastRowUsed()!.RowNumber());
  }

  // ---------- 列集与取值 ----------

  /// <summary>
  /// 本院区金额导出恰十列、列名与顺序符合阶段设计；序号 1..N 连续；项目类型与互认配置状态输出服务端文本，
  /// 项目类型未登记时单元格输出空；当前金额写数值单元格并声明两位小数格式，未配置金额单元格留空；
  /// 金额状态按金额是否已配置输出两态文本，零元是已配置；当前不可用原因为空输出空，配置停用行照常导出。
  /// </summary>
  [Fact]
  public async Task Export_writes_the_ten_frozen_columns_with_amount_semantics()
  {
    FakeRecognitionAmountRepository repository = new(
    [
      BuildItem("PROBE-001"),
      BuildItem("PROBE-002", standardItemName: "胸部CT", itemType: MedicalItemType.Examination,
        categoryName: "医学影像", groupName: "CT", currentAmount: 0.00m, configurationIsValid: false, groupIsValid: false),
      BuildItem("PROBE-003", currentAmount: null, itemType: (MedicalItemType)99)
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    ConfigurationExportFileReadModel file = await service.GetBranchRecognitionAmountExportAsync(ExportRequest());

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = workbook.Worksheets.First();
    Assert.Equal(
      ExportHeaders,
      Enumerable.Range(1, 10).Select(column => worksheet.Cell(1, column).GetString()).ToArray());

    Assert.Equal(["1", "检验", "临床检验", "血液学", "PROBE-001", "血常规", "125.5", "已配置", "启用", ""],
      [.. Enumerable.Range(1, 10).Select(column => worksheet.Cell(2, column).GetString())]);
    // 零元是有效配置：金额单元格有数值零，金额状态仍为已配置；配置停用与目录停用各行独立表达。
    Assert.Equal(["2", "检查", "医学影像", "CT", "PROBE-002", "胸部CT", "0", "已配置", "停用", "所属分组已停用"],
      [.. Enumerable.Range(1, 10).Select(column => worksheet.Cell(3, column).GetString())]);
    // 未配置金额行：金额单元格留空，金额状态输出未配置；项目类型取值未登记时项目类型单元格输出空，不伪造类型名。
    Assert.Equal(["3", "", "临床检验", "血液学", "PROBE-003", "血常规", "", "未配置", "启用", ""],
      [.. Enumerable.Range(1, 10).Select(column => worksheet.Cell(4, column).GetString())]);

    // 金额写数值单元格并声明两位小数格式；未配置金额单元格不是数值。
    Assert.Equal(XLDataType.Number, worksheet.Cell(2, 7).DataType);
    Assert.Equal("0.00", worksheet.Cell(2, 7).Style.NumberFormat.Format);
    Assert.Equal(XLDataType.Number, worksheet.Cell(3, 7).DataType);
    Assert.Equal("0.00", worksheet.Cell(3, 7).Style.NumberFormat.Format);
    Assert.NotEqual(XLDataType.Number, worksheet.Cell(4, 7).DataType);
    Assert.Equal(string.Empty, worksheet.Cell(4, 7).GetString());

    // 序号列为数字类型。
    foreach (int rowNumber in new[] { 2, 3, 4 })
    {
      Assert.Equal(XLDataType.Number, worksheet.Cell(rowNumber, 1).DataType);
    }
  }

  /// <summary>
  /// 工作簿组装：单工作表、工作表名与页面名称一致、表头加粗、冻结首行、列宽按内容适配；
  /// 文件名为「本院区互认项目金额 + 组装时刻 yyyyMMddHHmmss + .xlsx」，文件格式为工作簿内容类型。
  /// </summary>
  [Fact]
  public async Task Export_assembles_a_single_bold_frozen_sheet_with_adapted_columns_and_timestamped_name()
  {
    FakeRecognitionAmountRepository repository = new(
    [
      BuildItem("PROBE-001", standardItemName: "血常规"),
      BuildItem("PROBE-002", standardItemName: "胸部CT扫描与影像检查项目名称样例")
    ]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    ConfigurationExportFileReadModel file = await service.GetBranchRecognitionAmountExportAsync(ExportRequest());

    Assert.Matches(@"^本院区互认项目金额\d{14}\.xlsx$", file.FileName);
    Assert.Equal(ExcelFileFormat, file.FileFormat);

    using XLWorkbook workbook = new(new MemoryStream(file.FileStream));
    IXLWorksheet worksheet = Assert.Single(workbook.Worksheets);
    Assert.Equal("本院区互认项目金额", worksheet.Name);

    Assert.All(Enumerable.Range(1, 10), column => Assert.True(worksheet.Cell(1, column).Style.Font.Bold));
    Assert.Equal(1, worksheet.SheetView.SplitRow);
    Assert.Equal(0, worksheet.SheetView.SplitColumn);

    // 列宽按内容适配：内容长的列比序号列更宽，十个使用中的列都有按内容计算的宽度。
    double indexColumnWidth = worksheet.Column(1).Width;
    double nameColumnWidth = worksheet.Column(6).Width;
    Assert.True(nameColumnWidth > indexColumnWidth,
      $"标准项目名称列宽 {nameColumnWidth} 应大于序号列宽 {indexColumnWidth}。");
    Assert.All(Enumerable.Range(1, 10), column => Assert.True(worksheet.Column(column).Width > 0));
  }

  /// <summary>判据 4：请求提供院区业务编码时，导出过滤按可信组织、可信医院与解析出的院区编码下传。</summary>
  [Fact]
  public async Task Export_uses_request_branch_code_when_provided()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    await service.GetBranchRecognitionAmountExportAsync(new BranchRecognitionAmountExportRequest
    {
      BranchCode = RequestBranchCode,
    });

    Assert.Equal(1, repository.CallCount);
    Assert.Equal(
      (FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode, (string?)null, (string?)null),
      repository.ReceivedQuery);
  }

  /// <summary>判据 4 归一口径：请求院区编码带首尾空白时按去空白后的业务编码校验归属，下传的是归一后的编码。</summary>
  [Fact]
  public async Task Export_normalizes_request_branch_code_before_matching_the_trusted_hospital()
  {
    FakeRecognitionAmountRepository repository = new([BuildItem("PROBE-001")]);
    MedicalRecognitionReportQueryAppService service = CreateService(repository, CreateTrustedOrganizationService());

    await service.GetBranchRecognitionAmountExportAsync(new BranchRecognitionAmountExportRequest
    {
      BranchCode = $"  {RequestBranchCode}  ",
    });

    Assert.Equal(1, repository.CallCount);
    Assert.Equal(
      (FakeIamScope.OrganizationCode, FakeIamScope.HospitalCode, FakeIamScope.BranchCode, (string?)null, (string?)null),
      repository.ReceivedQuery);
  }

  // ---------- 夹具 ----------

  /// <summary>构造被测查询应用服务：注入查询端口替身、外部组织服务替身、外部用户服务替身与平台参数替身。</summary>
  /// <param name="repository">查询端口替身。</param>
  /// <param name="organizationService">外部组织服务替身。</param>
  /// <param name="iamScope">当前 IAM 工作身份范围；为空时使用默认三层链。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(
    FakeRecognitionAmountRepository repository, StubOrganizationAppService organizationService, FakeIamScope? iamScope = null)
  {
    FakeIamScope scope = iamScope ?? FakeIamScope.Default;
    return new(repository, new PlatformParameterRepositoryStub(), scope, scope, scope);
  }

  /// <summary>构造被测查询应用服务，并指定外部用户服务替身与当前 IAM 工作身份范围。</summary>
  /// <param name="repository">查询端口替身。</param>
  /// <param name="organizationService">外部组织服务替身。</param>
  /// <param name="userService">外部用户服务替身；当前导出链不读取用户档案，保留以验证未被调用。</param>
  /// <param name="iamScope">当前 IAM 工作身份范围；为空时使用默认三层链。</param>
  /// <returns>可直接调用的查询应用服务。</returns>
  private static MedicalRecognitionReportQueryAppService CreateService(
    FakeRecognitionAmountRepository repository, StubOrganizationAppService organizationService, StubUserAppService userService, FakeIamScope? iamScope = null)
  {
    FakeIamScope scope = iamScope ?? FakeIamScope.Default;
    return new(repository, new PlatformParameterRepositoryStub(), scope, scope, scope);
  }

  /// <summary>构造本院区金额页的导出条件；该请求不含组织与医院字段。</summary>
  /// <param name="branchCode">请求院区编码。</param>
  /// <param name="keyword">可选关键字。</param>
  /// <returns>可直接交给导出入口的请求。</returns>
  private static BranchRecognitionAmountExportRequest ExportRequest(string branchCode = RequestBranchCode, string? keyword = null) => new()
  {
    BranchCode = branchCode,
    Keyword = keyword
  };

  /// <summary>构造只含可信组织路径的外部组织服务替身：一个组织、一家医院、一个院区，三层都启用。</summary>
  /// <returns>已完成初始化的外部组织服务替身。</returns>
  private static StubOrganizationAppService CreateTrustedOrganizationService() =>
    AddTrustedPath(new StubOrganizationAppService());

  /// <summary>向替身填入可信组织、可信医院与请求院区三层启用的路径数据。</summary>
  /// <param name="organizationService">待填充的外部组织服务替身。</param>
  /// <returns>同一个已填充的替身，便于链式构造。</returns>
  private static StubOrganizationAppService AddTrustedPath(StubOrganizationAppService organizationService)
  {
    organizationService.Organizations.Add(new OrganizationDto { Id = TrustedOrganizationCode, Name = "示例组织", IsValid = true });
    organizationService.HospitalsByOrganization[TrustedOrganizationCode] =
      [new HospitalDto { Id = TrustedHospitalCode, Name = "示例医院", OrgId = TrustedOrganizationCode, IsValid = true }];
    organizationService.BranchesByHospital[TrustedHospitalCode] =
      [new BranchDto { Id = RequestBranchCode, Name = "示例院区", HosId = TrustedHospitalCode, OrgId = TrustedOrganizationCode, IsValid = true }];
    return organizationService;
  }

  /// <summary>构造一条金额行投影，只把用例关心的字段参数化，其余字段取固定值。</summary>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="standardItemName">标准项目名称。</param>
  /// <param name="itemType">项目类型。</param>
  /// <param name="categoryName">所属分类名称。</param>
  /// <param name="groupName">所属分组名称。</param>
  /// <param name="currentAmount">当前金额；null 表示未配置金额。</param>
  /// <param name="configurationIsValid">配置自身是否启用。</param>
  /// <param name="groupIsValid">所属分组是否启用。</param>
  /// <returns>查询端口返回的金额行投影。</returns>
  private static RecognitionAmountListItem BuildItem(
    string standardProjectCode,
    string standardItemName = "血常规",
    MedicalItemType itemType = MedicalItemType.Laboratory,
    string categoryName = "临床检验",
    string groupName = "血液学",
    decimal? currentAmount = 125.50m,
    bool configurationIsValid = true,
    bool groupIsValid = true) => new()
    {
      StandardProjectCode = standardProjectCode,
      StandardItemName = standardItemName,
      ItemType = itemType,
      CategoryName = categoryName,
      GroupName = groupName,
      CurrentAmount = currentAmount,
      ConfigurationIsValid = configurationIsValid,
      CategoryIsValid = true,
      GroupIsValid = groupIsValid,
      ItemIsValid = true
    };

  /// <summary>
  /// 金额查询端口的手写替身：记录每次取数收到的业务键与关键字参数及调用次数，按设置的集合返回结果。
  /// </summary>
  /// <remarks>集合支持延迟序列：十万行上限用例按惰性序列构造行数，不预先物化。</remarks>
  private sealed class FakeRecognitionAmountRepository : IMedicalRecognitionReportQueryRepository
  {
    /// <summary>
    /// 创建替身并设置取数返回的投影集合。
    /// </summary>
    /// <param name="items">取数返回的金额行投影集合。</param>
    public FakeRecognitionAmountRepository(IEnumerable<RecognitionAmountListItem> items) => Items = items;

    /// <summary>取数返回的金额行投影集合。</summary>
    public IEnumerable<RecognitionAmountListItem> Items { get; set; }

    /// <summary>本次取数收到的组织、医院、院区、标准项目编码与关键字参数；未进入查询端口时为 null。</summary>
    public (string OrganizationCode, string HospitalCode, string BranchCode, string? StandardProjectCode, string? Keyword)? ReceivedQuery { get; private set; }

    /// <summary>每次取数收到的参数，按调用顺序排列。</summary>
    public List<(string OrganizationCode, string HospitalCode, string BranchCode, string? StandardProjectCode, string? Keyword)> ReceivedQueries { get; } = [];

    /// <summary>进入金额查询端口的次数。</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc/>
    public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(
      string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode, string? keyword)
    {
      CallCount++;
      ReceivedQuery = (organizationCode, hospitalCode, branchCode, standardProjectCode, keyword);
      ReceivedQueries.Add(ReceivedQuery.Value);
      return Task.FromResult(Items);
    }

    /// <summary>本用例集不覆盖其余查询成员，调用即失败。</summary>
    /// <returns>不返回结果。</returns>
    public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? keyword, ConfigurationStatus? configurationStatus) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QuerySelectableStandardItemListAsync(string organizationCode, string? keyword) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid bacteriaResultId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsByVersionsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesByItemsAsync(IReadOnlyList<Guid> examinationItemIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
      string organizationCode, string hospitalCode, string branchCode, Guid patientId, string identityDocumentTypeCode,
      string identityDocumentNo, VisitType visitType, string visitSerialNo, IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc/>
    public Task<bool> HasCurrentEffectiveRecognitionMatchAsync(Guid reportVersionId, string organizationCode, string hospitalCode, string branchCode) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
      string organizationCode, string hospitalCode, string branchCode, string identityDocumentTypeCode,
      string identityDocumentNo, VisitType visitType, string visitSerialNo) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(IReadOnlyList<string> standardProjectCodes) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");

    /// <inheritdoc cref="QueryMedicalStandardCategoryListAsync(MedicalItemType?)"/>
    public Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId) => throw new NotSupportedException("本替身只覆盖本院区金额导出取数。");
  }
}
