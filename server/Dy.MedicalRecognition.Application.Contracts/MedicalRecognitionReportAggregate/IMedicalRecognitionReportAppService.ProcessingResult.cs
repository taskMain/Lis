using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 互认处理结果的写入口契约。
/// </summary>
/// <remarks>
/// 本分片声明一个医院接入能力：按互认匹配记录整组提交处理结果。成功统一返回 <see langword="true"/>，失败以业务异常表达；
/// 接收组织、医院、院区取自可信上下文，操作人取自登录上下文，调用方都不提交。
/// </remarks>
public partial interface IMedicalRecognitionReportAppService
{
  /// <summary>
  /// 按互认匹配记录整组提交医生作出的采纳或不采纳处理结果。
  /// </summary>
  /// <remarks>
  /// 接收组织、医院与院区取自可信上下文，操作人取自登录上下文；互认时间、互认科室与互认医生是组级事实，只在组级提供一次。
  /// 本入口是事务边界：整组处理结果共同保存或共同不保存；命中完全相同的既有事实时按幂等成功返回，
  /// 此时不读金额、不校验报告版本有效性、不新增记录、不重复统计、处理结果保存时间不变；
  /// 相同业务键下参与比较的事实不一致时返回冲突且不覆盖既有决定。
  /// </remarks>
  /// <param name="request">处理结果提交请求，携带互认匹配记录标识、组级互认时间与决定主体，以及完整覆盖该组全部匹配项的决定集合。</param>
  /// <returns>
  /// 整批保存成功或命中幂等时返回 <see langword="true"/>；
  /// 本方法不返回 <see langword="false"/>，入参不合法、可信身份不可解析与业务拒绝都以异常结束。
  /// </returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">
  /// 互认匹配记录标识为空、互认科室或互认医生的标识或名称缺失、空串或纯空白，
  /// 决定集合为空或元素标识为空、决定与不采纳原因不是已登记取值、补充说明超长时抛出。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，互认匹配记录不存在或不属于本次提交的组织、医院与院区，
  /// 项目集合缺项、重复或夹带其他组的匹配项，互认时间早于匹配记录生成时间或晚于本次请求接收时间，
  /// 采纳时带有不采纳原因，不采纳时未提供原因或选择了其他原因却缺少补充说明，
  /// 互认科室或互认医生的标识与名称不成对，未命中幂等时绑定报告版本已非当前有效版本，
  /// 相同业务键下参与比较的事实不一致，或处理结果与匹配记录处理结果保存时间的写入影响行数不为 1 时抛出；
  /// 以上情况都不保存任何项目、不登记事件。
  /// </exception>
  Task<bool> SubmitRecognitionProcessingResultsAsync(RecognitionProcessingResultSubmissionRequest request);
}
