using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 验证契约工程已成功生成并加载：标准目录写入口契约类型在运行时可解析。
/// 该断言用于在构建阶段暴露契约工程未被引用或生成失败的问题。
/// </summary>
public sealed class SmokeTests
{
  /// <summary>
  /// 断言 <see cref="Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.IMedicalRecognitionReportAppService"/>
  /// 在测试进程中可解析，即契约程序集已随测试工程加载。
  /// </summary>
  [Fact]
  public void Generated_contract_is_available()
  {
    Assert.True(typeof(Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.IMedicalRecognitionReportAppService) != null);
  }
}
