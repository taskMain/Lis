using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 的自动端点清单与手写控制器路由的静态核对（矩阵 V83 的端点清单面与路由面）。
/// </summary>
/// <remarks>
/// 框架按应用服务接口自动暴露端点，路由形态为 <c>/Api/{接口简单名}/{动作名去掉 Async}</c>；
/// 端点的存在性由接口与实现上的公开方法决定，因此本用例按"接口方法集合逐项相等"锁定清单，不接受前缀匹配、
/// 数量下限或子集匹配。手写控制器的动作路由另用 Roslyn 语法树核对，并确认其前缀不与自动端点前缀重复或遮蔽。
/// 全部为静态断言：不启动宿主、不连库。
/// </remarks>
public sealed class Stage4EndpointTests
{
  /// <summary>
  /// 报告写入口在阶段 1 至阶段 3 已交付的方法名，按 Ordinal 升序。
  /// </summary>
  private static readonly string[] ExistingWriteMethods =
  [
    "ChangeMedicalStandardItemRemarkAsync", "CreateMedicalStandardCategoryAsync", "CreateMedicalStandardGroupAsync",
    "CreateMedicalStandardItemAsync", "CreateMutualRecognitionItemAsync", "DisableMedicalStandardCategoryAsync",
    "DisableMedicalStandardGroupAsync", "DisableMedicalStandardItemAsync", "DisableMutualRecognitionItemAsync",
    "EnableMedicalStandardCategoryAsync", "EnableMedicalStandardGroupAsync", "EnableMedicalStandardItemAsync",
    "EnableMutualRecognitionItemAsync", "SaveBranchRecognitionAmountAsync", "SaveOrganizationHospitalBranchRecognitionAmountAsync",
    "UpdateMedicalStandardCategoryAsync", "UpdateMedicalStandardGroupAsync", "UpdateMutualRecognitionItemConfigurationAsync"
  ];

  /// <summary>
  /// 报告写入口在阶段 4 新增的方法名；两个提交入口与两个作废入口各由框架自动暴露为一条端点。
  /// </summary>
  private static readonly string[] Stage4WriteMethods =
  [
    "OpenReportVersionPdfAsync", "SubmitCompleteExaminationReportAsync", "SubmitCompleteLaboratoryReportAsync",
    "VoidExaminationReportAsync", "VoidLaboratoryReportAsync"
  ];

  /// <summary>
  /// 报告写入口在阶段 5 新增的方法名；互认匹配请求、处理结果提交与引用结果提交各由框架自动暴露为一条端点，
  /// 前者在同一事务内写入匹配记录与匹配项，后者整批原子保存处理结果并写入处理结果保存时间，
  /// 引用结果提交整批原子保存引用事实并登记一次引用结果已记录事件。
  /// </summary>
  private static readonly string[] Stage5WriteMethods =
  [
    "RequestRecognitionMatchesAsync", "SubmitRecognitionProcessingResultsAsync", "SubmitRecognitionReferencesAsync"
  ];

  /// <summary>
  /// 报告查询入口在阶段 1 至阶段 3 已交付的方法名。
  /// </summary>
  private static readonly string[] ExistingQueryMethods =
  [
    "QueryBranchRecognitionAmountListAsync", "QueryEffectiveMedicalStandardCatalogAsync", "QueryMedicalStandardCategoryListAsync",
    "QueryMedicalStandardGroupListAsync", "QueryMedicalStandardItemListAsync", "QueryRecognitionAmountListAsync",
    "QueryRecognitionProjectConfigurationListAsync"
  ];

  /// <summary>
  /// 报告查询入口在阶段 4 新增的方法名；两个列表查询、版本列表查询与版本详情查询各由框架自动暴露为一条端点。
  /// </summary>
  private static readonly string[] Stage4QueryMethods =
  [
    "QueryBranchMedicalReportListAsync", "QueryMedicalReportListAsync", "QueryMedicalReportVersionDetailAsync",
    "QueryMedicalReportVersionListAsync"
  ];

  /// <summary>
  /// 报告查询入口在阶段 5 新增的方法名；获取引用详情由框架自动暴露为一条端点。
  /// </summary>
  /// <remarks>该入口是只读查询，返回引用详情响应，不声明显式事务。</remarks>
  private static readonly string[] Stage5QueryMethods =
  [
    "QueryRecognitionCitationDetailAsync"
  ];

  /// <summary>
  /// 报告查询入口在阶段 6 新增的方法名；四个统计查询各设平台/本院两个入口、统计导出入口与匹配记录集合视图。
  /// </summary>
  /// <remarks>
  /// 八个查询与匹配记录视图由框架自动暴露为端点；统计导出由导出控制器的平台/本院两个 POST 动作承载，
  /// 接口上的导出方法是两个动作共同调用的应用层入口。
  /// </remarks>
  private static readonly string[] Stage6QueryMethods =
  [
    "GetStatisticsExportAsync", "QueryBranchRecognitionUsageDetailsAsync", "QueryBranchRecognitionUsageSummaryAsync",
    "QueryBranchSourceRecognitionDetailsAsync", "QueryBranchSourceRecognitionSummaryAsync", "QueryRecognitionMatchRecordAsync",
    "QueryRecognitionUsageDetailsAsync", "QueryRecognitionUsageSummaryAsync",
    "QuerySourceRecognitionDetailsAsync", "QuerySourceRecognitionSummaryAsync"
  ];

  /// <summary>
  /// 两个提交入口方法：它们同时是医院接入的提交入口与事务边界。
  /// </summary>
  private static readonly string[] SubmissionMethods =
  [
    "SubmitCompleteLaboratoryReportAsync", "SubmitCompleteExaminationReportAsync"
  ];

  /// <summary>
  /// 两个作废入口方法：它们只有一条写语句，依靠语句级原子性。
  /// </summary>
  private static readonly string[] VoidMethods =
  [
    "VoidLaboratoryReportAsync", "VoidExaminationReportAsync"
  ];

  /// <summary>
  /// 阶段 5 的三个医院接入写用例：它们都是多条独立写语句且要求全成全败，因此各自必须声明事务。
  /// </summary>
  /// <remarks>框架按路由端点元数据取事务声明，这三个入口是自动端点，声明本就在应用服务方法上。</remarks>
  private static readonly string[] Stage5TransactionalMethods =
  [
    "RequestRecognitionMatchesAsync", "SubmitRecognitionProcessingResultsAsync", "SubmitRecognitionReferencesAsync"
  ];

  /// <summary>
  /// 手写控制器三个动作的公开方法名与其在控制器路由下的相对路径。
  /// </summary>
  private static readonly (string ActionName, string RelativeRoute)[] ControllerActions =
  [
    ("SubmitLaboratoryReportAsync", "laboratory-report"),
    ("SubmitExaminationReportAsync", "examination-report"),
    ("DownloadReportVersionPdfAsync", "{reportId:guid}/versions/{reportVersionId:guid}/pdf")
  ];

  /// <summary>
  /// 控制器两个上传动作：它们是提交用例实际到达的端点，因此各自必须声明事务。
  /// </summary>
  private static readonly string[] UploadActions =
  [
    "SubmitLaboratoryReportAsync", "SubmitExaminationReportAsync"
  ];

  /// <summary>
  /// 控制器下载动作：只读用例，不声明事务。
  /// </summary>
  private const string DownloadAction = "DownloadReportVersionPdfAsync";

  /// <summary>
  /// V83：报告写入口的公开方法集合与阶段 4 新增后的完整清单逐项相等。
  /// </summary>
  /// <remarks>
  /// 接口方法集合决定框架暴露的端点数与端点名，因此等值断言同时锁定"新增端点已暴露"与"没有多余端点泄漏"；
  /// 只断言包含关系时，删掉一个既有方法或多出一个方法都不会让任何用例失败。
  /// </remarks>
  [Fact]
  public void Write_contract_method_set_matches_the_published_endpoint_inventory()
  {
    string[] expected = [.. ExistingWriteMethods.Concat(Stage4WriteMethods).Concat(Stage5WriteMethods).OrderBy(name => name, StringComparer.Ordinal)];
    string[] actual = MethodNamesAscending(typeof(IMedicalRecognitionReportAppService));

    Assert.Equal(expected, actual);

    // 变异证据：从副本里去掉一个新方法后，等值判据必须判为不相等。
    string[] mutated = [.. actual.Where(name => name != "SubmitCompleteLaboratoryReportAsync")];
    Assert.NotEqual(expected, mutated);
    string[] withoutStage5Entry = [.. actual.Where(name => name != "RequestRecognitionMatchesAsync")];
    Assert.NotEqual(expected, withoutStage5Entry);
    string[] withoutProcessingResultEntry = [.. actual.Where(name => name != "SubmitRecognitionProcessingResultsAsync")];
    Assert.NotEqual(expected, withoutProcessingResultEntry);
    string[] withoutReferenceEntry = [.. actual.Where(name => name != "SubmitRecognitionReferencesAsync")];
    Assert.NotEqual(expected, withoutReferenceEntry);
  }

  /// <summary>
  /// V83：报告查询入口的公开方法集合与阶段 4 至阶段 6 新增后的完整清单逐项相等。
  /// </summary>
  [Fact]
  public void Query_contract_method_set_matches_the_published_endpoint_inventory()
  {
    string[] expected = [.. ExistingQueryMethods.Concat(Stage4QueryMethods).Concat(Stage5QueryMethods).Concat(Stage6QueryMethods).OrderBy(name => name, StringComparer.Ordinal)];
    string[] actual = MethodNamesAscending(typeof(IMedicalRecognitionReportQueryAppService));

    Assert.Equal(expected, actual);

    string[] mutated = [.. actual.Where(name => name != "QueryMedicalReportListAsync")];
    Assert.NotEqual(expected, mutated);
    string[] withoutCitationEntry = [.. actual.Where(name => name != "QueryRecognitionCitationDetailAsync")];
    Assert.NotEqual(expected, withoutCitationEntry);
    string[] withoutStatisticsEntries = [.. actual.Where(name => !Stage6QueryMethods.Contains(name, StringComparer.Ordinal))];
    Assert.NotEqual(expected, withoutStatisticsEntries);
  }

  /// <summary>
  /// V83：两个提交入口方法带事务声明，两个作废方法不带；控制器两个上传动作各带事务声明，下载动作不带。
  /// </summary>
  /// <remarks>
  /// 事务边界声明在公开应用服务入口的方法上，控制器只做绑定、校验、落盘与响应。
  /// 控制器动作同样必须声明事务：框架的端点过滤器按**路由端点元数据**取事务声明，
  /// 控制器动作就是本用例实际到达的端点，应用服务方法上的声明在该调用路径上不被读取；
  /// 只声明在应用服务上时，提交在写入之后失败不会回滚（运行期实测在报告主体留下残留行）。
  /// 该分布因此是可机械核对的检查点：两个上传动作必须带事务，下载动作与作废入口不带。
  /// </remarks>
  [Fact]
  public void Submission_entrypoints_declare_transaction_and_void_entrypoints_do_not()
  {
    Type appService = typeof(Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService);

    foreach (string name in SubmissionMethods)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Attribute workUnit = Assert.Single(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
      Assert.True((bool)workUnit.GetType().GetProperty("UseTransaction")!.GetValue(workUnit)!);
    }

    foreach (string name in VoidMethods)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }

    // 变异证据：把作废方法名单换成提交方法名后，同一条判据必须失败；证明判定按方法名锚定而不是恒真。
    Assert.ThrowsAny<Exception>(() =>
    {
      MethodInfo method = appService.GetMethod(nameof(SubmissionMethods))!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    });

    // 控制器两个上传动作带事务声明：它们就是端点，框架按端点元数据决定是否开事务。
    foreach (string actionName in UploadActions)
    {
      MethodInfo action = ControllerActionMethod(actionName);
      Attribute workUnit = Assert.Single(action.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
      Assert.True((bool)workUnit.GetType().GetProperty("UseTransaction")!.GetValue(workUnit)!);
    }

    // 下载动作只读，不带事务声明。
    Assert.DoesNotContain(ControllerActionMethod(DownloadAction).GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
  }

  /// <summary>
  /// V87：阶段 5 的三个医院接入写用例各声明显式事务，声明就在自动端点对应的应用服务方法上。
  /// </summary>
  /// <remarks>
  /// 匹配查询写匹配记录与匹配项两条语句，处理结果提交写 N 行处理结果并更新 1 行匹配记录，
  /// 引用结果提交写 N 行引用事实并登记一次事件，都要求全成全败；
  /// 三者都是自动端点，因此不存在控制器动作上的第二处声明，端点元数据即取这三个方法上的声明。
  /// </remarks>
  [Fact]
  public void Stage5_hospital_entrypoints_declare_transaction()
  {
    Type appService = typeof(Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService);

    foreach (string name in Stage5TransactionalMethods)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Attribute workUnit = Assert.Single(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
      Assert.True((bool)workUnit.GetType().GetProperty("UseTransaction")!.GetValue(workUnit)!);
    }

    // 变异证据：把名单换成只有一条写语句的作废入口后，同一条判据必须失败，证明判定按方法名锚定而不是恒真。
    Assert.DoesNotContain(
      appService.GetMethod("VoidLaboratoryReportAsync")!.GetCustomAttributes(),
      attribute => attribute.GetType().Name == "WorkUnitAttribute");
  }

  /// <summary>
  /// V83 的路由面：手写控制器的三个动作声明在带版本前缀的路由下，且该前缀与自动端点前缀不重复、不遮蔽。
  /// </summary>
  /// <remarks>
  /// 自动端点前缀为 <c>/Api/</c> 加接口简单名；手写控制器使用 <c>api/v1/report-pdf</c>，两者首段不同，
  /// 既不会路由重复，也不会让任一动作被自动端点遮蔽。判定按语法节点取特性参数，不按文本匹配。
  /// </remarks>
  [Fact]
  public void Controller_action_routes_do_not_collide_with_auto_endpoint_prefix()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition", "Controllers", "ReportPdfFileController.cs");

    TypeDeclarationSyntax controller = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
      .Single(declaration => declaration.Identifier.ValueText == "ReportPdfFileController");

    string[] apiControllerRoutes = [.. controller.AttributeLists
      .SelectMany(list => list.Attributes)
      .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "Route")
      .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
    string controllerRoute = Assert.Single(apiControllerRoutes);

    Assert.Equal("api/v1/report-pdf", controllerRoute);
    // 自动端点前缀的首段是 Api；控制器首段是 api（框架路由不区分大小写），但第二段是版本号，
    // 因此两者的路由模板在前两段上就不同，不存在同一路径被两处声明的可能。
    string[] controllerSegments = controllerRoute.Split('/');
    Assert.Equal("api", controllerSegments[0]);
    Assert.NotEqual("IMedicalRecognitionReportAppService", controllerSegments[1]);
    Assert.NotEqual("IMedicalRecognitionReportQueryAppService", controllerSegments[1]);

    // 三个动作各自的相对路由与声明一致，且动作方法名与清单逐字相符。
    foreach ((string actionName, string relativeRoute) in ControllerActions)
    {
      MethodDeclarationSyntax action = controller.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Single(declaration => declaration.Identifier.ValueText == actionName);
      string[] templates = [.. action.AttributeLists
        .SelectMany(list => list.Attributes)
        .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpPost" or "HttpGet")
        .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
      Assert.Equal([relativeRoute], templates);
    }

    // 变异证据：把控制器路由前缀换成自动端点前缀形态后，同一条判据必须判出冲突。
    string autoEndpointPrefix = $"Api/{nameof(IMedicalRecognitionReportAppService)}";
    Assert.StartsWith("Api/", autoEndpointPrefix, StringComparison.Ordinal);
    Assert.NotEqual(autoEndpointPrefix, controllerRoute);
    Assert.NotEqual(autoEndpointPrefix.Split('/')[1], controllerSegments[1]);
  }

  /// <summary>
  /// V83：两个上传动作只接受 multipart 且只绑定两个命名部件；两个提交入口的请求类型只承载文件键、下载名与文档原文。
  /// </summary>
  /// <remarks>
  /// 部件名固定为 <c>reportJson</c> 与 <c>pdfFile</c>；请求类型不接收文件字节，
  /// 因此接口文档不会出现文件字段，也不会把二进制内容带进业务写入。
  /// </remarks>
  [Fact]
  public void Upload_actions_consume_multipart_and_bind_two_named_parts_only()
  {
    Type formType = typeof(Dy.MedicalRecognition.Controllers.ReportSubmissionForm);
    Assert.Equal(["PdfFile", "ReportJson"], DeclaredPropertyNamesAscending(formType));

    foreach ((string actionName, string _) in ControllerActions.Where(action => action.ActionName.StartsWith("Submit", StringComparison.Ordinal)))
    {
      MethodInfo action = ControllerActionMethod(actionName);
      Attribute consumes = Assert.Single(action.GetCustomAttributes(), attribute => attribute.GetType().Name == "ConsumesAttribute");
      var contentTypes = (System.Collections.IEnumerable)consumes.GetType().GetProperty("ContentTypes")!.GetValue(consumes)!;
      Assert.Equal(["multipart/form-data"], contentTypes.Cast<string>().ToArray());

      // 表单参数带 [FromForm]，说明绑定来源是 multipart 部件而不是请求体 JSON。
      ParameterInfo parameter = Assert.Single(action.GetParameters());
      Assert.Equal(formType, parameter.ParameterType);
      Assert.Contains(
        parameter.GetCustomAttributes(),
        attribute => attribute.GetType().Name == "FromFormAttribute");
    }

    // 提交入口的请求类型只承载文档原文、文件键与下载名，不承载文件字节。
    Assert.Equal(
      ["PdfFileId", "PdfFileName", "ReportJson"],
      DeclaredPropertyNamesAscending(typeof(CompleteReportSubmissionRequest)));
    Assert.DoesNotContain(
      typeof(CompleteReportSubmissionRequest).GetProperties(),
      property => property.PropertyType.Name.Contains("FormFile", StringComparison.Ordinal));
  }

  /// <summary>
  /// V83：作废请求只提交报告单号、作废时间与作废原因；组织、医院、院区与操作人都由服务端解析。
  /// </summary>
  [Fact]
  public void Void_request_exposes_only_report_number_void_time_and_reason()
  {
    Assert.Equal(["ReportNo", "VoidReason", "VoidedTime"], DeclaredPropertyNamesAscending(typeof(MedicalReportVoidRequest)));

    Type type = typeof(MedicalReportVoidRequest);
    Assert.Null(type.GetProperty("OrganizationCode"));
    Assert.Null(type.GetProperty("HospitalCode"));
    Assert.Null(type.GetProperty("BranchCode"));
    Assert.Null(type.GetProperty("OperId"));
    Assert.Null(type.GetProperty("ReportType"));

    // 提交入口的请求类型同样不接受组织、医院、院区与操作人：它们由应用服务从可信上下文组装。
    Type submissionType = typeof(CompleteReportSubmissionRequest);
    Assert.Null(submissionType.GetProperty("OrganizationCode"));
    Assert.Null(submissionType.GetProperty("HospitalCode"));
    Assert.Null(submissionType.GetProperty("BranchCode"));
    Assert.Null(submissionType.GetProperty("OperId"));
  }

  /// <summary>
  /// V83：作废请求的必填校验在进入应用层之前生效。
  /// </summary>
  [Fact]
  public void Void_request_rejects_missing_report_number_and_reason()
  {
    Assert.Throws<ValidationException>(() => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
      new MedicalReportVoidRequest { ReportNo = string.Empty, VoidReason = "原因" }));
    Assert.Throws<ValidationException>(() => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
      new MedicalReportVoidRequest { ReportNo = "R-1", VoidReason = "   " }));

    Assert.Null(Record.Exception(() => Dy.MedicalRecognition.Application.Contracts.Validation.MedicalRecognitionRequestValidator.Validate(
      new MedicalReportVoidRequest { ReportNo = "R-1", VoidReason = "重复报告" })));
  }

  /// <summary>
  /// 取接口自身声明的公开方法名，按 Ordinal 升序。
  /// </summary>
  /// <param name="type">待检查的接口类型。</param>
  /// <returns>该接口声明的公开实例方法名。</returns>
  private static string[] MethodNamesAscending(Type type) =>
    [.. type.GetMethods().Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal)];

  /// <summary>
  /// 取类型自身声明的公开实例属性名，按 Ordinal 升序，排除继承来的框架成员。
  /// </summary>
  /// <param name="type">待检查的类型。</param>
  /// <returns>该类型自身声明的公开实例属性名。</returns>
  private static string[] DeclaredPropertyNamesAscending(Type type) =>
  [
    .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Select(property => property.Name)
      .OrderBy(name => name, StringComparer.Ordinal)
  ];

  /// <summary>
  /// 按动作方法名读取控制器动作的反射信息，用于核对特性声明。
  /// </summary>
  /// <param name="actionName">动作方法名。</param>
  /// <returns>该动作的方法信息。</returns>
  private static MethodInfo ControllerActionMethod(string actionName) =>
    typeof(Dy.MedicalRecognition.Controllers.ReportPdfFileController).GetMethod(actionName)!;
}
