using System.Collections.ObjectModel;

namespace Dy.MedicalRecognition.Application.Queries.EnumMetadata;

/// <summary>
/// 集中保存对外枚举的 SourceGen 描述器，供 OpenAPI 契约转换器与枚举元数据查询共同使用。
/// </summary>
/// <remarks>
/// 新增对外枚举（出现在请求或只读模型上的枚举）时必须在此登记，否则 OpenAPI 只会声明 <c>integer</c>、
/// 生成端会把可空枚举退化为空对象、线上形状变成 <c>{}</c>，而且枚举元数据查询也查不到该枚举的中文说明。
/// 中文说明的唯一来源是枚举成员上的 <c>[Description]</c>，注册表只登记、不重写文案。
/// 覆盖范围由回归用例守卫：出现既未登记、也不属于已确认排除清单的对外枚举时，相关用例会失败。
/// </remarks>
internal static class MedicalRecognitionEnumDescriptorRegistry
{
  /// <summary>
  /// 获取按对外枚举名称索引的只读描述器集合。
  /// </summary>
  internal static IReadOnlyDictionary<string, IReadOnlyList<IEnumDescriptor>> Descriptors { get; } =
    new ReadOnlyDictionary<string, IReadOnlyList<IEnumDescriptor>>(
      new Dictionary<string, IReadOnlyList<IEnumDescriptor>>(StringComparer.Ordinal)
      {
        [nameof(ConfigurationStatus)] = ToReadOnly(ConfigurationStatusDescriptorList.List),
        [nameof(MedicalItemType)] = ToReadOnly(MedicalItemTypeDescriptorList.List),
      });

  /// <summary>
  /// 复制生成器输出，避免外部代码改变跨请求共享的描述器顺序。
  /// </summary>
  /// <param name="descriptors">SourceGen 生成的枚举描述器。</param>
  /// <returns>保持生成顺序的只读描述器集合。</returns>
  private static IReadOnlyList<IEnumDescriptor> ToReadOnly(IEnumerable<IEnumDescriptor> descriptors) =>
    Array.AsReadOnly(descriptors.ToArray());
}
