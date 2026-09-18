using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 报告采集与生命周期所需的读写端口：报告、报告版本、平台患者与七类专项内容明细。
/// </summary>
/// <remarks>
/// 只做数据读写并返回行数或查询结果，不判断业务状态；并发违反唯一约束时数据库异常按原样向外传播，本层不识别数据库错误码，
/// 也不把持久化异常翻译为内部异常类型。
/// </remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 按证件类型代码与证件号码读取平台患者。
  /// </summary>
  /// <remarks>两个入参都必须是规范形式（去首尾空白，证件号码已统一大写）；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="identityDocumentTypeCode">证件类型代码。</param>
  /// <param name="identityDocumentNo">证件号码。</param>
  /// <returns>平台患者实体。</returns>
  Task<PlatformPatient?> GetPlatformPatientByDocumentAsync(string identityDocumentTypeCode, string identityDocumentNo);
  /// <summary>
  /// 插入一个平台患者。
  /// </summary>
  /// <remarks>并发首次解析同一证件时由证件类型代码与证件号码唯一约束拒绝，数据库异常按原样向外传播。</remarks>
  /// <param name="platformPatient">待插入的患者实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreatePlatformPatientAsync(PlatformPatient platformPatient);
  /// <summary>
  /// 按报告标识读取报告。
  /// </summary>
  /// <remarks>已作废报告同样返回，由领域层判定是否拒绝；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="id">报告标识。</param>
  /// <returns>报告实体。</returns>
  Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByIdAsync(Guid id);
  /// <summary>
  /// 按组织、医院、院区、报告类型与报告单号定位报告。
  /// </summary>
  /// <remarks>五个业务键列上的唯一索引保证至多命中一行；不筛选生命周期状态；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="hospitalCode">医院编码。</param>
  /// <param name="branchCode">院区编码。</param>
  /// <param name="reportType">报告类型。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <returns>报告实体。</returns>
  Task<MedicalRecognitionReport?> GetMedicalRecognitionReportByBusinessKeyAsync(
    string organizationCode, string hospitalCode, string branchCode, MedicalReportType reportType, string reportNo);
  /// <summary>
  /// 插入一份当前有效的报告。
  /// </summary>
  /// <param name="medicalRecognitionReport">待插入的报告实体，携带当前版本指向与三个检索列。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport);
  /// <summary>
  /// 追加版本后更新报告的当前版本指向与报告主体上的报告时间、患者姓名、证件号码，并刷新操作字段。
  /// </summary>
  /// <remarks>三列与当前版本指向在同一语句内更新，因此报告主体上的三列恒等于当前版本的对应取值。</remarks>
  /// <param name="medicalRecognitionReport">携带报告标识、新的当前版本指向、三个检索列与操作字段的报告实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateMedicalRecognitionReportCurrentVersionAsync(MedicalRecognitionReport medicalRecognitionReport);
  /// <summary>
  /// 把报告作废，写入作废时间、作废原因与操作字段。
  /// </summary>
  /// <remarks>条件更新带原生命周期状态：报告已作废或不存在时影响 0 行，调用方据此区分幂等与并发结果。</remarks>
  /// <param name="medicalRecognitionReport">携带报告标识、目标状态、原状态、作废事实与操作字段的报告实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> VoidMedicalRecognitionReportAsync(MedicalRecognitionReport medicalRecognitionReport);
  /// <summary>
  /// 读取某个报告当前最大的版本序号。
  /// </summary>
  /// <param name="reportId">报告标识。</param>
  /// <returns>当前最大版本序号；该报告尚无版本时返回 0。</returns>
  Task<int> GetMedicalReportMaxVersionNumberAsync(Guid reportId);
  /// <summary>
  /// 按报告版本标识读取版本。
  /// </summary>
  /// <remarks>供作废时读取当前版本的平台接收时间、供下载与版本详情定位版本；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="id">报告版本标识。</param>
  /// <returns>报告版本实体。</returns>
  Task<MedicalReportVersion?> GetMedicalReportVersionByIdAsync(Guid id);
  /// <summary>
  /// 插入一个报告版本。
  /// </summary>
  /// <param name="medicalReportVersion">待插入的版本实体，携带版本序号、公共信息、文件键与下载名。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMedicalReportVersionAsync(MedicalReportVersion medicalReportVersion);
  /// <summary>
  /// 插入一条检验报告专项内容。
  /// </summary>
  /// <param name="laboratoryReportContent">待插入的检验专项内容实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateLaboratoryReportContentAsync(LaboratoryReportContent laboratoryReportContent);
  /// <summary>
  /// 插入一条普通检验结果。
  /// </summary>
  /// <param name="laboratoryResultItem">待插入的普通结果实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateLaboratoryResultItemAsync(LaboratoryResultItem laboratoryResultItem);
  /// <summary>
  /// 插入一条细菌鉴定结果。
  /// </summary>
  /// <param name="laboratoryBacteriaResult">待插入的细菌鉴定结果实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateLaboratoryBacteriaResultAsync(LaboratoryBacteriaResult laboratoryBacteriaResult);
  /// <summary>
  /// 插入一条药敏结果。
  /// </summary>
  /// <param name="laboratoryAntimicrobialSusceptibility">待插入的药敏结果实体，携带所属细菌鉴定结果标识。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateLaboratoryAntimicrobialSusceptibilityAsync(LaboratoryAntimicrobialSusceptibility laboratoryAntimicrobialSusceptibility);
  /// <summary>
  /// 插入一条检查报告专项内容。
  /// </summary>
  /// <param name="examinationReportContent">待插入的检查专项内容实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateExaminationReportContentAsync(ExaminationReportContent examinationReportContent);
  /// <summary>
  /// 插入一条检查项目。
  /// </summary>
  /// <param name="examinationItem">待插入的检查项目实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateExaminationItemAsync(ExaminationItem examinationItem);
  /// <summary>
  /// 插入一条检查部位。
  /// </summary>
  /// <param name="examinationSite">待插入的检查部位实体，携带所属检查项目标识。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateExaminationSiteAsync(ExaminationSite examinationSite);
}
