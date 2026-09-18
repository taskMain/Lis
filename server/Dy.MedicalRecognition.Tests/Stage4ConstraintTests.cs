using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 的数据访问调用点与实现约束核对（矩阵 V81 的调用点与物理表两个子面，以及设计决策的符合性核对）。
/// </summary>
/// <remarks>
/// 证据形态全部是静态断言或源码守卫：调用点按 Roslyn 语法节点逐处核对，类型数量按反射清单冻结，
/// 禁止 API 按源码声明扫描。判定不依赖"人工确认已遵守"，也不连接数据库、不启动宿主。
/// </remarks>
public sealed class Stage4ConstraintTests
{
  /// <summary>
  /// 参与调用点核对的阶段 4 仓储实现文件，路径相对仓库根目录。
  /// </summary>
  private static readonly string[] StageRepositorySources =
  [
    "server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.Submission.cs",
    "server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Reports.cs"
  ];

  /// <summary>
  /// 数据映射器上必须逐调用显式传作用域与语句标识的读写成员名。
  /// </summary>
  private static readonly string[] MapperCallMembers =
  [
    "InsertAsync", "UpdateAsync", "QueryAsync", "QuerySingleAsync"
  ];

  /// <summary>
  /// 阶段 4 的十个表映射文件与一个查询映射文件，只允许引用 <c>mrec_</c> 前缀表。
  /// </summary>
  private static readonly string[] StageMapFiles =
  [
    "PlatformPatient.xml", "MedicalRecognitionReport.xml", "MedicalReportVersion.xml",
    "LaboratoryReportContent.xml", "LaboratoryResultItem.xml", "LaboratoryBacteriaResult.xml",
    "LaboratoryAntimicrobialSusceptibility.xml", "ExaminationReportContent.xml", "ExaminationItem.xml", "ExaminationSite.xml",
    "MedicalRecognitionReportQuery.xml"
  ];

  /// <summary>
  /// 唯一约束冲突翻译只允许出现在这三个内部异常类型与仓储的翻译点上。
  /// </summary>
  private static readonly string[] DuplicateExceptionTypeNames =
  [
    "DuplicatePlatformPatientException", "DuplicateMedicalRecognitionReportException", "DuplicateMedicalReportVersionException"
  ];

  /// <summary>
  /// 报告聚合之外的唯一约束冲突异常类型名；这些是阶段 2 与阶段 3 的既有实体，不属于本阶段的重复实现。
  /// </summary>
  private static readonly string[] PreExistingDuplicateExceptionTypeNames =
  [
    "DuplicateMutualRecognitionItemException", "DuplicateOrganizationHospitalBranchRecognitionAmountException"
  ];

  /// <summary>
  /// V81 的调用点面：仓储的每个数据访问调用都显式传 <c>scope</c> 与 <c>sqlId</c>，不使用共享映射器的上下文设置方法。
  /// </summary>
  /// <remarks>
  /// 缺少显式参数时框架会退化到默认推导，调用点与映射文件的对应关系随之隐式化：语句被改名或作用域被改动，
  /// 只在运行期表现为找不到语句。这里按语法节点逐处核对，并对每个缺失情形给出文件、方法名与行号。
  /// </remarks>
  [Fact]
  public void Repository_call_sites_pass_scope_and_statement_id_explicitly()
  {
    List<string> violations = [];
    int checkedCallCount = 0;

    foreach (string relativePath in StageRepositorySources)
    {
      CompilationUnitSyntax root = SourceSyntaxGuard.Read(relativePath.Split('/'));
      foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
      {
        string? memberName = SourceSyntaxGuard.MemberName(invocation.Expression);
        if (memberName is null || !MapperCallMembers.Contains(memberName)) continue;

        checkedCallCount++;
        HashSet<string> argumentNames = [.. invocation.ArgumentList.Arguments
          .Where(argument => argument.NameColon is not null)
          .Select(argument => argument.NameColon!.Name.Identifier.ValueText)];

        if (!argumentNames.Contains("scope")) violations.Add($"{relativePath}: {memberName} 缺少显式 scope 参数");
        if (!argumentNames.Contains("sqlId")) violations.Add($"{relativePath}: {memberName} 缺少显式 sqlId 参数");
      }

      // 上下文设置方法会把作用域变成运行时可变状态，共享映射器实例之间互相串扰。
      Assert.DoesNotContain("SetContext", File.ReadAllText(Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), relativePath)), StringComparison.Ordinal);
    }

    Assert.Empty(violations);

    // 扫描规模下界与变异证据：路径规则改坏成"扫不到任何调用"时先在这里失败；注入一个缺参调用后判据必须命中。
    Assert.True(checkedCallCount >= 20, $"受核对的数据访问调用点应不少于 20 处，实际 {checkedCallCount} 处。");

    CompilationUnitSyntax probe = SourceSyntaxGuard.Parse(
      "class Probe { void M(object mapper, object value) { mapper.InsertAsync(value); } }");
    InvocationExpressionSyntax probeCall = probe.DescendantNodes().OfType<InvocationExpressionSyntax>()
      .Single(invocation => SourceSyntaxGuard.MemberName(invocation.Expression) == "InsertAsync");
    Assert.DoesNotContain(probeCall.ArgumentList.Arguments, argument => argument.NameColon is not null);
  }

  /// <summary>
  /// V81 的物理表面：阶段 4 的映射文件只访问 <c>mrec_</c> 前缀表，且十个表文件里没有业务调用点的无条件全量查询。
  /// </summary>
  [Fact]
  public void Stage_maps_target_platform_tables_only_and_remove_unconditional_queries()
  {
    foreach (string mapFile in StageMapFiles)
    {
      string xml = File.ReadAllText(FindSqlMap(mapFile));

      string[] tableNames = [.. Regex
        .Matches(xml, @"\b(?:from|join|insert\s+into|update)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
        .Select(match => match.Groups[1].Value)
        .Distinct(StringComparer.Ordinal)];

      Assert.NotEmpty(tableNames);
      Assert.All(tableNames, tableName => Assert.StartsWith("mrec_", tableName, StringComparison.Ordinal));

      // 无条件全量查询既没有业务键定位语义，也没有调用方；十张表文件里必须全部移除。
      Assert.DoesNotContain("QueryAll", xml, StringComparison.Ordinal);
    }

    // 变异证据：把表名前缀去掉后同一条判据必须判出未加前缀的表名。
    string reportMap = File.ReadAllText(FindSqlMap("MedicalRecognitionReport.xml"));
    string unprefixed = reportMap.Replace("mrec_", string.Empty, StringComparison.Ordinal);
    Assert.NotEqual(reportMap, unprefixed);
    Assert.All(
      Regex.Matches(unprefixed, @"\b(?:from|join|insert\s+into|update)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
        .Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal),
      tableName => Assert.False(tableName.StartsWith("mrec_", StringComparison.Ordinal)));
  }

  /// <summary>
  /// S4-D2：报告聚合只有一个管理器实现与一套仓储实现，未新增第二套聚合、管理器或仓储。
  /// </summary>
  /// <remarks>
  /// 类型数量按仓库内源码文件清单冻结：新增第二套实现会改变清单，因此不需要人工确认。
  /// </remarks>
  [Fact]
  public void Single_aggregate_manager_and_repository_implementations_are_kept()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string domainAggregate = Path.Combine(root, "server", "Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate");

    string[] managerFiles = [.. Directory.EnumerateFiles(domainAggregate, "MedicalRecognitionReportManager*.cs").Select(Path.GetFileName)!];
    Assert.Equal(
      ["MedicalRecognitionReportManager.Submission.cs", "MedicalRecognitionReportManager.SubmissionValidation.cs", "MedicalRecognitionReportManager.cs"],
      [.. managerFiles.OrderBy(name => name, StringComparer.Ordinal)]);

    // 领域管理器只有一个类型：三个文件必须都声明同一个部分类型，按职责分文件不构成第二套实现。
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> managerDeclarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReportManager");
    Assert.Equal(3, managerDeclarations.Count);
    foreach ((string relativePath, CompilationUnitSyntax managerRoot) in managerDeclarations)
    {
      ClassDeclarationSyntax declaration = managerRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
        .Single(item => item.Identifier.ValueText == "MedicalRecognitionReportManager");
      Assert.Contains(declaration.Modifiers, modifier => modifier.ValueText == "partial");
      Assert.Contains("MedicalRecognitionReportAggregate", relativePath, StringComparison.Ordinal);
    }

    // 写侧仓储接口按职责分为两个部分文件、实现分为两个部分文件，各自仍只有一个类型。
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> writePortDeclarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "IMedicalRecognitionReportRepository");
    Assert.Equal(2, writePortDeclarations.Count);
    Assert.All(writePortDeclarations, declaration => Assert.Contains(
      declaration.Root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
        .Single(item => item.Identifier.ValueText == "IMedicalRecognitionReportRepository").Modifiers,
      modifier => modifier.ValueText == "partial"));

    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> writeImplementationDeclarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportRepository");
    Assert.Equal(2, writeImplementationDeclarations.Count);
    Assert.All(writeImplementationDeclarations, declaration => Assert.Contains(
      declaration.Root.DescendantNodes().OfType<ClassDeclarationSyntax>()
        .Single(item => item.Identifier.ValueText == "MedicalRecognitionReportRepository").Modifiers,
      modifier => modifier.ValueText == "partial"));

    // 查询侧端口按职责分为两个部分文件，报告查询与既有目录查询共用一个实现类型。
    Assert.Equal(
      2,
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "IMedicalRecognitionReportQueryRepository").Count);
    Assert.Equal(
      2,
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Repository", "MedicalRecognitionReportQueryRepository").Count);

    // 聚合根只有一个：领域工程内声明 MedicalRecognitionReport 的文件数量为一。
    Assert.Single(SourceSyntaxGuard.ReadTypeDeclarations(
      "server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate", "MedicalRecognitionReport"));

    // 变异证据：按一个不存在的类型名扫描必须得不到声明，证明"数量唯一"不是扫描面恒真的结果。
    Assert.Empty(SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", "MedicalRecognitionReportManagerProbe"));
  }

  /// <summary>
  /// S4-D24：后端未新增异常中间件、异常过滤器、响应转换器或错误包装模型，业务失败直接抛出。
  /// </summary>
  /// <remarks>
  /// 按源码类型声明扫描本阶段的宿主与应用工程：出现这四类类型的声明即判失败。
  /// 既有的 Result 包装、响应转换器与异常过滤器集中在框架层，本平台不重复建设。
  /// </remarks>
  [Fact]
  public void No_exception_middleware_filter_converter_or_error_wrapper_is_added()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string[] bannedTypeSuffixes = ["Middleware", "ExceptionFilter", "ExceptionHandler", "ResultFilter", "ResponseWrapper", "ErrorWrapper", "ApiResponse"];
    string[] scannedProjects = ["Dy.MedicalRecognition", "Dy.MedicalRecognition.Application", "Dy.MedicalRecognition.Repository"];

    List<string> violations = [];
    int scannedTypeCount = 0;

    foreach (string project in scannedProjects)
    {
      string projectDirectory = Path.Combine(root, "server", project);
      foreach (string file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
      {
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.EndsWith(".g.cs", StringComparison.Ordinal))
        {
          continue;
        }

        foreach (BaseTypeDeclarationSyntax declaration in SourceSyntaxGuard.Parse(File.ReadAllText(file))
          .DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
          scannedTypeCount++;
          string name = declaration.Identifier.ValueText;
          string? matched = bannedTypeSuffixes.FirstOrDefault(suffix => name.EndsWith(suffix, StringComparison.Ordinal));
          if (matched is not null) violations.Add($"{Path.GetRelativePath(root, file)}: {name} 命中禁止类型后缀 {matched}");
        }
      }
    }

    Assert.Empty(violations);

    // 扫描规模下界：本平台的宿主、应用与仓储三个工程手写类型合计约三十个，路径规则改坏成"扫不到任何类型"时先在这里失败。
    Assert.True(scannedTypeCount >= 20, $"受扫描的类型声明应不少于 20 个，实际 {scannedTypeCount} 个。");

    // 变异证据：合成一个带禁止后缀的类型声明，同一条判据必须命中。
    Assert.Contains(
      SourceSyntaxGuard.Parse("class ProbeMiddleware { }").DescendantNodes().OfType<BaseTypeDeclarationSyntax>(),
      declaration => bannedTypeSuffixes.Any(suffix => declaration.Identifier.ValueText.EndsWith(suffix, StringComparison.Ordinal)));
  }

  /// <summary>
  /// S4-D24：仓储的唯一约束冲突翻译只出现在仓储到管理器内部，不改变对外响应形态。
  /// </summary>
  /// <remarks>
  /// 三个报告侧的唯一约束冲突异常类型只允许由仓储声明与抛出、由管理器捕获并翻译为业务拒绝；
  /// 契约与宿主工程不得引用它们，因此对外响应形态不因冲突而改变。
  /// </remarks>
  [Fact]
  public void Duplicate_constraint_translation_stays_inside_repository_and_manager()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();

    // 异常类型只由领域聚合声明：仓储引用领域类型属于既有分层。
    foreach (string typeName in DuplicateExceptionTypeNames)
    {
      IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
        SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", typeName);
      (string relativePath, _) = Assert.Single(declarations);
      Assert.Contains("MedicalRecognitionReportAggregate", relativePath, StringComparison.Ordinal);
    }

    // 阶段 2 与阶段 3 的同类异常同样是单一声明，避免本阶段引入第二套同类实现。
    foreach (string typeName in PreExistingDuplicateExceptionTypeNames)
      Assert.Single(SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Domain", typeName));

    // 抛出点只在仓储实现：管理器只捕获并翻译，契约与宿主不感知。
    string repositorySource = File.ReadAllText(Path.Combine(
      root, "server", "Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportRepository.Submission.cs"));
    foreach (string typeName in DuplicateExceptionTypeNames)
      Assert.Contains($"new {typeName}(", repositorySource, StringComparison.Ordinal);

    string managerSource = File.ReadAllText(Path.Combine(
      root, "server", "Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportManager.Submission.cs"));
    Assert.Contains("catch (DuplicateMedicalRecognitionReportException", managerSource, StringComparison.Ordinal);
    Assert.Contains("catch (DuplicateMedicalReportVersionException", managerSource, StringComparison.Ordinal);
    Assert.Contains("catch (DuplicatePlatformPatientException)", managerSource, StringComparison.Ordinal);

    // 对外契约与宿主工程不得出现这三个异常类型名。
    foreach (string project in new[] { "Dy.MedicalRecognition.Application.Contracts", "Dy.MedicalRecognition" })
    {
      foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "server", project), "*.cs", SearchOption.AllDirectories))
      {
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;

        string source = File.ReadAllText(file);
        foreach (string typeName in DuplicateExceptionTypeNames)
          Assert.DoesNotContain(typeName, source, StringComparison.Ordinal);
      }
    }

    // 变异证据：把异常类型名注入契约源码副本后，同一条判据必须命中。
    const string contractSource = "public sealed class ProbeContract { }";
    Assert.DoesNotContain(DuplicateExceptionTypeNames[0], contractSource, StringComparison.Ordinal);
    Assert.Contains(DuplicateExceptionTypeNames[0], contractSource + DuplicateExceptionTypeNames[0], StringComparison.Ordinal);
  }

  /// <summary>
  /// S4-D1：使用受保护的手工维护推进，再生成保护清单中的配置与启动文件保留人工内容。
  /// </summary>
  /// <remarks>
  /// 判据是四个配置与启动文件仍然存在自有的、与生成器基准值不同的内容：端口、PDF 存储配置节与启动地址。
  /// 这些内容被生成器覆盖时会消失，因此可用静态断言核对。
  /// </remarks>
  [Fact]
  public void Protected_configuration_and_startup_files_keep_manual_content()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string hostDirectory = Path.Combine(root, "server", "Dy.MedicalRecognition");

    string appSettings = File.ReadAllText(Path.Combine(hostDirectory, "appsettings.json"));
    Assert.Contains("\"ReportPdfFiles\"", appSettings, StringComparison.Ordinal);
    Assert.Contains("\"MaxFileBytes\"", appSettings, StringComparison.Ordinal);
    Assert.Contains("http://localhost:15014", appSettings, StringComparison.Ordinal);

    string developmentSettings = File.ReadAllText(Path.Combine(hostDirectory, "appsettings.Development.json"));
    Assert.Contains("\"ReportPdfFiles\"", developmentSettings, StringComparison.Ordinal);
    Assert.Contains("http://localhost:15014", developmentSettings, StringComparison.Ordinal);

    string launchSettings = File.ReadAllText(Path.Combine(hostDirectory, "Properties", "launchSettings.json"));
    Assert.Contains("http://localhost:15014", launchSettings, StringComparison.Ordinal);

    string earthraceConfig = File.ReadAllText(Path.Combine(hostDirectory, "EarthraceConfig.json"));
    Assert.Contains("PostgreSql", earthraceConfig, StringComparison.OrdinalIgnoreCase);

    Assert.True(File.Exists(Path.Combine(root, "server", "global.json")));
    Assert.True(File.Exists(Path.Combine(root, "server", "Directory.Packages.props")));

    // 变异证据：去掉配置节后同一条判据必须失败；证明断言不是对任意文本都成立。
    string withoutSection = appSettings.Replace("\"ReportPdfFiles\"", "\"Probe\"", StringComparison.Ordinal);
    Assert.DoesNotContain("\"ReportPdfFiles\"", withoutSection, StringComparison.Ordinal);
  }

  /// <summary>
  /// S4-D7：单文件上限必须低于宿主请求体上限，使控制器的超限拒绝分支可达。
  /// </summary>
  /// <remarks>
  /// 宿主请求体上限由框架程序集 <c>Dy.Apron.dll</c> 的 <c>ConfigureKestrel</c> 设置，应用源码内无配置键，
  /// 本机实测约为 104857600 字节。两者取值相同时，任何超过单文件上限的文件都会使整个请求体超过宿主上限，
  /// 请求在 multipart 读取阶段即被框架拒绝，控制器的「超过单文件上限」分支永远不会被触发。
  /// 因此该上限必须严格低于宿主上限；本断言固定这一相对关系，防止后续把取值改回与宿主上限相同。
  /// </remarks>
  [Fact]
  public void Single_file_limit_stays_below_the_host_request_body_limit()
  {
    const long measuredHostRequestBodyLimit = 104857600;

    Assert.True(
      ReportPdfFileOptions.DefaultMaxFileBytes < measuredHostRequestBodyLimit,
      $"单文件上限默认值必须低于宿主请求体上限 {measuredHostRequestBodyLimit}，实际为 {ReportPdfFileOptions.DefaultMaxFileBytes}。");

    // 受版本控制的默认配置与代码默认值必须一致，否则部署未提供该键时两者会给出不同的上限。
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string appSettings = File.ReadAllText(Path.Combine(root, "server", "Dy.MedicalRecognition", "appsettings.json"));
    Assert.Contains($"\"MaxFileBytes\": {ReportPdfFileOptions.DefaultMaxFileBytes}", appSettings, StringComparison.Ordinal);

    // 变异证据：把默认值换成宿主上限时同一条判据必须失败，证明断言不是恒真。
    const long wouldBeUnreachable = measuredHostRequestBodyLimit;
    Assert.False(wouldBeUnreachable < measuredHostRequestBodyLimit);
  }

  /// <summary>
  /// 在仓储工程内按文件名定位 SqlMap 文件。
  /// </summary>
  /// <param name="fileName">映射文件名。</param>
  /// <returns>该文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">两个候选目录都不存在该文件时抛出。</exception>
  private static string FindSqlMap(string fileName)
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string aggregatePath = Path.Combine(root, "server", "Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", fileName);
    if (File.Exists(aggregatePath)) return aggregatePath;

    string queriesPath = Path.Combine(root, "server", "Dy.MedicalRecognition.Repository", "Queries", fileName);
    if (File.Exists(queriesPath)) return queriesPath;

    throw new FileNotFoundException($"未找到仓储映射文件 '{fileName}'。");
  }
}
