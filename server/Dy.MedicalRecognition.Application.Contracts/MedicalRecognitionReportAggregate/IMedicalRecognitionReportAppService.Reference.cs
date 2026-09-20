using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 引用结果的写入口声明。
/// </summary>
/// <remarks>
/// 本分片声明一个医院接入能力：提交已成功写入本次病历的实际引用事实。成功统一返回 <see langword="true"/>，失败以业务异常表达；
/// 接收组织、医院、院区取自可信上下文，操作人取自登录上下文，调用方都不提交。
/// </remarks>
public partial interface IMedicalRecognitionReportAppService
{
  /// <summary>
  /// 提交一个或多个已经实际写入病历的引用项目。
  /// </summary>
  /// <remarks>
  /// 每个引用项目只通过互认匹配项标识定位，不要求提交匹配记录标识、本次来源就诊或互认时间；
  /// 一次请求可以包含来自不同互认匹配记录的项目，全部项目整体校验并原子保存，任一项目失败时整次失败、不提供逐条部分结果。
  /// 本入口是事务边界；命中完全一致的既有引用事实时按幂等成功返回，相同匹配项下参与比较的事实不一致时返回冲突且不覆盖既有事实。
  /// 引用结果不校验绑定报告版本在提交时是否仍有效，也不受引用详情有效期限制。
  /// </remarks>
  /// <param name="request">引用结果提交请求，携带本次已经实际写入病历的引用项目集合。</param>
  /// <returns>
  /// 整批保存成功或命中幂等时返回 <see langword="true"/>；
  /// 本方法不返回 <see langword="false"/>，入参不合法、可信身份不可解析与业务拒绝都以异常结束。
  /// </returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">
  /// 引用项目集合为空或缺少集合本身，或任一项目的互认匹配项标识为空、引用科室或引用医生的标识或名称缺失、空串或纯空白时抛出。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，互认匹配项不存在，该匹配项对应的处理结果不存在或决定不是采纳，
  /// 该匹配项所属匹配记录的接收三值与本次调用归属不一致，引用科室或引用医生的标识与名称不成对，
  /// 实际引用时间早于该组已保存的互认时间或晚于本次请求接收时间，
  /// 相同互认匹配项下参与比较的事实不一致，或引用事实写入影响的行数不为 1 时抛出；
  /// 以上情况都不保存任何引用事实、不登记事件。
  /// </exception>
  Task<bool> SubmitRecognitionReferencesAsync(RecognitionReferenceSubmissionRequest request);
}
