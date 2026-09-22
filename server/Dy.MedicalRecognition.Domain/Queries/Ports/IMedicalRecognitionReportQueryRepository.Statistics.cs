namespace Dy.MedicalRecognition.Domain.Queries.Ports;

/// <summary>
/// 互认统计与导出的只读查询端口：接收侧与来源侧汇总、接收侧四类明细、来源侧明细与匹配记录集合视图。
/// </summary>
/// <remarks>
/// 方法名去掉 <c>Async</c> 后缀即查询作用域 <c>MedicalRecognitionReportQuery</c> 下的语句标识，
/// 框架按调用方法名定位语句；全部查询只筛选与投影，不修改数据，也不判断业务状态。
/// 计数语句与数据语句共用同一套筛选条件；汇总的计数与分页都作用于聚合后的分组行；
/// 分页窗口起点与页容量由应用层校验后传入，窗口语法由数据映射器的当前 Provider 适配层生成。
/// 原因汇总语句不带分页：行集按分组键加原因分组，应用层把原因行装配进当页所属分组行。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryRepository
{
  /// <summary>
  /// 统计接收侧互认使用汇总满足筛选条件的分组行总数。
  /// </summary>
  /// <remarks>计数对象是按所选维度聚合后的分组行；与当页分组语句共用同一套筛选条件，计数语句不带排序。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <returns>满足条件的分组行总数。</returns>
  Task<long> CountRecognitionUsageSummaryGroupsAsync(RecognitionUsageSummaryFilter filter);

  /// <summary>
  /// 取接收侧互认使用汇总的当页分组行。
  /// </summary>
  /// <remarks>
  /// 六项指标与预计节省金额由条件聚合一次成型：提醒按匹配生成时间、采纳与不采纳及金额按处理结果互认时间、
  /// 引用按引用事实实际引用时间各自独立过滤同一起止日期；
  /// 互认科室维度的展示名称取范围内互认时间最晚一条处理结果保存的名称；
  /// 分组键升序即稳定唯一顺序；未匹配任何分组时返回空集合。
  /// </remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <param name="skipCount">分页窗口起点，即当页之前已经越过的分组行数。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页汇总分组行。</returns>
  Task<IEnumerable<RecognitionUsageSummaryGroupItem>> QueryRecognitionUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 取接收侧汇总的不采纳原因汇总行。
  /// </summary>
  /// <remarks>
  /// 行集按与汇总当页语句同套的分组键加原因代码分组，独立成行返回原因代码与次数；
  /// 行集限定为处理结果为不采纳、原因已填写且互认时间在统计范围内的匹配项；
  /// 不带分页，调用方按分组键装配进所属分组行；无不采纳事实时返回空集合。
  /// </remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <returns>原因汇总行集合。</returns>
  Task<IEnumerable<RecognitionUsageReasonItem>> QueryRecognitionUsageSummaryReasonsAsync(RecognitionUsageSummaryFilter filter);

  /// <summary>
  /// 统计来源医院被互认汇总满足筛选条件的分组行总数。
  /// </summary>
  /// <remarks>行集为统计范围内的采纳事实，计数对象是按所选维度聚合后的分组行；与当页分组语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <returns>满足条件的分组行总数。</returns>
  Task<long> CountSourceRecognitionSummaryGroupsAsync(SourceRecognitionSummaryFilter filter);

  /// <summary>
  /// 取来源医院被互认汇总的当页分组行。
  /// </summary>
  /// <remarks>被互认次数等于本行范围内的采纳事实数；分组键升序即稳定唯一顺序；未匹配任何分组时返回空集合。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页来源侧汇总分组行。</returns>
  Task<IEnumerable<SourceRecognitionSummaryGroupItem>> QuerySourceRecognitionSummaryPageAsync(SourceRecognitionSummaryFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 统计接收侧提醒明细满足筛选条件的行数。
  /// </summary>
  /// <remarks>提醒事实即统计范围内按匹配生成时间落入的匹配项，含尚未反馈处理结果的项；与当页明细语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <returns>满足条件的提醒明细行数。</returns>
  Task<long> CountRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter);

  /// <summary>
  /// 取接收侧提醒明细的当页行。
  /// </summary>
  /// <remarks>
  /// 包含尚未反馈处理结果的项，处理结果字段为空值表示未反馈；
  /// 排序为匹配生成时间倒序加匹配项标识升序，跨页无重复无遗漏；无匹配时返回空集合。
  /// </remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页提醒明细行。</returns>
  Task<IEnumerable<RecognitionReminderDetailItem>> QueryRecognitionUsageReminderDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 统计接收侧采纳明细满足筛选条件的行数。
  /// </summary>
  /// <remarks>行集限定为处理结果为采纳且互认时间在统计范围内的匹配项；与当页明细语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <returns>满足条件的采纳明细行数。</returns>
  Task<long> CountRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter);

  /// <summary>
  /// 取接收侧采纳明细的当页行。
  /// </summary>
  /// <remarks>随行返回预计节省金额；排序为互认时间倒序加匹配项标识升序；无匹配时返回空集合。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页采纳明细行。</returns>
  Task<IEnumerable<RecognitionAdoptionDetailItem>> QueryRecognitionUsageAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 统计接收侧不采纳明细满足筛选条件的行数。
  /// </summary>
  /// <remarks>行集限定为处理结果为不采纳且互认时间在统计范围内的匹配项；与当页明细语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <returns>满足条件的不采纳明细行数。</returns>
  Task<long> CountRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter);

  /// <summary>
  /// 取接收侧不采纳明细的当页行。
  /// </summary>
  /// <remarks>随行返回原因代码与补充说明；排序为互认时间倒序加匹配项标识升序；无匹配时返回空集合。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页不采纳明细行。</returns>
  Task<IEnumerable<RecognitionNonAdoptionDetailItem>> QueryRecognitionUsageNonAdoptionDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 统计接收侧引用明细满足筛选条件的行数。
  /// </summary>
  /// <remarks>行集限定为引用事实的实际引用时间在统计范围内的匹配项；与当页明细语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <returns>满足条件的引用明细行数。</returns>
  Task<long> CountRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter);

  /// <summary>
  /// 取接收侧引用明细的当页行。
  /// </summary>
  /// <remarks>随行返回引用科室与引用医生；排序为实际引用时间倒序加匹配项标识升序；无匹配时返回空集合。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页引用明细行。</returns>
  Task<IEnumerable<RecognitionReferenceDetailItem>> QueryRecognitionUsageReferenceDetailsAsync(RecognitionUsageDetailFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 统计来源侧被互认明细满足筛选条件的行数。
  /// </summary>
  /// <remarks>行集限定为来源组与接收组范围内、互认时间在统计范围内的采纳事实；与当页明细语句共用同一套筛选条件。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <returns>满足条件的来源侧明细行数。</returns>
  Task<long> CountSourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter);

  /// <summary>
  /// 取来源侧被互认明细的当页行。
  /// </summary>
  /// <remarks>来源侧明细只反映被采纳事实；排序为互认时间倒序加匹配项标识升序；无匹配时返回空集合。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="skipCount">分页窗口起点。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>当页来源侧明细行。</returns>
  Task<IEnumerable<SourceRecognitionDetailItem>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailFilter filter, int skipCount, int pageSize);

  /// <summary>
  /// 按标识读取互认匹配记录的组级信息行。
  /// </summary>
  /// <remarks>
  /// 记录是否已反馈由组内全部匹配项的反馈状态聚合判定，全部有处理结果才算已反馈；
  /// 记录级的互认时间、互认科室与互认医生在组内聚合归一，未反馈记录为空值；未命中时返回 <see langword="null"/>。
  /// </remarks>
  /// <param name="recognitionMatchRecordId">互认匹配记录标识。</param>
  /// <returns>匹配记录组级行；记录不存在时为 <see langword="null"/>。</returns>
  Task<RecognitionMatchRecordView?> QueryRecognitionMatchRecordViewAsync(Guid recognitionMatchRecordId);

  /// <summary>
  /// 按标识读取互认匹配记录的全部匹配项行。
  /// </summary>
  /// <remarks>反馈状态与决策取各匹配项自身的处理结果；排序按匹配项标识升序，顺序稳定；无匹配项时返回空集合。</remarks>
  /// <param name="recognitionMatchRecordId">互认匹配记录标识。</param>
  /// <returns>匹配项行集合。</returns>
  Task<IEnumerable<RecognitionMatchRecordViewItem>> QueryRecognitionMatchRecordViewItemsAsync(Guid recognitionMatchRecordId);
}
