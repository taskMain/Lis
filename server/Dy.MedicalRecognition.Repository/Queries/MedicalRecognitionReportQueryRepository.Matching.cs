using Dy.Core.Abstractions.Modularity;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Dy.MedicalRecognition.Repository.Queries;

/// <summary>
/// 互认匹配查询所需的只读查询映射实现：候选报告筛选与匹配响应的报告事实读取。
/// </summary>
/// <remarks>
/// 两条查询都只筛选与投影，不修改数据，也不判断业务状态。
/// 候选报告的匹配基准时间、项目类型与标准项目编码由映射语句在两个报告类型分支内同源派生；
/// 报告事实按本次绑定的版本集合一次取回，读取次数不随命中项目数增长。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryRepository
{
  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionMatchCandidateReportItem>> QueryRecognitionMatchCandidateReportsAsync(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    Guid patientId,
    string identityDocumentTypeCode,
    string identityDocumentNo,
    VisitType visitType,
    string visitSerialNo,
    IReadOnlyList<string> standardProjectCodes) =>
    await dataMapper.QueryAsync<RecognitionMatchCandidateReportItem>(new
    {
      OrganizationCode = organizationCode,
      HospitalCode = hospitalCode,
      BranchCode = branchCode,
      PatientId = patientId,
      IdentityDocumentTypeCode = identityDocumentTypeCode,
      IdentityDocumentNo = identityDocumentNo,
      VisitType = visitType,
      VisitSerialNo = visitSerialNo,
      // 集合参数按映射语句声明的形态直接赋给同名属性：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      StandardProjectCodes = standardProjectCodes
    }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionMatchReportFactsItem>> QueryRecognitionMatchReportFactsAsync(IReadOnlyList<Guid> reportVersionIds) =>
    await dataMapper.QueryAsync<RecognitionMatchReportFactsItem>(new
    {
      // 集合参数与候选查询同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
      ReportVersionIds = reportVersionIds
    }, scope: SqlScope);

  /// <inheritdoc/>
  public async Task<IEnumerable<RecognitionValidReportVersionItem>> QueryValidRecognitionReportVersionIdsAsync(IReadOnlyList<Guid> reportVersionIds)
  {
    // 空集合必须在进入映射语句之前短路：该语句用集合参数写法，本平台实测观察到空集合在真实 Provider 上展开为空值列表、
    // 语句语法不成立（阶段 5 测试报告的票 08 段）。
    // 处理结果提交与引用详情在部分路径上会传入空版本集合，因此这里不把空集合交给映射器。
    if (reportVersionIds.Count == 0) return [];

    // 集合参数与前两条同一形态：参数名跟在 in 之后、不带括号，由框架展开为值列表。
    return await dataMapper.QueryAsync<RecognitionValidReportVersionItem>(new
    {
      ReportVersionIds = reportVersionIds
    }, scope: SqlScope);
  }
}
