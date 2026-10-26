using Dy.MedicalRecognition.Application.Contracts.PlatformParameter.Requests;

namespace Dy.MedicalRecognition.Application.Contracts.PlatformParameter;

/// <summary>
/// 平台参数维护能力：查询全部参数与按编码保存单条参数。
/// </summary>
/// <remarks>读取与保存共用同一套参数值校验；不声明角色或接口级授权判定。</remarks>
public interface IPlatformParameterMaintenanceAppService
{
  /// <summary>
  /// 查询全部平台参数的维护列表。
  /// </summary>
  /// <returns>按参数编码升序排列的参数列表；没有记录时为空集合。</returns>
  Task<IReadOnlyList<PlatformParameterReadModel>> QueryPlatformParameterListAsync();

  /// <summary>
  /// 按参数编码保存单条平台参数；编码不存在时新增，存在时更新。
  /// </summary>
  /// <param name="parameterCode">参数编码；必须是本应用维护的三个参数之一。</param>
  /// <param name="request">参数值、说明与启用状态。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">请求字段不满足必填约束时抛出。</exception>
  /// <exception cref="InvalidOperationException">参数编码未登记、参数值非法、操作人不可解析或保存未影响任何行时抛出。</exception>
  Task<bool> SavePlatformParameterAsync(string parameterCode, SavePlatformParameterRequest request);
}
