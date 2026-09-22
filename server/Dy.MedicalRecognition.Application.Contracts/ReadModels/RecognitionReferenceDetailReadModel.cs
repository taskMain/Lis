namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 明细与集合视图承载的引用事实：引用状态、实际引用时间与引用科室、医生。
/// </summary>
public sealed record RecognitionReferenceDetailReadModel
{
  /// <summary>是否已被引用写入病历。</summary>
  public bool IsReferenced { get; init; }
  /// <summary>引用事实的实际引用时间；引用次数按该时间统计，未引用时无值。</summary>
  public DateTime? ReferenceTime { get; init; }
  /// <summary>引用科室ID；取引用事实自身保存的医院业务科室标识。</summary>
  public string? ReferenceDeptId { get; init; }
  /// <summary>引用科室名称；取引用事实自身保存的名称。</summary>
  public string? ReferenceDeptName { get; init; }
  /// <summary>引用医生ID；取引用事实自身保存的医院业务人员标识。</summary>
  public string? ReferenceDoctorId { get; init; }
  /// <summary>引用医生名称；取引用事实自身保存的名称。</summary>
  public string? ReferenceDoctorName { get; init; }
}
