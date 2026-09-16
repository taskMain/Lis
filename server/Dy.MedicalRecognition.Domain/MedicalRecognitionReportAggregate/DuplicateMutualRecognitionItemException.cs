namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 表示同一可信组织下已存在同一标准项目互认配置的持久化冲突事实。
/// </summary>
/// <remarks>
/// 由仓储在写入互认配置违反“组织编码 + 标准项目编码”唯一约束时抛出，领域层据此翻译为可读的重复配置业务拒绝；
/// 该类型只表达冲突事实，不承载面向用户的提示文案，也不作为公共契约返回值。
/// </remarks>
public sealed class DuplicateMutualRecognitionItemException : Exception
{
  /// <summary>
  /// 以重复配置事实和原始数据库异常创建异常。
  /// </summary>
  /// <param name="message">描述重复事实的技术消息，不作为面向用户的提示文案。</param>
  /// <param name="innerException">数据库返回的唯一约束冲突异常，用于保留原始错误以便排查。</param>
  public DuplicateMutualRecognitionItemException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
