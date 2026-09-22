using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 6 统计导出的端点面：应用层两个导出实现方法以 NonAction 声明排除自动端点暴露，
/// 手工导出控制器以带版本前缀路由下的两个 POST 动作承载导出，响应组装为禁止缓存的文件流。
/// </summary>
/// <remarks>
/// 对应后端设计验证矩阵 V1（宿主共十一个端点：九个自动端点加导出控制器两个 POST 动作）与
/// V38 的静态核对面（docs/plans/008-阶段6-互认统计与导出/Server/design.md「验证矩阵」章）。
/// 全部为静态断言：反射核对特性声明，Roslyn 语法节点核对路由、绑定与响应组装；不启动宿主、不连库。
/// 真实宿主上的导出下载链路（经已鉴权客户端取回字节）由票 09 的宿主批次取证。
/// </remarks>
public sealed class Stage6EndpointTests
{
  /// <summary>手工导出控制器的仓库内相对路径。</summary>
  private const string ExportControllerPath = "server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs";

  /// <summary>禁止缓存的响应头取值，与报告 PDF 下载先例一致。</summary>
  private const string NoStoreCacheControl = "no-store, no-cache, must-revalidate";

  /// <summary>
  /// V1：宿主不把导出入口自动暴露为端点——平台导出方法与本院导出入口都以 NonAction 声明排除，
  /// 两个导出入口与控制器两个动作都是只读用例、不声明事务。
  /// </summary>
  /// <remarks>
  /// 框架的动态控制器约定对带 NonAction 声明的动作方法跳过自动端点注册；
  /// 既有查询入口不带该声明，因此判定不是对任意方法恒真。
  /// </remarks>
  [Fact]
  public void Export_entries_declare_non_action_while_query_entries_do_not()
  {
    Type appService = typeof(MedicalRecognitionReportQueryAppService);
    MethodInfo platformExport = appService.GetMethod("GetStatisticsExportAsync", [typeof(RecognitionStatisticsExportRequest)])!;
    MethodInfo branchExport = appService.GetMethod("GetBranchStatisticsExportAsync", [typeof(BranchRecognitionStatisticsExportRequest)])!;
    Assert.NotNull(FindAttribute(platformExport, "NonActionAttribute"));
    Assert.NotNull(FindAttribute(branchExport, "NonActionAttribute"));

    // 变异证据：既有查询入口不带 NonAction 声明；若判定对任意方法都返回非空，下一条断言即失败。
    MethodInfo queryEntry = appService.GetMethod("QueryRecognitionMatchRecordAsync", [typeof(RecognitionMatchRecordQueryRequest)])!;
    Assert.Null(FindAttribute(queryEntry, "NonActionAttribute"));

    // 统计链零写入：导出入口只读，不声明事务；两个控制器动作同样只读，事务核对见导出控制器动作。
    Assert.Null(FindAttribute(platformExport, "WorkUnitAttribute"));
    Assert.Null(FindAttribute(branchExport, "WorkUnitAttribute"));

    MethodInfo platformAction = ControllerAction("ExportPlatformStatisticsAsync");
    MethodInfo branchAction = ControllerAction("ExportBranchStatisticsAsync");
    Assert.Null(FindAttribute(platformAction, "WorkUnitAttribute"));
    Assert.Null(FindAttribute(branchAction, "WorkUnitAttribute"));
  }

  /// <summary>
  /// V1：导出控制器声明在带版本前缀的路由下，两个 POST 动作的相对路由与请求体绑定类型逐项冻结，
  /// 且该前缀与自动端点前缀不重复、不遮蔽。
  /// </summary>
  /// <remarks>
  /// 自动端点前缀首段为 <c>Api</c>；控制器路由为 <c>api/v1/statistics-export</c>，第二段是版本号，
  /// 两者在前两段上不同，不存在同一路径被两处声明的可能。平台动作绑定平台请求类型，本院动作绑定本院请求类型。
  /// </remarks>
  [Fact]
  public void Export_controller_declares_versioned_route_with_two_post_actions()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read("server", "Dy.MedicalRecognition", "Controllers", "RecognitionStatisticsExportController.cs");

    TypeDeclarationSyntax controller = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
      .Single(declaration => declaration.Identifier.ValueText == "RecognitionStatisticsExportController");

    string[] apiControllerRoutes = [.. controller.AttributeLists
      .SelectMany(list => list.Attributes)
      .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "Route")
      .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
    string controllerRoute = Assert.Single(apiControllerRoutes);

    Assert.Equal("api/v1/statistics-export", controllerRoute);
    string[] controllerSegments = controllerRoute.Split('/');
    Assert.Equal("api", controllerSegments[0]);
    Assert.Equal("v1", controllerSegments[1]);

    (string ActionName, string RelativeRoute, Type RequestBodyType)[] actions =
    [
      ("ExportPlatformStatisticsAsync", "platform", typeof(RecognitionStatisticsExportRequest)),
      ("ExportBranchStatisticsAsync", "branch", typeof(BranchRecognitionStatisticsExportRequest))
    ];
    foreach ((string actionName, string relativeRoute, Type requestBodyType) in actions)
    {
      MethodDeclarationSyntax action = controller.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Single(declaration => declaration.Identifier.ValueText == actionName);
      string[] templates = [.. action.AttributeLists
        .SelectMany(list => list.Attributes)
        .Where(attribute => SourceSyntaxGuard.SimpleTypeName(attribute.Name) is "HttpPost")
        .Select(attribute => SourceSyntaxGuard.LiteralStringArgument(attribute) ?? string.Empty)];
      Assert.Equal([relativeRoute], templates);

      // 动作各自绑定平台/本院请求类型，绑定来源是请求体。
      ParameterInfo parameter = Assert.Single(ControllerAction(actionName).GetParameters());
      Assert.Equal(requestBodyType, parameter.ParameterType);
      Assert.Contains(parameter.GetCustomAttributes(), attribute => attribute.GetType().Name == "FromBodyAttribute");
    }
  }

  /// <summary>
  /// V38：两个导出动作经同一响应组装方法返回工作簿文件流——设置禁止缓存响应头，
  /// 按统一内容类型与读模型文件名返回文件字节。
  /// </summary>
  /// <remarks>
  /// 判定按语法节点核对：响应组装方法内恰好一处缓存头赋值、一次 File 调用且实参逐项冻结；
  /// 两个动作都调用该组装方法。删除缓存头赋值的变更副本必须使单处判定失败。
  /// </remarks>
  [Fact]
  public void Export_actions_assemble_no_store_file_stream_responses()
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Read("server", "Dy.MedicalRecognition", "Controllers", "RecognitionStatisticsExportController.cs");

    MethodDeclarationSyntax responseAssembler = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(declaration => declaration.Identifier.ValueText == "CreateExportResponse");
    AssignmentExpressionSyntax cacheHeader = Assert.Single(
      responseAssembler.DescendantNodes().OfType<AssignmentExpressionSyntax>(),
      assignment => assignment.Left.ToString() == "Response.Headers.CacheControl");
    Assert.Equal(NoStoreCacheControl, Assert.IsType<LiteralExpressionSyntax>(cacheHeader.Right).Token.ValueText);

    InvocationExpressionSyntax fileInvocation = Assert.Single(
      responseAssembler.DescendantNodes().OfType<InvocationExpressionSyntax>(),
      invocation => invocation.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "File");
    Assert.Equal(
      ["file.FileStream", "ExcelContentType", "file.FileName"],
      [.. fileInvocation.ArgumentList.Arguments.Select(argument => argument.Expression.ToString())]);

    foreach (string actionName in new[] { "ExportPlatformStatisticsAsync", "ExportBranchStatisticsAsync" })
    {
      MethodDeclarationSyntax action = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Single(declaration => declaration.Identifier.ValueText == actionName);
      Assert.Contains(
        action.DescendantNodes().OfType<InvocationExpressionSyntax>(),
        invocation => invocation.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "CreateExportResponse");
    }

    // 变异证据：把缓存头赋值目标改名后，同一条单处判定必须失败，证明判定按赋值目标锚定而不是恒真。
    string mutatedSource = root.ToString().Replace(
      "Response.Headers.CacheControl", "Response.Headers.Probe", StringComparison.Ordinal);
    CompilationUnitSyntax mutatedRoot = SourceSyntaxGuard.Parse(mutatedSource);
    MethodDeclarationSyntax mutatedAssembler = mutatedRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
      .Single(declaration => declaration.Identifier.ValueText == "CreateExportResponse");
    Assert.ThrowsAny<Exception>(() => Assert.Single(
      mutatedAssembler.DescendantNodes().OfType<AssignmentExpressionSyntax>(),
      assignment => assignment.Left.ToString() == "Response.Headers.CacheControl"));
  }

  /// <summary>
  /// 按方法名取导出控制器动作的反射信息，用于核对特性与绑定声明。
  /// </summary>
  /// <param name="actionName">动作方法名。</param>
  /// <returns>该动作的方法信息。</returns>
  private static MethodInfo ControllerAction(string actionName) =>
    typeof(Dy.MedicalRecognition.Controllers.RecognitionStatisticsExportController).GetMethod(actionName)!;

  /// <summary>
  /// 按特性类型简单名查找方法上声明的特性实例。
  /// </summary>
  /// <param name="method">待检查的方法。</param>
  /// <param name="attributeTypeName">特性类型的简单名。</param>
  /// <returns>首个命中的特性实例；未声明时为 <see langword="null"/>。</returns>
  private static Attribute? FindAttribute(MethodInfo method, string attributeTypeName) =>
    method.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().Name == attributeTypeName);
}
