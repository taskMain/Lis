using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 管理端页面导出的只读查询契约分片：按页面可产生的服务端查询条件生成 Excel 工作簿文件。
/// </summary>
/// <remarks>
/// 导出继承对应列表查询的条件口径、行集与排序，不使用页面分页，也不引入状态筛选字段；
/// 导出过程不写入数据库、不登记事件、不留存文件副本。入口由手工导出控制器的 POST 动作调用，
/// 实现以 NonAction 声明排除框架自动端点暴露。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 按当前查询条件生成互认项目配置导出文件（由导出控制器的互认项目 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 请求组织必须等于服务端可信当前组织，不等即拒绝且不降级为空集合；配置状态参数恒为空，
  /// 导出同时包含启用与停用的配置；零记录按业务拒绝，不生成空文件、不扩大查询范围；
  /// 取回行数超过十万行按业务拒绝并提示缩小查询条件。
  /// </remarks>
  /// <param name="request">导出条件：组织编码必填，关键字可选；不含分页对象与配置状态字段。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织编码缺失或为空白、关键字已传入但为空白文本时抛出。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织、请求组织与可信当前组织不一致、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  Task<ConfigurationExportFileReadModel> GetRecognitionProjectConfigurationExportAsync(RecognitionProjectConfigurationExportRequest request);

  /// <summary>
  /// 按当前查询条件生成平台金额页导出文件（由导出控制器的平台金额 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 请求组织、医院与院区按请求使用，服务端校验三者存在、启用与父子归属，非法组合整次拒绝，
  /// 不要求请求组织等于服务端可信当前组织；零记录按业务拒绝，不生成空文件、不扩大查询范围；
  /// 取回行数超过十万行按业务拒绝并提示缩小查询条件。
  /// </remarks>
  /// <param name="request">导出条件：组织、医院与院区编码必填，关键字可选；不含分页对象与状态筛选字段。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织、医院或院区编码缺失或为空白文本时抛出。</exception>
  /// <exception cref="InvalidOperationException">请求的组织、医院或院区不存在、已停用或父子归属不匹配、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  Task<ConfigurationExportFileReadModel> GetRecognitionAmountExportAsync(RecognitionAmountExportRequest request);

  /// <summary>
  /// 按当前查询条件生成本院区金额页导出文件（由导出控制器的本院区金额 POST 动作调用）。
  /// </summary>
  /// <remarks>
  /// 组织与医院由服务端可信上下文解析（令牌优先、缺失层按登录用户档案补齐、不一致即拒绝、补齐后仍空拒绝），
  /// 请求院区再经服务端校验存在、启用且属于可信医院，不属于即整次拒绝；零记录按业务拒绝，
  /// 不生成空文件、不扩大查询范围；取回行数超过十万行按业务拒绝并提示缩小查询条件。
  /// </remarks>
  /// <param name="request">导出条件：院区编码必填，关键字可选；不含组织与医院字段、分页对象与状态筛选字段。</param>
  /// <returns>导出文件读模型：文件名、文件格式与文件字节。</returns>
  /// <exception cref="ArgumentNullException">导出条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">院区编码缺失或为空白文本时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信组织或可信医院无法解析、请求院区不存在、已停用或不属于可信医院、当前条件下无可导出数据，或取回行数超过十万行上限时抛出。</exception>
  Task<ConfigurationExportFileReadModel> GetBranchRecognitionAmountExportAsync(BranchRecognitionAmountExportRequest request);
}
