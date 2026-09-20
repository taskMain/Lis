namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 提交引用结果命令中的一个实际引用事实：互认匹配项标识，以及该项目被写入病历的时间与执行引用的科室和医生。
/// </summary>
/// <remarks>
/// 实际引用时间、引用科室与引用医生都是项目级事实，同一次请求中的不同项目允许各不相同，因此不在命令上提供组级公共取值；
/// 匹配记录标识、本次来源就诊与互认时间都由平台按该匹配项已保存的事实反查取得，调用方不重复提交；
/// 引用科室与引用医生的标识和名称必须成对提供，名称只保存、不参与幂等一致性判断。
/// </remarks>
public sealed record SubmitRecognitionReferenceCommandItem
{
  /// <summary>
  /// 被引用的互认匹配项标识；同时是定位所属匹配记录、已保存处理结果与本次幂等比较的业务键。
  /// </summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>
  /// 该项目实际被写入病历的时间；不得早于所属处理结果的互认时间、不得晚于本次请求接收时间，两端含边界。
  /// </summary>
  public DateTime ReferencedTime { get; init; }
  /// <summary>
  /// 引用科室在来源系统中的标识；按请求直接保存，不向权限系统补查。
  /// </summary>
  public string ReferenceDeptId { get; init; } = string.Empty;
  /// <summary>
  /// 引用科室名称；与引用科室标识成对保存，只保存不参与幂等一致性判断。
  /// </summary>
  public string ReferenceDeptName { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生在来源系统中的人员标识；按请求直接保存，不以调用账号替代。
  /// </summary>
  public string ReferenceDoctorId { get; init; } = string.Empty;
  /// <summary>
  /// 引用医生名称；与引用医生标识成对保存，只保存不参与幂等一致性判断。
  /// </summary>
  public string ReferenceDoctorName { get; init; } = string.Empty;
}
