namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 持久化层识别出的业务键唯一约束冲突：同一组织、医院、院区、报告类型与报告单号下已存在报告。
/// </summary>
/// <remarks>
/// 只由仓储在插入报告时抛出，用于把并发下的重复插入与其它持久化错误区分开；
/// 是否构成业务拒绝以及对外文案由领域管理器决定，本异常不改变对外响应形态。
/// </remarks>
public sealed class DuplicateMedicalRecognitionReportException : Exception
{
  /// <summary>
  /// 用默认消息初始化异常。
  /// </summary>
  public DuplicateMedicalRecognitionReportException()
  {
  }

  /// <summary>
  /// 用指定消息初始化异常。
  /// </summary>
  /// <param name="message">描述冲突事实的消息。</param>
  public DuplicateMedicalRecognitionReportException(string message) : base(message)
  {
  }

  /// <summary>
  /// 用指定消息与内部异常初始化异常。
  /// </summary>
  /// <param name="message">描述冲突事实的消息。</param>
  /// <param name="innerException">触发本次冲突的原始持久化异常。</param>
  public DuplicateMedicalRecognitionReportException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
