namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 持久化层识别出的业务键唯一约束冲突：同一证件类型与证件号码的平台患者已存在。
/// </summary>
/// <remarks>
/// 只由仓储在插入平台患者时抛出；领域管理器据此复读一次并按核心身份一致性决定继续追加还是按身份冲突拒绝，
/// 本异常不改变对外响应形态。
/// </remarks>
public sealed class DuplicatePlatformPatientException : Exception
{
  /// <summary>
  /// 用默认消息初始化异常。
  /// </summary>
  public DuplicatePlatformPatientException()
  {
  }

  /// <summary>
  /// 用指定消息初始化异常。
  /// </summary>
  /// <param name="message">描述冲突事实的消息。</param>
  public DuplicatePlatformPatientException(string message) : base(message)
  {
  }

  /// <summary>
  /// 用指定消息与内部异常初始化异常。
  /// </summary>
  /// <param name="message">描述冲突事实的消息。</param>
  /// <param name="innerException">触发本次冲突的原始持久化异常。</param>
  public DuplicatePlatformPatientException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
