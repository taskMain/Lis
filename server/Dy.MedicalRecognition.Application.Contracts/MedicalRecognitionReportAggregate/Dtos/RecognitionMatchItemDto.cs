using Dy.Core.Abstractions.Domain.Dtos;
using Dy.Core.SourceGen;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;

/// <summary>
/// 互认匹配项的对外返回数据。
/// </summary>
[IPropertyChangedAware]
public partial record RecognitionMatchItemDto : Dto
{
  /// <summary>
  /// 匹配项标识；是定位互认处理结果与引用事实的全局唯一依据。
  /// </summary>
  public partial Guid Id { get; set; }
  /// <summary>
  /// 所属互认匹配记录标识；处理结果须完整覆盖本记录下的全部匹配项。
  /// </summary>
  public partial Guid RecognitionMatchRecordId { get; set; }
  /// <summary>
  /// 被命中项目的项目类型；0=检验、1=检查，决定按检验还是检查报告内容展示。
  /// </summary>
  public partial MedicalItemType ItemType { get; set; }
  /// <summary>
  /// 被命中的互认项目编码；须命中当前组织启用且标准目录层级有效的互认配置，一次匹配只形成一条匹配项。
  /// </summary>
  public partial string StandardProjectCode { get; set; }
  /// <summary>
  /// 提供该匹配内容的报告标识。
  /// </summary>
  public partial Guid ReportId { get; set; }
  /// <summary>
  /// 提供该匹配内容的报告版本标识；须为当前有效版本。
  /// </summary>
  public partial Guid ReportVersionId { get; set; }
  /// <summary>
  /// 操作人标识；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial Guid OperId { get; set; }
  /// <summary>
  /// 操作时间；由服务端写入，不代表调用方提交值。
  /// </summary>
  public partial DateTimeOffset OperTime { get; set; }
}
