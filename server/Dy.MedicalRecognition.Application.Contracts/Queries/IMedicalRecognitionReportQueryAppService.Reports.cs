using Dy.MedicalRecognition.Application.Contracts.Queries;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告管理端只读查询契约：两个列表入口、版本列表与版本详情。
/// </summary>
/// <remarks>
/// 平台管理员入口的组织、医院、院区取自请求；医院管理员入口的组织与医院取自令牌可信上下文并覆盖同名请求字段。
/// 两个入口共用同一分页契约与同一读模型；本契约不做写入、不登记事件，也不引入报告查看权限模型。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 查询报告列表（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 分页窗口校验在第一次仓储访问之前完成；名称回填按当页涉及的组织与医院批量读取，调用次数不随返回行数增长。
  /// 患者证件号码与患者姓名按规范形式做前缀匹配，只作为明确输入的查询条件。
  /// </remarks>
  /// <param name="request">筛选条件与分页参数；组织、医院、院区按请求使用。</param>
  /// <returns>当页报告与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">
  /// 分页窗口越界、组织或医院或院区不存在、已停用、父子归属不匹配，或外部组织服务不可用时抛出；不降级为空名称或编码。
  /// </exception>
  Task<PageResultDto<MedicalReportListReadModel>> QueryMedicalReportListAsync(ReportListQueryRequest request);

  /// <summary>
  /// 查询本可信范围内的报告列表（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 组织与医院只取自可信上下文，不接受请求提交；请求院区必须属于可信医院，不属于即拒绝，不返回其他医院的数据。
  /// </remarks>
  /// <param name="request">院区与筛选条件、分页参数；请求不提交组织与医院。</param>
  /// <returns>当页报告与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界，或请求院区不存在、已停用、不属于可信医院时抛出。
  /// </exception>
  Task<PageResultDto<MedicalReportListReadModel>> QueryBranchMedicalReportListAsync(BranchReportListQueryRequest request);

  /// <summary>
  /// 查询某个报告的全部历史版本。
  /// </summary>
  /// <remarks>
  /// 按版本序号返回全部版本，并区分当前有效版本、已被后续版本替代与报告已作废状态；
  /// 责任人员以结构化字段返回，检验版本另带检验人名称与明细检测人，检查版本另带检查医生名称。
  /// </remarks>
  /// <param name="request">报告标识。</param>
  /// <returns>版本列表读模型集合；该报告无版本时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">报告标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">报告不存在、报告不在可信范围内时抛出。</exception>
  Task<IReadOnlyList<MedicalReportVersionListReadModel>> QueryMedicalReportVersionListAsync(MedicalReportVersionListQueryRequest request);

  /// <summary>
  /// 查询某个报告版本的完整内容。
  /// </summary>
  /// <remarks>
  /// 按报告类型返回检验内容或检查内容读模型之一，另一支为空引用；枚举文本由服务端按枚举描述派生；
  /// 患者姓名与证件号码完整返回，患者联系电话按来源字段脱敏后返回。
  /// </remarks>
  /// <param name="request">报告标识与报告版本标识。</param>
  /// <returns>版本详情读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">报告标识或报告版本标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">版本不存在或不属于该报告、报告不在可信范围内时抛出。</exception>
  Task<MedicalReportVersionDetailQueryReadModel> QueryMedicalReportVersionDetailAsync(MedicalReportVersionDetailQueryRequest request);
}
