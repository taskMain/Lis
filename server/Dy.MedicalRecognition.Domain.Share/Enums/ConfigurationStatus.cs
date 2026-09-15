namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认项目配置的启用状态；数值 1 启用、2 停用。
/// </summary>
public enum ConfigurationStatus
{
  /// <summary>
  /// 配置启用，数值 1；参与互认匹配与后续引用。
  /// </summary>
  Enabled = 1,
  /// <summary>
  /// 配置停用，数值 2；数据保留，可重新启用；与标准目录实体的布尔启用状态字段无关，两者不可互换。
  /// </summary>
  Disabled = 2,
}
