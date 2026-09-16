using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.Validation;

/// <summary>
/// 项目请求的校验入口（读请求与写请求共用）；必须显式调用，否则必填缺失或值域越界会透传到领域层，表现为业务拒绝或空引用。
/// </summary>
public static class MedicalRecognitionRequestValidator
{
    /// <summary>
    /// 按请求模型声明的 DataAnnotations 特性执行校验；这些特性只是声明，反序列化和参数绑定都不会自动执行它们。
    /// </summary>
    /// <param name="request">
    /// 待校验的请求模型实例（读请求与写请求同一入口，行为无差异）；应为本命名空间之外定义的 Request 类型，由调用方保证，不进行类型判定。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> 为 null 时抛出；空请求没有任何可校验的属性。
    /// </exception>
    /// <exception cref="ValidationException">
    /// 任一必填属性缺失或值域越界时抛出，异常消息汇总了违规属性名与原因。
    /// </exception>
    public static void Validate(object request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // DataAnnotations 仅在被显式触发时运行；validateAllProperties 保证未被访问过的属性同样纳入校验。
        ValidationContext validationContext = new(request);
        Validator.ValidateObject(request, validationContext, validateAllProperties: true);
    }
}
