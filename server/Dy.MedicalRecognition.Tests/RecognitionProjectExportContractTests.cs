using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 管理端页面导出的契约与端点静态面：导出请求只承载页面可产生的服务端查询条件
/// （互认项目为组织与可选关键字，平台金额为组织、医院、院区与可选关键字，本院区金额为院区与可选关键字）、
/// 导出文件读模型恰含三个交付字段、查询契约以 <c>.Export.cs</c> 分片声明互认项目、平台金额与本院区金额三个导出入口、
/// 实现以 NonAction 排除自动端点，手工导出控制器在带版本前缀路由下以三个 POST 动作承载导出并返回禁止缓存的文件流。
/// </summary>
/// <remarks>
/// 全部为源码契约判定（Roslyn 语法节点，工具复用 <see cref="SourceSyntaxGuard"/>）：注释与字符串里的同名写法
/// 不产生节点，声明被删除、字段被改名、特性被移除都会直接失败；不启动宿主、不连库。
/// 真实宿主上的导出下载链路由宿主批次取证。
/// </remarks>
public sealed class RecognitionProjectExportContractTests
{
  /// <summary>互认项目导出请求类型的仓库内相对路径。</summary>
  private const string ExportRequestPath =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/MutualRecognition/RecognitionProjectConfigurationExportRequest.cs";

  /// <summary>平台金额导出请求类型的仓库内相对路径。</summary>
  private const string AmountExportRequestPath =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionAmount/RecognitionAmountExportRequest.cs";

  /// <summary>本院区金额导出请求类型的仓库内相对路径。</summary>
  private const string BranchAmountExportRequestPath =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionAmount/BranchRecognitionAmountExportRequest.cs";

  /// <summary>查询契约分片的仓库内相对路径。</summary>
  private const string ExportContractPath =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Export.cs";

  /// <summary>应用层导出实现分片的仓库内相对路径。</summary>
  private const string ExportImplementationPath =
    "server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Export.cs";

  /// <summary>导出控制器动作的应用层入口方法名。</summary>
  private const string ExportEntryMethod = "GetRecognitionProjectConfigurationExportAsync";

  /// <summary>平台金额导出控制器动作的应用层入口方法名。</summary>
  private const string AmountExportEntryMethod = "GetRecognitionAmountExportAsync";

  /// <summary>本院区金额导出控制器动作的应用层入口方法名。</summary>
  private const string BranchAmountExportEntryMethod = "GetBranchRecognitionAmountExportAsync";

  /// <summary>
  /// 导出请求恰声明组织编码与关键字两个字段：组织必填（Required 加非空白声明），关键字可选且传入空白文本按校验拒绝，
  /// 与互认配置列表请求同一口径；不携带配置状态筛选字段（导出同时包含启用与停用的配置）、不携带分页对象，
  /// 也没有任何多余范围字段。
  /// </summary>
  [Fact]
  public void Export_request_declares_organization_and_optional_keyword_only()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(ExportRequestPath.Split('/'));

    string? namespaceText = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
      .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
      .SingleOrDefault();
    Assert.Equal("Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition", namespaceText);

    TypeDeclarationSyntax request = Assert.Single(root.DescendantNodes().OfType<TypeDeclarationSyntax>());
    Assert.Contains(request.Modifiers, modifier => modifier.ValueText == "sealed");
    Assert.Equal("RecognitionProjectConfigurationExportRequest", request.Identifier.ValueText);

    List<PropertyDeclarationSyntax> properties = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>()];
    Assert.Equal(
      ["OrganizationCode", "Keyword"],
      properties.Select(property => property.Identifier.ValueText).ToArray());

    PropertyDeclarationSyntax organization = properties.Single(property => property.Identifier.ValueText == "OrganizationCode");
    Assert.Equal("string", SourceSyntaxGuard.SimpleTypeName(organization.Type));
    Assert.NotNull(SourceSyntaxGuard.FindAttribute(organization.AttributeLists, "Required"));
    AttributeSyntax? organizationNonEmpty = SourceSyntaxGuard.FindAttribute(organization.AttributeLists, "NonEmpty");
    Assert.NotNull(organizationNonEmpty);
    Assert.Equal("参数校验失败：组织编码不能是空白。", SourceSyntaxGuard.LiteralStringArgument(organizationNonEmpty));

    PropertyDeclarationSyntax keyword = properties.Single(property => property.Identifier.ValueText == "Keyword");
    Assert.Equal("string?", DescribeType(keyword.Type));
    AttributeSyntax? keywordNonEmpty = SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "NonEmpty");
    Assert.NotNull(keywordNonEmpty);
    Assert.Equal("参数校验失败：关键字不能是空白。", SourceSyntaxGuard.LiteralStringArgument(keywordNonEmpty));
    Assert.Null(SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "Required"));
  }

  /// <summary>
  /// 平台金额导出请求恰声明组织、医院、院区与关键字四个字段：三个范围值必填（Required 加非空白声明），
  /// 关键字可选且缺失或去首尾空白后为空都视为未提供（不声明必填、也不拒绝空白文本，与金额列表请求一致）；
  /// 不携带标准项目编码、任何状态筛选字段与分页对象。
  /// </summary>
  [Fact]
  public void Amount_export_request_declares_three_scope_fields_and_optional_keyword()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(AmountExportRequestPath.Split('/'));

    string? namespaceText = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
      .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
      .SingleOrDefault();
    Assert.Equal("Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount", namespaceText);

    TypeDeclarationSyntax request = Assert.Single(root.DescendantNodes().OfType<TypeDeclarationSyntax>());
    Assert.Contains(request.Modifiers, modifier => modifier.ValueText == "sealed");
    Assert.Equal("RecognitionAmountExportRequest", request.Identifier.ValueText);

    List<PropertyDeclarationSyntax> properties = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>()];
    Assert.Equal(
      ["OrganizationCode", "HospitalCode", "BranchCode", "Keyword"],
      properties.Select(property => property.Identifier.ValueText).ToArray());

    foreach ((string propertyName, string blankMessage) in new[]
    {
      ("OrganizationCode", "参数校验失败：组织编码不能是空白。"),
      ("HospitalCode", "参数校验失败：医院编码不能是空白。"),
      ("BranchCode", "参数校验失败：院区编码不能是空白。")
    })
    {
      PropertyDeclarationSyntax scope = properties.Single(property => property.Identifier.ValueText == propertyName);
      Assert.Equal("string", SourceSyntaxGuard.SimpleTypeName(scope.Type));
      Assert.NotNull(SourceSyntaxGuard.FindAttribute(scope.AttributeLists, "Required"));
      AttributeSyntax? scopeNonEmpty = SourceSyntaxGuard.FindAttribute(scope.AttributeLists, "NonEmpty");
      Assert.NotNull(scopeNonEmpty);
      Assert.Equal(blankMessage, SourceSyntaxGuard.LiteralStringArgument(scopeNonEmpty));
    }

    PropertyDeclarationSyntax keyword = properties.Single(property => property.Identifier.ValueText == "Keyword");
    Assert.Equal("string?", DescribeType(keyword.Type));
    Assert.Null(SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "Required"));
    Assert.Null(SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "NonEmpty"));
  }

  /// <summary>
  /// 本院区金额导出请求恰声明院区编码与关键字两个字段：院区必填（Required 加非空白声明），
  /// 关键字可选且缺失或去首尾空白后为空都视为未提供（不声明必填、也不拒绝空白文本，与本院区列表请求一致）；
  /// 不携带组织与医院字段（两者由可信上下文解析，请求不得提交）、不携带标准项目编码、任何状态筛选字段与分页对象。
  /// </summary>
  [Fact]
  public void Branch_amount_export_request_declares_branch_and_optional_keyword_only()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(BranchAmountExportRequestPath.Split('/'));

    string? namespaceText = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
      .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
      .SingleOrDefault();
    Assert.Equal("Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount", namespaceText);

    TypeDeclarationSyntax request = Assert.Single(root.DescendantNodes().OfType<TypeDeclarationSyntax>());
    Assert.Contains(request.Modifiers, modifier => modifier.ValueText == "sealed");
    Assert.Equal("BranchRecognitionAmountExportRequest", request.Identifier.ValueText);

    List<PropertyDeclarationSyntax> properties = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>()];
    Assert.Equal(["BranchCode", "Keyword"], properties.Select(property => property.Identifier.ValueText).ToArray());

    PropertyDeclarationSyntax branch = properties.Single(property => property.Identifier.ValueText == "BranchCode");
    Assert.Equal("string", SourceSyntaxGuard.SimpleTypeName(branch.Type));
    Assert.NotNull(SourceSyntaxGuard.FindAttribute(branch.AttributeLists, "Required"));
    AttributeSyntax? branchNonEmpty = SourceSyntaxGuard.FindAttribute(branch.AttributeLists, "NonEmpty");
    Assert.NotNull(branchNonEmpty);
    Assert.Equal("参数校验失败：院区编码不能是空白。", SourceSyntaxGuard.LiteralStringArgument(branchNonEmpty));

    PropertyDeclarationSyntax keyword = properties.Single(property => property.Identifier.ValueText == "Keyword");
    Assert.Equal("string?", DescribeType(keyword.Type));
    Assert.Null(SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "Required"));
    Assert.Null(SourceSyntaxGuard.FindAttribute(keyword.AttributeLists, "NonEmpty"));
  }

  /// <summary>
  /// 导出文件读模型恰含文件名、文件格式与文件字节三个字段，独立声明、不复用统计导出读模型；
  /// 读模型不承载展示以外的职责，也没有公开可写集合。
  /// </summary>
  [Fact]
  public void Export_file_read_model_declares_exactly_name_format_and_bytes()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations = SourceSyntaxGuard.ReadTypeDeclarations(
      "server/Dy.MedicalRecognition.Application.Contracts/ReadModels", "ConfigurationExportFileReadModel");

    string expectedPath = "server/Dy.MedicalRecognition.Application.Contracts/ReadModels/ConfigurationExportFileReadModel.cs";
    Assert.True(
      declarations.Count == 1 && declarations[0].RelativePath == expectedPath,
      $"导出文件读模型应恰好声明在 {expectedPath}，实际：{string.Join("、", declarations.Select(declaration => declaration.RelativePath))}。");

    Assert.Equal(
      [("FileName", "string"), ("FileFormat", "string"), ("FileStream", "byte[]")],
      declarations[0].Root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
        .Select(property => (property.Identifier.ValueText, DescribeType(property.Type))).ToArray());
  }

  /// <summary>
  /// 查询契约以 <c>.Export.cs</c> 分片扩展，分片内恰声明互认项目、平台金额与本院区金额三个导出入口，签名冻结为
  /// 返回导出文件读模型、接收各自导出请求；应用层实现分片的同名入口以 NonAction 声明排除自动端点暴露，
  /// 也不声明事务（导出为只读链）。
  /// </summary>
  [Fact]
  public void Export_contract_partial_declares_the_frozen_entries_and_the_implementation_excludes_auto_endpoints()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> contractDeclarations =
      SourceSyntaxGuard.ReadTypeDeclarations(
        "server/Dy.MedicalRecognition.Application.Contracts/Queries", nameof(Dy.MedicalRecognition.Application.Contracts.Queries.IMedicalRecognitionReportQueryAppService));
    Assert.Contains(
      contractDeclarations,
      declaration => declaration.RelativePath == ExportContractPath);

    CompilationUnitSyntax contractRoot = contractDeclarations.Single(declaration => declaration.RelativePath == ExportContractPath).Root;
    MethodDeclarationSyntax[] contractMethods = [.. contractRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()];
    Assert.Equal(3, contractMethods.Length);

    (string MethodName, string RequestType)[] entries =
    [
      (ExportEntryMethod, "RecognitionProjectConfigurationExportRequest"),
      (AmountExportEntryMethod, "RecognitionAmountExportRequest"),
      (BranchAmountExportEntryMethod, "BranchRecognitionAmountExportRequest")
    ];
    foreach ((string methodName, string requestType) in entries)
    {
      MethodDeclarationSyntax contractEntry = Assert.Single(
        contractMethods,
        candidate => candidate.Identifier.ValueText == methodName);
      Assert.Null(contractEntry.Body);
      Assert.Null(contractEntry.ExpressionBody);
      Assert.Equal("Task<ConfigurationExportFileReadModel>", DescribeType(contractEntry.ReturnType));
      Assert.Equal(
        [$"{requestType} request"],
        contractEntry.ParameterList.Parameters.Select(parameter =>
          $"{DescribeType(parameter.Type!)} {parameter.Identifier.ValueText}").ToArray());
    }

    CompilationUnitSyntax implementationRoot = SourceSyntaxGuard.Read(ExportImplementationPath.Split('/'));
    foreach ((string methodName, string _) in entries)
    {
      MethodDeclarationSyntax implementationEntry = Assert.Single(
        implementationRoot.DescendantNodes().OfType<MethodDeclarationSyntax>(),
        candidate => candidate.Identifier.ValueText == methodName);
      Assert.NotNull(SourceSyntaxGuard.FindAttribute(implementationEntry.AttributeLists, "NonAction"));
      Assert.Null(SourceSyntaxGuard.FindAttribute(implementationEntry.AttributeLists, "WorkUnit"));
    }
  }

  /// <summary>
  /// 导出控制器声明在带版本前缀的路由下，三个 POST 动作的相对路由冻结为互认项目、平台金额与本院区金额，
  /// 各自绑定请求体上的对应导出请求类型；动作经统一组装方法返回禁止缓存的文件流，
  /// 该前缀与自动端点前缀不重复、不遮蔽。
  /// </summary>
  [Fact]
  public void Export_controller_declares_versioned_route_with_the_recognition_projects_post_action()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition", "Controllers", "RecognitionExportController.cs");

    TypeDeclarationSyntax controller = Assert.Single(root.DescendantNodes().OfType<ClassDeclarationSyntax>());
    Assert.Equal("RecognitionExportController", controller.Identifier.ValueText);

    string[] apiControllerRoutes = [.. controller.AttributeLists
      .SelectMany(list => list.Attributes)
      .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "Route")
      .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
    string controllerRoute = Assert.Single(apiControllerRoutes);
    Assert.Equal("api/v1/recognition-export", controllerRoute);
    Assert.Equal("api", controllerRoute.Split('/')[0]);
    Assert.Equal("v1", controllerRoute.Split('/')[1]);
    Assert.NotEqual("IMedicalRecognitionReportQueryAppService", controllerRoute.Split('/')[1]);

    (string ActionName, string RelativeRoute, string RequestType, string EntryMethod)[] actions =
    [
      ("ExportRecognitionProjectsAsync", "recognition-projects", "RecognitionProjectConfigurationExportRequest", ExportEntryMethod),
      ("ExportRecognitionAmountsAsync", "recognition-amounts", "RecognitionAmountExportRequest", AmountExportEntryMethod),
      ("ExportBranchRecognitionAmountsAsync", "branch-recognition-amounts", "BranchRecognitionAmountExportRequest", BranchAmountExportEntryMethod)
    ];
    foreach ((string actionName, string relativeRoute, string requestType, string entryMethod) in actions)
    {
      MethodDeclarationSyntax action = controller.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Single(declaration => declaration.Identifier.ValueText == actionName);
      string[] templates = [.. action.AttributeLists
        .SelectMany(list => list.Attributes)
        .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpPost")
        .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
      Assert.Equal([relativeRoute], templates);

      ParameterSyntax parameter = Assert.Single(action.ParameterList.Parameters);
      Assert.Equal(requestType, DescribeType(parameter.Type!));
      Assert.NotNull(SourceSyntaxGuard.FindAttribute(parameter.AttributeLists, "FromBody"));

      // 动作把请求转交应用层导出入口，再经统一组装方法返回文件流：业务判断不进入控制器。
      Assert.Contains(
        action.DescendantNodes().OfType<InvocationExpressionSyntax>(),
        invocation => SourceSyntaxGuard.MemberName(invocation.Expression) == entryMethod);
      Assert.Contains(
        action.DescendantNodes().OfType<InvocationExpressionSyntax>(),
        invocation => invocation.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "CreateExportResponse");
    }

    // 响应组装方法设置禁止缓存响应头，并按读模型的字节、内容类型与文件名返回文件。
    MethodDeclarationSyntax responseAssembler = controller.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(declaration => declaration.Identifier.ValueText == "CreateExportResponse");
    AssignmentExpressionSyntax cacheHeader = Assert.Single(
      responseAssembler.DescendantNodes().OfType<AssignmentExpressionSyntax>(),
      assignment => assignment.Left.ToString() == "Response.Headers.CacheControl");
    Assert.Equal("no-store, no-cache, must-revalidate", Assert.IsType<LiteralExpressionSyntax>(cacheHeader.Right).Token.ValueText);
    InvocationExpressionSyntax fileInvocation = Assert.Single(
      responseAssembler.DescendantNodes().OfType<InvocationExpressionSyntax>(),
      invocation => invocation.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "File");
    Assert.Equal(
      ["file.FileStream", "ExcelContentType", "file.FileName"],
      [.. fileInvocation.ArgumentList.Arguments.Select(argument => argument.Expression.ToString())]);
  }

  /// <summary>重建属性、参数或返回类型的类型文本：保留可空标记与泛型实参，限定名取简单名。</summary>
  /// <param name="type">类型语法节点。</param>
  /// <returns>归一后的类型文本。</returns>
  private static string DescribeType(TypeSyntax type) => type switch
  {
    Microsoft.CodeAnalysis.CSharp.Syntax.NullableTypeSyntax nullable => DescribeType(nullable.ElementType) + "?",
    Microsoft.CodeAnalysis.CSharp.Syntax.GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(", ", generic.TypeArgumentList.Arguments.Select(DescribeType))}>",
    Microsoft.CodeAnalysis.CSharp.Syntax.ArrayTypeSyntax array => $"{DescribeType(array.ElementType)}[]",
    _ => SourceSyntaxGuard.SimpleTypeName(type)
  };
}
