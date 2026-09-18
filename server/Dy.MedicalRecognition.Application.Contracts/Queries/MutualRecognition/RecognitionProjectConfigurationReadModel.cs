using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;

/// <summary>互认项目配置列表项；标准项目资料按当前标准目录实时关联，不在互认配置中重复保存。</summary>
public sealed record RecognitionProjectConfigurationReadModel
{
  /// <summary>互认配置ID。</summary>
  public Guid ConfigurationId { get; init; }
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string StandardItemName { get; init; } = string.Empty;
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按"未知类型"安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>分类名称。</summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>分组名称。</summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>可互认时间天数；从报告时间起按连续24小时计算。</summary>
  public int RecognitionDurationDays { get; init; }
  /// <summary>配置状态；只表达配置自身的启用或停用，与标准目录的启用状态无关。</summary>
  public ConfigurationStatus ConfigurationStatus { get; init; }
  /// <summary>
  /// 配置状态中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份状态文案。
  /// </summary>
  /// <remarks>
  /// 取值由 is_valid 布尔派生、集合封闭，未登记取值按严格解析抛出（解析规则与理由见 <c>EnumDescriptorText.Get</c>），不静默降级。
  /// 注意发布契约的形状差异：本属性是只读计算属性，ASP.NET 生成的 OpenAPI 会把它声明为可空
  /// （生成端为 <c>string | null</c>），与 LisCenter 同构；C# 侧仍是非空声明。
  /// </remarks>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ConfigurationStatusText => EnumDescriptorText.Get(ConfigurationStatus, ConfigurationStatusDescriptorList.List);
  /// <summary>当前不可用原因；只表达所属分类、分组或标准项目停用，目录三层全部启用时为 null，配置自身停用不占用该字段。</summary>
  public string? UnavailableReason { get; init; }
}
