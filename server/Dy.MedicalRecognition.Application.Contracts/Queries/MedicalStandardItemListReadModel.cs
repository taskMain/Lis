using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>标准医疗项目列表项。</summary>
public sealed record MedicalStandardItemListReadModel
{
  /// <summary>标准项目ID。</summary>
  public Guid ItemId { get; init; }
  /// <summary>所属分类ID。</summary>
  public Guid CategoryId { get; init; }
  /// <summary>所属分组ID。</summary>
  public Guid GroupId { get; init; }
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按"未知类型"安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>标准项目编码。</summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>是否启用。</summary>
  public bool IsValid { get; init; }
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
}
