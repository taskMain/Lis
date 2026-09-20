using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 在线互认主流程的医院接入写入口：互认处理结果提交。
/// </summary>
/// <remarks>
/// 组织、医院与院区三层都取自可信上下文，操作人取自登录上下文；本入口不读取系统参数；
/// 本入口是事务边界：整组处理结果共同保存或共同不保存，命中幂等时直接返回成功且不产生任何写入。
/// </remarks>
public partial class MedicalRecognitionReportAppService
{
  /// <summary>
  /// 按互认匹配记录整组提交处理结果：全部匹配项的采纳或不采纳决定整批原子保存。
  /// </summary>
  /// <remarks>
  /// 顺序固定：公共请求校验 → 可信三层解析 → 操作人解析 → 命令组装 → 领域整批校验与保存。
  /// 请求校验先于一切领域读写，因此校验失败时既不读取处理结果与匹配项，也不写入任何内容、不登记事件；
  /// 请求不提交组织、医院、院区与操作人，本次请求接收时间由服务端取并作为互认时间的上界与处理结果保存时间的取值来源。
  /// </remarks>
  /// <param name="request">处理结果提交请求，携带互认匹配记录标识、组级互认时间与决定主体，以及完整的组内项目决定集合。</param>
  /// <returns>整批保存成功或命中幂等时返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">请求字段不满足契约声明的必填或值域约束时抛出，此时不进入领域调用、不写入任何数据。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，或领域层判定匹配记录不存在、归属不一致、项目集合不完整、时间顺序非法、
  /// 决定内容与原因不合规、决定主体不成对、绑定报告版本失效、既有事实冲突与写入影响行数异常时抛出；此时不保存任何项目、不登记事件。
  /// </exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitRecognitionProcessingResultsAsync(RecognitionProcessingResultSubmissionRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out Guid userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");

    // 两个时间用途不同、口径也不同，因此分别取值：
    // 操作字段取协调世界时，由项目约定统一用于行的操作时间；互认时间与处理结果保存时间是无时区业务时间，
    // 其上界与保存时间的取值必须与业务时间的写入侧同一时钟口径，因此请求接收时点取本地墙上时间。
    DateTimeOffset operTime = DateTimeOffset.UtcNow;
    DateTime receivedTime = DateTime.Now;

    SubmitRecognitionProcessingResultsCommand command = new()
    {
      RecognitionMatchRecordId = request.RecognitionMatchRecordId,
      OrganizationCode = trustedScope.OrganizationCode,
      HospitalCode = trustedScope.HospitalCode,
      BranchCode = trustedScope.BranchCode,
      // 本次请求的接收时间取服务端本地墙上时间，同时作为互认时间的上界与处理结果保存时间。
      RecognitionTime = request.RecognitionTime,
      // 互认科室与医生的标识与名称按请求直接保存：不向权限系统补查名称，也不以调用账号替代。
      RecognitionDeptId = request.RecognitionDeptId,
      RecognitionDeptName = request.RecognitionDeptName,
      RecognitionDoctorId = request.RecognitionDoctorId,
      RecognitionDoctorName = request.RecognitionDoctorName,
      ProcessingResults =
      [
        .. request.ProcessingResults.Select(item => new SubmitRecognitionProcessingResultCommandItem
        {
          RecognitionMatchItemId = item.RecognitionMatchItemId,
          Result = item.Result,
          NonAdoptionReason = item.NonAdoptionReason,
          NonAdoptionDescription = item.NonAdoptionDescription
        })
      ],
      OperTime = operTime,
      ReceivedTime = receivedTime,
      OperId = userId
    };

    return await manager.SubmitRecognitionProcessingResultsAsync(command);
  }
}
