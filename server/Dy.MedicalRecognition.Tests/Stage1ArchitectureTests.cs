using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 校验标准目录写入口的架构约束：事务声明的范围、公共请求校验的执行顺序、
/// 业务代码不自行控制事务，以及读写契约与实现的分层分离。
/// </summary>
public sealed class Stage1ArchitectureTests
{
  /// <summary>
  /// 校验阶段 1 的写入口只按业务原子边界声明事务：仅"先校验父级启用再写入"的
  /// 创建分组与创建标准项目需要事务，其余单条原子写语句不声明 <c>WorkUnitAttribute</c>。
  /// </summary>
  [Fact]
  public void Write_entrypoints_declare_only_the_transactional_work_units()
  {
    Type appService = typeof(Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService);
    string[] transactional = ["CreateMedicalStandardGroupAsync", "CreateMedicalStandardItemAsync"];
    string[] plain = Array.FindAll(Stage1WriteEntrypoints, name => !transactional.Contains(name));

    foreach (string name in transactional)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Attribute workUnit = Assert.Single(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
      Assert.True((bool)workUnit.GetType().GetProperty("UseTransaction")!.GetValue(workUnit)!);
    }

    foreach (string name in plain)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }
  }

  /// <summary>
  /// 阶段 1 的 12 个写入口名称，供事务声明与校验入口断言共用。
  /// </summary>
  private static readonly string[] Stage1WriteEntrypoints =
  [
    "CreateMedicalStandardCategoryAsync", "UpdateMedicalStandardCategoryAsync", "EnableMedicalStandardCategoryAsync", "DisableMedicalStandardCategoryAsync",
    "CreateMedicalStandardGroupAsync", "UpdateMedicalStandardGroupAsync", "EnableMedicalStandardGroupAsync", "DisableMedicalStandardGroupAsync",
    "CreateMedicalStandardItemAsync", "ChangeMedicalStandardItemRemarkAsync", "EnableMedicalStandardItemAsync", "DisableMedicalStandardItemAsync"
  ];

  /// <summary>
  /// 阶段 1 的 4 个查询入口名称，供查询侧请求校验断言使用。
  /// </summary>
  private static readonly string[] Stage1QueryEntrypoints =
  [
    "QueryMedicalStandardCategoryListAsync", "QueryMedicalStandardGroupListAsync",
    "QueryMedicalStandardItemListAsync", "QueryEffectiveMedicalStandardCatalogAsync"
  ];

  /// <summary>
  /// 校验阶段 1 的 12 个写入口都在映射领域命令之前执行了公共请求校验，
  /// 避免非法入参绕过 Request 层约束直接进入领域判断。
  /// 该断言读取应用服务源码文本，因为反射无法观察方法体内的调用顺序。
  /// </summary>
  [Fact]
  public void Write_entrypoints_validate_the_public_request_before_mapping()
  {
    string source = ReadApplicationServiceSource("MedicalRecognitionReportAggregate", "MedicalRecognitionReportAppService.cs");

    Assert.Empty(FindValidationOrderViolations(source, Stage1WriteEntrypoints));

    // 探针自校验一：声明名被改掉时必须报"未找到方法"，证明判据按方法名锚定声明，而不是对任意文本都返回空违规。
    string renamed = source.Replace(
      "CreateMedicalStandardCategoryAsync", "CreateMedicalStandardCategoriesAsync", StringComparison.Ordinal);
    Assert.NotEqual(source, renamed);
    Assert.Contains(
      "CreateMedicalStandardCategoryAsync: 未在应用服务源码中找到方法",
      FindValidationOrderViolations(renamed, Stage1WriteEntrypoints));

    // 探针自校验二：移除公共请求校验后必须报"缺少校验"。
    string validationRemoved = source.Replace(
      "MedicalRecognitionRequestValidator.Validate(request);", "/* 变更：移除公共请求校验 */", StringComparison.Ordinal);
    Assert.NotEqual(source, validationRemoved);
    Assert.Contains(
      "CreateMedicalStandardCategoryAsync: 缺少公共请求校验调用",
      FindValidationOrderViolations(validationRemoved, Stage1WriteEntrypoints));

    // 探针自校验三：让命令映射先于公共请求校验出现时必须报"校验晚于映射"，证明顺序判定不是恒真。
    string mappingBeforeValidation = validationRemoved.Replace(
      "/* 变更：移除公共请求校验 */",
      "var probe = request.MapToProbeMutation();\n    MedicalRecognitionRequestValidator.Validate(request);",
      StringComparison.Ordinal);
    Assert.NotEqual(validationRemoved, mappingBeforeValidation);
    Assert.Contains(
      "CreateMedicalStandardCategoryAsync: 公共请求校验晚于命令映射",
      FindValidationOrderViolations(mappingBeforeValidation, Stage1WriteEntrypoints));
  }

  /// <summary>
  /// 校验阶段 1 的 4 个查询入口都先执行公共请求校验，避免空 Guid、空串或纯空白、未定义枚举等筛选条件
  /// 被当作“不过滤”处理并返回超出调用方预期的数据。该断言读取查询应用服务源码文本，因为反射无法观察方法体内的调用。
  /// </summary>
  [Fact]
  public void Query_entrypoints_validate_the_public_request()
  {
    string source = ReadApplicationServiceSource("Queries", "MedicalRecognitionReportQueryAppService.cs");

    Assert.Empty(FindQueryValidationViolations(source, Stage1QueryEntrypoints));

    // 探针自校验一：声明名被改掉时必须报"未找到方法"。
    string renamed = source.Replace(
      "QueryMedicalStandardItemListAsync", "QueryMedicalStandardItemListsAsync", StringComparison.Ordinal);
    Assert.NotEqual(source, renamed);
    Assert.Contains(
      "QueryMedicalStandardItemListAsync: 未在查询应用服务源码中找到方法",
      FindQueryValidationViolations(renamed, Stage1QueryEntrypoints));

    // 探针自校验二：移除公共请求校验后必须报"缺少校验"。
    string validationRemoved = source.Replace(
      "MedicalRecognitionRequestValidator.Validate(request);", "/* 变更：移除公共请求校验 */", StringComparison.Ordinal);
    Assert.NotEqual(source, validationRemoved);
    Assert.Contains(
      "QueryMedicalStandardItemListAsync: 缺少公共请求校验调用",
      FindQueryValidationViolations(validationRemoved, Stage1QueryEntrypoints));
  }

  /// <summary>
  /// 读取应用服务实现源码文本：探针需要观察方法体内部的调用顺序，反射无法提供。
  /// </summary>
  /// <param name="directory">应用服务工程下的子目录名，例如 <c>Queries</c>。</param>
  /// <param name="fileName">源码文件名。</param>
  /// <returns>该文件的源码文本。</returns>
  private static string ReadApplicationServiceSource(string directory, string fileName) => File.ReadAllText(Path.Combine(
    SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Application", directory, fileName));

  /// <summary>
  /// 检查给定写入口是否都先执行公共请求校验、再映射领域命令。
  /// </summary>
  /// <param name="source">应用服务的 C# 源码文本；可以是用例构造的变更副本。</param>
  /// <param name="entrypoints">待检查的写入口方法名。</param>
  /// <returns>违规描述集合；全部满足时为空集合。</returns>
  private static List<string> FindValidationOrderViolations(string source, string[] entrypoints)
  {
    List<string> violations = [];
    foreach (string name in entrypoints)
    {
      int start = source.IndexOf($" {name}(", StringComparison.Ordinal);
      if (start < 0)
      {
        violations.Add($"{name}: 未在应用服务源码中找到方法");
        continue;
      }

      int nextMethod = source.IndexOf("\n  public ", start + 1, StringComparison.Ordinal);
      string body = nextMethod < 0 ? source[start..] : source[start..nextMethod];
      int validateIndex = body.IndexOf("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal);
      int mapIndex = body.IndexOf(".MapTo", StringComparison.Ordinal);

      if (validateIndex < 0)
      {
        violations.Add($"{name}: 缺少公共请求校验调用");
      }
      else if (mapIndex >= 0 && validateIndex > mapIndex)
      {
        violations.Add($"{name}: 公共请求校验晚于命令映射");
      }
    }

    return violations;
  }

  /// <summary>
  /// 检查给定查询入口是否都执行了公共请求校验。
  /// </summary>
  /// <param name="source">查询应用服务的 C# 源码文本；可以是用例构造的变更副本。</param>
  /// <param name="entrypoints">待检查的查询入口方法名。</param>
  /// <returns>违规描述集合；全部满足时为空集合。</returns>
  private static List<string> FindQueryValidationViolations(string source, string[] entrypoints)
  {
    List<string> violations = [];
    foreach (string name in entrypoints)
    {
      int start = source.IndexOf($" {name}(", StringComparison.Ordinal);
      if (start < 0)
      {
        violations.Add($"{name}: 未在查询应用服务源码中找到方法");
        continue;
      }

      int nextMethod = source.IndexOf("\n  public ", start + 1, StringComparison.Ordinal);
      string body = nextMethod < 0 ? source[start..] : source[start..nextMethod];
      if (!body.Contains("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal))
      {
        violations.Add($"{name}: 缺少公共请求校验调用");
      }
    }

    return violations;
  }

  /// <summary>
  /// 查询契约的每个方法及其完整签名：方法名、返回类型与逐项参数（modifier、类型、名称、默认值）。
  /// </summary>
  private static readonly (string MethodName, string ReturnType, string[] Parameters)[] QueryContractSignatures =
  [
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalStandardCategoryListAsync),
      "Task<IEnumerable<MedicalStandardCategoryListReadModel>>", ["MedicalStandardCategoryListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalStandardGroupListAsync),
      "Task<IEnumerable<MedicalStandardGroupListReadModel>>", ["MedicalStandardGroupListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalStandardItemListAsync),
      "Task<IEnumerable<MedicalStandardItemListReadModel>>", ["MedicalStandardItemListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryEffectiveMedicalStandardCatalogAsync),
      "Task<EffectiveMedicalStandardCatalogReadModel>", ["EffectiveMedicalStandardCatalogQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync),
      "Task<IEnumerable<RecognitionProjectConfigurationReadModel>>", ["RecognitionProjectConfigurationListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionAmountListAsync),
      "Task<IEnumerable<RecognitionAmountReadModel>>", ["RecognitionAmountListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchRecognitionAmountListAsync),
      "Task<IEnumerable<RecognitionAmountReadModel>>", ["BranchRecognitionAmountListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalReportListAsync),
      "Task<PageResultDto<MedicalReportListReadModel>>", ["ReportListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchMedicalReportListAsync),
      "Task<PageResultDto<MedicalReportListReadModel>>", ["BranchReportListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalReportVersionListAsync),
      "Task<IReadOnlyList<MedicalReportVersionListReadModel>>", ["MedicalReportVersionListQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryMedicalReportVersionDetailAsync),
      "Task<MedicalReportVersionDetailQueryReadModel>", ["MedicalReportVersionDetailQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionCitationDetailAsync),
      "Task<RecognitionCitationDetailReadModel>", ["RecognitionCitationDetailRequest request"]),
    // 阶段 6 的统计与导出：四个查询能力各设平台/本院两个入口，加一个导出入口与一个匹配记录视图。
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionUsageSummaryAsync),
      "Task<PageResultDto<RecognitionUsageSummaryReadModel>>", ["RecognitionUsageSummaryQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionUsageDetailsAsync),
      "Task<PageResultDto<RecognitionUsageDetailReadModel>>", ["RecognitionUsageDetailsQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QuerySourceRecognitionSummaryAsync),
      "Task<PageResultDto<SourceRecognitionSummaryReadModel>>", ["SourceRecognitionSummaryQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QuerySourceRecognitionDetailsAsync),
      "Task<PageResultDto<SourceRecognitionDetailReadModel>>", ["SourceRecognitionDetailsQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchRecognitionUsageSummaryAsync),
      "Task<PageResultDto<RecognitionUsageSummaryReadModel>>", ["BranchRecognitionUsageSummaryQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchRecognitionUsageDetailsAsync),
      "Task<PageResultDto<RecognitionUsageDetailReadModel>>", ["BranchRecognitionUsageDetailsQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchSourceRecognitionSummaryAsync),
      "Task<PageResultDto<SourceRecognitionSummaryReadModel>>", ["BranchSourceRecognitionSummaryQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryBranchSourceRecognitionDetailsAsync),
      "Task<PageResultDto<SourceRecognitionDetailReadModel>>", ["BranchSourceRecognitionDetailsQueryRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.GetStatisticsExportAsync),
      "Task<StatisticsExportFileReadModel>", ["RecognitionStatisticsExportRequest request"]),
    (nameof(IMedicalRecognitionReportQueryAppService.QueryRecognitionMatchRecordAsync),
      "Task<RecognitionMatchRecordReadModel>", ["RecognitionMatchRecordQueryRequest request"])
  ];

  /// <summary>
  /// 查询契约的每个方法都逐项冻结返回类型与参数：只冻结方法名时，参数被删除、换类型、加默认值或加 ref/out
  /// 修饰符，以及同名声明被复制都不会让用例失败。
  /// </summary>
  /// <remarks>
  /// 契约方法分散在同一个接口的多个按功能分片的源文件里，因此按接口全部声明文件一起扫描：
  /// 只读某一个文件时，写在另一个分片里的方法会被判成"未找到声明"，冻结判据对分片位置敏感。
  /// 扫描面覆盖同一目录下全部声明该接口的文件，新增分片自动纳入；同名声明跨文件出现两次同样判失败。
  /// </remarks>
  [Fact]
  public void Query_contract_method_signatures_are_frozen()
  {
    List<MethodDeclarationSyntax> declarations = [];
    foreach ((string relativePath, CompilationUnitSyntax root) in SourceSyntaxGuard.ReadTypeDeclarations(
      "server/Dy.MedicalRecognition.Application.Contracts/Queries", nameof(IMedicalRecognitionReportQueryAppService)))
    {
      Assert.StartsWith("server/Dy.MedicalRecognition.Application.Contracts/Queries/", relativePath, StringComparison.Ordinal);
      declarations.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>());
    }

    foreach ((string methodName, string returnType, string[] parameters) in QueryContractSignatures)
    {
      // 带方法体的同名声明必须恰好一处：0 处（声明被删除）与多处（同名声明被复制）都由该判定直接失败。
      MethodDeclarationSyntax method = declarations.Count(declaration => declaration.Identifier.ValueText == methodName) == 1
        ? declarations.Single(declaration => declaration.Identifier.ValueText == methodName)
        : throw new InvalidOperationException(
          $"方法 {methodName} 的声明应恰好 1 处，实际 {declarations.Count(declaration => declaration.Identifier.ValueText == methodName)} 处。");

      // 契约方法只能是接口声明形式：出现方法体或表达式体说明实现被写进了契约。
      Assert.Null(method.Body);
      Assert.Null(method.ExpressionBody);
      Assert.DoesNotContain(method.Modifiers, modifier => modifier.ValueText is "public" or "protected");
      Assert.Equal(returnType, NormalizeSignature(method.ReturnType.ToString()));
      Assert.Equal(parameters, method.ParameterList.Parameters.Select(DescribeParameter).ToArray());
    }
  }

  /// <summary>
  /// 把参数声明冻结为“modifier 类型 名称 = 默认值”文本：修饰符、类型、名称与默认值任一漂移都会让断言失败。
  /// </summary>
  /// <param name="parameter">参数声明节点。</param>
  /// <returns>该参数的冻结文本。</returns>
  private static string DescribeParameter(ParameterSyntax parameter)
  {
    string declaration = string.Join(' ', new[]
    {
      string.Join(' ', parameter.Modifiers.Select(modifier => modifier.ValueText)),
      parameter.Type!.ToString(),
      parameter.Identifier.ValueText
    }.Where(part => part.Length > 0));

    return parameter.Default is null ? declaration : $"{declaration} = {parameter.Default.Value}";
  }

  /// <summary>
  /// 把签名文本中的换行与连续空白归一为单个空格，使冻结判据对签名内容敏感、对排版换行不敏感。
  /// </summary>
  /// <param name="text">签名文本。</param>
  /// <returns>空白归一后的签名文本。</returns>
  private static string NormalizeSignature(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

  /// <summary>
  /// 事务生命周期只能由框架的工作单元机制管理，业务代码不得出现手工事务 API。
  /// 只扫描生产工程：测试工程本身要写出这些关键字才能断言它们被禁止。
  /// </summary>
  [Fact]
  public void Backend_source_does_not_control_transactions_manually()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string[] banned = ["TransactionScope", "BeginTransaction", "IDbTransaction", "DbTransaction", "UnitOfWork", "TransactionExecutor"];
    List<string> violations = [];

    foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "server"), "*.cs", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(root, file);
      if (relative.Contains("Tests", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.EndsWith(".g.cs", StringComparison.Ordinal))
      {
        continue;
      }

      string source = File.ReadAllText(file);
      foreach (string keyword in banned)
      {
        if (source.Contains(keyword, StringComparison.Ordinal))
        {
          violations.Add($"{relative}: {keyword}");
        }
      }
    }

    Assert.Empty(violations);
  }

  /// <summary>
  /// 受 Provider 中立判据治理的工程：公共契约、应用、领域、领域共享与仓储。
  /// </summary>
  private static readonly string[] ProviderNeutralProjects =
  [
    "Dy.MedicalRecognition.Application.Contracts", "Dy.MedicalRecognition.Application",
    "Dy.MedicalRecognition.Domain", "Dy.MedicalRecognition.Domain.Share", "Dy.MedicalRecognition.Repository"
  ];

  /// <summary>
  /// Provider 中立禁词判据：第一项是用于报告的特征名，第二项是匹配该特征的正则；匹配一律不区分大小写。
  /// </summary>
  /// <remarks>
  /// 具体 Provider 类型按“类型名 + 可选的驱动特有后缀”匹配，覆盖 Npgsql、SqlClient、Oracle、MySql 与 SQLite 的连接、命令、
  /// 参数、事务与批量写入类型，以及提供程序工厂；方言特征覆盖分页（<c>limit</c>、<c>offset</c>、<c>top</c>）、
  /// 空值排序（<c>nulls first</c>、<c>nulls last</c>）、JSON 运算符与大小写不敏感比较（<c>jsonb</c>、<c>ilike</c>）、
  /// 系统表（<c>information_schema</c>）、类型转换符号（<c>::</c>）、行值聚合（<c>count(distinct (</c>）与查询提示（<c>/*+</c>、<c>with (nolock)</c>、<c>option (</c>、<c>readpast</c>、<c>holdlock</c>）。
  /// </remarks>
  private static readonly (string Feature, string Pattern)[] ProviderNeutralityForbiddenFeatures =
  [
    ("Npgsql 类型族", @"\bNpgsql[A-Za-z]*\b"),
    ("SqlClient 连接类型", @"\bSqlConnection\b"),
    ("SqlClient 命令类型", @"\bSqlCommand\b"),
    ("SqlClient 适配器类型", @"\bSqlDataAdapter\b"),
    ("SqlClient 参数类型", @"\bSqlParameter\b"),
    ("SqlClient 批量写入类型", @"\bSqlBulkCopy\b"),
    ("SqlClient 命名空间", @"\bSystem\.Data\.SqlClient\b"),
    ("Microsoft.Data.SqlClient 命名空间", @"\bMicrosoft\.Data\.SqlClient\b"),
    ("Oracle 数据访问类型", @"\bOracle[A-Za-z]*(Connection|Command|DataAdapter|Parameter|Transaction)\b"),
    ("Oracle 托管驱动命名空间", @"\bOracle\.ManagedDataAccess\b"),
    ("MySql 数据访问类型", @"\bMySql[A-Za-z]*(Connection|Command|DataAdapter|Parameter|Transaction)\b"),
    ("MySql 连接器命名空间", @"\bMySqlConnector\b"),
    ("SQLite 数据访问类型", @"\bSQLite[A-Za-z]*(Connection|Command|DataAdapter|Parameter|Transaction)\b"),
    ("驱动工厂类型", @"\bDbProviderFactories\b"),
    ("分页方言 limit", @"\bLIMIT\b"),
    ("分页方言 offset", @"\bOFFSET\b"),
    ("分页方言 top", @"\bTOP\b"),
    ("空值排序 nulls first", @"\bNULLS\s+FIRST\b"),
    ("空值排序 nulls last", @"\bNULLS\s+LAST\b"),
    ("JSON 运算符 jsonb", @"\bJSONB\b"),
    ("大小写不敏感比较 ilike", @"\bILIKE\b"),
    ("系统表 information_schema", @"\bINFORMATION_SCHEMA\b"),
    ("类型转换符号双冒号", @"::"),
    ("行值聚合 count(distinct (", @"COUNT\s*\(\s*DISTINCT\s*\("),
    ("查询提示 Oracle 风格", @"/\*\+"),
    ("查询提示 with (nolock)", @"\bWITH\s*\(\s*NOLOCK\s*\)"),
    ("查询提示 option (", @"\bOPTION\s*\("),
    ("查询提示 readpast", @"\bREADPAST\b"),
    ("查询提示 holdlock", @"\bHOLDLOCK\b")
  ];

  /// <summary>
  /// 项目声明数据库 Provider 中立，公共类型、DataRequest 与共享 SQL/XML 都不得引入具体 Provider 类型或数据库方言特征。
  /// </summary>
  /// <remarks>
  /// 扫描范围：<c>server</c> 下五个受治理工程的手写 <c>.cs</c>，以及 <c>Repository</c> 下的 SqlMap <c>.xml</c> 与建表 <c>.sql</c>；
  /// 排除 <c>bin</c>、<c>obj</c> 与 <c>*.g.cs</c>、<c>*.Designer.cs</c> 生成文件。
  /// <c>Repository/EarthraceConfig.json</c> 不在范围内：该文件的作用正是选择当前 Provider 与连接串，不属于被禁的公共类型与共享语句。
  /// 已登记的覆盖缺口：该文件的 <c>Database.DbProvider.Name</c> 当前为 <c>Oracle</c>，而共享建表脚本 <c>Scripts/mutual_recognition_item.sql</c> 按 PostgreSQL 方言编写，
  /// 宿主配置为 <c>PostgreSql</c>；
  /// 该 JSON 属 Provider 选型这一外部关注面，本判据不对其内容断言（断言会随外部选型改动误红），
  /// 因此"Provider 选型与共享 DDL 口径一致"不在本判据的覆盖范围内。
  /// 判据口径：Provider 类型名与 SQL 关键字都按不区分大小写的整词匹配，词边界使 C# 的 <c>DateTimeOffset</c> 不会命中 <c>offset</c>；
  /// 方言符号（<c>::</c>、<c>/*+</c>、<c>count(distinct (</c>）按原文匹配。
  /// 用例自带两条自校验，证明判据既不恒真也不恒假：含全部 29 条特征的合成源码必须逐条报出违规，
  /// 只含 <c>DateTimeOffset</c>、<c>int.MaxValue</c>、<c>JsonSerializerOptions</c> 等中立写法的合成源码必须不报。
  /// </remarks>
  [Fact]
  public void Public_types_and_shared_sql_stay_database_provider_neutral()
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    List<string> violations = [];
    int governedFileCount = 0;

    foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "server"), "*", SearchOption.AllDirectories))
    {
      if (!IsProviderNeutralityGovernedFile(root, file)) continue;
      governedFileCount++;
      violations.AddRange(FindProviderNeutralityViolations(
        Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllText(file)));
    }

    Assert.Empty(violations);

    // 扫描规模下界：受治理文件集体消失、或被路径规则改坏成"扫不到任何文件"时先在这里失败，避免空扫描变成假绿。
    Assert.True(governedFileCount >= 200, $"受治理的 Provider 中立扫描文件数应不少于 200，实际 {governedFileCount}。");

    // 自校验一：全部 29 条判据都必须被报出，证明每条特征都有判别力。
    // 样本只覆盖部分特征时，未被覆盖判据的正则被写成永不命中的形式也不会有任何用例失败，
    // 因此样本覆盖全部判据，并按特征名逐条断言，而不是只断言"违规集合非空"。
    const string foreignProviderSample =
      "new NpgsqlConnection(connectionString);\n" +
      "new SqlConnection(connectionString); new SqlCommand(sql); new SqlDataAdapter(adapter); new SqlParameter(\"@id\", value); new SqlBulkCopy(connection);\n" +
      "System.Data.SqlClient.SqlConnection legacy = connection; Microsoft.Data.SqlClient.SqlConnection modern = connection;\n" +
      "new OracleConnection(connectionString); Oracle.ManagedDataAccess.Client.OracleCommand oracleCommand = command;\n" +
      "new MySqlConnection(connectionString); MySqlConnector.MySqlCommand mySqlCommand = command; new SQLiteConnection(connectionString);\n" +
      "DbProviderFactories.GetFactory(providerName);\n" +
      "select top 10 /*+ index(t) */ id::int, count(distinct (a, b)) from information_schema.tables t where name ilike 'x' order by id nulls first, code nulls last limit 10 offset 5;\n" +
      "select payload from t with (nolock) where extra = cast(doc as jsonb);\n" +
      "select id from t with (readpast, holdlock) option (recompile);";
    List<string> foreignProviderViolations = FindProviderNeutralityViolations("probe.cs", foreignProviderSample);
    foreach (string feature in ProviderNeutralityForbiddenFeatures.Select(criterion => criterion.Feature))
    {
      Assert.Contains(foreignProviderViolations, violation => violation.StartsWith($"probe.cs: {feature} =>", StringComparison.Ordinal));
    }

    // 判据条数与命中条数一致：任一条判据的正则失效（样本覆盖但永不命中）都会在这里失败。
    Assert.Equal(ProviderNeutralityForbiddenFeatures.Length, foreignProviderViolations.Count);
    // 特征名必须两两不同：重名会让"逐条断言"退化成只断言其中一条，判据数量与命中数量的对应关系失去意义。
    Assert.Equal(
      ProviderNeutralityForbiddenFeatures.Length,
      ProviderNeutralityForbiddenFeatures.Select(criterion => criterion.Feature).Distinct(StringComparer.Ordinal).Count());

    // 自校验二：C# 的中立写法不得被误报，证明整词口径没有把公共类型（如 DateTimeOffset 里的 Offset）判成方言。
    const string neutralSample =
      "DateTimeOffset operTime = DateTimeOffset.UtcNow;\n" +
      "int max = int.MaxValue;\n" +
      "JsonSerializerOptions options = new();\n" +
      "string total = maximum.ToString();";
    Assert.Empty(FindProviderNeutralityViolations("probe.cs", neutralSample));
  }

  /// <summary>
  /// 判断给定文件是否落在 Provider 中立判据的扫描范围内。
  /// </summary>
  /// <param name="repositoryRoot">仓库根目录绝对路径。</param>
  /// <param name="filePath">待判断文件的绝对路径。</param>
  /// <returns>属于受治理的手写源码或共享 SQL/XML 时为 <see langword="true"/>。</returns>
  private static bool IsProviderNeutralityGovernedFile(string repositoryRoot, string filePath)
  {
    string relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace(Path.DirectorySeparatorChar, '/');
    if (relativePath.Contains("/bin/", StringComparison.Ordinal) ||
        relativePath.Contains("/obj/", StringComparison.Ordinal) ||
        filePath.EndsWith(".g.cs", StringComparison.Ordinal) ||
        filePath.EndsWith(".Designer.cs", StringComparison.Ordinal))
    {
      return false;
    }

    if (filePath.EndsWith(".cs", StringComparison.Ordinal))
    {
      return ProviderNeutralProjects.Any(project => relativePath.StartsWith($"server/{project}/", StringComparison.Ordinal));
    }

    return relativePath.StartsWith("server/Dy.MedicalRecognition.Repository/", StringComparison.Ordinal) &&
      (filePath.EndsWith(".xml", StringComparison.Ordinal) || filePath.EndsWith(".sql", StringComparison.Ordinal));
  }

  /// <summary>
  /// 在单份源码文本中查找具体 Provider 类型与数据库方言特征，每个命中的特征只报告首个匹配文本。
  /// </summary>
  /// <param name="relativePath">用于报告的仓库内相对路径（使用 <c>/</c>）。</param>
  /// <param name="source">待检查的源码或语句文本；可以是用例构造的合成样本。</param>
  /// <returns>违规描述集合；未命中任何特征时为空集合。</returns>
  private static List<string> FindProviderNeutralityViolations(string relativePath, string source)
  {
    List<string> violations = [];
    foreach ((string feature, string pattern) in ProviderNeutralityForbiddenFeatures)
    {
      Match match = Regex.Match(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
      if (match.Success) violations.Add($"{relativePath}: {feature} => {match.Value}");
    }

    return violations;
  }

  /// <summary>
  /// 校验读写契约分离：查询契约只继承应用服务标记而不含写方法，查询契约与查询仓储实现可相互赋值，
  /// 查询契约的方法集合固定为阶段 1 的四个目录查询、阶段 2 的互认配置列表查询、阶段 3 的两个金额列表查询、
  /// 阶段 4 的报告列表与版本查询、阶段 5 的引用详情查询，以及阶段 6 的八个统计查询、统计导出入口与匹配记录视图，
  /// 且写入口契约中不得出现只读查询方法。
  /// </summary>
  [Fact]
  public void Query_contract_is_separated_from_write_service_and_repository_implementation()
  {
    Assert.Contains(typeof(IMedicalRecognitionReportQueryAppService).GetInterfaces(), type => type.Name == "IApplicationService");
    Assert.True(typeof(IMedicalRecognitionReportQueryAppService).IsAssignableFrom(typeof(MedicalRecognitionReportQueryAppService)));
    Assert.True(typeof(IMedicalRecognitionReportQueryRepository).IsAssignableFrom(typeof(Dy.MedicalRecognition.Repository.Queries.MedicalRecognitionReportQueryRepository)));

    string[] expectedQueryMethods =
    [
      "GetStatisticsExportAsync", "QueryBranchMedicalReportListAsync", "QueryBranchRecognitionAmountListAsync",
      "QueryBranchRecognitionUsageDetailsAsync", "QueryBranchRecognitionUsageSummaryAsync",
      "QueryBranchSourceRecognitionDetailsAsync", "QueryBranchSourceRecognitionSummaryAsync",
      "QueryEffectiveMedicalStandardCatalogAsync", "QueryMedicalReportListAsync", "QueryMedicalReportVersionDetailAsync",
      "QueryMedicalReportVersionListAsync", "QueryMedicalStandardCategoryListAsync", "QueryMedicalStandardGroupListAsync",
      "QueryMedicalStandardItemListAsync", "QueryRecognitionAmountListAsync", "QueryRecognitionCitationDetailAsync",
      "QueryRecognitionMatchRecordAsync", "QueryRecognitionProjectConfigurationListAsync",
      "QueryRecognitionUsageDetailsAsync", "QueryRecognitionUsageSummaryAsync",
      "QuerySourceRecognitionDetailsAsync", "QuerySourceRecognitionSummaryAsync"
    ];
    string[] actualQueryMethods = [.. typeof(IMedicalRecognitionReportQueryAppService).GetMethods().Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal)];
    Assert.Equal(expectedQueryMethods, actualQueryMethods);

    // 写入口契约上不得出现任何名称以 Query 开头的公开方法：写入口的名称以 Query 开头即表达该入口是只读查询，与写侧职责冲突。
    Assert.DoesNotContain(typeof(Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.IMedicalRecognitionReportAppService).GetMethods(), method => method.Name.StartsWith("Query", StringComparison.Ordinal));
  }

  /// <summary>
  /// 校验分组改名契约不携带所属分类：修改分组请求与对应领域命令都不含分类属性，从契约层面阻止分组跨分类迁移。
  /// </summary>
  [Fact]
  public void Update_group_contract_cannot_change_parent_category()
  {
    Assert.Null(typeof(Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests.UpdateMedicalStandardGroupRequest).GetProperty("CategoryId"));
    Assert.Null(typeof(Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands.UpdateMedicalStandardGroupCommand).GetProperty("CategoryId"));
  }

  /// <summary>
  /// 冻结测试工程的并行化口径：<c>xunit.runner.json</c> 必须存在，且程序集与测试集合两个并行化开关都为 <see langword="false"/>。
  /// </summary>
  /// <remarks>
  /// 测试工程写进程级共享状态：静态事件队列、进程内可信请求上下文，以及在测试进程内注册的类型处理器工厂条目。
  /// 这些状态在并行执行下会跨用例串扰；并行化开关被改回 <see langword="true"/>、键被改名或文件被改名删除时，
  /// 运行器只会静默并行，不会有任何用例失败，因此这里按 JSON 键逐项断言（不比对整文件文本，允许注释与排版变化），
  /// 且缺键时给出缺失的键名。
  /// </remarks>
  [Fact]
  public void Test_runner_disables_assembly_and_collection_parallelization()
  {
    string runnerConfigPath = Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Tests", "xunit.runner.json");
    Assert.True(File.Exists(runnerConfigPath), $"测试工程的运行器配置缺失：{runnerConfigPath}。");

    using JsonDocument runnerConfig = JsonDocument.Parse(File.ReadAllText(runnerConfigPath));
    foreach (string key in new[] { "parallelizeAssembly", "parallelizeTestCollections" })
    {
      Assert.True(
        runnerConfig.RootElement.TryGetProperty(key, out JsonElement value),
        $"测试工程的运行器配置缺少键 {key}：{runnerConfigPath}。");
      Assert.Equal(JsonValueKind.False, value.ValueKind);
    }
  }
}
