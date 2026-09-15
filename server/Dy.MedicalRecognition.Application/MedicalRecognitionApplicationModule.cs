using Dy.Apron.Abstractions.Core;

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
}
