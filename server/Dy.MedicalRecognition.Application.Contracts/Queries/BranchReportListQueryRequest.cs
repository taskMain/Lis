using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 报告列表查询：医院管理员入口；组织与医院取自令牌可信上下文并覆盖同名请求字段，院区取自请求。
/// </summary>
/// <remarks>
/// 组织与医院由服务端注入，请求提交的同名字段不生效；院区必须存在、启用且属于可信医院，不属于即拒绝。
/// 本类型不声明组织与医院字段，因此请求侧不存在可提交的组织与医院。
/// </remarks>
public sealed record BranchReportListQueryRequest : MedicalReportListQueryRequest
{
  /// <summary>院区编码；必填，必须存在、启用且属于可信医院。</summary>
  [Required(ErrorMessage = "参数校验失败：院区编码不能为空。")]
  [NonEmpty(ErrorMessage = "参数校验失败：院区编码不能是空白。")]
  public string BranchCode { get; init; } = string.Empty;
}
