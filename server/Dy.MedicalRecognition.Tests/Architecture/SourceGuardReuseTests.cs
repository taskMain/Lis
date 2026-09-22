using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests.Architecture;

/// <summary>
/// 源码守卫自身的复用治理：通用语法工具只允许存在一份，消费方必须复用共享入口，
/// 且复用判定要能区分"真的调用共享入口"与"只写在注释里的伪调用"。
/// </summary>
/// <remarks>
/// 静态守卫是所有架构判据的地基。复制一份实现后，收口只会改到其中一份：改名副本、测试文件里同名的假替身、
/// 以及只写在注释或字符串里的伪调用都不会被编译器发现，判据会静默失效。
/// 本文件既检查测试工程的实际消费情况，也用合成源码验证复用判定本身的判别力。
/// </remarks>
public sealed class SourceGuardReuseTests
{
  /// <summary>共享语法工具的工程内相对路径；只有该文件可以直接调用 Roslyn 解析源码。</summary>
  private const string SharedGuardPath = "Architecture/SourceSyntaxGuard.cs";

  /// <summary>必须复用共享语法工具的测试文件清单（工程内相对路径，使用 <c>/</c> 分隔）。</summary>
  private static readonly string[] GovernedTestFiles =
  [
    "Architecture/SourceGuardReuseTests.cs",
    "Stage1ArchitectureTests.cs",
    "Stage2EnumContractTests.cs",
    "Stage2EnumMetadataQueryTests.cs",
    "Stage2WritePathTests.cs",
    // 阶段 3 的金额写入用例按语法节点判定写入路径不含累加运算，因此登记为共享语法工具的消费方。
    "Stage3WritePathTests.cs",
    // 阶段 3 的金额查询用例按语法节点判定两个入口先校验公共请求，因此同样登记为共享语法工具的消费方。
    "Stage3QueryTests.cs",
    // 阶段 4 的映射守卫按语法节点核对建表脚本与映射文件，登记为共享语法工具的消费方。
    "Stage4SqlMapTests.cs",
    // 阶段 4 的端点清单守卫按语法节点核对控制器路由与动作特性，登记为共享语法工具的消费方。
    "Stage4EndpointTests.cs",
    // 阶段 4 的实现约束守卫按语法节点核对数据访问调用点与禁止类型，登记为共享语法工具的消费方。
    "Stage4ConstraintTests.cs",
    // 阶段 5 的映射守卫按仓储目录内定位建表脚本与映射文件，登记为共享语法工具的消费方。
    "Stage5SqlMapTests.cs",
    // 阶段 5 的收尾守卫按语法节点核对四个入口的事务声明、禁止类型后缀与引用详情链的写入面，登记为共享语法工具的消费方。
    "Stage5ConstraintTests.cs",
    // 阶段 6 的统计契约守卫按语法节点核对契约分片、请求与读模型形状、枚举声明与分页形状，登记为共享语法工具的消费方。
    "Stage6StatisticsContractTests.cs",
    // 阶段 6 的统计索引守卫按仓储目录内定位迁移文件与建表脚本，登记为共享语法工具的消费方。
    "Stage6SqlMapTests.cs",
    // 阶段 6 的导出端点守卫按语法节点核对导出控制器的路由、动作绑定与响应组装，登记为共享语法工具的消费方。
    "Stage6EndpointTests.cs",
    // 阶段 6 的收口守卫按语法节点核对统计与导出链的零写入面与统计语句的只读动词，登记为共享语法工具的消费方。
    "Stage6ConstraintTests.cs"
  ];

  /// <summary>测试工程内不得重复声明的方法名：这两份私有实现历史上各复制过一次。</summary>
  private static readonly string[] ForbiddenLocalHelpers = ["FindRepositoryRoot", "StripComments"];

  /// <summary>
  /// 复用检测必须拒绝改名后的副本，并且不把只写在注释里的共享入口调用当成复用。
  /// </summary>
  [Fact]
  public void Reuse_detection_rejects_renamed_copy_and_comment_only_reference()
  {
    const string source = """
      internal static class RenamedSyntaxProbe
      {
        internal static void Probe(string source)
        {
          // SourceSyntaxGuard.Parse(source); 只写在注释里，不算复用共享入口。
          _ = CSharpSyntaxTree.ParseText(source);
        }
      }
      """;

    IReadOnlyList<string> violations = FindReuseViolations(source, "Stage2EnumContractTests.cs");

    Assert.Contains("not-reused:Stage2EnumContractTests.cs", violations);
    Assert.Contains(violations, violation => violation.StartsWith("duplicate-implementation:", StringComparison.Ordinal));
  }

  /// <summary>
  /// 复用检测必须拒绝测试文件里同名的假替身：它既不是共享实现，也不受共享实现的修复影响。
  /// </summary>
  [Fact]
  public void Reuse_detection_rejects_local_fake_guard()
  {
    const string source = """
      internal static class SourceSyntaxGuard
      {
        internal static object Parse(string source) => new();
      }

      internal sealed class ProbeTests
      {
        internal static void Probe(string source) => _ = SourceSyntaxGuard.Parse(source);
      }
      """;

    IReadOnlyList<string> violations = FindReuseViolations(source, "ProbeTests.cs");

    Assert.Contains("local-fake-guard:ProbeTests.cs", violations);
    // 假替身的调用形状与共享入口相同，因此"未复用"这一条不会命中；两类判据互补而不是重复。
    Assert.DoesNotContain(violations, violation => violation.StartsWith("not-reused:", StringComparison.Ordinal));
  }

  /// <summary>
  /// 共享语法工具的方法查找必须把声明数量当成判据：恰好一处才返回，
  /// 0 处（声明被删除）与 2 处（同名声明被复制或重载）都必须显式失败，不能静默只取某一处。
  /// </summary>
  [Fact]
  public void Shared_guard_method_lookup_requires_exactly_one_declaration()
  {
    const string oneDeclaration = """
      internal sealed class Probe
      {
        internal void Target() { }
      }
      """;
    const string noDeclaration = """
      internal sealed class Probe
      {
        internal void Other() { }
      }
      """;
    const string twoDeclarations = """
      internal sealed class Probe
      {
        internal void Target() { }
        internal void Target(int value) { }
      }
      """;

    Assert.Equal("Target", SourceSyntaxGuard.FindSingleMethod(SourceSyntaxGuard.Parse(oneDeclaration), "Target").Identifier.ValueText);
    Assert.Empty(SourceSyntaxGuard.FindMethods(SourceSyntaxGuard.Parse(noDeclaration), "Target"));
    Assert.Throws<InvalidOperationException>(() => SourceSyntaxGuard.FindSingleMethod(SourceSyntaxGuard.Parse(noDeclaration), "Target"));
    Assert.Equal(2, SourceSyntaxGuard.FindMethods(SourceSyntaxGuard.Parse(twoDeclarations), "Target").Count);
    Assert.Throws<InvalidOperationException>(() => SourceSyntaxGuard.FindSingleMethod(SourceSyntaxGuard.Parse(twoDeclarations), "Target"));

    // 接口里的声明形式（分号结束、没有方法体）同样计入匹配：契约冻结必须能判到这种声明，不能只认实现方法。
    const string declarationOnly = """
      internal interface Probe
      {
        void Target();
      }
      """;
    Assert.Null(SourceSyntaxGuard.FindSingleMethod(SourceSyntaxGuard.Parse(declarationOnly), "Target").Body);
  }

  /// <summary>
  /// 跨文件声明读取必须按类型简单名收集全部声明文件并排除生成物目录：
  /// 只读某一个固定文件时，类型被拆分到别的分片或搬到别的目录都判不出来。
  /// </summary>
  [Fact]
  public void Shared_guard_collects_every_declaration_part_of_a_type()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Application", "MedicalRecognitionApplicationModule");

    // 应用模块当前恰好一处声明：新增分片或删除声明都会在这里失败。
    Assert.Equal(
      ["server/Dy.MedicalRecognition.Application/MedicalRecognitionApplicationModule.cs"],
      declarations.Select(declaration => declaration.RelativePath).ToArray());
    // 判定按类型简单名而不是文件名：该目录下没有这个类型时必须收集为空，不能因为文件名无关而误命中。
    Assert.Empty(SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Tests/Architecture", "NotDeclaredAnywhereProbe"));
  }

  /// <summary>
  /// 测试工程内只能有一份源码语法工具：消费方清单必须与登记一致，
  /// 也不得再出现直接调用 Roslyn 的实现或重复声明的仓库根定位等本地助手。
  /// </summary>
  [Fact]
  public void Architecture_guards_reuse_the_shared_source_guard()
  {
    string testsRoot = Path.Combine(SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Tests");
    string[] files = [.. Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
      .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
      .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
      .Select(path => Path.GetRelativePath(testsRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
      .Order(StringComparer.Ordinal)];

    Assert.Contains(SharedGuardPath, files);

    List<string> violations = [];
    List<string> consumers = [];
    foreach (string relativePath in files)
    {
      string source = File.ReadAllText(Path.Combine(testsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
      violations.AddRange(FindReuseViolations(source, relativePath));
      if (relativePath != SharedGuardPath && ReferencesSharedGuard(SourceSyntaxGuard.Parse(source)))
        consumers.Add(relativePath);
    }

    // 受治理清单与真实消费方必须精确一致：新增消费方未登记、或登记了并不消费的文件都会失败。
    Assert.Equal(GovernedTestFiles.Order(StringComparer.Ordinal), consumers.Order(StringComparer.Ordinal));
    Assert.Empty(violations);
  }

  /// <summary>
  /// 查找单个源码文件中的守卫复用违规。
  /// </summary>
  /// <param name="source">待检查的源码。</param>
  /// <param name="relativePath">用于报告的工程内相对路径。</param>
  /// <returns>复用违规条目；无违规时为空。</returns>
  private static IReadOnlyList<string> FindReuseViolations(string source, string relativePath)
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Parse(source);
    List<string> violations = [];

    if (relativePath != SharedGuardPath &&
        root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
          .Any(declaration => declaration.Identifier.ValueText == "SourceSyntaxGuard"))
    {
      violations.Add($"local-fake-guard:{relativePath}");
    }

    if (relativePath != SharedGuardPath)
    {
      violations.AddRange(root.DescendantNodes().OfType<InvocationExpressionSyntax>()
        .Where(IsDirectRoslynCall)
        .Select(invocation => $"duplicate-implementation:{relativePath}:{invocation.Expression}"));
    }

    // 共享工具自己声明仓库根定位等通用助手；其余文件重复声明同样的助手会被这里拦下。
    if (relativePath != SharedGuardPath)
    {
      violations.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Where(method => ForbiddenLocalHelpers.Contains(method.Identifier.ValueText, StringComparer.Ordinal))
        .Select(method => $"local-helper:{relativePath}:{method.Identifier.ValueText}"));
    }

    if (GovernedTestFiles.Contains(relativePath, StringComparer.Ordinal) && !ReferencesSharedGuard(root))
      violations.Add($"not-reused:{relativePath}");

    return violations;
  }

  /// <summary>
  /// 判断源码是否直接调用 Roslyn 解析、编译或取语义模型。
  /// </summary>
  /// <param name="invocation">待判断的调用表达式。</param>
  /// <returns>直接调用 Roslyn 入口时为 <see langword="true"/>。</returns>
  private static bool IsDirectRoslynCall(InvocationExpressionSyntax invocation)
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return SourceSyntaxGuard.MemberName(memberAccess.Expression)
        is "CSharpSyntaxTree" or "CSharpCompilation" or "MetadataReference" ||
      SourceSyntaxGuard.MemberName(invocation.Expression) is "GetSemanticModel" or "GetCompilationUnitRoot";
  }

  /// <summary>
  /// 判断源码是否真实调用了共享语法工具（按类型名接收方判定，注释与字符串里的同名文本不算）。
  /// </summary>
  /// <param name="root">编译单元语法树根节点。</param>
  /// <returns>存在对共享语法工具的成员访问时为 <see langword="true"/>。</returns>
  private static bool ReferencesSharedGuard(CompilationUnitSyntax root) =>
    root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(access =>
      access.Expression is IdentifierNameSyntax identifier &&
      identifier.Identifier.ValueText == "SourceSyntaxGuard");
}
