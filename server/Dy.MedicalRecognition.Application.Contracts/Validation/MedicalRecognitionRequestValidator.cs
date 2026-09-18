using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Dy.MedicalRecognition.Application.Contracts.Validation;

/// <summary>
/// 项目请求的校验入口（读请求与写请求共用）；必须显式调用，否则必填缺失或值域越界会透传到领域层，表现为业务拒绝或空引用。
/// </summary>
public static class MedicalRecognitionRequestValidator
{
    /// <summary>
    /// 按请求模型声明的 DataAnnotations 特性执行校验；这些特性只是声明，反序列化和参数绑定都不会自动执行它们。
    /// </summary>
    /// <remarks>
    /// 校验覆盖请求自身与它内嵌的复杂类型属性（含集合元素），因为 <c>Validator.ValidateObject</c> 只校验当前实例，
    /// 内嵌类型上声明的必填与值域特性不会自动生效。嵌套层级按属性图递归，同一实例只校验一次。
    /// </remarks>
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
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        ValidateRecursively(request, visited);
    }

    /// <summary>
    /// 校验当前实例，并递归校验它内嵌的复杂类型属性与集合元素。
    /// </summary>
    /// <param name="instance">待校验的实例。</param>
    /// <param name="visited">已校验过的实例，用于避免循环引用导致重复校验或无限递归。</param>
    /// <exception cref="ValidationException">当前实例或其嵌套实例违反声明时抛出。</exception>
    private static void ValidateRecursively(object instance, HashSet<object> visited)
    {
        if (!visited.Add(instance)) return;

        Validator.ValidateObject(instance, new ValidationContext(instance), validateAllProperties: true);

        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;

            object? value = property.GetValue(instance);
            if (value is null) continue;

            if (value is string) continue;

            if (value is System.Collections.IEnumerable items)
            {
                foreach (object? item in items)
                {
                    if (item is null || item is string) continue;
                    if (IsSimple(item.GetType())) continue;
                    ValidateRecursively(item, visited);
                }

                continue;
            }

            if (IsSimple(value.GetType())) continue;
            ValidateRecursively(value, visited);
        }
    }

    /// <summary>
    /// 判断类型是否由框架自身完成校验，不需要递归进入。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>基元类型、字符串、枚举、日期时间与 Guid 等简单类型返回 <see langword="true"/>。</returns>
    private static bool IsSimple(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
          || actual.IsEnum
          || actual == typeof(string)
          || actual == typeof(decimal)
          || actual == typeof(DateTime)
          || actual == typeof(DateTimeOffset)
          || actual == typeof(TimeSpan)
          || actual == typeof(Guid)
          || actual == typeof(DateOnly)
          || actual == typeof(TimeOnly);
    }
}
