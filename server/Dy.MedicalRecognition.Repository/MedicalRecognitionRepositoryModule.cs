using Dy.Apron.Abstractions.Core;
using Dy.Earthrace.Abstractions.Annotations;
using Dy.Earthrace.TypeHandlers;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository;

/// <summary>
/// 标记本程序集为默认持久化模块；本程序集内嵌的映射文件与 IHasDataMapper 实现因此成为默认数据映射来源。
/// </summary>
/// <remarks>
/// 同时注册共享 SQL 通过名称引用的布尔参数绑定，使布尔列与参数的比较在当前数据库 Provider 上可用；
/// 并注册报告 PDF 本地文件存储与其配置，使配置项与实现同处持久化模块。
/// </remarks>
[Earthrace(IsDefault = true)]
public sealed class MedicalRecognitionRepositoryModule : ApronModule
{
  /// <summary>
  /// 以类型名作为模块名称创建模块实例，模块名称用于装配时识别该持久化模块。
  /// </summary>
  public MedicalRecognitionRepositoryModule() : base(nameof(MedicalRecognitionRepositoryModule))
  {
  }

  /// <summary>
  /// 注册共享 SQL 通过名称引用的布尔参数类型处理器，以及报告 PDF 本地文件存储与其配置。
  /// </summary>
  /// <remarks>
  /// 框架默认把布尔参数按整数绑定，布尔列与整数比较在 PostgreSQL 上不存在对应操作符，带布尔筛选的查询会失败；
  /// 类型处理器把绑定交给当前 Provider 适配层处理。
  /// 报告 PDF 存储配置按配置节绑定后在启动时校验一次：根目录为空、不是绝对路径、目录不存在或不可写，
  /// 以及单文件上限不大于零时直接失败，不隐式回落到应用基目录。
  /// </remarks>
  /// <param name="context">模块装配上下文，提供配置与服务集合。</param>
  public override void OnConfigureServices(ServiceConfigurationContext context)
  {
    TypeHandlerFactory.Register("MedicalRecognitionBoolean", new BooleanTypeHandler());

    context.Services.AddOptions<ReportPdfFileOptions>()
      .Bind(context.Configuration.GetSection(ReportPdfFileOptions.SectionName))
      .Validate(
        options =>
        {
          options.ResolveRootDirectoryOrThrow();
          return true;
        },
        "报告 PDF 存储配置非法。")
      .ValidateOnStart();
    // 存储实现按框架选项注入方式取值：先解析已绑定的选项，再把它交给本地文件存储实现。
    context.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReportPdfFileOptions>>().Value);
    context.Services.AddSingleton<IReportPdfFileStore, LocalReportPdfFileStore>();
  }
}
