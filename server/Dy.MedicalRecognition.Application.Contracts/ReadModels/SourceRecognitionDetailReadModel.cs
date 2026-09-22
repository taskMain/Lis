namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 来源医院被互认明细行：本组织作为来源方被其他医院采纳的逐项事实，只反映被采纳事实。
/// </summary>
/// <remarks>
/// 明细的互认科室与医生取各业务记录自身保存值；患者姓名取自匹配项绑定报告的报告主体检索列，
/// 证件号码取匹配记录保存值，均按业务原值返回。
/// </remarks>
public sealed record SourceRecognitionDetailReadModel
{
  /// <summary>互认匹配记录标识；用于打开匹配记录集合视图。</summary>
  public Guid RecognitionMatchRecordId { get; init; }
  /// <summary>互认匹配项标识。</summary>
  public Guid RecognitionMatchItemId { get; init; }
  /// <summary>来源组织编码。</summary>
  public string SourceOrganizationCode { get; init; } = string.Empty;
  /// <summary>来源组织名称；按当页涉及编码批量回填。</summary>
  public string SourceOrganizationName { get; init; } = string.Empty;
  /// <summary>来源医院编码。</summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>来源医院名称；按当页涉及编码批量回填。</summary>
  public string SourceHospitalName { get; init; } = string.Empty;
  /// <summary>来源院区编码。</summary>
  public string SourceBranchCode { get; init; } = string.Empty;
  /// <summary>来源院区名称；按当页涉及编码批量回填。</summary>
  public string SourceBranchName { get; init; } = string.Empty;
  /// <summary>接收组织编码。</summary>
  public string ReceiverOrganizationCode { get; init; } = string.Empty;
  /// <summary>接收组织名称；按当页涉及编码批量回填。</summary>
  public string ReceiverOrganizationName { get; init; } = string.Empty;
  /// <summary>接收医院编码。</summary>
  public string ReceiverHospitalCode { get; init; } = string.Empty;
  /// <summary>接收医院名称；按当页涉及编码批量回填。</summary>
  public string ReceiverHospitalName { get; init; } = string.Empty;
  /// <summary>接收院区编码。</summary>
  public string ReceiverBranchCode { get; init; } = string.Empty;
  /// <summary>接收院区名称；按当页涉及编码批量回填。</summary>
  public string ReceiverBranchName { get; init; } = string.Empty;
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>互认科室ID；取处理结果自身保存值。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称；取处理结果自身保存的名称。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>互认医生ID；取处理结果自身保存的医院业务人员标识。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>互认医生名称；取处理结果自身保存的名称。</summary>
  public string? RecognitionDoctorName { get; init; }
  /// <summary>处理结果的互认时间；来源侧明细只反映采纳事实，恒有值。</summary>
  public DateTime RecognitionTime { get; init; }
  /// <summary>患者姓名；按业务原值返回，取自匹配项绑定报告的报告主体检索列。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者证件号码；取匹配记录保存值，按业务原值返回。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
}
