using Dy.Apron.Abstractions.Core;

namespace Dy.MedicalRecognition.Domain;

/// <summary>
/// 标准项目目录与互认业务领域层的框架模块声明；只承担装配标识，不承载业务规则。
/// </summary>
public sealed class MedicalRecognitionDomainModule : ApronModule
{
  /// <summary>
  /// 以本类型名称注册领域模块，使框架按模块名识别本工程的领域层。
  /// </summary>
  public MedicalRecognitionDomainModule() : base(nameof(MedicalRecognitionDomainModule))
  {
  }
}
