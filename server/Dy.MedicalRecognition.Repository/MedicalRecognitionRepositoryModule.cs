using Dy.Apron.Abstractions.Core;
using Dy.Earthrace.Abstractions.Annotations;
using Dy.Earthrace.TypeHandlers;

namespace Dy.MedicalRecognition.Repository;

/// <summary>
/// 标记本程序集为默认持久化模块；本程序集内嵌的映射文件与 IHasDataMapper 实现因此成为默认数据映射来源。
/// </summary>
/// <remarks>同时注册共享 SQL 通过名称引用的布尔参数绑定，使布尔列与参数的比较在当前数据库 Provider 上可用。</remarks>
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
  /// 注册共享 SQL 通过名称引用的布尔参数类型处理器。
  /// </summary>
  /// <remarks>
  /// 框架默认把布尔参数按整数绑定，布尔列与整数比较在 PostgreSQL 上不存在对应操作符，带布尔筛选的查询会失败；
  /// 类型处理器把绑定交给当前 Provider 适配层处理。
  /// </remarks>
  /// <param name="context">模块装配上下文；本模块不通过它注册额外服务。</param>
  public override void OnConfigureServices(ServiceConfigurationContext context)
  {
    TypeHandlerFactory.Register("MedicalRecognitionBoolean", new BooleanTypeHandler());
  }
}
