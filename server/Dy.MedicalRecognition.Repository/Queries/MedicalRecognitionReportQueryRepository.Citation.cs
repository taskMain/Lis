using Dy.Core.Abstractions.Modularity;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 获取引用详情所需的只读查询映射实现：本次就诊的候选采纳记录与报告公共上下文。
/// </summary>
/// <remarks>
/// 两条查询都只筛选与投影，不修改数据，也不判断业务状态。
/// 候选记录的有效期判定、报告版本有效性逐项校验与同版本同项目的择优都由应用层承担；
/// 报告公共上下文按本次要返回的版本集合一次读回，读取次数不随返回的匹配项数增长。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryRepository
{
  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionCitationCandidateItem>> QueryRecognitionCitationCandidatesAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo) =>
    await dataMapper.QueryAsync<RecognitionCitationCandidateItem>(new
    {
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      IdentityDocumentTypeCode = identityDocumentTypeCode,
      IdentityDocumentNo = identityDocumentNo,
      VisitType = visitType,
      VisitSerialNo = visitSerialNo
    }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionCitationReportContextItem>> QueryRecognitionCitationReportContextsAsync(IReadOnlyList<Guid> reportVersionIds) =>
    await dataMapper.QueryAsync<RecognitionCitationReportContextItem>(new
    {
      // 集合参数与匹配侧同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      ReportVersionIds = reportVersionIds
    }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<CitationStandardProjectNameItem>> QueryRecognitionCitationStandardProjectNamesAsync(
    IReadOnlyList<string> standardProjectCodes) =>
    await dataMapper.QueryAsync<CitationStandardProjectNameItem>(new
    {
      // 集合参数与前两条同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      StandardProjectCodes = standardProjectCodes
    }, scope: SqlScope);
}
