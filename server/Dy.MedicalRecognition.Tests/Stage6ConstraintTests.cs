using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 6 的跨接口静态收口：统计与导出链零写入，统计语句只承载读取动词。
/// </summary>
/// <remarks>
/// 覆盖后端设计验证矩阵 V10（统计链零写入静态守卫）与「外部调用、失败语义与静态守卫」节的客户端路径扫描扩展
/// （docs/plans/008-阶段6-互认统计与导出/Server/design.md「静态守卫同批项」段与「验证矩阵」章）：
/// 统计与导出链无事务声明、无事件登记符号、无写语句、无状态变更调用，阶段 6 的新增面也没有未登记的异常处理位置，
/// 业务失败按原样抛出。判定沿用阶段 5 收口守卫的口径：C# 判据按 Roslyn 语法节点执行，注释与字符串里的同名写法不产生节点；
/// 语句动词按去注释归一后的文本做整词匹配。每条判据都带变异证据：把违规形态注入同一判定函数后必须报出违规。
/// </remarks>
public sealed class Stage6ConstraintTests
{
  /// <summary>
  /// 统计与导出链的源文件，路径相对仓库根目录并使用 <c>/</c> 分隔，按设计类型归属表冻结。
  /// </summary>
  /// <remarks>
  /// 链由查询入口接口分片、应用层统计分片、应用层导出分片、查询端口 Statistics 分片、
  /// 查询仓储 Statistics 分片与导出控制器组成。查询侧仓储与查询端口的写入面缺口由阶段 5 收口守卫的
  /// 按目录全量断言补齐（Statistics 分片自动纳入其扫描面），本清单负责链内逐文件的
  /// 事务、事件、状态变化与异常处理四类判定。
  /// </remarks>
  private static readonly string[] StatisticsChainSources =
  [
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Statistics.cs",
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs",
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.StatisticsExport.cs",
    "server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.Statistics.cs",
    "server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.Statistics.cs",
    "server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs"
  ];

  /// <summary>阶段 6 追加进查询作用域的十七条统计语句标识，按映射文件内的声明顺序排列。</summary>
  /// <remarks>语句位置、共用筛选与排序结构由阶段 6 映射守卫冻结，本清单只承载动词判定面。</remarks>
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

  /// <summary>
  /// 统计语句的写动词标记：出现任一标记即说明该语句变更数据。
  /// </summary>
  /// <remarks>
  /// 按整词匹配：updated_time 这类携带动词词根的列名不产生命中；
  /// 语句里的行内 XML 注释先被移除，注释里的示例动词同样不参与判定。
  /// </remarks>
  private static readonly (string Feature, string Pattern)[] WriteVerbPatterns =
  [
    ("写入动词 insert", @"\binsert\b"),
    ("写入动词 update", @"\bupdate\b"),
    ("写入动词 delete", @"\bdelete\b"),
    ("写入动词 truncate", @"\btruncate\b"),
    ("写入动词 merge into", @"\bmerge\s+into\b"),
    ("写入动词 create", @"\bcreate\b"),
    ("写入动词 alter", @"\balter\b"),
    ("写入动词 drop", @"\bdrop\b")
  ];

  /// <summary>
  /// 写入型数据映射成员名：统计与导出链出现任一名称即说明该链承担了写入。
  /// </summary>
  /// <remarks>与阶段 5 收口守卫同口径：插入、更新、删除与执行语句四类成员覆盖写入面。</remarks>
  private static readonly string[] WriteMapperMembers =
  [
    "InsertAsync", "UpdateAsync", "DeleteAsync", "DeleteAllAsync", "ExecuteAsync", "BulkInsertAsync"
  ];

  /// <summary>领域事件登记成员名；统计与导出链不登记任何事件。</summary>
  private static readonly string[] EventRegistrationMembers =
  [
    "AddEvent", "AddEvents", "EnqueueEvent", "RegisterEvent", "Enqueue", "FlushAsync", "PublishAsync"
  ];

  /// <summary>
  /// 状态变更方法名的前缀：统计与导出链不产生任何持久化状态变化。
  /// </summary>
  /// <remarks>
  /// 与阶段 5 收口守卫的前缀集合相比不含 <c>Append</c>：导出装配在方法内的内存单元格列表上追加取值
  /// （如 <c>AppendExportTypeTailCells</c>），追加对象随方法返回丢弃，无持久化状态面；
  /// 聚合与领域对象的状态流转成员仍由其余六个前缀判定。
  /// </remarks>
  private static readonly string[] StateChangeMemberPrefixes =
  [
    "Enable", "Disable", "Void", "Mark", "Consume", "Invalidate"
  ];

  /// <summary>
  /// V10：统计与导出链不声明事务、不调用写入型映射成员、不登记事件、不调用状态变更方法，
  /// 也没有任何 try 语句——阶段 6 对异常处理位置的登记面为空。
  /// </summary>
  /// <remarks>
  /// 统计与导出是只读投影：不写入数据库、不登记事件、不产生状态变化；业务失败直接抛出，
  /// 链内出现 try 语句即未登记的异常翻译位置。判定按语法节点执行，注释与字符串里的同名写法不产生节点。
  /// </remarks>
  [Fact]
  public void Statistics_chain_keeps_zero_write_and_zero_exception_surface()
  {
    List<string> violations = [];
    int scannedTypeCount = 0;

    foreach (string relativePath in StatisticsChainSources)
    {
      CompilationUnitSyntax root = SourceSyntaxGuard.Read(relativePath.Split('/'));
      violations.AddRange(FindChainViolations(relativePath, root));
      scannedTypeCount += root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count();
    }

    Assert.Empty(violations);

    // 链清单无重复，且扫描规模有下界：清单被改坏成扫不到任何声明时先在这里失败。
    Assert.Equal(StatisticsChainSources.Length, StatisticsChainSources.Distinct(StringComparer.Ordinal).Count());
    Assert.True(
      scannedTypeCount >= 7,
      $"统计与导出链受核对的类型声明应不少于 7 个，实际 {scannedTypeCount} 个。");

    // 链不得引用写侧仓储接口：该接口携带全部写入能力，出现即说明查询链接触到了写入面。
    foreach (string relativePath in StatisticsChainSources)
      Assert.DoesNotContain("IMedicalRecognitionReportRepository", File.ReadAllText(ResolveFullPath(relativePath)), StringComparison.Ordinal);

    // 变异证据：注入事务、写入、事件、状态变化与 try 五类违规形态后，同一判定函数必须逐项命中且计数精确。
    IReadOnlyList<string> synthesized =
      FindChainViolations("probe/SynthesizedChainProbe.cs", SourceSyntaxGuard.Parse(ChainViolationProbeSource));
    Assert.Contains("probe/SynthesizedChainProbe.cs: Entry 声明了显式事务", synthesized);
    foreach (string memberName in WriteMapperMembers)
      Assert.Contains($"probe/SynthesizedChainProbe.cs: Entry 调用了写入型映射成员 {memberName}", synthesized);
    Assert.Contains("probe/SynthesizedChainProbe.cs: Entry 登记了领域事件 AddEvent", synthesized);
    Assert.Contains("probe/SynthesizedChainProbe.cs: Entry 调用了状态变更方法 MarkConsumed", synthesized);
    Assert.Contains("probe/SynthesizedChainProbe.cs: Entry 的 try 语句未登记为设计允许的异常翻译位置", synthesized);
    Assert.Equal(1 + WriteMapperMembers.Length + 1 + 1 + 1, synthesized.Count);

    // 判定的另一侧：只读调用与内存列表操作不报违规，证明判定锚定在写入与状态变化形态上。
    Assert.Empty(FindChainViolations("probe/NeutralChainProbe.cs", SourceSyntaxGuard.Parse(ChainNeutralProbeSource)));
  }

  /// <summary>
  /// V10 的语句面：十七条统计语句全部以 select 起始，不含任何写动词。
  /// </summary>
  /// <remarks>
  /// 语句由查询仓储的只读端口按显式 sqlId 调用，出现写动词即说明统计语句承担了数据变更；
  /// 语句的位置与结构判据（作用域、共用筛选、排序、分组键）由阶段 6 映射守卫另行冻结。
  /// </remarks>
  [Fact]
  public void Statistics_statements_declare_only_read_verbs()
  {
    XDocument document = XDocument.Load(QueryMapPath());

    foreach (string statementId in StatisticsStatementIds)
    {
      XElement statement = Assert.Single(StatementElements(document), element => (string?)element.Attribute("Id") == statementId);
      string text = NormalizeStatement(statement.Value);

      Assert.True(
        text.StartsWith("select ", StringComparison.OrdinalIgnoreCase),
        $"统计语句 {statementId} 应以 select 起始，实际：{text[..Math.Min(60, text.Length)]}");
      Assert.True(
        FindWriteVerbHits(text).Count == 0,
        $"统计语句 {statementId} 出现写动词：{string.Join(" | ", FindWriteVerbHits(text))}");
    }

    // 判别力：向语句副本注入写动词后同一条扫描必须命中；携带动词词根的列名不命中，证明整词匹配有确定边界。
    string probe = NormalizeStatement(StatementElements(document)[0].Value) + " ; delete from mrec_probe";
    Assert.Contains("写入动词 delete => delete", FindWriteVerbHits(probe));
    Assert.Empty(FindWriteVerbHits("select updated_time, deleted_flag from t"));
  }

  /// <summary>
  /// 客户端路径判据的违规形态样本：一条方法声明同时携带事务、写入、事件、状态变化与 try 五类违规。
  /// </summary>
  private const string ChainViolationProbeSource = """
    class Probe
    {
      [WorkUnit(UseTransaction = true)]
      public async System.Threading.Tasks.Task Entry()
      {
        mapper.InsertAsync(value);
        mapper.UpdateAsync(value);
        mapper.DeleteAsync(value);
        mapper.DeleteAllAsync(value);
        mapper.ExecuteAsync(script);
        mapper.BulkInsertAsync(rows);
        AddEvent(CreateEvent());
        entity.MarkConsumed();
        try { ReadOnce(); } catch (System.Exception) { throw; }
      }
    }
    """;

  /// <summary>
  /// 客户端路径判定的合规样本：只读调用与内存列表追加都不属于写入或状态变化。
  /// </summary>
  private const string ChainNeutralProbeSource = """
    class NeutralProbe
    {
      public async System.Threading.Tasks.Task Query()
      {
        await repository.CountRecognitionUsageReminderDetailsAsync(filter);
        await dataMapper.QueryAsync(statementId, parameters);
        AppendExportTailCells(exportType, model, cells);
      }
    }
    """;

  /// <summary>
  /// 在单份源码中查找统计与导出链的写入、事件、状态变化与异常处理违规。
  /// </summary>
  /// <param name="relativePath">用于报告的仓库内相对路径。</param>
  /// <param name="root">待检查的编译单元语法树根节点；可以是真实文件或合成源码。</param>
  /// <returns>违规描述集合；无违规时为空。</returns>
  private static IReadOnlyList<string> FindChainViolations(string relativePath, CompilationUnitSyntax root)
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
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 登记了领域事件 {memberName}");
        else if (StateChangeMemberPrefixes.Any(prefix => memberName.StartsWith(prefix, StringComparison.Ordinal)))
          violations.Add($"{relativePath}: {method.Identifier.ValueText} 调用了状态变更方法 {memberName}");
      }
    }

    // 异常处理位置登记面为空：链内出现 try 语句即未登记的异常翻译位置。
    foreach (TryStatementSyntax tryStatement in root.DescendantNodes().OfType<TryStatementSyntax>())
    {
      string scopeName =
        tryStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ??
        tryStatement.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ??
        "<类型或成员初始化器>";
      violations.Add($"{relativePath}: {scopeName} 的 try 语句未登记为设计允许的异常翻译位置");
    }

    return violations;
  }

  /// <summary>判断特性是否为启用事务的工作单元声明。</summary>
  /// <remarks>
  /// 特性里的 <c>UseTransaction = true</c> 是带名称的实参赋值，语法节点是 <c>NameEquals</c>；
  /// 方法调用的命名实参落在 <c>NameColon</c>，按 <c>NameColon</c> 判定会对真实声明恒假。
  /// </remarks>
  private static bool IsExplicitTransactionAttribute(AttributeSyntax attribute)
  {
    string name = attribute.Name.ToString().Split('.')[^1];
    if (name is not ("WorkUnit" or "WorkUnitAttribute")) return false;
    return attribute.ArgumentList?.Arguments.Any(argument =>
      argument.NameEquals?.Name.Identifier.ValueText == "UseTransaction" &&
      argument.Expression.ToString() == "true") == true;
  }

  /// <summary>找出文本命中的写动词标记，按标记声明顺序排列。</summary>
  /// <param name="text">去注释归一后的语句文本。</param>
  /// <returns>命中描述集合；无命中时为空。</returns>
  private static IReadOnlyList<string> FindWriteVerbHits(string text) =>
    [.. WriteVerbPatterns
      .Select(marker => (marker.Feature, Match: Regex.Match(text, marker.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
      .Where(hit => hit.Match.Success)
      .Select(hit => $"{hit.Feature} => {hit.Match.Value}")];

  /// <summary>查询映射文件声明的命名空间；元素与属性都需要按限定名查找。</summary>
  private static readonly XNamespace SqlMapNamespace = "http://dysoft.vip/schemas/EarthraceSqlMap.xsd";

  /// <summary>取映射文件里全部 Statement 节点，按声明顺序排列。</summary>
  /// <param name="document">已加载的映射文件。</param>
  /// <returns>按声明顺序排列的 Statement 节点。</returns>
  private static IReadOnlyList<XElement> StatementElements(XDocument document) =>
    document.Root is null
      ? throw new InvalidOperationException("映射文件缺少根节点。")
      : [.. document.Root.Elements(SqlMapNamespace + "Statements").Elements(SqlMapNamespace + "Statement")];

  /// <summary>去掉语句里的行内 XML 注释并把连续空白折成单个空格。</summary>
  /// <param name="statement">语句原文。</param>
  /// <returns>归一化后的语句文本。</returns>
  private static string NormalizeStatement(string statement) =>
    Regex.Replace(Regex.Replace(statement, "<!--.*?-->", " ", RegexOptions.Singleline), @"\s+", " ").Trim();

  /// <summary>在查询映射文件所在目录内定位查询侧映射。</summary>
  /// <returns>查询映射文件的绝对路径。</returns>
  private static string QueryMapPath() => SourceSyntaxGuard.FindRepositoryFile("Queries", "MedicalRecognitionReportQuery.xml");

  /// <summary>把仓库内相对路径解析为绝对路径。</summary>
  /// <param name="relativePath">仓库内相对路径，使用 <c>/</c> 分隔。</param>
  /// <returns>该路径的绝对路径。</returns>
  private static string ResolveFullPath(string relativePath) =>
    Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
}
