using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;

/// <summary>标准医疗项目分类列表项。</summary>
public sealed record MedicalStandardCategoryListReadModel
{
  /// <summary>分类ID。</summary>
  public Guid CategoryId { get; init; }
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按"未知类型"安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>分类名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>是否启用。</summary>
  public bool IsValid { get; init; }
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
  /// <summary>是否存在下级分组。</summary>
  public MedicalStandardUsageStatus UsageStatus { get; init; }
  /// <summary>
  /// 使用情况中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份使用情况文案。
  /// </summary>
  /// <remarks>
  /// 取值由"是否存在下级"派生、集合封闭，未登记取值按严格解析抛出（解析规则见 <c>EnumDescriptorText.Get</c>），不静默降级。
  /// 注意发布契约的形状差异：本属性是只读计算属性，ASP.NET 生成的 OpenAPI 会把它声明为可空
  /// （生成端为 <c>string | null</c>），与同平台的其它枚举中文文本字段同构；C# 侧仍是非空声明。
  /// </remarks>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string UsageStatusText => EnumDescriptorText.Get(UsageStatus, MedicalStandardUsageStatusDescriptorList.List);
}
