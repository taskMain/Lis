using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 统计读模型承载的互认项目：项目类型取值与中文文本、标准目录的分类、分组与标准项目资料。
/// </summary>
public sealed record RecognitionStatisticsItemReadModel
{
  /// <summary>项目类型。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按「未知类型」安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>标准目录分类名称。</summary>
  public string CategoryName { get; init; } = string.Empty;
  /// <summary>标准目录分组名称。</summary>
  public string GroupName { get; init; } = string.Empty;
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称；取自标准目录当前版本。</summary>
  public string StandardProjectName { get; init; } = string.Empty;
}
