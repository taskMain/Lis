using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

/// <summary>
/// 批量新建互认项目配置的请求：一次为一个可信组织建立同一可互认时间的多条配置。
/// </summary>
/// <remarks>
/// 组织编码由服务端取自登录令牌的组织声明写入，调用方不提交组织，也不提交内部标准项目标识；
/// 可互认时间在批次上只提供一份，整批配置共用同一取值，元素只携带标准项目编码；
/// 创建后的配置全部为启用状态，调用方不能提交启用状态。
/// 条数上限由请求声明，超出即在校验阶段拒绝，不会进入领域层；元素空白与批内重复由领域层判定，
/// 因为这两类规则针对集合元素，数据注解无法逐元素表达。
/// </remarks>
[IPropertyChangedAware]
public partial record CreateMutualRecognitionItemsRequest : Dto
{
  /// <summary>
  /// 标准项目编码集合；元素是服务端已有的标准项目编码，服务端按编码读取标准项目并取其标识保存。
  /// </summary>
  /// <remarks>
  /// 未提交时不进入领域层；提交空集合、超过单批上限、含空白元素或批内重复编码都由整批拒绝并报告违规编码，
  /// 其中条数上限在本类拒绝、其余三类在领域层拒绝。
  /// </remarks>
  [Required(ErrorMessage = "参数校验失败：标准项目编码集合不能为空。")]
  [MaxLength(MaxStandardProjectCodeCount, ErrorMessage = "参数校验失败：单批标准项目编码不能超过 1000 条。")]
  public partial IReadOnlyList<string> StandardProjectCodes { get; set; } = [];
  /// <summary>
  /// 可互认时间天数；本次提交的全部配置共用该取值，只允许正整数，零、负数与未提交均拒绝。
  /// </summary>
  [Range(1, int.MaxValue, ErrorMessage = "参数校验失败：可互认时间天数必须是正整数。")]
  public partial int RecognitionDurationDays { get; set; }

  /// <summary>
  /// 单批可提交的标准项目编码条数上限；与领域层同一上限，请求层先拒绝，领域层再作纵深防御。
  /// </summary>
  public const int MaxStandardProjectCodeCount = 1000;
}
