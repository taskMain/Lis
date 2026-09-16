using Dy.Core.Extensions.Models;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

/// <summary>
/// 为查询契约提供基于 SourceGen 描述列表的强类型枚举文本解析。
/// </summary>
/// <remarks>
/// 枚举中文的唯一来源是服务端枚举成员上的 <c>[Description]</c>，本类只做解析，不保存文案。
/// 口径与同组织 <c>Dy.LisCenter</c> 一致：展示文本由服务端契约交付，生成端不产出 TS 枚举文案。
/// </remarks>
internal static class EnumDescriptorText
{
  /// <summary>
  /// 从指定枚举的静态描述列表解析文本，未定义值时抛出 <see cref="ExtensionException"/>。
  /// </summary>
  /// <remarks>
  /// 适用于取值集合封闭的枚举：<c>ConfigurationStatus</c> 由 <c>mrec_mutual_recognition_item.is_valid</c> 布尔派生，
  /// 只有启用与停用两种取值，出现其它取值说明调用链已损坏，按异常暴露比静默降级更安全。
  /// 前提是传入的必须是该枚举自己的描述列表（生成器每个成员只产出一条描述、取值唯一）；
  /// 传错列表时全部元素都不匹配，会按"未定义取值"抛出而不是返回错误文案。
  /// </remarks>
  /// <typeparam name="TEnum">待解析的枚举类型。</typeparam>
  /// <param name="value">枚举值。</param>
  /// <param name="descriptors">SourceGen 生成的枚举描述列表，必须是该枚举自己的列表。</param>
  /// <returns>枚举值对应的中文说明。</returns>
  /// <exception cref="ExtensionException">
  /// 枚举值不在静态描述列表中时抛出；传入其它枚举的描述列表同样会因元素类型不匹配落到该分支。
  /// </exception>
  internal static string Get<TEnum>(TEnum value, IEnumerable<IEnumDescriptor> descriptors)
    where TEnum : struct, Enum
  {
    IEnumDescriptor? descriptor = descriptors.FirstOrDefault(item =>
      item.EnumValue is TEnum enumValue &&
      EqualityComparer<TEnum>.Default.Equals(enumValue, value));

    return descriptor?.Description
      ?? throw new ExtensionException($"枚举 {typeof(TEnum).Name} 未找到取值 {value} 的描述；请确认传入的是该枚举自己的描述列表。");
  }

  /// <summary>
  /// 从指定枚举的静态描述列表安全解析文本，未定义值返回 <see langword="null"/>。
  /// </summary>
  /// <remarks>
  /// 适用于取值不受存储约束的枚举：<c>MedicalItemType</c> 取自 <c>mrec_medical_standard_category.item_type</c>，
  /// 该列没有 <c>CHECK</c> 约束，越界取值只影响单行展示。此时按异常抛出会让整张列表查询失败，
  /// 与页面既有约定"未知取值安全展示、不默认成已知类型"冲突，故降级为 <see langword="null"/> 由前端兜底展示。
  /// <see cref="Get{TEnum}(TEnum, IEnumerable{IEnumDescriptor})"/> 的前置条件同样适用；在该前提下本方法只把
  /// "取值未登记"降级为 <see langword="null"/>，不区分"取值未登记"与"列表传错"。
  /// </remarks>
  /// <typeparam name="TEnum">待解析的枚举类型。</typeparam>
  /// <param name="value">枚举值。</param>
  /// <param name="descriptors">SourceGen 生成的枚举描述列表，必须是该枚举自己的列表。</param>
  /// <returns>枚举值对应的中文说明；取值未登记时返回 <see langword="null"/>。</returns>
  internal static string? GetOrNull<TEnum>(TEnum value, IEnumerable<IEnumDescriptor> descriptors)
    where TEnum : struct, Enum
  {
    IEnumDescriptor? descriptor = descriptors.FirstOrDefault(item =>
      item.EnumValue is TEnum enumValue &&
      EqualityComparer<TEnum>.Default.Equals(enumValue, value));

    return descriptor?.Description;
  }
}
