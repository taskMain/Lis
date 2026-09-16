using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 修改互认项目配置可互认时间的请求。
/// </summary>
/// <remarks>只提交配置标识与本次可互认时间天数；组织、标准项目与启用状态都不由调用方提交，也不能通过本请求改变。</remarks>
[IPropertyChangedAware]
public partial record UpdateMutualRecognitionItemConfigurationRequest : Dto
{
  /// <summary>
  /// 配置标识；必需值，空 Guid 由请求校验拒绝。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认项目配置ID不能为空。")]
  public partial Guid Id { get; set; }
  /// <summary>
  /// 可互认时间天数；只允许正整数，与当前值相同也照常保存。
  /// </summary>
  [Range(1, int.MaxValue, ErrorMessage = "参数校验失败：可互认时间天数必须是正整数。")]
  public partial int RecognitionDurationDays { get; set; }
}
