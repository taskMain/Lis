using Dy.Core.Abstractions.Domain;
using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

/// <summary>
/// 医院 HIS 按互认匹配记录整组提交处理结果：一次提交覆盖该组全部匹配项的采纳或不采纳决定。
/// </summary>
/// <remarks>
/// 互认时间、互认科室与互认医生是组级事实，对本次提交的全部匹配项共同适用，因此只在组级提供一次；
/// 接收组织、医院与院区取自可信调用身份，操作人取自登录上下文，调用方都不提交；
/// 项目集合必须完整覆盖该组全部匹配项且无重复、无其他组的匹配项，缺项、重复与夹带由领域层整批拒绝；
/// 采纳时不带不采纳原因，不采纳时必须提供平台统一原因代码，选择其他情形确需复查时补充说明必填。
/// </remarks>
[ObjectMap(typeof(RecognitionProcessingResultsSavedEvent), ObjectMapMode.OneWay, parameterizeOtherProperties: true)]
[IPropertyChangedAware]
public partial record SubmitRecognitionProcessingResultsCommand : ICommand
{
  /// <summary>
  /// 本次提交对应的互认匹配记录标识；同时是本组的归属范围，匹配项必须属于该记录。
  /// </summary>
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 可信接收组织编码；用于校验匹配记录归属，并作为读取采纳项目当前金额的业务键之一。
  /// </summary>
  public partial string OrganizationCode { get; set; }
  /// <summary>
  /// 可信接收医院编码；用于校验匹配记录归属，并作为读取采纳项目当前金额的业务键之一。
  /// </summary>
  public partial string HospitalCode { get; set; }
  /// <summary>
  /// 可信接收院区编码；用于校验匹配记录归属，并作为读取采纳项目当前金额的业务键之一。
  /// </summary>
  public partial string BranchCode { get; set; }
  /// <summary>
  /// 医生实际完成本组决定的互认时间；不得早于匹配记录生成时间、不得晚于本次请求接收时间，两端含边界。
  /// </summary>
  public partial DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 互认科室在来源系统中的标识；按请求直接保存，不向权限系统补查。
  /// </summary>
  public partial string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称；与互认科室标识成对保存，只保存不参与幂等一致性判断。
  /// </summary>
  public partial string RecognitionDeptName { get; set; }
  /// <summary>
  /// 互认医生在来源系统中的人员标识；按请求直接保存，不以调用账号替代。
  /// </summary>
  public partial string RecognitionDoctorId { get; set; }
  /// <summary>
  /// 互认医生名称；与互认医生标识成对保存，只保存不参与幂等一致性判断。
  /// </summary>
  public partial string RecognitionDoctorName { get; set; }
  /// <summary>
  /// 组内全部互认匹配项的决定集合；完整覆盖该组全部匹配项，重复项与夹带的其他组项都由领域层整批拒绝。
  /// </summary>
  public partial IReadOnlyList<SubmitRecognitionProcessingResultCommandItem> ProcessingResults { get; set; } = [];
  /// <summary>
  /// 操作时间；由应用服务取服务端协调世界时，写入处理结果的审计字段并作为领域事件的发生时间。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
  /// <summary>
  /// 本次请求的接收时间；由应用服务取服务端本地墙上时间，作为互认时间的上界与处理结果保存时间的取值来源。
  /// </summary>
  /// <remarks>
  /// 互认时间与处理结果保存时间同为无时区业务时间，因此上界与保存时间必须与业务时间的写入口径同源，不使用协调世界时。
  /// </remarks>
  public partial DateTime ReceivedTime { get; set; }
  /// <summary>
  /// 操作人标识；由应用服务从登录上下文解析，不是调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }

  /// <summary>
  /// 为本次整批保存的处理结果构造“处理结果已保存”领域事件。
  /// </summary>
  /// <remarks>
  /// 组级互认时间、互认科室与互认医生沿用命令取值；匹配项标识、逐项决定、逐项不采纳原因与预计节省金额合计
  /// 由领域层在校验通过并写入后按同一顺序补入，三组集合按下标逐项对应。
  /// </remarks>
  /// <param name="matchItemIds">本次保存的全部互认匹配项标识，按匹配项标识升序排列。</param>
  /// <param name="results">各匹配项的决定，下标与匹配项标识集合一致。</param>
  /// <param name="nonAdoptionReasons">各匹配项的不采纳原因，采纳项对应位置为空值。</param>
  /// <param name="estimatedSavingAmount">本次采纳项目形成的预计节省金额合计，单位元；全部不采纳时为零。</param>
  /// <returns>待登记的处理结果已保存事件。</returns>
  public RecognitionProcessingResultsSavedEvent CreateRecognitionProcessingResultsSavedEvent(
    IReadOnlyList<Guid> matchItemIds,
    IReadOnlyList<RecognitionResult> results,
    IReadOnlyList<RecognitionNonAdoptionReason?> nonAdoptionReasons,
    decimal estimatedSavingAmount)
  {
    RecognitionProcessingResultsSavedEvent recognitionProcessingResultsSavedEvent = this.MapToRecognitionProcessingResultsSavedEvent(
      eventCreator: OperId, eventCreatedTime: OperTime);
    recognitionProcessingResultsSavedEvent.RecognitionMatchItemIds = matchItemIds;
    recognitionProcessingResultsSavedEvent.Results = results;
    recognitionProcessingResultsSavedEvent.NonAdoptionReasons = nonAdoptionReasons;
    recognitionProcessingResultsSavedEvent.EstimatedSavingAmount = estimatedSavingAmount;
    return recognitionProcessingResultsSavedEvent;
  }
}
