using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 保存组织医院院区互认项目金额的请求（平台管理员入口）。
/// </summary>
/// <remarks>
/// 组织、医院与院区三个值按请求使用，服务端校验其存在、启用与父子归属，不要求请求组织等于登录令牌的组织声明；
/// 操作人与操作时间由服务端从可信上下文与当前时间写入，调用方不提交，也不提交金额记录标识。
/// 保存前提是当前组织已建立该标准项目编码的互认项目配置；金额只允许非负且最多两位小数。
/// </remarks>
[IPropertyChangedAware]
public partial record SaveOrganizationHospitalBranchRecognitionAmountRequest : Dto
{
  /// <summary>
  /// 组织编码；服务端按该编码校验组织存在且启用，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：组织编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：组织编码不能是空白。")]
  public partial string OrganizationCode { get; set; } = string.Empty;
  /// <summary>
  /// 医院编码；服务端按该编码校验医院存在、启用且属于请求组织，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：医院编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：医院编码不能是空白。")]
  public partial string HospitalCode { get; set; } = string.Empty;
  /// <summary>
  /// 院区编码；服务端按该编码校验院区存在、启用且属于请求医院与请求组织，缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：院区编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public partial string BranchCode { get; set; } = string.Empty;
  /// <summary>
  /// 标准项目编码；服务端按该编码校验当前组织已建立互认项目配置，缺失、空串或纯空白都由请求校验拒绝。
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
