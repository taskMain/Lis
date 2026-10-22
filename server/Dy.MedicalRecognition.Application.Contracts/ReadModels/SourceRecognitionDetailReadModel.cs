using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 提供方医院被互认明细行：本组织作为提供方被其他医院采纳的逐项事实，只反映被采纳事实。
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
  /// <summary>提供方组织编码。</summary>
  public string SourceOrganizationCode { get; init; } = string.Empty;
  /// <summary>提供方组织名称；按当页涉及编码批量回填。</summary>
  public string SourceOrganizationName { get; init; } = string.Empty;
  /// <summary>提供方医院编码。</summary>
  public string SourceHospitalCode { get; init; } = string.Empty;
  /// <summary>提供方医院名称；按当页涉及编码批量回填。</summary>
  public string SourceHospitalName { get; init; } = string.Empty;
  /// <summary>提供方院区编码。</summary>
  public string SourceBranchCode { get; init; } = string.Empty;
  /// <summary>提供方院区名称；按当页涉及编码批量回填。</summary>
  public string SourceBranchName { get; init; } = string.Empty;
  /// <summary>使用方组织编码。</summary>
  public string ReceiverOrganizationCode { get; init; } = string.Empty;
  /// <summary>使用方组织名称；按当页涉及编码批量回填。</summary>
  public string ReceiverOrganizationName { get; init; } = string.Empty;
  /// <summary>使用方医院编码。</summary>
  public string ReceiverHospitalCode { get; init; } = string.Empty;
  /// <summary>使用方医院名称；按当页涉及编码批量回填。</summary>
  public string ReceiverHospitalName { get; init; } = string.Empty;
  /// <summary>使用方院区编码。</summary>
  public string ReceiverBranchCode { get; init; } = string.Empty;
  /// <summary>使用方院区名称；按当页涉及编码批量回填。</summary>
  public string ReceiverBranchName { get; init; } = string.Empty;
  /// <summary>标准项目编码。</summary>
  public string StandardProjectCode { get; init; } = string.Empty;
  /// <summary>标准项目名称；取自标准目录当前版本，标准项目不在目录中时为空。</summary>
  public string? StandardProjectName { get; init; }
  /// <summary>项目类型；取匹配项自身保存值，恒有值。</summary>
  public MedicalItemType ItemType { get; init; }
  /// <summary>
  /// 项目类型中文；由服务端按枚举声明解析，页面直接展示，不在前端重写一份类型文案。
  /// </summary>
  /// <remarks>取值未登记时返回 null（项目类型列无存储约束校验），由页面按「未知类型」安全展示。</remarks>
  public string? ItemTypeText => EnumDescriptorText.GetOrNull(ItemType, MedicalItemTypeDescriptorList.List);
  /// <summary>标准目录分类名称；取自标准项目所属分类，分类缺失时为空。</summary>
  public string? CategoryName { get; init; }
  /// <summary>标准目录分组名称；取自标准项目所属分组，分组缺失时为空。</summary>
  public string? GroupName { get; init; }
  /// <summary>互认科室ID；取处理结果自身保存值。</summary>
  public string? RecognitionDeptId { get; init; }
  /// <summary>互认科室名称；取处理结果自身保存的名称。</summary>
  public string? RecognitionDeptName { get; init; }
  /// <summary>互认医生ID；取处理结果自身保存的医院业务人员标识。</summary>
  public string? RecognitionDoctorId { get; init; }
  /// <summary>互认医生名称；取处理结果自身保存的名称。</summary>
  public string? RecognitionDoctorName { get; init; }
  /// <summary>处理结果的互认时间；提供方明细只反映采纳事实，恒有值。</summary>
  public DateTime RecognitionTime { get; init; }
  /// <summary>患者姓名；按业务原值返回，取自匹配项绑定报告的报告主体检索列。</summary>
  public string PatientName { get; init; } = string.Empty;
  /// <summary>患者证件号码；取匹配记录保存值，按业务原值返回。</summary>
  public string IdentityDocumentNo { get; init; } = string.Empty;
}
