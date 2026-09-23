using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 7 排除项回归的静态守卫：SRS 排除项与设计约束中的能力级不建设内容未被实现。
/// </summary>
/// <remarks>
/// 对应阶段 7 票「排除项回归」的 V1-V5 静态证据面与阶段 7 设计「排除项核对清单」章
/// （docs/plans/009-阶段7-横向治理与发布收口/design.md）：EX-02/04/07/11/16/17/20/22/24/25/27、
/// DC-01（V1 端点与路由清单）、EX-23、DC-11（V2 HIS 面开放范围）、EX-03/06/08/10/13/15、DC-04/06/07/09
/// （V3 关键字扫描）、EX-05/12/14/19、DC-10（V4 契约字段与表形态）、EX-18、DC-08（V5 导出契约）。
/// 全部为静态断言（反射、语法节点、冻结的 OpenAPI 快照、建表脚本与前端路由清单），不依赖数据库与宿主；
/// 宿主行为证据（V6）由独立条目取证。测试为回归守卫性质，写完即应通过；
/// 任何断言失败说明排除项被实现，按阶段 7 设计的分流口径处理。
/// </remarks>
public sealed class Stage7ExclusionGuardTests
{
  /// <summary>
  /// 排除类能力在方法、类型与成员声明名上的标识符子串集合。
  /// </summary>
  /// <remarks>
  /// 覆盖调阅状态与反馈（EX-02）、历史报告导入（EX-04/DC-01）、逐份上传与逐份审批（EX-07）、
  /// 敏感分级与专项授权（EX-11）、采集与轮询队列（EX-08）、推送（EX-06/DC-06）、
  /// 计费结算退费（EX-13）、审计日志（EX-15/DC-09）、趋势排名绩效（EX-17/DC-09）、
  /// 更正撤销删除冲销恢复重启用（EX-20/EX-23）、对照同步（EX-22）、危急值闭环（EX-12）、
  /// 自动推断与相似度（EX-10/DC-04）、上传状态与过程状态等过程字段（EX-05）。
  /// 每个子串都以当前全部生产源码声明名零命中为前提（同名无关词已逐个甄别并排除在集合之外：
  /// CancellationToken 中的 Cancel、Collection 与 SpecimenCollectedTime 中的 Collect、
  /// savedTimeRecord 中的 timer 子序列），命中任意子串即说明排除能力出现在声明面。
  /// </remarks>
  private static readonly string[] ExcludedSymbolSubstrings =
  [
    "Retrieval", "Approv", "Import", "Correct", "Revoke", "Reverse", "Restore", "Reactivate",
    "Trend", "Rank", "Performance", "Notification", "Acknowledge", "Receipt", "Escalation",
    "Audit", "Billing", "Settle", "Refund", "Similarity", "Fuzzy", "Infer",
    "Dicom", "Pacs", "Push", "Poll", "Queue", "Hangfire", "BackgroundService", "IHostedService",
    "UploadStatus", "ProcessStatus", "VerifyFlag", "BatchNo"
  ];

  /// <summary>
  /// 同步类能力的声明形态：Sync 后紧跟大写字母（SyncXxx 的 Pascal 命名）。
  /// </summary>
  /// <remarks>
  /// 同步能力单独用形态判定：子串 Sync 在忽略大小写下会命中全部 XxxAsync 异步方法的
  /// Async 后缀（a-Sync 形态），因此集合扫描排除 Sync，改由本模式判定同步能力声明；
  /// Async 后缀中 Sync 前是小写 a、后无大写字母，不会命中。
  /// </remarks>
  private static readonly Regex SyncCapabilityPattern = new("Sync(?=[A-Z])", RegexOptions.Compiled);

  /// <summary>排除类能力在 OpenAPI 路径与前端路由文本上的子串集合，判定不区分大小写。</summary>
  private static readonly string[] ExcludedRouteSubstrings =
  [
    "trend", "rank", "approv", "import", "retrieval", "sync", "push",
    "download-record", "collection", "non-adoption-reason", "version-management",
    "parameter", "match-export", "performance",
    "nonadoptionreason", "clinicalcontent", "downloadrecord"
  ];

  /// <summary>
  /// 排除类管理操作在公开操作名上的专项子串集合。
  /// </summary>
  /// <remarks>
  /// 这些能力的驼峰操作名（如 CreateNonAdoptionReason、DeleteProcessingResult、CollectReportTask、
  /// UpdateClinicalContent、DownloadRecord 查询）不会在通用子串的连字符形态中命中，且不能进入全局符号
  /// 子串集合——NonAdoptionReason 是处理结果契约的保留侧字段、Delete/Collect 是仓储层合法成员
  /// 形态（DeleteAsync）与同名无关词（Collection）——因此只在公开操作名这一层面单独判定。
  /// </remarks>
  private static readonly string[] ExcludedOperationSubstrings =
  [
    "Delete", "NonAdoptionReason", "ClinicalContent", "Collect", "Dict", "DownloadRecord"
  ];

  /// <summary>排除类过程字段在契约上的精确属性名集合（EX-05）。</summary>
  private static readonly string[] ExcludedContractPropertyNames =
  [
    "Priority", "ProcessStatus", "UploadStatus", "VerifyFlag", "CheckFlag",
    "EventId", "EventNo", "BatchNo", "TransBatchNo", "QuerySerialNo"
  ];

  /// <summary>
  /// V1：写侧、读侧应用服务与手工控制器的全部公开方法名不声明任何排除类能力。
  /// </summary>
  /// <remarks>
  /// 判定面是公开入口的方法名集合：应用服务的公开方法即框架自动端点的操作来源，
  /// 两个手工控制器的动作即手工端点。排除类操作（调阅反馈、导入、审批、更正撤销、
  /// 趋势排名、对照同步、字典管理等）无论位于哪一层都会出现在该集合中。
  /// 方法数量有下界，集合为空时先在数量断言失败，避免空集合让负向断言恒真。
  /// </remarks>
  [Fact]
  public void V1_public_operations_declare_no_excluded_capability()
  {
    string[] operationNames =
    [
      .. typeof(Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Select(method => method.Name),
      .. typeof(MedicalRecognitionReportQueryAppService)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Select(method => method.Name),
      .. typeof(Controllers.RecognitionStatisticsExportController)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Select(method => method.Name),
      .. typeof(Controllers.ReportPdfFileController)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Select(method => method.Name)
    ];

    Assert.True(
      operationNames.Length >= 50,
      $"公开操作数量应有下界以证明扫描面有效，实际 {operationNames.Length} 个。");

    Assert.Empty(FindExcludedSymbols(operationNames));

    foreach (string name in operationNames)
    {
      foreach (string substring in ExcludedOperationSubstrings)
        Assert.False(
          name.Contains(substring, StringComparison.OrdinalIgnoreCase),
          $"公开操作 {name} 命中排除类管理操作标识符 {substring}。");
    }
  }

  /// <summary>
  /// V1：冻结的 OpenAPI 快照路径清单不声明任何排除类操作路由。
  /// </summary>
  /// <remarks>
  /// 路径全量等值已由 <c>Stage2EnumContractTests.Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes</c>
  /// 冻结为阶段 6 交付后的 54 条快照；本用例在该清单上按排除子串再扫一遍，
  /// 让「排除项被实现并新增路由」这一回归即使在新清单断言被同步放行时也会被本用例拦下。
  /// 路径数量同时复断言 54，防止快照被整体替换后扫描面悄悄缩小。
  /// </remarks>
  [Fact]
  public void V1_frozen_openapi_paths_declare_no_excluded_operation()
  {
    JsonNode document = JsonNode.Parse(File.ReadAllText(OpenApiSnapshotPath()))!;
    IReadOnlyList<string> paths = [.. document["paths"]!.AsObject().Select(property => property.Key)];

    Assert.Equal(54, paths.Count);
    foreach (string path in paths)
    {
      foreach (string substring in ExcludedRouteSubstrings)
        Assert.False(
          path.Contains(substring, StringComparison.OrdinalIgnoreCase),
          $"OpenAPI 路径 {path} 命中排除类能力子串 {substring}。");
    }
  }

  /// <summary>
  /// V1：管理端前端路由清单与交付页面集合精确等值，且不出现排除类页面路由。
  /// </summary>
  /// <remarks>
  /// 子应用无本地菜单定义（菜单由宿主按授权注入），路由清单即页面全集：
  /// 阶段 1 两界面节合并为同一工作台后共 10 个管理端页面路由，加上仅开发环境挂载的原型路由。
  /// 采集任务管理（EX-16）、调阅状态（EX-02）、逐份审批（EX-07）、版本管理（EX-20/EX-23）、
  /// 下载记录查询（EX-24）、不采纳原因字典管理（EX-27）、参数配置（EX-19/DC-10）、
  /// 趋势排名绩效（EX-17/DC-09）、推送记录（EX-06）等页面路由出现即失败。
  /// </remarks>
  [Fact]
  public void V1_management_router_declares_exactly_the_delivered_pages()
  {
    string routerSource = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(),
      "client", "apps", "dy-medical-recognition", "src", "router", "routes.tsx"));

    string[] declaredPaths =
    [
      .. Regex.Matches(routerSource, @"path:\s*'([^']+)'")
        .Select(match => match.Groups[1].Value)
    ];
    string[] expectedPaths =
    [
      "/", "standard-catalog", "recognition-projects", "recognition-amounts", "branch-recognition-amounts",
      "report-management", "branch-report-management", "recognition-usage-statistics",
      "branch-recognition-usage-statistics", "source-recognition-statistics",
      "branch-source-recognition-statistics", "prototype/standard-catalog"
    ];
    Assert.Equal(
      expectedPaths.Order(StringComparer.Ordinal).ToArray(),
      declaredPaths.Order(StringComparer.Ordinal).ToArray());

    foreach (string declaredPath in declaredPaths)
    {
      foreach (string substring in ExcludedRouteSubstrings)
        Assert.False(
          declaredPath.Contains(substring, StringComparison.OrdinalIgnoreCase),
          $"管理端路由 {declaredPath} 命中排除类页面子串 {substring}。");
    }
  }

  /// <summary>
  /// V2：医院接入面路由与 SRS 医院接入接口九项能力精确对应；历史版本查询、列表与详情入口不存在，
  /// 按版本标识定位的 PDF 下载属九项能力之一（语义为阶段 4 已确认决策 S4-D28，
  /// 见 docs/plans/006-阶段4-报告采集与生命周期/design.md 决策台账）。
  /// </summary>
  /// <remarks>
  /// 九项能力（总体计划设计 §4.4「医院接入接口」行与阶段 4、阶段 5 设计）在宿主上承载为 11 条路由：
  /// 两类完整报告提交各有 multipart 手工路由与既有自动端点两条（阶段 4 S4-D6/S4-D29 的双形态），
  /// 两类作废、匹配查询、处理结果提交、引用详情、引用结果各一条自动端点，PDF 文件下载一条手工路由。
  /// 历史版本列表与详情声明在只读查询应用服务上，挂管理端查询组 `/Api/MedicalRecognitionReportQuery/`；
  /// 历史 PDF 入口 OpenReportVersionPdf 声明在写侧应用服务上，经框架自动端点暴露在
  /// `/Api/MedicalRecognitionReport/OpenReportVersionPdf`（与管理端历史版本查看页面共用同一宿主认证体系，
  /// 属管理端历史版本查看能力的既有承载路由，医院接入九项能力清单不含它——
  /// CONTEXT.md 报告历史版本段：医院 HIS 在线业务接口只提供当前有效版本，不对外开放历史版本查询或下载）。
  /// </remarks>
  [Fact]
  public void V2_hospital_interface_routes_freeze_nine_capabilities_without_history_versions()
  {
    JsonNode document = JsonNode.Parse(File.ReadAllText(OpenApiSnapshotPath()))!;
    IReadOnlyList<string> paths = [.. document["paths"]!.AsObject().Select(property => property.Key)];

    string[] hospitalInterfaceRoutes =
    [
      "/api/v1/report-pdf/laboratory-report",
      "/api/v1/report-pdf/examination-report",
      "/api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf",
      "/Api/MedicalRecognitionReport/SubmitCompleteLaboratoryReport",
      "/Api/MedicalRecognitionReport/SubmitCompleteExaminationReport",
      "/Api/MedicalRecognitionReport/VoidLaboratoryReport",
      "/Api/MedicalRecognitionReport/VoidExaminationReport",
      "/Api/MedicalRecognitionReport/RequestRecognitionMatches",
      "/Api/MedicalRecognitionReport/SubmitRecognitionProcessingResults",
      "/Api/MedicalRecognitionReport/SubmitRecognitionReferences",
      "/Api/MedicalRecognitionReportQuery/QueryRecognitionCitationDetail"
    ];

    foreach (string route in hospitalInterfaceRoutes)
      Assert.Contains(route, paths);

    Assert.DoesNotContain("/Api/MedicalRecognitionReport/QueryMedicalReportVersionList", paths);
    Assert.DoesNotContain("/Api/MedicalRecognitionReport/QueryMedicalReportVersionDetail", paths);
  }

  /// <summary>
  /// V2：管理端历史版本的三个入口都是只读查询入口，不声明写事务。
  /// </summary>
  /// <remarks>
  /// 历史版本只能在管理端查看与下载（DC-11），不可修改、删除、恢复、重新启用：
  /// 列表与详情两个入口声明在只读查询应用服务上，版本 PDF 入口也不声明事务。
  /// 对照断言取写侧引用结果提交入口（阶段 5 设计声明的显式事务入口），
  /// 它必须带 WorkUnit 声明，证明「查 WorkUnit 为空」的判定对写入口会失败、判定本身有效。
  /// </remarks>
  [Fact]
  public void V2_management_history_version_entries_stay_read_only()
  {
    MethodInfo versionList = typeof(MedicalRecognitionReportQueryAppService).GetMethod("QueryMedicalReportVersionListAsync")!;
    MethodInfo versionDetail = typeof(MedicalRecognitionReportQueryAppService).GetMethod("QueryMedicalReportVersionDetailAsync")!;
    MethodInfo versionPdf = typeof(Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService).GetMethod("OpenReportVersionPdfAsync")!;

    Assert.Null(FindAttribute(versionList, "WorkUnitAttribute"));
    Assert.Null(FindAttribute(versionDetail, "WorkUnitAttribute"));
    Assert.Null(FindAttribute(versionPdf, "WorkUnitAttribute"));

    MethodInfo writeCounterpart = typeof(Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService)
      .GetMethod("SubmitRecognitionReferencesAsync")!;
    Assert.NotNull(FindAttribute(writeCounterpart, "WorkUnitAttribute"));
  }

  /// <summary>
  /// V3：全部生产源码的类型、方法、属性与字段声明名不出现排除类能力标识符。
  /// </summary>
  /// <remarks>
  /// 判定按语法节点收集声明名（注释与字符串字面量不产生节点），覆盖 Host、Application、
  /// Application.Contracts、Domain、Domain.Share 与 Repository 六个生产工程的全部 C# 文件。
  /// 声明名数量有下界，扫描面为空时先在数量断言失败。判别力由合成源码变异证据保证：
  /// 同一判定对注入排除标识符的源码必须命中。
  /// </remarks>
  [Fact]
  public void V3_production_symbols_declare_no_excluded_capability()
  {
    List<string> declarationNames = [];
    int sourceCount = 0;
    foreach ((string _, CompilationUnitSyntax root) in ReadProductionSources())
    {
      sourceCount++;
      declarationNames.AddRange(root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(declaration => declaration.Identifier.ValueText));
      declarationNames.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(method => method.Identifier.ValueText));
      declarationNames.AddRange(root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(property => property.Identifier.ValueText));
      declarationNames.AddRange(root.DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(field => field.Declaration.Variables)
        .Select(variable => variable.Identifier.ValueText));
    }

    Assert.True(sourceCount >= 60, $"生产源码文件数量应有下界以证明扫描面有效，实际 {sourceCount} 个。");
    Assert.True(
      declarationNames.Count >= 500,
      $"生产声明名数量应有下界以证明扫描面有效，实际 {declarationNames.Count} 个。");

    List<string> violations = [.. FindExcludedSymbols(declarationNames)];
    Assert.Empty(violations);

    // 变异证据：注入排除标识符的合成声明必须被同一判定命中，证明负向断言不是恒真；
    // SyncCatalogRecords 验证同步能力形态判定，AsyncXxx 形态不命中验证判定边界。
    Assert.NotEmpty(FindExcludedSymbols([
      "ReportRetrievalStatus", "ImportHistoryReports", "TrendStatistics", "PushToDownstream", "SyncCatalogRecords"
    ]));
    Assert.Empty(FindExcludedSymbols(["SaveReportAsync", "QueryListAsync"]));
  }

  /// <summary>
  /// V3：生产源码不构造远程地址字符串，影像调阅地址只原样透传、从不拼接。
  /// </summary>
  /// <remarks>
  /// DC-07：平台只存来源影像状态与单个 HTTPS 调阅地址，不自行拼接影像地址。
  /// 判定按语法节点执行：无以 http 起始的字符串字面量、无 Uri 与 UriBuilder 构造节点；
  /// <c>ImageAccessUrl</c> 标识符不得出现在插值字符串、字符串加法或 Combine 调用实参中
  /// （本地 PDF 文件存储的 Path.Combine 路径组装不受影响，其操作对象是文件键而非地址）。
  /// 判别力由合成源码变异证据保证。
  /// </remarks>
  [Fact]
  public void V3_production_sources_build_no_remote_address_and_never_compose_image_url()
  {
    List<string> violations = [];
    foreach ((string relativePath, CompilationUnitSyntax root) in ReadProductionSources())
      violations.AddRange(FindAddressCompositionViolations(relativePath, root));

    Assert.Empty(violations);

    // 变异证据：注入地址字面量、Uri 构造与影像地址拼接的合成源码必须被同一判定命中；
    // 影像地址以属性访问形态（fact.ImageAccessUrl）与标识符形态两种写法分别注入。
    IReadOnlyList<string> synthesized = FindAddressCompositionViolations(
      "probe/AddressCompositionProbe.cs",
      SourceSyntaxGuard.Parse(
        """
        class Probe
        {
          string Build(string host, string imageAccessUrl, Probe fact)
          {
            string remote = "https://pacs.example/viewer";
            var uri = new System.Uri(remote);
            return $"{host}/viewer?image=" + fact.ImageAccessUrl + imageAccessUrl;
          }
        }
        """));
    Assert.Contains("probe/AddressCompositionProbe.cs: 出现以 http 起始的字符串字面量", synthesized);
    Assert.Contains("probe/AddressCompositionProbe.cs: 出现 Uri 构造节点", synthesized);
    Assert.Contains("probe/AddressCompositionProbe.cs: 影像调阅地址被拼接（插值、加法或 Combine 实参）", synthesized);
  }

  /// <summary>
  /// V4：两个契约程序集的全部公共实例属性名不声明任何排除类过程字段。
  /// </summary>
  /// <remarks>
  /// EX-05：查询业务流水号、调用方事件标识、拟开项目业务标识、通用传输批次与
  /// 来源医嘱优先级、院内报告过程状态、上传状态、校验标志等过程字段均无采集。
  /// 判定面是 Application.Contracts 与 Domain.Share 两个程序集的全部公共实例属性
  /// （请求、DTO 与读模型的承载面），属性数量有下界。
  /// 「本次来源就诊流水号」「来源医嘱流水号」属 SRS 报告结构与匹配条件的既有业务字段，不在排除集合；
  /// 框架领域事件基类的 EventId 等继承成员属事件基础设施通用字段，只取各类型自身声明即可排除。
  /// </remarks>
  [Fact]
  public void V4_contracts_declare_no_process_field()
  {
    Assembly[] contractAssemblies =
    [
      typeof(LaboratoryResultItemDto).Assembly,
      typeof(Domain.Share.MedicalRecognitionReportAggregate.Requests.MedicalReportVersionRequest).Assembly
    ];

    List<string> propertyNames = [];
    foreach (Type type in contractAssemblies.SelectMany(assembly => assembly.GetTypes()))
    {
      // 只取各类型自身声明的属性：框架基类（如领域事件基类 DomainEvent）的 EventId 等继承成员
      // 属基础设施通用字段，承载面是框架事件总线，与医院上报契约无关。
      propertyNames.AddRange(
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
          .Select(property => property.Name));
    }

    Assert.True(
      propertyNames.Count >= 300,
      $"契约属性数量应有下界以证明扫描面有效，实际 {propertyNames.Count} 个。");

    foreach (string name in propertyNames)
    {
      foreach (string excluded in ExcludedContractPropertyNames)
        Assert.False(
          name.Equals(excluded, StringComparison.Ordinal),
          $"契约属性 {name} 属排除类过程字段。");
    }
  }

  /// <summary>
  /// V4：危急值只保留来源标志形态，无通知、确认、回执或升级闭环成员。
  /// </summary>
  /// <remarks>
  /// EX-12 的保留侧是「只保存来源标志」：领域实体与对外 DTO 上以 Critical 起始的成员
  /// 恰好是危急值标志一项，闭环类成员（通知、确认、回执、升级）出现即失败。
  /// </remarks>
  [Fact]
  public void V4_critical_value_stays_a_single_source_flag()
  {
    foreach (Type type in new[] { typeof(LaboratoryResultItem), typeof(LaboratoryResultItemDto) })
    {
      string[] criticalMembers =
      [
        .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Select(property => property.Name)
          .Where(name => name.StartsWith("Critical", StringComparison.Ordinal))
      ];
      Assert.Equal(["CriticalValueFlag"], criticalMembers);
    }
  }

  /// <summary>
  /// V4：互认项目金额表只有当前金额单值形态，无版本、历史或恢复列。
  /// </summary>
  /// <remarks>
  /// EX-14：金额不设独立版本、变更历史与重算。判定面是建表脚本的列集合精确等值：
  /// 除业务键与操作审计列外只有当前金额一列，任何版本化列都会破坏等值。
  /// </remarks>
  [Fact]
  public void V4_amount_table_keeps_single_current_amount_columns()
  {
    string script = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(),
      "server", "Dy.MedicalRecognition.Repository", "Scripts", "mrec_organization_hospital_branch_recognition_amount.sql"));

    string[] columns = [.. ParseCreateTableColumns(script, "mrec_organization_hospital_branch_recognition_amount")];
    Assert.Equal(
      ["id", "organization_code", "hospital_code", "branch_code", "standard_project_code", "current_amount", "oper_time", "oper_id"],
      columns);
  }

  /// <summary>
  /// V4：建表脚本清单与已交付表精确等值，无参数表、采集表、危急值表、推送记录表等排除类表。
  /// </summary>
  /// <remarks>
  /// DC-10（无专用参数表）、EX-16 与 EX-08（无采集表）、EX-12（无危急值闭环表）、
  /// EX-06（无推送记录表）、EX-15 与 DC-09（无审计日志表）、EX-24（无下载记录表）、
  /// EX-22（无对照同步表）都以「清单里没有对应表」为静态证据：任何新表加入该目录都会破坏等值断言。
  /// </remarks>
  [Fact]
  public void V4_ddl_scripts_freeze_delivered_tables_only()
  {
    string scriptsDirectory = Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(),
      "server", "Dy.MedicalRecognition.Repository", "Scripts");
    string[] scripts = [.. Directory.GetFiles(scriptsDirectory, "*.sql").Select(file => Path.GetFileName(file))!];

    Assert.Equal(
      [
        "mrec_examination_item.sql", "mrec_examination_report_content.sql", "mrec_examination_site.sql",
        "mrec_laboratory_antimicrobial_susceptibility.sql", "mrec_laboratory_bacteria_result.sql",
        "mrec_laboratory_report_content.sql", "mrec_laboratory_result_item.sql",
        "mrec_medical_recognition_report.sql", "mrec_medical_report_version.sql",
        "mrec_medical_standard_category.sql", "mrec_medical_standard_group.sql", "mrec_medical_standard_item.sql",
        "mrec_organization_hospital_branch_recognition_amount.sql", "mrec_platform_patient.sql",
        "mrec_recognition_match_item.sql", "mrec_recognition_match_record.sql",
        "mrec_recognition_processing_result.sql", "mrec_recognition_reference.sql", "mutual_recognition_item.sql"
      ],
      scripts.Order(StringComparer.Ordinal).ToArray());
  }

  /// <summary>
  /// V4：三项系统参数只经既有系统参数服务的平台级按编码读取，不出现组织级覆盖或其他参数面。
  /// </summary>
  /// <remarks>
  /// DC-10：引用详情有效时长、本院报告匹配开关与本院报告排除时长复用既有系统参数服务，
  /// 不建专用参数表、配置页面与组织级覆盖。判定面是外部参数服务接口成员的调用集合：
  /// 写侧与读侧应用服务全部声明分片中恰好只有平台级按编码读取一项；组织级覆盖成员
  /// （GetOrg/QueryOrg 前缀）同属该接口，一旦出现即被集合等值拦下。
  /// 应用服务内部的私有参数读取包装不是服务成员，不参与集合。
  /// 判别力由合成源码变异证据保证。
  /// </remarks>
  [Fact]
  public void V4_system_parameters_go_through_existing_parameter_service_only()
  {
    List<(string RelativePath, CompilationUnitSyntax Root)> entryDeclarations =
    [
      .. SourceSyntaxGuard.ReadTypeDeclarations(
        "server/Dy.MedicalRecognition.Application", nameof(Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService)),
      .. SourceSyntaxGuard.ReadTypeDeclarations(
        "server/Dy.MedicalRecognition.Application", nameof(MedicalRecognitionReportQueryAppService))
    ];

    Assert.True(
      entryDeclarations.Count >= 4,
      $"两个参数读取入口的声明分片应有下界以证明扫描面有效，实际 {entryDeclarations.Count} 个分片。");

    // 判定面是外部参数服务接口成员的调用名集合：应用服务内部的私有包装方法
    // （如 ReadSystemParameterValueAsync）不是服务成员，其内部实现仍走按编码读取。
    string[] parameterServiceMembers = [..
      typeof(Dy.Base.Application.Contracts.SystemParameterAggregate.ISystemParameterAppService)
        .GetMethods()
        .Select(method => method.Name)];

    SortedSet<string> parameterMemberCalls = new(StringComparer.Ordinal);
    foreach ((string _, CompilationUnitSyntax root) in entryDeclarations)
    {
      parameterMemberCalls.UnionWith(FindParameterServiceCalls(root, parameterServiceMembers));
    }

    Assert.Equal(["GetSystemParameterByCodeAsync"], parameterMemberCalls.ToArray());

    // 变异证据：组织级覆盖读取必须被同一集合等值判定拦下。
    Assert.Contains(
      "GetOrgSystemParameterValueByCodeAsync",
      FindParameterServiceCalls(
        SourceSyntaxGuard.Parse(
          """
          class Probe
          {
            void Read(Dy.Base.Application.Contracts.SystemParameterAggregate.ISystemParameterAppService service)
            {
              service.GetOrgSystemParameterValueByCodeAsync(null);
            }
          }
          """),
        parameterServiceMembers));
  }

  /// <summary>
  /// V5：导出契约只提供 Excel .xlsx 单一格式，导出请求继承查询条件且无格式选择字段。
  /// </summary>
  /// <remarks>
  /// EX-18 与 DC-08：CSV/PDF 等其他导出格式不存在，导出不解除脱敏或扩大数据权限。
  /// 请求属性集合的继承与无分页已由
  /// <c>Stage6StatisticsContractTests.Export_requests_carry_type_dimension_and_filters_without_paging</c> 冻结，
  /// 两个导出端点与响应组装已由 <c>Stage6EndpointTests</c> 冻结；本用例补齐格式面：
  /// 两个导出请求无格式选择属性、控制器内容类型常量为 xlsx 的官方内容类型、
  /// 冻结快照中导出端点恰两条且导出类型枚举恰七个取值、快照全文无 csv 字样。
  /// </remarks>
  [Fact]
  public void V5_export_contract_offers_single_xlsx_format_inheriting_query_filters()
  {
    foreach (Type requestType in new[] { typeof(RecognitionStatisticsExportRequest), typeof(BranchRecognitionStatisticsExportRequest) })
    {
      string[] propertyNames = [.. requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name)];
      Assert.NotEmpty(propertyNames);
      foreach (string formatProperty in new[] { "Format", "FileType", "ContentType", "ExportFormat" })
        Assert.False(propertyNames.Contains(formatProperty), $"{requestType.Name} 不得声明格式选择属性 {formatProperty}。");
    }

    FieldInfo? contentType = typeof(Controllers.RecognitionStatisticsExportController).GetField(
      "ExcelContentType", BindingFlags.NonPublic | BindingFlags.Static);
    Assert.NotNull(contentType);
    Assert.True(contentType!.IsLiteral, "ExcelContentType 应为编译期常量。");
    Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentType.GetRawConstantValue());

    JsonNode document = JsonNode.Parse(File.ReadAllText(OpenApiSnapshotPath()))!;
    IReadOnlyList<string> paths = [.. document["paths"]!.AsObject().Select(property => property.Key)];
    Assert.Equal(
      ["/api/v1/statistics-export/branch", "/api/v1/statistics-export/platform"],
      [.. paths.Where(path => path.StartsWith("/api/v1/statistics-export/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)]);

    JsonNode exportType = document["components"]!["schemas"]![nameof(RecognitionStatisticsExportType)]!;
    Assert.Equal(7, exportType["enum"]!.AsArray().Count);

    Assert.DoesNotContain("csv", File.ReadAllText(OpenApiSnapshotPath()), StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// 在声明名集合中查找排除类能力标识符，返回命中描述。
  /// </summary>
  /// <param name="declarationNames">待检查的声明名集合。</param>
  /// <returns>命中描述集合；无命中时为空。</returns>
  private static IReadOnlyList<string> FindExcludedSymbols(IReadOnlyList<string> declarationNames) =>
    [.. declarationNames
      .Select(name => (Name: name, Hits: ExcludedSymbolHits(name).ToArray()))
      .Where(entry => entry.Hits.Length > 0)
      .SelectMany(entry => entry.Hits.Select(hit => $"{entry.Name} 命中排除类能力标识符 {hit}"))];

  /// <summary>
  /// 找出单个声明名命中的排除类能力标识符，子串集合加同步能力形态判定。
  /// </summary>
  /// <param name="name">待检查的声明名。</param>
  /// <returns>命中的标识符描述集合。</returns>
  private static IEnumerable<string> ExcludedSymbolHits(string name)
  {
    foreach (string substring in ExcludedSymbolSubstrings)
    {
      if (name.Contains(substring, StringComparison.OrdinalIgnoreCase))
        yield return substring;
    }

    if (SyncCapabilityPattern.IsMatch(name))
      yield return "Sync（同步能力声明形态）";
  }

  /// <summary>
  /// 在单份源码中查找远程地址构造与影像地址拼接违规。
  /// </summary>
  /// <param name="relativePath">用于报告的仓库内相对路径。</param>
  /// <param name="root">待检查的编译单元语法树根节点；可以是真实文件或合成源码。</param>
  /// <returns>违规描述集合；无违规时为空。</returns>
  private static IReadOnlyList<string> FindAddressCompositionViolations(string relativePath, CompilationUnitSyntax root)
  {
    List<string> violations = [];

    foreach (LiteralExpressionSyntax literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
    {
      if (literal.IsKind(SyntaxKind.StringLiteralExpression) &&
          literal.Token.ValueText.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        violations.Add($"{relativePath}: 出现以 http 起始的字符串字面量");
    }

    foreach (ObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
    {
      if (SourceSyntaxGuard.SimpleTypeName(creation.Type) is "Uri" or "UriBuilder")
        violations.Add($"{relativePath}: 出现 Uri 构造节点");
    }

    foreach (SyntaxNode identifier in root.DescendantNodes()
      .Where(node => node is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "ImageAccessUrl" ||
        node is MemberBindingExpressionSyntax memberBinding && memberBinding.Name.Identifier.ValueText == "ImageAccessUrl"))
    {
      bool composed = identifier.Ancestors().Any(ancestor =>
        ancestor is InterpolatedStringExpressionSyntax ||
        (ancestor is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression)) ||
        (ancestor is InvocationExpressionSyntax invocation && SourceSyntaxGuard.MemberName(invocation.Expression) == "Combine"));
      if (composed)
        violations.Add($"{relativePath}: 影像调阅地址被拼接（插值、加法或 Combine 实参）");
    }

    return violations;
  }

  /// <summary>
  /// 收集单份源码中参数服务接口成员的调用名，供集合等值判定与变异证据使用。
  /// </summary>
  /// <param name="root">待检查的编译单元语法树根节点。</param>
  /// <param name="parameterServiceMembers">参数服务接口的成员名集合。</param>
  /// <returns>调用过的参数服务接口成员名集合。</returns>
  private static IReadOnlyList<string> FindParameterServiceCalls(CompilationUnitSyntax root, string[] parameterServiceMembers) =>
    [.. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
      .Select(invocation => SourceSyntaxGuard.MemberName(invocation.Expression))
      .Where(memberName => memberName is not null && parameterServiceMembers.Contains(memberName, StringComparer.Ordinal))
      .Select(memberName => memberName!)];

  /// <summary>读取冻结的 OpenAPI 快照文件路径。</summary>
  /// <returns>快照文件绝对路径。</returns>
  private static string OpenApiSnapshotPath() => Path.Combine(
    SourceSyntaxGuard.FindRepositoryRoot(),
    "client", "packages", "api-client-medical-recognition", "openapi", "medical-recognition.openapi.json");

  /// <summary>收集六个生产工程全部 C# 源码的语法树，排除 bin 与 obj。</summary>
  /// <returns>仓库内相对路径与语法树根节点的集合，按相对路径排序。</returns>
  private static IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> ReadProductionSources()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    string[] productionDirectories =
    [
      "server/Dy.MedicalRecognition",
      "server/Dy.MedicalRecognition.Application",
      "server/Dy.MedicalRecognition.Application.Contracts",
      "server/Dy.MedicalRecognition.Domain",
      "server/Dy.MedicalRecognition.Domain.Share",
      "server/Dy.MedicalRecognition.Repository"
    ];

    List<(string RelativePath, CompilationUnitSyntax Root)> sources = [];
    foreach (string directory in productionDirectories)
    {
      foreach (string file in Directory.EnumerateFiles(
        Path.Combine(repositoryRoot, directory.Replace('/', Path.DirectorySeparatorChar)),
        "*.cs",
        SearchOption.AllDirectories))
      {
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
          continue;

        sources.Add((Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'), SourceSyntaxGuard.Parse(File.ReadAllText(file))));
      }
    }

    return [.. sources.OrderBy(source => source.RelativePath, StringComparer.Ordinal)];
  }

  /// <summary>
  /// 解析建表脚本中指定表的列名序列，按声明顺序。
  /// </summary>
  /// <param name="script">建表脚本原文。</param>
  /// <param name="tableName">表名。</param>
  /// <returns>列名集合；主键约束行不参与。</returns>
  private static IReadOnlyList<string> ParseCreateTableColumns(string script, string tableName)
  {
    int statementStart = script.IndexOf($"create table {tableName} (", StringComparison.Ordinal);
    Assert.True(statementStart >= 0, $"建表脚本缺少 {tableName} 的建表语句。");
    int openParen = script.IndexOf('(', statementStart);
    int closeParen = script.IndexOf(");", openParen, StringComparison.Ordinal);
    Assert.True(closeParen > openParen, $"建表脚本 {tableName} 的列定义块不完整。");

    return [.. script[(openParen + 1)..closeParen]
      .Split('\n')
      .Select(line => line.Trim())
      .Where(line => line.Length > 0 && !line.StartsWith("primary key", StringComparison.Ordinal))
      .Select(line => line.Split(' ')[0])];
  }

  /// <summary>
  /// 按特性类型简单名查找方法上声明的特性实例。
  /// </summary>
  /// <param name="method">待检查的方法。</param>
  /// <param name="attributeTypeName">特性类型的简单名。</param>
  /// <returns>首个命中的特性实例；未声明时为 <see langword="null"/>。</returns>
  private static Attribute? FindAttribute(MethodInfo method, string attributeTypeName) =>
    method.GetCustomAttributes().FirstOrDefault(attribute => attribute.GetType().Name == attributeTypeName);
}
