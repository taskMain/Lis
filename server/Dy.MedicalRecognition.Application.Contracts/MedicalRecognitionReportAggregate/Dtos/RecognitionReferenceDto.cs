using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 互认引用事实的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record RecognitionReferenceDto : Dto
{
  /// <summary>
  /// 引用事实标识；同一匹配项在不同引用时间下可形成多条引用事实。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 被引用的互认匹配项标识；据此反查所属匹配记录。
  /// </summary>
  public partial Guid RecognitionMatchItemId { get; set; }
  /// <summary>
  /// 医生实际引用时间；引用统计的业务时间，首次保存后不可更正、撤销或删除。
  /// </summary>
  public partial DateTime ReferencedTime { get; set; }
  /// <summary>
  /// 引用科室在来源系统中的标识，使用字符串承载。
  /// </summary>
  public partial string ReferenceDeptId { get; set; }
  /// <summary>
  /// 引用科室名称，与标识成对提供；不参与幂等一致性判断。
  /// </summary>
  public partial string ReferenceDeptName { get; set; }
  /// <summary>
  /// 引用医生在来源系统中的人员标识，使用字符串承载。
  /// </summary>
  public partial string ReferenceDoctorId { get; set; }
  /// <summary>
  /// 引用医生名称，与标识成对提供；不参与幂等一致性判断。
  /// </summary>
  public partial string ReferenceDoctorName { get; set; }
  /// <summary>
  /// 操作人标识；与引用医生标识属于不同身份空间，由服务端写入。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
