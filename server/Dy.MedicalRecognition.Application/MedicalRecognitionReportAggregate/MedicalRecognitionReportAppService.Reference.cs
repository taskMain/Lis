using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 在线互认主流程的医院接入写入口：提交引用结果。
/// </summary>
/// <remarks>
/// 组织、医院与院区三层都取自可信上下文，操作人取自登录上下文；本入口不读取系统参数，也不读取任何参数化的有效期；
/// 本入口是事务边界：整批引用事实共同保存或共同不保存，命中幂等时直接返回成功且不产生任何写入；
/// 引用科室与引用医生由请求显式提供，不以调用账号替代。
/// </remarks>
public partial class MedicalRecognitionReportAppService
{
  /// <summary>
  /// 提交已经写入本次病历的实际引用事实：全部引用项目整批校验后原子保存并登记一次事件。
  /// </summary>
  /// <remarks>
  /// 顺序固定：公共请求校验 → 可信三层解析 → 操作人解析 → 命令组装 → 领域整批校验与保存。
  /// 请求校验先于一切领域读写，因此校验失败时既不读取匹配项、处理结果、匹配记录与既有引用事实，也不写入任何内容、不登记事件；
  /// 请求不提交组织、医院、院区、操作人、匹配记录标识、本次来源就诊与互认时间，它们都由服务端按各匹配项已保存的事实定位取得；
  /// 本次请求接收时间由服务端取并作为实际引用时间的上界。
  /// </remarks>
  /// <param name="request">引用结果提交请求，携带本次已经实际写入病历的引用项目集合。</param>
  /// <returns>整批保存成功或命中幂等时返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或非空白约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，或领域层判定匹配项不存在、处理结果不存在、对应决定不是采纳、
  /// 接收三值与所属匹配记录不一致、引用科室或引用医生不成对、实际引用时间早于互认时间或晚于请求接收时间、
  /// 相同匹配项下事实不一致、既有引用事实不可覆盖与写入影响行数异常时抛出；此时不保存任何引用事实、不登记事件。
  /// </exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitRecognitionReferencesAsync(RecognitionReferenceSubmissionRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out Guid userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");

    // 两个时间用途不同、口径也不同，因此分别取值：
    // 操作字段取协调世界时，由项目约定统一用于行的操作时间；实际引用时间是无时区业务时间，
    // 其上界必须与业务时间的写入侧同一时钟口径，因此请求接收时点取本地墙上时间。
    DateTimeOffset operTime = DateTimeOffset.UtcNow;
    DateTime receivedTime = DateTime.Now;

    SubmitRecognitionReferencesCommand command = new()
    {
      OrganizationCode = trustedScope.OrganizationCode,
      HospitalCode = trustedScope.HospitalCode,
      BranchCode = trustedScope.BranchCode,
      // 实际引用时间、引用科室与引用医生都是项目级事实：按请求逐项原样写入，不以调用账号替代。
      ReferenceItems =
      [
        .. request.ReferenceItems.Select(item => new SubmitRecognitionReferenceCommandItem
        {
          RecognitionMatchItemId = item.RecognitionMatchItemId,
          ReferencedTime = item.ReferencedTime,
          ReferenceDeptId = item.ReferenceDeptId,
          ReferenceDeptName = item.ReferenceDeptName,
          ReferenceDoctorId = item.ReferenceDoctorId,
          ReferenceDoctorName = item.ReferenceDoctorName
        })
      ],
      OperTime = operTime,
      ReceivedTime = receivedTime,
      OperId = userId
    };

    return await manager.SubmitRecognitionReferencesAsync(command);
  }
}
