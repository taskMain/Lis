using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dy.MedicalRecognition.Tests.Architecture;

/// <summary>
/// 测试用的源码结构分析共享入口：把 C# 源码解析为语法树，供各静态守卫按语法节点判定。
/// </summary>
/// <remarks>
/// 判定一律走语法节点而不是文本或正则，因此三类误判在结构上不成立：
/// 注释里的写法不是节点（"把注册语句注释掉""只在注释里解释不使用反射"都不会被当成代码）；
/// 字符串字面量不是节点（文案里恰好含有违规写法不会假绿）；
/// 限定名与类型参数由节点给出（<c>System.String</c>、<c>System.Collections.Generic.Dictionary</c>、
/// <c>Type?</c> 等写法不依赖正则的大小写与前后缀假设）。
/// 本类只提供通用语法工具，不做语义绑定：被守卫的写法都落在语法形状上，无需符号解析。
/// 已知边界只剩预处理器——<c>#if false</c> 或被关闭的条件分支会成为禁用文本 trivia 而不产生节点，
/// 该分支里的违规写法对节点扫描不可见；超出语法形状的判定（如目标类型 <c>new()</c> 的具体类型）
/// 由调用方在各自的 remarks 里说明。
/// </remarks>
internal static class SourceSyntaxGuard
{
  /// <summary>
  /// 解析 C# 源码为编译单元语法树。
  /// </summary>
  /// <remarks>源码片段即使不能通过编译也应尽量解析：本方法只取语法节点，不编译、不抛出。</remarks>
  /// <param name="source">待解析的 C# 源码或源码片段。</param>
  /// <returns>编译单元语法树根节点。</returns>
  internal static CompilationUnitSyntax Parse(string source) =>
    CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

  /// <summary>
  /// 读取仓库内源码文件并解析为编译单元语法树。
  /// </summary>
  /// <param name="relativePath">相对仓库根目录的源码路径片段，例如 <c>server</c>、<c>文件名.cs</c>。</param>
  /// <returns>该文件的编译单元语法树根节点。</returns>
  /// <exception cref="FileNotFoundException">仓库内不存在该文件时由 <see cref="File.ReadAllText(string)"/> 抛出。</exception>
  internal static CompilationUnitSyntax Read(params string[] relativePath) =>
    Parse(File.ReadAllText(Path.Combine(relativePath.Prepend(FindRepositoryRoot()).ToArray())));

  /// <summary>
  /// 在指定目录下收集声明了某个类型的全部源码文件，供跨文件 partial 声明一起判定。
  /// </summary>
  /// <remarks>
  /// 只读取某一个固定文件时，类型被拆到别的分片后判据会对"写在另一分片"的内容视而不见：
  /// 既可能把真实存在的声明判成缺失，也可能因为文件里出现同名文本而假绿。
  /// 本方法按类型简单名收集全部声明文件（排除 <c>bin</c>、<c>obj</c>），调用方据此冻结声明数量与内容。
  /// </remarks>
  /// <param name="relativeDirectory">相对仓库根目录的目录路径片段，例如 <c>server</c>、<c>工程目录</c>。</param>
  /// <param name="typeName">类型简单名（精确匹配）。</param>
  /// <returns>声明该类型的源码文件仓库内相对路径（使用 <c>/</c>）与其语法树根节点，按路径排序；无声明时为空集合。</returns>
  internal static IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> ReadTypeDeclarations(string relativeDirectory, string typeName)
  {
    string repositoryRoot = FindRepositoryRoot();
    string directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
    List<(string RelativePath, CompilationUnitSyntax Root)> declarations = [];
    foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
    {
      if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
      {
        continue;
      }

      CompilationUnitSyntax root = Parse(File.ReadAllText(file));
      if (!root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Any(declaration => declaration.Identifier.ValueText == typeName))
      {
        continue;
      }

      declarations.Add((Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'), root));
    }

    return [.. declarations.OrderBy(declaration => declaration.RelativePath, StringComparer.Ordinal)];
  }

  /// <summary>
  /// 按名称查找方法声明，返回全部匹配。
  /// </summary>
  /// <remarks>
  /// 带方法体的实现方法、接口与抽象类的声明形式（以分号结束、没有方法体）都计入匹配；
  /// 需要区分声明形式时由调用方按 <see cref="MethodDeclarationSyntax.Body"/> 自行判定。
  /// </remarks>
  /// <param name="root">编译单元语法树根节点。</param>
  /// <param name="methodName">方法名（简单名，精确匹配）。</param>
  /// <returns>全部匹配的方法声明；不存在时为空集合。</returns>
  internal static IReadOnlyList<MethodDeclarationSyntax> FindMethods(CompilationUnitSyntax root, string methodName) =>
    [.. root.DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .Where(method => method.Identifier.ValueText == methodName)];

  /// <summary>
  /// 按名称查找方法声明，并要求恰好只有一处声明。
  /// </summary>
  /// <remarks>
  /// 只按名称取第一处匹配时，声明被删除（0 处）会取到默认值而让断言恒真，
  /// 同名声明被复制或重载（多处）时又只判定其中一处而放过其余声明；
  /// 因此"恰好一处"必须由调用方显式冻结，本方法把 0 处与多处都作为失败暴露。
  /// </remarks>
  /// <param name="root">编译单元语法树根节点。</param>
  /// <param name="methodName">方法名（简单名，精确匹配）。</param>
  /// <returns>唯一匹配的方法声明。</returns>
  /// <exception cref="InvalidOperationException">同名声明不是恰好一处时抛出，异常信息包含实际数量。</exception>
  internal static MethodDeclarationSyntax FindSingleMethod(CompilationUnitSyntax root, string methodName)
  {
    IReadOnlyList<MethodDeclarationSyntax> declarations = FindMethods(root, methodName);
    return declarations.Count == 1
      ? declarations[0]
      : throw new InvalidOperationException($"方法 {methodName} 的声明应恰好 1 处，实际 {declarations.Count} 处。");
  }

  /// <summary>
  /// 取成员访问或调用的简单成员名。
  /// </summary>
  /// <param name="expression">调用表达式、成员访问表达式或简单名。</param>
  /// <returns>成员简单名（<c>a.B()</c>、<c>B()</c>、<c>a.B&lt;T&gt;()</c> 都返回 <c>B</c>）；无法判定时为 <see langword="null"/>。</returns>
  internal static string? MemberName(ExpressionSyntax expression) => expression switch
  {
    MemberAccessExpressionSyntax memberAccess => MemberName(memberAccess.Name),
    InvocationExpressionSyntax invocation => MemberName(invocation.Expression),
    MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
    SimpleNameSyntax simple => simple.Identifier.ValueText,
    _ => null
  };

  /// <summary>
  /// 取调用或成员访问上的类型参数简单名。
  /// </summary>
  /// <param name="expression">调用表达式或成员访问表达式。</param>
  /// <returns>类型参数的简单名序列；没有类型参数时为空。</returns>
  internal static IReadOnlyList<string> TypeArguments(ExpressionSyntax expression)
  {
    SimpleNameSyntax? name = expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
      InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax nested } => nested.Name,
      InvocationExpressionSyntax { Expression: SimpleNameSyntax simple } => simple,
      SimpleNameSyntax direct => direct,
      _ => null
    };

    return name is GenericNameSyntax generic
      ? [.. generic.TypeArgumentList.Arguments.Select(SimpleTypeName)]
      : [];
  }

  /// <summary>
  /// 取类型语法的简单名。
  /// </summary>
  /// <param name="type">类型语法节点。</param>
  /// <returns>最右侧标识符（解包可空与限定名后）；无法判定时返回节点文本。</returns>
  internal static string SimpleTypeName(TypeSyntax type) => type switch
  {
    NullableTypeSyntax nullable => SimpleTypeName(nullable.ElementType),
    QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
    AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
    GenericNameSyntax generic => generic.Identifier.ValueText,
    PredefinedTypeSyntax predefined => predefined.Keyword.ValueText,
    _ => type.ToString()
  };

  /// <summary>
  /// 取类型语法的泛型实参，解包可空、限定名与别名限定名写法。
  /// </summary>
  /// <param name="type">类型语法节点。</param>
  /// <returns>泛型实参序列；非泛型类型时为空。</returns>
  internal static IReadOnlyList<TypeSyntax> TypeArgumentList(TypeSyntax type) => type switch
  {
    NullableTypeSyntax nullable => TypeArgumentList(nullable.ElementType),
    QualifiedNameSyntax qualified => TypeArgumentList(qualified.Right),
    AliasQualifiedNameSyntax alias => TypeArgumentList(alias.Name),
    GenericNameSyntax generic => [.. generic.TypeArgumentList.Arguments],
    _ => []
  };

  /// <summary>
  /// 判断表达式是否创建指定简单类型名的实例。
  /// </summary>
  /// <param name="expression">待判断表达式。</param>
  /// <param name="typeName">期望的类型简单名。</param>
  /// <returns>显式 <c>new T(...)</c> 且类型简单名相同时为 <see langword="true"/>。</returns>
  internal static bool CreatesType(ExpressionSyntax expression, string typeName) =>
    expression is ObjectCreationExpressionSyntax creation && SimpleTypeName(creation.Type) == typeName;

  /// <summary>
  /// 按简单名查找特性，同时接受带与不带 <c>Attribute</c> 后缀两种写法。
  /// </summary>
  /// <param name="attributeLists">特性列表。</param>
  /// <param name="attributeName">特性简单名，例如 <c>Description</c> 或 <c>DescriptionAttribute</c>。</param>
  /// <returns>第一个匹配的特性；不存在时为 <see langword="null"/>。</returns>
  internal static AttributeSyntax? FindAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName) =>
    attributeLists.SelectMany(list => list.Attributes).FirstOrDefault(attribute =>
      SimpleAttributeName(attribute) is string name &&
      (name == attributeName || name == $"{attributeName}Attribute"));

  /// <summary>
  /// 取特性的字面量字符串参数。
  /// </summary>
  /// <param name="attribute">特性节点，允许为 <see langword="null"/>。</param>
  /// <returns>唯一的字符串字面量参数值；无参数、多参数或非字面量时为 <see langword="null"/>。</returns>
  internal static string? LiteralStringArgument(AttributeSyntax? attribute) =>
    attribute?.ArgumentList?.Arguments.Count == 1 &&
    attribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
    literal.IsKind(SyntaxKind.StringLiteralExpression)
      ? literal.Token.ValueText
      : null;

  /// <summary>
  /// 逐级向上定位包含 <c>AGENTS.md</c> 的仓库根目录。
  /// </summary>
  /// <returns>仓库根目录绝对路径。</returns>
  /// <exception cref="InvalidOperationException">逐级向上都未找到 <c>AGENTS.md</c> 时抛出。</exception>
  internal static string FindRepositoryRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
    {
      directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("未能定位仓库根目录（未找到 AGENTS.md）。");
  }

  /// <summary>
  /// 在仓储工程的指定相对目录内定位唯一文件，不递归、不跨目录回退。
  /// </summary>
  /// <remarks>
  /// 映射文件与建表脚本的目录归属由目录冻结断言保证；定位时再限定目录，可以把"文件被挪走"直接判为失败，
  /// 而不是让断言在别的目录里悄悄读到同一个文件。
  /// </remarks>
  /// <param name="relativeDirectory">相对 <c>server/Dy.MedicalRecognition.Repository</c> 的目录，层级用 <c>/</c> 分隔。</param>
  /// <param name="fileName">文件名，例如 <c>MutualRecognitionItem.xml</c>。</param>
  /// <returns>该文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">指定目录内不存在该文件时抛出。</exception>
  internal static string FindRepositoryFile(string relativeDirectory, string fileName)
  {
    string directory = Path.Combine(
      FindRepositoryRoot(),
      "server",
      "Dy.MedicalRecognition.Repository",
      relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
    string candidate = Path.Combine(directory, fileName);
    return File.Exists(candidate)
      ? candidate
      : throw new FileNotFoundException($"仓储目录 '{relativeDirectory}' 内未找到文件 '{fileName}'。");
  }
  /// <summary>
  /// 取特性名的简单名，容忍限定名与别名限定名写法。
  /// </summary>
  /// <param name="attribute">特性节点。</param>
  /// <returns>特性简单名。</returns>
  private static string SimpleAttributeName(AttributeSyntax attribute) => attribute.Name switch
  {
    QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
    AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
    SimpleNameSyntax simple => simple.Identifier.ValueText,
    _ => attribute.Name.ToString()
  };
}
