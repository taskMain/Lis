using Dy.Apron.Abstractions.Core;
using Dy.Core.Abstractions.Modularity;
using Dy.IAM.Apron;
using Dy.IAM.Apron.OpenApi;
using Dy.IAM.Application.Contracts.OrganizationAggregate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Application;

/// <summary>
/// 承载本工程应用层的装配标识；用例编排位于应用服务，领域规则不在此实现。
/// </summary>
/// <remarks>
/// 认证由 IAM 宿主接入模块提供：本模块声明依赖它以在宿主启动时装载 IAM 认证、错误处理与工作单元适配，
/// 工程内不再注册旧的共享密钥认证方案。
/// </remarks>
[DependsOn(typeof(IamApronOpenApiModule), typeof(IamManagementClientModule))]
public sealed class MedicalRecognitionApplicationModule : ApronModule
{
  /// <summary>
  /// 跨域策略名称：配置存在时按该名称注册并挂载，配置缺失时两者都不发生。
  /// </summary>
  private const string CorsPolicyName = "MedicalRecognitionHostApp";

  /// <summary>
  /// 以本类型名称注册应用模块，使框架按模块名识别本工程的应用层。
  /// </summary>
  public MedicalRecognitionApplicationModule() : base(nameof(MedicalRecognitionApplicationModule))
  {
  }

  /// <inheritdoc/>
  public override void OnPostConfigureServices(ServiceConfigurationContext context)
  {
    // 手工导出控制器按具体类注入查询应用服务（本院导出入口未声明在接口上），
    // 框架扫描只注册接口形态，这里补齐具体类注册使控制器可激活。
    context.Services.AddScoped<Queries.MedicalRecognitionReportQueryAppService>();

    // 平台参数维护由手工控制器独占路由：接口不继承自动端点标记，显式注册供控制器按接口注入。
    context.Services.AddScoped<Contracts.PlatformParameter.IPlatformParameterMaintenanceAppService, PlatformParameter.PlatformParameterMaintenanceAppService>();
    // 参数维护控制器按具体类注入应用服务；框架扫描只注册接口形态，这里补齐具体类注册使控制器可激活。
    context.Services.AddScoped<PlatformParameter.PlatformParameterMaintenanceAppService>();

    // 公共枚举的取值集合只能由服务端声明：缺少时生成端会把可空枚举写成空对象。
    context.Services.ConfigureAll<OpenApiOptions>(options =>
      options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
  }

  /// <summary>
  /// 注册跨域服务：配置了 <c>Cors:Origins</c> 时按具名策略注册，未配置时不注册。
  /// </summary>
  /// <param name="context">模块装配上下文，提供配置与服务集合。</param>
  public override void OnConfigureServices(ServiceConfigurationContext context)
  {
    // 组织树与组织资料读取使用 IAM 管理接口客户端：每次调用透传当前已验证的访问令牌，不另配客户端凭据。
    context.Services.AddIamManagementClient<IOrganizationAppService>();
    // 组织业务编码读取点复用同一管理接口客户端：已发布契约不声明 code，本工程按自有契约读取同一接口。
    context.Services.AddIamManagementClient<OrganizationScope.IOrganizationBusinessCodeQueryClient>();

    string[]? origins = context.Configuration.GetSection("Cors:Origins").Get<string[]>();
    if (origins is null)
    {
      return;
    }

    context.Services.AddCors(options => options.AddPolicy(
      CorsPolicyName,
      policy => policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader()));
  }

  /// <summary>
  /// 在认证与授权之前挂载跨域中间件，保证匿名预检（OPTIONS）不被 401 拦截。
  /// </summary>
  /// <param name="context">应用初始化前上下文，提供应用构建器。</param>
  public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
  {
    string[]? origins = context.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Cors:Origins").Get<string[]>();
    if (origins is null)
    {
      return;
    }

    object appBuilder = context.ServiceProvider.GetRequiredService<AdvancedContext>().AppBuilder
      ?? throw new InvalidOperationException("应用构建器不可用，无法挂载跨域中间件。");
    IApplicationBuilder applicationBuilder = (IApplicationBuilder)appBuilder;
    applicationBuilder.UseCors(CorsPolicyName);
  }
}