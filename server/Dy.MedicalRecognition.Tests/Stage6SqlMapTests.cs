using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 6 的 DDL、依赖声明与统计语句静态守卫：冻结统计索引迁移文件的三条索引声明与逐条中文注释，
/// 三份建表脚本与迁移文件的双写一致性，匹配记录表新索引与既有业务键索引的列序差异，
/// ClosedXML 的集中包管理声明与 Application 工程显式引用面，
/// 以及十七条互认统计语句的作用域、标识、共用筛选、按指标业务时间独立过滤、维度分组键、稳定排序与数据库中立性。
/// </summary>
/// <remarks>
/// 覆盖阶段 6 验证矩阵 V7 的脚本面、V8 的依赖声明面与 V1、V9、V23 的语句面静态判据（行为面归票 03-05 与票 09）。
/// 迁移文件由负责人在目标库执行，本文件只冻结仓库内的脚本文本；索引在目标库的实际存在性属建表完成后的验收面，不在本文件内验证。
/// 内容敏感性由两类证据分别承担：索引声明与注释按文本抽取后与冻结常量逐字等值，列序差异按抽取的列清单位置判定；
/// 语句面按归一化文本的谓词与子句片段逐项判定。
/// </remarks>
public sealed class Stage6SqlMapTests
{
  /// <summary>统计索引迁移文件名；日期为交付日，命名形态参照 LisCenter 的 Migrations 先例。</summary>
  private const string MigrationFile = "20260921_s6_statistics_indexes.sql";

  /// <summary>
  /// 阶段 6 追加进查询作用域的十七条统计语句标识，按映射文件内的声明顺序排列。
  /// </summary>
  /// <remarks>
  /// 语句标识由查询端口方法名去掉 <c>Async</c> 后缀推导，因此必须与端口方法名逐字相同；
  /// 顺序同时是映射文件内的追加位置，语句被改名、被删减或插到既有语句之间都会让这里的等值判据失败。
  /// </remarks>
  private static readonly string[] StatisticsStatementIds =
  [
    "CountRecognitionUsageSummaryGroups", "QueryRecognitionUsageSummaryPage", "QueryRecognitionUsageSummaryReasons",
    "CountSourceRecognitionSummaryGroups", "QuerySourceRecognitionSummaryPage",
    "CountRecognitionUsageReminderDetails", "QueryRecognitionUsageReminderDetails",
    "CountRecognitionUsageAdoptionDetails", "QueryRecognitionUsageAdoptionDetails",
    "CountRecognitionUsageNonAdoptionDetails", "QueryRecognitionUsageNonAdoptionDetails",
    "CountRecognitionUsageReferenceDetails", "QueryRecognitionUsageReferenceDetails",
    "CountSourceRecognitionDetails", "QuerySourceRecognitionDetails",
    "QueryRecognitionMatchRecordView", "QueryRecognitionMatchRecordViewItems"
  ];

  /// <summary>查询作用域在追加统计语句前的既有语句条数；与十七条合计用于冻结清单等值扩展。</summary>
  private const int ExistingQueryStatementCount = 28;

  /// <summary>计数语句与当页语句的配对及其行集是否按分组键聚合；汇总对作用于聚合后的分组行。</summary>
  private static readonly (string CountId, string PageId, bool GroupedRow)[] CountPagePairs =
  [
    ("CountRecognitionUsageSummaryGroups", "QueryRecognitionUsageSummaryPage", true),
    ("CountSourceRecognitionSummaryGroups", "QuerySourceRecognitionSummaryPage", true),
    ("CountRecognitionUsageReminderDetails", "QueryRecognitionUsageReminderDetails", false),
    ("CountRecognitionUsageAdoptionDetails", "QueryRecognitionUsageAdoptionDetails", false),
    ("CountRecognitionUsageNonAdoptionDetails", "QueryRecognitionUsageNonAdoptionDetails", false),
    ("CountRecognitionUsageReferenceDetails", "QueryRecognitionUsageReferenceDetails", false),
    ("CountSourceRecognitionDetails", "QuerySourceRecognitionDetails", false)
  ];

  /// <summary>
  /// 共享 SQL 的方言特征：出现即视为违反数据库 Provider 中立约定。
  /// </summary>
  /// <remarks>
  /// 在阶段 5 同类标记的基础上补入窗口函数标记：统计语句只用标准 <c>CASE</c> 条件聚合，
  /// 窗口函数尚不属于本项目确认的公共 SQL 子集。
  /// </remarks>
  private static readonly (string Feature, string Pattern)[] DialectMarkers =
  [
    ("分页方言 limit", @"\bLIMIT\b"),
    ("分页方言 offset", @"\bOFFSET\b"),
    ("分页方言 top", @"\bTOP\b"),
    ("分页方言 fetch first", @"\bFETCH\s+FIRST\b"),
    ("分页方言 rows only", @"\bROWS\s+ONLY\b"),
    ("空值排序 nulls first", @"\bNULLS\s+FIRST\b"),
    ("空值排序 nulls last", @"\bNULLS\s+LAST\b"),
    ("类型转换符号双冒号", @"::"),
    ("结构化类型转换 cast", @"\bCAST\s*\("),
    ("JSON 运算符 jsonb", @"\bJSONB\b"),
    ("JSON 运算符箭头", @"->>?"),
    ("大小写不敏感比较 ilike", @"\bILIKE\b"),
    ("系统表 information_schema", @"\bINFORMATION_SCHEMA\b"),
    ("系统表 pg_catalog", @"\bPG_CATALOG\b"),
    ("查询提示 Oracle 风格", @"/\*\+"),
    ("查询提示 with (nolock)", @"\bWITH\s*\(\s*NOLOCK\s*\)"),
    ("查询提示 option (", @"\bOPTION\s*\("),
    ("冲突处理 on conflict", @"\bON\s+CONFLICT\b"),
    ("数据库当前时间 current_timestamp", @"\bCURRENT_TIMESTAMP\b"),
    ("行值聚合 count(distinct (", @"COUNT\s*\(\s*DISTINCT\s*\("),
    ("窗口函数 over", @"\bOVER\s*\("),
    ("窗口函数 row_number", @"\bROW_NUMBER\s*\("),
    ("窗口聚合 first_value", @"\bFIRST_VALUE\s*\(")
  ];

  /// <summary>
  /// 三条统计索引的索引名、所属建表脚本、声明文本与中文索引注释；声明文本在迁移文件与建表脚本中逐字一致。
  /// </summary>
  /// <remarks>
  /// 列名取自三份建表脚本的物理列名：匹配记录表按接收组织、接收医院、接收院区三值加互认匹配生成时间，
  /// 处理结果表按互认时间，引用事实表按实际引用时间；三条均为普通索引，不加 if not exists。
  /// </remarks>
  private static readonly (string IndexName, string ScriptFile, string Declaration, string Comment)[] ExpectedIndexes =
  [
    (
      "ix_mrec_recognition_match_record_receiver_time",
      "mrec_recognition_match_record.sql",
      "create index ix_mrec_recognition_match_record_receiver_time\non mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, match_created_time);",
      "comment on index ix_mrec_recognition_match_record_receiver_time is '接收组织、接收医院与接收院区三值等值筛选并按互认匹配生成时间做范围扫描，与业务键索引互补';"
    ),
    (
      "ix_mrec_recognition_processing_result_recognition_time",
      "mrec_recognition_processing_result.sql",
      "create index ix_mrec_recognition_processing_result_recognition_time\non mrec_recognition_processing_result (recognition_time);",
      "comment on index ix_mrec_recognition_processing_result_recognition_time is '按互认时间统计采纳次数、不采纳次数与预计节省金额的时间范围扫描';"
    ),
    (
      "ix_mrec_recognition_reference_referenced_time",
      "mrec_recognition_reference.sql",
      "create index ix_mrec_recognition_reference_referenced_time\non mrec_recognition_reference (referenced_time);",
      "comment on index ix_mrec_recognition_reference_referenced_time is '按实际引用时间统计引用次数的时间范围扫描';"
    )
  ];

  /// <summary>携带项目类型筛选的统计语句：两侧汇总计数、取页与原因语句，加接收侧四类与来源侧明细对。</summary>
  private static readonly string[] ItemTypeFilteredStatementIds =
  [
    "CountRecognitionUsageSummaryGroups", "QueryRecognitionUsageSummaryPage", "QueryRecognitionUsageSummaryReasons",
    "CountRecognitionUsageReminderDetails", "QueryRecognitionUsageReminderDetails",
    "CountRecognitionUsageAdoptionDetails", "QueryRecognitionUsageAdoptionDetails",
    "CountRecognitionUsageNonAdoptionDetails", "QueryRecognitionUsageNonAdoptionDetails",
    "CountRecognitionUsageReferenceDetails", "QueryRecognitionUsageReferenceDetails",
    "CountSourceRecognitionSummaryGroups", "QuerySourceRecognitionSummaryPage",
    "CountSourceRecognitionDetails", "QuerySourceRecognitionDetails"
  ];

  /// <summary>解决方案内禁用的其他 Excel 库包名（小写）；ClosedXML 是唯一允许的 Excel 生成库。</summary>
  private static readonly string[] ForbiddenExcelPackages =
  [
    "npoi", "epplus", "miniexcel", "documentformat.openxml", "aspose.cells",
    "gembox", "exceldatareader", "fastexcel", "spreadsheetlight"
  ];

  /// <summary>
  /// V7：迁移文件恰好含三条统计索引声明，逐条带中文索引注释，无 if not exists，也无建表、改表或删除类语句。
  /// </summary>
  /// <remarks>
  /// 迁移文件是负责人在目标库执行增量 DDL 的唯一输入：多一条、少一条、换名或夹带其他结构变更都会在这里失败。
  /// </remarks>
  [Fact]
  public void Migration_script_declares_exactly_three_statistics_indexes_with_comments()
  {
    string script = Normalize(File.ReadAllText(SourceSyntaxGuard.FindRepositoryFile("Scripts/Migrations", MigrationFile)));

    // 迁移只做增量加索引：无条件建索引会让重复执行直接撞已存在对象。
    Assert.DoesNotContain("if not exists", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("create table", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("alter table", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("drop ", script, StringComparison.OrdinalIgnoreCase);

    // 索引集合与设计逐项等值。
    string[] declaredNames =
    [
      .. Regex.Matches(script, @"create (?<unique>unique )?index (?<name>[a-z_][a-z0-9_]*)", RegexOptions.CultureInvariant)
        .Select(match => match.Groups["name"].Value)
    ];
    Assert.Equal(
      ExpectedIndexes.Select(index => index.IndexName).Order(StringComparer.Ordinal),
      declaredNames.Order(StringComparer.Ordinal));

    foreach ((string indexName, string _, string declaration, string comment) in ExpectedIndexes)
    {
      // 普通索引：三条统计索引都不承载唯一性，"create unique index" 的写法在这里判为不成立。
      Assert.Contains($"create index {indexName}\n", script, StringComparison.Ordinal);
      Assert.Equal(declaration, ExtractIndexDeclaration(script, indexName));
      Assert.Equal(1, CountDeclarations(script, indexName));

      // 逐条中文注释：恰好一条且与冻结文案逐字一致，文案为空时数据库元数据里没有可用说明。
      Assert.Equal(comment, ExtractIndexComment(script, indexName));
    }

    // 变异证据：把时间列挪到接收院区之前的声明与冻结常量逐字不同，等值判据对列序敏感。
    string reordered = ExpectedIndexes[0].Declaration.Replace(
      "receiver_branch_code, match_created_time",
      "match_created_time, receiver_branch_code",
      StringComparison.Ordinal);
    Assert.NotEqual(ExpectedIndexes[0].Declaration, reordered);
    Assert.NotEqual(reordered, ExtractIndexDeclaration(script, ExpectedIndexes[0].IndexName));
  }

  /// <summary>
  /// V7：三份建表脚本各自含与迁移文件逐字一致的三条索引声明与中文索引注释，任一侧漂移都会在这里失败。
  /// </summary>
  /// <remarks>
  /// 表结构变更双写规则要求迁移文件与建表脚本两处同步：只改迁移会让按最终建表脚本重建时丢失索引，
  /// 只改建表脚本会让增量执行丢索引，因此两侧文本分别抽取后逐字等值，冻结常量只作为第三重锚点。
  /// </remarks>
  [Fact]
  public void Table_scripts_carry_index_declarations_identical_to_the_migration()
  {
    string migration = Normalize(File.ReadAllText(SourceSyntaxGuard.FindRepositoryFile("Scripts/Migrations", MigrationFile)));

    foreach ((string indexName, string scriptFile, string declaration, string comment) in ExpectedIndexes)
    {
      string tableScript = Normalize(File.ReadAllText(SourceSyntaxGuard.FindRepositoryFile("Scripts", scriptFile)));

      Assert.Equal(ExtractIndexDeclaration(migration, indexName), ExtractIndexDeclaration(tableScript, indexName));
      Assert.Equal(ExtractIndexComment(migration, indexName), ExtractIndexComment(tableScript, indexName));
      Assert.Equal(declaration, ExtractIndexDeclaration(tableScript, indexName));
      Assert.Equal(comment, ExtractIndexComment(tableScript, indexName));
    }
  }

  /// <summary>
  /// V7：匹配记录表新统计索引与既有业务键索引的列序差异被冻结——时间列位置决定范围扫描能力。
  /// </summary>
  /// <remarks>
  /// 业务键索引的互认匹配生成时间位于证件类型与证件号码之后：接收三值等值后要先越过两列证件才能到达时间列，
  /// 时间范围条件无法作为相邻前缀参与扫描；新索引的时间列紧跟接收院区之后，同一张表两条索引互补且各恰好声明一次。
  /// </remarks>
  [Fact]
  public void Match_record_time_column_position_differs_between_business_key_and_receiver_time_index()
  {
    string script = Normalize(File.ReadAllText(SourceSyntaxGuard.FindRepositoryFile("Scripts", "mrec_recognition_match_record.sql")));

    // 既有业务键索引的列序保持不变：接收三值之后是两列证件，时间列在最后。
    string[] businessKeyColumns = ExtractIndexColumns(script, "ix_mrec_recognition_match_record_business_key");
    Assert.Equal(
      ["receiver_organization_code", "receiver_hospital_code", "receiver_branch_code", "identity_document_type_code", "identity_document_no", "match_created_time"],
      businessKeyColumns);
    Assert.NotEqual(
      businessKeyColumns.IndexOf("match_created_time"),
      businessKeyColumns.IndexOf("receiver_branch_code") + 1);

    // 新统计索引的时间列紧跟接收三值之后，支撑接收三值等值加时间范围扫描。
    string[] receiverTimeColumns = ExtractIndexColumns(script, "ix_mrec_recognition_match_record_receiver_time");
    Assert.Equal(
      ["receiver_organization_code", "receiver_hospital_code", "receiver_branch_code", "match_created_time"],
      receiverTimeColumns);
    Assert.Equal(
      receiverTimeColumns.IndexOf("match_created_time"),
      receiverTimeColumns.IndexOf("receiver_branch_code") + 1);

    // 两条索引互补不重复：同表各恰好声明一次。
    Assert.Equal(1, CountDeclarations(script, "ix_mrec_recognition_match_record_business_key"));
    Assert.Equal(1, CountDeclarations(script, "ix_mrec_recognition_match_record_receiver_time"));
  }

  /// <summary>
  /// V8：ClosedXML 在集中包管理中恰好声明一次，且 PackageReference 只出现在 Application 工程，未引入其他 Excel 库。
  /// </summary>
  /// <remarks>
  /// 导出组装在应用层（S6-D7），宿主与其余工程不得直接引用；DocumentFormat.OpenXml 是 ClosedXML 的传递依赖，
  /// 只允许经传递引入，直接声明同样算引入了其他 Excel 库。
  /// </remarks>
  [Fact]
  public void Closedxml_is_declared_central_and_referenced_only_by_application_project()
  {
    string centralPackages = File.ReadAllText(ResolveServerFile("Directory.Packages.props"));

    // 集中包管理：ClosedXML 恰好声明一次，版本随集中管理维护。
    Assert.Contains("Include=\"ClosedXML\"", centralPackages, StringComparison.Ordinal);
    Assert.Single(Regex.Matches(centralPackages, "Include=\"ClosedXML\"", RegexOptions.CultureInvariant));

    // 显式引用：Application 工程恰好一次，宿主与其余工程零次。
    string applicationProject = File.ReadAllText(ResolveServerFile("Dy.MedicalRecognition.Application/Dy.MedicalRecognition.Application.csproj"));
    Assert.Contains("<PackageReference Include=\"ClosedXML\" />", applicationProject, StringComparison.Ordinal);

    string serverRoot = Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), "server");
    string[] projectFiles =
    [
      .. Directory.EnumerateFiles(serverRoot, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
    ];
    Assert.NotEmpty(projectFiles);
    foreach (string projectFile in projectFiles)
    {
      string project = File.ReadAllText(projectFile);
      string relativePath = Path.GetRelativePath(serverRoot, projectFile).Replace('\\', '/');
      int expected = relativePath == "Dy.MedicalRecognition.Application/Dy.MedicalRecognition.Application.csproj" ? 1 : 0;
      Assert.Equal(expected, Regex.Matches(project, "Include=\"ClosedXML\"", RegexOptions.CultureInvariant).Count);

      foreach (string packageName in ForbiddenExcelPackages)
      {
        Assert.DoesNotContain(packageName, project, StringComparison.OrdinalIgnoreCase);
      }
    }

    foreach (string packageName in ForbiddenExcelPackages)
    {
      Assert.DoesNotContain(packageName, centralPackages, StringComparison.OrdinalIgnoreCase);
    }
  }

  /// <summary>
  /// V1、V9：十七条统计语句全部注册在既有查询作用域 <c>MedicalRecognitionReportQuery</c> 下，
  /// 按冻结顺序恰好追加在既有语句之后，每条带说明职责的 XML 功能注释。
  /// </summary>
  /// <remarks>
  /// 语句标识由查询端口方法名去掉 <c>Async</c> 后缀推导，框架按调用方法名定位语句；
  /// 标识或作用域不一致只在运行期表现为找不到语句，静态检查不会失败，因此这里逐条冻结。
  /// </remarks>
  [Fact]
  public void Statistics_statements_are_registered_in_the_query_scope_with_comments()
  {
    XDocument document = XDocument.Load(QueryMapPath());
    Assert.Equal("MedicalRecognitionReportQuery", (string?)document.Root!.Attribute("Scope"));

    string[] allStatementIds = StatementIds(document);
    Assert.Equal(ExistingQueryStatementCount + StatisticsStatementIds.Length, allStatementIds.Length);
    Assert.Equal(StatisticsStatementIds, allStatementIds[ExistingQueryStatementCount..]);
    Assert.Equal(StatisticsStatementIds, StatisticsStatementIds.Distinct(StringComparer.Ordinal).ToArray());

    foreach (string statementId in StatisticsStatementIds)
    {
      XElement statement = Assert.Single(StatementElements(document), element => (string?)element.Attribute("Id") == statementId);
      string comment = Assert.IsType<XComment>(statement.PreviousNode).Value;
      Assert.Contains("功能：", comment, StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// V22、V23 的静态面：七对计数语句与当页语句共用同一套筛选条件；原因汇总语句在同一套筛选条件之上追加采纳范围限定。
  /// </summary>
  /// <remarks>
  /// 计数与取页的筛选不一致时总数与当页会静默错位，行为测试难以逐条件覆盖，因此按归一化文本比较筛选片段：
  /// 汇总对的片段取分组前最后一个 where 到第一个 group by 之间，明细对取 where 到 order by 之间。
  /// </remarks>
  [Fact]
  public void Statistics_counts_share_the_filter_block_with_their_page_queries()
  {
    foreach ((string countId, string pageId, bool groupedRow) in CountPagePairs)
    {
      string countRegion = ExtractFilterRegion(StatisticsStatementText(countId), groupedRow);
      string pageRegion = ExtractFilterRegion(StatisticsStatementText(pageId), groupedRow);
      Assert.True(
        string.Equals(countRegion, pageRegion, StringComparison.Ordinal),
        $"{countId} 与 {pageId} 的筛选片段不一致：\n计数：{countRegion}\n取页：{pageRegion}");

      // 判别力：从当页语句副本里去掉日期边界参数后，同一条比较必须判出差异。
      // IsNotEmpty 片段的 and 前缀由框架在渲染期注入，归一化文本里片段本身不带前缀；
      // 起始边界参数是七对语句共有的筛选内容，去掉它必然改变片段。
      string mutated = pageRegion.Replace("$PeriodStart", string.Empty, StringComparison.Ordinal);
      Assert.NotEqual(countRegion, mutated);
    }

    // 原因汇总与接收侧汇总计数同筛选，再追加只保留不采纳事实的限定谓词。
    string summaryRegion = ExtractFilterRegion(StatisticsStatementText("CountRecognitionUsageSummaryGroups"), true);
    string reasonsRegion = ExtractFilterRegion(StatisticsStatementText("QueryRecognitionUsageSummaryReasons"), true);
    Assert.StartsWith(summaryRegion, reasonsRegion, StringComparison.Ordinal);
    Assert.Contains("and pr.recognition_result = 2", reasonsRegion, StringComparison.Ordinal);
    Assert.Contains("and pr.non_adoption_reason is not null", reasonsRegion, StringComparison.Ordinal);
    Assert.Contains("pr.recognition_time >= $PeriodStart and pr.recognition_time < $PeriodEnd", reasonsRegion, StringComparison.Ordinal);
  }

  /// <summary>
  /// V23：十七条统计语句不含数据库方言特征，也不调用上下文设置方法。
  /// </summary>
  /// <remarks>
  /// 分页窗口由数据映射器的 Provider 适配层附加，语句自身不出现任何分页语法；
  /// 条件聚合只用标准 CASE，原因汇总独立成行，不依赖方言字符串聚合。
  /// </remarks>
  [Fact]
  public void Statistics_statements_stay_provider_neutral()
  {
    foreach (string statementId in StatisticsStatementIds)
    {
      string statement = StatisticsStatementText(statementId);
      Assert.NotEmpty(statement);

      List<string> dialectHits = [];
      foreach ((string feature, string pattern) in DialectMarkers)
      {
        Match match = Regex.Match(statement, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success) dialectHits.Add($"{statementId}: {feature} => {match.Value}");
      }

      Assert.True(dialectHits.Count == 0, string.Join(" | ", dialectHits));
    }

    Assert.DoesNotContain("SetContext", File.ReadAllText(QueryMapPath()), StringComparison.Ordinal);

    // 变异证据：把分页方言注入语句副本后，同一条扫描必须命中。
    Assert.NotEmpty(MatchesAnyDialectMarker($"{StatisticsStatementText(StatisticsStatementIds[0])} limit 1"));
  }

  /// <summary>
  /// V23：汇总与明细的当页语句带稳定唯一的排序，计数语句不带排序；汇总排序键与分组键同套，分组行序即窗口序。
  /// </summary>
  /// <remarks>
  /// 汇总的排序键就是分组键本身：分组键元组在行集内唯一，排序自然跨页无重复无遗漏；
  /// 明细按业务时间倒序加匹配项标识升序兜底；窗口语法由 Provider 适配层附加，语句文本不出现分页子句。
  /// </remarks>
  [Fact]
  public void Statistics_page_statements_sort_stably_and_counts_do_not_sort()
  {
    foreach ((string countId, string _, bool _) in CountPagePairs)
    {
      Assert.DoesNotContain("order by", StatisticsStatementText(countId), StringComparison.OrdinalIgnoreCase);
    }

    Assert.Contains(
      "order by g.receiver_organization_code asc, g.receiver_hospital_code asc, g.receiver_branch_code asc, " +
      "g.recognition_dept_id asc, g.item_type asc, g.standard_project_code asc",
      StatisticsStatementText("QueryRecognitionUsageSummaryPage"),
      StringComparison.Ordinal);
    Assert.Contains(
      "order by g.source_organization_code asc, g.source_hospital_code asc, g.source_branch_code asc, " +
      "g.item_type asc, g.standard_project_code asc",
      StatisticsStatementText("QuerySourceRecognitionSummaryPage"),
      StringComparison.Ordinal);

    // 汇总分组行计数作用于聚合之后：计数语句先在派生表内 group by，再对分组行计数。
    Assert.Contains("select count(1) as total_count from (", StatisticsStatementText("CountRecognitionUsageSummaryGroups"), StringComparison.Ordinal);
    Assert.Contains("select count(1) as total_count from (", StatisticsStatementText("CountSourceRecognitionSummaryGroups"), StringComparison.Ordinal);
    Assert.Contains("group by", StatisticsStatementText("CountRecognitionUsageSummaryGroups"), StringComparison.Ordinal);

    // 明细按业务时间倒序加匹配项标识升序兜底；匹配记录视图的匹配项按标识升序。
    Assert.Contains("order by mr.match_created_time desc, mi.id asc", StatisticsStatementText("QueryRecognitionUsageReminderDetails"), StringComparison.Ordinal);
    Assert.Contains("order by pr.recognition_time desc, mi.id asc", StatisticsStatementText("QueryRecognitionUsageAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains("order by pr.recognition_time desc, mi.id asc", StatisticsStatementText("QueryRecognitionUsageNonAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains("order by rf.referenced_time desc, mi.id asc", StatisticsStatementText("QueryRecognitionUsageReferenceDetails"), StringComparison.Ordinal);
    Assert.Contains("order by pr.recognition_time desc, mi.id asc", StatisticsStatementText("QuerySourceRecognitionDetails"), StringComparison.Ordinal);
    Assert.Contains("order by mi.id asc", StatisticsStatementText("QueryRecognitionMatchRecordViewItems"), StringComparison.Ordinal);

    // 判别力：同一排序判定对真实语句判无违规；兜底键被截断的变异文本上，同一判定必须报出排序与期望不一致。
    string statement = StatisticsStatementText("QueryRecognitionUsageReminderDetails");
    Assert.Empty(FindSortClauseViolations(statement, "order by mr.match_created_time desc, mi.id asc"));
    Assert.NotEmpty(FindSortClauseViolations(
      statement.Replace("order by mr.match_created_time desc, mi.id asc", "order by mr.match_created_time desc", StringComparison.Ordinal),
      "order by mr.match_created_time desc, mi.id asc"));
  }

  /// <summary>
  /// S6-D2 的语句面：汇总分组键按维度口径书写——接收侧为接收组织加接收医院、接收院区、
  /// 「接收院区加处理结果互认科室ID」与匹配项标准项目编码，来源侧为报告主体来源组织加来源医院、来源院区与标准项目编码；
  /// 互认科室维度在行集内排除无处理结果的未反馈项。
  /// </summary>
  /// <remarks>
  /// 维度以参数 <c>$GroupDimension</c> 的标准 CASE 表达式切换，分组行内未参与分组的维度键为空值；
  /// 互认科室维度的展示名称取范围内互认时间最晚一条处理结果保存的名称，经派生表携带组内最晚互认时间后，
  /// 按科室ID与时间等值并经匹配记录把接收三值限定在本分组行内，取最小归一名称。
  /// </remarks>
  [Fact]
  public void Statistics_group_keys_match_the_dimension_contract()
  {
    string usage = StatisticsStatementText("QueryRecognitionUsageSummaryPage");

    // 分组键：接收组织与接收医院随医院、院区、互认科室三个维度携带，院区随院区与互认科室携带。
    Assert.Contains("case when $GroupDimension in (1, 2, 3) then mr.receiver_organization_code end", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension in (1, 2, 3) then mr.receiver_hospital_code end", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension in (2, 3) then mr.receiver_branch_code end", usage, StringComparison.Ordinal);
    // 互认科室维度按「接收院区加处理结果互认科室ID」归类；标准项目维度取匹配项编码并随行回填标准目录三层。
    Assert.Contains("case when $GroupDimension = 3 then pr.recognition_dept_id end", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then mi.standard_project_code end", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then sc.item_type end", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then sc.name end as category_name", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then sg.name end as group_name", usage, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then si.name end as standard_item_name", usage, StringComparison.Ordinal);

    // 互认科室维度的展示名称：组内最晚互认时间经派生表携带，外层按科室ID与时间等值取最小归一名称，
    // 名称候选经匹配记录把接收组织、接收医院与接收院区三值限定在本分组行内。
    Assert.Contains("max(case when pr.recognition_time >= $PeriodStart and pr.recognition_time < $PeriodEnd then pr.recognition_time end) as last_recognition_time", usage, StringComparison.Ordinal);
    Assert.Contains(
      "( select min(l.recognition_dept_name) from mrec_recognition_processing_result l " +
      "join mrec_recognition_match_record m on m.id = l.recognition_match_record_id " +
      "where l.recognition_dept_id = g.recognition_dept_id and l.recognition_time = g.last_recognition_time",
      usage,
      StringComparison.Ordinal);
    Assert.Contains(
      "and l.recognition_time >= $PeriodStart and l.recognition_time < $PeriodEnd " +
      "and m.receiver_organization_code = g.receiver_organization_code " +
      "and m.receiver_hospital_code = g.receiver_hospital_code " +
      "and m.receiver_branch_code = g.receiver_branch_code ) as recognition_dept_name",
      usage,
      StringComparison.Ordinal);

    // 未反馈匹配项无处理结果、无互认科室归属，不计入互认科室维度：维度取互认科室时行集排除无处理结果的行。
    foreach (string statementId in new[] { "CountRecognitionUsageSummaryGroups", "QueryRecognitionUsageSummaryPage", "QueryRecognitionUsageSummaryReasons" })
    {
      Assert.Contains(
        "and ( $GroupDimension <> 3 or pr.recognition_dept_id is not null )",
        StatisticsStatementText(statementId),
        StringComparison.Ordinal);
    }

    string source = StatisticsStatementText("QuerySourceRecognitionSummaryPage");
    Assert.Contains("case when $GroupDimension in (1, 2) then r.organization_code end", source, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 1 then r.hospital_code end", source, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 2 then r.branch_code end", source, StringComparison.Ordinal);
    Assert.Contains("case when $GroupDimension = 4 then mi.standard_project_code end", source, StringComparison.Ordinal);

    // 判别力：同一分组键判定对真实语句判无缺失；把互认科室分组键换成接收院区分组键构造变异文本后，
    // 同一判定必须恰好报出被换掉的互认科室键。
    Assert.Empty(FindMissingGroupKeyFragments(usage));
    string mutated = usage.Replace(
      "case when $GroupDimension = 3 then pr.recognition_dept_id end",
      "case when $GroupDimension = 3 then mr.receiver_branch_code end",
      StringComparison.Ordinal);
    Assert.Equal(
      ["case when $GroupDimension = 3 then pr.recognition_dept_id end"],
      [.. FindMissingGroupKeyFragments(mutated)]);
  }

  /// <summary>
  /// S6-D6、V21 的语句面：同一行内各指标按自身业务时间列独立过滤同一日期范围——
  /// 提醒按匹配生成时间、采纳与不采纳与金额按处理结果互认时间、引用按引用事实实际引用时间，日期边界参数化。
  /// </summary>
  /// <remarks>
  /// 汇总行集的三向时间或条件保证分组只由与统计范围相关的事实组成；明细各对按各自业务时间列取行。
  /// </remarks>
  [Fact]
  public void Statistics_metrics_filter_on_their_own_business_time_columns()
  {
    string reminderWindow = "mr.match_created_time >= $PeriodStart and mr.match_created_time < $PeriodEnd";
    string recognitionWindow = "pr.recognition_time >= $PeriodStart and pr.recognition_time < $PeriodEnd";
    string referenceWindow = "rf.referenced_time >= $PeriodStart and rf.referenced_time < $PeriodEnd";

    // 接收侧汇总：提醒 CASE 用匹配生成时间，采纳、不采纳与金额 CASE 用互认时间，引用 CASE 用实际引用时间；
    // 行集的三向或条件让每个指标的时间窗口各出现一次，组内最晚互认时间再出现一次。
    string usage = StatisticsStatementText("QueryRecognitionUsageSummaryPage");
    Assert.Equal(2, Regex.Matches(usage, Regex.Escape(reminderWindow), RegexOptions.CultureInvariant).Count);
    Assert.Equal(5, Regex.Matches(usage, Regex.Escape(recognitionWindow), RegexOptions.CultureInvariant).Count);
    Assert.Equal(2, Regex.Matches(usage, Regex.Escape(referenceWindow), RegexOptions.CultureInvariant).Count);
    Assert.Contains($"count(case when mi.id is not null and {reminderWindow} then 1 end)", usage, StringComparison.Ordinal);
    Assert.Contains($"count(case when pr.recognition_result = 1 and {recognitionWindow} then 1 end)", usage, StringComparison.Ordinal);
    Assert.Contains($"count(case when pr.recognition_result = 2 and {recognitionWindow} then 1 end)", usage, StringComparison.Ordinal);
    Assert.Contains($"count(case when rf.id is not null and {referenceWindow} then 1 end)", usage, StringComparison.Ordinal);
    Assert.Contains(
      $"sum(case when pr.recognition_result = 1 and {recognitionWindow} then pr.estimated_saving_amount end)",
      usage,
      StringComparison.Ordinal);
    Assert.Contains("coalesce(sum(", usage, StringComparison.Ordinal);

    // 提醒明细含未反馈项：只按匹配生成时间取行，不按处理结果排除。
    Assert.Contains(reminderWindow, StatisticsStatementText("CountRecognitionUsageReminderDetails"), StringComparison.Ordinal);
    string reminderPage = StatisticsStatementText("QueryRecognitionUsageReminderDetails");
    Assert.Contains(reminderWindow, reminderPage, StringComparison.Ordinal);
    Assert.DoesNotContain("pr.id is null", reminderPage, StringComparison.Ordinal);

    // 提醒明细承载处理结果决策列：处理结果与匹配项一对一关联不放大行数，已反馈行携带决策值，未反馈行随左联自然为空。
    Assert.Contains("pr.recognition_result as decision", reminderPage, StringComparison.Ordinal);

    // 采纳随金额、不采纳随原因、引用按引用事实：各对按自身业务时间列取行。
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("CountRecognitionUsageAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("QueryRecognitionUsageAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 2 and {recognitionWindow}", StatisticsStatementText("CountRecognitionUsageNonAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 2 and {recognitionWindow}", StatisticsStatementText("QueryRecognitionUsageNonAdoptionDetails"), StringComparison.Ordinal);
    Assert.Contains($"rf.id is not null and {referenceWindow}", StatisticsStatementText("CountRecognitionUsageReferenceDetails"), StringComparison.Ordinal);
    Assert.Contains($"rf.id is not null and {referenceWindow}", StatisticsStatementText("QueryRecognitionUsageReferenceDetails"), StringComparison.Ordinal);

    // 来源侧被互认次数只计采纳事实：行集即范围内采纳事实。
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("CountSourceRecognitionSummaryGroups"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("QuerySourceRecognitionSummaryPage"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("CountSourceRecognitionDetails"), StringComparison.Ordinal);
    Assert.Contains($"pr.recognition_result = 1 and {recognitionWindow}", StatisticsStatementText("QuerySourceRecognitionDetails"), StringComparison.Ordinal);
  }

  /// <summary>
  /// 汇总与明细的项目类型筛选同源：全部统计语句的项目类型筛选统一取匹配项自身保存值列，
  /// 与明细读模型的项目类型投影同源，同一项目类型条件下汇总与明细筛选同一批事实。
  /// </summary>
  /// <remarks>
  /// 汇总分组键与展示投影仍实时取自标准目录所属分类，与筛选列各自独立；
  /// 项目类型筛选列回退为标准目录列会让同一条件下汇总与明细的行集错位，这里逐条冻结。
  /// </remarks>
  [Fact]
  public void Statistics_statements_filter_item_type_on_the_match_item_column()
  {
    foreach (string statementId in ItemTypeFilteredStatementIds)
    {
      string statement = StatisticsStatementText(statementId);
      Assert.Contains("mi.item_type = $ItemType", statement, StringComparison.Ordinal);
      Assert.DoesNotContain("sc.item_type = $ItemType", statement, StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// V30 的语句面：明细与匹配记录视图的患者姓名取自匹配项绑定报告的报告主体检索列，证件号码取匹配记录保存值。
  /// </summary>
  /// <remarks>
  /// 报告主体按匹配项的报告标识一对一关联，姓名与证件均按业务原值投影，语句内不做任何脱敏处理。
  /// </remarks>
  [Fact]
  public void Statistics_detail_statements_carry_patient_columns_from_the_bound_report()
  {
    foreach (string statementId in new[]
    {
      "QueryRecognitionUsageReminderDetails", "QueryRecognitionUsageAdoptionDetails",
      "QueryRecognitionUsageNonAdoptionDetails", "QueryRecognitionUsageReferenceDetails",
      "QuerySourceRecognitionDetails"
    })
    {
      string statement = StatisticsStatementText(statementId);
      Assert.Contains("r.patient_name as patient_name", statement, StringComparison.Ordinal);
      Assert.Contains("mr.identity_document_no as identity_document_no", statement, StringComparison.Ordinal);
    }

    Assert.Contains("max(r.patient_name) as patient_name", StatisticsStatementText("QueryRecognitionMatchRecordView"), StringComparison.Ordinal);
  }

  /// <summary>
  /// 抽取语句中承载筛选条件的片段：汇总语句取分组前最后一个 where 到第一个 group by 之间，明细语句取 where 到 order by 之间。
  /// </summary>
  /// <param name="statement">已归一化的语句文本。</param>
  /// <param name="groupedRow">行集是否按分组键聚合。</param>
  /// <returns>筛选片段文本，首尾不含空白。</returns>
  private static string ExtractFilterRegion(string statement, bool groupedRow)
  {
    int cutoff = groupedRow
      ? statement.IndexOf("group by", StringComparison.OrdinalIgnoreCase)
      : statement.IndexOf(" order by ", StringComparison.OrdinalIgnoreCase);
    cutoff = cutoff < 0 ? statement.Length : cutoff;

    int start = 0;
    while (true)
    {
      int next = statement.IndexOf("where", start, StringComparison.OrdinalIgnoreCase);
      if (next < 0 || next >= cutoff) break;
      start = next + "where".Length;
    }

    return statement[start..cutoff].Trim();
  }

  /// <summary>
  /// 取某条统计语句去掉行内 XML 注释并折行后的文本。
  /// </summary>
  /// <param name="statementId">统计语句标识。</param>
  /// <returns>归一化后的语句文本。</returns>
  /// <exception cref="InvalidOperationException">该标识在查询映射文件里不是恰好一处时抛出。</exception>
  private static string StatisticsStatementText(string statementId)
  {
    XElement statement = Assert.Single(StatementElements(XDocument.Load(QueryMapPath())), element => (string?)element.Attribute("Id") == statementId);
    return NormalizeStatement(statement.Value);
  }

  /// <summary>去掉语句里的行内 XML 注释并把连续空白折成单个空格。</summary>
  /// <param name="statement">语句原文。</param>
  /// <returns>归一化后的语句文本。</returns>
  private static string NormalizeStatement(string statement) =>
    Regex.Replace(Regex.Replace(statement, "<!--.*?-->", " ", RegexOptions.Singleline), @"\s+", " ").Trim();

  /// <summary>判断文本是否命中任一数据库方言特征。</summary>
  /// <param name="text">待扫描的 SQL 文本。</param>
  /// <returns>命中的特征与匹配文本。</returns>
  private static IReadOnlyList<string> MatchesAnyDialectMarker(string text) =>
    [
      .. DialectMarkers
        .Select(marker => (marker.Feature, Match: Regex.Match(text, marker.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
        .Where(hit => hit.Match.Success)
        .Select(hit => $"{hit.Feature} => {hit.Match.Value}")
    ];

  /// <summary>
  /// 找出语句排序子句与期望稳定排序不一致的违规项：排序子句缺失、多余排序键或方向不符都会报出。
  /// </summary>
  /// <param name="statement">已归一化的语句文本。</param>
  /// <param name="expectedOrderBy">期望的完整排序子句。</param>
  /// <returns>违规说明清单，语句排序子句与期望逐字一致时为空清单。</returns>
  private static IReadOnlyList<string> FindSortClauseViolations(string statement, string expectedOrderBy)
  {
    int start = statement.IndexOf("order by", StringComparison.OrdinalIgnoreCase);
    string actual = start < 0 ? string.Empty : statement[start..].Trim();
    return string.Equals(actual, expectedOrderBy, StringComparison.Ordinal)
      ? []
      : [$"排序子句与期望不一致：期望 {expectedOrderBy}，实际 {actual}"];
  }

  /// <summary>
  /// 找出互认使用汇总语句缺失的维度分组键片段：接收侧四个维度的分组键 CASE 表达式逐项核对，缺失即返回。
  /// </summary>
  /// <param name="statement">已归一化的语句文本。</param>
  /// <returns>缺失的分组键片段清单，分组键齐全时为空清单。</returns>
  private static IReadOnlyList<string> FindMissingGroupKeyFragments(string statement) =>
    [
      .. new[]
      {
        "case when $GroupDimension in (1, 2, 3) then mr.receiver_organization_code end",
        "case when $GroupDimension in (1, 2, 3) then mr.receiver_hospital_code end",
        "case when $GroupDimension in (2, 3) then mr.receiver_branch_code end",
        "case when $GroupDimension = 3 then pr.recognition_dept_id end",
        "case when $GroupDimension = 4 then sc.item_type end",
        "case when $GroupDimension = 4 then mi.standard_project_code end"
      }.Where(fragment => !statement.Contains(fragment, StringComparison.Ordinal))
    ];

  /// <summary>查询映射文件声明的命名空间；元素与属性都需要按限定名查找。</summary>
  private static readonly XNamespace SqlMapNamespace = "http://dysoft.vip/schemas/EarthraceSqlMap.xsd";

  /// <summary>取映射文件里全部 Statement 节点，按声明顺序排列。</summary>
  /// <param name="document">已加载的映射文件。</param>
  /// <returns>按声明顺序排列的 Statement 节点。</returns>
  private static IReadOnlyList<XElement> StatementElements(XDocument document) =>
    document.Root is null
      ? throw new InvalidOperationException("映射文件缺少根节点。")
      : [.. document.Root.Elements(SqlMapNamespace + "Statements").Elements(SqlMapNamespace + "Statement")];

  /// <summary>取映射文件里全部 Statement 节点的标识，按声明顺序排列。</summary>
  /// <param name="document">已加载的映射文件。</param>
  /// <returns>按声明顺序排列的语句标识。</returns>
  private static string[] StatementIds(XDocument document) =>
    [.. StatementElements(document).Select(element => (string)element.Attribute("Id")!)];

  /// <summary>在查询映射文件所在目录内定位查询侧映射。</summary>
  /// <returns>查询映射文件的绝对路径。</returns>
  private static string QueryMapPath() => SourceSyntaxGuard.FindRepositoryFile("Queries", "MedicalRecognitionReportQuery.xml");

  /// <summary>
  /// 抽出一条索引的完整声明文本：从 create index 起到 on 子句的终止分号。
  /// </summary>
  /// <param name="script">已做行尾归一的脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>声明文本，含终止分号。</returns>
  /// <exception cref="InvalidOperationException">脚本中不存在该索引的普通索引声明时抛出。</exception>
  private static string ExtractIndexDeclaration(string script, string indexName)
  {
    string marker = $"create index {indexName}";
    int start = script.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"脚本中未找到 {marker}。");
    int end = script.IndexOf(';', start);
    return script[start..(end + 1)];
  }

  /// <summary>
  /// 抽出一条索引的中文注释行：从 comment on index 起到文案的收尾分号。
  /// </summary>
  /// <param name="script">已做行尾归一的脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>注释行文本，含收尾分号。</returns>
  /// <exception cref="InvalidOperationException">脚本中不存在该索引的注释时抛出。</exception>
  private static string ExtractIndexComment(string script, string indexName)
  {
    string marker = $"comment on index {indexName} is '";
    int start = script.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"脚本中未找到 {marker}。");
    int end = script.IndexOf("';", start, StringComparison.Ordinal);
    return script[start..(end + 2)];
  }

  /// <summary>
  /// 抽出一条索引声明的列清单，按声明顺序排列。
  /// </summary>
  /// <param name="script">已做行尾归一的脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>列名清单。</returns>
  private static string[] ExtractIndexColumns(string script, string indexName)
  {
    string declaration = ExtractIndexDeclaration(script, indexName);
    int open = declaration.IndexOf(" (", StringComparison.Ordinal);
    int close = declaration.LastIndexOf(')');
    return [.. declaration[(open + 2)..close].Split(',').Select(column => column.Trim())];
  }

  /// <summary>
  /// 统计一条索引在脚本中的声明次数，普通与唯一两种动词形态都计入。
  /// </summary>
  /// <param name="script">脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>声明次数。</returns>
  private static int CountDeclarations(string script, string indexName) =>
    Regex.Matches(script, $"create (unique )?index {indexName}\\b", RegexOptions.CultureInvariant).Count;

  /// <summary>
  /// 把脚本文本的行尾归一为换行符，使断言不受操作系统差异影响。
  /// </summary>
  /// <param name="text">原始文本。</param>
  /// <returns>归一后的文本。</returns>
  private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

  /// <summary>
  /// 把 server 目录内相对路径解析为绝对路径。
  /// </summary>
  /// <param name="relativePath">server 目录内相对路径，使用 <c>/</c> 分隔。</param>
  /// <returns>该路径的绝对路径。</returns>
  private static string ResolveServerFile(string relativePath) =>
    Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), "server", relativePath.Replace('/', Path.DirectorySeparatorChar));
}
