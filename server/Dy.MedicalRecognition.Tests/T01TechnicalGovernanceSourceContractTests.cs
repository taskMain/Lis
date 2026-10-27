using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 本阶段后端静态治理的源码契约：组织路径构造的命名参数、查询侧与解析点两个私有编码规范化方法的空值归属，
/// 以及解析点编码收集只收非缺失值（本阶段后端矩阵 S01“三层字段映射”的静态面、S03“查询编码空值与去空白”、
/// S04“Resolver 编码边界”的编码收集面）。
/// </summary>
/// <remarks>
/// <para>
/// 判定一律走语法节点：注释与字符串里的同形写法不构成节点，因此不会造成假通过。
/// 按类型收集全部 partial 分片后再判定声明数量，避免把写在另一分片的同名方法漏在判定之外。
/// </para>
/// <para>
/// 组织路径的三处构造里，<c>FromScope</c> 使用目标类型推断构造（<c>new(...)</c>），另两处写全类型名；
/// 判定因此同时读取显式构造节点与 <c>FromScope</c> 的目标类型推断构造节点。
/// 其他文件中的目标类型推断构造无法在纯语法层归属到具体类型，因此这里只冻结本层能确定的两类构造位置，
/// 记录声明本身的九个参数由参数冻结断言保护。
/// </para>
/// <para>
/// 两个私有编码规范化方法的空值语义无法通过公共入口观察：证件类型与证件号码在规范化之前就被请求校验拒绝，
/// 缺失值因此进不到查询侧方法。这条差异由本文件的源码契约冻结，行为面证据由应用测试提供。
/// </para>
/// <para>
/// 记录参数冻结、两个编码规范化方法与编码收集这三项关键判定各有一条判定能力自检：
/// 自检把改坏的源码片段交给与生产用例同一个判定函数，要求判定报告不合格；
/// 合规范片段必须判为合格，因此判定恒真与判定恒假都会由这些自检暴露。
/// </para>
/// </remarks>
public sealed class T01TechnicalGovernanceSourceContractTests
{
  /// <summary>组织路径记录按声明顺序冻结的九个构造参数名。</summary>
  private static readonly string[] OrganizationPathParameterNames =
  [
    "OrganizationId",
    "OrganizationName",
    "OrganizationCode",
    "HospitalId",
    "HospitalName",
    "HospitalCode",
    "BranchId",
    "BranchName",
    "BranchCode"
  ];

  /// <summary>组织路径记录按声明顺序冻结的九个构造参数类型，与字段顺序同时固定。</summary>
  private static readonly string[] OrganizationPathParameterTypes =
  [
    "Guid",
    "string",
    "string",
    "Guid",
    "string",
    "string",
    "Guid",
    "string",
    "string"
  ];

  /// <summary>自检用的合规范记录片段：九个参数按冻结的名称、类型与顺序声明。</summary>
  private const string WellFormedSampleRecord = """
    internal sealed record SampleOrganizationPath(
      Guid OrganizationId,
      string OrganizationName,
      string OrganizationCode,
      Guid HospitalId,
      string HospitalName,
      string HospitalCode,
      Guid BranchId,
      string BranchName,
      string BranchCode);
    """;

  /// <summary>自检用的查询侧编码规范化片段：表达式体把缺失值经合并运算符收敛为空串。</summary>
  private const string WellFormedQuerySideNormalizeCode = """
    internal sealed class SampleQueryService
    {
      private static string NormalizeCode(string? value) => value?.Trim() ?? string.Empty;
    }
    """;

  /// <summary>自检用的解析点编码规范化片段：先取去空白后的编码，再把长度为零的编码交出为 <see langword="null"/>。</summary>
  private const string WellFormedResolverNormalizeCode = """
    internal sealed class SampleResolver
    {
      private static string? NormalizeCode(string? code)
      {
        string normalized = code?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
      }
    }
    """;

  /// <summary>收集链上必须先发生的规范化一步。</summary>
  private const string NormalizeStep = "Select(NormalizeCode)";

  /// <summary>收集链上只保留非空编码的一步。</summary>
  private const string PresentOnlyStep = "OfType<string>";

  /// <summary>收集链上按序比较去重的一步。</summary>
  private const string DeduplicateStep = "Distinct(StringComparer.Ordinal)";

  /// <summary>编码规范化方法对缺失值的取值口径。</summary>
  private enum NormalizeCodeMissingOutcome
  {
    /// <summary>缺失值收敛为空串。</summary>
    EmptyString,

    /// <summary>缺失值与纯空白收敛为 <see langword="null"/>。</summary>
    Null
  }

  /// <summary>
  /// S01 的静态面：组织路径的九个字段按冻结顺序与类型声明，三处构造都使用完整命名参数，且每个参数独立成行。
  /// </summary>
  /// <remarks>
  /// 三处构造分别是 <c>FromScope</c>、<c>ResolveNamesOrEmptyAsync</c> 与 <c>ValidateTarget</c>；
  /// 判定同时冻结构造位置数量（恰好三处）与每个实参的名称和顺序，因此把实参改成按位置传入、漏传一个参数，
  /// 或把两个实参写到同一行，都会由本用例失败暴露。
  /// </remarks>
  [Fact]
  public void Organization_path_creations_use_named_arguments_in_the_declared_order()
  {
    AssertRecordParametersAreFrozen();

    IReadOnlyList<(string Site, IReadOnlyList<ArgumentSyntax> Arguments)> sites = OrganizationPathConstructionSites();
    Assert.Equal(3, sites.Count);

    List<string> errors = [.. sites.SelectMany(site => ArgumentErrors(site.Arguments).Select(error => site.Site + "：" + error))];
    Assert.Empty(errors);
  }

  /// <summary>
  /// S01 静态面的判定能力自检：合规范写法必须判为合格，按位置传参、参数名与声明顺序不一致、同一行写两个参数都必须判为不合格。
  /// </summary>
  /// <remarks>
  /// 四种写法用源码片段构造，不进入生产文件：判定恒真的实现会在这里失败。
  /// </remarks>
  [Fact]
  public void Named_argument_guard_rejects_positional_reordered_and_single_line_arguments()
  {
    Assert.Empty(ArgumentErrors(SampleArguments(
      "OrganizationId: a,\n      OrganizationName: b,\n      OrganizationCode: c,\n      HospitalId: d,\n"
      + "      HospitalName: e,\n      HospitalCode: f,\n      BranchId: g,\n      BranchName: h,\n      BranchCode: i")));

    Assert.NotEmpty(ArgumentErrors(SampleArguments("a, b, c, d, e, f, g, h, i")));

    Assert.NotEmpty(ArgumentErrors(SampleArguments(
      "OrganizationName: b,\n      OrganizationId: a,\n      OrganizationCode: c,\n      HospitalId: d,\n"
      + "      HospitalName: e,\n      HospitalCode: f,\n      BranchId: g,\n      BranchName: h,\n      BranchCode: i")));

    Assert.NotEmpty(ArgumentErrors(SampleArguments(
      "OrganizationId: a, OrganizationName: b,\n      OrganizationCode: c,\n      HospitalId: d,\n"
      + "      HospitalName: e,\n      HospitalCode: f,\n      BranchId: g,\n      BranchName: h,\n      BranchCode: i")));

    Assert.NotEmpty(ArgumentErrors(SampleArguments(
      "OrganizationId: a,\n      OrganizationName: b,\n      OrganizationCode: c,\n      HospitalId: d,\n"
      + "      HospitalName: e,\n      HospitalCode: f,\n      BranchId: g,\n      BranchName: h")));
  }

  /// <summary>
  /// 记录参数冻结的判定能力自检：合规范写法必须判为合格，参数改名、顺序调换、类型改动与少传一个参数都必须判为不合格。
  /// </summary>
  /// <remarks>五段源码片段不进入生产文件：判定恒真的实现会在这里失败。</remarks>
  [Fact]
  public void Record_parameter_guard_rejects_renamed_reordered_retyped_and_resized_parameters()
  {
    Assert.Empty(RecordParameterErrors(SampleRecordParameters(WellFormedSampleRecord)));

    Assert.NotEmpty(RecordParameterErrors(SampleRecordParameters(WellFormedSampleRecord
      .Replace("string OrganizationName,", "string OrganizationLabel,", StringComparison.Ordinal))));

    string reordered = WellFormedSampleRecord
      .Replace("Guid HospitalId,", "Guid PlaceholderId,", StringComparison.Ordinal)
      .Replace("Guid BranchId,", "Guid HospitalId,", StringComparison.Ordinal)
      .Replace("Guid PlaceholderId,", "Guid BranchId,", StringComparison.Ordinal);
    Assert.NotEmpty(RecordParameterErrors(SampleRecordParameters(reordered)));

    Assert.NotEmpty(RecordParameterErrors(SampleRecordParameters(WellFormedSampleRecord
      .Replace("string OrganizationCode,", "int OrganizationCode,", StringComparison.Ordinal))));

    Assert.NotEmpty(RecordParameterErrors(SampleRecordParameters(WellFormedSampleRecord
      .Replace("string BranchName,", string.Empty, StringComparison.Ordinal))));
  }

  /// <summary>
  /// S03 的静态证据：查询侧私有编码规范化方法返回非空字符串，缺失值落成空串。
  /// </summary>
  /// <remarks>
  /// 判定冻结返回类型为不可空 <c>string</c>，且表达式体把空值经空合并收敛为 <c>string.Empty</c>；
  /// 若改成返回 <see langword="null"/> 或改成可空返回类型，本用例失败。
  /// </remarks>
  [Fact]
  public void Query_side_normalize_code_returns_empty_string_for_a_missing_code()
  {
    MethodDeclarationSyntax normalizeCode = SingleMethod("MedicalRecognitionReportQueryAppService", "NormalizeCode");

    Assert.Empty(NormalizeCodeErrors(normalizeCode, NormalizeCodeMissingOutcome.EmptyString));
  }

  /// <summary>
  /// S04 的静态证据：解析点私有编码规范化方法返回可空字符串，缺失、空串与纯空白一律落成 <see langword="null"/>。
  /// </summary>
  /// <remarks>
  /// 判定冻结三件事：返回类型可空；先按去空白取值、空值收敛为空串；最后把长度为零的编码返回为 <see langword="null"/>。
  /// 其中最后一条是解析点与查询侧语义不同的静态依据。
  /// </remarks>
  [Fact]
  public void Resolver_normalize_code_returns_null_for_a_missing_or_blank_code()
  {
    MethodDeclarationSyntax normalizeCode = SingleMethod("OrganizationPathResolver", "NormalizeCode");

    Assert.Empty(NormalizeCodeErrors(normalizeCode, NormalizeCodeMissingOutcome.Null));
  }

  /// <summary>
  /// 两个编码规范化方法的空值判定自检：合规范写法必须判为合格；可空返回类型被改动、
  /// 缺失值不再收敛为空串、以及解析点不再把长度为零的编码交给 <see langword="null"/> 都必须判为不合格。
  /// </summary>
  /// <remarks>
  /// 判定与两个生产用例是同一个函数，只按语法节点读取形态：局部变量改名属于等价重构，必须继续判为合格。
  /// </remarks>
  [Fact]
  public void Normalize_code_guards_reject_return_type_and_missing_value_drift()
  {
    Assert.Empty(NormalizeCodeErrors(SampleNormalizeCode(WellFormedQuerySideNormalizeCode), NormalizeCodeMissingOutcome.EmptyString));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedQuerySideNormalizeCode.Replace(
        "private static string NormalizeCode",
        "private static string? NormalizeCode",
        StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.EmptyString));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedQuerySideNormalizeCode.Replace(" ?? string.Empty", string.Empty, StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.EmptyString));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedQuerySideNormalizeCode.Replace("value?.Trim()", "value.Trim()", StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.EmptyString));

    Assert.Empty(NormalizeCodeErrors(SampleNormalizeCode(WellFormedResolverNormalizeCode), NormalizeCodeMissingOutcome.Null));
    Assert.Empty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedResolverNormalizeCode.Replace("normalized", "trimmedCode", StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.Null));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedResolverNormalizeCode.Replace(
        "private static string? NormalizeCode",
        "private static string NormalizeCode",
        StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.Null));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedResolverNormalizeCode.Replace("? null : normalized", "? normalized : normalized", StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.Null));
    Assert.NotEmpty(NormalizeCodeErrors(
      SampleNormalizeCode(WellFormedResolverNormalizeCode.Replace(
        "normalized.Length == 0 ? null : normalized",
        "normalized",
        StringComparison.Ordinal)),
      NormalizeCodeMissingOutcome.Null));
  }

  /// <summary>
  /// S04 的静态证据：解析点的编码收集先按同一规范化口径处理，再只保留非空编码并去重。
  /// </summary>
  /// <remarks>
  /// 缺失、空串与纯空白经规范化后的取值是 <see langword="null"/>，再由非空类型筛选排除，
  /// 因此缺失值不会进入本批需要读取的编码集合。判定同时对缺少筛选或缺少规范化的写法报告不合格。
  /// </remarks>
  [Fact]
  public void Resolver_collects_only_the_codes_that_are_present()
  {
    ExpressionSyntax collectExpression = SingleMethod("OrganizationPathResolver", "CollectCodes").ExpressionBody!.Expression;

    Assert.Empty(CollectCodeErrors(collectExpression));
    Assert.NotEmpty(CollectCodeErrors(WithFragmentRemoved(collectExpression, ".OfType<string>()")));
    Assert.NotEmpty(CollectCodeErrors(WithFragmentRemoved(collectExpression, ".Select(NormalizeCode)")));
  }

  /// <summary>
  /// 编码收集判定自检：合规范写法必须判为合格；缺少规范化、缺少非空筛选、缺少去重、
  /// 去重比较口径被替换，以及规范化与筛选次序颠倒都必须判为不合格。
  /// </summary>
  /// <remarks>判定与生产用例是同一个语法节点判定函数，断言的是节点链本身。</remarks>
  [Fact]
  public void Code_collection_guard_rejects_missing_normalization_filter_deduplication_and_order_swap()
  {
    const string wellFormed =
      "[.. targets.SelectMany(target => new[] { target.OrganizationCode }).Select(NormalizeCode).OfType<string>().Distinct(StringComparer.Ordinal)]";

    Assert.Empty(CollectCodeErrors(ParseExpression(wellFormed)));
    Assert.NotEmpty(CollectCodeErrors(ParseExpression(
      wellFormed.Replace(".Select(NormalizeCode)", string.Empty, StringComparison.Ordinal))));
    Assert.NotEmpty(CollectCodeErrors(ParseExpression(
      wellFormed.Replace(".OfType<string>()", string.Empty, StringComparison.Ordinal))));
    Assert.NotEmpty(CollectCodeErrors(ParseExpression(
      wellFormed.Replace(".Distinct(StringComparer.Ordinal)", string.Empty, StringComparison.Ordinal))));
    Assert.NotEmpty(CollectCodeErrors(ParseExpression(
      wellFormed.Replace("StringComparer.Ordinal", "StringComparer.OrdinalIgnoreCase", StringComparison.Ordinal))));
    Assert.NotEmpty(CollectCodeErrors(ParseExpression(wellFormed.Replace(
      ".Select(NormalizeCode).OfType<string>()",
      ".OfType<string>().Select(NormalizeCode)",
      StringComparison.Ordinal))));
  }

  /// <summary>
  /// 判定一次构造的实参是否满足命名参数契约，返回全部不合格说明。
  /// </summary>
  /// <param name="arguments">该构造的全部实参。</param>
  /// <returns>不合格说明；全部合格时为空集合。</returns>
  private static IReadOnlyList<string> ArgumentErrors(IReadOnlyList<ArgumentSyntax> arguments)
  {
    List<string> errors = [];
    if (arguments.Count != OrganizationPathParameterNames.Length)
    {
      errors.Add("实参数量应为 " + OrganizationPathParameterNames.Length + "，实际 " + arguments.Count);
      return errors;
    }

    for (int index = 0; index < OrganizationPathParameterNames.Length; index++)
    {
      ArgumentSyntax argument = arguments[index];
      if (argument.NameColon is null)
      {
        errors.Add("第 " + (index + 1) + " 个实参未使用命名参数");
        continue;
      }

      if (argument.NameColon.Name.Identifier.ValueText != OrganizationPathParameterNames[index])
      {
        errors.Add("第 " + (index + 1) + " 个实参名应为 " + OrganizationPathParameterNames[index]);
      }
    }

    IReadOnlyList<int> lines = [.. arguments.Select(StartLine)];
    if (lines.Distinct().Count() != lines.Count) errors.Add("存在写在同一行的两个实参");
    return errors;
  }

  /// <summary>
  /// 收集组织路径的三处构造及其实参。
  /// </summary>
  /// <remarks>
  /// <c>FromScope</c> 用目标类型推断构造，节点类型与显式构造不同，因此先按方法返回类型确认它构造的就是组织路径；
  /// 解析点的两处以 <c>new OrganizationPath(</c> 显式写法出现。
  /// </remarks>
  /// <returns>构造位置说明与该处的实参集合。</returns>
  private static IReadOnlyList<(string Site, IReadOnlyList<ArgumentSyntax> Arguments)> OrganizationPathConstructionSites()
  {
    CompilationUnitSyntax pathFile = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition.Application", "Validation", "OrganizationPath.cs");
    CompilationUnitSyntax resolverFile = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition.Application", "Validation", "OrganizationPathResolver.cs");

    MethodDeclarationSyntax fromScope = SourceSyntaxGuard.FindSingleMethod(pathFile, "FromScope");
    Assert.Equal("OrganizationPath", fromScope.ReturnType.ToString());
    ImplicitObjectCreationExpressionSyntax implicitCreation =
      Assert.IsType<ImplicitObjectCreationExpressionSyntax>(fromScope.ExpressionBody!.Expression);

    ObjectCreationExpressionSyntax[] explicitCreations =
    [
      .. resolverFile.DescendantNodes()
        .OfType<ObjectCreationExpressionSyntax>()
        .Where(creation => SourceSyntaxGuard.SimpleTypeName(creation.Type) == "OrganizationPath")
    ];
    Assert.All(explicitCreations, creation => Assert.NotNull(creation.ArgumentList));

    return
    [
      ("OrganizationPath.cs.FromScope", implicitCreation.ArgumentList.Arguments),
      .. explicitCreations.Select(creation =>
        ("OrganizationPathResolver.cs", (IReadOnlyList<ArgumentSyntax>)creation.ArgumentList!.Arguments))
    ];
  }

  /// <summary>把一段实参源码片段解析为实参集合，供判定能力自检使用。</summary>
  /// <param name="argumentSource">实参文本，参数之间用逗号分隔。</param>
  /// <returns>解析得到的实参集合。</returns>
  private static IReadOnlyList<ArgumentSyntax> SampleArguments(string argumentSource)
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Parse(
      "static class Sample { static object M() => new Sample(" + argumentSource + "); }");
    return [.. root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single().ArgumentList!.Arguments];
  }

  /// <summary>取节点的起始行号（从 0 开始）。</summary>
  /// <param name="node">待取行号的节点。</param>
  /// <returns>起始行号。</returns>
  private static int StartLine(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line;

  /// <summary>
  /// 冻结组织路径记录的九个构造参数：声明恰好一处，名称、类型与顺序与冻结值逐项相同。
  /// </summary>
  private static void AssertRecordParametersAreFrozen()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Application", "OrganizationPath");
    RecordDeclarationSyntax record = Assert.Single(
      declarations.SelectMany(declaration => declaration.Root.DescendantNodes().OfType<RecordDeclarationSyntax>()),
      candidate => candidate.Identifier.ValueText == "OrganizationPath");
    Assert.Empty(RecordParameterErrors(record.ParameterList!));
  }

  /// <summary>
  /// 在指定类型声明的全部 partial 分片内取恰好一处同名方法。
  /// </summary>
  /// <param name="typeName">类型简单名。</param>
  /// <param name="methodName">方法名。</param>
  /// <returns>唯一的方法声明。</returns>
  /// <exception cref="InvalidOperationException">同名方法不是恰好一处时抛出。</exception>
  private static MethodDeclarationSyntax SingleMethod(string typeName, string methodName)
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Application", typeName);
    Assert.NotEmpty(declarations);

    MethodDeclarationSyntax[] methods =
    [
      .. declarations.SelectMany(declaration => SourceSyntaxGuard.FindMethods(declaration.Root, methodName))
    ];
    return methods.Length == 1
      ? methods[0]
      : throw new InvalidOperationException(typeName + "." + methodName + " 的声明应恰好 1 处，实际 " + methods.Length + " 处。");
  }

  /// <summary>
  /// 按声明顺序判定记录的构造参数名称与类型，返回全部不合格说明。
  /// </summary>
  /// <param name="parameters">记录的参数清单。</param>
  /// <returns>不合格说明；参数数量、名称、类型与顺序全部符合时为空的集合。</returns>
  private static IReadOnlyList<string> RecordParameterErrors(ParameterListSyntax parameters)
  {
    List<string> errors = [];
    ParameterSyntax[] items = [.. parameters.Parameters];
    if (items.Length != OrganizationPathParameterNames.Length)
    {
      errors.Add("参数数量应为 " + OrganizationPathParameterNames.Length + "，实际 " + items.Length);
    }

    int compared = Math.Min(items.Length, OrganizationPathParameterNames.Length);
    for (int index = 0; index < compared; index++)
    {
      if (items[index].Identifier.ValueText != OrganizationPathParameterNames[index])
      {
        errors.Add("第 " + (index + 1) + " 个参数名应为 " + OrganizationPathParameterNames[index]
          + "，实际 " + items[index].Identifier.ValueText);
      }

      string actualType = items[index].Type?.ToString() ?? "（缺少类型）";
      if (actualType != OrganizationPathParameterTypes[index])
      {
        errors.Add("第 " + (index + 1) + " 个参数类型应为 " + OrganizationPathParameterTypes[index] + "，实际 " + actualType);
      }
    }

    return errors;
  }

  /// <summary>
  /// 按语法节点判定编码规范化方法的空值口径，返回全部不合格说明。
  /// </summary>
  /// <remarks>
  /// 判定只读取节点形态：可空返回类型、合并运算符两侧的取值方式、长度判断与交出的结果。
  /// 局部变量名取自同一方法内的声明节点，改名不影响判定。
  /// </remarks>
  /// <param name="method">待判定的方法声明。</param>
  /// <param name="missingOutcome">该方法对缺失编码应有的结果。</param>
  /// <returns>不合格说明；全部符合时为空的集合。</returns>
  private static IReadOnlyList<string> NormalizeCodeErrors(MethodDeclarationSyntax method, NormalizeCodeMissingOutcome missingOutcome)
  {
    List<string> errors = [];
    if (missingOutcome == NormalizeCodeMissingOutcome.Null)
    {
      if (method.ReturnType is not NullableTypeSyntax nullableReturn || !IsStringType(nullableReturn.ElementType))
      {
        errors.Add("返回类型应为可空 string，实际 " + method.ReturnType);
      }

      AppendNullableMissingCodeErrors(method, errors);
      return errors;
    }

    if (method.ReturnType is NullableTypeSyntax || !IsStringType(method.ReturnType))
    {
      errors.Add("返回类型应为不可空 string，实际 " + method.ReturnType);
    }

    AppendEmptyStringMissingCodeErrors(method, errors);
    return errors;
  }

  /// <summary>
  /// 判定缺失值收敛为空串的写法：表达式体把按可空取到的编码经合并运算符交给 <c>string.Empty</c>。
  /// </summary>
  /// <param name="method">待判定的方法声明。</param>
  /// <param name="errors">不合格说明的收集位置。</param>
  private static void AppendEmptyStringMissingCodeErrors(MethodDeclarationSyntax method, List<string> errors)
  {
    if (method.ExpressionBody is null)
    {
      errors.Add("空串口径应写成表达式体");
      return;
    }

    if (method.ExpressionBody.Expression is not BinaryExpressionSyntax coalesce || !coalesce.IsKind(SyntaxKind.CoalesceExpression))
    {
      errors.Add("表达式体应为合并运算符，把缺失值收敛为空串");
      return;
    }

    if (coalesce.Left is not ConditionalAccessExpressionSyntax)
    {
      errors.Add("合并运算符左侧应按可空取值（?.），否则缺失值会直接进入后续处理");
    }

    if (!IsStringEmptyMemberAccess(coalesce.Right, out string rightForm))
    {
      errors.Add("合并运算符右侧应为 string.Empty，实际 " + rightForm);
    }
  }

  /// <summary>
  /// 判定缺失值与纯空白收敛为 <see langword="null"/> 的写法：先按可空取值并合并为空串，
  /// 再按长度为零交出 <see langword="null"/>，否则交出该局部变量。
  /// </summary>
  /// <param name="method">待判定的方法声明。</param>
  /// <param name="errors">不合格说明的收集位置。</param>
  private static void AppendNullableMissingCodeErrors(MethodDeclarationSyntax method, List<string> errors)
  {
    if (method.ExpressionBody is not null || method.Body is not BlockSyntax body)
    {
      errors.Add("null 口径应写成语句体：先取去空白后的编码，再按长度判断是否交出 null");
      return;
    }

    if (body.Statements.Count != 2
      || body.Statements[0] is not LocalDeclarationStatementSyntax declaration
      || declaration.Declaration.Variables.Count != 1
      || body.Statements[1] is not ReturnStatementSyntax returnStatement)
    {
      errors.Add("语句体应恰好两条语句：一条局部声明保存去空白后的编码，一条按长度判断的返回");
      return;
    }

    VariableDeclaratorSyntax variable = declaration.Declaration.Variables[0];
    string variableName = variable.Identifier.ValueText;
    if (!IsStringType(declaration.Declaration.Type))
    {
      errors.Add("局部声明的类型应为不可空 string，实际 " + declaration.Declaration.Type);
    }

    if (variable.Initializer?.Value is not BinaryExpressionSyntax coalesce || !coalesce.IsKind(SyntaxKind.CoalesceExpression))
    {
      errors.Add("局部声明的初始化表达式应为合并运算符");
    }
    else
    {
      if (coalesce.Left is not ConditionalAccessExpressionSyntax)
      {
        errors.Add("合并运算符左侧应按可空取值（?.）");
      }

      if (!IsStringEmptyMemberAccess(coalesce.Right, out string rightForm))
      {
        errors.Add("合并运算符右侧应为 string.Empty，实际 " + rightForm);
      }
    }

    if (returnStatement.Expression is not ConditionalExpressionSyntax conditional)
    {
      errors.Add("返回语句应按长度判断交出 null 或去空白后的编码");
      return;
    }

    if (conditional.Condition is not BinaryExpressionSyntax lengthComparison
      || !lengthComparison.IsKind(SyntaxKind.EqualsExpression)
      || lengthComparison.Left is not MemberAccessExpressionSyntax lengthAccess
      || SourceSyntaxGuard.MemberName(lengthAccess) != "Length"
      || lengthAccess.Expression is not IdentifierNameSyntax lengthOwner
      || lengthOwner.Identifier.ValueText != variableName
      || lengthComparison.Right is not LiteralExpressionSyntax zero
      || !zero.IsKind(SyntaxKind.NumericLiteralExpression)
      || zero.Token.ValueText != "0")
    {
      errors.Add("条件应为「去空白后的编码长度等于 0」的长度判断，实际 " + conditional.Condition);
    }

    if (conditional.WhenTrue is not LiteralExpressionSyntax missing || !missing.IsKind(SyntaxKind.NullLiteralExpression))
    {
      errors.Add("长度为零时应交出 null，实际 " + conditional.WhenTrue);
    }

    if (conditional.WhenFalse is not IdentifierNameSyntax returned || returned.Identifier.ValueText != variableName)
    {
      errors.Add("长度不为零时应交出局部声明的编码，实际 " + conditional.WhenFalse);
    }
  }

  /// <summary>
  /// 按语法节点判定编码收集写法：先规范化、再只保留非空编码、最后去重，三步缺一不可且次序固定。
  /// </summary>
  /// <param name="collectExpression">编码收集方法的表达式体节点。</param>
  /// <returns>不合格说明；三步齐备且次序正确时为空的集合。</returns>
  private static IReadOnlyList<string> CollectCodeErrors(ExpressionSyntax collectExpression)
  {
    List<string> errors = [];
    if (collectExpression is not CollectionExpressionSyntax collection)
    {
      errors.Add("编码收集应写成集合表达式，实际 " + collectExpression.Kind());
      return errors;
    }

    if (collection.Elements.Count != 1 || collection.Elements[0] is not SpreadElementSyntax spread)
    {
      errors.Add("集合表达式应只展开一条编码来源链");
      return errors;
    }

    List<string> steps = CollectCodeSteps(spread.Expression);
    int normalize = steps.IndexOf(NormalizeStep);
    int presentOnly = steps.IndexOf(PresentOnlyStep);
    int deduplicate = steps.IndexOf(DeduplicateStep);

    if (normalize < 0) errors.Add("收集链缺少按 NormalizeCode 规范化的一步");
    if (presentOnly < 0) errors.Add("收集链缺少只保留非空编码的一步：" + PresentOnlyStep);
    if (deduplicate < 0) errors.Add("收集链缺少按 " + DeduplicateStep + " 去重的一步");
    if (normalize >= 0 && presentOnly >= 0 && presentOnly > normalize)
    {
      errors.Add("应先按 NormalizeCode 规范化，再按非空筛选");
    }

    if (presentOnly >= 0 && deduplicate >= 0 && deduplicate > presentOnly)
    {
      errors.Add("去重应发生在非空筛选之后");
    }

    return errors;
  }

  /// <summary>
  /// 按从外到内的顺序列出收集链上的调用形态。
  /// </summary>
  /// <param name="expression">最外层的调用表达式。</param>
  /// <returns>每一层调用的形态文本。</returns>
  private static List<string> CollectCodeSteps(ExpressionSyntax expression)
  {
    List<string> steps = [];
    ExpressionSyntax current = expression;
    while (current is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax access)
    {
      steps.Add(StepForm(invocation));
      current = access.Expression;
    }

    return steps;
  }

  /// <summary>
  /// 把一个调用写成「成员名＋类型参数＋实参」的形态文本，例如 <c>OfType&lt;string&gt;</c>。
  /// </summary>
  /// <param name="invocation">调用表达式。</param>
  /// <returns>该调用的形态文本。</returns>
  private static string StepForm(InvocationExpressionSyntax invocation)
  {
    IReadOnlyList<string> typeArguments = SourceSyntaxGuard.TypeArguments(invocation);
    string typeArgumentText = typeArguments.Count == 0 ? string.Empty : "<" + string.Join(", ", typeArguments) + ">";
    string argumentText = invocation.ArgumentList.Arguments.Count == 0
      ? string.Empty
      : "(" + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => argument.ToString())) + ")";
    return (SourceSyntaxGuard.MemberName(invocation) ?? invocation.ToString()) + typeArgumentText + argumentText;
  }

  /// <summary>
  /// 判断表达式是否为 <c>string.Empty</c> 成员访问。
  /// </summary>
  /// <param name="expression">待判断的表达式。</param>
  /// <param name="actualForm">该表达式的实际文本，供不合格说明使用。</param>
  /// <returns>是 <c>string.Empty</c> 时为 <see langword="true"/>。</returns>
  private static bool IsStringEmptyMemberAccess(ExpressionSyntax expression, out string actualForm)
  {
    actualForm = expression.ToString();
    return expression is MemberAccessExpressionSyntax access
      && access.Expression is PredefinedTypeSyntax owner
      && owner.Keyword.IsKind(SyntaxKind.StringKeyword)
      && SourceSyntaxGuard.MemberName(access) == "Empty";
  }

  /// <summary>
  /// 判断类型语法是否为 <c>string</c> 关键字的预定义类型。
  /// </summary>
  /// <param name="type">类型语法节点。</param>
  /// <returns>是 <c>string</c> 预定义类型时为 <see langword="true"/>。</returns>
  private static bool IsStringType(TypeSyntax type) =>
    type is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.StringKeyword);

  /// <summary>
  /// 把记录片段解析为参数清单，供判定能力自检使用。
  /// </summary>
  /// <param name="recordSource">记录声明的源码片段。</param>
  /// <returns>该记录的参数清单。</returns>
  private static ParameterListSyntax SampleRecordParameters(string recordSource)
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Parse(recordSource);
    RecordDeclarationSyntax record = Assert.Single(root.DescendantNodes().OfType<RecordDeclarationSyntax>());
    return record.ParameterList ?? throw new InvalidOperationException("自检片段的记录声明缺少参数清单。");
  }

  /// <summary>
  /// 把方法片段解析为编码规范化方法声明，供判定能力自检使用。
  /// </summary>
  /// <param name="methodSource">方法声明的源码片段。</param>
  /// <returns>该片段内的唯一 NormalizeCode 方法声明。</returns>
  private static MethodDeclarationSyntax SampleNormalizeCode(string methodSource) =>
    Assert.Single(SourceSyntaxGuard.FindMethods(SourceSyntaxGuard.Parse(methodSource), "NormalizeCode"));

  /// <summary>
  /// 把表达式片段解析为表达式节点，供判定能力自检使用。
  /// </summary>
  /// <param name="expressionSource">表达式源码片段。</param>
  /// <returns>解析得到的表达式节点。</returns>
  private static ExpressionSyntax ParseExpression(string expressionSource)
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Parse("static class Sample { static object Probe() => " + expressionSource + "; }");
    return Assert.Single(SourceSyntaxGuard.FindMethods(root, "Probe")).ExpressionBody!.Expression;
  }

  /// <summary>
  /// 删掉表达式里的某段写法后重新解析，供改坏自检使用。
  /// </summary>
  /// <param name="expression">原始表达式节点。</param>
  /// <param name="fragment">要删除的写法文本。</param>
  /// <returns>删除后重新解析的表达式节点。</returns>
  private static ExpressionSyntax WithFragmentRemoved(SyntaxNode expression, string fragment) =>
    ParseExpression(expression.ToString().Replace(fragment, string.Empty, StringComparison.Ordinal));
}
