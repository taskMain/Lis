using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 的 DDL 与 SqlMap 静态守卫：冻结报告采集与生命周期十张表的建表脚本物理形态，
/// 以及十个映射文件的作用域、语句标识清单、目标物理表与方言中立性。
/// </summary>
/// <remarks>
/// 覆盖矩阵 V80、V80b 的脚本面与 V81 的映射面（作用域、物理表、无方言特征）。
/// 这些内容改名、漏写或换序既不会编译失败，也不会让阶段 1 至阶段 3 的用例失败，因此必须在这里独立冻结；
/// 目标库中的实际结构、注释与索引属于建表完成后的验收面，不在本文件内验证。
/// </remarks>
public sealed class Stage4SqlMapTests
{
  /// <summary>
  /// 每个报告表建表脚本的物理表名、表注释、列序（列名与完整类型声明）与索引声明；
  /// 列序按项目 DDL 规则：主键、UML 业务列序、生命周期状态标志、操作时间、操作人。
  /// </summary>
  private static readonly (string ScriptFile, string TableName, string TableComment, (string Name, string Type)[], string[] IndexLines)[] ExpectedTables =
  [
    (
      "mrec_platform_patient.sql", "mrec_platform_patient", "平台患者",
      [
        ("id", "uuid not null"),
        ("identity_document_type_code", "text not null"),
        ("identity_document_no", "text not null"),
        ("patient_name", "text not null"),
        ("patient_gender_code", "text not null"),
        ("patient_birth_date", "timestamp without time zone not null"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_platform_patient (identity_document_type_code, identity_document_no);"]
    ),
    (
      "mrec_medical_recognition_report.sql", "mrec_medical_recognition_report", "互认报告",
      [
        ("id", "uuid not null"),
        ("organization_code", "text not null"),
        ("hospital_code", "text not null"),
        ("branch_code", "text not null"),
        ("report_type", "integer not null"),
        ("report_no", "text not null"),
        ("patient_id", "uuid not null"),
        ("current_version_id", "uuid not null"),
        ("report_time", "timestamp without time zone not null"),
        ("patient_name", "text not null"),
        ("identity_document_no", "text not null"),
        ("status", "integer not null"),
        ("voided_time", "timestamp without time zone"),
        ("void_reason", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      [
        "on mrec_medical_recognition_report (organization_code, hospital_code, branch_code, report_type, report_no);",
        "on mrec_medical_recognition_report (organization_code, hospital_code, branch_code, report_time desc, id desc);",
        "on mrec_medical_recognition_report (patient_name text_pattern_ops);",
        "on mrec_medical_recognition_report (identity_document_no text_pattern_ops);"
      ]
    ),
    (
      "mrec_medical_report_version.sql", "mrec_medical_report_version", "报告版本",
      [
        ("id", "uuid not null"),
        ("report_id", "uuid not null"),
        ("patient_id", "uuid not null"),
        ("version_number", "integer not null"),
        ("source_report_name", "text not null"),
        ("patient_name", "text not null"),
        ("patient_gender_code", "text not null"),
        ("patient_birth_date", "timestamp without time zone not null"),
        ("patient_phone_number", "text"),
        ("age_at_report", "text"),
        ("identity_document_type_code", "text not null"),
        ("identity_document_no", "text not null"),
        ("visit_type", "integer not null"),
        ("visit_serial_no", "text not null"),
        ("application_dept_id", "text not null"),
        ("application_dept_name", "text not null"),
        ("application_doctor_id", "text not null"),
        ("application_doctor_name", "text not null"),
        ("execution_dept_id", "text not null"),
        ("execution_dept_name", "text not null"),
        ("report_dept_id", "text not null"),
        ("report_dept_name", "text not null"),
        ("report_doctor_id", "text not null"),
        ("report_doctor_name", "text not null"),
        ("review_doctor_id", "text not null"),
        ("review_doctor_name", "text not null"),
        ("review_time", "timestamp without time zone"),
        ("inpatient_no", "text"),
        ("ward_name", "text"),
        ("room_name", "text"),
        ("bed_no", "text"),
        ("application_time", "timestamp without time zone not null"),
        ("report_time", "timestamp without time zone not null"),
        ("source_modified_time", "timestamp without time zone not null"),
        ("received_time", "timestamp without time zone not null"),
        ("pdf_file_id", "text not null"),
        ("pdf_file_name", "text not null"),
        ("source_confidential_flag", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_medical_report_version (report_id, version_number);"]
    ),
    (
      "mrec_laboratory_report_content.sql", "mrec_laboratory_report_content", "检验报告专项内容",
      [
        ("id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("report_category_code", "text"),
        ("report_category_name", "text"),
        ("report_remark", "text"),
        ("overall_abnormal_flag", "text"),
        ("source_order_serial_no", "text"),
        ("specimen_collected_time", "timestamp without time zone"),
        ("specimen_submitted_time", "timestamp without time zone"),
        ("laboratory_received_time", "timestamp without time zone"),
        ("source_specimen_no", "text not null"),
        ("specimen_type_code", "text not null"),
        ("specimen_type_name", "text not null"),
        ("testing_completed_time", "timestamp without time zone not null"),
        ("inspector_id", "text not null"),
        ("inspector_name", "text not null"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_laboratory_report_content (report_version_id);"]
    ),
    (
      "mrec_laboratory_result_item.sql", "mrec_laboratory_result_item", "普通检验结果",
      [
        ("id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("source_detail_key", "text"),
        ("source_project_name", "text not null"),
        ("source_project_code", "text"),
        ("standard_project_code", "text"),
        ("source_result_text", "text not null"),
        ("result_type", "integer not null"),
        ("loinc_code", "text"),
        ("unit", "text"),
        ("reference_range", "text"),
        ("testing_method", "text"),
        ("instrument_code", "text"),
        ("instrument_name", "text"),
        ("display_order", "integer not null"),
        ("abnormal_flag", "integer"),
        ("critical_value_flag", "boolean"),
        ("laboratory_charge_item_code", "text"),
        ("insurance_charge_item_code", "text"),
        ("inspector_id", "text"),
        ("inspector_name", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_laboratory_result_item (report_version_id);"]
    ),
    (
      "mrec_laboratory_bacteria_result.sql", "mrec_laboratory_bacteria_result", "细菌鉴定结果",
      [
        ("id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("source_detail_key", "text"),
        ("source_organism_code", "text"),
        ("source_organism_name", "text"),
        ("source_result_text", "text not null"),
        ("detection_conclusion", "text not null"),
        ("colony_count", "text"),
        ("culture_medium", "text"),
        ("culture_time", "text"),
        ("culture_condition", "text"),
        ("discovery_method", "text"),
        ("detection_method", "text"),
        ("description", "text"),
        ("instrument_code", "text"),
        ("instrument_name", "text"),
        ("test_panel_code", "text"),
        ("test_panel_name", "text"),
        ("inspector_id", "text"),
        ("inspector_name", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_laboratory_bacteria_result (report_version_id);"]
    ),
    (
      "mrec_laboratory_antimicrobial_susceptibility.sql", "mrec_laboratory_antimicrobial_susceptibility", "药敏结果",
      [
        ("id", "uuid not null"),
        ("bacteria_result_id", "uuid not null"),
        ("source_detail_key", "text"),
        ("drug_code", "text"),
        ("drug_name", "text not null"),
        ("susceptibility_code", "text"),
        ("source_conclusion_text", "text not null"),
        ("resistance_result_code", "text"),
        ("disk_content", "text"),
        ("mic_value", "text"),
        ("inhibition_zone_diameter", "text"),
        ("reference_value", "text"),
        ("display_order", "integer not null"),
        ("inspector_id", "text"),
        ("inspector_name", "text"),
        ("testing_method", "text"),
        ("test_panel_order", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_laboratory_antimicrobial_susceptibility (bacteria_result_id);"]
    ),
    (
      "mrec_examination_report_content.sql", "mrec_examination_report_content", "检查报告专项内容",
      [
        ("id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("source_examination_type_code", "text"),
        ("source_examination_type_name", "text"),
        ("report_remark", "text"),
        ("overall_abnormal_flag", "text"),
        ("findings", "text not null"),
        ("conclusion", "text not null"),
        ("condition_description", "text"),
        ("examination_purpose", "text"),
        ("source_diagnosis_code", "text"),
        ("source_diagnosis_name", "text not null"),
        ("examination_time", "timestamp without time zone not null"),
        ("examiner_id", "text not null"),
        ("examiner_name", "text not null"),
        ("source_image_status", "integer not null"),
        ("image_access_url", "text"),
        ("examination_method", "text"),
        ("device_code", "text"),
        ("device_name", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_examination_report_content (report_version_id);"]
    ),
    (
      "mrec_examination_item.sql", "mrec_examination_item", "检查项目",
      [
        ("id", "uuid not null"),
        ("report_version_id", "uuid not null"),
        ("source_project_name", "text not null"),
        ("source_project_code", "text"),
        ("standard_project_code", "text"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_examination_item (report_version_id);"]
    ),
    (
      "mrec_examination_site.sql", "mrec_examination_site", "检查部位",
      [
        ("id", "uuid not null"),
        ("examination_item_id", "uuid not null"),
        ("source_site_code", "text"),
        ("site_name", "text not null"),
        ("oper_time", "timestamptz not null"),
        ("oper_id", "uuid not null")
      ],
      ["on mrec_examination_site (examination_item_id);"]
    )
  ];

  /// <summary>
  /// 报告主体的两列文本前缀索引名称与列；索引写法属建表脚本的实现细节，只出现在脚本内。
  /// </summary>
  private static readonly (string IndexName, string Definition)[] ExpectedPrefixIndexes =
  [
    ("ix_mrec_medical_recognition_report_patient_name", "on mrec_medical_recognition_report (patient_name text_pattern_ops);"),
    ("ix_mrec_medical_recognition_report_identity_document_no", "on mrec_medical_recognition_report (identity_document_no text_pattern_ops);")
  ];

  /// <summary>
  /// 每个映射文件的作用域名与语句标识清单，顺序与文件内声明顺序一致；
  /// 这些标识被仓储调用点逐条显式引用，拼写与集合都必须逐字冻结。
  /// </summary>
  private static readonly (string MapFile, string Scope, string[] StatementIds)[] ExpectedMaps =
  [
    ("PlatformPatient.xml", "PlatformPatient",
      ["PlatformPatientColumns", "GetPlatformPatientByDocument", "InsertPlatformPatient"]),
    ("MedicalRecognitionReport.xml", "MedicalRecognitionReport",
      [
        "MedicalRecognitionReportColumns", "GetMedicalRecognitionReportByBusinessKey", "GetMedicalRecognitionReportById",
        "InsertMedicalRecognitionReport", "UpdateMedicalRecognitionReportCurrentVersion", "VoidMedicalRecognitionReport"
      ]),
    ("MedicalReportVersion.xml", "MedicalReportVersion",
      [
        "MedicalReportVersionColumns", "GetMedicalReportMaxVersionNumber", "GetMedicalReportVersionById",
        "QueryMedicalReportVersionList", "InsertMedicalReportVersion"
      ]),
    ("LaboratoryReportContent.xml", "LaboratoryReportContent",
      ["LaboratoryReportContentColumns", "GetLaboratoryReportContentByVersion", "InsertLaboratoryReportContent"]),
    ("LaboratoryResultItem.xml", "LaboratoryResultItem",
      ["LaboratoryResultItemColumns", "QueryLaboratoryResultItemsByVersion", "InsertLaboratoryResultItem"]),
    ("LaboratoryBacteriaResult.xml", "LaboratoryBacteriaResult",
      ["LaboratoryBacteriaResultColumns", "QueryLaboratoryBacteriaResultsByVersion", "InsertLaboratoryBacteriaResult"]),
    ("LaboratoryAntimicrobialSusceptibility.xml", "LaboratoryAntimicrobialSusceptibility",
      [
        "LaboratoryAntimicrobialSusceptibilityColumns", "QueryLaboratoryAntimicrobialSusceptibilitiesByBacteria",
        "InsertLaboratoryAntimicrobialSusceptibility"
      ]),
    ("ExaminationReportContent.xml", "ExaminationReportContent",
      ["ExaminationReportContentColumns", "GetExaminationReportContentByVersion", "InsertExaminationReportContent"]),
    ("ExaminationItem.xml", "ExaminationItem",
      ["ExaminationItemColumns", "QueryExaminationItemsByVersion", "InsertExaminationItem"]),
    ("ExaminationSite.xml", "ExaminationSite",
      ["ExaminationSiteColumns", "QueryExaminationSitesByItem", "InsertExaminationSite"])
  ];

  /// <summary>
  /// 查询侧映射文件的完整语句标识清单，按文件内声明顺序：阶段 1 至阶段 3 已交付的六条在前，阶段 4 新增的十四条在后。
  /// </summary>
  private static readonly string[] ExpectedQueryStatementIds =
  [
    "QueryMedicalStandardCategoryList", "QueryMedicalStandardGroupList", "QueryMedicalStandardItemList",
    "QueryEffectiveMedicalStandardCatalog", "QueryRecognitionProjectConfigurationList", "QueryRecognitionAmountList",
    "CountMedicalReportList", "QueryMedicalReportList", "QueryMedicalReportVersionList", "GetMedicalReportVersionDetail",
    "GetMedicalReportScope", "GetMedicalReportVersionCommon", "GetMedicalReportVersionFile",
    "GetLaboratoryReportContentByVersion", "QueryLaboratoryResultItemsByVersion", "QueryLaboratoryBacteriaResultsByVersion",
    "QueryLaboratorySusceptibilitiesByBacteria", "GetExaminationReportContentByVersion", "QueryExaminationItemsByVersion",
    "QueryExaminationSitesByItem"
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
    ("JSON 运算符 jsonb", @"\bJSONB\b"),
    ("大小写不敏感比较 ilike", @"\bILIKE\b"),
    ("系统表 information_schema", @"\bINFORMATION_SCHEMA\b"),
    ("查询提示 Oracle 风格", @"/\*\+"),
    ("查询提示 with (nolock)", @"\bWITH\s*\(\s*NOLOCK\s*\)"),
    ("查询提示 option (", @"\bOPTION\s*\("),
    ("查询提示 readpast", @"\bREADPAST\b"),
    ("查询提示 holdlock", @"\bHOLDLOCK\b"),
    ("冲突处理 on conflict", @"\bON\s+CONFLICT\b"),
    ("数据库当前时间 current_timestamp", @"\bCURRENT_TIMESTAMP\b"),
    ("行值聚合 count(distinct (", @"COUNT\s*\(\s*DISTINCT\s*\(")
  ];

  /// <summary>
  /// V80：十张表的建表脚本冻结物理表名、表注释、列序（列名与类型）、可空性、每列中文注释与索引声明，
  /// 并且不含 <c>varchar(n)</c>、数据库默认值、跨系统外键与后续结构变更语句。
  /// </summary>
  /// <remarks>
  /// 列序同时决定投影绑定与写入稳定性：加列、换序、改类型或放宽可空性都不会让任何编译或映射用例失败。
  /// 每条判据都带变异证据，证明它对内容敏感而不是对任何文本都成立。
  /// </remarks>
  [Fact]
  public void Report_ddl_freezes_table_names_columns_types_comments_and_indexes()
  {
    foreach ((string scriptFile, string tableName, string tableComment, (string Name, string Type)[] columns, string[] indexLines) in ExpectedTables)
    {
      string script = File.ReadAllText(FindRepositoryFile(scriptFile));

      // 物理表名与建表形态：平台自有表统一带 mrec_ 前缀，脚本只做一次建表。
      Assert.Contains($"create table {tableName} (", script, StringComparison.Ordinal);
      Assert.DoesNotContain("if not exists", script, StringComparison.OrdinalIgnoreCase);

      // 列序与完整类型声明逐项等值。
      Assert.Equal(columns, ExtractCreateTableColumnLines(script, tableName));

      // 每列一条中文注释，条数与列数一致，且文案非空。
      string[] columnCommentLines =
      [
        .. script.Split('\n')
          .Select(line => line.Trim())
          .Where(line => line.StartsWith($"comment on column {tableName}.", StringComparison.Ordinal))
      ];
      Assert.Equal(columns.Length, columnCommentLines.Length);
      foreach ((string column, string _) in columns)
      {
        string commentLine = Assert.Single(columnCommentLines, line => line.StartsWith($"comment on column {tableName}.{column} is '", StringComparison.Ordinal));
        string comment = commentLine[(commentLine.IndexOf(" is '", StringComparison.Ordinal) + " is '".Length)..].TrimEnd(';', '\'');
        Assert.False(string.IsNullOrWhiteSpace(comment), $"{tableName}.{column} 缺乏中文注释。");
      }

      // 表注释逐字冻结。
      Assert.Contains($"comment on table {tableName} is '{tableComment}';", script, StringComparison.Ordinal);

      // 索引声明逐条等值，且每条索引都带中文注释。
      foreach (string indexLine in indexLines)
      {
        Assert.Contains(indexLine, script, StringComparison.Ordinal);
      }

      // 五条负向判据：文本列不用带长度上限的 varchar、无数据库默认值、无跨系统外键、无后续结构变更、无带状态过滤的索引。
      Assert.DoesNotContain("varchar(", script, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain(" default ", script, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("references ", script, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("foreign key", script, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("alter table", script, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain(" where ", script, StringComparison.OrdinalIgnoreCase);

      // 变异证据：注入负向特征后同一条判据必须命中，证明上面的断言不是对任何文本都成立。
      Assert.Contains("varchar(", script.Replace("text not null", "varchar(100) not null", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
      Assert.Contains(" default ", script.Replace("uuid not null", "uuid not null default gen_random_uuid()", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
      Assert.Contains("alter table", script + $"\nalter table {tableName} add column probe text;", StringComparison.OrdinalIgnoreCase);
    }
  }

  /// <summary>
  /// V80：报告主体的三列检索位置与两列文本前缀索引逐字冻结。
  /// </summary>
  /// <remarks>
  /// 报告时间、患者姓名与证件号码是从当前版本冗余到报告主体的业务列，列位置固定在当前版本指向之后、生命周期状态之前；
  /// 两列前缀索引使用与排序规则匹配的文本模式运算符类，改名、换列或去掉运算符类都会让前缀筛选失去索引支撑。
  /// </remarks>
  [Fact]
  public void Report_ddl_keeps_search_columns_position_and_text_prefix_indexes()
  {
    string script = File.ReadAllText(FindRepositoryFile("mrec_medical_recognition_report.sql"));
    (string Name, string Type)[] columns = ExtractCreateTableColumnLines(script, "mrec_medical_recognition_report");

    string[] columnNames = [.. columns.Select(column => column.Name)];
    int currentVersionIndex = Array.IndexOf(columnNames, "current_version_id");
    int statusIndex = Array.IndexOf(columnNames, "status");
    Assert.True(currentVersionIndex >= 0 && statusIndex > currentVersionIndex);
    Assert.Equal(["report_time", "patient_name", "identity_document_no"], columnNames[(currentVersionIndex + 1)..statusIndex]);
    Assert.Equal("timestamp without time zone not null", columns.Single(column => column.Name == "report_time").Type);
    Assert.Equal("text not null", columns.Single(column => column.Name == "patient_name").Type);
    Assert.Equal("text not null", columns.Single(column => column.Name == "identity_document_no").Type);

    foreach ((string indexName, string definition) in ExpectedPrefixIndexes)
    {
      Assert.Contains($"create index {indexName}", script, StringComparison.Ordinal);
      Assert.Contains(definition, script, StringComparison.Ordinal);
      Assert.Contains($"comment on index {indexName} is '", script, StringComparison.Ordinal);
    }

    // 变异证据：去掉运算符类后判据必须判为不成立；把 report_time 移到 status 之后同样必须判为不成立。
    string withoutOperatorClass = script.Replace(" (patient_name text_pattern_ops);", " (patient_name);", StringComparison.Ordinal);
    Assert.NotEqual(script, withoutOperatorClass);
    Assert.DoesNotContain(ExpectedPrefixIndexes[0].Definition, withoutOperatorClass, StringComparison.Ordinal);
    Assert.NotEqual(
      ["report_time", "patient_name", "identity_document_no"],
      new[] { "patient_name", "identity_document_no", "report_time" });
  }

  /// <summary>
  /// V81：十个映射文件的作用域为实体名，语句标识清单与声明顺序与设计一致，且已移除无业务调用点的无条件全量查询。
  /// </summary>
  /// <remarks>
  /// 作用域曾为聚合名；框架按 <c>{作用域}.{语句标识}</c> 查找语句，作用域或标识不一致会让调用点在实际运行时报找不到语句，
  /// 静态检查不会失败。每条手写语句还必须有说明职责的 XML 注释。
  /// </remarks>
  [Fact]
  public void Report_sql_maps_freeze_scopes_statement_ids_and_remove_unused_queries()
  {
    foreach ((string mapFile, string scope, string[] statementIds) in ExpectedMaps)
    {
      XDocument document = XDocument.Load(FindRepositorySqlMap(mapFile));

      Assert.Equal(scope, (string?)document.Root!.Attribute("Scope"));

      // 作用域必须是实体名，与聚合名同名的报告实体除外：报告实体的实体名与聚合名恰好相同，
      // 因此这里按"作用域等于声明的实体名"判定，其余实体（含阶段 5 的匹配与引用实体）仍使用聚合名。
      Assert.NotEqual("MedicalRecognitionReportAggregate", scope);
      Assert.Contains(scope, mapFile, StringComparison.Ordinal);

      // 只取 Statement 节点：映射文件里的 ParameterMap 也带 Id，按任意 Id 取会把参数映射误判成语句。
      string[] actualIds = [.. StatementIds(document)];
      Assert.Equal(statementIds, actualIds);

      // 无条件全量查询必须消失：它既不满足业务键或主键定位语义，也没有调用方。
      Assert.DoesNotContain("QueryAll", document.ToString(), StringComparison.Ordinal);

      // 每条手写语句都有说明职责的 XML 注释。
      Assert.All(
        StatementElements(document),
        statement => Assert.True(
          statement.PreviousNode is XComment,
          $"语句 {statement.Attribute("Id")?.Value} 缺少 XML 注释"));
    }

    // 变异证据：把语句标识清单的副本改掉一项后，等值判据必须判为不相等。
    string[] mutatedIds = [.. ExpectedMaps[0].StatementIds];
    mutatedIds[0] = "PlatformPatientColumn";
    Assert.NotEqual(ExpectedMaps[0].StatementIds, mutatedIds);
  }

  /// <summary>
  /// V81：查询侧映射文件包含本阶段新增的全部查询语句。
  /// </summary>
  /// <remarks>
  /// 查询语句与计数语句共用同一作用域；语句标识清单被冻结后，列表查询、版本列表、版本详情、内容读取与文件信息读取
  /// 逐条可核对，缺失或改名都会在这里失败。
  /// </remarks>
  [Fact]
  public void Report_query_sql_map_contains_every_stage_statement()
  {
    XDocument document = XDocument.Load(FindRepositorySqlMap("MedicalRecognitionReportQuery.xml"));
    Assert.Equal("MedicalRecognitionReportQuery", (string?)document.Root!.Attribute("Scope"));

    // 只取 Statement 节点：映射文件里的 ParameterMap 也带 Id，按任意 Id 取会把参数映射误判成语句。
    string[] actualIds = [.. StatementIds(document)];

    // 语句清单逐项等值：缺一条或多一条都会在这里失败，因此不另设宽松的包含断言。
    Assert.Equal(ExpectedQueryStatementIds, actualIds);

    // 每条手写语句都有说明职责的 XML 注释。
    Assert.All(
      StatementElements(document),
      statement => Assert.True(
        statement.PreviousNode is XComment,
        $"语句 {statement.Attribute("Id")?.Value} 缺少 XML 注释"));

    // 计数语句与数据语句共用同一套筛选条件：两条语句都必须引用同一组筛选参数。
    Dictionary<string, string> queryStatements = LoadStatements(File.ReadAllText(FindRepositorySqlMap("MedicalRecognitionReportQuery.xml")));
    string count = queryStatements["CountMedicalReportList"];
    string page = queryStatements["QueryMedicalReportList"];
    foreach (string filter in new[] { "$OrganizationCode", "$HospitalCode", "$BranchCode", "$ReportTimeFrom", "$ReportTimeTo", "$ReportType", "$ReportNo", "$IdentityDocumentNoPrefix", "$PatientNamePrefix" })
    {
      Assert.Contains(filter, count, StringComparison.Ordinal);
      Assert.Contains(filter, page, StringComparison.Ordinal);
    }

    // 排序键与分页窗口排序逐字一致；报告时间恒有值，因此不设空值排序分支。
    Assert.Contains("order by r.report_time desc, r.id desc", page, StringComparison.Ordinal);
    Assert.DoesNotContain("nulls first", page, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("nulls last", page, StringComparison.OrdinalIgnoreCase);

    // 变异证据：把排序键副本改成升序后，同一条判据必须判为不成立。
    string ascending = page.Replace("order by r.report_time desc, r.id desc", "order by r.report_time asc, r.id asc", StringComparison.Ordinal);
    Assert.NotEqual(page, ascending);
    Assert.DoesNotContain("order by r.report_time desc, r.id desc", ascending, StringComparison.Ordinal);
  }

  /// <summary>
  /// V67：列表排序稳定——排序键为报告时间倒序加主键倒序，报告时间恒有值因此排序是全序，跨页无重复无遗漏。
  /// </summary>
  /// <remarks>
  /// 判据有三：数据语句的排序键与分页窗口排序逐字一致；报告时间取自报告主体的检索列、列为非空，
  /// 因此不存在需要空值排序分支的并列关系；主键作为兜底键使同一报告时间下的多行也有确定顺序。
  /// 真实多页数据的跨页无重复无遗漏由宿主验收与分页行为用例覆盖，本用例固定静态判据。
  /// </remarks>
  [Fact]
  public void V67_list_sort_is_stable_and_total()
  {
    Dictionary<string, string> statements = LoadStatements(File.ReadAllText(FindRepositorySqlMap("MedicalRecognitionReportQuery.xml")));
    string page = statements["QueryMedicalReportList"];

    // 排序键：报告时间倒序，主键倒序兜底，两者都不带方言化的空值排序分支。
    Assert.Contains("order by r.report_time desc, r.id desc", page, StringComparison.Ordinal);
    Assert.DoesNotContain("nulls first", page, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("nulls last", page, StringComparison.OrdinalIgnoreCase);

    // 报告时间列在最终脚本里非空，因此排序键恒有值，不需要空值分支。
    string script = File.ReadAllText(FindRepositoryFile("mrec_medical_recognition_report.sql"));
    Assert.Matches(@"(?is)report_time\s+timestamp\s+without\s+time\s+zone\s+not\s+null", script);

    // 变异证据：去掉主键兜底键后同一判据必须不成立，证明断言锚定在完整排序键上。
    string withoutTieBreaker = page.Replace("order by r.report_time desc, r.id desc", "order by r.report_time desc", StringComparison.Ordinal);
    Assert.NotEqual(page, withoutTieBreaker);
    Assert.DoesNotContain("order by r.report_time desc, r.id desc", withoutTieBreaker, StringComparison.Ordinal);
  }

  /// <summary>
  /// V81：十个映射文件与查询映射文件只访问 <c>mrec_</c> 前缀表，不调用共享映射器的上下文设置方法，且不含方言特征。
  /// </summary>
  /// <remarks>
  /// 作用域在本项目中一律由调用点显式传 <c>scope</c>，共享映射器的可变作用域依赖已被移除；
  /// 方言特征会让共享 SQL 绑定到单一数据库 Provider。
  /// </remarks>
  [Fact]
  public void Report_sql_maps_target_platform_tables_only_and_stay_provider_neutral()
  {
    string[] mapFiles =
    [
      .. ExpectedMaps.Select(map => map.MapFile),
      "MedicalRecognitionReportQuery.xml"
    ];

    foreach (string mapFile in mapFiles)
    {
      string xml = File.ReadAllText(FindRepositorySqlMap(mapFile));

      Assert.DoesNotContain("SetContext", xml, StringComparison.Ordinal);

      // 全文件的表名引用都必须是带 mrec_ 前缀的本平台表。
      string[] tableNames = [.. TableNameReferences(xml).Distinct(StringComparer.Ordinal)];
      Assert.NotEmpty(tableNames);
      Assert.All(tableNames, tableName => Assert.StartsWith("mrec_", tableName, StringComparison.Ordinal));

      // 变异证据：去掉表名前缀后同一条判据必须判出未加前缀的表名。
      string unprefixed = xml.Replace("mrec_", string.Empty, StringComparison.Ordinal);
      Assert.NotEqual(xml, unprefixed);
      Assert.NotEmpty(TableNameReferences(unprefixed));
      Assert.All(TableNameReferences(unprefixed), tableName => Assert.False(tableName.StartsWith("mrec_", StringComparison.Ordinal)));

      // 方言特征扫描：把命中的标记打进输出，失败时可直接看到违规词。
      List<string> dialectHits = [];
      foreach ((string feature, string pattern) in DialectMarkers)
      {
        Match match = Regex.Match(xml, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success) dialectHits.Add($"{mapFile}: {feature} => {match.Value}");
      }

      Console.WriteLine($"{mapFile} 方言标记命中：{(dialectHits.Count == 0 ? "none" : string.Join(", ", dialectHits))}");
      Assert.Empty(dialectHits);
    }
  }

  /// <summary>
  /// V80/V81：写入语句显式绑定命令时间与实体取值，不使用数据库当前时间，也不把新增语句的启用状态交给参数。
  /// </summary>
  /// <remarks>
  /// 新增与更新的操作时间必须取命令携带的服务端当前时间，使写入时间与领域事件时间一致；
  /// 生命周期状态在新增语句里写成常量，使调用方无法通过请求决定记录初始状态。
  /// </remarks>
  [Fact]
  public void Report_sql_maps_bind_operation_time_and_write_state_as_constants()
  {
    string reportMap = File.ReadAllText(FindRepositorySqlMap("MedicalRecognitionReport.xml"));
    Dictionary<string, string> reportStatements = LoadStatements(reportMap);

    Assert.Contains("$OperTime", reportStatements["InsertMedicalRecognitionReport"], StringComparison.Ordinal);
    Assert.Contains("$OperTime", reportStatements["UpdateMedicalRecognitionReportCurrentVersion"], StringComparison.Ordinal);
    Assert.Contains("$OperTime", reportStatements["VoidMedicalRecognitionReport"], StringComparison.Ordinal);
    Assert.DoesNotContain("current_timestamp", reportMap, StringComparison.OrdinalIgnoreCase);

    // 新增报告的状态由常量决定，作废语句按原有效状态条件更新，使影响行数表达并发结果。
    Assert.Contains("$Status", reportStatements["InsertMedicalRecognitionReport"], StringComparison.Ordinal);
    Assert.Contains("where id = $Id and status = 1", reportStatements["VoidMedicalRecognitionReport"], StringComparison.Ordinal);

    string versionMap = File.ReadAllText(FindRepositorySqlMap("MedicalReportVersion.xml"));
    Dictionary<string, string> versionStatements = LoadStatements(versionMap);
    Assert.Contains("$OperTime", versionStatements["InsertMedicalReportVersion"], StringComparison.Ordinal);
    Assert.DoesNotContain("current_timestamp", versionMap, StringComparison.OrdinalIgnoreCase);

    // 变异证据：把作废语句的原状态条件去掉后，同一条判据必须判为不成立。
    string withoutOriginalStatus = reportStatements["VoidMedicalRecognitionReport"].Replace("and status = 1", string.Empty, StringComparison.Ordinal);
    Assert.NotEqual(reportStatements["VoidMedicalRecognitionReport"], withoutOriginalStatus);
    Assert.DoesNotContain("and status = 1", withoutOriginalStatus, StringComparison.Ordinal);
  }

  /// <summary>
  /// 取出建表语句括号内的列定义行（列名与完整类型声明），跳过空行与主键语句。
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
  /// 收集文本中 from/join/insert into/update 之后引用的表名。
  /// </summary>
  /// <param name="text">映射文件或语句文本。</param>
  /// <returns>按出现顺序排列的表名。</returns>
  private static List<string> TableNameReferences(string text) =>
  [
    .. Regex.Matches(text, @"\b(?:from|join|insert\s+into|update)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)
      .Select(match => match.Groups[1].Value)
  ];

  /// <summary>
  /// 把映射文件里的语句读成按标识索引的文本字典。
  /// </summary>
  /// <param name="xml">映射文件全文。</param>
  /// <returns>语句标识到语句文本的映射。</returns>
  private static Dictionary<string, string> LoadStatements(string xml) =>
    StatementElements(XDocument.Parse(xml)).ToDictionary(element => (string)element.Attribute("Id")!, element => element.Value);

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

  /// <summary>
  /// 在仓储工程内按文件名定位 SqlMap 或建表脚本。
  /// </summary>
  /// <param name="fileName">文件名。</param>
  /// <returns>该文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">文件不存在时抛出。</exception>
  private static string FindRepositoryFile(string fileName)
  {
    string root = SourceSyntaxGuard.FindRepositoryRoot();
    string scriptsPath = Path.Combine(root, "server", "Dy.MedicalRecognition.Repository", "Scripts", fileName);
    if (File.Exists(scriptsPath)) return scriptsPath;

    string aggregatePath = Path.Combine(root, "server", "Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", fileName);
    if (File.Exists(aggregatePath)) return aggregatePath;

    string queriesPath = Path.Combine(root, "server", "Dy.MedicalRecognition.Repository", "Queries", fileName);
    if (File.Exists(queriesPath)) return queriesPath;

    throw new FileNotFoundException($"未找到仓储文件 '{fileName}'。");
  }

  /// <summary>
  /// 定位阶段 4 的映射文件；十个表文件位于聚合目录，查询文件位于查询目录。
  /// </summary>
  /// <param name="fileName">映射文件名。</param>
  /// <returns>该映射文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">两个候选目录都不存在该文件时抛出。</exception>
  private static string FindRepositorySqlMap(string fileName) => FindRepositoryFile(fileName);
}
