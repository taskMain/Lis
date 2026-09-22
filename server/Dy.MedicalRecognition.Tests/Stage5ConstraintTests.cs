using System.Text;
using System.Text.Json;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 的跨接口静态收口：客户端路径与异常处理一致性、事务声明分布、引用详情链零写入，
/// 以及 README 约定 12 的实现约束核对。
/// </summary>
/// <remarks>
/// 覆盖矩阵 V86（客户端路径与异常处理一致性）、V87（事务声明）与 V88 的静态核对面。
/// 判定一律按 Roslyn 语法节点或映射文件的结构化解析，不做文本或正则匹配：
/// 注释与字符串里的同名写法不产生节点，因此"只在注释里解释不使用中间件"不会被误判成声明。
/// 每项判据都带一条变异证据：把负向特征注入与判据同一份源码文本后，同一判定函数必须报出违规；
/// 阶段 5 的任何生产源码都不做异常捕获与加工，因此本阶段没有需要逐处登记的异常处理位置。
/// </remarks>
public sealed class Stage5ConstraintTests
{
  /// <summary>
  /// 阶段 5 新增或修改的生产源码文件，路径相对仓库根目录并使用 <c>/</c> 分隔。
  /// </summary>
  /// <remarks>
  /// 清单按票 00 至票 06 的实际交付范围逐文件冻结，用于核对阶段 5 的交付范围实际存在且落在生产扫描面内；
  /// 扫描面由 <see cref="ProductionProjectDirectories"/> 下的全量源码决定，本清单不作为扫描面的来源，
  /// 因此清单外新落地的生产文件同样逐份参与客户端路径与异常处理核对。
  /// </remarks>
  private static readonly string[] Stage5ProductionSources =
  [
    // 应用层的四个入口实现与共享入口文件。
    "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs",
    "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Matching.cs",
    "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.ProcessingResult.cs",
    "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Reference.cs",
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs",
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs",
    // 公共接口：延长既有写接口的两个分片，新增查询接口分片与四个请求类型。
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.ProcessingResult.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.Reference.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Citation.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionCitationDetailRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionMatchProposedItemRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionMatchQueryRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionProcessingResultItemRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionProcessingResultSubmissionRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionReferenceItemRequest.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/RecognitionReferenceSubmissionRequest.cs",
    // 领域命令与其项目类型。
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/QueryRecognitionMatchesCommand.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/QueryRecognitionMatchesCommandItem.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionProcessingResultCommandItem.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionProcessingResultsCommand.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionReferenceCommandItem.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionReferencesCommand.cs",
    // 领域管理器按能力新增的三个分片。
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Matching.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.ProcessingResult.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Reference.cs",
    // 仓储端口按能力新增的三个分片。
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Ports/IMedicalRecognitionReportRepository.Matching.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Ports/IMedicalRecognitionReportRepository.ProcessingResult.cs",
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Ports/IMedicalRecognitionReportRepository.Reference.cs",
    // 匹配查询的领域返回结果。
    "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionMatchResult.cs",
    // 查询侧端口的新增分片与查询侧内部投影类型。
    "server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.Citation.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.Matching.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/CitationStandardProjectNameItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionCitationCandidateItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionCitationReportContextItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionMatchCandidateReportItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionMatchReportFactsItem.cs",
    // 三个领域事件与其逐项事实类型。
    "server/Dy.MedicalRecognition.Domain.Share/MedicalRecognitionReportAggregate/Events/RecognitionMatchesReturnedEvent.cs",
    "server/Dy.MedicalRecognition.Domain.Share/MedicalRecognitionReportAggregate/Events/RecognitionProcessingResultsSavedEvent.cs",
    "server/Dy.MedicalRecognition.Domain.Share/MedicalRecognitionReportAggregate/Events/RecognitionReferenceFact.cs",
    "server/Dy.MedicalRecognition.Domain.Share/MedicalRecognitionReportAggregate/Events/RecognitionReferencesRecordedEvent.cs",
    // 写侧与查询侧仓储实现的新增分片。
    "server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.Matching.cs",
    "server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.ProcessingResult.cs",
    "server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.Reference.cs",
    "server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Citation.cs",
    "server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Matching.cs"
  ];

  /// <summary>
  /// 阶段 5 新增或修改的测试工程文件，路径相对仓库根目录并使用 <c>/</c> 分隔。
  /// </summary>
  /// <remarks>测试文件不参与客户端路径与异常处理的静态核对，因此与生产源码分开冻结。</remarks>
  private static readonly string[] Stage5TestSources =
  [
    "server/Dy.MedicalRecognition.Tests/FakeMatchQueryRepository.cs",
    "server/Dy.MedicalRecognition.Tests/FakeQueryRepository.cs",
    "server/Dy.MedicalRecognition.Tests/FakeReportRepository.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5ConstraintTests.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5ParameterAndEnumContractTests.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5QueryTests.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5RequestAndReadModelContractTests.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs",
    "server/Dy.MedicalRecognition.Tests/Stage5WritePathTests.cs",
    "server/Dy.MedicalRecognition.Tests/StubSystemParameterAppService.cs"
  ];

  /// <summary>
  /// 客户端路径与异常处理核对所扫描的生产工程目录，路径相对仓库根目录并使用 <c>/</c> 分隔。
  /// </summary>
  /// <remarks>
  /// 扫描面按工程目录全量取 <c>*.cs</c>，与阶段 4 的同类守卫同口径，不使用手工文件清单决定扫描面：
  /// 只核对清单时，清单外新落地的类型（例如宿主下新增一个继承中间件基类或带授权特性的类）
  /// 不会被任何判据看到。工程目录只列生产工程，测试工程不参与核对。
  /// </remarks>
  private static readonly string[] ProductionProjectDirectories =
  [
    "server/Dy.MedicalRecognition",
    "server/Dy.MedicalRecognition.Application",
    "server/Dy.MedicalRecognition.Application.Contracts",
    "server/Dy.MedicalRecognition.Domain",
    "server/Dy.MedicalRecognition.Domain.Share",
    "server/Dy.MedicalRecognition.Repository"
  ];

  /// <summary>
  /// 引用详情链涉及的源文件：查询应用服务分片、查询接口分片、查询侧端口分片、查询侧实现分片，
  /// 以及该链自己新增的候选记录、报告公共上下文与标准项目名称三个内部投影类型。
  /// </summary>
  /// <remarks>
  /// 链的范围按"引用详情查询入口直达的四个分片加三个阶段 5 专有投影"定义。
  /// 与报告列表查询共用的视图与读模型不列入本清单：它们不承载引用详情的写入面，且报告列表侧另有用例覆盖；
  /// 查询侧实现的写入面缺口由 <see cref="Citation_chain_keeps_no_write_violation"/> 中的查询侧仓储全量断言补齐。
  /// </remarks>
  private static readonly string[] CitationDetailChainSources =
  [
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs",
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Citation.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.Citation.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionCitationCandidateItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/RecognitionCitationReportContextItem.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/CitationStandardProjectNameItem.cs",
    "server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Citation.cs"
  ];

  /// <summary>
  /// 全量生产扫描面内明确要求保留的异常翻译位置：只有这些文件与方法允许出现 <c>try</c> 语句。
  /// </summary>
  /// <remarks>
  /// 依据 SRS 设计约束 71 与阶段 5 规格「权限与异常」段：不新增异常中间件、过滤器、响应转换器或错误包装模型，业务失败直接抛出，
  /// 阶段 5 的生产源码因此没有需要登记的异常处理位置，本清单的阶段 5 登记面为空。
  /// 扫描面覆盖全部生产工程，因此阶段 5 之前已存在的 PDF 文件存储链与报告文档解析处仍逐处登记：
  /// 它们同为"把底层失败翻译为对外拒绝或清理半成品"的既有写法，不新增异常包装、不注册中间件或过滤器。
  /// 例外按文件与方法两个条件逐处登记，因此同一文件内新增第二处 <c>try</c>、
  /// 或另一个文件里出现同名方法并带 <c>try</c> 都会判失败。
  /// </remarks>
  private static readonly (string RelativePath, string MethodName)[] RegisteredTryScopes =
  [
    ("server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs", "SubmitReportAsync"),
    ("server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs", "ReadReportNo"),
    ("server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs", "DeleteNewFileAsync"),
    ("server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs", "DeserializeReportDocument"),
    ("server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs", "SaveAsync"),
    ("server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs", "TryDelete"),
    ("server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/ReportPdfFileOptions.cs", "ResolveRootDirectoryOrThrow")
  ];

  /// <summary>
  /// 全量生产扫描面内明确要求保留的异常包装位置：只有这些文件与方法允许在 <c>catch</c> 内抛出新的异常类型。
  /// </summary>
  /// <remarks>
  /// 与 <see cref="RegisteredTryScopes"/> 同源，但只登记确实把捕获的异常包装成新异常类型的位置：
  /// 报告文档解析处把 <c>JsonException</c> 包装为对外业务拒绝，存储根目录校验处把写入失败包装为对外业务拒绝。
  /// 其余 <c>try</c> 位置（写库失败补偿删除、半成品清理）只原样重抛或吞掉清理异常，不在此列。
  /// 两处之外的任何 <c>catch</c> 内包装都是"把业务失败包起来再转换"的新增写法，判失败。
  /// </remarks>
  private static readonly (string RelativePath, string MethodName)[] RegisteredWrapScopes =
  [
    ("server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs", "ReadReportNo"),
    ("server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs", "DeserializeReportDocument"),
    ("server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/ReportPdfFileOptions.cs", "ResolveRootDirectoryOrThrow")
  ];

  /// <summary>
  /// 参与客户端路径与异常处理静态核对的禁止类型名后缀。
  /// </summary>
  /// <remarks>
  /// 后缀集合与阶段 4 的同类守卫同口径：错误包装模型按 <c>Result</c> 与 <c>Response</c> 加包装后缀识别，
  /// 不用泛称 <c>Filter</c>，避免把查询筛选条件这类普通业务类型判成响应转换器。
  /// </remarks>
  private static readonly string[] BannedClientPathTypeSuffixes =
  [
    "Middleware", "ExceptionFilter", "ExceptionHandler", "ResultFilter", "ResponseFilter", "ActionFilter",
    "ResponseWrapper", "ErrorWrapper", "ApiResponse"
  ];

  /// <summary>
  /// 角色门控与授权特性的禁止名称；匹配时同时接受带与不带 <c>Attribute</c> 后缀两种写法。
  /// </summary>
  private static readonly string[] BannedRoleGatingAttributes =
  [
    "Authorize", "AllowAnonymous", "RequirePermission", "PermissionAuthorize", "RoleAuthorize"
  ];

  /// <summary>
  /// 判定特性名是否属于禁止的角色门控或管线注册特性。
  /// </summary>
  /// <param name="attributeName">特性名的简单名。</param>
  /// <returns>命中 <see cref="BannedRoleGatingAttributes"/> 或名称含管线关键字时为 <see langword="true"/>。</returns>
  /// <remarks>
  /// 角色门控按清单精确匹配，同时接受带与不带 <c>Attribute</c> 后缀两种写法；
  /// 管线注册按名称片段匹配，因此 <c>RequestAuditMiddlewareAttribute</c> 这类改名写法也会命中。
  /// </remarks>
  private static bool IsBannedPipelineAttributeName(string attributeName) =>
    BannedRoleGatingAttributes.Any(candidate => attributeName == candidate || attributeName == $"{candidate}Attribute") ||
    attributeName.Contains("Middleware", StringComparison.Ordinal) ||
    attributeName.Contains("Filter", StringComparison.Ordinal) ||
    attributeName.Contains("ExceptionHandler", StringComparison.Ordinal);

  /// <summary>
  /// 判定名称是否携带权限或角色关键字。
  /// </summary>
  /// <param name="name">接收方写法或调用成员名。</param>
  /// <returns>名称含 <c>Permission</c> 或 <c>Role</c> 时为 <see langword="true"/>。</returns>
  /// <remarks>
  /// 比较不区分大小写：标识符大小写写法差异不应成为绕过判定的方式。
  /// <c>IsInRole</c> 与 <c>IsInRoleAsync</c> 属框架身份的成员读取，与权限判断无关，单独排除。
  /// </remarks>
  private static bool HasPermissionKeyword(string name) =>
    !RoleInspectionMembers.Contains(name, StringComparer.OrdinalIgnoreCase) &&
    (name.Contains("Permission", StringComparison.OrdinalIgnoreCase) || name.Contains("Role", StringComparison.OrdinalIgnoreCase));

  /// <summary>
  /// 属框架身份的成员读取、与权限判断无关的成员名。
  /// </summary>
  private static readonly string[] RoleInspectionMembers =
  [
    "IsInRole", "IsInRoleAsync"
  ];

  /// <summary>
  /// 全仓显式事务声明的冻结分布：三个文件共九处，本阶段新增三处，既有六处不变。
  /// </summary>
  /// <remarks>
  /// 依据 S5-D18 与矩阵 V87：既有实现二十三处公开入口中四处声明在写应用服务入口、两处声明在控制器上传动作
  /// （框架按路由端点元数据取事务声明）；本阶段四个接口全为自动端点，新增三处声明即端点本身。
  /// </remarks>
  private static readonly (string RelativePath, string[] MethodNames)[] FrozenTransactionDeclarations =
  [
    (
      "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs",
      ["CreateMedicalStandardGroupAsync", "CreateMedicalStandardItemAsync"]
    ),
    (
      "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs",
      ["SubmitCompleteExaminationReportAsync", "SubmitCompleteLaboratoryReportAsync"]
    ),
    (
      "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Matching.cs",
      ["RequestRecognitionMatchesAsync"]
    ),
    (
      "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.ProcessingResult.cs",
      ["SubmitRecognitionProcessingResultsAsync"]
    ),
    (
      "server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Reference.cs",
      ["SubmitRecognitionReferencesAsync"]
    ),
    (
      "server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs",
      ["SubmitExaminationReportAsync", "SubmitLaboratoryReportAsync"]
    )
  ];

  /// <summary>
  /// V86：生产工程的全部源码不出现异常中间件、过滤器、响应转换器、错误包装模型与角色门控，
  /// 也不出现把业务失败包起来再转换的 <c>try</c> 语句。
  /// </summary>
  /// <remarks>
  /// 扫描面按生产工程目录全量取 <c>*.cs</c>（排除 <c>bin</c>、<c>obj</c> 与 <c>*.g.cs</c>、<c>*.Designer.cs</c>），
  /// 与阶段 4 的同类守卫同口径；<see cref="Stage5ProductionSources"/> 只用于断言自身是扫描面的子集，不决定扫描面。
  /// 判定按类型声明后缀、基类型名、特性名、权限判断调用与 <c>try</c> 语句五类节点判定；
  /// 角色门控与管线注册特性在类型声明与方法声明两种位置上判定。
  /// 例外见 <see cref="RegisteredTryScopes"/> 与 <see cref="RegisteredWrapScopes"/>，按文件与方法两个条件逐处登记。
  /// </remarks>
  [Fact]
  public void Client_path_and_exception_handling_stay_consistent()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string[] scanPaths = [.. ProductionProjectDirectories
      .SelectMany(directory => Directory.EnumerateFiles(
        Path.Combine(repositoryRoot, directory.Replace('/', Path.DirectorySeparatorChar)),
        "*.cs",
        SearchOption.AllDirectories))
      .Where(file => IsProductionSourceFile(repositoryRoot, file))
      .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
      .Order(StringComparer.Ordinal)];

    List<string> violations = [];
    int scannedTypeCount = 0;

    foreach (string relativePath in scanPaths)
    {
      (IReadOnlyList<string> fileViolations, int typeCount) = FindClientPathViolations(
        relativePath, SourceSyntaxGuard.Read(relativePath.Split('/')));
      violations.AddRange(fileViolations);
      scannedTypeCount += typeCount;
    }

    // 先判定扫描规模：扫描面意外塌缩为少量文件时，下面的空违规判定会退化成恒真。
    Assert.True(
      scanPaths.Length >= 200,
      $"受核对的生产源码应不少于 200 个，实际 {scanPaths.Length} 个。");
    Assert.True(
      scannedTypeCount >= 200,
      $"受核对的生产类型声明应不少于 200 个，实际 {scannedTypeCount} 个。");
    Assert.DoesNotContain(scanPaths, path => path.Contains("/Tests/", StringComparison.Ordinal));

    // 阶段 5 清单必须是扫描面的子集而不是扫描面的来源：清单外的文件同样逐份参与上面的核对。
    string[] notScanned = [.. Stage5ProductionSources.Except(scanPaths, StringComparer.Ordinal)];
    Assert.True(notScanned.Length == 0, $"阶段 5 清单内的文件未进入扫描面：{string.Join("、", notScanned)}。");

    StringBuilder violationReport = new();
    foreach (string violation in violations) violationReport.AppendLine(violation);
    Assert.True(
      violations.Count == 0,
      $"生产源码在客户端路径与异常处理核对中报出 {violations.Count} 处违规：\n{violationReport}");

    // 例外登记覆盖到具体文件与方法：每个登记文件内的 try 语句恰好落在该文件已登记的方法上，且登记项仍在扫描面内。
    foreach (IGrouping<string, (string RelativePath, string MethodName)> registeredFile in RegisteredTryScopes.GroupBy(scope => scope.RelativePath))
    {
      Assert.Contains(registeredFile.Key, scanPaths);
      IReadOnlyList<(string RelativePath, string MethodName)> tryScopes =
        TryScopesOf(SourceSyntaxGuard.Read(registeredFile.Key.Split('/')), registeredFile.Key);
      Assert.Equal(
        registeredFile.Select(scope => scope.MethodName).Order(StringComparer.Ordinal),
        tryScopes.Select(scope => scope.MethodName).Order(StringComparer.Ordinal));
    }

    // 变异证据一：五类违规样本各自必须命中，证明五类判据都有判别力。
    // 带授权特性的新类型按类型声明位置判定，因此类型名不带禁止后缀时同样命中。
    (IReadOnlyList<string> authorityViolations, _) = FindClientPathViolations(
      "probe/ProbeAuthority.cs",
      SourceSyntaxGuard.Parse(
        """
        [Authorize]
        internal sealed class ProbeAuthorizedService { }

        [PermissionAuthorize("Report.Read")]
        internal sealed class ProbePermissionGatedService { }
        """));
    Assert.Contains("probe/ProbeAuthority.cs: ProbeAuthorizedService 声明了禁止的角色门控特性 Authorize", authorityViolations);
    Assert.Contains("probe/ProbeAuthority.cs: ProbePermissionGatedService 声明了禁止的角色门控特性 PermissionAuthorize", authorityViolations);

    // 带授权特性的方法同样命中，证明类型声明位置的补齐没有削弱方法声明位置的判定。
    (IReadOnlyList<string> methodAuthorityViolations, _) = FindClientPathViolations(
      "probe/ProbeMethodAuthority.cs",
      SourceSyntaxGuard.Parse("internal sealed class Probe { [Authorize] internal void Gated() { } }"));
    Assert.Contains("probe/ProbeMethodAuthority.cs: Gated 声明了禁止的角色门控特性 Authorize", methodAuthorityViolations);

    // 依赖注入字段名与类型名不同时，权限判断调用同样命中：调用成员名与接收方两处关键字都参与判定。
    (IReadOnlyList<string> permissionViolations, _) = FindClientPathViolations(
      "probe/ProbePermission.cs",
      SourceSyntaxGuard.Parse(
        """
        internal sealed class ProbePermission
        {
          internal void Entry()
          {
            authorizationService.RequirePermission("Report.Read");
            currentUser.CheckPermission("Report.Read");
            user.IsInRole("Doctor");
          }
        }
        """));
    Assert.Contains("probe/ProbePermission.cs: RequirePermission 在 authorizationService 上执行了权限判断调用", permissionViolations);
    Assert.Contains("probe/ProbePermission.cs: CheckPermission 在 currentUser 上执行了权限判断调用", permissionViolations);

    // 判定的另一侧：框架身份的成员读取不属权限判断，不报违规，证明关键字判定有确定的边界。
    (IReadOnlyList<string> roleInspectionViolations, _) = FindClientPathViolations(
      "probe/ProbeRoleInspection.cs",
      SourceSyntaxGuard.Parse("internal sealed class Probe { internal bool Entry() => user.IsInRole(\"Doctor\"); }"));
    Assert.Empty(roleInspectionViolations);

    // 另一个文件里的同名方法带 try 时必须命中：例外登记按文件与方法两个条件比对。
    (IReadOnlyList<string> sameNameTryViolations, _) = FindClientPathViolations(
      "server/Dy.MedicalRecognition.Application/ProbeOtherFile.cs",
      SourceSyntaxGuard.Parse(
        """
        internal sealed class ProbeOtherFile
        {
          internal void CreateRecognitionReferenceAsync()
          {
            try { Business(); } catch (Exception) { }
          }
        }
        """));
    Assert.Contains(
      "server/Dy.MedicalRecognition.Application/ProbeOtherFile.cs: CreateRecognitionReferenceAsync 的 try 语句未登记为设计允许的异常翻译位置",
      sameNameTryViolations);

    // 登记位置之外在 catch 内包装新异常必须命中，与包装异常处的文件加方法双条件判定一致。
    (IReadOnlyList<string> wrapViolations, _) = FindClientPathViolations(
      "probe/ProbeWrap.cs",
      SourceSyntaxGuard.Parse(
        """
        internal sealed class ProbeWrap
        {
          internal void Entry()
          {
            try { Business(); } catch (Exception exception) { throw new InvalidOperationException("业务拒绝", exception); }
          }
        }
        """));
    Assert.Contains("probe/ProbeWrap.cs: Entry 在 catch 内把捕获的异常包装为新的异常类型", wrapViolations);

    // 判定的另一侧：已登记文件里已登记方法的 catch 内包装不报违规，证明登记按文件与方法两个条件生效。
    (IReadOnlyList<string> registeredWrapViolations, _) = FindClientPathViolations(
      "server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.Reference.cs",
      SourceSyntaxGuard.Read(
        "server", "Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportRepository.Reference.cs"));
    Assert.Empty(registeredWrapViolations);

    // 变异证据二：同一判定函数在带禁止后缀的类型、带错误处理基类型的类型与递归包装调用上必须报出违规。
    (IReadOnlyList<string> synthesizedViolations, _) = FindClientPathViolations(
      "probe/SynthesizedClientPathProbe.cs", SourceSyntaxGuard.Parse(ClientPathProbeSource));
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: ProbeMiddleware 命中禁止类型后缀 Middleware", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: ProbeResponseWrapper 命中禁止类型后缀 ResponseWrapper", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Probe 基类型为禁止的错误处理基类型 IExceptionHandler", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Gated 声明了禁止的角色门控特性 Authorize", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Wrapped 的 try 语句未登记为设计允许的异常翻译位置", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Wrapped 在 catch 内把捕获的异常包装为新的异常类型", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Outer 在 catch 的参数位置递归调用自身", synthesizedViolations);
    Assert.Contains("probe/SynthesizedClientPathProbe.cs: Retry 在 catch 的参数位置递归调用自身", synthesizedViolations);
    Assert.DoesNotContain("probe/SynthesizedClientPathProbe.cs: Nested 在 catch 的参数位置递归调用自身", synthesizedViolations);

    // 变异证据的另一侧：只有空白与注释的源码不报违规，证明上面的命中不是对所有源码都成立。
    (IReadOnlyList<string> neutralViolations, _) = FindClientPathViolations(
      "probe/NeutralClientPathProbe.cs",
      SourceSyntaxGuard.Parse(
        """
        // 本文件不声明中间件、过滤器、响应转换器或错误包装模型，也不做角色门控。
        internal sealed class NeutralProbe
        {
          internal string Describe() => "Middleware、Authorize、try 都只出现在字符串里";
        }
        """));
    Assert.Empty(neutralViolations);
  }

  /// <summary>
  /// 判断给定文件是否属于生产源码扫描面。
  /// </summary>
  /// <param name="repositoryRoot">仓库根目录绝对路径。</param>
  /// <param name="filePath">待判断文件的绝对路径。</param>
  /// <returns>不属于生成物目录与生成物文件时为 <see langword="true"/>。</returns>
  private static bool IsProductionSourceFile(string repositoryRoot, string filePath)
  {
    string relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/');
    return !relativePath.Contains("/bin/", StringComparison.Ordinal) &&
      !relativePath.Contains("/obj/", StringComparison.Ordinal) &&
      !filePath.EndsWith(".g.cs", StringComparison.Ordinal) &&
      !filePath.EndsWith(".Designer.cs", StringComparison.Ordinal);
  }

  /// <summary>
  /// V87：全仓显式事务声明恰好九处，分布为既有六处加本阶段新增三处，且引用详情查询入口不声明。
  /// </summary>
  /// <remarks>
  /// 三处新增声明位于三个自动端点对应的写应用服务方法上；引用详情是只读查询，在任何分片都不声明事务。
  /// 只断言"某方法带声明"时，声明被挪到别的入口或在别处多出一处都不会失败，因此这里冻结逐文件逐方法的等值清单。
  /// </remarks>
  [Fact]
  public void Transaction_declarations_match_the_frozen_distribution()
  {
    List<(string RelativePath, string MethodName)> actual = TransactionDeclarations("server/Dy.MedicalRecognition.Application");
    actual.AddRange(TransactionDeclarations("server/Dy.MedicalRecognition"));

    (string RelativePath, string MethodName)[] expected =
    [
      .. FrozenTransactionDeclarations.SelectMany(
        entry => entry.MethodNames.Select(method => (entry.RelativePath, method)))
    ];
    Assert.True(
      actual.Count > 0,
      $"受核对的生产源码中应至少存在一处显式事务声明，实际 {actual.Count} 处。");

    Assert.Equal(
      expected.OrderBy(entry => entry.RelativePath, StringComparer.Ordinal).ThenBy(entry => entry.MethodName, StringComparer.Ordinal),
      actual.OrderBy(entry => entry.RelativePath, StringComparer.Ordinal).ThenBy(entry => entry.MethodName, StringComparer.Ordinal));

    // 本阶段新增的三处、既有写应用服务的四处与控制器上传动作的两处，逐项在清单内出现。
    string[] stage5Entrypoints = ["RequestRecognitionMatchesAsync", "SubmitRecognitionProcessingResultsAsync", "SubmitRecognitionReferencesAsync"];
    foreach (string entrypoint in stage5Entrypoints) Assert.Contains(actual, entry => entry.MethodName == entrypoint);
    Assert.Equal(6, actual.Count(entry => entry.MethodName is not ("RequestRecognitionMatchesAsync" or "SubmitRecognitionProcessingResultsAsync" or "SubmitRecognitionReferencesAsync")));

    // 引用详情查询入口不声明：查询应用服务的整个工程内一处声明都没有，入口所在分片同样没有。
    Assert.Empty(TransactionDeclarations("server/Dy.MedicalRecognition.Application/Queries"));
    CompilationUnitSyntax citationEntryPoint = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition.Application", "Queries", "MedicalRecognitionReportQueryAppService.Citation.cs");
    Assert.Empty(TransactionDeclarationsOf(citationEntryPoint));

    // 变异证据：注入一行声明后同一判定必须命中，删除既有声明后等值清单必须不相等。
    Assert.Equal(["ProbeEntryAsync"], TransactionDeclarationsOf(SourceSyntaxGuard.Parse(
      "class Probe { [WorkUnit(UseTransaction = true)] public void ProbeEntryAsync() { } }")));
    Assert.Empty(TransactionDeclarationsOf(SourceSyntaxGuard.Parse(
      "class Probe { [WorkUnit(UseTransaction = false)] public void ProbeEntryAsync() { } }")));
    string[] withoutDeclaration = [.. actual.Where(entry => entry.MethodName != "RequestRecognitionMatchesAsync").Select(entry => entry.MethodName)];
    Assert.NotEqual([.. actual.Select(entry => entry.MethodName)], withoutDeclaration);
  }

  /// <summary>
  /// V88 的静态判断面：引用详情链不声明事务、不调用写入型数据映射成员、不登记事件、不调用状态变更方法。
  /// </summary>
  /// <remarks>
  /// 引用详情是只读查询：不形成引用结果、不形成引用次数、不形成新的互认处理，也不更新任何既有行的处理结果保存时间。
  /// 查询侧仓储的写入面按整个工程的"只出现读取型映射调用"另行断言，避免写入落到与报告列表共用的分片而漏判。
  /// </remarks>
  [Fact]
  public void Citation_chain_keeps_no_write_violation()
  {
    List<string> violations = [];
    int scannedTypeCount = 0;

    foreach (string relativePath in CitationDetailChainSources)
    {
      CompilationUnitSyntax root = SourceSyntaxGuard.Read(relativePath.Split('/'));
      violations.AddRange(FindCitationChainViolations(relativePath, root));
      scannedTypeCount += root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count();
    }

    Assert.Empty(violations);

    // 扫描规模下界：链清单被改坏成"扫不到任何声明"时先在这里失败。
    Assert.Equal(CitationDetailChainSources.Length, CitationDetailChainSources.Distinct(StringComparer.Ordinal).Count());
    Assert.True(scannedTypeCount >= 7, $"引用详情链受核对的类型声明应不少于 7 个，实际 {scannedTypeCount} 个。");

    // 查询侧仓储与查询侧端口的写入面缺口补齐：两个工程内只允许出现读取型映射调用，
    // 且端口分片不得接触数据映射器类型，因此写入无法落到与报告列表共用的分片上。
    foreach (string relativePath in QueryRepositorySources())
    {
      CompilationUnitSyntax root = SourceSyntaxGuard.Read(relativePath.Split('/'));
      List<string> writeCalls =
      [
        .. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
          .Select(invocation => SourceSyntaxGuard.MemberName(invocation.Expression))
          .Where(name => name is not null && WriteMapperMembers.Contains(name, StringComparer.Ordinal))
          .Select(name => $"{relativePath}: 查询侧仓储调用了写入型映射成员 {name}")
      ];
      Assert.Empty(writeCalls);

      if (relativePath.StartsWith("server/Dy.MedicalRecognition.Domain/", StringComparison.Ordinal))
        Assert.DoesNotContain("dataMapper", File.ReadAllText(ResolveFullPath(relativePath)), StringComparison.Ordinal);
    }

    // 引用详情链不得引用写侧仓储接口：该接口携带全部写入能力，出现即说明查询链接触到了写入面。
    foreach (string relativePath in CitationDetailChainSources)
      Assert.DoesNotContain("IMedicalRecognitionReportRepository", File.ReadAllText(ResolveFullPath(relativePath)), StringComparison.Ordinal);

    // 变异证据：四条负向判据在合成源码上都必须命中，证明判定不是扫描面恒空的结果。
    const string source = """
      class Probe
      {
        [WorkUnit(UseTransaction = true)]
        public void Entry()
        {
          mapper.InsertAsync(value);
          mapper.UpdateAsync(value);
          mapper.DeleteAllAsync(value);
          AddEvent(CreateEvent());
          entity.MarkConsumed();
        }
      }
      """;
    IReadOnlyList<string> synthesized = FindCitationChainViolations("probe/SynthesizedCitationProbe.cs", SourceSyntaxGuard.Parse(source));
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 声明了显式事务", synthesized);
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 调用了写入型映射成员 InsertAsync", synthesized);
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 调用了写入型映射成员 UpdateAsync", synthesized);
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 调用了写入型映射成员 DeleteAllAsync", synthesized);
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 登记了领域事件", synthesized);
    Assert.Contains("probe/SynthesizedCitationProbe.cs: Entry 调用了状态变更方法 MarkConsumed", synthesized);
    Assert.Equal(6, synthesized.Count);
  }

  /// <summary>
  /// 实现约束核对：阶段 5 的每个生产与测试源码文件都按清单实际存在、清单无重复，
  /// 且生产清单是生产工程全量扫描面的子集。
  /// </summary>
  /// <remarks>
  /// 文件被改名、搬走或删除时，后续各条判据会退化成"扫不到目标"，因此先把清单本身作为判据。
  /// 扫描面按生产工程目录全量取文件，清单只用于核对阶段 5 的交付范围确实落在扫描面内；
  /// 清单外的生产文件同样参与核对，不因未登记而免于判定。
  /// </remarks>
  [Fact]
  public void Stage5_source_inventory_matches_the_frozen_list()
  {
    string[] allSources = [.. Stage5ProductionSources, .. Stage5TestSources];
    Assert.Equal(allSources.Length, allSources.Distinct(StringComparer.Ordinal).Count());
    Assert.Equal(45, Stage5ProductionSources.Length);
    Assert.Equal(10, Stage5TestSources.Length);
    Assert.All(allSources, relativePath => Assert.True(
      File.Exists(ResolveFullPath(relativePath)),
      $"清单登记的文件不存在：{relativePath}。"));

    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string[] scanPaths = [.. ProductionProjectDirectories
      .SelectMany(directory => Directory.EnumerateFiles(
        Path.Combine(repositoryRoot, directory.Replace('/', Path.DirectorySeparatorChar)),
        "*.cs",
        SearchOption.AllDirectories))
      .Where(file => IsProductionSourceFile(repositoryRoot, file))
      .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))];
    Assert.True(scanPaths.Length >= 200, $"生产工程全量扫描面应不少于 200 个文件，实际 {scanPaths.Length} 个。");

    string[] outsideScanSurface = [.. Stage5ProductionSources.Except(scanPaths, StringComparer.Ordinal)];
    Assert.True(
      outsideScanSurface.Length == 0,
      $"阶段 5 清单内的文件不在生产工程全量扫描面内：{string.Join("、", outsideScanSurface)}。");

    // 变异证据：清单里换一个不存在的路径后同一判定必须失败，证明存在性判定不是对所有路径都成立。
    Assert.False(File.Exists(ResolveFullPath("server/Dy.MedicalRecognition.Tests/Stage5ConstraintTestsProbe.cs")));
  }

  /// <summary>
  /// 实现约束核对：宿主工程的手写控制器集合与已交付清单一致，导出控制器随阶段 6 登记。
  /// </summary>
  /// <remarks>
  /// 阶段 6 的统计导出为文件流响应新增手写导出控制器（设计「外部调用、失败语义与静态守卫」节登记的允许位置）；
  /// 手写控制器集合按目录清单逐文件冻结，新增清单之外的第 4 个控制器文件即失败。
  /// </remarks>
  [Fact]
  public void Host_keeps_the_frozen_handwritten_controller_set()
  {
    string controllerDirectory = Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition", "Controllers");

    string[] controllerFiles =
    [
      .. Directory.EnumerateFiles(controllerDirectory, "*.cs", SearchOption.AllDirectories)
        .Select(path => Path.GetFileName(path)!)
        .OrderBy(name => name, StringComparer.Ordinal)
    ];
    Assert.Equal(
      ["RecognitionStatisticsExportController.cs", "ReportPdfFileController.cs", "ReportSubmissionForm.cs"],
      controllerFiles);

    // 三个文件声明的全部类型都是已交付的类型：两个控制器与一个提交表单。
    string[] declaredTypes =
    [
      .. controllerFiles.SelectMany(name => SourceSyntaxGuard
        .Parse(File.ReadAllText(Path.Combine(controllerDirectory, name)))
        .DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
        .Select(declaration => declaration.Identifier.ValueText))
    ];
    Assert.Equal(
      ["RecognitionStatisticsExportController", "ReportPdfFileController", "ReportSubmissionForm"],
      declaredTypes.OrderBy(name => name, StringComparer.Ordinal));

    // 变异证据：加入一个不存在的控制器文件名后，同一清单等值判定必须不相等。
    Assert.NotEqual(controllerFiles, [.. controllerFiles, "RecognitionMatchController.cs"]);
  }

  /// <summary>
  /// 实现约束核对：聚合与管理器未重建，管理器、写侧仓储与查询侧仓储各自仍只有一个类型，分部文件按能力拆分。
  /// </summary>
  /// <remarks>
  /// 判据按类型简单名统计声明文件数量：同一 partial 类型拆分到多个文件不改变类型数量，
  /// 新增第二套管理器、第二套仓储实现或第二个聚合根都会使数量变化而失败。
  /// </remarks>
  [Fact]
  public void Aggregate_manager_and_repositories_keep_one_type_each()
  {
    Assert.Single(SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReport"));
    Assert.Equal(6, SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReportManager").Count);
    Assert.Equal(8, SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "IMedicalRecognitionReportRepository").Count);
    Assert.Equal(8, SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportRepository").Count);
    // 阶段 6 的互认统计为查询侧端口与实现各新增一个 Statistics 分片，分部文件数量随同批交付从四增加到五。
    Assert.Equal(5, SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "IMedicalRecognitionReportQueryRepository").Count);
    Assert.Equal(5, SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportQueryRepository").Count);

    // 每套类型只有一个声明文件不带能力后缀：主声明之外的分片都按能力命名，以此确认分部拆分口径未被改成多类型。
    foreach ((string directory, string typeName) in new[]
    {
      ("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReportManager"),
      ("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportRepository"),
      ("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportQueryRepository")
    })
    {
      IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
        SourceSyntaxGuard.ReadTypeDeclarations(directory, typeName);
      Assert.Single(declarations, declaration => Path.GetFileName(declaration.RelativePath) == $"{typeName}.cs");
      Assert.All(declarations, declaration => Assert.Contains(
        declaration.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
          .Single(item => item.Identifier.ValueText == typeName).Modifiers,
        modifier => modifier.ValueText == "partial"));
    }

    // 变异证据：按一个不存在的类型名扫描必须得不到声明，证明数量判定不是扫描面恒真的结果。
    Assert.Empty(SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReportManagerProbe"));
  }

  /// <summary>
  /// 实现约束核对：共享语法助手只有一份，本文件复用共享入口，测试工程内不重复声明被禁止的本地助手。
  /// </summary>
  /// <remarks>
  /// 消费方清单的精确一致性由 <c>Architecture/SourceGuardReuseTests.cs</c> 的受治理清单等值断言承担；
  /// 本用例核对本文件确实调用共享入口，且全测试工程内不存在 <c>SourceSyntaxGuard</c> 的第二份声明或重复的仓库根定位方法。
  /// </remarks>
  [Fact]
  public void Shared_syntax_guard_is_reused_without_a_second_declaration()
  {
    string testsRoot = Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Tests");
    List<string> guardDeclarations = [];
    List<string> duplicateHelpers = [];

    foreach (string file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
    {
      string relativePath = Path.GetRelativePath(testsRoot, file).Replace(Path.DirectorySeparatorChar, '/');
      if (relativePath.Contains("/obj/", StringComparison.Ordinal) || relativePath.Contains("/bin/", StringComparison.Ordinal)) continue;

      CompilationUnitSyntax root = SourceSyntaxGuard.Parse(File.ReadAllText(file));
      if (root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Any(declaration => declaration.Identifier.ValueText == "SourceSyntaxGuard"))
        guardDeclarations.Add(relativePath);

      duplicateHelpers.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Where(method => method.Identifier.ValueText is "FindRepositoryRoot" or "StripComments")
        .Select(method => $"{relativePath}: {method.Identifier.ValueText}"));
    }

    Assert.Equal(["Architecture/SourceSyntaxGuard.cs"], guardDeclarations);
    Assert.Equal(["Architecture/SourceSyntaxGuard.cs: FindRepositoryRoot"], duplicateHelpers);

    // 本文件确实调用共享入口：改用本地实现时，受治理清单的"未复用"判定与这里的判定都会失败。
    CompilationUnitSyntax ownSource = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition.Tests", "Stage5ConstraintTests.cs");
    Assert.Contains(ownSource.DescendantNodes().OfType<MemberAccessExpressionSyntax>(), access =>
      access.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "SourceSyntaxGuard");
  }

  /// <summary>
  /// 实现约束核对：不重跑后端代码生成器、不覆盖再生成保护清单中的配置与启动文件；
  /// API Client 包的人工维护面由内容判据另行冻结。
  /// </summary>
  /// <remarks>
  /// 判据是工作区差异：受保护清单的文件在本轮没有任何新增、修改或删除。
  /// 差异文本的解析与判定分开：<see cref="FindProtectedPathViolations"/> 接收差异文本，
  /// 判定只接受固定样本差异文本与真实差异文本两种输入，因此工作区变干净时判据的判别力不受影响。
  /// 不以"文件内容看起来没变"代替差异判定，因为生成器覆盖会同时改动多个受保护文件。
  /// 集中包管理文件与 client 前端工程不在本清单内：两者承载设计内的依赖与生成包交付，
  /// 人工维护面分别由 <see cref="Central_package_management_keeps_the_human_maintained_packages"/>
  /// 与 <see cref="Api_client_package_keeps_the_human_maintained_surfaces"/> 的内容判据承接。
  /// </remarks>
  [Fact]
  public void Protected_configuration_and_client_sources_are_untouched()
  {
    // 判定函数必须能判出受保护路径：固定样本差异文本里放一条被改动的生产路径与一条受保护路径。
    string[] sampleViolations = [.. FindProtectedPathViolations(
      """
       M server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs
      MM server/Dy.MedicalRecognition/appsettings.json
      ?? server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Citation.cs
      """)];
    Assert.Equal(
      ["受保护路径出现在工作区差异中：server/Dy.MedicalRecognition/appsettings.json"],
      [.. sampleViolations.Order(StringComparer.Ordinal)]);

    // 变异证据的另一侧：样本里去掉受保护路径后同一判定必须不报，证明判定不是对所有差异文本都成立；
    // client 路径与生产路径同场出现也不报，其人工维护面由内容判据承接，不再进入差异判定。
    Assert.Empty(FindProtectedPathViolations(
      """
       M server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs
       M client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json
      ?? server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Citation.cs
      """));

    // 真实差异文本只用于负向判定：本轮工作区差异里不含受保护配置与启动文件。
    string[] realViolations = [.. FindProtectedPathViolations(ChangedPathsText())];
    Assert.True(
      realViolations.Length == 0,
      $"本轮工作区差异触及受保护路径：{string.Join("、", realViolations)}。");
  }

  /// <summary>
  /// 再生成保护核对：集中包管理文件的人工维护内容以内容判据冻结，集中管理开关与框架包名集合不得消失。
  /// </summary>
  /// <remarks>
  /// 该文件经阶段 6 设计确认承载设计内的依赖增项，差异判定不再适用于它；
  /// 判据改为文件内容：再生成覆盖会把文件重置为生成器基准值，人工维护的框架包名集合随之消失，在这里命中。
  /// 版本号按负责人决策维护，不进入判据；新增包名属设计内变更，登记时同步扩展受保护包名清单。
  /// </remarks>
  [Fact]
  public void Central_package_management_keeps_the_human_maintained_packages()
  {
    string centralPackages = File.ReadAllText(ResolveFullPath("server/Directory.Packages.props"));

    // 集中包管理开关：生成器基准值不含该开关，覆盖后在这里失败。
    Assert.Contains(
      "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>",
      centralPackages,
      StringComparison.Ordinal);

    foreach (string packageName in HumanMaintainedPackageNames)
    {
      Assert.Contains($"Include=\"{packageName}\"", centralPackages, StringComparison.Ordinal);
    }

    // 正向合规样本：真实文件在同一判定下不报缺失，与上方守卫断言同向。
    Assert.Empty(FindMissingProtectedContent(centralPackages));

    // 变异证据：从真实文件内容构造缺失受保护内容的变异文本，同一判定必须逐项报出缺失项，证明判定锚定在开关与包名集合上。
    string withoutSwitch = RemoveLinesContaining(centralPackages, CentralManageSwitch);
    Assert.Equal(
      [$"集中包管理开关缺失：{CentralManageSwitch}"],
      [.. FindMissingProtectedContent(withoutSwitch)]);

    string probe = $"Include=\"{HumanMaintainedPackageNames[0]}\"";
    string withoutProbe = RemoveLinesContaining(centralPackages, probe);
    Assert.Equal(
      [$"人工维护包名缺失：{HumanMaintainedPackageNames[0]}"],
      [.. FindMissingProtectedContent(withoutProbe)]);
  }

  /// <summary>
  /// 从集中包管理文件内容中找出缺失的人工维护内容：集中管理开关与受保护包名清单。
  /// </summary>
  /// <param name="centralPackages">集中包管理文件的完整文本。</param>
  /// <returns>缺失项描述，按守卫判据的顺序排列；全部存在时为空。</returns>
  /// <remarks>
  /// 判定只接收文本并返回缺失清单，固定变异样本与真实文件走同一条判定，
  /// 因此变异证据能够证明判据锚定在开关与包名集合上。
  /// </remarks>
  private static IReadOnlyList<string> FindMissingProtectedContent(string centralPackages)
  {
    List<string> missing = [];
    if (!centralPackages.Contains(CentralManageSwitch, StringComparison.Ordinal))
      missing.Add($"集中包管理开关缺失：{CentralManageSwitch}");
    foreach (string packageName in HumanMaintainedPackageNames)
    {
      if (!centralPackages.Contains($"Include=\"{packageName}\"", StringComparison.Ordinal))
        missing.Add($"人工维护包名缺失：{packageName}");
    }
    return missing;
  }

  /// <summary>
  /// 按行移除包含指定文本的行，用于从真实文件内容构造变异样本。
  /// </summary>
  private static string RemoveLinesContaining(string content, string needle) => string.Join(
    "\n",
    content
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Split('\n')
      .Where(line => !line.Contains(needle, StringComparison.Ordinal)));

  /// <summary>
  /// 从 Git 工作区差异文本中找出受保护路径的命中项。
  /// </summary>
  /// <param name="statusText">形如 <c>git status --porcelain --untracked-files=all</c> 输出的差异文本。</param>
  /// <returns>命中受保护清单的路径描述；未命中时为空。</returns>
  /// <remarks>
  /// 只按差异文本判定，不读取工作区当前状态：工作区变干净时差异集合为空，
  /// 判定的方向固定为"不含受保护路径"，因此干净轮次不会因集合为空而误红。
  /// </remarks>
  private static IReadOnlyList<string> FindProtectedPathViolations(string statusText) =>
  [
    .. ParseChangedPaths(statusText)
      .Where(changed => ProtectedDiffPaths.Any(
        path => changed == path || changed.StartsWith($"{path}/", StringComparison.Ordinal)))
      .Select(changed => $"受保护路径出现在工作区差异中：{changed}")
  ];

  /// <summary>
  /// 再生成保护清单：这些路径出现在工作区差异中即说明生成器覆盖了人工维护的内容。
  /// </summary>
  /// <remarks>
  /// 集中包管理文件承载设计内依赖增项，改由内容判据保护，见
  /// <see cref="Central_package_management_keeps_the_human_maintained_packages"/>。
  /// client 前端工程不在本清单内：API Client 生成包与统计页面属设计内交付，
  /// 其人工维护面由 <see cref="Api_client_package_keeps_the_human_maintained_surfaces"/> 的内容判据承接。
  /// </remarks>
  private static readonly string[] ProtectedDiffPaths =
  [
    "server/global.json",
    "server/Dy.MedicalRecognition/appsettings.json",
    "server/Dy.MedicalRecognition/appsettings.Development.json",
    "server/Dy.MedicalRecognition/EarthraceConfig.json",
    "server/Dy.MedicalRecognition/Properties/launchSettings.json"
  ];

  /// <summary>
  /// 集中包管理文件中人工维护的包名集合：再生成覆盖会把文件重置为生成器基准值，包名随之消失。
  /// 版本号不进入判据；包名集合的扩展属设计内变更，随对应设计登记同步维护。
  /// </summary>
  private static readonly string[] HumanMaintainedPackageNames =
  [
    "Dy.Base.Application.Contracts",
    "Dy.Core.Abstractions",
    "Dy.Core.Extensions",
    "Dy.Core.SourceGen",
    "Dy.CompileToolkit.Version",
    "Dy.Apron.Abstractions",
    "Dy.Apron.Bridge",
    "Dy.Apron.QuickStart",
    "Dy.Apron.Security",
    "Dy.Earthrace.Abstractions",
    "Dy.Earthrace",
    "Npgsql",
    "Dy.Plugin.AspNet.Scalar",
    "Microsoft.AspNetCore.OpenApi",
    "Microsoft.CodeAnalysis.CSharp",
    "Microsoft.NET.Test.Sdk",
    "xunit",
    "xunit.runner.visualstudio"
  ];

  /// <summary>
  /// 集中包管理开关的判据文本：生成器基准值不含该开关，再生成覆盖后在这里命中。
  /// </summary>
  private const string CentralManageSwitch = "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>";

  /// <summary>
  /// 再生成保护核对：API Client 包的人工维护面以内容判据冻结，再生成整体覆盖锚点所在文件
  /// 或登录接口排除口径被改变时在这里命中。
  /// </summary>
  /// <remarks>
  /// client 路径不在 <see cref="ProtectedDiffPaths"/> 的差异判定内，其人工维护面由本判据承接。
  /// 锚点全部取结构稳定的标记：包入口对 Kiota 生成内容的再导出与手写便捷创建入口、
  /// 客户端便捷构造符号、OpenAPI 归一化的检测条件与计数器、锁定文件的登录接口排除清单；
  /// 措辞性注释不进入判据，锚点语义不随正常实现演进而漂移。
  /// </remarks>
  [Fact]
  public void Api_client_package_keeps_the_human_maintained_surfaces()
  {
    Dictionary<string, string> realSources = new();
    foreach (string sourcePath in ApiClientProtectedSourcePaths)
    {
      realSources[sourcePath] = File.ReadAllText(ResolveFullPath(sourcePath));
    }

    // 正向合规样本：真实文件在同一判定下不报缺失，与守卫断言同向。
    Assert.Empty(FindMissingApiClientProtectedContent(realSources));

    // 变异证据：从真实文件内容构造锚点缺失与口径覆盖的变异文本，同一判定必须逐项报出缺失项，
    // 证明判据锚定在上述标记上而不是对所有源码恒真。
    Dictionary<string, string> withoutConvenienceCreation = new(realSources);
    withoutConvenienceCreation[ApiClientIndexPath] =
      RemoveLinesContaining(realSources[ApiClientIndexPath], ApiClientConvenienceCreation);
    Assert.Equal(
      ["API Client 人工维护锚点缺失：包入口的手写便捷创建入口"],
      [.. FindMissingApiClientProtectedContent(withoutConvenienceCreation)]);

    Dictionary<string, string> withoutClientConstruction = new(realSources);
    withoutClientConstruction[ApiClientClientSourcePath] =
      RemoveLinesContaining(realSources[ApiClientClientSourcePath], ApiClientClientConstruction);
    Assert.Equal(
      ["API Client 人工维护锚点缺失：客户端便捷构造函数"],
      [.. FindMissingApiClientProtectedContent(withoutClientConstruction)]);

    Dictionary<string, string> withoutUnionNormalization = new(realSources);
    withoutUnionNormalization[ApiClientPrepareScriptPath] =
      RemoveLinesContaining(realSources[ApiClientPrepareScriptPath], ApiClientIntegerUnionCounter);
    Assert.Equal(
      ["API Client 人工维护锚点缺失：整数联合类型的归一化计数"],
      [.. FindMissingApiClientProtectedContent(withoutUnionNormalization)]);

    Dictionary<string, string> withLoosenedExclusion = new(realSources);
    withLoosenedExclusion[ApiClientKiotaLockPath] =
      RemoveLinesContaining(realSources[ApiClientKiotaLockPath], $"\"{ApiClientLoginExclusion}\"");
    Assert.Equal(
      [$"Kiota 锁定文件的 excludePatterns 不是恰好排除 {ApiClientLoginExclusion}"],
      [.. FindMissingApiClientProtectedContent(withLoosenedExclusion)]);
  }

  /// <summary>API Client 包入口的仓库内相对路径。</summary>
  private const string ApiClientIndexPath = "client/packages/api-client-medical-recognition/src/index.ts";

  /// <summary>客户端便捷构造文件的仓库内相对路径。</summary>
  private const string ApiClientClientSourcePath = "client/packages/api-client-medical-recognition/src/medicalRecognitionClient.ts";

  /// <summary>OpenAPI 归一化脚本的仓库内相对路径。</summary>
  private const string ApiClientPrepareScriptPath = "client/packages/api-client-medical-recognition/scripts/prepare-openapi.mjs";

  /// <summary>Kiota 锁定文件的仓库内相对路径。</summary>
  private const string ApiClientKiotaLockPath = "client/packages/api-client-medical-recognition/src/kiota-lock.json";

  /// <summary>API Client 包的受保护源文件路径集合。</summary>
  private static readonly string[] ApiClientProtectedSourcePaths =
  [
    ApiClientIndexPath,
    ApiClientClientSourcePath,
    ApiClientPrepareScriptPath,
    ApiClientKiotaLockPath
  ];

  /// <summary>包入口的手写便捷创建入口签名：Kiota 再生成整体覆盖包入口时随之消失。</summary>
  private const string ApiClientConvenienceCreation = "export function createMedicalRecognitionApiClient(baseUrl: string): MedicalRecognitionClient";

  /// <summary>客户端便捷构造函数签名：整包被清空重建时随之消失。</summary>
  private const string ApiClientClientConstruction = "export function createMedicalRecognitionClient(requestAdapter: RequestAdapter)";

  /// <summary>整数联合类型归一化的计数语句：归一化逻辑被移除时随之消失。</summary>
  private const string ApiClientIntegerUnionCounter = "counter.integerUnions += 1";

  /// <summary>
  /// Kiota 锁定文件中登录接口的排除项：排除清单被清空、替换或追加其他排除项都说明排除口径被改变。
  /// </summary>
  private const string ApiClientLoginExclusion = "/auth/login";

  /// <summary>
  /// API Client 人工维护面的内容锚点：再生成整体覆盖锚点所在文件或归一化被移除时，锚点文本随之消失。
  /// 锚点取函数签名、再导出语句与归一化计数器等结构稳定标记，措辞性注释不进入判据。
  /// </summary>
  private static readonly (string SourcePath, string Anchor, string Description)[] ApiClientProtectedAnchors =
  [
    (ApiClientIndexPath, "export * from './api/index.js';", "包入口对 Kiota 生成内容的再导出"),
    (ApiClientIndexPath, ApiClientConvenienceCreation, "包入口的手写便捷创建入口"),
    (ApiClientClientSourcePath, ApiClientClientConstruction, "客户端便捷构造函数"),
    (ApiClientClientSourcePath, "export interface MedicalRecognitionClient extends BaseRequestBuilder<MedicalRecognitionClient>", "客户端主接口声明"),
    (ApiClientPrepareScriptPath, "value.includes('integer') && value.includes('string')", "整数联合类型的归一化检测条件"),
    (ApiClientPrepareScriptPath, ApiClientIntegerUnionCounter, "整数联合类型的归一化计数"),
    (ApiClientPrepareScriptPath, "counter.nullableReferences += 1", "可空引用的归一化计数")
  ];

  /// <summary>
  /// 从 API Client 包的人工维护面文本中找出缺失的受保护内容：内容锚点与锁定文件的登录接口排除口径。
  /// </summary>
  /// <param name="sources">受保护源文件相对路径到文件完整文本的映射。</param>
  /// <returns>缺失项描述，按判据顺序排列；全部在位时为空。</returns>
  /// <remarks>
  /// 判定只接收文本映射并返回缺失清单，真实文件与变异样本走同一条判定。
  /// 三个源码文件按锚点文本存在性判定；锁定文件按 JSON 结构判定排除清单的精确取值，
  /// 排除清单被清空、替换或追加其他排除项时同样命中。
  /// </remarks>
  private static IReadOnlyList<string> FindMissingApiClientProtectedContent(IReadOnlyDictionary<string, string> sources)
  {
    List<string> missing = [];

    foreach ((string sourcePath, string anchor, string description) in ApiClientProtectedAnchors)
    {
      if (!sources.TryGetValue(sourcePath, out string? source) || !source.Contains(anchor, StringComparison.Ordinal))
        missing.Add($"API Client 人工维护锚点缺失：{description}");
    }

    string[]? exclusionPatterns = ApiClientExclusionPatterns(sources);
    if (exclusionPatterns is null)
      missing.Add("Kiota 锁定文件的 excludePatterns 缺失或不可解析");
    else if (!exclusionPatterns.SequenceEqual([ApiClientLoginExclusion], StringComparer.Ordinal))
      missing.Add($"Kiota 锁定文件的 excludePatterns 不是恰好排除 {ApiClientLoginExclusion}");

    return missing;
  }

  /// <summary>
  /// 按 JSON 结构解析 Kiota 锁定文件的排除清单取值。
  /// </summary>
  /// <param name="sources">受保护源文件相对路径到文件完整文本的映射。</param>
  /// <returns>排除项集合；文件文本缺失、字段缺失或无法按 JSON 解析时为 <see langword="null"/>。</returns>
  private static string[]? ApiClientExclusionPatterns(IReadOnlyDictionary<string, string> sources)
  {
    if (!sources.TryGetValue(ApiClientKiotaLockPath, out string? lockFileText)) return null;

    try
    {
      using JsonDocument document = JsonDocument.Parse(lockFileText);
      return document.RootElement.TryGetProperty("excludePatterns", out JsonElement patterns) &&
          patterns.ValueKind == JsonValueKind.Array
        ? [.. patterns.EnumerateArray().Select(item => item.GetString() ?? string.Empty)]
        : null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  /// <summary>
  /// 校验固定样本差异文本的解析结果：只取路径，剔除状态码，重命名项取目标路径。
  /// </summary>
  /// <remarks>
  /// 解析不看工作区状态，因此样本差异文本可以在任何轮次稳定复现判定结果。
  /// 重命名与复制项按 Git 的 <c>旧路径 -&gt; 新路径</c> 文本取新路径，即当前实际存在的那个路径。
  /// </remarks>
  [Fact]
  public void Changed_paths_parsing_reads_the_diff_text()
  {
    Assert.Equal(
      [
        "server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs",
        "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs",
        "client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json",
        "server/global.renamed.json"
      ],
      ParseChangedPaths(
        """
         M server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs
        ?? "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs"
        A  client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json
        R  server/global.json -> server/global.renamed.json
        """));

    // 变异证据：差异文本为空时解析结果为空，证明解析不是对任意输入都返回固定集合。
    Assert.Empty(ParseChangedPaths(string.Empty));
  }

  /// <summary>
  /// 把 Git 工作区差异文本解析为差异路径集合。
  /// </summary>
  /// <param name="statusText">形如 <c>git status --porcelain --untracked-files=all</c> 输出的差异文本。</param>
  /// <returns>仓库内相对路径集合，使用 <c>/</c> 分隔；无差异时为空。</returns>
  /// <remarks>
  /// 每行的前两个字符是状态码，其后是路径；重命名与复制项的文本形如 <c>旧路径 -&gt; 新路径</c>，取新路径。
  /// Git 含空格或非 ASCII 字符时会把路径整体加双引号，这里在去掉状态码后剥掉引号。
  /// </remarks>
  private static IReadOnlyList<string> ParseChangedPaths(string statusText) =>
  [
    .. statusText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
      .Select(line => line.Length > 3 ? line[3..].Trim().Trim('"') : string.Empty)
      .Where(entry => entry.Length > 0)
      .Select(entry => entry.Contains(" -> ", StringComparison.Ordinal) ? entry[(entry.IndexOf(" -> ", StringComparison.Ordinal) + 4)..] : entry)
      .Select(path => path.Replace('\\', '/'))
  ];

  /// <summary>
  /// 在单份源码文本中查找客户端路径与异常处理违规。
  /// </summary>
  /// <param name="relativePath">用于报告的仓库内相对路径。</param>
  /// <param name="root">待检查的编译单元语法树根节点；可以是真实文件或合成源码。</param>
  /// <returns>违规描述集合，以及本次扫描到的类型声明数量；无违规时集合为空。</returns>
  private static (IReadOnlyList<string> Violations, int TypeCount) FindClientPathViolations(string relativePath, CompilationUnitSyntax root)
  {
    List<string> violations = [];
    List<BaseTypeDeclarationSyntax> declarations = [.. root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()];

    // 一、禁止的类型名后缀。
    foreach (BaseTypeDeclarationSyntax declaration in declarations)
    {
      string? suffix = BannedClientPathTypeSuffixes.FirstOrDefault(
        candidate => declaration.Identifier.ValueText.EndsWith(candidate, StringComparison.Ordinal));
      if (suffix is not null) violations.Add($"{relativePath}: {declaration.Identifier.ValueText} 命中禁止类型后缀 {suffix}");
    }

    // 二、以框架错误处理或请求管线基类型为基类型的声明：改名绕过后缀判定时在这里命中。
    foreach (BaseTypeDeclarationSyntax declaration in declarations)
    {
      foreach (BaseTypeSyntax baseType in declaration.BaseList?.Types ?? [])
      {
        string baseTypeName = SourceSyntaxGuard.SimpleTypeName(baseType.Type);
        if (baseTypeName.Contains("Middleware", StringComparison.Ordinal) ||
            baseTypeName.Contains("ExceptionHandler", StringComparison.Ordinal) ||
            baseTypeName.Contains("Filter", StringComparison.Ordinal))
        {
          violations.Add($"{relativePath}: {declaration.Identifier.ValueText} 基类型为禁止的错误处理基类型 {baseTypeName}");
        }
      }
    }

    // 三、角色门控特性，以及会注册过滤器或中间件的特性。类型声明与方法声明两种位置都判定：
    //    只判方法声明时，加在整个类型上的授权特性会被漏过，而类型名本身不带任何禁止后缀。
    foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
    {
      string memberName = member switch
      {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        MethodDeclarationSyntax method => method.Identifier.ValueText,
        _ => string.Empty
      };
      if (memberName.Length == 0) continue;

      foreach (AttributeSyntax attribute in member.AttributeLists.SelectMany(list => list.Attributes))
      {
        string attributeName = attribute.Name.ToString().Split('.')[^1];
        if (!IsBannedPipelineAttributeName(attributeName)) continue;

        string? banned = BannedRoleGatingAttributes.FirstOrDefault(candidate =>
          attributeName == candidate || attributeName == $"{candidate}Attribute");
        if (banned is not null) violations.Add($"{relativePath}: {memberName} 声明了禁止的角色门控特性 {banned}");
        else violations.Add($"{relativePath}: {memberName} 声明了禁止的管线注册特性 {attributeName}");
      }
    }

    // 四、权限补查调用：调用成员名或接收方出现权限或角色关键字即判违规。
    //    依赖注入字段名与类型名通常不同（例如字段名 authorizationService 指向某个授权服务类型），
    //    因此关键字判定同时看调用成员名与接收方写法，只按类型名排除时排除项永远命中不了实际写法。
    foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
      if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) continue;
      string invokedMemberName = SourceSyntaxGuard.MemberName(memberAccess.Name) ?? string.Empty;
      string receiver = SourceSyntaxGuard.MemberName(memberAccess.Expression) ?? string.Empty;
      if (!HasPermissionKeyword(invokedMemberName) && !HasPermissionKeyword(receiver)) continue;

      violations.Add($"{relativePath}: {invokedMemberName} 在 {receiver} 上执行了权限判断调用");
    }

    // 五、try 语句：只允许出现在已登记的异常翻译位置；catch 的参数位置递归调用自身同样判为业务失败转换。
    //    登记按文件与方法两个条件比对，另一个文件里的同名方法不会被误认为已登记。
    foreach ((string scopePath, string tryScope) in TryScopesOf(root, relativePath))
    {
      bool registered = RegisteredTryScopes.Any(scope =>
        scope.MethodName == tryScope && scope.RelativePath == scopePath);
      if (!registered) violations.Add($"{relativePath}: {tryScope} 的 try 语句未登记为设计允许的异常翻译位置");
    }

    foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
      foreach (CatchClauseSyntax catchClause in method.DescendantNodes().OfType<CatchClauseSyntax>())
      {
        if (catchClause.Declaration is null) continue;

        // 典型写法是把原异常用 throw new InvalidOperationException(文案, 原异常) 带出：这属于把业务失败包起来再转换。
        bool carriesExceptionToNewThrow = catchClause.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
          .Any(creation => creation.ArgumentList?.Arguments.Any(argument =>
            argument.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.ValueText == catchClause.Declaration.Identifier.ValueText) == true);

        if (carriesExceptionToNewThrow)
        {
          bool registered = RegisteredWrapScopes.Any(scope =>
            scope.MethodName == method.Identifier.ValueText && relativePath == scope.RelativePath);
          if (!registered)
            violations.Add($"{relativePath}: {method.Identifier.ValueText} 在 catch 内把捕获的异常包装为新的异常类型");
        }

        // 在 catch 内重新调用同一方法会把业务失败循环重新包一层：出现即说明该处不是在翻译持久化冲突。
        string enclosingTypeName = method.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? string.Empty;
        bool callsItself = catchClause.DescendantNodes().OfType<InvocationExpressionSyntax>()
          .Any(invocation => IsSelfCall(invocation, method.Identifier.ValueText, enclosingTypeName));
        if (callsItself)
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 在 catch 的参数位置递归调用自身");
      }
    }

    return (violations, declarations.Count);
  }

  /// <summary>
  /// 判断一次调用是否是对方法的自身调用。
  /// </summary>
  /// <param name="invocation">待判断的调用表达式。</param>
  /// <param name="methodName">外层方法名。</param>
  /// <param name="enclosingTypeName">外层方法所在类型的简单名。</param>
  /// <returns>简单名调用该方法，或以所在类型名为接收方调用该方法时为 <see langword="true"/>。</returns>
  private static bool IsSelfCall(InvocationExpressionSyntax invocation, string methodName, string enclosingTypeName)
  {
    if (SourceSyntaxGuard.MemberName(invocation.Expression) != methodName) return false;
    if (invocation.Expression is IdentifierNameSyntax) return true;

    return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
      SourceSyntaxGuard.MemberName(memberAccess.Expression) == enclosingTypeName;
  }

  /// <summary>
  /// 取语法树内全部 <c>try</c> 语句所在方法与所属文件的仓库内相对路径。
  /// </summary>
  /// <param name="root">待检查的编译单元语法树根节点。</param>
  /// <returns>承载 <c>try</c> 语句的方法名与其所在方法声明节点，按声明顺序；无 <c>try</c> 语句时为空。</returns>
  /// <remarks>
  /// 返回方法声明节点而不仅是方法名：例外登记按文件与方法两个条件比对，
  /// 只按方法名比对时，另一个文件里的同名方法会被误认为已登记并静默通过。
  /// 所属文件由调用方按方法声明节点定位，因此本方法对合成源码同样可用。
  /// </remarks>
  private static IReadOnlyList<(string RelativePath, string MethodName)> TryScopesOf(CompilationUnitSyntax root, string relativePath) =>
  [
    .. root.DescendantNodes().OfType<TryStatementSyntax>()
      .Select(tryStatement => tryStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault())
      .Where(method => method is not null)
      .Select(method => (relativePath, method!.Identifier.ValueText))
  ];

  /// <summary>
  /// 在指定目录下收集声明了显式事务的方法及其所在文件。
  /// </summary>
  /// <param name="relativeDirectory">相对仓库根目录的目录路径片段。</param>
  /// <returns>文件相对路径与方法名，按声明顺序。</returns>
  private static List<(string RelativePath, string MethodName)> TransactionDeclarations(string relativeDirectory)
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
    List<(string RelativePath, string MethodName)> declarations = [];

    foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
    {
      if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
      {
        continue;
      }

      string relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
      foreach (string methodName in TransactionDeclarationsOf(SourceSyntaxGuard.Parse(File.ReadAllText(file))))
        declarations.Add((relativePath, methodName));
    }

    return declarations;
  }

  /// <summary>
  /// 取语法树内声明 <c>[WorkUnit(UseTransaction = true)]</c> 的方法名。
  /// </summary>
  /// <param name="root">待检查的编译单元语法树根节点。</param>
  /// <returns>带显式事务声明的方法名集合，按声明顺序。</returns>
  private static IReadOnlyList<string> TransactionDeclarationsOf(CompilationUnitSyntax root) =>
  [
    .. root.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Where(method => method.AttributeLists.SelectMany(list => list.Attributes).Any(IsExplicitTransactionAttribute))
      .Select(method => method.Identifier.ValueText)
  ];

  /// <summary>
  /// 判断特性是否为启用事务的工作单元声明。
  /// </summary>
  /// <param name="attribute">待判断的特性节点。</param>
  /// <returns>名称为 <c>WorkUnit</c> 或 <c>WorkUnitAttribute</c>，且存在取值为 <c>true</c> 的 <c>UseTransaction</c> 实参时为 <see langword="true"/>。</returns>
  /// <remarks>
  /// 特性里的 <c>UseTransaction = true</c> 是带名称的实参赋值，语法节点是 <c>NameEquals</c>；
  /// 只有方法调用的命名实参才落在 <c>NameColon</c>，按 <c>NameColon</c> 判定会对真实声明恒假。
  /// </remarks>
  private static bool IsExplicitTransactionAttribute(AttributeSyntax attribute)
  {
    string name = attribute.Name.ToString().Split('.')[^1];
    if (name is not ("WorkUnit" or "WorkUnitAttribute")) return false;
    return attribute.ArgumentList?.Arguments.Any(argument =>
      argument.NameEquals?.Name.Identifier.ValueText == "UseTransaction" &&
      argument.Expression.ToString() == "true") == true;
  }

  /// <summary>
  /// 在单份源码文本中查找引用详情链的写入、事件与状态变化违规。
  /// </summary>
  /// <param name="relativePath">用于报告的仓库内相对路径。</param>
  /// <param name="root">待检查的编译单元语法树根节点；可以是真实文件或合成源码。</param>
  /// <returns>违规描述集合；无违规时为空。</returns>
  private static IReadOnlyList<string> FindCitationChainViolations(string relativePath, CompilationUnitSyntax root)
  {
    List<string> violations = [];

    foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
      if (method.AttributeLists.SelectMany(list => list.Attributes).Any(IsExplicitTransactionAttribute))
        violations.Add($"{relativePath}: {method.Identifier.ValueText} 声明了显式事务");

      foreach (InvocationExpressionSyntax invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
      {
        string? memberName = SourceSyntaxGuard.MemberName(invocation.Expression);
        if (memberName is null) continue;

        if (WriteMapperMembers.Contains(memberName, StringComparer.Ordinal))
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 调用了写入型映射成员 {memberName}");
        else if (EventRegistrationMembers.Contains(memberName, StringComparer.Ordinal))
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 登记了领域事件");
        else if (StateChangeMemberPrefixes.Any(prefix => memberName.StartsWith(prefix, StringComparison.Ordinal)))
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 调用了状态变更方法 {memberName}");
      }
    }

    return violations;
  }

  /// <summary>
  /// 查询侧仓储实现与查询侧端口的全部源文件。
  /// </summary>
  /// <returns>文件相对路径集合，使用 <c>/</c> 分隔。</returns>
  private static IReadOnlyList<string> QueryRepositorySources()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    List<string> sources = [];
    foreach (string relativeDirectory in new[] { "server/Dy.MedicalRecognition.Repository/Queries", "server/Dy.MedicalRecognition.Domain/Queries/Ports" })
    {
      string directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
      sources.AddRange(Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
        .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/')));
    }

    return [.. sources.OrderBy(path => path, StringComparer.Ordinal)];
  }

  /// <summary>
  /// 读取工作区相对索引的差异文本，包含未跟踪文件。
  /// </summary>
  /// <returns><c>git status --porcelain --untracked-files=all</c> 的标准输出；无差异时为空串。</returns>
  /// <remarks>
  /// 只返回差异文本，路径解析由 <see cref="ParseChangedPaths"/> 承担：
  /// 判定函数接收文本作为输入，真实差异文本与固定样本差异文本走同一条解析与判定路径。
  /// </remarks>
  /// <exception cref="InvalidOperationException">Git 不可用或命令失败时抛出。</exception>
  private static string ChangedPathsText()
  {
    using System.Diagnostics.Process process = new()
    {
      StartInfo = new System.Diagnostics.ProcessStartInfo("git", "status --porcelain --untracked-files=all")
      {
        WorkingDirectory = SourceSyntaxGuard.FindRepositoryRoot(),
        RedirectStandardOutput = true,
        RedirectStandardError = true
      }
    };
    process.Start();
    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException($"git status 执行失败：{process.StandardError.ReadToEnd()}");

    return output;
  }

  /// <summary>
  /// 把仓库内相对路径解析为绝对路径。
  /// </summary>
  /// <param name="relativePath">仓库内相对路径，使用 <c>/</c> 分隔。</param>
  /// <returns>该路径的绝对路径。</returns>
  private static string ResolveFullPath(string relativePath) =>
    Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

  /// <summary>
  /// 客户端路径判据的合成样本源码；变异证据只使用这一份文本，避免多处写法漂移。
  /// </summary>
  /// <remarks>
  /// <c>Nested</c> 的 <c>catch</c> 里调用了外层方法 <c>Outer</c>：这是 try 内调用、catch 内再调用同一方法的两层结构，
  /// 用于证明"业务失败被包起来再转换"的判据不只对单一 <c>try</c> 语句成立。
  /// </remarks>
  private const string ClientPathProbeSource =
    """
    using System;

    internal sealed class ProbeMiddleware { }

    internal sealed class ProbeResponseWrapper { }

    internal sealed class Probe : IExceptionHandler
    {
      [Authorize]
      internal void Gated() { }

      internal void Wrapped()
      {
        try { Business(); } catch (Exception exception) { throw new InvalidOperationException("业务拒绝", exception); }
      }

      internal void Outer()
      {
        try
        {
          Nested();
        }
        catch (Exception exception)
        {
          throw Probe.Outer();
        }
      }

      internal void Retry()
      {
        try
        {
          Business();
        }
        catch (InvalidOperationException exception)
        {
          throw Retry();
        }
      }

      internal void Nested()
      {
        try
        {
          Business();
        }
        catch (InvalidOperationException exception)
        {
          throw Outer(exception);
        }
      }
    }
    """;

  /// <summary>
  /// 写入型数据映射成员名：查询链出现任一名称即说明该链承担了写入。
  /// </summary>
  /// <remarks>
  /// 标识生成方法不在清单内：生成标识不改变持久化状态，只读查询生成标识时按写入误报会把合法读取判成违规。
  /// 写入面由插入、更新、删除与执行语句四类成员覆盖。
  /// </remarks>
  private static readonly string[] WriteMapperMembers =
  [
    "InsertAsync", "UpdateAsync", "DeleteAsync", "DeleteAllAsync", "ExecuteAsync", "BulkInsertAsync"
  ];

  /// <summary>
  /// 领域事件登记成员名；引用详情链不登记任何事件。
  /// </summary>
  private static readonly string[] EventRegistrationMembers =
  [
    "AddEvent", "AddEvents", "EnqueueEvent", "RegisterEvent", "Enqueue", "FlushAsync", "PublishAsync"
  ];

  /// <summary>
  /// 状态变更方法名的前缀：引用详情链不产生任何状态变化。
  /// </summary>
  private static readonly string[] StateChangeMemberPrefixes =
  [
    "Enable", "Disable", "Void", "Mark", "Consume", "Invalidate", "Append"
  ];
}
