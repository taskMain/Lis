using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 报告版本快照的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record MedicalReportVersionDto : Dto
{
  /// <summary>
  /// 报告版本标识；报告内容明细、匹配项与引用详情都以该标识为归属依据。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属报告标识。
  /// </summary>
  public partial Guid ReportId { get; set; }
  /// <summary>
  /// 平台患者标识；本版本解析到的患者必须与报告既有关联一致，冲突时不允许追加。
  /// </summary>
  public partial Guid PatientId { get; set; }
  /// <summary>
  /// 版本序号；按平台成功追加顺序从 1 递增，同一报告内依次递增。
  /// </summary>
  public partial int VersionNumber { get; set; }
  /// <summary>
  /// 来源报告名称，按来源原文保存；新的提交形成新版本而不改写已存在的版本。
  /// </summary>
  public partial string SourceReportName { get; set; }
  /// <summary>
  /// 患者姓名；参与患者身份一致性核对，不使用姓名相似度匹配。
  /// </summary>
  public partial string PatientName { get; set; }
  /// <summary>
  /// 患者性别代码，按来源受控值保存；参与患者身份一致性核对。
  /// </summary>
  public partial string PatientGenderCode { get; set; }
  /// <summary>
  /// 患者出生日期；参与患者身份一致性核对，不用电话或院内卡替代。
  /// </summary>
  public partial DateTime PatientBirthDate { get; set; }
  /// <summary>
  /// 患者联系电话，仅用于授权展示与人工核对；展示时默认脱敏，不参与患者匹配。
  /// </summary>
  public partial string? PatientPhoneNumber { get; set; }
  /// <summary>
  /// 报告时年龄，以来源文本承载；不归一化为数值，也不参与患者匹配。
  /// </summary>
  public partial string? AgeAtReport { get; set; }
  /// <summary>
  /// 证件类型代码，按来源受控值保存；与证件号码组合精确识别患者。
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 证件号码，按证件类型与号码精确关联跨院患者。
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 本次来源就诊类型；1=门诊、2=急诊、3=住院、4=体检、5=其他。
  /// </summary>
  public partial VisitType VisitType { get; set; }
  /// <summary>
  /// 本次来源院内就诊流水号，与就诊类型共同定位同次就诊。
  /// </summary>
  public partial string VisitSerialNo { get; set; }
  /// <summary>
  /// 申请科室在来源系统中的人员标识；与名称须成对提供。
  /// </summary>
  public partial string ApplicationDeptId { get; set; }
  /// <summary>
  /// 申请科室名称，与标识成对提供；平台不依据名称反查科室。
  /// </summary>
  public partial string ApplicationDeptName { get; set; }
  /// <summary>
  /// 申请医生在来源系统中的人员标识；与名称须成对提供。
  /// </summary>
  public partial string ApplicationDoctorId { get; set; }
  /// <summary>
  /// 申请医生名称，与标识成对提供。
  /// </summary>
  public partial string ApplicationDoctorName { get; set; }
  /// <summary>
  /// 执行科室在来源系统中的标识，指实际执行本次检验或检查的科室。
  /// </summary>
  public partial string ExecutionDeptId { get; set; }
  /// <summary>
  /// 执行科室名称，与标识成对提供；平台不依据名称反查科室。
  /// </summary>
  public partial string ExecutionDeptName { get; set; }
  /// <summary>
  /// 报告科室在来源系统中的标识，指出具报告的科室。
  /// </summary>
  public partial string ReportDeptId { get; set; }
  /// <summary>
  /// 报告科室名称，与标识成对提供。
  /// </summary>
  public partial string ReportDeptName { get; set; }
  /// <summary>
  /// 报告医生在来源系统中的人员标识，指出具报告的医生。
  /// </summary>
  public partial string ReportDoctorId { get; set; }
  /// <summary>
  /// 报告医生名称，与标识成对提供。
  /// </summary>
  public partial string ReportDoctorName { get; set; }
  /// <summary>
  /// 审核医生在来源系统中的人员标识，指审核签发报告的医生。
  /// </summary>
  public partial string ReviewDoctorId { get; set; }
  /// <summary>
  /// 审核医生名称，与标识成对提供。
  /// </summary>
  public partial string ReviewDoctorName { get; set; }
  /// <summary>
  /// 报告审核通过时间，缺失表示来源未提供或报告尚未审核，平台不代为补录。
  /// </summary>
  public partial DateTime? ReviewTime { get; set; }
  /// <summary>
  /// 住院号，仅用于展示与人工核对，不参与患者匹配。
  /// </summary>
  public partial string? InpatientNo { get; set; }
  /// <summary>
  /// 病区名称，住院场景的来源病区信息，平台不做病区字典归一。
  /// </summary>
  public partial string? WardName { get; set; }
  /// <summary>
  /// 病房名称，住院场景的来源病房信息，与病区、床位一并用于展示。
  /// </summary>
  public partial string? RoomName { get; set; }
  /// <summary>
  /// 床位号，住院场景的来源床位信息，不作为患者标识使用。
  /// </summary>
  public partial string? BedNo { get; set; }
  /// <summary>
  /// 申请时间，来源系统提出申请的时间；不参与时间窗判定。
  /// </summary>
  public partial DateTime ApplicationTime { get; set; }
  /// <summary>
  /// 报告时间，来源系统的报告签发时间；是互认匹配时间窗判定的依据之一。
  /// </summary>
  public partial DateTime ReportTime { get; set; }
  /// <summary>
  /// 来源系统最后修改该报告的时间，仅用于来源追溯。
  /// </summary>
  public partial DateTime SourceModifiedTime { get; set; }
  /// <summary>
  /// 平台接收该版本的时间。
  /// </summary>
  public partial DateTime ReceivedTime { get; set; }
  /// <summary>
  /// 平台保存的报告 PDF 文件标识；按文件流下载，不转 Base64，创建后不再改写。
  /// </summary>
  public partial string PdfFileId { get; set; }
  /// <summary>
  /// PDF 原始文件名，来自来源系统的报告文件名，供下载时还原名称。
  /// </summary>
  public partial string PdfFileName { get; set; }
  /// <summary>
  /// 来源系统的保密级别原文，未归一化为枚举；平台不据此划分敏感等级或过滤匹配。
  /// </summary>
  public partial string? SourceConfidentialFlag { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
