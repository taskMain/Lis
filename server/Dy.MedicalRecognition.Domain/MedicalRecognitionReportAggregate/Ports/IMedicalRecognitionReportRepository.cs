using Dy.Core.Abstractions.Domain;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 报告聚合写侧仓储端口：主键生成与各业务能力的读写契约。
/// </summary>
/// <remarks>
/// 端口按业务能力分为多个部分文件，本文件只放各能力共用的主键生成；
/// 业务能力契约见 <c>IMedicalRecognitionReportRepository.StandardCatalog.cs</c>、
/// <c>IMedicalRecognitionReportRepository.MutualRecognition.cs</c>、
/// <c>IMedicalRecognitionReportRepository.RecognitionAmount.cs</c> 与
/// <c>IMedicalRecognitionReportRepository.Submission.cs</c>。
/// 只做数据读写并返回行数或查询结果，不判断业务状态。
/// </remarks>
public partial interface IMedicalRecognitionReportRepository : IRepository
{
  /// <summary>
  /// 生成一个新的实体主键。
  /// </summary>
  /// <remarks>供领域层在写入前为分类、分组与标准项目赋值；生成值冲突由主键唯一性兜底。</remarks>
  /// <returns>新的主键标识。</returns>
  Guid CreateGuid();
}
