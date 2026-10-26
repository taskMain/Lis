using System.Xml.Linq;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// T02 统计语句的组织范围投影守卫：数据语句只输出组织、医院、院区业务编码列，
/// 计数与原因聚合语句不输出这些列；逐条固定语句标识与投影别名。
/// </summary>
/// <remarks>
/// 组织范围以 IAM 组织节点上配置的业务编码为准，实例标识只在 IAM 目录读取与校验中使用，不落业务列、也不随查询结果输出；
/// 因此统计语句的投影、分组与排序键都必须是编码列，出现 <c>organization_id</c>、<c>hospital_id</c>、<c>branch_id</c> 即视为越过该口径。
/// 只读取仓储工程内的 SqlMap 文本，不连接数据库；每项断言按语句标识定位，避免新增语句让检查退化。
/// </remarks>
public sealed class T02StatisticsInstanceIdTests
{
  /// <summary>统计语句所在映射文件名。</summary>
  private const string SqlMapFile = "MedicalRecognitionReportQuery.xml";

  /// <summary>七条返回组织维度的统计数据语句，逐条断言只输出业务编码列。</summary>
  private static readonly string[] OrganizationScopedDataStatements =
  [
    "QueryRecognitionUsageSummaryPage",
    "QuerySourceRecognitionSummaryPage",
    "QueryRecognitionUsageReminderDetails",
    "QueryRecognitionUsageAdoptionDetails",
    "QueryRecognitionUsageNonAdoptionDetails",
    "QueryRecognitionUsageReferenceDetails",
    "QuerySourceRecognitionDetails"
  ];

  /// <summary>
  /// V16：七条统计数据语句逐条输出组织、医院与院区业务编码列，且不输出实例标识列。
  /// </summary>
  [Fact]
  public void Statistics_data_statements_project_organization_business_codes()
  {
    XDocument document = LoadDocument();

    foreach (string statementId in OrganizationScopedDataStatements)
    {
      // 组织范围列只允许业务编码：实例标识列已从业务表删除，投影它们会在真实运行中报列不存在。
      string statement = Statement(document, statementId);
      Assert.DoesNotContain("organization_id", statement, StringComparison.Ordinal);
      Assert.DoesNotContain("hospital_id", statement, StringComparison.Ordinal);
      Assert.DoesNotContain("branch_id", statement, StringComparison.Ordinal);
    }

    // 使用方汇总：分组键按维度取使用方三层业务编码，匹配记录侧同样取编码列。
    string usageSummary = Statement(document, "QueryRecognitionUsageSummaryPage");
    Assert.Contains("g.receiver_organization_code", usageSummary, StringComparison.Ordinal);
    Assert.Contains("g.receiver_hospital_code", usageSummary, StringComparison.Ordinal);
    Assert.Contains("g.receiver_branch_code", usageSummary, StringComparison.Ordinal);
    Assert.Contains("mr.receiver_organization_code", usageSummary, StringComparison.Ordinal);

    // 提供方汇总：分组键取报告主体上的提供方三层业务编码。
    string sourceSummary = Statement(document, "QuerySourceRecognitionSummaryPage");
    Assert.Contains("g.source_organization_code", sourceSummary, StringComparison.Ordinal);
    Assert.Contains("g.source_hospital_code", sourceSummary, StringComparison.Ordinal);
    Assert.Contains("g.source_branch_code", sourceSummary, StringComparison.Ordinal);

    // 四条使用方明细与一条提供方明细：使用方三层取匹配记录编码，提供方三层取报告主体编码并投影为同名别名。
    foreach (string statementId in new[]
    {
      "QueryRecognitionUsageReminderDetails",
      "QueryRecognitionUsageAdoptionDetails",
      "QueryRecognitionUsageNonAdoptionDetails",
      "QueryRecognitionUsageReferenceDetails",
      "QuerySourceRecognitionDetails"
    })
    {
      string statement = Statement(document, statementId);
      Assert.Contains("mr.receiver_organization_code", statement, StringComparison.Ordinal);
      Assert.Contains("mr.receiver_hospital_code", statement, StringComparison.Ordinal);
      Assert.Contains("mr.receiver_branch_code", statement, StringComparison.Ordinal);
      Assert.Contains("r.organization_code as source_organization_code", statement, StringComparison.Ordinal);
      Assert.Contains("r.hospital_code as source_hospital_code", statement, StringComparison.Ordinal);
      Assert.Contains("r.branch_code as source_branch_code", statement, StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// V16：七条计数语句与不采纳原因聚合语句只做计数或原因分组，不输出实例标识列。
  /// </summary>
  [Fact]
  public void Count_and_reason_statements_do_not_project_instance_ids()
  {
    XDocument document = LoadDocument();
    foreach (string statementId in new[]
    {
      "CountRecognitionUsageSummaryGroups",
      "CountSourceRecognitionSummaryGroups",
      "CountRecognitionUsageReminderDetails",
      "CountRecognitionUsageAdoptionDetails",
      "CountRecognitionUsageNonAdoptionDetails",
      "CountRecognitionUsageReferenceDetails",
      "CountSourceRecognitionDetails",
      "QueryRecognitionUsageSummaryReasons"
    })
    {
      Assert.DoesNotContain("organization_id", Statement(document, statementId), StringComparison.Ordinal);
      Assert.DoesNotContain("hospital_id", Statement(document, statementId), StringComparison.Ordinal);
      Assert.DoesNotContain("branch_id", Statement(document, statementId), StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// 读取统计映射文档。
  /// </summary>
  /// <returns>映射 XML 文档。</returns>
  private static XDocument LoadDocument() =>
    XDocument.Load(SourceSyntaxGuard.FindRepositoryFile("Queries", SqlMapFile));

  /// <summary>
  /// 按语句标识取单条语句的文本。
  /// </summary>
  /// <param name="document">映射文档。</param>
  /// <param name="statementId">语句标识。</param>
  /// <returns>语句文本。</returns>
  private static string Statement(XDocument document, string statementId) =>
    document.Descendants().Single(element => (string?)element.Attribute("Id") == statementId).Value;
}
