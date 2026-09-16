using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 验证契约工程已成功生成并加载：标准目录写入口契约类型在运行时可解析，
/// 且该类型确实由契约程序集声明并具备预期的公开成员形状。
/// 该断言用于在构建阶段暴露契约工程未被引用、类型被搬到别的程序集或公开成员形状漂移的问题。
/// </summary>
public sealed class SmokeTests
{
  /// <summary>
  /// 断言 <see cref="IMedicalRecognitionReportAppService"/> 由契约程序集声明，
  /// 并冻结四个互认写入口的名称与返回类型。
  /// </summary>
  /// <remarks>
  /// 原判据是 <c>Assert.True(typeof(...) != null)</c>，恒为真而没有判别力：<see cref="Type"/> 实例永远不等于 <see langword="null"/>，
  /// 契约类型被删除、被搬到别的程序集或换成本地同名替身时该断言都不会失败。
  /// 这里改为三条可判别判据：类型来自契约程序集（不是测试程序集里的同名替身），
  /// 且四个互认写入口各自恰好一处声明、返回 <see cref="Task{TResult}"/> 且泛型实参为 <see cref="bool"/>，
  /// 使"契约程序集未被引用""类型被搬移""入口被删除或返回类型漂移"都能让本用例失败。
  /// </remarks>
  [Fact]
  public void Generated_contract_is_available()
  {
    Type contract = typeof(IMedicalRecognitionReportAppService);

    Assert.Equal("Dy.MedicalRecognition.Application.Contracts", contract.Assembly.GetName().Name);
    Assert.NotSame(typeof(SmokeTests).Assembly, contract.Assembly);

    (string Name, Type ReturnType)[] mutualWriteEntrypoints =
    [
      (nameof(IMedicalRecognitionReportAppService.CreateMutualRecognitionItemAsync), typeof(Task<bool>)),
      (nameof(IMedicalRecognitionReportAppService.UpdateMutualRecognitionItemConfigurationAsync), typeof(Task<bool>)),
      (nameof(IMedicalRecognitionReportAppService.EnableMutualRecognitionItemAsync), typeof(Task<bool>)),
      (nameof(IMedicalRecognitionReportAppService.DisableMutualRecognitionItemAsync), typeof(Task<bool>))
    ];

    foreach ((string name, Type returnType) in mutualWriteEntrypoints)
    {
      MethodInfo method = Assert.Single(contract.GetMethods(), candidate => candidate.Name == name);
      Assert.Equal(returnType, method.ReturnType);
    }
  }
}
