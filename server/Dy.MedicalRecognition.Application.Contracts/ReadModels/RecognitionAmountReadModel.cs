namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionAmountReadModel
{
  /// <summary>
  /// 组织编码
  /// </summary>
  public string OrganizationCode { get; set; }
  /// <summary>
  /// 医院编码
  /// </summary>
  public string HospitalCode { get; set; }
  /// <summary>
  /// 院区编码
  /// </summary>
  public string BranchCode { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 标准项目名称
  /// </summary>
  public string StandardProjectName { get; set; }
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
  /// 互认配置状态
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
  /// 当前金额
  /// </summary>
  public decimal CurrentAmount { get; set; }
  /// <summary>
  /// 金额已配置
  /// </summary>
  public bool IsAmountConfigured { get; set; }
  /// <summary>
  /// 最后修改时间
  /// </summary>
  public DateTime LastModifiedTime { get; set; }
  /// <summary>
  /// 最后修改人
  /// </summary>
  public string LastModifiedBy { get; set; }
}
