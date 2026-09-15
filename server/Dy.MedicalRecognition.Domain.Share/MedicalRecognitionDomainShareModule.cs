using Dy.Apron.Abstractions.Core;

namespace Dy.MedicalRecognition.Domain.Share;

/// <summary>
/// 跨层共享领域类型的框架模块声明；使装配层能发现本工程的领域事件与共享枚举，只存放被其它模块稳定引用的领域事实与共享枚举。
/// </summary>
public sealed class MedicalRecognitionDomainShareModule : ApronModule
{
  /// <summary>
  /// 以本类型名称注册共享领域模块；本工程不放公共请求、读模型或传输对象。
  /// </summary>
  public MedicalRecognitionDomainShareModule() : base(nameof(MedicalRecognitionDomainShareModule))
  {
  }
}
