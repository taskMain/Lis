using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 互认处理结果的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record RecognitionProcessingResultDto : Dto
{
  /// <summary>
  /// 处理结果标识。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属互认匹配记录标识；其下匹配项须一次完整提交，按整组原子保存且首次保存后不可更改。
  /// </summary>
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 被处理的互认匹配项标识；每个匹配项只保存一条最终决定。
  /// </summary>
  public partial Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 医生作出互认决定的时间；采纳统计的业务时间。
  /// </summary>
  public partial DateTime RecognitionTime { get; set; }
  /// <summary>
  /// 互认决定结果；1=采纳、2=不采纳，首次保存后不可更改或在两者之间转换。
  /// </summary>
  public partial RecognitionResult RecognitionResult { get; set; }
  /// <summary>
  /// 互认科室在来源系统中的标识，使用字符串承载。
  /// </summary>
  public partial string RecognitionDeptId { get; set; }
  /// <summary>
  /// 互认科室名称，与标识成对提供；不参与幂等一致性判断。
  /// </summary>
  public partial string RecognitionDeptName { get; set; }
  /// <summary>
  /// 互认医生在来源系统中的人员标识，使用字符串承载。
  /// </summary>
  public partial string RecognitionDoctorId { get; set; }
  /// <summary>
  /// 互认医生名称，与标识成对提供；不参与幂等一致性判断。
  /// </summary>
  public partial string RecognitionDoctorName { get; set; }
  /// <summary>
  /// 不采纳原因，取值见 <see cref="RecognitionNonAdoptionReason"/>；仅在不采纳时填写，采纳时不得带有原因。
  /// </summary>
  public partial RecognitionNonAdoptionReason? NonAdoptionReason { get; set; }
  /// <summary>
  /// 不采纳的补充说明；选择“其他确需复查”等原因时必填。
  /// </summary>
  public partial string? NonAdoptionDescription { get; set; }
  /// <summary>
  /// 预计节省金额，单位元；采纳时按接收组织、医院、院区的当前金额一次形成，未配置时按零元形成。
  /// </summary>
  public partial decimal? EstimatedSavingAmount { get; set; }
  /// <summary>
  /// 操作人标识；与互认医生标识属于不同身份空间，由服务端写入。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
