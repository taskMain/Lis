using System.Reflection;
using System.Text.Json.Nodes;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// T02 票 02 的后端认证与当前会话静态契约核对（矩阵 V1、V2、V3、V4、V14、V17 的静态面）。
/// </summary>
/// <remarks>
/// 宿主认证由框架模块提供，401 与 200 的运行结果由真实接口取证承担；
/// 本类冻结的是代码与配置中可机械判定的部分：IAM 模块装配、配置节、旧认证依赖归零，
/// 以及会话只读入口的存在性、路由与返回字段集合。
/// 判据全部按语法节点与 JSON 结构读取，不做整文件文本匹配。
/// </remarks>
public sealed class T02IamAuthenticationTests
{
  /// <summary>IAM 宿主接入模块的类型简单名。</summary>
  private const string IamOpenApiModuleTypeName = "IamApronOpenApiModule";

  /// <summary>旧的共享密钥认证包名。</summary>
  private const string LegacySecurityPackage = "Dy.Apron.Security";

  /// <summary>旧认证方案携带的 Scalar 插件包名，改由 IAM OpenAPI 包提供文档页。</summary>
  private const string LegacyScalarPackage = "Dy.Plugin.AspNet.Scalar";

  /// <summary>IAM 宿主接入包名。</summary>
  private const string IamOpenApiPackage = "Dy.IAM.Apron.OpenApi";

  /// <summary>
  /// V1：应用模块声明对 IAM OpenAPI 模块的依赖，宿主因此加载 IAM 认证而不注册旧认证方案。
  /// </summary>
  /// <remarks>
  /// 判据读运行时的模块依赖特性：模块名按简单名比较，依赖集合必须恰好包含 IAM 模块一次。
  /// 依赖缺失时宿主不会加载 IAM 认证，所有业务端点退回匿名访问。
  /// </remarks>
  [Fact]
  public void Application_module_depends_on_the_iam_openapi_module()
  {
    Type moduleType = typeof(Dy.MedicalRecognition.Application.MedicalRecognitionApplicationModule);
    Attribute? dependsOn = moduleType.GetCustomAttributes()
      .SingleOrDefault(attribute => attribute.GetType().Name == "DependsOnAttribute");

    Assert.NotNull(dependsOn);

    // 依赖以 Type[] 暴露；按简单名比较，避免测试项目直接绑定 IAM 包版本。
    Type[] dependedTypes = (Type[])dependsOn!.GetType().GetProperty("DependedTypes")!.GetValue(dependsOn)!;
    Assert.Contains(dependedTypes, type => type.Name == IamOpenApiModuleTypeName);

    // 变异证据：把依赖集合换成不含 IAM 模块的样本后，同一条判据必须判为不存在。
    Type[] sampleWithoutIamModule = [typeof(string)];
    Assert.DoesNotContain(sampleWithoutIamModule, type => type.Name == IamOpenApiModuleTypeName);
  }

  /// <summary>
  /// V1、V17：集中包管理声明 IAM 接入包，旧的共享密钥认证包与旧 Scalar 插件包不再出现在依赖清单中。
  /// </summary>
  /// <remarks>包名集合决定还原结果；任何一处保留旧包都会让旧认证模块继续参与启动。</remarks>
  [Fact]
  public void Central_package_management_declares_iam_and_drops_legacy_authentication_packages()
  {
    string centralPackages = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Directory.Packages.props"));

    Assert.Contains($"Include=\"{IamOpenApiPackage}\"", centralPackages, StringComparison.Ordinal);
    Assert.Contains($"Include=\"Dy.Apron.QuickStart\"", centralPackages, StringComparison.Ordinal);
    Assert.DoesNotContain($"Include=\"{LegacySecurityPackage}\"", centralPackages, StringComparison.Ordinal);
    Assert.DoesNotContain($"Include=\"{LegacyScalarPackage}\"", centralPackages, StringComparison.Ordinal);
  }

  /// <summary>
  /// V1、V17：全部工程文件都不再引用旧的共享密钥认证包与旧 Scalar 插件包，IAM 接入包由应用模块工程引用。
  /// </summary>
  /// <remarks>
  /// 工程引用决定运行时会加载哪些程序集；集中包管理之外的直接引用同样计入判定。
  /// 扫描面是 server 下全部工程文件（含测试工程，排除生成物目录），任一工程重新引入旧包都会失败。
  /// </remarks>
  [Fact]
  public void Project_files_reference_iam_openapi_without_legacy_authentication_packages()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string[] projectFiles =
    [
      .. Directory.EnumerateFiles(Path.Combine(repositoryRoot, "server"), "*.csproj", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
    ];

    // 扫描面下界：工程被移走或过滤条件写错时，下面的空违规定义会退化成恒真。
    Assert.Equal(7, projectFiles.Length);

    foreach (string projectFile in projectFiles)
    {
      string project = File.ReadAllText(projectFile);
      Assert.DoesNotContain($"PackageReference Include=\"{LegacySecurityPackage}\"", project, StringComparison.Ordinal);
      Assert.DoesNotContain($"PackageReference Include=\"{LegacyScalarPackage}\"", project, StringComparison.Ordinal);
    }

    Assert.Contains(
      $"PackageReference Include=\"{IamOpenApiPackage}\"",
      File.ReadAllText(Path.Combine(repositoryRoot, "server", "Dy.MedicalRecognition.Application", "Dy.MedicalRecognition.Application.csproj")),
      StringComparison.Ordinal);
  }

  /// <summary>
  /// V1、V4、V17：受版本控制的应用配置声明 IAM 配置节与宿主接入三节，旧的共享密钥配置不再存在。
  /// </summary>
  /// <remarks>
  /// Authority 指向 IAM 签发端，ApplicationCode 为本应用登记编码，AllowHttp 允许内网 HTTP 地址，
  /// ProfileTimeoutSeconds 落在框架允许的 1 至 120 秒。旧共享密钥配置继续存在时，
  /// 旧认证模块即使不被引用也可能被后续改动重新接回运行链路。
  /// appsettings.json 是仓库内受版本控制的配置；开发环境配置文件不入库，判据只覆盖入库文件，
  /// 干净检出同样成立。该文件不再列入再生成差异保护清单，OpenApi、Cors 与 HttpConfig 三节
  /// 改由这里的显式断言冻结：生成器覆盖会让端口、跨域来源与文档页开关一起消失。
  /// </remarks>
  [Fact]
  public void Application_settings_declare_the_iam_section_without_shared_secret()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    JsonNode settings = JsonNode.Parse(File.ReadAllText(
      Path.Combine(repositoryRoot, "server", "Dy.MedicalRecognition", "appsettings.json")))!;

    Assert.Null(settings["JwtSettings"]);

    JsonNode? iam = settings["Iam"];
    Assert.NotNull(iam);
    Assert.Equal("http://183.224.180.166:35015", iam!["Authority"]!.GetValue<string>());
    Assert.Equal("medical-recognition", iam!["ApplicationCode"]!.GetValue<string>());
    Assert.True(iam!["AllowHttp"]!.GetValue<bool>());

    int profileTimeoutSeconds = iam!["ProfileTimeoutSeconds"]!.GetValue<int>();
    Assert.InRange(profileTimeoutSeconds, 1, 120);

    // 宿主接入三节：文档页、跨域来源与外部服务默认地址；节点缺失或取值漂移都在这里失败。
    Assert.True(settings["OpenApi"]!["Enabled"]!.GetValue<bool>());

    JsonArray origins = settings["Cors"]!["Origins"]!.AsArray();
    Assert.Equal(["*"], origins.Select(origin => origin!.GetValue<string>()));

    JsonNode defaultService = settings["HttpConfig"]!["HttpServiceConfigs"]!["Default"]!;
    Assert.Equal("http://localhost", defaultService["Host"]!.GetValue<string>());
    Assert.Equal(100, defaultService["Timeout"]!.GetValue<int>());
  }

  /// <summary>
  /// V17：生产源码不再引用旧认证命名空间，也不再读取或解析 JWT 令牌。
  /// </summary>
  /// <remarks>
  /// 判定面是全部生产工程的 <c>using</c> 指令与代码标识符；注释与字符串不参与标识符判定。
  /// 旧命名空间仍被引用、或代码仍手工解析令牌时，认证来源就不止 IAM 一处。
  /// </remarks>
  [Fact]
  public void Production_sources_do_not_reference_legacy_authentication_or_jwt_token_reading()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string[] productionDirectories =
    [
      "server/Dy.MedicalRecognition",
      "server/Dy.MedicalRecognition.Application",
      "server/Dy.MedicalRecognition.Application.Contracts",
      "server/Dy.MedicalRecognition.Domain",
      "server/Dy.MedicalRecognition.Domain.Share",
      "server/Dy.MedicalRecognition.Repository"
    ];

    HashSet<string> bannedIdentifiers = new(StringComparer.Ordinal)
    {
      "JwtSettings", "JwtSecurityTokenHandler", "SymmetricSecurityKey", "ReadJwtToken"
    };

    List<string> violations = [];
    int scannedFiles = 0;
    foreach (string relativeDirectory in productionDirectories)
    {
      string directory = Path.Combine(repositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
      foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
      {
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.EndsWith(".g.cs", StringComparison.Ordinal))
        {
          continue;
        }

        scannedFiles++;
        CompilationUnitSyntax root = SourceSyntaxGuard.Parse(File.ReadAllText(file));
        string relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');

        foreach (UsingDirectiveSyntax usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
          string? namespaceName = usingDirective.Name?.ToString();
          if (namespaceName is not null && namespaceName.StartsWith(LegacySecurityPackage, StringComparison.Ordinal))
            violations.Add($"{relativePath}: 引用了旧认证命名空间 {namespaceName}");
        }

        foreach (string identifier in root.DescendantTokens().Select(token => token.ValueText).Where(bannedIdentifiers.Contains))
          violations.Add($"{relativePath}: 读取或解析旧 JWT 令牌 {identifier}");
      }
    }

    // 扫描面下界：目录被搬空时下面的空违规定义会退化成恒真。
    Assert.True(scannedFiles >= 200, $"受核对的生产源码应不少于 200 个，实际 {scannedFiles} 个。");
    Assert.Empty(violations);
  }

  /// <summary>
  /// V2、V3、V4：会话只读入口以 GET 暴露，注入 IAM 当前上下文，返回账号与会话组织的五个字段。
  /// </summary>
  /// <remarks>
  /// 入口只读取已验证身份，不做权限判定；未选择工作身份时组织字段可空且不报错。
  /// 返回字段集合精确冻结：多出字段会把内部标识泄漏到契约，缺少字段则调用方无法核对会话组织。
  /// </remarks>
  [Fact]
  public void Session_endpoint_exposes_the_current_identity_as_a_read_only_entry()
  {
    CompilationUnitSyntax controller = SourceSyntaxGuard.Read(
      "server", "Dy.MedicalRecognition", "Controllers", "CurrentSessionController.cs");
    ClassDeclarationSyntax declaration = controller.DescendantNodes().OfType<ClassDeclarationSyntax>()
      .Single(type => type.Identifier.ValueText == "CurrentSessionController");

    string[] routes = [.. declaration.AttributeLists.SelectMany(list => list.Attributes)
      .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "Route")
      .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
    Assert.Equal(["api/v1/current-session"], routes);

    MethodDeclarationSyntax action = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(method => method.Identifier.ValueText == "Get");
    Assert.Contains(
      action.AttributeLists.SelectMany(list => list.Attributes),
      attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpGet");

    ParameterSyntax constructorParameter = declaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
      .Single().ParameterList.Parameters.Single();
    Assert.Equal("ICurrentIamContext", SourceSyntaxGuard.SimpleTypeName(constructorParameter.Type!));

    Type? responseType = typeof(Dy.MedicalRecognition.Application.Contracts.Queries.IMedicalRecognitionReportQueryAppService)
      .Assembly.GetType("Dy.MedicalRecognition.Application.Contracts.Queries.Session.CurrentSessionReadModel");
    Assert.NotNull(responseType);

    string[] responseFields = [.. responseType!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Select(property => property.Name)
      .Order(StringComparer.Ordinal)];
    Assert.Equal(
      ["OrganizationId", "SessionId", "TenantId", "UserId", "WorkIdentityId"],
      responseFields);
  }
}
