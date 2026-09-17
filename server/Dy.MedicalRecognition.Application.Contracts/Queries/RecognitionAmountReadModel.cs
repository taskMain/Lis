using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 互认项目金额列表项；两个金额查询入口共用同一返回契约。
/// </summary>
/// <remarks>
/// 标准项目资料与三层名称按当前目录与外部组织服务实时关联，不在金额记录中重复保存；
/// 不返回组织编码、医院编码与院区编码，也不返回最后修改时间与最后修改人；
/// 目录三层启用状态只以派生后的 <see cref="UnavailableReason"/> 返回，配置自身状态只由 <see cref="ConfigurationStatus"/> 表达；
/// 项目类型与配置状态的中文由服务端按枚举声明解析后随契约返回（<see cref="ItemTypeText"/>、<see cref="ConfigurationStatusText"/>），前端不重写一份文案。
/// </remarks>
public sealed record RecognitionAmountReadModel
{
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string StandardProjectName { get; init; } = string.Empty;
  /// <summary>项目类型；取自其所属分类。</summary>
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
  /// <summary>组织名称；取外部组织服务的当前值。</summary>
  public string OrganizationName { get; init; } = string.Empty;
  /// <summary>医院名称；取外部组织服务的当前值。</summary>
  public string HospitalName { get; init; } = string.Empty;
  /// <summary>院区名称；取外部组织服务的当前值。</summary>
  public string BranchName { get; init; } = string.Empty;
  /// <summary>互认配置状态；只表达配置自身的启用或停用，与标准目录的启用状态无关。</summary>
  public ConfigurationStatus ConfigurationStatus { get; init; }
  /// <summary>
  /// 配置状态中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份状态文案。
  /// </summary>
  /// <remarks>
  /// 取值由 is_valid 布尔派生、集合封闭，未登记取值按严格解析抛出（解析规则与理由见 <c>EnumDescriptorText.Get</c>），不静默降级。
  /// 注意发布契约的形状差异：本属性是只读计算属性，ASP.NET 生成的 OpenAPI 会把它声明为可空
  /// （生成端为 <c>string | null</c>），与阶段 2 的 `ConfigurationStatusText` 同构；C# 侧仍是非空声明。
  /// </remarks>
  /// <exception cref="Dy.Core.Extensions.Models.ExtensionException">取值不在枚举描述列表中时由序列化期间抛出。</exception>
  public string ConfigurationStatusText => EnumDescriptorText.Get(ConfigurationStatus, ConfigurationStatusDescriptorList.List);
  /// <summary>当前不可用原因；只表达所属分类、分组或标准项目停用，目录三层全部启用时为 null，配置自身停用不占用该字段。</summary>
  public string? UnavailableReason { get; init; }
  /// <summary>当前金额；未配置金额时为 null，不以 0 冒充未配置，零元是有效配置。</summary>
  public decimal? CurrentAmount { get; init; }
  /// <summary>金额已配置；零元同样为真，用于把已配置的零元与从未配置区分开。</summary>
  public bool IsAmountConfigured { get; init; }
}
