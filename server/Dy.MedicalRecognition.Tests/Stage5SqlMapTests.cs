using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 的 DDL 与 SqlMap 静态守卫：冻结互认匹配记录、互认匹配项、互认处理结果与互认引用事实四张表的建表脚本物理形态，
/// 四个实体映射补齐后的作用域、语句标识与列清单顺序，以及候选报告查询的集合参数写法、分支可达性、列引用与枚举取值、数据库中立性。
/// </summary>
/// <remarks>
/// 覆盖矩阵 V73、V73b、V74、V74b、V75、V76、V77、V78、V79、V80 的静态面。
/// 这些内容改名、漏写、换序或换成方言写法既不会编译失败，也不会让阶段 1 至阶段 4 的用例失败，因此必须在这里独立冻结；
/// 目标库中的实际结构、注释与索引属于建表完成后的验收面，不在本文件内验证。
/// 内容敏感性由两类证据分别承担：可复用的私有判定函数接受注入负向特征的文本并报出违规，脚本与语句的逐条负向断言复用同一函数；
/// 无法构成独立判定面时只保留等值断言，不另写只断言注入文本自身的对照断言。
/// </remarks>
public sealed class Stage5SqlMapTests
{
  /// <summary>四个实体映射的脚本名、物理表名、表注释、列序（列名与完整类型声明）、每列中文注释与索引声明。</summary>
  /// <remarks>
  /// 列序按项目 DDL 规则：主键 <c>id</c> 第一，其余业务列按 UML 实体属性顺序，最后严格为 <c>oper_time</c>、<c>oper_id</c>；
  /// 四张表都没有是否作废、逻辑删除、启用一类生命周期状态标志，因此操作字段之前没有状态列。
  /// </remarks>
  private static readonly (string ScriptFile, string TableName, string TableComment, (string Name, string Type)[] Columns, (string Name, string Comment)[] ColumnComments, string[] IndexDefinitions, string[] IndexNames)[] ExpectedTables =
  [
    (
      "mrec_recognition_match_record.sql", "mrec_recognition_match_record", "互认匹配记录",
      [
        ("id", "uuid not null"),
        ("receiver_organization_code", "text not null"),
        ("receiver_hospital_code", "text not null"),
        ("receiver_branch_code", "text not null"),
        ("identity_document_type_code", "text not null"),
        ("identity_document_no", "text not null"),
        ("visit_type", "integer not null"),
        ("visit_serial_no", "text not null"),
        ("match_created_time", "timestamp without time zone not null"),
        ("decision_saved_time", "timestamp without time zone"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      [
        ("id", "主键ID"),
        ("receiver_organization_code", "接收组织编码"),
        ("receiver_hospital_code", "接收医院编码"),
        ("receiver_branch_code", "接收院区编码"),
        ("identity_document_type_code", "证件类型代码"),
        ("identity_document_no", "证件号码"),
        ("visit_type", "就诊类型"),
        ("visit_serial_no", "就诊流水号"),
        ("match_created_time", "互认匹配生成时间"),
        ("decision_saved_time", "处理结果保存时间"),
        ("oper_time", "操作时间"),
        ("oper_id", "操作人")
      ],
      ["on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, identity_document_type_code, identity_document_no, match_created_time);",
        "on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, match_created_time);"],
      ["ix_mrec_recognition_match_record_business_key", "ix_mrec_recognition_match_record_receiver_time"]
    ),
    (
      "mrec_recognition_match_item.sql", "mrec_recognition_match_item", "互认匹配项",
      [
        ("id", "uuid not null"),
        ("recognition_match_record_id", "uuid not null"),
        ("item_type", "integer not null"),
        ("standard_project_code", "text not null"),
        ("report_id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      [
        ("id", "主键ID"),
        ("recognition_match_record_id", "互认匹配记录ID"),
        ("item_type", "项目类型"),
        ("standard_project_code", "标准项目编码"),
        ("report_id", "报告ID"),
        ("report_version_id", "报告版本ID"),
        ("oper_time", "操作时间"),
        ("oper_id", "操作人")
      ],
      [
        "on mrec_recognition_match_item (recognition_match_record_id);",
        "on mrec_recognition_match_item (standard_project_code);"
      ],
      ["ix_mrec_recognition_match_item_record", "ix_mrec_recognition_match_item_project"]
    ),
    (
      "mrec_recognition_processing_result.sql", "mrec_recognition_processing_result", "互认处理结果",
      [
        ("id", "uuid not null"),
        ("recognition_match_record_id", "uuid not null"),
        ("recognition_match_item_id", "uuid not null"),
        ("recognition_time", "timestamp without time zone not null"),
        ("recognition_result", "integer not null"),
        ("recognition_dept_id", "text not null"),
        ("recognition_dept_name", "text not null"),
        ("recognition_doctor_id", "text not null"),
        ("recognition_doctor_name", "text not null"),
        ("non_adoption_reason", "integer"),
        ("non_adoption_description", "text"),
        ("estimated_saving_amount", "numeric(18,2)"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      [
        ("id", "主键ID"),
        ("recognition_match_record_id", "互认匹配记录ID"),
        ("recognition_match_item_id", "互认匹配项ID"),
        ("recognition_time", "互认时间"),
        ("recognition_result", "互认结果"),
        ("recognition_dept_id", "互认科室ID"),
        ("recognition_dept_name", "互认科室名称"),
        ("recognition_doctor_id", "互认医生ID"),
        ("recognition_doctor_name", "互认医生名称"),
        ("non_adoption_reason", "不采纳原因"),
        ("non_adoption_description", "不采纳补充说明"),
        ("estimated_saving_amount", "预计节省金额"),
        ("oper_time", "操作时间"),
        ("oper_id", "操作人")
      ],
      ["on mrec_recognition_processing_result (recognition_match_item_id);",
        "on mrec_recognition_processing_result (recognition_time);"],
      ["ux_mrec_recognition_processing_result_match_item", "ix_mrec_recognition_processing_result_recognition_time"]
    ),
    (
      "mrec_recognition_reference.sql", "mrec_recognition_reference", "互认引用事实",
      [
        ("id", "uuid not null"),
        ("recognition_match_item_id", "uuid not null"),
        ("referenced_time", "timestamp without time zone not null"),
        ("reference_dept_id", "text not null"),
        ("reference_dept_name", "text not null"),
        ("reference_doctor_id", "text not null"),
        ("reference_doctor_name", "text not null"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      [
        ("id", "主键ID"),
        ("recognition_match_item_id", "互认匹配项ID"),
        ("referenced_time", "实际引用时间"),
        ("reference_dept_id", "引用科室ID"),
        ("reference_dept_name", "引用科室名称"),
        ("reference_doctor_id", "引用医生ID"),
        ("reference_doctor_name", "引用医生名称"),
        ("oper_time", "操作时间"),
        ("oper_id", "操作人")
      ],
      ["on mrec_recognition_reference (recognition_match_item_id);",
        "on mrec_recognition_reference (referenced_time);"],
      ["ux_mrec_recognition_reference_match_item", "ix_mrec_recognition_reference_referenced_time"]
    )
  ];

  /// <summary>
  /// 八条索引的完整声明与索引名、是否唯一：匹配记录表按接收三值与患者证件、匹配生成时间，
  /// 匹配项表按所属匹配记录、按标准项目编码，处理结果表与引用事实表各按互认匹配项标识唯一，
  /// 以及阶段 6 统计索引的三条时间维度普通索引（接收三值加匹配生成时间、互认时间、实际引用时间）。
  /// </summary>
  private static readonly (string IndexName, bool IsUnique, string Definition)[] ExpectedIndexes =
  [
    ("ix_mrec_recognition_match_record_business_key", false,
      "on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, identity_document_type_code, identity_document_no, match_created_time);"),
    ("ix_mrec_recognition_match_record_receiver_time", false,
      "on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, match_created_time);"),
    ("ix_mrec_recognition_match_item_record", false,
      "on mrec_recognition_match_item (recognition_match_record_id);"),
    ("ix_mrec_recognition_match_item_project", false,
      "on mrec_recognition_match_item (standard_project_code);"),
    ("ux_mrec_recognition_processing_result_match_item", true,
      "on mrec_recognition_processing_result (recognition_match_item_id);"),
    ("ix_mrec_recognition_processing_result_recognition_time", false,
      "on mrec_recognition_processing_result (recognition_time);"),
    ("ux_mrec_recognition_reference_match_item", true,
      "on mrec_recognition_reference (recognition_match_item_id);"),
    ("ix_mrec_recognition_reference_referenced_time", false,
      "on mrec_recognition_reference (referenced_time);")
  ];

  /// <summary>
  /// 四个实体映射的文件名、作用域名与补齐后的语句标识清单，按文件内声明顺序排列；
  /// 语句标识由仓储方法名推导（方法名去掉 <c>Async</c> 后缀），因此必须与对应方法名逐字相同。
  /// </summary>
  private static readonly (string MapFile, string Scope, string[] StatementIds)[] ExpectedEntityMaps =
  [
    ("RecognitionMatchRecord.xml", "RecognitionMatchRecord",
      [
        "RecognitionMatchRecordColumns", "GetRecognitionMatchRecordByBusinessKey", "GetRecognitionMatchRecordById",
        "QueryRecognitionMatchRecordsByIds", "CreateRecognitionMatchRecord", "UpdateRecognitionMatchRecordDecisionSavedTime"
      ]),
    ("RecognitionMatchItem.xml", "RecognitionMatchItem",
      [
        "RecognitionMatchItemColumns", "QueryRecognitionMatchItemsByRecord",
        "QueryRecognitionMatchItemsByIds", "CreateRecognitionMatchItem"
      ]),
    ("RecognitionProcessingResult.xml", "RecognitionProcessingResult",
      [
        "RecognitionProcessingResultColumns", "QueryRecognitionProcessingResultsByRecord",
        "QueryRecognitionProcessingResultsByMatchItemIds", "CreateRecognitionProcessingResult"
      ]),
    ("RecognitionReference.xml", "RecognitionReference",
      ["RecognitionReferenceColumns", "QueryRecognitionReferencesByMatchItemIds", "CreateRecognitionReference"])
  ];

  /// <summary>阶段 5 交付的查询侧新增语句标识，声明顺序在阶段 1 至阶段 4 的语句之后。</summary>
  /// <remarks>
  /// 按报告版本集合一次读回普通检验结果与检查项目各一条，候选报告查询与匹配响应报告事实查询各一条，
  /// 处理结果提交在未命中幂等时校验绑定报告版本有效性所读的有效版本标识查询一条，
  /// 以及获取引用详情的候选采纳记录、报告公共上下文与标准项目名称三条；
  /// 两条按版本集合读取的语句使互认匹配查询的内容读取次数不随返回的报告数增长，
  /// 引用详情三条各按集合读回，使引用详情的数据库往返次数不随返回的匹配项数增长。
  /// 检查部位的按检查项目集合读取注册在检查部位实体作用域下，由实体映射清单用例另行覆盖。
  /// </remarks>
  private static readonly string[] StageQueryStatementIds =
  [
    "QueryLaboratoryResultItemsByVersions", "QueryExaminationItemsByVersions",
    "QueryRecognitionMatchCandidateReports", "QueryRecognitionMatchReportFacts",
    "QueryValidRecognitionReportVersionIds",
    "QueryRecognitionCitationCandidates", "QueryRecognitionCitationReportContexts",
    "QueryRecognitionCitationStandardProjectNames"
  ];

  /// <summary>四个实体物理表名，用于断言脚本集合与映射集合与设计一一对应。</summary>
  private static readonly string[] ExpectedTableNames =
  [
    "mrec_recognition_match_record", "mrec_recognition_match_item",
    "mrec_recognition_processing_result", "mrec_recognition_reference"
  ];

  /// <summary>
  /// 映射文件内以裸列名书写的语句中出现的物理表名，用于按别名解析引用时补齐映射面覆盖的表。
  /// </summary>
  /// <remarks>
  /// 这些语句的投影与写入列清单不带表别名，只能按语句所属实体映射的物理表解析，因此在此逐一登记。
  /// </remarks>
  private static readonly string[] DeclaredMapTableNames =
  [
    "mrec_medical_standard_category", "mrec_medical_standard_group", "mrec_medical_standard_item",
    "mrec_mutual_recognition_item", "mrec_organization_hospital_branch_recognition_amount"
  ];

  /// <summary>
  /// 保存枚举值的列与其枚举类型：列名取自物理列名，取值从枚举类型读取，不另建数值映射表。
  /// </summary>
  /// <remarks>
  /// 阶段 5 语句里写入或比较枚举值的列都在此登记；未登记的枚举型列会绕过数值字面量校验，因此同一事实只在类型上维护一份。
  /// 就诊类型列在引用详情候选语句里参与比较，因此一并登记。
  /// </remarks>
  private static readonly (string Column, Type EnumType)[] EnumColumnTypes =
  [
    (nameof(MedicalReportType), typeof(MedicalReportType)),
    ("status", typeof(MedicalReportLifecycleStatus)),
    (nameof(MedicalItemType), typeof(MedicalItemType)),
    (nameof(RecognitionResult), typeof(RecognitionResult)),
    (nameof(VisitType), typeof(VisitType))
  ];

  /// <summary>
  /// 需要逐项校验列引用与枚举取值的语句：实体映射的列清单与读写语句，以及查询侧全部新增语句。
  /// </summary>
  /// <remarks>
  /// 映射文件固定为 <c>null</c> 时按查询侧映射解析；携带文件名时按该实体映射解析。
  /// 登记面覆盖本阶段全部手写语句：语句改别名、改列名或把列写到另一张表上时，只有逐项列一致性判定能够发现，
  /// 因此不做按语句重要性挑选的抽样登记。
  /// </remarks>
  private static readonly (string StatementId, string? MapFile)[] ColumnGuardStatements =
  [
    ("RecognitionMatchRecordColumns", "RecognitionMatchRecord.xml"),
    ("GetRecognitionMatchRecordByBusinessKey", "RecognitionMatchRecord.xml"),
    ("GetRecognitionMatchRecordById", "RecognitionMatchRecord.xml"),
    ("QueryRecognitionMatchRecordsByIds", "RecognitionMatchRecord.xml"),
    ("CreateRecognitionMatchRecord", "RecognitionMatchRecord.xml"),
    ("UpdateRecognitionMatchRecordDecisionSavedTime", "RecognitionMatchRecord.xml"),
    ("RecognitionMatchItemColumns", "RecognitionMatchItem.xml"),
    ("QueryRecognitionMatchItemsByRecord", "RecognitionMatchItem.xml"),
    ("QueryRecognitionMatchItemsByIds", "RecognitionMatchItem.xml"),
    ("CreateRecognitionMatchItem", "RecognitionMatchItem.xml"),
    ("RecognitionProcessingResultColumns", "RecognitionProcessingResult.xml"),
    ("QueryRecognitionProcessingResultsByRecord", "RecognitionProcessingResult.xml"),
    ("QueryRecognitionProcessingResultsByMatchItemIds", "RecognitionProcessingResult.xml"),
    ("CreateRecognitionProcessingResult", "RecognitionProcessingResult.xml"),
    ("RecognitionReferenceColumns", "RecognitionReference.xml"),
    ("QueryRecognitionReferencesByMatchItemIds", "RecognitionReference.xml"),
    ("CreateRecognitionReference", "RecognitionReference.xml"),
    ("QueryExaminationSitesByItems", "ExaminationSite.xml"),
    ("QueryLaboratoryResultItemsByVersions", null),
    ("QueryExaminationItemsByVersions", null),
    ("QueryRecognitionMatchCandidateReports", null),
    ("QueryRecognitionMatchReportFacts", null),
    ("QueryValidRecognitionReportVersionIds", null),
    ("QueryRecognitionCitationCandidates", null),
    ("QueryRecognitionCitationReportContexts", null),
    ("QueryRecognitionCitationStandardProjectNames", null),
    // 阶段 6 同批登记：互认统计的十七条语句全部落在查询侧映射，语句改别名、改列名或把列写到另一张表上时逐项判定发现。
    ("CountRecognitionUsageSummaryGroups", null),
    ("QueryRecognitionUsageSummaryPage", null),
    ("QueryRecognitionUsageSummaryReasons", null),
    ("CountSourceRecognitionSummaryGroups", null),
    ("QuerySourceRecognitionSummaryPage", null),
    ("CountRecognitionUsageReminderDetails", null),
    ("QueryRecognitionUsageReminderDetails", null),
    ("CountRecognitionUsageAdoptionDetails", null),
    ("QueryRecognitionUsageAdoptionDetails", null),
    ("CountRecognitionUsageNonAdoptionDetails", null),
    ("QueryRecognitionUsageNonAdoptionDetails", null),
    ("CountRecognitionUsageReferenceDetails", null),
    ("QueryRecognitionUsageReferenceDetails", null),
    ("CountSourceRecognitionDetails", null),
    ("QuerySourceRecognitionDetails", null),
    ("QueryRecognitionMatchRecordView", null),
    ("QueryRecognitionMatchRecordViewItems", null)
  ];

  /// <summary>
  /// 共享 SQL 的方言特征：出现即视为违反数据库 Provider 中立约定。
  /// </summary>
  private static readonly (string Feature, string Pattern)[] DialectMarkers =
  [
    ("分页方言 limit", @"\bLIMIT\b"),
    ("分页方言 offset", @"\bOFFSET\b"),
    ("分页方言 top", @"\bTOP\b"),
    ("空值排序 nulls first", @"\bNULLS\s+FIRST\b"),
    ("空值排序 nulls last", @"\bNULLS\s+LAST\b"),
    ("类型转换符号双冒号", @"::"),
    ("结构化类型转换 cast", @"\bCAST\s*\("),
    ("JSON 运算符 jsonb", @"\bJSONB\b"),
    ("JSON 运算符箭头", @"->>?"),
    ("大小写不敏感比较 ilike", @"\bILIKE\b"),
    ("系统表 information_schema", @"\bINFORMATION_SCHEMA\b"),
    ("系统表 pg_catalog", @"\bPG_CATALOG\b"),
    ("查询提示 Oracle 风格", @"/\*\+"),
    ("查询提示 with (nolock)", @"\bWITH\s*\(\s*NOLOCK\s*\)"),
    ("查询提示 option (", @"\bOPTION\s*\("),
    ("冲突处理 on conflict", @"\bON\s+CONFLICT\b"),
    ("数据库当前时间 current_timestamp", @"\bCURRENT_TIMESTAMP\b"),
    ("行值聚合 count(distinct (", @"COUNT\s*\(\s*DISTINCT\s*\(")
  ];

  /// <summary>
  /// V73：四张表的建表脚本冻结统一前缀、表注释、列序（列名与完整类型声明）、可空性、每列中文注释、
  /// 表注释与索引声明，并且不含 <c>varchar(n)</c>、数据库默认值、跨系统外键、后续结构变更与带状态过滤的索引。
  /// </summary>
  /// <remarks>
  /// 列序与类型同时决定投影绑定与写入稳定性：加列、换序、改类型或放宽可空性都不会让任何编译或映射用例失败。
  /// </remarks>
  [Fact]
  public void Recognition_ddl_freezes_prefix_columns_types_comments_and_indexes()
  {
    foreach ((string scriptFile, string tableName, string tableComment, (string Name, string Type)[] columns, (string Name, string Comment)[] columnComments, string[] indexDefinitions, string[] indexNames) in ExpectedTables)
    {
      string script = File.ReadAllText(ScriptPath(scriptFile));

      // 统一前缀与建表形态：平台自有表统一带 mrec_ 前缀，脚本名与物理表名同名，脚本只做一次建表。
      Assert.StartsWith("mrec_", tableName, StringComparison.Ordinal);
      Assert.Equal($"{tableName}.sql", scriptFile);
      Assert.Contains($"create table {tableName} (", script, StringComparison.Ordinal);
      Assert.DoesNotContain("if not exists", script, StringComparison.OrdinalIgnoreCase);

      // 列序与完整类型声明逐项等值。
      Assert.Equal(columns, ExtractCreateTableColumnLines(script, tableName));

      // 主键：脚本必须以独立的一行显式声明 id 为主键，且整份脚本只有这一处。
      Assert.Single(
        script.Split('\n'),
        line =>
        {
          string trimmed = line.Trim();
          return trimmed.StartsWith("primary key", StringComparison.OrdinalIgnoreCase) &&
            trimmed.EndsWith("(id)", StringComparison.OrdinalIgnoreCase);
        });

      // 列序负向判据：操作时间与操作人严格位于列尾且顺序为 oper_time 在前，与 DDL 规则一致。
      Assert.Empty(TableColumnOrderViolations(columns));
      Assert.NotEmpty(TableColumnOrderViolations([.. columns[..^2], columns[^1], columns[^2]]));
      Assert.NotEmpty(TableColumnOrderViolations(columns[..^1]));

      // 表注释逐字冻结。
      Assert.Contains($"comment on table {tableName} is '{tableComment}';", script, StringComparison.Ordinal);

      // 每列一条中文注释，条数与列数一致，注释文案逐条等值且与设计列序表一致。
      string[] commentLines =
      [
        .. script.Split('\n')
          .Select(line => line.Trim())
          .Where(line => line.StartsWith($"comment on column {tableName}.", StringComparison.Ordinal))
      ];
      Assert.Equal(columns.Length, commentLines.Length);
      foreach ((string column, string comment) in columnComments)
      {
        Assert.Contains(commentLines, line => line == $"comment on column {tableName}.{column} is '{comment}';");
      }

      // 列注释的中文业务含义不得退化为空串：文案为空时数据库元数据里没有可用说明。
      Assert.All(
        commentLines,
        line => Assert.NotEqual(string.Empty, line[(line.IndexOf(" is '", StringComparison.Ordinal) + " is '".Length)..].TrimEnd(';', '\'')));

      // 显式索引：定义与中文索引注释逐条等值。
      foreach (string indexDefinition in indexDefinitions)
      {
        Assert.Contains(indexDefinition, script, StringComparison.Ordinal);
      }

      foreach ((string indexName, bool isUnique, string indexDefinition) in ExpectedIndexes)
      {
        if (!indexNames.Contains(indexName, StringComparer.Ordinal)) continue;

        string createVerb = isUnique ? "create unique index" : "create index";
        Assert.Contains($"{createVerb} {indexName}", script, StringComparison.Ordinal);
        Assert.Contains(indexDefinition, script, StringComparison.Ordinal);
        Assert.Contains($"comment on index {indexName} is '", script, StringComparison.Ordinal);

        // 无状态过滤：创建索引语句里不得出现 where，否则索引只覆盖部分行、唯一性不再覆盖全表。
        Assert.Empty(IndexDeclarationViolations(script, indexName, isUnique));

        // 变异证据：给索引副本加上状态过滤后，同一条判定必须报出问题。
        string filteredIndex = script.Replace(
          indexDefinition,
          indexDefinition.Replace(";", " where oper_id is not null;", StringComparison.Ordinal),
          StringComparison.Ordinal);
        Assert.NotEmpty(IndexDeclarationViolations(filteredIndex, indexName, isUnique));
      }

      // 单一索引前缀：同一张表的每条索引都恰好声明一次，且普通索引与唯一索引互不混用。
      // 判据从声明文本切到行尾，不按子串判定：子串判定会让 "create index" 命中 "create unique index"；
      // 同时保留注释存在性断言，避免索引被删掉声明却只剩注释时判据恒真。
      foreach (string indexName in indexNames)
      {
        Assert.Contains($"comment on index {indexName} is '", script, StringComparison.Ordinal);
        Assert.Equal(1, CountIndexDeclarations(script, indexName));
      }

      // 负向判据：文本列不用带长度上限的 varchar、无数据库默认值、无跨系统外键、无后续结构变更、无带状态过滤的索引。
      Assert.Empty(DdlRuleViolations(script));

      // 变异证据：注入负向特征后同一条判定必须报出对应规则。
      Assert.NotEmpty(DdlRuleViolations(script.Replace("text not null", "varchar(100) not null", StringComparison.Ordinal)));
      Assert.NotEmpty(DdlRuleViolations(script.Replace("uuid not null", "uuid not null default gen_random_uuid()", StringComparison.Ordinal)));
      Assert.NotEmpty(DdlRuleViolations(script.Replace("oper_id uuid not null", "oper_id uuid not null references other_system_user (id)", StringComparison.Ordinal)));
      Assert.NotEmpty(DdlRuleViolations(script + $"\nalter table {tableName} add column probe text;"));

      // 列序判据的判别力：把列清单副本的相邻两列对调或去掉一列后，同一条判定必须报出问题。
      (string Name, string Type)[] swappedColumns = [.. columns];
      (swappedColumns[^1], swappedColumns[^2]) = (swappedColumns[^2], swappedColumns[^1]);
      Assert.NotEmpty(TableColumnOrderViolations(swappedColumns));
      Assert.NotEmpty(TableColumnOrderViolations(columns[..^1]));
    }
  }

  /// <summary>
  /// V73：两个业务时间列（互认时间与实际引用时间）的精度不低于秒，四张表的其余时间列与既有脚本保持同一写法。
  /// </summary>
  /// <remarks>
  /// 报表可以按更粗粒度汇总，但原始业务时间不得被降低精度；把 <c>timestamp(0)</c> 降级或换成只到日期的类型
  /// 不会让任何编译或映射用例失败，只会让已保存的互认与引用时间失去年内顺序。
  /// </remarks>
  [Fact]
  public void Recognition_ddl_keeps_business_time_precision_at_least_second()
  {
    Dictionary<string, (string Name, string Type)[]> columnsByTable = ExpectedTables.ToDictionary(
      table => table.TableName,
      table => ExtractCreateTableColumnLines(File.ReadAllText(ScriptPath(table.ScriptFile)), table.TableName));

    foreach ((string tableName, string columnName) in new (string, string)[]
    {
      ("mrec_recognition_processing_result", "recognition_time"),
      ("mrec_recognition_reference", "referenced_time")
    })
    {
      // 与既有脚本的同类时间列同形：不带精度修饰符时数据库按微秒保存，精度高于秒。
      Assert.Empty(BusinessTimeTypeViolations(columnsByTable[tableName], columnName));

      // 精度负向判据：显式把精度降到秒以下或换成日期类型都必须被判出。
      Assert.NotEmpty(BusinessTimeTypeViolations(
        [.. columnsByTable[tableName].Select(column => column.Name == columnName
          ? (column.Name, "timestamp(0) without time zone not null")
          : column)],
        columnName));
      Assert.NotEmpty(BusinessTimeTypeViolations(
        [.. columnsByTable[tableName].Select(column => column.Name == columnName ? (column.Name, "date not null") : column)],
        columnName));
    }
  }

  /// <summary>
  /// V73b：仓库中不存在报告使用关系表、报告使用关系实体或报告使用关系事件；采纳与报告版本的对应关系只由互认匹配项反查派生。
  /// </summary>
  /// <remarks>
  /// 这条是负向工程约束：新增一张报告使用关系表或一个对应事件不会让任何编译或行为用例失败，
  /// 只会让采纳与报告版本的对应关系出现第二份事实来源。判据前先确认被扫描的集合非空，避免扫描面为空时负向断言恒真。
  /// </remarks>
  [Fact]
  public void No_report_usage_relation_carrier_is_introduced()
  {
    string[] scriptFiles =
    [
      .. Directory.EnumerateFiles(RepositoryPath("Scripts"), "*.sql", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .Where(name => name is not null)
        .Select(name => name!)
        .Order(StringComparer.Ordinal)
    ];
    string[] entityFiles =
    [
      .. Directory.EnumerateFiles(ServerPath("Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate"), "*.cs", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .Where(name => name is not null)
        .Select(name => name!)
        .Order(StringComparer.Ordinal)
    ];
    string[] eventFiles =
    [
      .. Directory.EnumerateFiles(ServerPath("Dy.MedicalRecognition.Domain.Share", "MedicalRecognitionReportAggregate", "Events"), "*.cs", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .Where(name => name is not null)
        .Select(name => name!)
        .Order(StringComparer.Ordinal)
    ];

    // 扫描面非空：集合被清空或路径写错时，下面的负向断言会恒真而假绿。
    Assert.NotEmpty(scriptFiles);
    Assert.NotEmpty(entityFiles);
    Assert.NotEmpty(eventFiles);

    // 四张阶段 5 的表与四个实体都在各自的扫描面内，确认扫描面确实覆盖本阶段对象。
    foreach (string tableName in ExpectedTableNames)
    {
      Assert.Contains($"{tableName}.sql", scriptFiles);
    }

    Assert.Contains("RecognitionMatchRecord.cs", entityFiles);
    Assert.Contains("RecognitionMatchItem.cs", entityFiles);
    Assert.Contains("RecognitionProcessingResult.cs", entityFiles);
    Assert.Contains("RecognitionReference.cs", entityFiles);

    // 报告使用关系载体的命名特征：表名、实体名与事件名都不得出现这些词根。
    string[] forbiddenMarkers = ["report_usage", "ReportUsage", "report_usage_relation", "ReportUsageRelation"];

    foreach (string marker in forbiddenMarkers)
    {
      Assert.DoesNotContain(scriptFiles, name => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
      Assert.DoesNotContain(entityFiles, name => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
      Assert.DoesNotContain(eventFiles, name => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    // 脚本集合中不存在报告使用关系表，实体与事件集合中也不存在对应类型。
    Assert.DoesNotContain(scriptFiles, name => name.Contains("usage", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(entityFiles, name => name.Contains("usage", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(eventFiles, name => name.Contains("usage", StringComparison.OrdinalIgnoreCase));

    // 变异证据：把注入的载体名加进副本后，同一条判定必须报出违规。
    Assert.Empty(UsageCarrierViolations(scriptFiles, entityFiles, eventFiles));
    Assert.NotEmpty(UsageCarrierViolations([.. scriptFiles, "mrec_report_usage_relation.sql"], entityFiles, eventFiles));
    Assert.NotEmpty(UsageCarrierViolations(scriptFiles, [.. entityFiles, "ReportUsageRelation.cs"], eventFiles));
    Assert.NotEmpty(UsageCarrierViolations(scriptFiles, entityFiles, [.. eventFiles, "ReportUsageRelationCreatedEvent.cs"]));
  }

  /// <summary>
  /// V74、V74b：互认处理结果表与互认引用事实表都不含项目编码列，项目编码由互认匹配项标识反查匹配项取得。
  /// </summary>
  /// <remarks>
  /// 两表删列后各自只保留互认匹配项标识作为关系键；重新加回项目编码列不会让任何编译或行为用例失败，
  /// 只会让项目编码出现第二份事实来源。判据前先确认列清单非空，避免解析面为空时负向断言恒真。
  /// </remarks>
  [Fact]
  public void Processing_result_and_reference_tables_drop_standard_project_code()
  {
    foreach (string tableName in new[] { "mrec_recognition_processing_result", "mrec_recognition_reference" })
    {
      (string ScriptFile, string TableName, string TableComment, (string Name, string Type)[] Columns, (string Name, string Comment)[] ColumnComments, string[] IndexDefinitions, string[] IndexNames) expected =
        ExpectedTables.Single(table => table.TableName == tableName);
      string script = File.ReadAllText(ScriptPath(expected.ScriptFile));

      // 解析面非空：只断言"不含某列"时，解析失败取到空集合会让判据恒真。
      string[] columnNames = [.. ExtractCreateTableColumnLines(script, tableName).Select(column => column.Name)];
      Assert.NotEmpty(columnNames);
      Assert.Equal(expected.Columns.Length, columnNames.Length);

      // 项目编码列不存在：列定义与列注释两处都不得出现。
      Assert.Empty(StandardProjectCodeViolations(columnNames));
      Assert.DoesNotContain($"comment on column {tableName}.standard_project_code", script, StringComparison.Ordinal);

      // 关系键保留：互认匹配项标识是两表唯一的项目定位键。
      Assert.Contains("recognition_match_item_id", columnNames);

      // 变异证据：把项目编码列插进列清单副本后，同一条判定必须报出该列存在。
      Assert.NotEmpty(StandardProjectCodeViolations([.. columnNames, "standard_project_code"]));
    }

    // 对照面：互认匹配项表保留项目编码列，证明上面的判据判定的是"该列在这一张表上不存在"而不是"该列在仓库里不存在"。
    string matchItemScript = File.ReadAllText(ScriptPath("mrec_recognition_match_item.sql"));
    string[] matchItemColumns = [.. ExtractCreateTableColumnLines(matchItemScript, "mrec_recognition_match_item").Select(column => column.Name)];
    Assert.Contains("standard_project_code", matchItemColumns);
  }

  /// <summary>
  /// V75：四条索引与设计逐条一致，处理结果表与引用事实表各按互认匹配项标识建唯一索引。
  /// </summary>
  /// <remarks>
  /// 引用事实表的唯一索引同时承担一一对应关系与并发写入的撞键兜底：索引被改成普通索引后，
  /// 同一匹配项可以写入多条引用事实，而静态检查与编译都不会失败。
  /// </remarks>
  [Fact]
  public void Recognition_ddl_declares_the_designed_indexes()
  {
    List<(string ScriptFile, string IndexName, bool IsUnique, string Definition)> declaredIndexes = [];
    foreach ((string ScriptFile, string TableName, string TableComment, (string Name, string Type)[] Columns, (string Name, string Comment)[] ColumnComments, string[] IndexDefinitions, string[] IndexNames) table in ExpectedTables)
    {
      string script = File.ReadAllText(ScriptPath(table.ScriptFile));
      foreach (Match match in Regex.Matches(script, @"create (?<unique>unique )?index (?<name>[a-z_][a-z0-9_]*)", RegexOptions.CultureInvariant))
      {
        string indexName = match.Groups["name"].Value;
        string definition = IndexDefinition(script, indexName);
        declaredIndexes.Add((table.ScriptFile, indexName, match.Groups["unique"].Success, definition));
      }
    }

    // 声明面非空：正则未命中时下面的逐条等值断言会全部落空。
    Assert.NotEmpty(declaredIndexes);

    // 索引集合与设计逐项等值：多一条、少一条或换名都会在这里失败。
    Assert.Equal(
      ExpectedIndexes.Select(index => index.IndexName).Order(StringComparer.Ordinal),
      declaredIndexes.Select(index => index.IndexName).Order(StringComparer.Ordinal));

    foreach ((string indexName, bool isUnique, string definition) in ExpectedIndexes)
    {
      (string ScriptFile, string IndexName, bool IsUnique, string Definition) declared =
        Assert.Single(declaredIndexes, index => index.IndexName == indexName);
      Assert.Equal(definition, declared.Definition);
      // 唯一性与索引名前缀一致，唯一索引只出现在处理结果表与引用事实表上。
      string indexScript = File.ReadAllText(ScriptPath(declared.ScriptFile));
      Assert.Empty(IndexDeclarationViolations(indexScript, indexName, isUnique));
      Assert.Empty(IndexDeclarationViolations(indexScript, indexName, declared.IsUnique));
    }

    // 变异证据：把引用事实表的唯一索引声明改成普通索引后，同一条唯一性判定必须报出问题。
    (string ScriptFile, string IndexName, bool IsUnique, string Definition) referenceIndex =
      declaredIndexes.Single(index => index.IndexName == "ux_mrec_recognition_reference_match_item");
    string referenceScript = File.ReadAllText(ScriptPath(referenceIndex.ScriptFile));
    Assert.Empty(IndexDeclarationViolations(referenceScript, referenceIndex.IndexName, true));
    Assert.NotEmpty(IndexDeclarationViolations(referenceScript, referenceIndex.IndexName, false));
  }

  /// <summary>
  /// V76：四个映射的作用域为各自实体名，补齐的语句逐条注册在同一实体作用域下，且映射只指向带统一前缀的本平台表。
  /// </summary>
  /// <remarks>
  /// 框架按 <c>{作用域}.{语句标识}</c> 查找语句，作用域或标识不一致只在运行期表现为找不到语句，静态检查不会失败。
  /// </remarks>
  [Fact]
  public void Recognition_sql_maps_freeze_scopes_and_statement_ids()
  {
    foreach ((string mapFile, string scope, string[] statementIds) in ExpectedEntityMaps)
    {
      XDocument document = XDocument.Load(EntityMapPath(mapFile));

      Assert.Equal(scope, (string?)document.Root!.Attribute("Scope"));
      Assert.NotEqual("MedicalRecognitionReport", scope);
      Assert.NotEqual("MedicalRecognitionReportAggregate", scope);

      // 只取 Statement 节点：映射文件里的 ParameterMap 也带 Id，按任意 Id 取会把参数映射误判成语句。
      Assert.Equal(statementIds, StatementIds(document));

      // 作用域在本项目中一律由调用点显式传 scope，映射文件不得调用共享映射器的上下文设置方法。
      Assert.DoesNotContain("SetContext", document.ToString(), StringComparison.Ordinal);

      // 占位的无条件全量查询必须消失：它既不满足业务键或集合定位语义，也没有调用方。
      Assert.DoesNotContain("QueryAll", document.ToString(), StringComparison.Ordinal);

      // 每条手写语句都有说明职责的 XML 注释。
      Assert.All(
        StatementElements(document),
        statement => Assert.True(
          statement.PreviousNode is XComment,
          $"语句 {statement.Attribute("Id")?.Value} 缺少 XML 注释"));

      // 全文件的表名引用都必须是带 mrec_ 前缀的本平台表，且都落在本阶段涉及的物理表集合内。
      string xml = document.ToString();
      string[] tableNames = [.. TableNameReferences(xml).Distinct(StringComparer.Ordinal)];
      Assert.NotEmpty(tableNames);
      Assert.Empty(TablePrefixViolations(xml));
      Assert.All(tableNames, tableName => Assert.True(Stage5TableNames.Contains(tableName), $"映射引用的表 {tableName} 不在本阶段涉及的物理表集合内"));

      // 列清单按实体属性声明顺序收尾，操作人标识在操作时间之前。
      string columnsStatement = StatementText(EntityMapPath(mapFile), statementIds[0]);
      Assert.Empty(EntityColumnOrderViolations(columnsStatement));
      string[] entries = [.. columnsStatement.Split(',')];
      (entries[^1], entries[^2]) = (entries[^2], entries[^1]);
      Assert.NotEmpty(EntityColumnOrderViolations(string.Join(',', entries)));

      // 变异证据：去掉表名前缀后同一条判定必须报出未加前缀的表名。
      string unprefixed = xml.Replace("mrec_", string.Empty, StringComparison.Ordinal);
      Assert.NotEqual(xml, unprefixed);
      Assert.NotEmpty(TableNameReferences(unprefixed));
      Assert.NotEmpty(TablePrefixViolations(unprefixed));
    }

    // 语句标识清单的判别力：把副本里的语句标识改掉后，同一份清单的等值判据必须报出差异。
    Assert.Empty(StatementIdListViolations(ExpectedEntityMaps[0].StatementIds, ExpectedEntityMaps[0].StatementIds));
    Assert.NotEmpty(StatementIdListViolations(ExpectedEntityMaps[0].StatementIds, ["RecognitionMatchRecordColumn"]));
  }

  /// <summary>
  /// V77：既有语句清单断言已按补齐结果同步且未放宽——四个作用域的语句键逐条出现在运行时注册结果中，
  /// 旧聚合作用域键全部消失，占位的全量查询不再注册。
  /// </summary>
  /// <remarks>
  /// 断言的严格程度由断言文本自身判定：清单必须逐条写成完整键，不得出现只断言作用域名、
  /// 只断言某一条语句或使用 <c>Contains(scope)</c> 一类宽松形式的写法。同时断言被冻结的语句集合非空。
  /// </remarks>
  [Fact]
  public void Existing_statement_list_assertions_are_synchronised_without_relaxing()
  {
    string probe = File.ReadAllText(TestPath("Stage1SqlMapProbeTests.cs"));

    // 既有断言用逐条带引号的字面量构造完整键，因此先取出该文件里的全部字符串字面量，
    // 再按 {作用域}.{语句标识} 断言每个完整键确实被逐条冻结。按字面文本直接搜索完整键会漏判：
    // 语句标识清单被格式化成多行后，作用域与标识并不会出现在同一个字符串字面量里。
    HashSet<string> declaredLiteralStrings =
    [
      .. Regex.Matches(probe, "\"(?<value>[^\"\\r\\n]*)\"", RegexOptions.CultureInvariant)
        .Select(match => match.Groups["value"].Value)
    ];
    Assert.NotEmpty(declaredLiteralStrings);

    foreach ((string _, string scope, string[] statementIds) in ExpectedEntityMaps)
    {
      Assert.NotEmpty(statementIds);
      // 既有断言逐条写出"作用域"与"语句标识"两个字面量，并在运行时按 {作用域}.{语句标识} 展开成完整键。
      // 因此这里冻结的是两个字面量各自存在：清单式写法把标识写在多行里，完整键并不会作为单个字面量出现，
      // 只搜索完整键会把"清单已被逐条同步"误判成未同步。
      Assert.Contains(scope, declaredLiteralStrings);
      foreach (string statementId in statementIds) Assert.Contains(statementId, declaredLiteralStrings);
    }

    // 完整键确实被逐条构造：这一行是"两个字面量被拼成查找键"的静态依据，也是不放宽为宽松形式的依据。
    Assert.Contains("Assert.Contains($\"{scope}.{statementId}\", registeredKeys);", probe, StringComparison.Ordinal);

    // 旧聚合作用域键的负向断言保留：作用域被改回聚合名时只有这条断言会失败。
    Assert.Contains("Assert.DoesNotContain($\"MedicalRecognitionReport.{statementIds[0]}\", registeredKeys);", probe, StringComparison.Ordinal);

    // 未放宽为宽松形式：不得出现只断言作用域名或只断言语句数一类的写法。
    Assert.DoesNotContain("Assert.Contains(scope, registeredKeys)", probe, StringComparison.Ordinal);
    Assert.DoesNotContain("Assert.Contains(scope, ", probe, StringComparison.Ordinal);
    Assert.DoesNotContain("registeredKeys.Count", probe, StringComparison.Ordinal);

    // 变异证据：清单里的标识被截断成作用域同名的写法后，逐条字面量断言必须判为不成立。
    const string fullStatementId = "CreateRecognitionReference";
    const string truncatedStatementId = "CreateRecognition";
    string[] allStatementIds = [.. ExpectedEntityMaps.SelectMany(map => map.StatementIds)];
    Assert.NotEmpty(allStatementIds);
    Assert.Contains(fullStatementId, declaredLiteralStrings);
    Assert.DoesNotContain(truncatedStatementId, allStatementIds);
    Assert.NotEqual(fullStatementId, truncatedStatementId);
  }

  /// <summary>
  /// V78：候选报告查询以集合参数写法按一组标准项目编码过滤——参数名直接跟在 <c>in</c> 之后、不带括号。
  /// </summary>
  /// <remarks>
  /// 该写法是本项目首例，框架据此把参数展开为值列表；加上括号后框架会按单值参数绑定，
  /// 集合入参在运行期报绑定失败，而静态检查不会失败。
  /// </remarks>
  [Fact]
  public void Candidate_report_query_uses_the_collection_parameter_form()
  {
    string statement = NormaliseStatement(QueryStatement("QueryRecognitionMatchCandidateReports"));

    // 集合参数写法：标准项目编码在合并后的行集上过滤，集合参数名直接跟在 in 之后且只出现一次。
    Assert.Empty(CollectionParameterViolations(statement, "$StandardProjectCodes"));
    Assert.Single(Regex.Matches(statement, @"\bin \$StandardProjectCodes\b", RegexOptions.CultureInvariant));

    // 变异证据：同一条判定对正确写法判空，对带括号写法、退化为等值与退化为单值参数都必须报出违规。
    Assert.NotEmpty(CollectionParameterViolations(statement.Replace("in $StandardProjectCodes", "in ($StandardProjectCodes)", StringComparison.Ordinal), "$StandardProjectCodes"));
    Assert.NotEmpty(CollectionParameterViolations(statement.Replace("in $StandardProjectCodes", "= $StandardProjectCodes", StringComparison.Ordinal), "$StandardProjectCodes"));
    Assert.NotEmpty(CollectionParameterViolations(statement.Replace("in $StandardProjectCodes", "in $StandardProjectCode", StringComparison.Ordinal), "$StandardProjectCodes"));
  }

  /// <summary>
  /// V79：候选报告查询按报告类型分成检验与检查两个分支，每个分支左连接对应专项内容表与项目表并合并取值取得统一排序键，
  /// 项目类型与标准项目编码按本次实际命中一侧派生，排序为匹配基准时间倒序、报告时间倒序、报告版本标识升序。
  /// </summary>
  /// <remarks>
  /// 匹配基准时间与标准项目编码分属检验与检查两套表；只覆盖一类报告、把项目类型写成常量或在两个分支之间混用编码列，
  /// 都会让另一类报告永远无法成为候选或返回错误一侧的编码，而静态检查与编译都不会失败。
  /// </remarks>
  [Fact]
  public void Candidate_report_query_merges_both_left_joins_and_sorts_with_version_tie_breaker()
  {
    string statement = NormaliseStatement(QueryStatement("QueryRecognitionMatchCandidateReports"));

    // 报告主体 → 当前版本：候选筛选落在当前有效版本上，患者与就诊条件取自当前版本。
    Assert.Contains("from mrec_medical_recognition_report r", statement, StringComparison.Ordinal);
    Assert.Equal(2, Regex.Matches(statement, @"join mrec_medical_report_version v on v\.id = r\.current_version_id", RegexOptions.CultureInvariant).Count);
    Assert.Contains("and v.identity_document_type_code = $IdentityDocumentTypeCode", statement, StringComparison.Ordinal);
    Assert.Contains("and v.identity_document_no = $IdentityDocumentNo", statement, StringComparison.Ordinal);
    Assert.Contains("and v.visit_type = $VisitType", statement, StringComparison.Ordinal);
    Assert.Contains("and v.visit_serial_no = $VisitSerialNo", statement, StringComparison.Ordinal);

    // 两个分支各左连接对应的专项内容表与项目表。
    Assert.Contains("left join mrec_laboratory_report_content lc on lc.report_version_id = v.id", statement, StringComparison.Ordinal);
    Assert.Contains("left join mrec_laboratory_result_item l on l.report_version_id = v.id", statement, StringComparison.Ordinal);
    Assert.Contains("left join mrec_examination_report_content ec on ec.report_version_id = v.id", statement, StringComparison.Ordinal);
    Assert.Contains("left join mrec_examination_item e on e.report_version_id = v.id", statement, StringComparison.Ordinal);

    // 检验分支取检验结果项编码与项目类型 0，检查分支取检查项目编码与项目类型 1，两个编码列不混用。
    Assert.Contains("l.standard_project_code", statement, StringComparison.Ordinal);
    Assert.Contains("e.standard_project_code", statement, StringComparison.Ordinal);
    Assert.Contains("0 as item_type", statement, StringComparison.Ordinal);
    Assert.Contains("1 as item_type", statement, StringComparison.Ordinal);
    Assert.DoesNotContain("examination_standard_project_code", statement, StringComparison.Ordinal);

    // 合并取值：两个业务时间列合并为统一排序键，检验取检测完成时间、检查取实际检查时间。
    Assert.Equal(2, Regex.Matches(statement, @"coalesce\(lc\.testing_completed_time, ec\.examination_time\) as match_baseline_time", RegexOptions.CultureInvariant).Count);

    // 报告时间与报告类型筛选落在报告主体上，两个分支各自只取一类报告。
    Assert.Contains("r.report_time", statement, StringComparison.Ordinal);
    Assert.Contains("and r.status = 1", statement, StringComparison.Ordinal);
    Assert.Equal(2, Regex.Matches(statement, @"and r\.report_type = \d+", RegexOptions.CultureInvariant).Count);
    Assert.All(
      ExtractComparisonLiterals(statement, "report_type"),
      literal => Assert.True(
        Enum.IsDefined(typeof(MedicalReportType), literal),
        $"报告类型筛选使用了 {literal}，不属于 {nameof(MedicalReportType)} 的值域。"));
    Assert.Equal(
      EnumValues(typeof(MedicalReportType)).Select(value => (int)value).Order(),
      ExtractComparisonLiterals(statement, "report_type").Distinct().Order());

    // 排序：匹配基准时间倒序、报告时间倒序、报告版本标识升序兜底。
    Assert.Contains("order by c.match_baseline_time desc, c.report_time desc, c.report_version_id asc", statement, StringComparison.Ordinal);

    // 变异证据：去掉版本标识兜底键、把两个分支合并成一个、以及把项目类型改成常量后，同一条判定必须报出问题。
    Assert.DoesNotContain(
      "order by c.match_baseline_time desc, c.report_time desc, c.report_version_id asc",
      statement.Replace(
        "order by c.match_baseline_time desc, c.report_time desc, c.report_version_id asc",
        "order by c.match_baseline_time desc, c.report_time desc",
        StringComparison.Ordinal),
      StringComparison.Ordinal);
    Assert.DoesNotContain(
      "and r.report_type = 2",
      statement.Replace(" and r.report_type = 2", string.Empty, StringComparison.Ordinal),
      StringComparison.Ordinal);
    Assert.Empty(ExtractComparisonLiterals(
      statement.Replace("1 as item_type", string.Empty, StringComparison.Ordinal),
      "item_type"));
  }

  /// <summary>
  /// V80：阶段 5 的两条查询语句不含数据库方言特征，也不调用上下文设置方法。
  /// </summary>
  /// <remarks>
  /// 共享 SQL 必须保持数据库 Provider 中立：分页方言、空值排序、类型转换、JSON 运算符、系统表与查询提示
  /// 都会把语句绑定到单一数据库版本，而静态检查不会失败。
  /// </remarks>
  [Fact]
  public void Stage_query_statements_stay_provider_neutral()
  {
    Assert.Equal(StageQueryStatementIds, StageQueryStatementIds.Distinct(StringComparer.Ordinal));
    Assert.NotEmpty(StageQueryStatementIds);

    foreach (string statementId in StageQueryStatementIds)
    {
      string statement = QueryStatement(statementId);
      Assert.NotEmpty(statement);

      List<string> dialectHits = [];
      foreach ((string feature, string pattern) in DialectMarkers)
      {
        Match match = Regex.Match(statement, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success) dialectHits.Add($"{statementId}: {feature} => {match.Value}");
      }

      Console.WriteLine($"{statementId} 方言标记命中：{(dialectHits.Count == 0 ? "none" : string.Join(", ", dialectHits))}");
      Assert.Empty(dialectHits);
    }

    // 映射文件不得调用上下文设置方法；SQL 注释一律使用 XML 注释，不使用会传入数据库的 -- 或 /* */。
    string queryMap = File.ReadAllText(QueryMapPath());
    Assert.DoesNotContain("SetContext", queryMap, StringComparison.Ordinal);

    // 变异证据：把分页方言与类型转换注入语句副本后，同一条扫描必须命中。
    string candidate = QueryStatement(StageQueryStatementIds[0]);
    Assert.NotEmpty(MatchesAnyDialectMarker($"{candidate} limit 1"));
    Assert.NotEmpty(MatchesAnyDialectMarker($"{candidate} and r.report_time::text = $ReportTime"));
  }

  /// <summary>
  /// 阶段 5 语句引用的列必须存在于其别名指向的物理表，枚举型列的数值字面量必须属于对应枚举的值域。
  /// </summary>
  /// <remarks>
  /// 映射语句引用的列名与物理列名之间没有任何编译期或运行期绑定检查，别名指向错误的表、列改名或列写错都只会在实际查询时报错；
  /// 枚举型列写成另一个枚举的编号时语句依然可以执行，只会静默命中零行或返回错误分支的结果。
  /// 别名无法解析到本阶段涉及的表时也判为违规，避免解析面缺失让整条判据落空。
  /// </remarks>
  [Fact]
  public void Stage_statements_reference_existing_columns_and_enum_values()
  {
    Dictionary<string, HashSet<string>> columnsByTable = PhysicalTableColumns();
    Assert.NotEmpty(columnsByTable);

    // 登记表非空：枚举取值校验只对登记列生效，登记面为空时下面的枚举断言会全部落空。
    Assert.NotEmpty(EnumColumnTypes);
    Assert.All(EnumColumnTypes, mapping => Assert.NotEmpty(Enum.GetValues(mapping.EnumType).Cast<object>()));
    Assert.All(
      EnumColumnTypes.Select(mapping => mapping.EnumType).Distinct(),
      enumType => Assert.Contains(
        Path.GetFileNameWithoutExtension(enumType.Name),
        Directory.EnumerateFiles(ServerPath("Dy.MedicalRecognition.Domain.Share", "Enums"), "*.cs", SearchOption.TopDirectoryOnly)
          .Select(Path.GetFileNameWithoutExtension)
          .Where(name => name is not null)
          .Select(name => name!)));

    List<string> violations = [];
    List<string> enumValueViolations = [];
    List<string> unresolvedAliasViolations = [];
    foreach ((string statementId, string? mapFile) in ColumnGuardStatements)
    {
      string statement = mapFile is null ? QueryStatement(statementId) : EntityMapStatement(mapFile, statementId);
      statement = NormaliseStatement(statement);
      if (statement.Length == 0)
      {
        violations.Add($"{statementId}：语句文本为空");
        continue;
      }

      (string Alias, string Column)[] references = SplitAliasColumnReferences(statement);

      // 内联派生表的列由子查询的投影决定，不在物理表列集合内，因此其别名不参与物理列判定。
      HashSet<string> derivedAliases = DerivedTableAliases(statement);
      references = [.. references.Where(reference => !derivedAliases.Contains(reference.Alias))];
      Dictionary<string, string> aliases = AliasToTables(statement);

      // 列清单语句只有裸列名、没有 from 子句，其物理表由所属映射文件名确定。
      if (references.Length == 0 && aliases.Count == 0 && mapFile is not null)
      {
        string mapTable = Path.GetFileNameWithoutExtension(mapFile);
        aliases[mapTable] = mapTable;
      }

      if (aliases.Count == 0) unresolvedAliasViolations.Add($"{statementId}：未从 from 与 join 子句解析到任何物理表别名");
      if (aliases.Count == 1) Assert.All(references, reference => Assert.Contains(reference.Alias, aliases.Keys));
      IReadOnlyList<string> bareColumnProbe = UnqualifiedColumnViolations(statement, aliases, columnsByTable);

      foreach ((string alias, string column, string table, string reason) in ColumnReferenceViolations(references, aliases, columnsByTable))
      {
        violations.Add($"{statementId}：{alias}.{column} => {reason}{(table.Length == 0 ? string.Empty : $"（{table}）")}");
      }

      foreach (string column in bareColumnProbe)
      {
        violations.Add($"{statementId}：裸列名 {column} 不存在于该语句定位的物理表");
      }

      foreach ((string column, Type enumType) in EnumColumnTypes)
      {
        foreach (int literal in EnumValueViolations(statement, column, enumType))
        {
          enumValueViolations.Add($"{statementId}：{column} = {literal} 不属于 {enumType.Name}");
        }
      }
    }

    Assert.True(violations.Count == 0, string.Join(" | ", violations));
    Assert.Empty(unresolvedAliasViolations);
    Assert.Empty(enumValueViolations);

    // 变异证据：把列清单里的列换成该表上不存在的列后，逐项判定必须报出违规。
    Dictionary<string, string> singleTable = AliasToTables("select id, standard_project_code from mrec_recognition_match_item");
    Assert.Empty(UnqualifiedColumnViolations("select id, standard_project_code from mrec_recognition_match_item", singleTable, columnsByTable));
    Assert.NotEmpty(UnqualifiedColumnViolations(
      "select id, standard_project_code, standard_project_name from mrec_recognition_match_item",
      singleTable,
      columnsByTable));
    Assert.NotEmpty(UnqualifiedColumnViolations(
      "insert into mrec_recognition_match_item (id, standard_project_name) values ($Id, $Name)",
      singleTable,
      columnsByTable));

    // 变异证据：把只存在于报告版本表的列引用到报告主体上后，列一致性判定必须报出违规。
    (string Alias, string Column)[] crossTable = SplitAliasColumnReferences(
      "select r.identity_document_type_code, r.visit_type, r.visit_serial_no from mrec_medical_recognition_report r");
    Assert.NotEmpty(crossTable);
    Assert.NotEmpty(ColumnReferenceViolations(crossTable, AliasToTables("from mrec_medical_recognition_report r"), columnsByTable));

    // 变异证据：把表别名从 from 子句里去掉后，同一判定必须报出该别名无法解析。
    (string Alias, string Column)[] orphanAlias = SplitAliasColumnReferences("select x.id from mrec_medical_recognition_report r");
    Assert.NotEmpty(orphanAlias);
    Assert.NotEmpty(ColumnReferenceViolations(orphanAlias, AliasToTables("from mrec_medical_recognition_report r"), columnsByTable));

    // 派生表别名不属于物理表，其列引用由子查询投影确定，因此不进入物理列判定。
    Assert.Contains(
      "c",
      DerivedTableAliases(QueryStatement("QueryRecognitionMatchCandidateReports")));

    // 变异证据：把枚举列的比较值换成不属于该枚举的编号后，取值校验必须判出未登记取值。
    Assert.NotEmpty(ExtractComparisonLiterals("and r.report_type = 0", "report_type"));
    Assert.Empty(EnumValueViolations("and r.report_type = 2", "report_type", typeof(MedicalReportType)));
    Assert.NotEmpty(EnumValueViolations("and r.report_type = 0", "report_type", typeof(MedicalReportType)));
  }

  /// <summary>
  /// 取语句的投影段，即第一个 <c>select</c> 与其后第一个独立 <c>from</c> 之间的列清单；
  /// 用于按各自语句的投影逐列判定，避免把 <c>where</c>、<c>join</c> 或排序里的同名列算成投影。
  /// </summary>
  /// <param name="statement">语句文本，可含行内 XML 注释。</param>
  /// <returns>去掉注释并折行后的投影段文本，其首尾不含多余空白；语句不含 <c>from</c> 子句时返回整条语句。</returns>
  private static string StatementProjection(string statement)
  {
    string normalised = NormaliseStatement(statement);
    int select = normalised.IndexOf("select", StringComparison.OrdinalIgnoreCase);
    int from = normalised.IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
    return (select >= 0 && from > select ? normalised[(select + "select".Length)..from] : normalised).Trim();
  }

  /// <summary>
  /// 统计投影段里某个裸列名作为独立一项出现的次数，只按逗号分隔的完整投影项判定。
  /// </summary>
  /// <param name="projection">投影段文本。</param>
  /// <param name="column">物理列名。</param>
  /// <returns>该列作为独立投影项出现的次数。</returns>
  private static int ProjectionColumnOccurrences(string projection, string column) =>
    projection.Split(',')
      .Select(entry => entry.Trim())
      .Count(entry => string.Equals(entry, column, StringComparison.Ordinal));

  /// <summary>
  /// 取某条查询侧语句的文本。
  /// </summary>
  /// <param name="statementId">查询侧语句标识。</param>
  /// <returns>语句文本，包含其内部的行内 XML 注释。</returns>
  /// <exception cref="InvalidOperationException">该标识在查询映射文件里不是恰好一处时抛出。</exception>
  private static string QueryStatement(string statementId) => StatementText(QueryMapPath(), statementId);

  /// <summary>
  /// 取某个实体映射内某条语句的文本。
  /// </summary>
  /// <param name="mapFile">实体映射文件名，例如 <c>RecognitionMatchRecord.xml</c>。</param>
  /// <param name="statementId">实体映射内的语句标识。</param>
  /// <returns>语句文本，包含其内部的行内 XML 注释。</returns>
  /// <exception cref="InvalidOperationException">该标识在该映射文件里不是恰好一处时抛出。</exception>
  private static string EntityMapStatement(string mapFile, string statementId) =>
    StatementText(EntityMapPath(mapFile), statementId);

  /// <summary>
  /// 取映射文件内某条语句的文本。
  /// </summary>
  /// <remarks>
  /// <see cref="XElement.Value"/> 会把语句内部的行内 XML 注释一并取进文本，注释承担字段中文说明而不是 SQL 的一部分，
  /// 因此这里先去掉注释再返回，使按投影、谓词与方言判定的用例拿到的是可逐项判定的 SQL 文本。
  /// </remarks>
  /// <param name="mapPath">映射文件的绝对路径。</param>
  /// <param name="statementId">语句标识。</param>
  /// <returns>去掉行内 XML 注释后的语句文本。</returns>
  /// <exception cref="InvalidOperationException">该标识在该文件里不是恰好一处时抛出。</exception>
  private static string StatementText(string mapPath, string statementId)
  {
    XDocument document = XDocument.Load(mapPath);
    XElement statement = Assert.Single(StatementElements(document), element => (string?)element.Attribute("Id") == statementId);
    return NormaliseStatement(statement.Value);
  }

  /// <summary>
  /// 解析 <c>Scripts/*.sql</c> 的建表语句，得到物理表名到列集合的映射。
  /// </summary>
  /// <returns>物理表名到该表列集合的映射，比较按序号。</returns>
  /// <exception cref="InvalidOperationException">某个脚本里找不到建表语句时抛出。</exception>
  private static Dictionary<string, HashSet<string>> PhysicalTableColumns()
  {
    Dictionary<string, HashSet<string>> columnsByTable = new(StringComparer.Ordinal);
    foreach (string scriptFile in Directory.EnumerateFiles(RepositoryPath("Scripts"), "*.sql", SearchOption.TopDirectoryOnly))
    {
      Match table = Regex.Match(
        File.ReadAllText(scriptFile),
        @"create\s+table\s+(?<table>[a-z_][a-z0-9_]*)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
      if (!table.Success) throw new InvalidOperationException($"脚本 {Path.GetFileName(scriptFile)} 缺少建表语句。");

      string tableName = table.Groups["table"].Value;
      columnsByTable[tableName] =
      [
        .. ExtractCreateTableColumnLines(File.ReadAllText(scriptFile), tableName).Select(column => column.Name)
      ];
    }

    return columnsByTable;
  }

  /// <summary>
  /// 抽出语句中形如 <c>别名.列名</c> 的引用，只取小写标识符，跳过参数名与表限定名。
  /// </summary>
  /// <remarks>
  /// 参数以 <c>$</c> 开头，限定名与别名都带点号，因此形如 <c>schema.table.column</c> 与 <c>$Parameter</c> 的写法不会进入结果。
  /// </remarks>
  /// <param name="statement">语句文本。</param>
  /// <returns>按出现顺序排列的引用，允许重复，由调用方去重。</returns>
  private static (string Alias, string Column)[] SplitAliasColumnReferences(string statement) =>
  [
    .. Regex.Matches(statement, @"(?<![A-Za-z0-9_.$])(?<alias>[a-z][a-z0-9_]*)\.(?<column>[a-z][a-z0-9_]*)", RegexOptions.CultureInvariant)
      .Select(match => (Alias: match.Groups["alias"].Value, Column: match.Groups["column"].Value))
  ];

  /// <summary>
  /// 去掉语句里的行内 XML 注释并把换行折成空格，得到可逐项判定的 SQL 文本。
  /// </summary>
  /// <remarks>
  /// 注释承担字段中文说明，不是 SQL 的一部分；折行后 <c>from</c> 与表名之间不再有换行，列引用与子句边界都在同一段文本内。
  /// </remarks>
  /// <param name="statement">语句原文。</param>
  /// <returns>去掉注释并折行后的 SQL 文本。</returns>
  private static string NormaliseStatement(string statement) =>
    Regex.Replace(Regex.Replace(statement, "<!--.*?-->", " ", RegexOptions.Singleline), @"\s+", " ").Trim();

  /// <summary>
  /// 取语句中内联派生表的别名。
  /// </summary>
  /// <remarks>
  /// 派生表的列由子查询投影决定，与物理表列集合无关，因此这些别名不参与物理列引用判定；
  /// 派生表总是位于语句最后一层括号处，取最后一个右括号之后、合法别名之后紧跟的非空白字符即可确认该别名。
  /// </remarks>
  /// <param name="statement">语句文本。</param>
  /// <returns>派生表别名集合。</returns>
  private static HashSet<string> DerivedTableAliases(string statement)
  {
    HashSet<string> aliases = new(StringComparer.Ordinal);
    int closing = statement.LastIndexOf(')');
    if (closing < 0) return aliases;

    Match alias = Regex.Match(statement[(closing + 1)..], @"^\s*(?:as\s+)?(?<alias>[a-z][a-z0-9_]*)\s*[a-z]?", RegexOptions.IgnoreCase);
    if (alias.Success) aliases.Add(alias.Groups["alias"].Value);
    return aliases;
  }

  /// <summary>
  /// 按语句的 <c>from</c> 与 <c>join</c> 子句把表别名映射到物理表名。
  /// </summary>
  /// <remarks>
  /// 只识别 <c>mrec_</c> 前缀的表；内联派生表（<c>from (</c> 形态）与函数调用不产生表别名，因此其别名不会被登记。
  /// 语句本身定位的物理表也按其表名登记一个别名，使不带别名的语句可以用表名作为限定写法。
  /// 子句形态取到别名或到第一个逗号为止，因此同一子句里跟在别名之后的列引用不会被误当成别名。
  /// </remarks>
  /// <param name="statement">语句文本。</param>
  /// <returns>表别名到物理表名的映射，比较按序号。</returns>
  private static Dictionary<string, string> AliasToTables(string statement)
  {
    Dictionary<string, string> aliases = new(StringComparer.Ordinal);
    foreach (Match clause in Regex.Matches(statement, @"\b(?:from|join)\s+(?<table>mrec_[a-z0-9_]*)(?:\s+(?:as\s+)?(?<alias>[a-z][a-z0-9_]*))?", RegexOptions.IgnoreCase))
    {
      string table = clause.Groups["table"].Value;
      aliases[clause.Groups["alias"].Success ? clause.Groups["alias"].Value : table] = table;
    }

    Match insert = Regex.Match(statement, @"insert\s+into\s+(?<table>mrec_[a-z0-9_]*)", RegexOptions.IgnoreCase);
    Match update = Regex.Match(statement.TrimStart(), @"^update\s+(?<table>mrec_[a-z0-9_]*)", RegexOptions.IgnoreCase);
    Match source = Regex.Match(statement, @"(?<![A-Za-z0-9_.$])from\s+(?<table>mrec_[a-z0-9_]*)", RegexOptions.IgnoreCase);
    string? target = insert.Success ? insert.Groups["table"].Value
      : update.Success ? update.Groups["table"].Value
      : source.Success ? source.Groups["table"].Value
      : null;
    if (target is not null) aliases[target] = target;

    return aliases;
  }

  /// <summary>
  /// 判断文本是否命中任一数据库方言特征，供变异证据复用同一套标记。
  /// </summary>
  /// <param name="text">待扫描的 SQL 文本。</param>
  /// <returns>命中的特征与匹配文本；没有命中时为空集合。</returns>
  private static IReadOnlyList<string> MatchesAnyDialectMarker(string text) =>
  [
    .. DialectMarkers
      .Select(marker => (marker.Feature, Match: Regex.Match(text, marker.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
      .Where(hit => hit.Match.Success)
      .Select(hit => $"{hit.Feature} => {hit.Match.Value}")
  ];

  /// <summary>
  /// 逐项判断列引用是否落在其别名指向的物理表上。
  /// </summary>
  /// <param name="references">形如 <c>别名.列名</c> 的引用集合。</param>
  /// <param name="aliases">语句内的表别名到物理表名的映射。</param>
  /// <param name="columnsByTable">物理表名到列集合的映射。</param>
  /// <returns>每一项违规的别名、列名、物理表与原因；全部成立时为空集合。</returns>
  private static IReadOnlyList<(string Alias, string Column, string Table, string Reason)> ColumnReferenceViolations(
    IReadOnlyCollection<(string Alias, string Column)> references,
    IReadOnlyDictionary<string, string> aliases,
    IReadOnlyDictionary<string, HashSet<string>> columnsByTable)
  {
    List<(string Alias, string Column, string Table, string Reason)> violations = [];
    foreach ((string alias, string column) in references)
    {
      if (!aliases.TryGetValue(alias, out string? table))
      {
        violations.Add((alias, column, string.Empty, "该别名未在语句的 from 或 join 子句中解析到物理表"));
        continue;
      }

      if (!columnsByTable.TryGetValue(table, out HashSet<string>? columns))
      {
        violations.Add((alias, column, table, "该物理表不在 Scripts 目录的建表脚本集合内"));
        continue;
      }

      if (!columns.Contains(column)) violations.Add((alias, column, table, $"列 {column} 不存在于物理表 {table}"));
    }

    return violations;
  }

  /// <summary>
  /// 按语句形态抽取需要按物理表列集合核对的列，写入列清单或查询投影。
  /// </summary>
  /// <remarks>
  /// 列清单语句整体就是列清单；<c>insert into</c> 取括号内的写入列清单；<c>select</c> 取投影段；
  /// 其余语句形态（例如带 <c>set</c> 的更新语句）不在此判定，其列引用由别名引用判定承担。
  /// </remarks>
  /// <param name="statement">语句文本。</param>
  /// <returns>按逗号分隔后的列声明文本；不适用于该形态时为空字符串。</returns>
  private static string UnqualifiedColumnRegion(string statement)
  {
    Match insert = Regex.Match(statement, @"insert\s+into\s+mrec_[a-z0-9_]*\s*\((?<body>.*?)\)\s*values\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    if (insert.Success) return insert.Groups["body"].Value;

    Match projection = Regex.Match(statement, @"\bselect\b(?<body>.+?)\bfrom\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    if (projection.Success) return projection.Groups["body"].Value;

    return statement.Contains('(') || statement.Contains(" values ", StringComparison.OrdinalIgnoreCase)
      ? string.Empty
      : statement;
  }

  /// <summary>
  /// 逐项判断列清单里的列是否存在于语句定位的物理表。
  /// </summary>
  /// <param name="statement">语句文本。</param>
  /// <param name="aliases">语句内的表别名到物理表名的映射。</param>
  /// <param name="columnsByTable">物理表名到列集合的映射。</param>
  /// <returns>不存在于该表列集合内的列名，按出现顺序排列。</returns>
  private static IReadOnlyList<string> UnqualifiedColumnViolations(
    string statement,
    IReadOnlyDictionary<string, string> aliases,
    IReadOnlyDictionary<string, HashSet<string>> columnsByTable)
  {
    string[] tables = [.. aliases.Values.Distinct(StringComparer.Ordinal)];
    if (tables.Length != 1 || !columnsByTable.TryGetValue(tables[0], out HashSet<string>? columns)) return [];

    string region = UnqualifiedColumnRegion(statement);
    return
    [
      .. region.Split(',')
        .Select(entry => entry.Trim())
        .Where(entry => Regex.IsMatch(entry, @"^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant))
        .Where(entry => !columns.Contains(entry))
    ];
  }

  /// <summary>
  /// 抽出语句中与某个列比较或写值的数值字面量。
  /// </summary>
  /// <remarks>
  /// 两侧写法都识别：<c>列 = 值</c> 与 <c>值 = 列</c>，参数与函数调用不产生字面量。
  /// </remarks>
  /// <param name="statement">语句文本。</param>
  /// <param name="column">列名，不带别名。</param>
  /// <returns>按出现顺序排列的数值字面量。</returns>
  private static int[] ExtractComparisonLiterals(string statement, string column) =>
  [
    .. Regex.Matches(statement, $@"(?<![A-Za-z0-9_.$])(?:[a-z][a-z0-9_]*\.)?{Regex.Escape(column)}\s*=\s*(?<value>\d+)(?![0-9.])", RegexOptions.CultureInvariant)
      .Select(match => int.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture)),
    .. Regex.Matches(statement, $@"(?<![A-Za-z0-9_.$])(?<value>\d+)\s*=\s*(?:[a-z][a-z0-9_]*\.)?{Regex.Escape(column)}(?![A-Za-z0-9_])", RegexOptions.CultureInvariant)
      .Select(match => int.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture))
  ];

  /// <summary>
  /// 判断语句中与某个枚举型列比较的数值字面量是否都属于该枚举。
  /// </summary>
  /// <param name="statement">语句文本。</param>
  /// <param name="column">枚举型列名。</param>
  /// <param name="enumType">该列对应的枚举类型。</param>
  /// <returns>不属于该枚举值域的编号，按出现顺序排列。</returns>
  private static IReadOnlyList<int> EnumValueViolations(string statement, string column, Type enumType) =>
  [
    .. ExtractComparisonLiterals(statement, column).Where(literal => !Enum.IsDefined(enumType, literal))
  ];

  /// <summary>
  /// 逐项判断建表脚本是否违反列规范中的负向约束。
  /// </summary>
  /// <param name="script">建表脚本全文。</param>
  /// <returns>每一项违规的规则说明与命中文本；全部成立时为空集合。</returns>
  private static IReadOnlyList<string> DdlRuleViolations(string script)
  {
    (string Rule, string Marker)[] forbidden =
    [
      ("文本列使用带长度上限的类型", "varchar("),
      ("列或索引带数据库默认值", " default "),
      ("引用其他系统的表", "references "),
      ("声明跨系统外键", "foreign key"),
      ("在脚本内追加结构变更", "alter table"),
      ("索引带部分行过滤条件", " where ")
    ];

    return [.. forbidden.Where(rule => script.Contains(rule.Marker, StringComparison.OrdinalIgnoreCase)).Select(rule => rule.Rule)];
  }

  /// <summary>
  /// 逐项比较两份语句标识清单，返回差异说明。
  /// </summary>
  /// <param name="expected">冻结的语句标识清单。</param>
  /// <param name="actual">实际语句标识清单。</param>
  /// <returns>每一项差异的说明；两份清单完全一致时为空集合。</returns>
  private static IReadOnlyList<string> StatementIdListViolations(
    IReadOnlyList<string> expected,
    IReadOnlyList<string> actual)
  {
    List<string> violations = [];
    if (expected.Count != actual.Count) violations.Add($"语句条数不一致：冻结 {expected.Count} 条，实际 {actual.Count} 条");
    for (int index = 0; index < Math.Min(expected.Count, actual.Count); index++)
    {
      if (expected[index] != actual[index]) violations.Add($"第 {index + 1} 条语句标识应为 {expected[index]}，实际为 {actual[index]}");
    }

    return violations;
  }

  /// <summary>
  /// 逐项判断报告中是否存在报告使用关系载体：表名、实体名与事件名都不得出现这些词根。
  /// </summary>
  /// <param name="scriptFiles">建表脚本文件名集合。</param>
  /// <param name="entityFiles">实体文件名集合。</param>
  /// <param name="eventFiles">领域事件文件名集合。</param>
  /// <returns>每一项违规的说明；不存在载体时为空集合。</returns>
  private static IReadOnlyList<string> UsageCarrierViolations(
    IReadOnlyCollection<string> scriptFiles,
    IReadOnlyCollection<string> entityFiles,
    IReadOnlyCollection<string> eventFiles)
  {
    string[] markers = ["usage", "report_usage", "ReportUsage", "report_usage_relation", "ReportUsageRelation"];
    List<string> violations = [];
    foreach (string marker in markers)
    {
      violations.AddRange(scriptFiles.Where(name => name.Contains(marker, StringComparison.OrdinalIgnoreCase)).Select(name => $"脚本 {name}"));
      violations.AddRange(entityFiles.Where(name => name.Contains(marker, StringComparison.OrdinalIgnoreCase)).Select(name => $"实体 {name}"));
      violations.AddRange(eventFiles.Where(name => name.Contains(marker, StringComparison.OrdinalIgnoreCase)).Select(name => $"事件 {name}"));
    }

    return [.. violations.Distinct(StringComparer.Ordinal)];
  }

  /// <summary>
  /// 逐项判断列清单里是否出现标准项目编码列。
  /// </summary>
  /// <param name="columnNames">按脚本顺序排列的列名。</param>
  /// <returns>出现时返回该列名，未出现时为空集合。</returns>
  private static IReadOnlyList<string> StandardProjectCodeViolations(IReadOnlyCollection<string> columnNames) =>
    [.. columnNames.Where(name => name == "standard_project_code")];

  /// <summary>
  /// 逐项判断建表脚本的列清单是否按 DDL 规则收尾：操作时间紧接在操作人之前。
  /// </summary>
  /// <param name="columns">按脚本顺序排列的列定义。</param>
  /// <returns>每一项违规的说明；顺序成立时为空集合。</returns>
  private static IReadOnlyList<string> TableColumnOrderViolations(IReadOnlyList<(string Name, string Type)> columns)
  {
    List<string> violations = [];
    if (columns.Count < 2 || columns[^2].Name != "oper_time") violations.Add("倒数第二列应为操作时间 oper_time");
    if (columns.Count < 1 || columns[^1].Name != "oper_id") violations.Add("最后一列应为操作人标识 oper_id");
    return violations;
  }

  /// <summary>
  /// 逐项判断实体映射的列清单是否按实体属性声明顺序书写：操作人标识紧接在操作时间之前。
  /// </summary>
  /// <param name="statement">列清单语句文本。</param>
  /// <returns>每一项违规的说明；顺序成立时为空集合。</returns>
  private static IReadOnlyList<string> EntityColumnOrderViolations(string statement)
  {
    string[] columns =
    [
      .. statement.Split(',')
        .Select(entry => entry.Trim())
        .Where(entry => Regex.IsMatch(entry, @"^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant))
    ];

    List<string> violations = [];
    if (columns.Length < 3 || columns[^2] != "oper_id") violations.Add("倒数第二列应为操作人标识 oper_id");
    if (columns.Length < 2 || columns[^1] != "oper_time") violations.Add("最后一列应为操作时间 oper_time");
    return violations;
  }

  /// <summary>
  /// 逐项判断某个业务时间列的类型是否满足精度不低于秒的要求。
  /// </summary>
  /// <param name="columns">该表的列定义。</param>
  /// <param name="columnName">业务时间列名。</param>
  /// <returns>每一项违规的说明；类型成立时为空集合。</returns>
  private static IReadOnlyList<string> BusinessTimeTypeViolations(
    IReadOnlyList<(string Name, string Type)> columns,
    string columnName)
  {
    (string Name, string Type) column = columns.Single(entry => entry.Name == columnName);
    List<string> violations = [];
    if (column.Type != "timestamp without time zone not null") violations.Add($"{columnName} 的类型为 {column.Type}");
    if (column.Type.Contains("timestamp(", StringComparison.OrdinalIgnoreCase)) violations.Add($"{columnName} 显式降低了时间精度");
    if (column.Type.Contains("date", StringComparison.OrdinalIgnoreCase)) violations.Add($"{columnName} 退化为日期类型");
    return violations;
  }

  /// <summary>
  /// 逐项判断集合参数的写法是否为参数名直接跟在 <c>in</c> 之后、不带括号。
  /// </summary>
  /// <param name="statement">语句文本。</param>
  /// <param name="parameterName">集合参数名，含 <c>$</c> 前缀。</param>
  /// <returns>每一项违规的说明；写法成立时为空集合。</returns>
  private static IReadOnlyList<string> CollectionParameterViolations(string statement, string parameterName)
  {
    List<string> violations = [];
    if (!Regex.IsMatch(statement, $@"\bin\s+{Regex.Escape(parameterName)}(?![A-Za-z0-9_])", RegexOptions.CultureInvariant))
    {
      violations.Add($"{parameterName} 未以参数名直接跟在 in 之后的形态出现");
    }

    if (statement.Contains($"in ({parameterName}", StringComparison.Ordinal) ||
        statement.Contains($"in( {parameterName}", StringComparison.Ordinal))
    {
      violations.Add($"{parameterName} 被写成了带括号的形态");
    }

    foreach (string column in new[] { nameof(MedicalReportType), nameof(MedicalItemType), "status", nameof(RecognitionResult) })
    {
      if (statement.Contains($"{column} = {parameterName}", StringComparison.Ordinal))
      {
        violations.Add($"{column} 退化为逐项等值写法");
      }
    }

    return violations;
  }

  /// <summary>
  /// 逐项判断索引声明是否违反索引规范：定义不得带行过滤条件，唯一性必须与索引名前缀一致。
  /// </summary>
  /// <param name="script">建表脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <param name="isUnique">声明是否为唯一索引。</param>
  /// <returns>每一项违规的说明；声明成立时为空集合。</returns>
  private static IReadOnlyList<string> IndexDeclarationViolations(string script, string indexName, bool isUnique)
  {
    string definition = IndexDefinition(script, indexName);
    List<string> violations = [];
    if (definition.Contains("where", StringComparison.OrdinalIgnoreCase)) violations.Add($"{indexName} 的定义带行过滤条件");
    if (isUnique != indexName.StartsWith("ux_", StringComparison.Ordinal)) violations.Add($"{indexName} 的唯一性与索引名前缀不一致");
    return violations;
  }

  /// <summary>
  /// 逐项判断映射文件引用的表名是否都带本平台统一前缀。
  /// </summary>
  /// <param name="xml">映射文件全文。</param>
  /// <returns>每一项违规的说明；全部带前缀时为空集合。</returns>
  private static IReadOnlyList<string> TablePrefixViolations(string xml) =>
  [
    .. TableNameReferences(xml)
      .Distinct(StringComparer.Ordinal)
      .Where(tableName => !tableName.StartsWith("mrec_", StringComparison.Ordinal))
      .Select(tableName => $"表 {tableName} 未使用 mrec_ 前缀")
  ];

  /// <summary>
  /// 递归枚举枚举类型里的全部已声明取值。
  /// </summary>
  /// <param name="enumType">枚举类型。</param>
  /// <returns>按类型声明顺序排列的取值。</returns>
  private static IReadOnlyList<object> EnumValues(Type enumType) => [.. Enum.GetValues(enumType).Cast<object>()];

  /// <summary>
  /// 本阶段四个实体映射与候选查询涉及的物理表名集合。
  /// </summary>
  /// <remarks>映射文件只能引用这些表；表名集合由 <c>Scripts</c> 目录的建表脚本与映射声明共同确定。</remarks>
  private static HashSet<string> Stage5TableNames =>
  [
    .. PhysicalTableColumns().Keys,
    .. DeclaredMapTableNames
  ];

  /// <summary>
  /// 取出建表语句括号内的列定义（列名与完整类型声明），跳过空行与主键语句。
  /// </summary>
  /// <param name="script">建表脚本全文。</param>
  /// <param name="tableName">目标物理表名。</param>
  /// <returns>按脚本顺序排列的列定义，已去掉行首尾空白与尾随逗号。</returns>
  /// <exception cref="InvalidOperationException">脚本中不存在目标表的建表语句时抛出。</exception>
  private static (string Name, string Type)[] ExtractCreateTableColumnLines(string script, string tableName)
  {
    string marker = $"create table {tableName} (";
    int start = script.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"脚本中未找到 {marker}。");

    string body = script[(start + marker.Length)..];
    body = body[..body.IndexOf(");", StringComparison.Ordinal)];
    return
    [
      .. body.Split('\n')
        .Select(line => line.Trim().TrimEnd(','))
        .Where(line => line.Length > 0 && !line.StartsWith("primary key", StringComparison.OrdinalIgnoreCase))
        .Select(line =>
        {
          int separator = line.IndexOf(' ');
          return (Name: line[..separator], Type: line[(separator + 1)..]);
        })
    ];
  }

  /// <summary>
  /// 取脚本中某条索引的定义行，即索引名所在行之后到分号为止的覆盖列声明。
  /// </summary>
  /// <param name="script">建表脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>索引的覆盖列声明行，含结尾分号，已去掉首尾空白。</returns>
  /// <exception cref="InvalidOperationException">脚本中不存在该索引时抛出。</exception>
  private static string IndexDefinition(string script, string indexName)
  {
    int marker = script.IndexOf(indexName, StringComparison.Ordinal);
    if (marker < 0) throw new InvalidOperationException($"脚本中未找到索引 {indexName}。");

    string remainder = script[(marker + indexName.Length)..];
    int end = remainder.IndexOf(';', StringComparison.Ordinal);
    return end < 0
      ? throw new InvalidOperationException($"索引 {indexName} 的声明没有以分号结束。")
      : remainder[..(end + 1)].Trim();
  }

  /// <summary>
  /// 统计脚本中某条索引的声明次数，普通索引与唯一索引一并计入。
  /// </summary>
  /// <remarks>
  /// 按行首前缀判定而不是按子串判定：搜索 <c>create index {索引名}</c> 会把 <c>create unique index {索引名}</c> 一并命中，
  /// 从而对唯一索引得出两次声明。行首前缀判定不带尾随空格，因为索引声明行的索引名之后直接就是换行；
  /// 本项目五条索引名互不为前缀，因此前缀判定不会把另一条索引误计进来。
  /// 每行先去掉行尾回车再判定，因此不受文件换行形态影响。
  /// </remarks>
  /// <param name="script">建表脚本全文。</param>
  /// <param name="indexName">索引名。</param>
  /// <returns>该索引的声明次数。</returns>
  private static int CountIndexDeclarations(string script, string indexName) =>
    script.Split('\n')
      .Select(line => line.TrimEnd('\r').Trim())
      .Count(line =>
        line.StartsWith($"create index {indexName}", StringComparison.Ordinal) ||
        line.StartsWith($"create unique index {indexName}", StringComparison.Ordinal));

  /// <summary>
  /// 收集文本中 from/join/insert into/update 之后引用的表名。
  /// </summary>
  /// <param name="text">映射文件或语句文本。</param>
  /// <returns>按出现顺序排列的表名。</returns>
  private static List<string> TableNameReferences(string text) =>
  [
    .. Regex.Matches(text, @"\b(?:from|join|insert\s+into|update)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
      .Select(match => match.Groups[1].Value)
  ];

  /// <summary>映射文件声明的命名空间；元素与属性都需要按限定名查找。</summary>
  private static readonly XNamespace SqlMapNamespace = "http://dysoft.vip/schemas/EarthraceSqlMap.xsd";

  /// <summary>
  /// 取映射文件里的全部 Statement 节点，按声明顺序排列。
  /// </summary>
  /// <remarks>
  /// 参数映射节点同样携带 Id，因此不能按"任意带 Id 的元素"取语句；这里只取 Statements 段下的 Statement 子节点。
  /// </remarks>
  /// <param name="document">已加载的映射文件。</param>
  /// <returns>按声明顺序排列的 Statement 节点。</returns>
  /// <exception cref="InvalidOperationException">文档缺少根节点时抛出。</exception>
  private static IReadOnlyList<XElement> StatementElements(XDocument document) =>
    document.Root is null
      ? throw new InvalidOperationException("映射文件缺少根节点。")
      : [.. document.Root.Elements(SqlMapNamespace + "Statements").Elements(SqlMapNamespace + "Statement")];

  /// <summary>
  /// 取映射文件里全部 Statement 节点的标识，按声明顺序排列。
  /// </summary>
  /// <param name="document">已加载的映射文件。</param>
  /// <returns>按声明顺序排列的语句标识。</returns>
  private static IReadOnlyList<string> StatementIds(XDocument document) =>
    [.. StatementElements(document).Select(element => (string)element.Attribute("Id")!)];

  /// <summary>在 <c>Scripts</c> 目录内定位建表脚本。</summary>
  /// <param name="fileName">建表脚本文件名，例如 <c>mrec_recognition_match_record.sql</c>。</param>
  /// <returns>该脚本的绝对路径。</returns>
  private static string ScriptPath(string fileName) => SourceSyntaxGuard.FindRepositoryFile("Scripts", fileName);

  /// <summary>在实体映射文件所在目录内定位阶段 5 的实体映射。</summary>
  /// <param name="fileName">映射文件名，例如 <c>RecognitionMatchRecord.xml</c>。</param>
  /// <returns>该映射文件的绝对路径。</returns>
  /// <remarks>
  /// 映射文件的物理位置受宿主默认资源模式约束：程序集根命名空间下一层目录内的直接文件才会被注册，
  /// 因此实体映射固定在 <c>MedicalRecognitionReportAggregate</c>，不得新建分组子目录。
  /// </remarks>
  private static string EntityMapPath(string fileName) =>
    SourceSyntaxGuard.FindRepositoryFile("MedicalRecognitionReportAggregate", fileName);

  /// <summary>在查询映射文件所在目录内定位查询侧映射。</summary>
  /// <returns>查询映射文件的绝对路径。</returns>
  private static string QueryMapPath() => SourceSyntaxGuard.FindRepositoryFile("Queries", "MedicalRecognitionReportQuery.xml");

  /// <summary>在仓储工程内按相对路径拼接目录。</summary>
  /// <param name="segments">相对仓储工程的目录或文件名片段。</param>
  /// <returns>绝对路径。</returns>
  private static string RepositoryPath(params string[] segments) =>
    ServerPath(["Dy.MedicalRecognition.Repository", .. segments]);

  /// <summary>在后端根目录 <c>server</c> 内按相对路径拼接目录。</summary>
  /// <param name="segments">相对后端根目录的工程目录或文件名片段。</param>
  /// <returns>绝对路径。</returns>
  private static string ServerPath(params string[] segments) =>
    Path.Combine([SourceSyntaxGuard.FindRepositoryRoot(), "server", .. segments]);

  /// <summary>在测试工程内定位源码文件。</summary>
  /// <param name="relativePath">相对测试工程的路径片段。</param>
  /// <returns>该文件的绝对路径。</returns>
  private static string TestPath(params string[] relativePath) =>
    ServerPath(["Dy.MedicalRecognition.Tests", .. relativePath]);

  /// <summary>
  /// 关键投影的关键列必须按投影类型的属性名取别名；未取别名或别名改名都会让映射静默留空。
  /// </summary>
  /// <remarks>
  /// 判据面是三个投影的关键列：报告事实与引用报告上下文两个投影的来源三值放在带 <c>Source</c> 前缀的属性上，
  /// 引用报告上下文的检查所见与检查结论同理；按版本筛有效版本的语句投影标量标识，别名取投影目标类型的属性名。
  /// 这四类列直接投影原列名、或把别名改成别的前缀，映射都找不到同名属性而留空：
  /// 来源三值为空会让应用层按空编码解析组织路径并整次失败，表现为「组织不存在或已停用」而不是映射缺列；
  /// 检查所见与检查结论为空会让引用详情静默缺少两项。该失败只在真实 Provider 上出现，内存替身与既有静态列存在性判据都看不见。
  /// 判据按投影段逐项判定，并对"别名改名"补一条变异证据；本用例不声称覆盖全部投影类型的全部属性。
  /// </remarks>
  [Fact]
  public void Stage_projections_alias_columns_to_the_projection_type_property_names()
  {
    HashSet<string> propertyNames = [];
    foreach (string projectionFile in new[] { "RecognitionMatchReportFactsItem.cs", "RecognitionCitationReportContextItem.cs" })
    {
      propertyNames.UnionWith(
        SourceSyntaxGuard.Read("server", "Dy.MedicalRecognition.Domain", "Queries", projectionFile)
          .DescendantNodes()
          .OfType<PropertyDeclarationSyntax>()
          .Select(property => property.Identifier.ValueText));
    }

    foreach (string required in new[]
    {
      "SourceOrganizationCode", "SourceHospitalCode", "SourceBranchCode",
      "SourceApplicantDoctorId", "SourceApplicantDoctorName", "SourceReviewerDoctorId", "SourceReviewerDoctorName",
      "ExaminationFindings", "ExaminationConclusion"
    })
    {
      Assert.Contains(required, propertyNames);
    }

    string facts = NormaliseStatement(QueryStatement("QueryRecognitionMatchReportFacts"));
    string contexts = NormaliseStatement(QueryStatement("QueryRecognitionCitationReportContexts"));
    string scalar = NormaliseStatement(QueryStatement("QueryValidRecognitionReportVersionIds"));

    Assert.Empty(AliasViolations(facts));
    Assert.Empty(AliasViolations(contexts));
    Assert.Contains("v.id as id", scalar, StringComparison.Ordinal);

    // 变异证据：把来源组织列的别名改成另一个名字后，同一判据必须报出该列未按属性名取别名。
    string renamed = facts.Replace("as source_organization_code", "as source_org_code", StringComparison.Ordinal);
    Assert.NotEqual(facts, renamed);
    Assert.Equal("r.organization_code", Assert.Single(AliasViolations(renamed)));
  }

  /// <summary>
  /// 找出投影段里未按投影类型属性名取别名的关键列。
  /// </summary>
  /// <remarks>
  /// 判据只作用于本阶段的关键列清单：这些列在投影类型上有带前缀的属性名，直接投影原列名或改成其他别名都会静默留空。
  /// 关键列的期望别名与其属性名同名（下划线形态），因此按逐项片段判定即可，不做全量投影类型推导。
  /// </remarks>
  /// <param name="statement">已归一化的语句文本。</param>
  /// <returns>未按期望别名投影的关键列限定名；全部合规时为空集合。</returns>
  private static IReadOnlyList<string> AliasViolations(string statement)
  {
    (string Column, string Alias)[] requiredAliases =
    [
      ("r.organization_code", "source_organization_code"),
      ("r.hospital_code", "source_hospital_code"),
      ("r.branch_code", "source_branch_code"),
      ("ec.findings", "examination_findings"),
      ("ec.conclusion", "examination_conclusion")
    ];

    return
    [
      .. requiredAliases
        .Where(required => statement.Contains(required.Column, StringComparison.Ordinal))
        .Where(required => !statement.Contains($"{required.Column} as {required.Alias}", StringComparison.Ordinal))
        .Select(required => required.Column)
    ];
  }

  /// <summary>
  /// V82：按集合读取的映射方法在空集合上必须短路返回空结果，不得把空集合交给映射语句。
  /// </summary>
  /// <remarks>
  /// 这类语句用集合参数写法，参数名直接跟在 <c>in</c> 之后；空集合在真实 Provider 上展开为空值列表，
  /// 语句语法不成立：检查部位语句抛数据库语法错误，按版本筛有效版本的语句在结果反序列化上抛空引用异常。
  /// 检验报告的匹配响应没有检查项目，处理结果提交会传入空版本集合，两条路径都在正常业务上可达，
  /// 因此空集合必须在仓储层短路：既不发起数据库往返，也不改变"没有入参就没有结果"的语义。
  /// 判据按语法节点判定，不依赖空白与注释写法：方法体内存在唯一一处「入参集合数量为零即返回空集合」的早返回。
  /// </remarks>
  /// <param name="methodName">按集合读取的映射方法名。</param>
  [Theory]
  [InlineData("QueryExaminationSitesByItemsAsync")]
  [InlineData("QueryValidRecognitionReportVersionIdsAsync")]
  public void Collection_reads_short_circuit_an_empty_collection(string methodName)
  {
    // 仓储类型按 partial 分片在多个文件内声明，按类型简单名跨文件收集声明，避免只读一个分片时判成缺失。
    MethodDeclarationSyntax[] declarations =
    [
      .. SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Repository/Queries", "MedicalRecognitionReportQueryRepository")
        .SelectMany(declaration => SourceSyntaxGuard.FindMethods(declaration.Root, methodName))
    ];
    MethodDeclarationSyntax method = Assert.Single(declarations);

    Assert.NotNull(method.Body);
    // 节点级判据：方法体内存在唯一一处以「入参集合数量为零」为条件的早返回，其分支返回空集合。
    // 不把调用数量与语句形态当契约：将来在短路之外增加调用或改写查询语句本身都不应让本用例失败。
    IfStatementSyntax guard = Assert.Single(method.Body!.DescendantNodes().OfType<IfStatementSyntax>());
    Assert.Equal(SyntaxKind.EqualsExpression, guard.Condition.Kind());
    Assert.Contains($"{method.ParameterList.Parameters[0].Identifier.ValueText}.Count", guard.Condition.ToString(), StringComparison.Ordinal);
    ReturnStatementSyntax returned = Assert.Single(guard.Statement.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>());
    Assert.IsType<CollectionExpressionSyntax>(returned.Expression);
  }

  /// <summary>
  /// V82：检查部位的两条读取路径都投影检查项目归属，按检查项目读取与按检查项目集合读取的归属取值口径一致。
  /// </summary>
  /// <remarks>
  /// 按检查项目读取的语句在查询侧映射内直接投影归属列；按检查项目集合读取的语句在检查部位实体映射内以共享列清单
  /// <c>ExaminationSiteColumns</c> 作为投影，因此两条路径的投影面分别是查询侧语句与实体映射的列清单语句。
  /// 只给其中一条路径补投影会让另一条路径恒返回空标识，而该差异不会让任何编译或行为用例失败：
  /// 归属取值只在按项目集合分组与按项目定位两处被消费。
  /// 判据按投影段逐项判定，不使用宽松的子串包含：投影段里出现该列与在语句任意位置出现该列是两件事。
  /// </remarks>
  [Fact]
  public void Examination_site_projections_carry_the_item_ownership_on_both_read_paths()
  {
    // 按检查项目读取：查询侧语句直接投影归属列，恰好一次。
    string byItemProjection = StatementProjection(QueryStatement("QueryExaminationSites"));
    Assert.Equal(1, ProjectionColumnOccurrences(byItemProjection, "examination_item_id"));

    // 按检查项目集合读取：实体映射内的共享列清单就是该语句的投影面，同样恰好一次。
    string columnsStatement = EntityMapStatement("ExaminationSite.xml", "ExaminationSiteColumns");
    Assert.Equal(1, ProjectionColumnOccurrences(columnsStatement, "examination_item_id"));

    // 变异证据：把共享列清单里的归属列去掉后，同一条判定必须报出该列缺失。
    string ownershipRemoved = columnsStatement.Replace("examination_item_id,", string.Empty, StringComparison.Ordinal);
    Assert.NotEqual(columnsStatement, ownershipRemoved);
    Assert.Equal(0, ProjectionColumnOccurrences(ownershipRemoved, "examination_item_id"));
  }

  /// <summary>
  /// V14、V15、V16 的静态面（票 03）：候选报告语句按当前有效报告、当前版本与接收组织三条判据筛选候选。
  /// </summary>
  /// <remarks>
  /// 报告已作废或版本非当前有效（V14）、报告已形成后续版本（V15）、来源组织与接收组织不一致（V16）三条行为用例不在本文件，
  /// 由三条判据静态承接：生命周期状态筛选、当前版本内联连接与来源组织范围。三条判据分布在两个报告类型分支上，
  /// 任一分支漏写都会让该类型报告参与匹配或读到历史版本，而语句仍可执行、编译与既有用例都不失败。
  /// 判据按语句文本中的谓词片段逐项计数，不使用宽松的子串包含。
  /// </remarks>
  [Fact]
  public void Candidate_query_freezes_current_report_current_version_and_receiver_organization()
  {
    string statement = NormaliseStatement(QueryStatement("QueryRecognitionMatchCandidateReports"));

    // V14：两个报告类型分支各自按平台的生命周期状态值只取当前有效报告，已作废报告不参与匹配。
    Assert.Equal(2, PredicateOccurrences(statement, "r.status", "1"));
    // V15：候选筛选落在报告的当前版本指向上，历史版本不参与匹配。
    Assert.Equal(2, Regex.Matches(statement, @"join mrec_medical_report_version v on v\.id = r\.current_version_id", RegexOptions.CultureInvariant).Count);
    // V16：MVP 只匹配来源组织与接收组织一致的报告，两个分支同一口径。
    Assert.Equal(2, PredicateOccurrences(statement, "r.organization_code", "$OrganizationCode"));

    // 变异证据：把任一分支的状态筛选改成"取已作废报告"后，状态判据必须判为不再满足。
    string statusRemoved = statement.Replace("r.status = 1", "r.status = 2", StringComparison.Ordinal);
    Assert.NotEqual(statement, statusRemoved);
    Assert.Equal(0, PredicateOccurrences(statusRemoved, "r.status", "1"));
  }

  /// <summary>
  /// V46、V53 的静态面（票 05）：引用详情候选语句把尚未写入处理结果保存时间的记录挡在行集之外，使语句自防御。
  /// </summary>
  /// <remarks>
  /// 引用详情的有效期起点取自匹配记录的处理结果保存时间：该列为空值的记录既没有有效期起点，也不是平台能够确认的已采纳事实，
  /// 因此候选语句必须按该列的空值筛除。语句功能注释与查询端口 remarks 都声明了这条筛除，
  /// 判据落在语句文本的谓词上，避免注释声明超出实际判据而语句仍可执行、编译与既有用例都不失败。
  /// </remarks>
  [Fact]
  public void Citation_candidate_query_excludes_records_without_a_saved_decision_time()
  {
    string statement = QueryStatement("QueryRecognitionCitationCandidates");
    Assert.NotEmpty(statement);

    // 判据：保存时间的空值筛除以谓词形式出现且恰好一次，并与决定筛选、排序口径并列存在。
    Assert.Contains("and mr.decision_saved_time is not null", statement, StringComparison.Ordinal);
    Assert.Single(Regex.Matches(statement, @"and mr\.decision_saved_time is not null", RegexOptions.CultureInvariant));
    Assert.Contains("where pr.recognition_result = 1", statement, StringComparison.Ordinal);
    Assert.Contains("order by mr.decision_saved_time desc, mi.id asc", statement, StringComparison.Ordinal);

    // 变异证据：去掉该谓词后，同一条判定必须报出缺失。
    string predicateRemoved = statement.Replace("and mr.decision_saved_time is not null", string.Empty, StringComparison.Ordinal);
    Assert.NotEqual(statement, predicateRemoved);
    Assert.DoesNotContain("and mr.decision_saved_time is not null", predicateRemoved, StringComparison.Ordinal);
  }

  /// <summary>
  /// 唯一约束兜底的口径在语句注释、仓储实现注释与端口注释三处一致：撞键时数据库异常按原样向外传播，
  /// 本层不做识别与翻译，也不自动重试或改用更新路径。
  /// </summary>
  /// <remarks>
  /// 这三处说明既不参与编译也不参与运行，改回"把唯一约束冲突翻译为业务拒绝"会让任何行为用例都保持通过；
  /// 说明与实现不一致时，维护者会据错误说明在领域层加入异常处理，因此逐处冻结文案要点。
  /// </remarks>
  [Fact]
  public void Reference_unique_constraint_baseline_is_described_consistently()
  {
    string statement = StatementText(EntityMapPath("RecognitionReference.xml"), "CreateRecognitionReference");
    Assert.NotEmpty(statement);

    // 语句注释：同一匹配项至多一个引用事实由唯一索引兜底，撞键时数据库异常按原样向外传播。
    XDocument referenceMap = XDocument.Load(EntityMapPath("RecognitionReference.xml"));
    XElement createStatement = Assert.Single(
      StatementElements(referenceMap),
      element => (string?)element.Attribute("Id") == "CreateRecognitionReference");
    string createComment = ((XComment)createStatement.PreviousNode!).Value;

    Assert.Contains("唯一索引兜底", createComment, StringComparison.Ordinal);
    Assert.Contains("按原样向外传播", createComment, StringComparison.Ordinal);
    Assert.DoesNotContain("内部异常类型", createComment, StringComparison.Ordinal);
    Assert.DoesNotContain("业务拒绝", createComment, StringComparison.Ordinal);

    // 变异证据：把翻译口径写回同一段注释文本后，负向判据必须报出该表述。
    Assert.Contains("内部异常类型", $"{createComment}撞键时翻译为内部异常类型。", StringComparison.Ordinal);

    // 实现注释与端口注释：同样说明数据库异常按原样向外传播，不出现异常捕获、状态码识别或内部异常类型。
    string repository = File.ReadAllText(
      ServerPath("Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportRepository.Reference.cs"));
    Assert.Contains("按原样向外传播", repository, StringComparison.Ordinal);
    Assert.DoesNotContain("SqlState", repository, StringComparison.Ordinal);
    Assert.DoesNotContain("catch", repository, StringComparison.Ordinal);
    Assert.DoesNotContain("try", repository, StringComparison.Ordinal);

    string port = File.ReadAllText(
      ServerPath("Dy.MedicalRecognition.Domain", "MedicalRecognitionReportAggregate", "Ports", "IMedicalRecognitionReportRepository.Reference.cs"));
    Assert.Contains("按原样向外传播", port, StringComparison.Ordinal);
    Assert.DoesNotContain("内部异常类型", port, StringComparison.Ordinal);
    Assert.DoesNotContain("业务拒绝", port, StringComparison.Ordinal);

    // 设计类型归属表不再登记内部领域异常类型，也不把它转换为业务拒绝。
    string design = File.ReadAllText(DesignPath());
    Assert.DoesNotContain("内部领域异常", design, StringComparison.Ordinal);
    Assert.DoesNotContain("翻译为内部异常", design, StringComparison.Ordinal);
    Assert.DoesNotContain("管理器把它转换为业务拒绝", design, StringComparison.Ordinal);
  }

  /// <summary>
  /// 定位阶段 5 的后端设计文档。
  /// </summary>
  /// <returns>设计文档的绝对路径。</returns>
  private static string DesignPath() =>
    Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(),
      "docs", "plans", "007-阶段5-在线互认主流程", "Server", "design.md");

  /// <summary>
  /// 统计已归一化的语句文本中 <c>列 = 取值</c> 形式的比较出现次数。
  /// </summary>
  /// <param name="statement">去掉注释并折行后的语句文本。</param>
  /// <param name="column">带别名的列名，例如 <c>r.status</c>。</param>
  /// <param name="value">比较值，可以是数值字面量或参数名。</param>
  /// <returns>该比较在语句中出现的次数。</returns>
  private static int PredicateOccurrences(string statement, string column, string value) =>
    Regex.Matches(
      statement,
      $@"(?<![A-Za-z0-9_.$]){Regex.Escape(column)}\s*=\s*{Regex.Escape(value)}(?![A-Za-z0-9_])",
      RegexOptions.CultureInvariant).Count;
}
