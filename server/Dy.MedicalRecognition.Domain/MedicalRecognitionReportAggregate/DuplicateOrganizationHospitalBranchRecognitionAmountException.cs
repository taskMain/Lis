namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 表示同一组织、医院、院区与标准项目组合下已存在金额记录的持久化冲突事实。
/// </summary>
/// <remarks>
/// 由仓储在新增金额违反"组织编码 + 医院编码 + 院区编码 + 标准项目编码"唯一约束时抛出，
/// 领域层据此翻译为可读的并发保存业务拒绝；该类型只表达冲突事实，
/// 不承载面向用户的提示文案，也不作为公共契约返回值。
/// </remarks>
public sealed class DuplicateOrganizationHospitalBranchRecognitionAmountException : Exception
{
  /// <summary>
  /// 以重复金额事实和原始数据库异常创建异常。
  /// </summary>
  /// <param name="message">描述重复事实的技术消息，不作为面向用户的提示文案。</param>
  /// <param name="innerException">数据库返回的唯一约束冲突异常，用于保留原始错误以便排查。</param>
  public DuplicateOrganizationHospitalBranchRecognitionAmountException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
