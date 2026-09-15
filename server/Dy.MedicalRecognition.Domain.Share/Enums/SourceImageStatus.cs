namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 检查报告来源影像的状态；平台不接收影像文件，也不按调阅地址是否为空反推状态，状态与地址的一致性由来源侧负责。
/// </summary>
public enum SourceImageStatus
{
  /// <summary>
  /// 有影像，数值 1；地址缺失表示暂不可调阅。
  /// </summary>
  Available = 1,
  /// <summary>
  /// 无影像，数值 2；调阅地址必须为空。
  /// </summary>
  None = 2,
  /// <summary>
  /// 状态未知，数值 3；调阅地址可有可无。
  /// </summary>
  Unknown = 3,
}
