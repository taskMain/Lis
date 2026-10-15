using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 管理端页面导出的手工控制器。
/// </summary>
/// <remarks>
/// 只做请求绑定与文件流响应：范围校验、零记录与十万行上限校验、工作簿组装都在应用层导出入口完成，
/// 控制器不承载业务规则，也不做领域判断与 SQL。路由带版本前缀，避开框架按接口名自动推导的端点前缀；
/// 应用层导出实现方法以 NonAction 声明排除自动端点暴露，本控制器的 POST 动作因此是导出能力的全部对外端点。
/// 响应统一走框架既有形态：导出返回 Excel 工作簿文件流。
/// </remarks>
[ApiController]
[Route("api/v1/recognition-export")]
public sealed class RecognitionExportController : ControllerBase
{
  /// <summary>
  /// 导出工作簿的统一内容类型，与应用层写入读模型的文件格式一致。
  /// </summary>
  private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>
  /// 统计与导出的查询应用服务，承载互认项目页、平台金额页与本院区金额页的导出入口。
  /// </summary>
  private readonly MedicalRecognitionReportQueryAppService queryAppService;

  /// <summary>
  /// 初始化控制器。
  /// </summary>
  /// <param name="queryAppService">统计与导出的查询应用服务。</param>
  public RecognitionExportController(MedicalRecognitionReportQueryAppService queryAppService)
  {
    this.queryAppService = queryAppService;
  }

  /// <summary>
  /// 按互认项目页条件生成互认项目配置导出文件。
  /// </summary>
  /// <remarks>
  /// 请求组织必须等于服务端可信当前组织，关键字可选；零记录与超过十万行上限由应用层按业务拒绝，
  /// 此时本动作不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：组织编码必填，关键字可选。</param>
  /// <returns>Excel 工作簿文件流。</returns>
  [HttpPost("recognition-projects")]
  public async Task<IActionResult> ExportRecognitionProjectsAsync([FromBody] RecognitionProjectConfigurationExportRequest request)
  {
    ConfigurationExportFileReadModel file = await queryAppService.GetRecognitionProjectConfigurationExportAsync(request);
    return CreateExportResponse(file);
  }

  /// <summary>
  /// 按平台金额页条件生成互认项目金额导出文件。
  /// </summary>
  /// <remarks>
  /// 请求组织、医院与院区按请求使用，服务端校验三者存在、启用与父子归属，非法组合整次拒绝，
  /// 不要求请求组织等于服务端可信当前组织；零记录与超过十万行上限由应用层按业务拒绝，
  /// 此时本动作不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：组织、医院与院区编码必填，关键字可选。</param>
  /// <returns>Excel 工作簿文件流。</returns>
  [HttpPost("recognition-amounts")]
  public async Task<IActionResult> ExportRecognitionAmountsAsync([FromBody] RecognitionAmountExportRequest request)
  {
    ConfigurationExportFileReadModel file = await queryAppService.GetRecognitionAmountExportAsync(request);
    return CreateExportResponse(file);
  }

  /// <summary>
  /// 按本院区金额页条件生成本院区互认项目金额导出文件。
  /// </summary>
  /// <remarks>
  /// 组织与医院由服务端可信上下文解析（令牌优先、缺失层按登录用户档案补齐、不一致即拒绝、补齐后仍空拒绝），
  /// 请求院区由服务端校验存在、启用且属于可信医院，越权院区整次拒绝；
  /// 零记录与超过十万行上限由应用层按业务拒绝，此时本动作不返回文件响应。
  /// </remarks>
  /// <param name="request">导出条件：院区编码必填，关键字可选。</param>
  /// <returns>Excel 工作簿文件流。</returns>
  [HttpPost("branch-recognition-amounts")]
  public async Task<IActionResult> ExportBranchRecognitionAmountsAsync([FromBody] BranchRecognitionAmountExportRequest request)
  {
    ConfigurationExportFileReadModel file = await queryAppService.GetBranchRecognitionAmountExportAsync(request);
    return CreateExportResponse(file);
  }

  /// <summary>
  /// 把应用层导出文件组装为禁止缓存的工作簿文件流响应。
  /// </summary>
  /// <remarks>
  /// 文件名、文件字节取自应用层读模型；导出数据随查询条件变化，响应不得被中间层或浏览器缓存，
  /// 因此按统计导出控制器先例设置禁止缓存响应头。
  /// </remarks>
  /// <param name="file">应用层生成的导出文件：文件名、文件格式与文件字节。</param>
  /// <returns>携带禁止缓存响应头的文件流响应。</returns>
  private IActionResult CreateExportResponse(ConfigurationExportFileReadModel file)
  {
    Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    return File(file.FileStream, ExcelContentType, file.FileName);
  }
}
