using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 停用互认项目配置的请求。
/// </summary>
/// <remarks>只提交配置标识；组织编码取自登录令牌的组织声明，标准项目与启用状态由服务端按已保存的配置与领域规则决定，调用方不能通过本请求改变。</remarks>
[IPropertyChangedAware]
public partial record DisableMutualRecognitionItemRequest : Dto
{
  /// <summary>
  /// 配置标识；必需值，空 Guid 由请求校验拒绝。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：互认项目配置ID不能为空。")]
  public partial Guid Id { get; set; }
}
