namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准项目在当前标准目录中的归属与三层启用状态，供按编码集合批量判定互认项目配置能否建立。
/// </summary>
/// <remarks>
/// 一个标准项目是否属于当前有效目录由它自身与其所属分类、所属分组三层共同决定，任一层停用即不可用，
/// 因此本投影同时携带三层的启用状态，使一次读取即可完成逐编码判定，不需要按编码逐条读取分类与分组。
/// 编码集合内不存在或上级目录资料缺失的标准项目不会出现在读取结果中，由领域层按请求编码与返回编码的差集判定。
/// 本类型是仓储读取的内部投影，不作为对外返回的数据。
/// </remarks>
public sealed record MedicalStandardItemCatalogState
{
  /// <summary>
  /// 标准项目编码；与请求提交的编码逐项对应，是差集判定的依据。
  /// </summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>
  /// 标准项目标识；互认项目配置保存该标识，不保存编码之外的其他目录资料。
  /// </summary>
  public Guid StandardItemId { get; init; }
  /// <summary>
  /// 所属分类标识。
  /// </summary>
  public Guid CategoryId { get; init; }
  /// <summary>
  /// 所属分组标识。
  /// </summary>
  public Guid GroupId { get; init; }
  /// <summary>
  /// 标准项目自身的启用状态；分类与分组都启用而该值为 false 时，该编码不可新建互认项目配置。
  /// </summary>
  public bool ItemIsValid { get; init; }
  /// <summary>
  /// 所属分类的启用状态；为 false 时该编码不可新建互认项目配置，且原因优先于分组与项目两层。
  /// </summary>
  public bool CategoryIsValid { get; init; }
  /// <summary>
  /// 所属分组的启用状态；分类启用而该值为 false 时，该编码不可新建互认项目配置。
  /// </summary>
  public bool GroupIsValid { get; init; }
}
