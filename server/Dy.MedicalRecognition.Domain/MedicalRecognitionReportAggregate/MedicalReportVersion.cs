using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 一次采集形成的报告版本快照，检验、检查与细菌内容均挂在本版本下；每次完整报告成功上传追加一个版本，作废报告不再追加。
/// </summary>
[IPropertyChangedAware]
public partial class MedicalReportVersion : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 报告版本主键。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 报告ID
  /// </summary>
  public partial Guid ReportId { get; set; }
  /// <summary>
  /// 平台患者ID；患者身份、就诊、组织人员与关键业务时间均按来源原文保存；只有当前版本参与新的匹配与引用。
  /// </summary>
  public partial Guid PatientId { get; set; }
  /// <summary>
  /// 版本序号；在同一报告内依次递增，新的提交形成新版本而不改写已存在的版本。
  /// </summary>
  public partial int VersionNumber { get; set; }
  /// <summary>
  /// 来源报告名称
  /// </summary>
  public partial string SourceReportName { get; set; }
  /// <summary>
  /// 患者姓名；与性别代码、出生日期等身份字段创建后不再改写，使历史版本可复现当时用于互认的数据。
  /// </summary>
  public partial string PatientName { get; set; }
  /// <summary>
  /// 患者性别代码
  /// </summary>
  public partial string PatientGenderCode { get; set; }
  /// <summary>
  /// 患者出生日期
  /// </summary>
  public partial DateTime PatientBirthDate { get; set; }
  /// <summary>
  /// 患者联系电话
  /// </summary>
  public partial string? PatientPhoneNumber { get; set; }
  /// <summary>
  /// 报告时年龄
  /// </summary>
  public partial string? AgeAtReport { get; set; }
  /// <summary>
  /// 证件类型代码
  /// </summary>
  public partial string IdentityDocumentTypeCode { get; set; }
  /// <summary>
  /// 证件号码
  /// </summary>
  public partial string IdentityDocumentNo { get; set; }
  /// <summary>
  /// 就诊类型
  /// </summary>
  public partial VisitType VisitType { get; set; }
  /// <summary>
  /// 就诊流水号
  /// </summary>
  public partial string VisitSerialNo { get; set; }
  /// <summary>
  /// 申请科室ID
  /// </summary>
  public partial string ApplicationDeptId { get; set; }
  /// <summary>
  /// 申请科室名称
  /// </summary>
  public partial string ApplicationDeptName { get; set; }
  /// <summary>
  /// 申请医生ID
  /// </summary>
  public partial string ApplicationDoctorId { get; set; }
  /// <summary>
  /// 申请医生名称
  /// </summary>
  public partial string ApplicationDoctorName { get; set; }
  /// <summary>
  /// 执行科室ID
  /// </summary>
  public partial string ExecutionDeptId { get; set; }
  /// <summary>
  /// 执行科室名称
  /// </summary>
  public partial string ExecutionDeptName { get; set; }
  /// <summary>
  /// 报告科室ID
  /// </summary>
  public partial string ReportDeptId { get; set; }
  /// <summary>
  /// 报告科室名称
  /// </summary>
  public partial string ReportDeptName { get; set; }
  /// <summary>
  /// 报告医生ID
  /// </summary>
  public partial string ReportDoctorId { get; set; }
  /// <summary>
  /// 报告医生名称
  /// </summary>
  public partial string ReportDoctorName { get; set; }
  /// <summary>
  /// 审核医生ID
  /// </summary>
  public partial string ReviewDoctorId { get; set; }
  /// <summary>
  /// 审核医生名称
  /// </summary>
  public partial string ReviewDoctorName { get; set; }
  /// <summary>
  /// 审核时间
  /// </summary>
  public partial DateTime? ReviewTime { get; set; }
  /// <summary>
  /// 住院号
  /// </summary>
  public partial string? InpatientNo { get; set; }
  /// <summary>
  /// 病区名称
  /// </summary>
  public partial string? WardName { get; set; }
  /// <summary>
  /// 病房名称
  /// </summary>
  public partial string? RoomName { get; set; }
  /// <summary>
  /// 床位号
  /// </summary>
  public partial string? BedNo { get; set; }
  /// <summary>
  /// 申请时间
  /// </summary>
  public partial DateTime ApplicationTime { get; set; }
  /// <summary>
  /// 报告时间
  /// </summary>
  public partial DateTime ReportTime { get; set; }
  /// <summary>
  /// 源端报告修改时间
  /// </summary>
  public partial DateTime SourceModifiedTime { get; set; }
  /// <summary>
  /// 平台接收时间
  /// </summary>
  public partial DateTime ReceivedTime { get; set; }
  /// <summary>
  /// PDF文件标识；创建后不再改写。
  /// </summary>
  public partial string PdfFileId { get; set; }
  /// <summary>
  /// PDF文件名
  /// </summary>
  public partial string PdfFileName { get; set; }
  /// <summary>
  /// 来源保密标识
  /// </summary>
  public partial string? SourceConfidentialFlag { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由数据库当前时间写入，命令时间只用于领域事件。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
