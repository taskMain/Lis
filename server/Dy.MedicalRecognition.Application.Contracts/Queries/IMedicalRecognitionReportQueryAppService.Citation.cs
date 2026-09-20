using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>
/// 获取引用详情的只读查询契约：医院 HIS 在医生采纳互认匹配后按本次就诊取回可写入病历的内容。
/// </summary>
/// <remarks>
/// 本入口是只读查询，不产生写入、事件或状态变化，也不声明显式事务；
/// 组织、医院与院区取自可信调用身份，请求只提供医院必然掌握的患者证件与本次来源就诊。
/// </remarks>
public partial interface IMedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 按患者证件与本次来源就诊获取仍可引用的已采纳项目内容与报告公共上下文。
  /// </summary>
  /// <remarks>
  /// 有效期按各匹配记录的处理结果保存时间分别计算，每次请求读取当前有效时长参数值，参数调整后立即按原起点生效；
  /// 逐项校验决定为采纳、匹配项归属正确且绑定报告版本仍为当前有效版本；
  /// 同一报告版本与同一互认项目存在多条采纳时只返回保存时间最近的一条；
  /// 部分项目超期或失效时返回仍可用的部分，全部项目均不可返回时按平台实际确认的原因拒绝，不返回成功空集合。
  /// </remarks>
  /// <param name="request">患者证件类型、证件号码、本次来源就诊类型与就诊流水号。</param>
  /// <returns>仍可引用的已采纳项目与其所属报告的必要公共上下文。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">请求字段不满足契约声明的必填或值域约束时抛出，此时不读取参数与任何业务数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，引用详情有效时长参数不存在、取值非法或参数服务读取失败时抛出；
  /// 本次就诊在该医院、院区与证件下没有已采纳记录，或全部相关项目均超出有效时长、绑定报告版本均已失效时同样抛出。
  /// </exception>
  Task<RecognitionCitationDetailReadModel> QueryRecognitionCitationDetailAsync(RecognitionCitationDetailRequest request);
}
