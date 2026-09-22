using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 互认统计与导出的只读查询契约：接收侧与来源侧各设平台/本院两个汇总与明细入口、
/// 一个统计导出应用层入口与一个互认匹配记录集合视图。
/// </summary>
/// <remarks>
/// 平台入口的组织、医院与院区取自请求并经组织路径解析校验；本院入口的本侧组织与医院取自令牌可信上下文，
/// 请求不携带本侧组织与医院字段。八个查询共用同一分页契约与同一读模型；导出继承查询条件，
/// 由导出控制器的平台/本院两个 POST 动作调用同一入口；本契约不做写入、不登记事件，也不引入权限模型。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 查询接收侧互认使用汇总（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 每行为所选汇总维度的一个分组值，未参与分组的维度字段为空；分页窗口校验在第一次仓储访问之前完成；
  /// 提醒按互认匹配生成时间统计，采纳、不采纳与金额按互认时间统计，引用按实际引用时间统计。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、接收组与来源组筛选及分页参数；范围条件按请求使用。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">
  /// 分页窗口越界、范围条件不存在、已停用或父子归属不匹配，或来源组与接收组组织编码不一致时抛出。
  /// </exception>
  Task<PageResultDto<RecognitionUsageSummaryReadModel>> QueryRecognitionUsageSummaryAsync(RecognitionUsageSummaryQueryRequest request);

  /// <summary>
  /// 查询接收侧互认使用明细（平台管理员入口）。
  /// </summary>
  /// <remarks>明细类型必填；互认医生与不采纳原因代码只作明细查询条件，不形成汇总维度。</remarks>
  /// <param name="request">日期范围、明细类型、接收组与来源组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界、范围条件不存在、已停用或父子归属不匹配时抛出。</exception>
  Task<PageResultDto<RecognitionUsageDetailReadModel>> QueryRecognitionUsageDetailsAsync(RecognitionUsageDetailsQueryRequest request);

  /// <summary>
  /// 查询来源医院被互认汇总（平台管理员入口）。
  /// </summary>
  /// <remarks>来源侧仅接受来源医院、来源院区与标准项目维度，互认科室维度值按业务拒绝。</remarks>
  /// <param name="request">日期范围、汇总维度、来源组与接收组筛选及分页参数。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 分页窗口越界、范围条件不存在、已停用、父子归属不匹配，或汇总维度取互认科室值时抛出。
  /// </exception>
  Task<PageResultDto<SourceRecognitionSummaryReadModel>> QuerySourceRecognitionSummaryAsync(SourceRecognitionSummaryQueryRequest request);

  /// <summary>
  /// 查询来源医院被互认明细（平台管理员入口）。
  /// </summary>
  /// <remarks>来源侧明细只反映被采纳事实，不适用互认科室与不采纳原因筛选。</remarks>
  /// <param name="request">日期范围、来源组与接收组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界、范围条件不存在、已停用或父子归属不匹配时抛出。</exception>
  Task<PageResultDto<SourceRecognitionDetailReadModel>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailsQueryRequest request);

  /// <summary>
  /// 查询本可信范围内的接收侧互认使用汇总（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 接收组织与接收医院取自可信上下文，请求不提交；本侧接收院区可选，为空按可信医院全院范围统计；
  /// 来源组织恒为可信组织，请求只提交来源组医院与院区的可选筛选。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、本侧院区与来源组筛选及分页参数。</param>
  /// <returns>当页汇总分组行与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界，或本侧院区、来源组条件不存在、已停用、不属于可信医院时抛出。
  /// </exception>
  Task<PageResultDto<RecognitionUsageSummaryReadModel>> QueryBranchRecognitionUsageSummaryAsync(BranchRecognitionUsageSummaryQueryRequest request);

  /// <summary>
  /// 查询本可信范围内的接收侧互认使用明细（医院管理员入口）。
  /// </summary>
  /// <remarks>明细类型必填；本侧院区可选，为空按可信医院全院范围查询明细。</remarks>
  /// <param name="request">日期范围、明细类型、本侧院区与来源组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界，或本侧院区、来源组条件不存在、已停用、不属于可信医院时抛出。
  /// </exception>
  Task<PageResultDto<RecognitionUsageDetailReadModel>> QueryBranchRecognitionUsageDetailsAsync(BranchRecognitionUsageDetailsQueryRequest request);

  /// <summary>
  /// 查询本可信范围的来源医院被互认汇总（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 来源组织与来源医院固定为可信上下文，请求无法指定其他来源医院；
  /// 本侧来源院区可选，为空按可信医院全部来源院区统计；互认科室维度值在来源侧按业务拒绝。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、本侧来源院区与接收组筛选及分页参数。</param>
  /// <returns>当页汇总分组行与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界、汇总维度取互认科室值，或本侧院区、接收组条件不存在、已停用、不属于可信医院时抛出。
  /// </exception>
  Task<PageResultDto<SourceRecognitionSummaryReadModel>> QueryBranchSourceRecognitionSummaryAsync(BranchSourceRecognitionSummaryQueryRequest request);

  /// <summary>
  /// 查询本可信范围的来源医院被互认明细（医院管理员入口）。
  /// </summary>
  /// <remarks>来源侧明细只反映被采纳事实，不适用互认科室与不采纳原因筛选。</remarks>
  /// <param name="request">日期范围、本侧来源院区与接收组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界，或本侧院区、接收组条件不存在、已停用、不属于可信医院时抛出。
  /// </exception>
  Task<PageResultDto<SourceRecognitionDetailReadModel>> QueryBranchSourceRecognitionDetailsAsync(BranchSourceRecognitionDetailsQueryRequest request);

  /// <summary>
  /// 按当前查询条件生成互认统计导出文件（应用层入口，由导出控制器的平台/本院两个 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 导出继承发起时的全部查询条件、汇总维度与业务归属范围，不使用页面分页，取全部记录并封顶十万行；
  /// 汇总维度按导出类型校验来源侧维度子集（来源侧类型不接受互认科室维度）；
  /// 零记录按业务拒绝，不生成空文件、不扩大查询范围；导出过程不写入数据库、不登记事件、不留存文件副本。
  /// </remarks>
  /// <param name="request">导出类型、汇总维度、日期范围与全部统计查询条件；不含分页对象。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">导出类型、汇总维度或筛选条件取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 当前条件下无可导出数据、记录数超过十万行上限，或汇总维度不符合导出类型的维度子集时抛出。
  /// </exception>
  Task<StatisticsExportFileReadModel> GetStatisticsExportAsync(RecognitionStatisticsExportRequest request);

  /// <summary>
  /// 按标识查看互认匹配记录集合视图（单记录读取，不分页）。
  /// </summary>
  /// <remarks>返回组级信息、患者字段、就诊、反馈状态、决定主体与全部匹配项；入口只挂接收侧四类明细行。</remarks>
  /// <param name="request">互认匹配记录标识。</param>
  /// <returns>匹配记录集合视图读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">匹配记录标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">匹配记录不存在或不在可信范围内时抛出。</exception>
  Task<RecognitionMatchRecordReadModel> QueryRecognitionMatchRecordAsync(RecognitionMatchRecordQueryRequest request);
}
