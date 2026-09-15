namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

public sealed record SourceRecognitionDetailReadModel
{
  /// <summary>
  /// 匹配记录ID
  /// </summary>
  public Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 匹配项ID
  /// </summary>
  public Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 来源组织编码
  /// </summary>
  public string SourceOrganizationCode { get; set; }
  /// <summary>
  /// 来源组织名称
  /// </summary>
  public string SourceOrganizationName { get; set; }
  /// <summary>
  /// 来源医院编码
  /// </summary>
  public string SourceHospitalCode { get; set; }
  /// <summary>
  /// 来源医院名称
  /// </summary>
  public string SourceHospitalName { get; set; }
  /// <summary>
  /// 来源院区编码
  /// </summary>
  public string SourceBranchCode { get; set; }
  /// <summary>
  /// 来源院区名称
  /// </summary>
  public string SourceBranchName { get; set; }
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
  /// 标准项目编码
  /// </summary>
  public string StandardProjectCode { get; set; }
  /// <summary>
  /// 互认科室ID
  /// </summary>
  public string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称
  /// </summary>
  public string RecognitionDeptName { get; set; }
  /// <summary>
  /// 互认医生ID
  /// </summary>
  public string RecognitionDoctorId { get; set; }
  /// <summary>
  /// 互认医生名称
  /// </summary>
  public string RecognitionDoctorName { get; set; }
  /// <summary>
  /// 互认时间
  /// </summary>
  public DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 患者脱敏信息
  /// </summary>
  public string MaskedPatientInfo { get; set; }
}
