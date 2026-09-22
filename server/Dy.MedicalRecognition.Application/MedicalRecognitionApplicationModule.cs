using Dy.Apron.Abstractions.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Application;

/// <summary>
/// 承载本工程应用层的装配标识；用例编排位于应用服务，领域规则不在此实现。
/// </summary>
public sealed class MedicalRecognitionApplicationModule : ApronModule
{
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

    // 公共枚举的取值集合只能由服务端声明：缺少时生成端会把可空枚举写成空对象。
    context.Services.ConfigureAll<OpenApiOptions>(options =>
      options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
  }
}
