namespace Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

/// <summary>
/// 互认引用结果已记录事件中的一个实际引用事实：一个互认匹配项及其本次被引用的项目级业务数据。
/// </summary>
/// <remarks>
/// 实际引用时间、引用科室与引用医生都是项目级事实，同一次请求中的不同引用事实允许具有不同的时间与主体；
/// 本类型只承载已保存事实的载荷，不保存匹配记录、本次来源就诊与互认时间，那些由所属匹配项与匹配记录确定。
/// </remarks>
public sealed record RecognitionReferenceFact
{
  /// <summary>
  /// 本次形成引用事实的互认匹配项标识
  /// </summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>
  /// 该项目级的实际引用时间
  /// </summary>
  public DateTime ReferencedTime { get; init; }
  /// <summary>
  /// 引用科室在来源系统中的标识；使用字符串承载
  /// </summary>
  public string ReferenceDeptId { get; init; } = string.Empty;
  /// <summary>
  /// 引用科室名称；与引用科室标识成对保存
  /// </summary>
  public string ReferenceDeptName { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生在来源系统中的人员标识；使用字符串承载
  /// </summary>
  public string ReferenceDoctorId { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生名称；与引用医生标识成对保存
  /// </summary>
  public string ReferenceDoctorName { get; init; } = string.Empty;
}
