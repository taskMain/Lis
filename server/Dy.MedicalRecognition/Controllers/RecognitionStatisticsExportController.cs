using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 互认统计导出的手工控制器。
/// </summary>
/// <remarks>
/// 只做平台/本院两个 POST 请求绑定与文件流响应：范围解析、零记录与十万行上限校验、工作簿组装
/// 都在应用层导出入口完成，控制器不承载业务规则，也不做领域判断与 SQL。
/// 路由带版本前缀，避开框架按接口名自动推导的端点前缀；应用层两个导出实现方法以 NonAction 声明排除自动端点暴露，
/// 本控制器的两个 POST 动作因此是统计导出的全部对外端点。响应统一走框架既有形态：导出返回 Excel 工作簿文件流。
/// </remarks>
[ApiController]
[Route("api/v1/statistics-export")]
public sealed class RecognitionStatisticsExportController : ControllerBase
{
  /// <summary>
  /// 导出工作簿的统一内容类型，与应用层写入读模型的文件格式一致。
  /// </summary>
  private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>
  /// 统计与导出的查询应用服务，承载平台/本院两个导出入口。
  /// </summary>
  private readonly MedicalRecognitionReportQueryAppService queryAppService;

  /// <summary>
  /// 初始化控制器。
  /// </summary>
  /// <param name="queryAppService">统计与导出的查询应用服务。</param>
  public RecognitionStatisticsExportController(MedicalRecognitionReportQueryAppService queryAppService)
  {
    this.queryAppService = queryAppService;
  }

  /// <summary>
  /// 按平台管理员条件生成互认统计导出文件。
  /// </summary>
  /// <remarks>
  /// 接收组与来源组的范围条件及全部筛选条件取自请求体；零记录与超过十万行上限由应用层按业务拒绝，
  /// 此时本动作不返回文件响应。
  /// </remarks>
  /// <param name="request">导出类型、汇总维度、日期范围与全部统计查询条件。</param>
  /// <returns>Excel 工作簿文件流。</returns>
  [HttpPost("platform")]
  public async Task<IActionResult> ExportPlatformStatisticsAsync([FromBody] RecognitionStatisticsExportRequest request)
  {
    StatisticsExportFileReadModel file = await queryAppService.GetStatisticsExportAsync(request);
    return CreateExportResponse(file);
  }

  /// <summary>
  /// 按医院管理员条件生成本可信范围的互认统计导出文件。
  /// </summary>
  /// <remarks>
  /// 本侧组织与医院由应用层从可信上下文注入，请求不携带本侧组织与医院字段；
  /// 拒绝语义与平台动作一致。
  /// </remarks>
  /// <param name="request">导出类型、汇总维度、日期范围与筛选条件。</param>
  /// <returns>Excel 工作簿文件流。</returns>
  [HttpPost("branch")]
  public async Task<IActionResult> ExportBranchStatisticsAsync([FromBody] BranchRecognitionStatisticsExportRequest request)
  {
    StatisticsExportFileReadModel file = await queryAppService.GetBranchStatisticsExportAsync(request);
    return CreateExportResponse(file);
  }

  /// <summary>
  /// 把应用层导出文件组装为禁止缓存的工作簿文件流响应。
  /// </summary>
  /// <remarks>
  /// 文件名、文件字节取自应用层读模型；导出数据随查询条件变化，响应不得被中间层或浏览器缓存，
  /// 因此按报告 PDF 下载先例设置禁止缓存响应头。
  /// </remarks>
  /// <param name="file">应用层生成的导出文件：文件名、文件格式与文件字节。</param>
  /// <returns>携带禁止缓存响应头的文件流响应。</returns>
  private IActionResult CreateExportResponse(StatisticsExportFileReadModel file)
  {
    Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    return File(file.FileStream, ExcelContentType, file.FileName);
  }
}
