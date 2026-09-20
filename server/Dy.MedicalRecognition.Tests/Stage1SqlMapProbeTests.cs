using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Dy.MedicalRecognition.Domain.Queries;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 校验标准目录 SqlMap 与运行时注册的架构约束：新增语句不依赖请求参数写入启用状态、
/// 启停语句保留布尔字面量，并核对运行时注册出的完整语句标识与预期一致。
/// </summary>
public sealed class Stage1SqlMapProbeTests
{
  /// <summary>
  /// 校验三个标准目录的新增语句把启用状态写成常量、不引用请求参数 <c>$IsValid</c>，
  /// 避免调用方通过新增请求决定记录初始是否启用。
  /// </summary>
  [Fact]
  public void Create_statements_default_is_valid_without_request_parameter()
  {
    foreach (var fileName in new[] { "MedicalStandardCategory.xml", "MedicalStandardGroup.xml", "MedicalStandardItem.xml" })
    {
      var document = XDocument.Load(FindRepositorySqlMap(fileName));
      var create = document.Descendants().Single(element => (string?)element.Attribute("Id") == $"Create{Path.GetFileNameWithoutExtension(fileName)}").Value;

      Assert.Contains("is_valid,", create, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("true", create, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("$IsValid", create, StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// 校验三个标准目录的启用语句写入 <c>true</c>、停用语句写入 <c>false</c>，
  /// 保证启停状态由动作本身决定而不是由入参决定。
  /// </summary>
  [Fact]
  public void Enable_and_disable_statements_keep_boolean_literals()
  {
    foreach (var fileName in new[] { "MedicalStandardCategory.xml", "MedicalStandardGroup.xml", "MedicalStandardItem.xml" })
    {
      var document = XDocument.Load(FindRepositorySqlMap(fileName));
      var statements = document.Descendants()
        .Where(element => element.Attribute("Id") is not null)
        .ToDictionary(element => (string)element.Attribute("Id")!, element => element.Value);

      Assert.Contains("is_valid = true", statements.Single(statement => statement.Key.StartsWith("Enable", StringComparison.Ordinal)).Value, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("is_valid = false", statements.Single(statement => statement.Key.StartsWith("Disable", StringComparison.Ordinal)).Value, StringComparison.OrdinalIgnoreCase);
    }
  }

  /// <summary>
  /// 在不连接数据库的前提下加载宿主输出目录中的程序集并构建 SqlMap 注册表，
  /// 核对注册出的完整语句标识：聚合级标识不应存在，具体实体的新增、更新与查询标识必须齐全；
  /// 阶段 2 追加核对互认项目配置的实体作用域注册键、独立查询作用域注册键，以及旧聚合作用域键已消失。
  /// </summary>
  [Fact]
  public void Runtime_registration_reports_full_sql_ids_without_opening_database()
  {
    var hostDirectory = FindHostOutputDirectory();
    var earthraceAssembly = LoadAssembly(hostDirectory, "Dy.Earthrace.dll");
    var repositoryAssembly = LoadAssembly(hostDirectory, "Dy.MedicalRecognition.Repository.dll");
    var builderType = earthraceAssembly.GetType("Dy.Earthrace.EarthraceBuilder", throwOnError: true)!;
    var resourceType = LoadAssembly(hostDirectory, "Dy.Earthrace.Abstractions.dll")
      .GetType("Dy.Earthrace.Abstractions.ResourceType", throwOnError: true)!;
    var optionsType = earthraceAssembly.GetType("Dy.Earthrace.Options.SqlMapOptions", throwOnError: true)!;

    using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostDirectory, "EarthraceConfig.json")));
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    Flatten(json.RootElement, "", values);
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    var options = Activator.CreateInstance(optionsType);
    configuration.GetSection("SqlMapConfigs:0").Bind(options);
    Assert.NotNull(options);
    var settings = optionsType.GetProperty("Settings")!.GetValue(options)!;
    settings.GetType().GetProperty("ParameterPrefix")!.SetValue(settings, "@");
    var database = optionsType.GetProperty("Database")!.GetValue(options)!;
    var dbProvider = database.GetType().GetProperty("DbProvider")!.GetValue(database)!;
    dbProvider.GetType().GetProperty("ParameterPrefix")!.SetValue(dbProvider, "@");

    var builder = Activator.CreateInstance(builderType)!;
    RegisterBooleanTypeHandler(earthraceAssembly);
    Invoke(builder, "UseHybridConfig", options!);
    // 按本项目固定的资源命名形态注册：程序集根命名空间 + 一层目录 + 文件名，即 `{程序集名}.*.*.xml`。
    // 宿主实际还能再吃一层目录（对照项目 Dy.LisCenter 的两级目录映射即依赖这一点），更深则要靠
    // EmbeddedResource 的 LogicalName 压平；本项目不使用这些形态，映射文件固定与其仓储类同目录。
    // 这里不能按内嵌资源清单反推命名空间、也不能用 `**` 这类更宽松的通配：两者都会把"文件在磁盘上存在"
    // 当成"宿主能注册"，映射文件被挪进子目录后写侧作用域全部漏注册，本用例仍会通过。
    Invoke(builder, "AddSqlMaps", Enum.Parse(resourceType, "Embedded"),
      "Dy.MedicalRecognition.Repository.*.*.xml,Dy.MedicalRecognition.Repository",
      repositoryAssembly);

    Invoke(builder, "Build");

    var sqlConfig = builderType.GetProperty("SqlConfig")!.GetValue(builder)!;

    var registeredKeys = GetRegisteredKeys(sqlConfig);
    Assert.NotEmpty(registeredKeys);

    var aggregateKey = "MedicalRecognitionReport.CreateMedicalStandardCategory";
    var categoryKey = "MedicalStandardCategory.CreateMedicalStandardCategory";
    Console.WriteLine($"FullSqlId lookup: {aggregateKey} => {(registeredKeys.Contains(aggregateKey) ? "registered" : "not registered")}");
    Console.WriteLine($"FullSqlId lookup: {categoryKey} => {(registeredKeys.Contains(categoryKey) ? "registered" : "not registered")}");
    Console.WriteLine("Registered Statement keys:");
    foreach (var key in registeredKeys) Console.WriteLine(key);

    Assert.DoesNotContain(aggregateKey, registeredKeys);
    Assert.Contains(categoryKey, registeredKeys);
    Assert.Contains("MedicalStandardCategory.UpdateMedicalStandardCategory", registeredKeys);
    Assert.Contains("MedicalStandardGroup.CreateMedicalStandardGroup", registeredKeys);
    Assert.Contains("MedicalStandardItem.CreateMedicalStandardItem", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryMedicalStandardCategoryList", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryEffectiveMedicalStandardCatalog", registeredKeys);
    // 阶段 5 同步：四个互认实体映射补齐语句后，占位的无条件全量查询已删除，这里改为断言它不再注册；
    // 保留原来的"必须存在"断言会让删掉占位语句的补齐工作必然失败，删除该断言而不补负向断言又会让它重新出现时无人发现。
    Assert.DoesNotContain("RecognitionReference.QueryAllRecognitionReference", registeredKeys);
    Assert.Contains("RecognitionReference.RecognitionReferenceColumns", registeredKeys);

    // 阶段 2 追加核对：互认项目配置的写入、启停与按组织读取语句必须注册在实体作用域下，
    // 查询语句注册在独立查询作用域，且五个旧聚合作用域键（新增、修改、启用、停用、按标识读取）必须全部消失；
    // 缺少这些断言时，作用域或语句标识被改名不会让本用例失败，V23 会静默通过。
    Assert.DoesNotContain("MedicalRecognitionReport.CreateMutualRecognitionItem", registeredKeys);
    Assert.DoesNotContain("MedicalRecognitionReport.UpdateMutualRecognitionItemConfiguration", registeredKeys);
    Assert.DoesNotContain("MedicalRecognitionReport.EnableMutualRecognitionItem", registeredKeys);
    Assert.DoesNotContain("MedicalRecognitionReport.DisableMutualRecognitionItem", registeredKeys);
    Assert.DoesNotContain("MedicalRecognitionReport.GetMutualRecognitionItemById", registeredKeys);
    Assert.Contains("MutualRecognitionItem.CreateMutualRecognitionItem", registeredKeys);
    Assert.Contains("MutualRecognitionItem.UpdateMutualRecognitionItemConfiguration", registeredKeys);
    Assert.Contains("MutualRecognitionItem.EnableMutualRecognitionItem", registeredKeys);
    Assert.Contains("MutualRecognitionItem.DisableMutualRecognitionItem", registeredKeys);
    Assert.Contains("MutualRecognitionItem.GetMutualRecognitionItemById", registeredKeys);
    Assert.Contains("MedicalStandardItem.GetMedicalStandardItemByCode", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryRecognitionProjectConfigurationList", registeredKeys);

    // 复用的字段清单语句与全量查询语句同样必须注册在实体作用域下：这两条键此前只出现在上面的打印清单里，
    // 语句标识被改名或作用域被改动时没有任何用例会失败。
    Assert.Contains("MutualRecognitionItem.MutualRecognitionItemColumns", registeredKeys);
    Assert.Contains("MutualRecognitionItem.QueryAllMutualRecognitionItem", registeredKeys);

    // 阶段 3 追加核对：组织医院院区互认项目金额的字段清单、按业务键读取、新增与更新语句必须全部注册在实体作用域下，
    // 且两个旧聚合作用域键（字段清单、已移除的无 where 全量查询）必须消失；
    // 缺少这些断言时，金额映射的作用域被改回聚合名、或语句标识被改名，都不会让任何用例失败，V24 会静默通过。
    Assert.DoesNotContain("MedicalRecognitionReport.OrganizationHospitalBranchRecognitionAmountColumns", registeredKeys);
    Assert.DoesNotContain("MedicalRecognitionReport.QueryAllOrganizationHospitalBranchRecognitionAmount", registeredKeys);
    Assert.Contains("OrganizationHospitalBranchRecognitionAmount.OrganizationHospitalBranchRecognitionAmountColumns", registeredKeys);
    Assert.Contains("OrganizationHospitalBranchRecognitionAmount.GetOrganizationHospitalBranchRecognitionAmountByBusinessKey", registeredKeys);
    Assert.Contains("OrganizationHospitalBranchRecognitionAmount.CreateOrganizationHospitalBranchRecognitionAmount", registeredKeys);
    Assert.Contains("OrganizationHospitalBranchRecognitionAmount.UpdateOrganizationHospitalBranchRecognitionAmount", registeredKeys);

    // 阶段 3 追加核对：互认项目金额保存的"当前组织是否已建立该标准项目配置"读取语句注册在实体作用域下，
    // 以及金额列表查询语句注册在独立查询作用域下；
    // 缺少这两条断言时，语句标识或作用域被改名只在运行时表现为"找不到语句"，静态检查不会失败。
    Assert.Contains("MutualRecognitionItem.GetMutualRecognitionItemByOrganizationAndProject", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryRecognitionAmountList", registeredKeys);

    // 阶段 5 追加核对：获取引用详情的候选采纳记录、报告公共上下文与标准项目名称三条查询语句注册在同一查询作用域下；
    // 缺少这些断言时，语句标识被改名或作用域被改动只在运行时表现为"找不到语句"，静态检查不会失败。
    Assert.Contains("MedicalRecognitionReportQuery.QueryRecognitionCitationCandidates", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryRecognitionCitationReportContexts", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryRecognitionCitationStandardProjectNames", registeredKeys);

    // 阶段 4 追加核对：报告采集与生命周期十张表的映射语句必须注册在各自的实体名作用域下，
    // 报告实体的实体名与聚合名恰好相同，因此这里逐键断言而不只看作用域名称；缺少这些断言时，
    // 作用域被改回聚合级或语句标识被改名只在运行时表现为"找不到语句"，静态检查不会失败。
    foreach ((string scope, string[] statementIds) in Stage4SqlMapStatements)
    {
      foreach (string statementId in statementIds) Assert.Contains($"{scope}.{statementId}", registeredKeys);
    }

    // 每个实体必须使用自己的作用域名：框架按作用域名注册语句集合，同一作用域被多个映射文件使用时，
    // 后注册的文件会整体替换先前注册的语句集合，先注册实体的语句键随之消失并只在运行期表现为找不到语句。
    // 因此匹配与引用四个实体也各自使用实体名作用域，且旧的聚合名作用域键必须全部消失。
    // 阶段 5 补齐语句后，四个作用域的语句清单按补齐结果逐条等值冻结；不放宽为只断言作用域名或只断言其中一条语句：
    // 语句标识被改名、被删减或作用域被改回聚合名，只有这些逐条键断言才会失败。
    foreach ((string scope, string[] statementIds) in new (string, string[])[]
    {
      ("RecognitionMatchRecord",
        [
          "RecognitionMatchRecordColumns", "GetRecognitionMatchRecordByBusinessKey", "GetRecognitionMatchRecordById",
          "QueryRecognitionMatchRecordsByIds", "CreateRecognitionMatchRecord", "UpdateRecognitionMatchRecordDecisionSavedTime"
        ]),
      ("RecognitionMatchItem",
        [
          "RecognitionMatchItemColumns", "QueryRecognitionMatchItemsByRecord",
          "QueryRecognitionMatchItemsByIds", "CreateRecognitionMatchItem"
        ]),
      ("RecognitionProcessingResult",
        [
          "RecognitionProcessingResultColumns", "QueryRecognitionProcessingResultsByRecord",
          "QueryRecognitionProcessingResultsByMatchItemIds", "CreateRecognitionProcessingResult"
        ]),
      ("RecognitionReference",
        [
          "RecognitionReferenceColumns", "QueryRecognitionReferencesByMatchItemIds", "CreateRecognitionReference"
        ])
    })
    {
      foreach (string statementId in statementIds) Assert.Contains($"{scope}.{statementId}", registeredKeys);
      // 旧聚合作用域键必须消失：语句标识本身仍存在，只有前缀为聚合名时才能判出作用域未改正。
      Assert.DoesNotContain($"MedicalRecognitionReport.{statementIds[0]}", registeredKeys);
    }
  }

  /// <summary>
  /// 映射文件固定放在宿主默认资源模式覆盖的形态：程序集根命名空间 + 一层目录 + 文件名，且与它的仓储类同目录。
  /// </summary>
  /// <remarks>
  /// 宿主不按目录名注册，而是用默认模式片段（位于 <c>Dy.Earthrace.Abstractions.dll</c>）匹配内嵌资源的逻辑名。
  /// 实测与对照项目 <c>Dy.LisCenter</c> 一致：根下一级与两级目录的映射文件都会被注册，更深则需要在该文件的
  /// <c>EmbeddedResource</c> 上写 <c>LogicalName</c> 把逻辑名压回浅层（LisCenter 对唯一一份三级目录的映射就是这么做的）。
  /// 本项目不使用 <c>LogicalName</c>，映射文件与其仓储类同目录、固定在一级；本断言钉住这一约定，
  /// 避免再次出现「把映射文件挪进子目录 → 逻辑名变深 → 写侧作用域全部未注册」而编译与静态断言都不失败的情况。
  /// </remarks>
  [Fact]
  public void Sql_map_resources_stay_directly_under_one_folder()
  {
    var repositoryAssembly = LoadAssembly(FindHostOutputDirectory(), "Dy.MedicalRecognition.Repository.dll");
    string assemblyName = repositoryAssembly.GetName().Name!;

    string[] sqlMapResources =
    [
      .. repositoryAssembly.GetManifestResourceNames()
        .Where(name => name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        .OrderBy(name => name, StringComparer.Ordinal)
    ];

    Assert.Equal(20, sqlMapResources.Length);
    foreach (string resource in sqlMapResources)
    {
      Assert.StartsWith($"{assemblyName}.", resource, StringComparison.Ordinal);

      // 去掉程序集前缀与 .xml 后缀后，只允许剩下「一层目录 + 文件名」之间那一个点。
      string relative = resource[(assemblyName.Length + 1)..^4];
      Assert.Equal(1, relative.Count(character => character == '.'));

      // 变异证据：多嵌一层目录的资源名必须被判出与本项目约定不符，证明该判据对目录深度敏感。
      Assert.Equal(2, $"{relative}.Extra".Count(character => character == '.'));
    }
  }

  /// <summary>
  /// 阶段 4 十张报告表的实体名作用域与语句标识清单；与各映射文件的 <c>SqlMap Scope</c> 与 <c>Statement Id</c> 逐字一致。
  /// </summary>
  private static readonly (string Scope, string[] StatementIds)[] Stage4SqlMapStatements =
  [
    ("PlatformPatient", ["PlatformPatientColumns", "GetPlatformPatientByDocument", "CreatePlatformPatient"]),
    ("MedicalRecognitionReport",
      [
        "MedicalRecognitionReportColumns", "GetMedicalRecognitionReportByBusinessKey", "GetMedicalRecognitionReportById",
        "CreateMedicalRecognitionReport", "UpdateMedicalRecognitionReportCurrentVersion", "VoidMedicalRecognitionReport"
      ]),
    ("MedicalReportVersion",
      [
        "MedicalReportVersionColumns", "GetMedicalReportMaxVersionNumber", "GetMedicalReportVersionById",
        "QueryMedicalReportVersionList", "CreateMedicalReportVersion"
      ]),
    ("LaboratoryReportContent", ["LaboratoryReportContentColumns", "GetLaboratoryReportContentByVersion", "CreateLaboratoryReportContent"]),
    ("LaboratoryResultItem", ["LaboratoryResultItemColumns", "QueryLaboratoryResultItemsByVersion", "CreateLaboratoryResultItem"]),
    ("LaboratoryBacteriaResult", ["LaboratoryBacteriaResultColumns", "QueryLaboratoryBacteriaResultsByVersion", "CreateLaboratoryBacteriaResult"]),
    ("LaboratoryAntimicrobialSusceptibility",
      [
        "LaboratoryAntimicrobialSusceptibilityColumns", "QueryLaboratoryAntimicrobialSusceptibilitiesByBacteria",
        "CreateLaboratoryAntimicrobialSusceptibility"
      ]),
    ("ExaminationReportContent", ["ExaminationReportContentColumns", "GetExaminationReportContentByVersion", "CreateExaminationReportContent"]),
    ("ExaminationItem", ["ExaminationItemColumns", "QueryExaminationItemsByVersion", "CreateExaminationItem"]),
    ("ExaminationSite", ["ExaminationSiteColumns", "QueryExaminationSitesByItem", "QueryExaminationSitesByItems", "CreateExaminationSite"])
  ];

  /// <summary>
  /// 校验绑定布尔参数的查询语句声明了布尔参数映射与类型处理器，并被该语句引用。
  /// 原因：框架默认按整数绑定布尔参数，PostgreSQL 的 boolean 列无法与 integer 比较；
  /// 未声明处理器时，带启用状态筛选的标准项目查询会直接失败，类型处理器把绑定交给当前 Provider 适配层。
  /// </summary>
  [Fact]
  public void Boolean_query_parameter_declares_type_handler_and_is_referenced_by_statement()
  {
    var document = XDocument.Load(FindRepositoryFile("MedicalRecognitionReportQuery.xml"));
    var ns = (XNamespace)"http://dysoft.vip/schemas/EarthraceSqlMap.xsd";

    var statement = document.Descendants(ns + "Statement")
      .Single(element => (string?)element.Attribute("Id") == "QueryMedicalStandardItemList");
    Assert.Contains("$IsValid", statement.Value, StringComparison.Ordinal);

    // 阶段 2 的互认配置列表查询同样以 `$IsValid` 过滤启用列（停用筛选下发 false 时也必须渲染该谓词），
    // 缺少这条断言时，该语句的筛选谓词被删改不会让任何用例失败。
    var configurationStatement = document.Descendants(ns + "Statement")
      .Single(element => (string?)element.Attribute("Id") == "QueryRecognitionProjectConfigurationList");
    Assert.True(ConfigurationQueryFiltersByEnabledState(configurationStatement.Value));

    // 变异证据：把副本里的 `$IsValid` 谓词换成恒真条件后，同一条判据必须判为"没有按启用状态过滤"，
    // 证明上面的断言不是对任何文本都成立。
    string predicateRemoved = configurationStatement.Value.Replace("m.is_valid = $IsValid", "1 = 1", StringComparison.Ordinal);
    Assert.NotEqual(configurationStatement.Value, predicateRemoved);
    Assert.False(ConfigurationQueryFiltersByEnabledState(predicateRemoved));

    Assert.Equal("MedicalRecognitionBooleanParameters", (string?)configurationStatement.Attribute("ParameterMap"));

    var parameterMap = document.Descendants(ns + "ParameterMap")
      .SingleOrDefault(element => (string?)element.Attribute("Id") == "MedicalRecognitionBooleanParameters");
    Assert.NotNull(parameterMap);
    var isValidParameter = parameterMap!.Elements(ns + "Parameter")
      .Single(element => (string?)element.Attribute("Property") == "IsValid");
    Assert.Equal("MedicalRecognitionBoolean", (string?)isValidParameter.Attribute("TypeHandler"));
    Assert.Equal("MedicalRecognitionBooleanParameters", (string?)statement.Attribute("ParameterMap"));

    var moduleSource = File.ReadAllText(FindRepositoryFile("MedicalRecognitionRepositoryModule.cs"));
    Assert.Contains("TypeHandlerFactory.Register(\"MedicalRecognitionBoolean\", new BooleanTypeHandler())", moduleSource, StringComparison.Ordinal);
  }

  /// <summary>
  /// 校验互认配置列表查询语句冻结了组织范围谓词、稳定排序键与投影别名绑定：
  /// 组织谓词决定只返回该组织的配置，排序键决定结果顺序稳定，投影列与只读投影
  /// <see cref="RecognitionProjectConfigurationListItem"/> 的属性必须一一对应，否则字段会静默漏绑或错绑。
  /// </summary>
  [Fact]
  public void Recognition_configuration_query_freezes_organization_sort_and_projection_binding()
  {
    var document = XDocument.Load(FindRepositoryFile("MedicalRecognitionReportQuery.xml"));
    var ns = (XNamespace)"http://dysoft.vip/schemas/EarthraceSqlMap.xsd";
    var statement = document.Descendants(ns + "Statement")
      .Single(element => (string?)element.Attribute("Id") == "QueryRecognitionProjectConfigurationList");
    string sql = NormalizeSqlText(statement.Value);

    // 组织范围谓词：缺失或改成"不过滤"会让一个组织的调用方读到其他组织的配置。
    Assert.True(RecognitionConfigurationQueryFiltersByOrganization(sql));
    string organizationPredicateRemoved = statement.Value.Replace(
      "and m.organization_code = $OrganizationCode", "and 1 = 1", StringComparison.Ordinal);
    Assert.NotEqual(statement.Value, organizationPredicateRemoved);
    Assert.False(RecognitionConfigurationQueryFiltersByOrganization(NormalizeSqlText(organizationPredicateRemoved)));
    // 稳定排序键：缺失时同一组织的配置顺序随数据库执行计划变化，页面顺序会抖动。
    Assert.Contains("order by m.standard_project_code asc, m.id asc", sql, StringComparison.Ordinal);

    // 投影列逐项冻结：列表达式必须出现在 select 列表中，且其别名与投影属性名按 snake_case 一一对应。
    string[] expectedAliases = [.. RecognitionConfigurationProjection
      .Select(projection => ToSnakeCase(projection.PropertyName))
      .Order(StringComparer.Ordinal)];
    string[] projectedAliases = [.. typeof(RecognitionProjectConfigurationListItem)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Select(property => ToSnakeCase(property.Name))
      .Order(StringComparer.Ordinal)];
    Assert.Equal(projectedAliases, expectedAliases);

    foreach ((string selectExpression, string propertyName) in RecognitionConfigurationProjection)
    {
      Assert.Contains(NormalizeSqlText(selectExpression), sql, StringComparison.Ordinal);
      // 别名与属性名必须逐项对应：有别名取 as 之后的别名，没有别名取列名（列名本身就是 snake_case 形式）。
      string expectedAlias = selectExpression.Contains(" as ", StringComparison.Ordinal)
        ? selectExpression[(selectExpression.IndexOf(" as ", StringComparison.Ordinal) + " as ".Length)..]
        : selectExpression[(selectExpression.IndexOf('.') + 1)..];
      Assert.Equal(expectedAlias, ToSnakeCase(propertyName));
    }
  }

  /// <summary>
  /// 互认配置列表查询的投影列与只读投影属性的逐项对应：select 列表里的列表达式，以及它绑定的属性名。
  /// </summary>
  private static readonly (string SelectExpression, string PropertyName)[] RecognitionConfigurationProjection =
  [
    ("m.id as configuration_id", nameof(RecognitionProjectConfigurationListItem.ConfigurationId)),
    ("m.standard_project_code", nameof(RecognitionProjectConfigurationListItem.StandardProjectCode)),
    ("i.name as standard_item_name", nameof(RecognitionProjectConfigurationListItem.StandardItemName)),
    ("c.item_type", nameof(RecognitionProjectConfigurationListItem.ItemType)),
    ("c.name as category_name", nameof(RecognitionProjectConfigurationListItem.CategoryName)),
    ("g.name as group_name", nameof(RecognitionProjectConfigurationListItem.GroupName)),
    ("m.recognition_duration_days", nameof(RecognitionProjectConfigurationListItem.RecognitionDurationDays)),
    ("m.is_valid", nameof(RecognitionProjectConfigurationListItem.IsValid)),
    ("c.is_valid as category_is_valid", nameof(RecognitionProjectConfigurationListItem.CategoryIsValid)),
    ("g.is_valid as group_is_valid", nameof(RecognitionProjectConfigurationListItem.GroupIsValid)),
    ("i.is_valid as item_is_valid", nameof(RecognitionProjectConfigurationListItem.ItemIsValid))
  ];

  /// <summary>
  /// 判断互认配置列表查询语句是否按可信组织范围过滤，即是否包含组织范围谓词。
  /// </summary>
  /// <param name="statement">查询语句文本。</param>
  /// <returns>语句包含组织范围谓词时为 <see langword="true"/>。</returns>
  private static bool RecognitionConfigurationQueryFiltersByOrganization(string statement) =>
    statement.Contains("m.organization_code = $OrganizationCode", StringComparison.Ordinal);

  /// <summary>
  /// 判断互认配置列表查询语句是否按启用状态过滤，即是否引用布尔参数 <c>$IsValid</c> 比较配置启用列。
  /// </summary>
  /// <param name="statement">查询语句文本。</param>
  /// <returns>语句包含启用状态谓词时为 <see langword="true"/>。</returns>
  private static bool ConfigurationQueryFiltersByEnabledState(string statement) =>
    statement.Contains("m.is_valid = $IsValid", StringComparison.Ordinal);

  /// <summary>
  /// 校验互认项目配置建表脚本的物理形态：列序、列类型口径、没有后续变更语句、唯一索引名称与覆盖列，以及注释覆盖。
  /// </summary>
  /// <remarks>
  /// 这些内容与仓储语句、领域字段名一起构成物理映射，改名或漏写时既不会编译失败，也不会让 SqlMap 断言失败。
  /// </remarks>
  [Fact]
  public void Mutual_recognition_item_ddl_freezes_columns_index_and_comments()
  {
    string script = File.ReadAllText(FindRepositoryFile("mutual_recognition_item.sql"));
    string[] expectedColumns =
    [
      "id", "organization_code", "standard_item_id", "standard_project_code",
      "recognition_duration_days", "is_valid", "oper_time", "oper_id"
    ];

    // 列序与列数：投影与写入语句按列位置绑定，插入列或调换顺序都会让真实运行与静态断言不一致。
    string tableBody = script[script.IndexOf("create table mrec_mutual_recognition_item (", StringComparison.Ordinal)..];
    tableBody = tableBody[..tableBody.IndexOf(");", StringComparison.Ordinal)];
    string[] columns = [.. tableBody.Split('\n')
      .Skip(1)
      .Select(line => line.Trim())
      .Where(line => line.Length > 0 && !line.StartsWith("primary key", StringComparison.OrdinalIgnoreCase))
      .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])];
    Assert.Equal(expectedColumns, columns);

    // 列类型口径：文本列不使用带长度上限的 varchar，避免编码长度被物理截断。
    Assert.DoesNotContain("varchar", script, StringComparison.OrdinalIgnoreCase);
    // 建表脚本只做一次建表：出现 alter 说明表结构被后续脚本改写，仓库里只有这一处物理定义。
    Assert.DoesNotContain("alter", script, StringComparison.OrdinalIgnoreCase);

    // 唯一索引：名称与覆盖列决定同一组织与标准项目只能有一条配置，是重复配置拒绝的物理依据。
    Assert.Contains("create unique index ux_mrec_mutual_recognition_org_project", script, StringComparison.Ordinal);
    Assert.Contains("on mrec_mutual_recognition_item (organization_code, standard_project_code)", script, StringComparison.Ordinal);

    // 注释覆盖：表、8 个列与唯一索引都必须有中文说明，注释条数与目标集合一起冻结。
    string[] commentLines = [.. script.Split('\n')
      .Select(line => line.Trim())
      .Where(line => line.StartsWith("comment on ", StringComparison.Ordinal))];
    Assert.Equal(10, commentLines.Length);
    Assert.Single(commentLines, line => line.StartsWith("comment on table mrec_mutual_recognition_item ", StringComparison.Ordinal));
    Assert.Single(commentLines, line => line.StartsWith("comment on index ux_mrec_mutual_recognition_org_project ", StringComparison.Ordinal));
    Assert.Equal(8, commentLines.Count(line => line.StartsWith("comment on column mrec_mutual_recognition_item.", StringComparison.Ordinal)));
    foreach (string column in expectedColumns)
    {
      Assert.Contains(
        commentLines,
        line => line.StartsWith($"comment on column mrec_mutual_recognition_item.{column} is ", StringComparison.Ordinal));
    }
  }

  /// <summary>
  /// 把 SQL 文本中的换行与连续空白归一为单个空格，使冻结判据只对语句内容敏感、不对映射文件排版敏感。
  /// </summary>
  /// <param name="text">SQL 文本或语句片段。</param>
  /// <returns>空白归一后的 SQL 文本。</returns>
  private static string NormalizeSqlText(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

  /// <summary>
  /// 把投影属性名转换为查询别名使用的 snake_case 形式，用于逐项对应。
  /// </summary>
  /// <param name="propertyName">PascalCase 属性名。</param>
  /// <returns>snake_case 形式，例如 <c>ConfigurationId</c> 对应 <c>configuration_id</c>。</returns>
  private static string ToSnakeCase(string propertyName)
  {
    StringBuilder builder = new();
    for (int index = 0; index < propertyName.Length; index++)
    {
      char current = propertyName[index];
      if (char.IsUpper(current) && index > 0) builder.Append('_');
      builder.Append(char.ToLowerInvariant(current));
    }

    return builder.ToString();
  }

  /// <summary>
  /// 在当前测试进程内注册共享 SQL 引用的布尔参数类型处理器，复现宿主装配阶段的同一前置条件。
  /// </summary>
  /// <param name="earthraceAssembly">已加载的 <c>Dy.Earthrace</c> 程序集，提供类型处理器工厂与布尔处理器。</param>
  /// <exception cref="InvalidOperationException">工厂上不存在两个参数的公开静态 <c>Register</c> 方法时，由 <c>Single</c> 抛出。</exception>
  private static void RegisterBooleanTypeHandler(Assembly earthraceAssembly)
  {
    var factoryType = earthraceAssembly.GetType("Dy.Earthrace.TypeHandlers.TypeHandlerFactory", throwOnError: true)!;
    var handlerType = earthraceAssembly.GetType("Dy.Earthrace.TypeHandlers.BooleanTypeHandler", throwOnError: true)!;
    var handler = Activator.CreateInstance(handlerType)!;
    var register = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method => method.Name == "Register" && method.GetParameters().Length == 2)
      .Single(method => method.GetParameters()[0].ParameterType == typeof(string)
        && method.GetParameters()[1].ParameterType.IsInstanceOfType(handler));
    register.Invoke(null, new[] { (object)"MedicalRecognitionBoolean", handler });
  }

  /// <summary>
  /// 从已构建的 SqlMap 配置对象中枚举全部语句，取出各自注册的完整语句标识。
  /// </summary>
  /// <param name="sqlConfig">宿主程序集构建出的 SqlMap 配置对象，其 <c>SqlMaps</c> 属性按作用域存放语句集合。</param>
  /// <returns>去重排序后的完整语句标识列表；没有任何语句注册时返回空数组，由调用方断言非空。</returns>
  private static IReadOnlyList<string> GetRegisteredKeys(object sqlConfig)
  {
    var sqlMaps = (System.Collections.IDictionary)sqlConfig.GetType().GetProperty("SqlMaps")!.GetValue(sqlConfig)!;
    var keys = new List<string>();
    foreach (System.Collections.DictionaryEntry entry in sqlMaps)
    {
      var sqlMap = entry.Value!;
      var statements = (System.Collections.IDictionary)sqlMap.GetType().GetProperty("Statements")!.GetValue(sqlMap)!;
      foreach (System.Collections.DictionaryEntry statement in statements)
      {
        var fullSqlId = (string?)statement.Value!.GetType().GetProperty("FullSqlId")!.GetValue(statement.Value);
        if (!string.IsNullOrWhiteSpace(fullSqlId)) keys.Add(fullSqlId);
      }
    }

    return keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
  }

  /// <summary>
  /// 按方法名与参数个数反射调用目标对象的实例方法，用于驱动宿主构建 SqlMap 注册表。
  /// </summary>
  /// <param name="target">被调用的宿主构建器实例。</param>
  /// <param name="methodName">目标方法名，按名称与实参个数唯一定位。</param>
  /// <param name="arguments">按顺序传给目标方法的实参。</param>
  /// <returns>目标方法的返回值；目标方法返回 void 时为 <see langword="null"/>。</returns>
  /// <exception cref="InvalidOperationException">目标类型上不存在名称与参数个数同时匹配的公开实例方法时，由 <c>Single</c> 抛出。</exception>
  private static object? Invoke(object target, string methodName, params object[] arguments) =>
    target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
      .Invoke(target, arguments);

  /// <summary>
  /// 把宿主配置 JSON 递归展开为以冒号分隔的扁平键值集合，供内存配置源按层级读取。
  /// </summary>
  /// <param name="element">待展开的 JSON 元素，可为对象、数组或叶子值。</param>
  /// <param name="prefix">当前元素在配置中的键前缀；根节点传空串。</param>
  /// <param name="values">接收展开结果的目标集合，叶子值按原样写入，同一键重复出现时以最后一次为准。</param>
  private static void Flatten(JsonElement element, string prefix, IDictionary<string, string?> values)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (var property in element.EnumerateObject())
        Flatten(property.Value, string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}", values);
      return;
    }

    if (element.ValueKind == JsonValueKind.Array)
    {
      var index = 0;
      foreach (var item in element.EnumerateArray()) Flatten(item, $"{prefix}:{index++}", values);
      return;
    }

    values[prefix] = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
  }

  /// <summary>
  /// 定位已构建的宿主输出目录，该目录同时提供运行时注册所需的程序集与宿主配置。
  /// </summary>
  /// <returns>包含 <c>Dy.Earthrace.dll</c> 的宿主输出目录绝对路径。</returns>
  /// <exception cref="DirectoryNotFoundException">从测试程序集所在目录逐级向上都未找到包含 <c>Dy.Earthrace.dll</c> 的宿主输出目录时抛出；此时需先构建宿主工程。</exception>
  private static string FindHostOutputDirectory()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var candidate = Path.Combine(directory.FullName, "Dy.MedicalRecognition", "bin", "Debug", "net10.0");
      if (File.Exists(Path.Combine(candidate, "Dy.Earthrace.dll"))) return candidate;
      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the built host output containing Dy.Earthrace.dll.");
  }

  /// <summary>
  /// 在仓储工程内按文件名定位任意 SqlMap 文件，供查询侧断言读取语句与参数映射。
  /// </summary>
  /// <param name="fileName">SqlMap 文件名，例如 <c>MedicalRecognitionReportQuery.xml</c>。</param>
  /// <returns>仓储工程中该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">从测试程序集所在目录逐级向上都未找到该 SqlMap 文件时抛出；此时检查仓储工程的 SqlMap 是否已随源码保留在原位。</exception>
  private static string FindRepositoryFile(string fileName)
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var repositoryDirectory = Path.Combine(directory.FullName, "Dy.MedicalRecognition.Repository");
      if (Directory.Exists(repositoryDirectory))
      {
        var matches = Directory.GetFiles(repositoryDirectory, fileName, SearchOption.AllDirectories);
        if (matches.Length == 1) return matches[0];
      }

      directory = directory.Parent;
    }

    throw new FileNotFoundException($"Could not locate repository SqlMap '{fileName}'.");
  }

  /// <summary>
  /// 定位指定名称的仓储 SqlMap 文件，供断言直接读取语句文本。
  /// </summary>
  /// <param name="fileName">SqlMap 文件名，例如 <c>MedicalStandardCategory.xml</c>。</param>
  /// <returns>仓储工程中该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">从测试程序集所在目录逐级向上都未找到该 SqlMap 文件时抛出；此时检查仓储工程的 SqlMap 是否已随源码保留在原位。</exception>
  private static string FindRepositorySqlMap(string fileName)
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var repositoryDirectory = Path.Combine(directory.FullName, "Dy.MedicalRecognition.Repository");
      if (Directory.Exists(repositoryDirectory))
      {
        var matches = Directory.GetFiles(repositoryDirectory, fileName, SearchOption.AllDirectories);
        if (matches.Length == 1) return matches[0];
      }

      directory = directory.Parent;
    }

    throw new FileNotFoundException($"Could not locate repository SqlMap '{fileName}'.");
  }

  /// <summary>
  /// 按文件路径把指定程序集加载进测试进程的默认加载上下文，供反射读取 SqlMap 注册结果。
  /// </summary>
  /// <param name="directory">程序集所在目录。</param>
  /// <param name="fileName">程序集文件名，例如 <c>Dy.Earthrace.dll</c>。</param>
  /// <returns>已加载的程序集。</returns>
  private static Assembly LoadAssembly(string directory, string fileName)
  {
    var path = Path.Combine(directory, fileName);
    return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
  }
}
