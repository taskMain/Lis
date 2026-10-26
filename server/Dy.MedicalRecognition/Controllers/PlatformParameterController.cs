using Dy.MedicalRecognition.Application.Contracts.PlatformParameter;
using Dy.MedicalRecognition.Application.Contracts.PlatformParameter.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 平台参数维护手工控制器：查询全部参数与按编码保存单条参数。
/// </summary>
/// <remarks>
/// 只做路由与请求绑定：参数值校验、存在性与操作字段由应用层与领域层完成，控制器不做业务判断、不判角色。
/// 路由带版本前缀；维护接口不继承自动端点标记，本控制器是其唯一对外入口。
/// </remarks>
[ApiController]
[Route("api/v1/platform-parameters")]
public sealed class PlatformParameterController : ControllerBase
{
  /// <summary>
  /// 平台参数维护应用服务。
  /// </summary>
  private readonly IPlatformParameterMaintenanceAppService appService;

  /// <summary>
  /// 初始化维护控制器。
  /// </summary>
  /// <param name="appService">平台参数维护应用服务。</param>
  public PlatformParameterController(IPlatformParameterMaintenanceAppService appService) => this.appService = appService;

  /// <summary>
  /// 查询全部平台参数的维护列表。
  /// </summary>
  /// <returns>按参数编码升序排列的参数列表；没有记录时为空集合。</returns>
  [HttpGet]
  public async Task<ActionResult<IReadOnlyList<PlatformParameterReadModel>>> QueryPlatformParameterListAsync() =>
    Ok(await appService.QueryPlatformParameterListAsync());

  /// <summary>
  /// 按参数编码保存单条平台参数；编码不存在时新增，存在时更新。
  /// </summary>
  /// <param name="parameterCode">参数编码；必须是本应用维护的三个参数之一。</param>
  /// <param name="request">参数值、说明与启用状态。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  [HttpPut("{parameterCode}")]
  public async Task<ActionResult<bool>> SavePlatformParameterAsync(
    [FromRoute] string parameterCode,
    [FromBody] SavePlatformParameterRequest request) =>
    Ok(await appService.SavePlatformParameterAsync(parameterCode, request));
}
