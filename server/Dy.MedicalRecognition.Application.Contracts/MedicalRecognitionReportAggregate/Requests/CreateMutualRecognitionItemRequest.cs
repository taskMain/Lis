using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 新建互认项目配置的请求。
/// </summary>
/// <remarks>
/// 组织编码由服务端取自登录令牌的组织声明写入，调用方不提交组织，也不提交内部标准项目 ID；
/// 请求提交的标准项目编码由服务端读取标准项目后取其标识保存，编码读不到标准项目即拒绝；
/// 创建后的配置即为启用状态，调用方不能提交启用状态。
/// 可互认时间只允许正整数，编码只做必填与空白校验，不设业务长度上限。
/// </remarks>
[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemRequest : Dto
{
  /// <summary>
  /// 标准项目编码；服务端按该编码读取标准项目并取其标识保存，读不到标准项目即拒绝；缺失、空串或纯空白都由请求校验拒绝。
  /// </summary>
  [Required(ErrorMessage = "参数校验失败：标准项目编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：标准项目编码不能是空白。")]
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 可互认时间天数；只允许正整数，零、负数与未提交均拒绝。
  /// </summary>
  [Range(1, int.MaxValue, ErrorMessage = "参数校验失败：可互认时间天数必须是正整数。")]
  public partial int RecognitionDurationDays { get; set; }
}
