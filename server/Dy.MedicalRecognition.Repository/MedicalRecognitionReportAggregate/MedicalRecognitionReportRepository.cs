using Dy.Core.Abstractions.Data;
using Dy.Core.Abstractions.Models;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

namespace Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告聚合持久化实现的共用基础设施：映射器注入与标识生成。
/// </summary>
/// <remarks>
/// 仓储实现按业务能力分为多个部分文件，本文件只放各能力共用的基础设施，不承载具体语句调用；
/// 业务能力实现见 <c>MedicalRecognitionReportRepository.StandardCatalog.cs</c>、
/// <c>MedicalRecognitionReportRepository.MutualRecognition.cs</c>、
/// <c>MedicalRecognitionReportRepository.RecognitionAmount.cs</c> 与
/// <c>MedicalRecognitionReportRepository.Submission.cs</c>。
/// 并发冲突由数据库唯一索引兜底，本层不识别数据库错误码，也不把持久化异常翻译为领域异常。
/// </remarks>
public partial class MedicalRecognitionReportRepository : IMedicalRecognitionReportRepository, IHasDataMapper
{
  private IDataMapper dataMapper = default!;

  /// <summary>
  /// 框架写入的映射器实例，供本仓储的全部读写方法使用。
  /// </summary>
  public IDataMapper DataMapper { get => dataMapper; set => dataMapper = value; }

  /// <inheritdoc/>
  public Guid CreateGuid() => dataMapper.CreateGuid();
}
