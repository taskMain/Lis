using System.Reflection;
using Dy.Core.Extensions.Models;
using Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 1 只读模型的枚举中文（总体设计 5.3「枚举协作契约」、S1-D32）：
/// 分类、分组、标准项目与有效目录读模型必须以**服务端枚举声明**交付项目类型与使用情况中文，
/// 页面直接展示，不再在前端本地派生一份中文。
/// </summary>
/// <remarks>
/// 本文件以反射读取属性：属性尚不存在时按断言失败报告，而不是编译错误，
/// 使该子面在实现前先有可复现的 RED。全部为静态证据，不依赖数据库与宿主。
/// </remarks>
public sealed class Stage1ReadModelEnumTextTests
{
  /// <summary>项目类型中文按枚举声明解析；三个承载项目类型的读模型都必须交付该字段。</summary>
  /// <param name="itemType">项目类型取值。</param>
  /// <param name="expected">期望的中文。</param>
  [Theory]
  [InlineData(MedicalItemType.Laboratory, "检验")]
  [InlineData(MedicalItemType.Examination, "检查")]
  public void Category_item_and_catalog_type_read_models_expose_item_type_text(MedicalItemType itemType, string expected)
  {
    Assert.Equal(expected, TextOf(new MedicalStandardCategoryListReadModel { ItemType = itemType }, "ItemTypeText"));
    Assert.Equal(expected, TextOf(new MedicalStandardItemListReadModel { ItemType = itemType }, "ItemTypeText"));
    Assert.Equal(expected, TextOf(new EffectiveMedicalStandardCatalogTypeReadModel { ItemType = itemType }, "ItemTypeText"));
  }

  /// <summary>使用情况中文按枚举声明解析；分类与分组读模型都必须交付该字段。</summary>
  /// <param name="usageStatus">使用情况取值。</param>
  /// <param name="expected">期望的中文。</param>
  [Theory]
  [InlineData(MedicalStandardUsageStatus.Unused, "未使用")]
  [InlineData(MedicalStandardUsageStatus.InUse, "已使用")]
  public void Category_and_group_read_models_expose_usage_status_text(MedicalStandardUsageStatus usageStatus, string expected)
  {
    Assert.Equal(expected, TextOf(new MedicalStandardCategoryListReadModel { UsageStatus = usageStatus }, "UsageStatusText"));
    Assert.Equal(expected, TextOf(new MedicalStandardGroupListReadModel { UsageStatus = usageStatus }, "UsageStatusText"));
  }

  /// <summary>
  /// 两个中文的声明口径：项目类型取自无存储约束的列，未登记取值必须安全降级为 <see langword="null"/>；
  /// 使用情况取值集合封闭，未登记取值必须抛出而不是静默降级成"未知"。
  /// </summary>
  [Fact]
  public void Enum_text_nullability_follows_the_value_domain()
  {
    // 未登记的项目类型：降级为 null，由页面按"未知类型"安全展示。
    Assert.Null(TextOf(new MedicalStandardCategoryListReadModel { ItemType = (MedicalItemType)99 }, "ItemTypeText"));

    // 未登记的使用情况：取值不可由正常业务路径产生（由"是否存在下级"派生、集合封闭），
    // 这里只用于验证防御行为——损坏的调用链不得被静默降级。
    PropertyInfo usageStatusText = PropertyOf(new MedicalStandardCategoryListReadModel(), "UsageStatusText");
    TargetInvocationException error = Assert.Throws<TargetInvocationException>(
      () => usageStatusText.GetValue(new MedicalStandardCategoryListReadModel { UsageStatus = (MedicalStandardUsageStatus)99 }));
    Assert.IsType<ExtensionException>(error.InnerException);

    // 两个中文都是只读计算属性：不可被赋值（没有 setter），页面只能展示服务端声明。
    Assert.Null(PropertyOf(new MedicalStandardCategoryListReadModel(), "ItemTypeText").SetMethod);
    Assert.Null(usageStatusText.SetMethod);
  }

  /// <summary>
  /// 属性的**声明**可空性：项目类型中文允许缺失，使用情况中文由服务端保证存在。
  /// 运行时取值断言（见上一个用例）对 <c>string</c> 与 <c>string?</c> 同样成立，无法锁定声明，
  /// 因此这里用 <see cref="NullabilityInfoContext"/> 按可空性反射标注核对，与阶段 2 读模型的同一判据一致。
  /// </summary>
  [Fact]
  public void Enum_text_properties_declare_the_expected_nullability()
  {
    Assert.Equal(NullabilityState.Nullable, ReadNullability(typeof(MedicalStandardCategoryListReadModel), "ItemTypeText"));
    Assert.Equal(NullabilityState.NotNull, ReadNullability(typeof(MedicalStandardCategoryListReadModel), "UsageStatusText"));
    Assert.Equal(NullabilityState.NotNull, ReadNullability(typeof(MedicalStandardGroupListReadModel), "UsageStatusText"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(typeof(MedicalStandardItemListReadModel), "ItemTypeText"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(typeof(EffectiveMedicalStandardCatalogTypeReadModel), "ItemTypeText"));
  }

  /// <summary>取出读模型上的字符串属性值；属性不存在时立即判红。</summary>
  /// <param name="model">读模型实例。</param>
  /// <param name="propertyName">属性名。</param>
  /// <returns>属性值。</returns>
  private static string? TextOf(object model, string propertyName) =>
    (string?)PropertyOf(model, propertyName).GetValue(model);

  /// <summary>按名取出读模型属性；不存在时以断言失败报告。</summary>
  /// <param name="model">读模型实例。</param>
  /// <param name="propertyName">属性名。</param>
  /// <returns>属性信息。</returns>
  private static PropertyInfo PropertyOf(object model, string propertyName)
  {
    PropertyInfo? property = model.GetType().GetProperty(propertyName);
    Assert.NotNull(property);
    return property!;
  }

  /// <summary>读取属性的可空性标注，用于核对契约声明的可空字段。</summary>
  /// <param name="type">属性所属的公共契约类型。</param>
  /// <param name="propertyName">属性名；必须是该类型自身声明的公开属性。</param>
  /// <returns>该属性读取方向的可空性标注。</returns>
  private static NullabilityState ReadNullability(Type type, string propertyName)
  {
    PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
    return new NullabilityInfoContext().Create(property).ReadState;
  }
}
