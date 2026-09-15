namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record RecognitionUsageSummaryReadModel
{
  /// <summary>
  /// 接收组织编码
  /// </summary>
  public string ReceiverOrganizationCode { get; set; }
  /// <summary>
  /// 接收组织名称
  /// </summary>
  public string ReceiverOrganizationName { get; set; }
  /// <summary>
  /// 接收医院编码
  /// </summary>
  public string ReceiverHospitalCode { get; set; }
  /// <summary>
  /// 接收医院名称
  /// </summary>
  public string ReceiverHospitalName { get; set; }
  /// <summary>
  /// 接收院区编码
  /// </summary>
  public string ReceiverBranchCode { get; set; }
  /// <summary>
  /// 接收院区名称
  /// </summary>
  public string ReceiverBranchName { get; set; }
  /// <summary>
  /// 互认科室ID
  /// </summary>
  public string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称
  /// </summary>
  public string RecognitionDeptName { get; set; }
  /// <summary>
  /// 项目类型
  /// </summary>
  public object ItemType { get; set; }
  /// <summary>
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 分类名称
  /// </summary>
  public string CategoryName { get; set; }
  /// <summary>
  /// 分组名称
  /// </summary>
  public string GroupName { get; set; }
  /// <summary>
  /// 统计开始时间
  /// </summary>
  public DateTime PeriodStart { get; set; }
  /// <summary>
  /// 统计结束时间
  /// </summary>
  public DateTime PeriodEnd { get; set; }
  /// <summary>
  /// 采纳次数
  /// </summary>
  public int AdoptionCount { get; set; }
  /// <summary>
  /// 不采纳次数
  /// </summary>
  public int NonAdoptionCount { get; set; }
  /// <summary>
  /// 引用次数
  /// </summary>
  public int ReferenceCount { get; set; }
  /// <summary>
  /// 提醒次数
  /// </summary>
  public int ReminderCount { get; set; }
  /// <summary>
  /// 同期互认率
  /// </summary>
  public decimal SamePeriodRecognitionRate { get; set; }
  /// <summary>
  /// 同期互认率已计算
  /// </summary>
  public bool SamePeriodRecognitionRateCalculated { get; set; }
  /// <summary>
  /// 不采纳原因汇总
  /// </summary>
  public object NonAdoptionReasons { get; set; }
  /// <summary>
  /// 预计节省金额
  /// </summary>
  public decimal EstimatedSavingAmount { get; set; }
}
