using Dy.Core.Abstractions.Data;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate;

/// <summary>
/// 平台参数：本应用维护的全平台统一配置项，按参数编码唯一。
/// </summary>
/// <remarks>参数编码是业务唯一键；参数值按编码对应的业务规则校验，停用状态由维护入口维护。</remarks>
[IPropertyChangedAware]
public partial class PlatformParameter : Dy.Core.Abstractions.Domain.Entity
{
  /// <summary>
  /// 主键标识。
  /// </summary>
  public partial Guid Id { get; set; }

  /// <summary>
  /// 参数编码；全平台唯一，用于业务读取与维护入口定位同一参数。
  /// </summary>
  public partial string ParameterCode { get; set; }

  /// <summary>
  /// 参数值；按参数编码对应的规则保存，不做组织级覆盖。
  /// </summary>
  public partial string ParameterValue { get; set; }

  /// <summary>
  /// 参数说明；仅用于维护界面展示，不参与业务判定。
  /// </summary>
  public partial string? Description { get; set; }

  /// <summary>
  /// 是否启用；停用后业务读取按参数缺失处理。
  /// </summary>
  public partial bool IsValid { get; set; }

  /// <summary>
  /// 操作人标识；取当前 IAM 用户标识。
  /// </summary>
  public partial Guid OperId { get; set; }

  /// <summary>
  /// 操作时间；取本次保存时的服务端当前时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
