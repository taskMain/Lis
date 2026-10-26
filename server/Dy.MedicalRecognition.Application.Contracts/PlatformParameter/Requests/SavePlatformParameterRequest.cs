using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.PlatformParameter.Requests;

/// <summary>
/// 保存单条平台参数的公共请求。
/// </summary>
/// <remarks>
/// 参数编码由维护入口的路径定位，不在请求体中重复提交；参数值按编码对应的业务规则校验，
/// 启用状态决定保存后是否参与业务读取。
/// </remarks>
public sealed record SavePlatformParameterRequest
{
  /// <summary>
  /// 参数值；必填，具体格式按参数编码对应的规则校验。
  /// </summary>
  [Required]
  public string ParameterValue { get; init; } = string.Empty;

  /// <summary>
  /// 参数说明；仅用于维护界面展示，可为空。
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// 是否启用；必填，停用后业务读取按参数缺失处理。
  /// </summary>
  [Required]
  public bool? IsValid { get; init; }
}
