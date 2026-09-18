namespace Dy.MedicalRecognition.Domain.Queries.Ports;

/// <summary>
/// 报告与报告版本的只读查询端口。
/// </summary>
/// <remarks>
/// 读取报告列表、版本列表、版本详情内容与下载所需的文件信息；返回的内部投影不作为对外契约。
/// 全部查询只筛选与投影，不修改数据，也不判断业务状态。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 统计满足筛选条件的报告总数。
  /// </summary>
  /// <remarks>与数据语句共用同一套筛选条件，计数语句不带排序。</remarks>
  /// <param name="filter">已规范化的筛选条件。</param>
  /// <returns>满足条件的总记录数。</returns>
  Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter);
  /// <summary>
  /// 取报告列表的当页数据。
  /// </summary>
  /// <remarks>
  /// 排序为报告时间倒序加报告标识倒序；时间范围作用于报告主体上的报告时间、左闭右开；
  /// 患者筛选按前缀匹配，<c>%</c> 与 <c>_</c> 按普通字符处理；无匹配时返回空集合。
  /// 分页窗口起点与页容量由应用层校验后传入，窗口语法由数据映射器的当前 Provider 适配层生成，本语句保持中立。
  /// </remarks>
  /// <param name="filter">已规范化的筛选条件。</param>
  /// <param name="skipCount">分页窗口起点，即当页之前已经越过的记录数。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页报告行。</returns>
  Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize);
  /// <summary>
  /// 取某个报告的全部版本。
  /// </summary>
  /// <remarks>按版本序号升序；同时给出当前版本标识、是否已被替代与报告生命周期状态；无版本时返回空集合。</remarks>
  /// <param name="reportId">报告标识。</param>
  /// <returns>版本行集合。</returns>
  Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId);
  /// <summary>
  /// 按报告标识与报告版本标识读取版本详情行。
  /// </summary>
  /// <remarks>报告标识与版本标识必须同时命中同一行；未命中时返回 <see langword="null"/>。</remarks>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>版本详情行。</returns>
  Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId);
  /// <summary>
  /// 读取某个报告版本的检验专项内容行。
  /// </summary>
  /// <remarks>一个版本至多一条；未命中时返回 <see langword="null"/>。</remarks>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检验专项内容行。</returns>
  Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId);
  /// <summary>
  /// 读取某个报告版本的普通检验结果。
  /// </summary>
  /// <remarks>按展示序号升序；无结果时返回空集合。</remarks>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>普通检验结果行集合。</returns>
  Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId);
  /// <summary>
  /// 读取某个报告版本的细菌鉴定结果。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>细菌鉴定结果行集合；无结果时为空集合。</returns>
  Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId);
  /// <summary>
  /// 读取某条细菌鉴定结果下的药敏结果。
  /// </summary>
  /// <remarks>按展示序号升序；无结果时返回空集合。</remarks>
  /// <param name="bacteriaResultId">细菌鉴定结果标识。</param>
  /// <returns>药敏结果行集合。</returns>
  Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId);
  /// <summary>
  /// 读取某个报告版本的检查专项内容行。
  /// </summary>
  /// <remarks>一个版本至多一条；未命中时返回 <see langword="null"/>。</remarks>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检查专项内容行。</returns>
  Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId);
  /// <summary>
  /// 读取某个报告版本的检查项目。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检查项目行集合；无项目时为空集合。</returns>
  Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId);
  /// <summary>
  /// 读取某条检查项目下的检查部位。
  /// </summary>
  /// <param name="examinationItemId">检查项目标识。</param>
  /// <returns>检查部位行集合；无部位时为空集合。</returns>
  Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId);
  /// <summary>
  /// 按报告标识读取报告的来源归属行，供管理端按报告标识校验可信范围。
  /// </summary>
  /// <param name="reportId">报告标识。</param>
  /// <returns>报告来源归属行；报告不存在时为 <see langword="null"/>。</returns>
  Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId);
  /// <summary>
  /// 读取某个报告版本的公共信息行。
  /// </summary>
  /// <remarks>版本不存在时返回 <see langword="null"/>；公共信息按来源原值保存。</remarks>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>版本公共信息行。</returns>
  Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId);
  /// <summary>
  /// 按报告标识与报告版本标识读取下载所需的文件信息行。
  /// </summary>
  /// <remarks>报告标识用于归属定位、版本标识用于定位版本；两者必须同时命中同一行；未命中时返回 <see langword="null"/>。</remarks>
  /// <param name="reportId">报告标识。</param>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>文件信息行，含不对外返回的文件键。</returns>
  Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId);
}
