using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 保存本院区互认项目金额的请求（医院管理员入口）。
/// </summary>
/// <remarks>
/// 组织与医院由服务端从可信上下文注入，请求不提交也不得覆盖；请求院区必须属于可信医院，否则拒绝。
/// 操作人与操作时间由服务端从可信上下文与当前时间写入，调用方不提交，也不提交金额记录标识。
/// 保存前提是该组织已建立该标准项目编码的互认项目配置；金额只允许非负且最多两位小数。
/// </remarks>
[IPropertyChangedAware]
public partial record SaveBranchRecognitionAmountRequest : Dto
{
  /// <summary>
  /// 院区编码；服务端按该编码校验院区存在、启用且属于可信组织与可信医院，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：院区编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public partial string BranchCode { get; set; } = string.Empty;
  /// <summary>
  /// 标准项目编码；服务端按该编码校验可信组织已建立互认项目配置，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：标准项目编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public partial string StandardProjectCode { get; set; } = string.Empty;
  /// <summary>
  /// 当前金额，单位元；只允许非负且最多两位小数，零元是有效配置，缺失由请求校验拒绝。
  /// </summary>
  /// <remarks>
  /// 声明为可空是必填校验生效的前提：<see cref="RequiredAttribute"/> 判的是值是否为 <see langword="null"/>，
  /// 而不可空值类型装箱后永不为 <see langword="null"/>，特性恒真、缺省会被反序列化成 <c>0</c> 并静默写入零元。
  /// </remarks>
  [Required(ErrorMessage = "参数校验失败：当前金额不能为空。")]
  public partial decimal? CurrentAmount { get; set; }
}
