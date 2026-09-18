using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 的 DDL 与 SqlMap 静态守卫：冻结组织医院院区互认项目金额建表脚本的物理形态，
/// 以及金额 SqlMap 的作用域、语句标识、目标表与 Provider 中立性。
/// </summary>
/// <remarks>
/// V23、V24 的静态子面。这些内容改名或漏写既不会编译失败，也不会让既有阶段 1/2 用例失败，
/// 因此必须在这里独立冻结；真实库的表结构、注释与索引属于建表完成后的验收面，不在本文件内验证。
/// </remarks>
public sealed class Stage3SqlMapTests
{
  /// <summary>目标物理表名，带本项目 <c>mrec_</c> 前缀。</summary>
  private const string TableName = "mrec_organization_hospital_branch_recognition_amount";

  /// <summary>目标物理表名去掉 <c>mrec_</c> 前缀后的形态，只用于校验"未加前缀的表名能被判据认出"。</summary>
  private const string UnprefixedTableName = "organization_hospital_branch_recognition_amount";

  /// <summary>四列业务键上的唯一索引名。</summary>
  private const string UniqueIndexName = "ux_mrec_org_hos_brh_project";

  /// <summary>金额 SqlMap 的实体作用域名。</summary>
  private const string AmountScope = "OrganizationHospitalBranchRecognitionAmount";

  /// <summary>V23：DDL 列为 <c>varchar(n)</c> 时必须失败；本项目字符串列没有已确认的业务长度上限。</summary>
  private const string VarcharMarker = "varchar(";

  /// <summary>
  /// 建表脚本的列序（含可空性口径），逐项与设计列序表一致；
  /// 状态标志列不存在，因此操作字段严格位于列尾。
  /// </summary>
  private static readonly (string Column, string Type)[] ExpectedColumns =
  [
    ("id", "uuid not null"),
    ("organization_code", "text not null"),
    ("hospital_code", "text not null"),
    ("branch_code", "text not null"),
    ("standard_project_code", "text not null"),
    ("current_amount", "numeric(18,2) not null"),
    ("oper_time", "timestamptz not null"),
    ("oper_id", "uuid not null")
  ];

  /// <summary>
  /// 金额 SqlMap 的语句标识清单，顺序与设计清单一致；写入语句标识由仓储方法名推导
  /// （方法名去掉 <c>Async</c> 后缀），因此拼写与集合都必须逐字冻结。
  /// </summary>
  private static readonly string[] ExpectedStatementIds =
  [
    "OrganizationHospitalBranchRecognitionAmountColumns",
    "GetOrganizationHospitalBranchRecognitionAmountByBusinessKey",
    "CreateOrganizationHospitalBranchRecognitionAmount",
    "UpdateOrganizationHospitalBranchRecognitionAmount"
  ];

  /// <summary>
  /// 共享 SQL 的方言特征：出现即视为违反 Provider 中立约定。
  /// </summary>
  private static readonly string[] DialectMarkers =
  [
    "on conflict",
    "limit ",
    "offset ",
    "nulls first",
    "nulls last",
    "count(distinct",
    "::",
    "current_timestamp"
  ];

  /// <summary>
  /// V23：建表脚本冻结物理表名、表注释、列序、列类型与可空性，并且不含 <c>varchar(n)</c>、
  /// 数据库默认值、跨系统外键与后续变更语句。
  /// </summary>
  /// <remarks>
  /// 列序与类型同时决定投影绑定与写入稳定性：加列、换序或放宽可空性都不会让任何编译或 SqlMap 用例失败。
  /// </remarks>
  [Fact]
  public void Amount_ddl_freezes_physical_table_columns_types_and_negatives()
  {
    string script = File.ReadAllText(FindRepositoryFile("mrec_organization_hospital_branch_recognition_amount.sql"));

    Assert.Contains($"create table {TableName} (", script, StringComparison.Ordinal);

    // 列序与类型逐项等值：只断言"包含某列名"时，换序、改类型或去掉 not null 都能静默通过。
    (string Column, string Type)[] declaredColumns = [.. ExtractCreateTableColumnLines(script)
      .Select(line =>
      {
        int separator = line.IndexOf(' ');
        return (Column: line[..separator], Type: line[(separator + 1)..]);
      })];
    Assert.Equal(ExpectedColumns, declaredColumns);

    // 变异证据：把金额列类型从 numeric(18,2) 改为 numeric(18,4) 后，同一条等值判据必须判出不相等。
    const string expectedAmountType = "numeric(18,2) not null";
    string widenedAmountType = expectedAmountType.Replace("18,2", "18,4", StringComparison.Ordinal);
    Assert.NotEqual(expectedAmountType, widenedAmountType);
    Assert.NotEqual(
      ExpectedColumns,
      declaredColumns.Select(column =>
        column.Column == "current_amount" ? (column.Column, widenedAmountType) : column).ToArray());

    // 主键：脚本必须以独立的一行显式声明 id 为主键，且整份脚本只有这一处。
    Assert.Single(
      script.Split('\n'),
      line =>
      {
        string trimmed = line.Trim();
        return trimmed.StartsWith("primary key", StringComparison.OrdinalIgnoreCase) &&
          trimmed.EndsWith("(id)", StringComparison.OrdinalIgnoreCase);
      });

    // 四条负向判据：文本列不用带长度上限的 varchar、全部列无数据库默认值、不建跨系统外键、脚本不做二次结构变更。
    Assert.DoesNotContain(VarcharMarker, script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain(" default ", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("references ", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("foreign key", script, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("alter table", script, StringComparison.OrdinalIgnoreCase);

    // 负向判据的判别力：把负向特征注入脚本副本后必须命中，证明上面的断言不是对任何文本都成立。
    Assert.Contains(VarcharMarker, script.Replace("text not null", "varchar(100) not null", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    Assert.Contains(" default ", script.Replace("uuid not null", "uuid not null default gen_random_uuid()", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    Assert.Contains("references ", script.Replace("oper_id uuid not null", "oper_id uuid not null references other_system_user (id)", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    Assert.Contains("alter table", script + "\nalter table " + TableName + " add column probe text;", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// V23：表注释、每列中文注释与唯一索引名、覆盖列、无状态过滤、索引注释与设计逐字一致。
  /// </summary>
  /// <remarks>
  /// 注释条数与目标集合一起冻结：漏写某一列注释时，下游无法从数据库元数据得知该列业务含义。
  /// </remarks>
  [Fact]
  public void Amount_ddl_freezes_table_column_and_index_comments()
  {
    string script = File.ReadAllText(FindRepositoryFile("mrec_organization_hospital_branch_recognition_amount.sql"));
    string[] commentLines = [.. script.Split('\n')
      .Select(line => line.Trim())
      .Where(line => line.StartsWith("comment on ", StringComparison.Ordinal))];

    // 注释条数：1 条表注释 + 8 条列注释 + 1 条索引注释。
    Assert.Equal(10, commentLines.Length);
    Assert.Single(commentLines, line => line == $"comment on table {TableName} is '组织医院院区互认项目金额';");
    Assert.Single(commentLines, line => line == $"comment on index {UniqueIndexName} is '同一组织医院院区标准项目金额唯一';");
    Assert.Equal(8, commentLines.Count(line => line.StartsWith($"comment on column {TableName}.", StringComparison.Ordinal)));

    // 每列注释逐条等值，文案取自设计列序表的中文列注释。
    (string Column, string Comment)[] expectedColumnComments =
    [
      ("id", "主键ID"),
      ("organization_code", "组织编码"),
      ("hospital_code", "医院编码"),
      ("branch_code", "院区编码"),
      ("standard_project_code", "标准项目编码"),
      ("current_amount", "当前金额"),
      ("oper_time", "操作时间"),
      ("oper_id", "操作人")
    ];
    foreach ((string column, string comment) in expectedColumnComments)
    {
      Assert.Contains(commentLines, line => line == $"comment on column {TableName}.{column} is '{comment}';");
    }

    // 注释必须紧跟建表语句：索引先建、注释未提交时，列在库里没有业务说明。
    int createIndex = script.IndexOf("create table ", StringComparison.Ordinal);
    int firstComment = script.IndexOf("comment on ", StringComparison.Ordinal);
    Assert.True(createIndex >= 0 && firstComment > createIndex);

    // 唯一索引：固定名称 + 四列业务键 + 无状态过滤，是同一业务键并发首次保存的唯一约束兜底。
    Assert.Contains($"create unique index {UniqueIndexName}", script, StringComparison.Ordinal);
    Assert.Contains(
      $"on {TableName} (organization_code, hospital_code, branch_code, standard_project_code);",
      script,
      StringComparison.Ordinal);
    // 无状态过滤：创建索引语句里不得出现 where，否则索引只覆盖部分行、唯一性不再覆盖全表。
    string indexStatement = script[script.IndexOf($"create unique index {UniqueIndexName}", StringComparison.Ordinal)..];
    indexStatement = indexStatement[..(indexStatement.IndexOf(';') + 1)];
    Assert.DoesNotContain("where", indexStatement, StringComparison.OrdinalIgnoreCase);

    // 变异证据：给索引副本加上状态过滤后，判据必须判出包含 where。
    Assert.Contains("where", indexStatement.Replace(";", " where is_valid = true;", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// V24：金额 SqlMap 的作用域为实体名、语句标识清单与设计一致，且已不再保留无 where 的全量查询语句。
  /// </summary>
  /// <remarks>
  /// 作用域曾为聚合名 <c>MedicalRecognitionReport</c>，框架按 <c>{作用域}.{语句标识}</c> 查找语句，
  /// 作用域或标识不一致会让调用点在实际运行时报"找不到语句"，而静态检查不会失败。
  /// </remarks>
  [Fact]
  public void Amount_sql_map_freezes_scope_statement_ids_and_removes_unused_query()
  {
    XDocument document = XDocument.Load(FindRepositorySqlMap("OrganizationHospitalBranchRecognitionAmount.xml"));

    Assert.Equal(AmountScope, (string?)document.Root!.Attribute("Scope"));
    Assert.NotEqual("MedicalRecognitionReport", (string?)document.Root.Attribute("Scope"));

    string[] statementIds = [.. document.Descendants()
      .Where(element => element.Attribute("Id") is not null)
      .Select(element => (string)element.Attribute("Id")!)];
    Assert.Equal(ExpectedStatementIds, statementIds);

    // 原无业务调用点的无 where 全量查询语句必须消失：它既不满足业务键定位语义，也没有调用方。
    Assert.DoesNotContain("QueryAllOrganizationHospitalBranchRecognitionAmount", document.ToString(), StringComparison.Ordinal);
    XElement businessKeyStatement = document.Descendants()
      .Single(element => (string?)element.Attribute("Id") == "GetOrganizationHospitalBranchRecognitionAmountByBusinessKey");
    Assert.Contains("where organization_code = $OrganizationCode", businessKeyStatement.Value, StringComparison.Ordinal);

    // 每条手写语句都有说明职责的 XML 注释。
    Assert.All(
      document.Descendants().Where(element => element.Attribute("Id") is not null),
      statement => Assert.True(
        statement.PreviousNode is XComment,
        $"语句 {statement.Attribute("Id")?.Value} 缺少 XML 注释"));

    // 列清单语句的列序与建表脚本列序一致：oper_id 与 oper_time 的顺序被冻结。
    // 判据必须在整段语句文本上取列名出现位置，不能只在"含 standard_project_code 的那一行"上取：
    // 列清单一个列一行，该行只有自己那一列，行内断言对任何列序都得不到 oper_time，属恒假判据。
    string columnsStatement = document.Descendants()
      .Single(element => (string?)element.Attribute("Id") == "OrganizationHospitalBranchRecognitionAmountColumns").Value;
    Assert.True(IsOperationTimeBeforeOperatorId(columnsStatement));

    // 变异证据：把列清单副本里的 oper_id 与 oper_time 对调后，同一条判据必须判为顺序错误；
    // 只断言"两个列名都出现"时，列序被对调不会让任何用例失败。
    string swappedColumns = SwapOperationColumns(columnsStatement);
    Assert.NotEqual(columnsStatement, swappedColumns);
    Assert.False(IsOperationTimeBeforeOperatorId(swappedColumns));
  }

  /// <summary>
  /// V24：金额 SqlMap 只访问 <c>mrec_</c> 前缀表，目标表名精确等值，且不调用 <c>SetContext</c>、不含方言特征。
  /// </summary>
  /// <remarks>
  /// 作用域在本项目中一律由调用点显式传 <c>scope</c>，共享映射器的可变作用域依赖已被移除；
  /// 方言特征会让共享 SQL 绑定到单一数据库 Provider。
  /// </remarks>
  [Fact]
  public void Amount_sql_map_targets_platform_tables_only_and_stays_provider_neutral()
  {
    string xml = File.ReadAllText(FindRepositorySqlMap("OrganizationHospitalBranchRecognitionAmount.xml"));
    XDocument document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

    // 不调用 SetContext：出现即说明作用域又被改成运行时可变状态。
    Assert.DoesNotContain("SetContext", xml, StringComparison.Ordinal);

    // 全文件的表名引用都必须是带 mrec_ 前缀的本平台表。
    string[] tableNames = [.. TableNameReferences(xml).Distinct(StringComparer.Ordinal)];
    Assert.NotEmpty(tableNames);
    Assert.All(tableNames, tableName => Assert.StartsWith("mrec_", tableName, StringComparison.Ordinal));
    Assert.Equal([TableName], tableNames);

    // 变异证据：把表名的 mrec_ 前缀去掉后，同一条判据必须判出未加前缀的表名。
    // 注意 TableName 常量自身已带前缀，替换时直接以 TableName 为被替换文本，不能再拼一次 mrec_，
    // 否则替换文本在文件中不存在、Replace 成为空操作，"去掉前缀"这条证据会变成恒真断言。
    string unprefixed = xml.Replace(TableName, UnprefixedTableName, StringComparison.Ordinal);
    Assert.NotEqual(xml, unprefixed);
    // 与 TableNameReferences 的真实产出比较，而不是拿预期表名字面量去比：
    // 判据产出的是正则捕获的表名文本，直接与本用例自己的预期常量比较才能同时暴露"漏识别"与"识别出多余表名"。
    Assert.Contains(UnprefixedTableName, TableNameReferences(unprefixed));
    // 去前缀后的表名不再以 mrec_ 开头，因此它只用于证明判定能认出未加前缀的表名，不参与上面的前缀断言。
    Assert.DoesNotContain(TableName, TableNameReferences(unprefixed));

    // 写语句目标表与读取语句目标表逐条等值，避免语句被指向别的表。
    Dictionary<string, string> statements = document.Descendants()
      .Where(element => element.Attribute("Id") is not null)
      .ToDictionary(element => (string)element.Attribute("Id")!, element => element.Value);
    Assert.Equal(TableName, StatementTargetTable(statements["CreateOrganizationHospitalBranchRecognitionAmount"], "insert into"));
    Assert.Equal(TableName, StatementTargetTable(statements["UpdateOrganizationHospitalBranchRecognitionAmount"], "update"));
    Assert.Equal(TableName, StatementTargetTable(statements["GetOrganizationHospitalBranchRecognitionAmountByBusinessKey"], "select", "from"));

    // 操作时间显式绑定命令时间，不使用数据库当前时间替代。
    Assert.Contains("$OperTime", statements["CreateOrganizationHospitalBranchRecognitionAmount"], StringComparison.Ordinal);
    Assert.Contains("oper_time = $OperTime", statements["UpdateOrganizationHospitalBranchRecognitionAmount"], StringComparison.Ordinal);

    // 方言特征扫描：把命中的标记打进输出，失败时可直接看到违规词。
    List<string> dialectHits = [];
    foreach (string marker in DialectMarkers)
    {
      if (xml.Contains(marker, StringComparison.OrdinalIgnoreCase)) dialectHits.Add(marker);
    }

    Console.WriteLine($"Amount SqlMap dialect marker hits: {(dialectHits.Count == 0 ? "none" : string.Join(", ", dialectHits))}");
    Assert.Empty(dialectHits);
  }

  /// <summary>
  /// 取出建表语句括号内的列定义行，跳过空行与主键语句。
  /// </summary>
  /// <param name="script">建表脚本全文。</param>
  /// <returns>按脚本顺序排列的列定义行，已去掉行首尾空白。</returns>
  /// <exception cref="InvalidOperationException">脚本中不存在目标表的建表语句时抛出。</exception>
  private static IReadOnlyList<string> ExtractCreateTableColumnLines(string script)
  {
    string marker = $"create table {TableName} (";
    int start = script.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"脚本中未找到 {marker}。");

    string body = script[(start + marker.Length)..];
    body = body[..body.IndexOf(");", StringComparison.Ordinal)];
    return [.. body.Split('\n')
      .Select(line => line.Trim().TrimEnd(','))
      .Where(line => line.Length > 0 && !line.StartsWith("primary key", StringComparison.OrdinalIgnoreCase))];
  }

  /// <summary>
  /// 判断金额 SqlMap 列清单语句里 <c>oper_time</c> 是否位于 <c>oper_id</c> 之前，即与建表脚本列序一致。
  /// </summary>
  /// <remarks>
  /// 判据在整段语句文本上取两个列名的出现位置，并要求两者都存在：
  /// 只写 <c>IndexOf("oper_time") &lt; IndexOf("oper_id")</c> 时，两个列名都被删掉会取到 -1 与 -1、
  /// -1 &lt; -1 为假，虽然这里恰好会失败，但同形判据在"只缺 oper_time"时也会取到 -1 而静默判真，
  /// 因此显式断言两个列名都必须出现。
  /// </remarks>
  /// <param name="columnsStatement">列清单语句文本。</param>
  /// <returns>两个列名都存在且 <c>oper_time</c> 在前时为 <see langword="true"/>。</returns>
  private static bool IsOperationTimeBeforeOperatorId(string columnsStatement)
  {
    int timeIndex = columnsStatement.IndexOf("oper_time", StringComparison.Ordinal);
    int idIndex = columnsStatement.IndexOf("oper_id", StringComparison.Ordinal);
    return timeIndex >= 0 && idIndex >= 0 && timeIndex < idIndex;
  }

  /// <summary>
  /// 对调列清单文本中的 <c>oper_time</c> 与 <c>oper_id</c> 两个列名，用于生成列序被改动的变异副本。
  /// </summary>
  /// <remarks>
  /// 按"列名出现在行尾注释之前"定位列所在行并交换整行，不按整段文本的下标区间拼接：
  /// 列名同样会出现在行尾注释里（例如 <c>&lt;!-- 操作人标识 --&gt;</c>），
  /// 全局下标定位会取到注释里的列名，使对调变成空操作、变异证据失效。
  /// </remarks>
  /// <param name="columnsStatement">列清单语句文本。</param>
  /// <returns>两个列所在行互换位置后的文本；任一列所在行未唯一命中时返回原文。</returns>
  private static string SwapOperationColumns(string columnsStatement)
  {
    string[] lines = columnsStatement.Split('\n');
    int timeIndex = FindColumnLineIndex(lines, "oper_time");
    int idIndex = FindColumnLineIndex(lines, "oper_id");
    if (timeIndex < 0 || idIndex < 0) return columnsStatement;

    (lines[timeIndex], lines[idIndex]) = (lines[idIndex], lines[timeIndex]);
    return string.Join('\n', lines);
  }

  /// <summary>
  /// 在列清单的行集合中定位某一列所在的行号：该行去掉行尾注释后必须恰为该列名，可带尾随逗号。
  /// </summary>
  /// <remarks>
  /// 尾随逗号可选，因为列清单最后一列（<c>oper_id</c>）没有逗号；
  /// 只按"列名 + 逗号"匹配时最后一列会取不到，对调退回空操作。
  /// </remarks>
  /// <param name="lines">列清单语句按换行拆出的行集合。</param>
  /// <param name="columnName">列名。</param>
  /// <returns>命中的行号；没有命中或多行同时命中时为 <c>-1</c>。</returns>
  private static int FindColumnLineIndex(string[] lines, string columnName)
  {
    string withComma = $"{columnName},";
    int found = -1;
    for (int index = 0; index < lines.Length; index++)
    {
      string trimmed = lines[index].Split("<!--", StringSplitOptions.None)[0].Trim();
      if (trimmed != withComma && trimmed != columnName) continue;
      if (found >= 0) return -1;
      found = index;
    }

    return found;
  }

  /// <summary>
  /// 收集文本中 from/join/insert into/update 之后引用的表名。
  /// </summary>
  /// <remarks>
  /// 只匹配"关键字 + 空格 + 单词"的形态，因此 <c>from mrec_x</c>、<c>join mrec_x</c> 与换行后的同形写法都能认出；
  /// 判据用于确认映射只指向本平台表，不用于解析任意 SQL 方言。
  /// </remarks>
  /// <param name="text">SqlMap 语句或整个映射文件文本。</param>
  /// <returns>按出现顺序排列的表名；没有引用时为可变空列表，由调用方断言非空。</returns>
  private static List<string> TableNameReferences(string text) =>
  [
    .. Regex.Matches(text, @"\b(?:from|join|insert\s+into|update)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
      .Select(match => match.Groups[1].Value)
  ];

  /// <summary>
  /// 取写语句或读取语句的目标表名，用于按等值断言冻结物理表。
  /// </summary>
  /// <param name="statement">语句文本。</param>
  /// <param name="verb">语句起始动词，例如 <c>insert into</c>、<c>update</c>、<c>select</c>。</param>
  /// <param name="tableMarker">取表名前的定位标记：写语句为动词本身，读取语句为 <c>from</c>。</param>
  /// <returns>动词（或 <c>from</c>）之后的表名。</returns>
  /// <exception cref="InvalidOperationException">语句不以该动词开头，或该动词之后找不到定位标记时抛出，避免判据静默取到空值。</exception>
  private static string StatementTargetTable(string statement, string verb, string? tableMarker = null)
  {
    string trimmed = statement.Trim();
    if (!trimmed.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException($"语句应以 {verb} 开头，实际为：{trimmed[..Math.Min(40, trimmed.Length)]}。");
    }

    string remainder = trimmed[verb.Length..];
    string marker = tableMarker ?? string.Empty;
    if (marker.Length > 0)
    {
      int markerIndex = remainder.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      if (markerIndex < 0) throw new InvalidOperationException($"语句中未找到表名定位标记 {marker}。");
      remainder = remainder[(markerIndex + marker.Length)..];
    }

    // 表名之后可能紧跟换行与空白（读取语句的 from 子句后就是行尾），因此必须按空白切词并去掉尾随逗号，
    // 否则取到的是带换行的片段，与表名常量的等值断言会失败在与表名无关的排版上。
    return remainder.TrimStart().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd(',');
  }

  /// <summary>
  /// 在仓储工程内按文件名定位任意 SqlMap 或建表脚本文件。
  /// </summary>
  /// <param name="fileName">文件名，例如 <c>OrganizationHospitalBranchRecognitionAmount.xml</c>。</param>
  /// <returns>仓储工程中该文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">从测试程序集所在目录逐级向上都未唯一命中该文件时抛出；此时检查仓储工程的文件是否已随源码保留在原位。</exception>
  private static string FindRepositoryFile(string fileName)
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var repositoryDirectory = Path.Combine(directory.FullName, "Dy.MedicalRecognition.Repository");
      if (Directory.Exists(repositoryDirectory))
      {
        var matches = Directory.GetFiles(repositoryDirectory, fileName, SearchOption.AllDirectories);
        if (matches.Length == 1) return matches[0];
      }

      directory = directory.Parent;
    }

    throw new FileNotFoundException($"未找到仓储文件 '{fileName}'。");
  }

  /// <summary>
  /// 定位仓储工程中的金额 SqlMap 文件，供断言直接读取语句文本。
  /// </summary>
  /// <param name="fileName">SqlMap 文件名。</param>
  /// <returns>该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">仓储工程中未找到该 SqlMap 文件时抛出。</exception>
  private static string FindRepositorySqlMap(string fileName) => FindRepositoryFile(fileName);
}
