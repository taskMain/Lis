using Dy.Apron.Abstractions.Core;
using Dy.Earthrace.Abstractions.Annotations;

namespace Dy.MedicalRecognition.Repository;

/// <summary>
/// 标记本程序集为默认持久化模块；本程序集内嵌的映射文件与 IHasDataMapper 实现因此成为默认数据映射来源。
/// </summary>
[Earthrace(IsDefault = true)]
public sealed class MedicalRecognitionRepositoryModule : ApronModule
{
  /// <summary>
  /// 以类型名作为模块名称创建模块实例，模块名称用于装配时识别该持久化模块。
  /// </summary>
  public MedicalRecognitionRepositoryModule() : base(nameof(MedicalRecognitionRepositoryModule))
  {
  }
}
