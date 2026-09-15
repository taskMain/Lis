namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionProjectConfigurationReadModel
{
  /// <summary>
  /// 互认配置ID
  /// </summary>
  public Guid ConfigurationId { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 标准项目名称
  /// </summary>
  public string StandardItemName { get; set; }
  /// <summary>
  /// 项目类型
  /// </summary>
  public object ItemType { get; set; }
  /// <summary>
  /// 分类名称
  /// </summary>
  public string CategoryName { get; set; }
  /// <summary>
  /// 分组名称
  /// </summary>
  public string GroupName { get; set; }
  /// <summary>
  /// 可互认时间天数
  /// </summary>
  public int RecognitionDurationDays { get; set; }
  /// <summary>
  /// 配置状态
  /// </summary>
  public object ConfigurationStatus { get; set; }
  /// <summary>
  /// 标准目录当前有效
  /// </summary>
  public bool IsStandardCatalogValid { get; set; }
  /// <summary>
  /// 当前可用于新匹配
  /// </summary>
  public bool IsAvailableForNewMatch { get; set; }
  /// <summary>
  /// 当前不可用原因
  /// </summary>
  public string UnavailableReason { get; set; }
  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreationTime { get; set; }
  /// <summary>
  /// 创建人
  /// </summary>
  public string Creator { get; set; }
  /// <summary>
  /// 最后修改时间
  /// </summary>
  public DateTime LastModifiedTime { get; set; }
  /// <summary>
  /// 最后修改人
  /// </summary>
  public string LastModifier { get; set; }
}
