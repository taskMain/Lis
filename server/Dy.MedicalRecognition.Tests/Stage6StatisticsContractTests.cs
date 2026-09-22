using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 6 互认统计与导出的契约与静态面：查询契约分片方法集合与签名、十一个请求类型的双入口字段集合、
/// 十三个统计读模型的强类型重建、三个统计枚举的取值与描述器声明，以及分页禁用字段守卫。
/// </summary>
/// <remarks>
/// 对应后端设计验证矩阵（docs/plans/008-阶段6-互认统计与导出/Server/design.md「验证矩阵」章）V1、V2、V3、V5、V6
/// 与票 00（docs/plans/008-阶段6-互认统计与导出/阶段6-Tickets/00-契约准备与枚举登记.md）的交付范围。
/// 全部为源码契约判定（Roslyn 语法节点，工具复用 <see cref="SourceSyntaxGuard"/>）：注释与字符串里的同名写法
/// 不产生节点，声明被删除、字段被改名、类型退化为 <c>object</c> 都会直接失败；不启动宿主、不连库。
/// </remarks>
public sealed class Stage6StatisticsContractTests
{
  /// <summary>统计查询契约分片的仓库内相对路径。</summary>
  private const string StatisticsContractPath =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Statistics.cs";

  /// <summary>统计请求类型的目录（相对仓库根，使用 <c>/</c> 分隔）。</summary>
  private const string StatisticsRequestDirectory =
    "server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionStatistics";

  /// <summary>统计请求类型的命名空间。</summary>
  private const string StatisticsRequestNamespace = "Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics";

  /// <summary>统计读模型目录（相对仓库根，使用 <c>/</c> 分隔）。</summary>
  private const string ReadModelDirectory = "server/Dy.MedicalRecognition.Application.Contracts/ReadModels";

  /// <summary>
  /// 统计查询契约分片声明的十个方法及其冻结签名：八个查询（四能力 × 平台/本院）、一个导出入口与一个匹配记录视图。
  /// </summary>
  private static readonly (string MethodName, string ReturnType, string Parameter)[] StatisticsContractMethods =
  [
    ("QueryRecognitionUsageSummaryAsync", "Task<PageResultDto<RecognitionUsageSummaryReadModel>>", "RecognitionUsageSummaryQueryRequest request"),
    ("QueryRecognitionUsageDetailsAsync", "Task<PageResultDto<RecognitionUsageDetailReadModel>>", "RecognitionUsageDetailsQueryRequest request"),
    ("QuerySourceRecognitionSummaryAsync", "Task<PageResultDto<SourceRecognitionSummaryReadModel>>", "SourceRecognitionSummaryQueryRequest request"),
    ("QuerySourceRecognitionDetailsAsync", "Task<PageResultDto<SourceRecognitionDetailReadModel>>", "SourceRecognitionDetailsQueryRequest request"),
    ("QueryBranchRecognitionUsageSummaryAsync", "Task<PageResultDto<RecognitionUsageSummaryReadModel>>", "BranchRecognitionUsageSummaryQueryRequest request"),
    ("QueryBranchRecognitionUsageDetailsAsync", "Task<PageResultDto<RecognitionUsageDetailReadModel>>", "BranchRecognitionUsageDetailsQueryRequest request"),
    ("QueryBranchSourceRecognitionSummaryAsync", "Task<PageResultDto<SourceRecognitionSummaryReadModel>>", "BranchSourceRecognitionSummaryQueryRequest request"),
    ("QueryBranchSourceRecognitionDetailsAsync", "Task<PageResultDto<SourceRecognitionDetailReadModel>>", "BranchSourceRecognitionDetailsQueryRequest request"),
    ("GetStatisticsExportAsync", "Task<StatisticsExportFileReadModel>", "RecognitionStatisticsExportRequest request"),
    ("QueryRecognitionMatchRecordAsync", "Task<RecognitionMatchRecordReadModel>", "RecognitionMatchRecordQueryRequest request")
  ];

  /// <summary>统计读模型与其顶层属性、嵌套子模型：名称与属性类型逐项冻结（类型文本按语法节点重建）。</summary>
  private static readonly (string TypeName, (string Name, string TypeName)[] Properties)[] ReadModels =
  [
    ("RecognitionUsageSummaryReadModel",
    [
      ("ReceiverOrganizationCode", "string?"), ("ReceiverOrganizationName", "string?"),
      ("ReceiverHospitalCode", "string?"), ("ReceiverHospitalName", "string?"),
      ("ReceiverBranchCode", "string?"), ("ReceiverBranchName", "string?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDeptName", "string?"),
      ("ItemType", "MedicalItemType?"), ("ItemTypeText", "string?"),
      ("StandardProjectCode", "string?"), ("StandardProjectName", "string?"),
      ("CategoryName", "string?"), ("GroupName", "string?"),
      ("PeriodStart", "DateTime"), ("PeriodEnd", "DateTime"),
      ("AdoptionCount", "int"), ("NonAdoptionCount", "int"), ("ReferenceCount", "int"), ("ReminderCount", "int"),
      ("SamePeriodRecognitionRate", "decimal"), ("SamePeriodRecognitionRateCalculated", "bool"),
      ("NonAdoptionReasons", "IReadOnlyList<NonAdoptionReasonSummaryReadModel>"),
      ("EstimatedSavingAmount", "decimal"), ("GroupDimension", "RecognitionStatisticsGroupDimension")
    ]),
    ("RecognitionUsageDetailReadModel",
    [
      ("RecognitionMatchRecordId", "Guid"), ("RecognitionMatchItemId", "Guid"), ("MatchCreatedTime", "DateTime"),
      ("VisitType", "VisitType"), ("VisitTypeText", "string"), ("VisitSerialNo", "string"),
      ("Source", "SourceOrganizationReadModel"), ("Receiver", "ReceiverOrganizationReadModel"),
      ("Item", "RecognitionStatisticsItemReadModel"), ("BusinessTime", "DateTime"), ("IsUnprocessed", "bool"),
      ("PatientName", "string"), ("IdentityDocumentNo", "string"),
      ("RecognitionDeptId", "string?"), ("RecognitionDeptName", "string?"),
      ("RecognitionDoctorId", "string?"), ("RecognitionDoctorName", "string?"),
      ("ProcessingResult", "RecognitionProcessingResultDetailReadModel?"), ("Reference", "RecognitionReferenceDetailReadModel?")
    ]),
    ("SourceRecognitionSummaryReadModel",
    [
      ("SourceOrganizationCode", "string?"), ("SourceOrganizationName", "string?"),
      ("SourceHospitalCode", "string?"), ("SourceHospitalName", "string?"),
      ("SourceBranchCode", "string?"), ("SourceBranchName", "string?"),
      ("ItemType", "MedicalItemType?"), ("ItemTypeText", "string?"),
      ("StandardProjectCode", "string?"), ("StandardProjectName", "string?"),
      ("CategoryName", "string?"), ("GroupName", "string?"),
      ("PeriodStart", "DateTime"), ("PeriodEnd", "DateTime"),
      ("RecognitionCount", "int"), ("GroupDimension", "RecognitionStatisticsGroupDimension")
    ]),
    ("SourceRecognitionDetailReadModel",
    [
      ("RecognitionMatchRecordId", "Guid"), ("RecognitionMatchItemId", "Guid"),
      ("SourceOrganizationCode", "string"), ("SourceOrganizationName", "string"),
      ("SourceHospitalCode", "string"), ("SourceHospitalName", "string"),
      ("SourceBranchCode", "string"), ("SourceBranchName", "string"),
      ("ReceiverOrganizationCode", "string"), ("ReceiverOrganizationName", "string"),
      ("ReceiverHospitalCode", "string"), ("ReceiverHospitalName", "string"),
      ("ReceiverBranchCode", "string"), ("ReceiverBranchName", "string"),
      ("StandardProjectCode", "string"),
      ("RecognitionDeptId", "string?"), ("RecognitionDeptName", "string?"),
      ("RecognitionDoctorId", "string?"), ("RecognitionDoctorName", "string?"),
      ("RecognitionTime", "DateTime"), ("PatientName", "string"), ("IdentityDocumentNo", "string")
    ]),
    ("StatisticsExportFileReadModel",
    [
      ("FileName", "string"), ("FileFormat", "string"), ("FileStream", "byte[]")
    ]),
    ("RecognitionMatchRecordReadModel",
    [
      ("RecognitionMatchRecordId", "Guid"), ("MatchCreatedTime", "DateTime"),
      ("Receiver", "ReceiverOrganizationReadModel"),
      ("PatientName", "string"), ("IdentityDocumentNo", "string"),
      ("VisitType", "VisitType"), ("VisitTypeText", "string"), ("VisitSerialNo", "string"),
      ("IsProcessed", "bool"), ("RecognitionTime", "DateTime?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDeptName", "string?"),
      ("RecognitionDoctorId", "string?"), ("RecognitionDoctorName", "string?"),
      ("MatchItems", "IReadOnlyList<RecognitionMatchRecordItemReadModel>")
    ]),
    ("RecognitionMatchRecordItemReadModel",
    [
      ("RecognitionMatchItemId", "Guid"), ("Item", "RecognitionStatisticsItemReadModel"), ("Source", "SourceOrganizationReadModel"),
      ("ReportId", "Guid"), ("ReportVersionId", "Guid"), ("IsProcessed", "bool"),
      ("Decision", "RecognitionResult?"), ("DecisionText", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("NonAdoptionReasonName", "string?")
    ]),
    ("SourceOrganizationReadModel",
    [
      ("OrganizationCode", "string"), ("OrganizationName", "string"),
      ("HospitalCode", "string"), ("HospitalName", "string"),
      ("BranchCode", "string"), ("BranchName", "string")
    ]),
    ("ReceiverOrganizationReadModel",
    [
      ("OrganizationCode", "string"), ("OrganizationName", "string"),
      ("HospitalCode", "string"), ("HospitalName", "string"),
      ("BranchCode", "string"), ("BranchName", "string")
    ]),
    ("RecognitionStatisticsItemReadModel",
    [
      ("ItemType", "MedicalItemType"), ("ItemTypeText", "string?"),
      ("CategoryName", "string"), ("GroupName", "string"),
      ("StandardProjectCode", "string"), ("StandardProjectName", "string")
    ]),
    ("RecognitionProcessingResultDetailReadModel",
    [
      ("IsProcessed", "bool"), ("RecognitionTime", "DateTime?"),
      ("Decision", "RecognitionResult?"), ("DecisionText", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("NonAdoptionReasonName", "string?"),
      ("NonAdoptionSupplementDescription", "string?"), ("EstimatedSavingAmount", "decimal")
    ]),
    ("RecognitionReferenceDetailReadModel",
    [
      ("IsReferenced", "bool"), ("ReferenceTime", "DateTime?"),
      ("ReferenceDeptId", "string?"), ("ReferenceDeptName", "string?"),
      ("ReferenceDoctorId", "string?"), ("ReferenceDoctorName", "string?")
    ]),
    ("NonAdoptionReasonSummaryReadModel",
    [
      ("ReasonCode", "string"), ("ReasonName", "string"), ("Count", "int"), ("Ratio", "decimal")
    ])
  ];

  /// <summary>三个统计枚举的取值、成员名与中文说明（与 UML 5 枚举块一致，顺序即声明顺序）。</summary>
  private static readonly (string EnumName, (string Member, int Value, string Description)[] Members)[] StatisticsEnums =
  [
    ("RecognitionStatisticsGroupDimension",
    [
      ("Hospital", 1, "医院"), ("Branch", 2, "院区"), ("RecognitionDepartment", 3, "互认科室"), ("StandardItem", 4, "标准项目")
    ]),
    ("RecognitionUsageDetailType",
    [
      ("Reminder", 1, "提醒"), ("Adopted", 2, "采纳"), ("NotAdopted", 3, "不采纳"), ("Referenced", 4, "引用")
    ]),
    ("RecognitionStatisticsExportType",
    [
      ("RecognitionUsageSummary", 1, "接收侧互认使用汇总"), ("RecognitionReminderDetails", 2, "接收侧提醒明细"),
      ("RecognitionAdoptionDetails", 3, "接收侧采纳明细"), ("RecognitionNonAdoptionDetails", 4, "接收侧不采纳明细"),
      ("RecognitionReferenceDetails", 5, "接收侧引用明细"), ("SourceRecognitionSummary", 6, "来源医院被互认汇总"),
      ("SourceRecognitionDetails", 7, "来源医院被互认明细")
    ])
  ];

  /// <summary>
  /// V1：统计契约分片恰好声明八个查询、一个导出入口与一个匹配记录视图共十个方法，
  /// 且每个方法的返回类型与参数冻结；接口的分部声明文件恰为既有三个分片加统计分片共四个。
  /// </summary>
  [Fact]
  public void Statistics_contract_partial_declares_exactly_ten_methods_with_frozen_signatures()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations = SourceSyntaxGuard.ReadTypeDeclarations(
      "server/Dy.MedicalRecognition.Application.Contracts/Queries", nameof(IMedicalRecognitionReportQueryAppService));
    Assert.Equal(
      [
        "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Citation.cs",
        "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Reports.cs",
        StatisticsContractPath,
        "server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs"
      ],
      declarations.Select(declaration => declaration.RelativePath).Order(StringComparer.Ordinal).ToArray());

    CompilationUnitSyntax statisticsRoot = SourceSyntaxGuard.Read(StatisticsContractPath.Split('/'));
    MethodDeclarationSyntax[] methods = [.. statisticsRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()];

    string[] actualNames = [.. methods.Select(method => method.Identifier.ValueText).Order(StringComparer.Ordinal)];
    string[] expectedNames = [.. StatisticsContractMethods.Select(entry => entry.MethodName).Order(StringComparer.Ordinal)];
    Assert.Equal(expectedNames, actualNames);

    foreach ((string methodName, string returnType, string parameter) in StatisticsContractMethods)
    {
      MethodDeclarationSyntax method = Assert.Single(methods, candidate => candidate.Identifier.ValueText == methodName);
      Assert.Null(method.Body);
      Assert.Null(method.ExpressionBody);
      Assert.Equal(returnType, NormalizeType(method.ReturnType.ToString()));
      Assert.Equal([parameter], method.ParameterList.Parameters.Select(DescribeParameter).ToArray());
    }
  }

  /// <summary>
  /// V1：十一个统计请求类型各自恰好一个声明文件，全部落在统计请求目录并使用统计请求命名空间。
  /// </summary>
  [Fact]
  public void Statistics_request_types_declared_in_the_recognition_statistics_directory()
  {
    string[] requestTypes =
    [
      nameof(RecognitionUsageSummaryQueryRequest), nameof(BranchRecognitionUsageSummaryQueryRequest),
      nameof(RecognitionUsageDetailsQueryRequest), nameof(BranchRecognitionUsageDetailsQueryRequest),
      nameof(SourceRecognitionSummaryQueryRequest), nameof(BranchSourceRecognitionSummaryQueryRequest),
      nameof(SourceRecognitionDetailsQueryRequest), nameof(BranchSourceRecognitionDetailsQueryRequest),
      nameof(RecognitionStatisticsExportRequest), nameof(BranchRecognitionStatisticsExportRequest),
      nameof(RecognitionMatchRecordQueryRequest)
    ];

    foreach (string typeName in requestTypes)
    {
      IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations = SourceSyntaxGuard.ReadTypeDeclarations(
        "server/Dy.MedicalRecognition.Application.Contracts/Queries", typeName);

      string expectedPath = $"{StatisticsRequestDirectory}/{typeName}.cs";
      Assert.True(
        declarations.Count == 1 && declarations[0].RelativePath == expectedPath,
        $"请求类型 {typeName} 应恰好声明在 {expectedPath}，实际：{string.Join("、", declarations.Select(declaration => declaration.RelativePath))}。");

      string? namespaceText = declarations[0].Root.DescendantNodes()
        .OfType<BaseNamespaceDeclarationSyntax>()
        .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
        .SingleOrDefault();
      Assert.Equal(StatisticsRequestNamespace, namespaceText);
    }
  }

  /// <summary>V1：平台版接收侧两个请求承载接收组与来源组两组组织/医院/院区、汇总或明细专属筛选与内嵌分页对象。</summary>
  [Fact]
  public void Platform_receive_side_requests_carry_both_scope_groups()
  {
    AssertRequestProperties(nameof(RecognitionUsageSummaryQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("OrganizationCode", "string?"), ("HospitalCode", "string?"), ("BranchCode", "string?"),
      ("SourceOrganizationCode", "string?"), ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"),
      ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("GroupDimension", "RecognitionStatisticsGroupDimension"), ("Page", "PageRequestDto")
    ]);

    AssertRequestProperties(nameof(RecognitionUsageDetailsQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("OrganizationCode", "string?"), ("HospitalCode", "string?"), ("BranchCode", "string?"),
      ("SourceOrganizationCode", "string?"), ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDoctorId", "string?"), ("ItemType", "MedicalItemType?"),
      ("CategoryName", "string?"), ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("DetailType", "RecognitionUsageDetailType"), ("Page", "PageRequestDto")
    ]);
  }

  /// <summary>
  /// V1：本院版接收侧两个请求不含本侧组织与医院字段（由可信上下文注入），本侧院区可选，
  /// 携带来源组医院/院区可选筛选。
  /// </summary>
  [Fact]
  public void Branch_receive_side_requests_omit_local_organization_and_hospital()
  {
    AssertRequestProperties(nameof(BranchRecognitionUsageSummaryQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"), ("BranchCode", "string?"),
      ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"),
      ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("GroupDimension", "RecognitionStatisticsGroupDimension"), ("Page", "PageRequestDto")
    ]);

    AssertRequestProperties(nameof(BranchRecognitionUsageDetailsQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"), ("BranchCode", "string?"),
      ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDoctorId", "string?"), ("ItemType", "MedicalItemType?"),
      ("CategoryName", "string?"), ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("DetailType", "RecognitionUsageDetailType"), ("Page", "PageRequestDto")
    ]);
  }

  /// <summary>V1：平台版来源侧两个请求承载来源组与接收组两组组织/医院/院区与标准项目筛选。</summary>
  [Fact]
  public void Platform_source_side_requests_carry_both_scope_groups()
  {
    AssertRequestProperties(nameof(SourceRecognitionSummaryQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("SourceOrganizationCode", "string?"), ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("ReceiverOrganizationCode", "string?"), ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"), ("GroupName", "string?"),
      ("StandardProjectCode", "string?"), ("GroupDimension", "RecognitionStatisticsGroupDimension"), ("Page", "PageRequestDto")
    ]);

    AssertRequestProperties(nameof(SourceRecognitionDetailsQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("SourceOrganizationCode", "string?"), ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("ReceiverOrganizationCode", "string?"), ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"), ("GroupName", "string?"),
      ("StandardProjectCode", "string?"), ("Page", "PageRequestDto")
    ]);
  }

  /// <summary>
  /// V1：本院版来源侧两个请求不含来源组织与来源医院字段（固定为可信上下文），本侧来源院区可选，
  /// 携带接收组医院/院区可选筛选。
  /// </summary>
  [Fact]
  public void Branch_source_side_requests_omit_local_organization_and_hospital()
  {
    AssertRequestProperties(nameof(BranchSourceRecognitionSummaryQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"), ("SourceBranchCode", "string?"),
      ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"), ("GroupName", "string?"),
      ("StandardProjectCode", "string?"), ("GroupDimension", "RecognitionStatisticsGroupDimension"), ("Page", "PageRequestDto")
    ]);

    AssertRequestProperties(nameof(BranchSourceRecognitionDetailsQueryRequest),
    [
      ("StartTime", "DateOnly"), ("EndTime", "DateOnly"), ("SourceBranchCode", "string?"),
      ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("ItemType", "MedicalItemType?"), ("CategoryName", "string?"), ("GroupName", "string?"),
      ("StandardProjectCode", "string?"), ("Page", "PageRequestDto")
    ]);
  }

  /// <summary>
  /// V1：两个导出请求含导出类型、必填汇总维度与可选不采纳原因代码，承载统计查询条件，且不含分页对象；
  /// 本院版导出请求不含任何组织与医院字段，仅保留本侧院区与来源/接收两组院区可选筛选。
  /// </summary>
  [Fact]
  public void Export_requests_carry_type_dimension_and_filters_without_paging()
  {
    AssertRequestProperties(nameof(RecognitionStatisticsExportRequest),
    [
      ("ExportType", "RecognitionStatisticsExportType"), ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("ReceiverOrganizationCode", "string?"), ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("SourceOrganizationCode", "string?"), ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDoctorId", "string?"), ("ItemType", "MedicalItemType?"),
      ("CategoryName", "string?"), ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("GroupDimension", "RecognitionStatisticsGroupDimension")
    ]);

    AssertRequestProperties(nameof(BranchRecognitionStatisticsExportRequest),
    [
      ("ExportType", "RecognitionStatisticsExportType"), ("StartTime", "DateOnly"), ("EndTime", "DateOnly"),
      ("BranchCode", "string?"),
      ("SourceHospitalCode", "string?"), ("SourceBranchCode", "string?"),
      ("ReceiverHospitalCode", "string?"), ("ReceiverBranchCode", "string?"),
      ("RecognitionDeptId", "string?"), ("RecognitionDoctorId", "string?"), ("ItemType", "MedicalItemType?"),
      ("CategoryName", "string?"), ("GroupName", "string?"), ("StandardProjectCode", "string?"),
      ("NonAdoptionReasonCode", "string?"), ("GroupDimension", "RecognitionStatisticsGroupDimension")
    ]);
  }

  /// <summary>V1：匹配记录集合视图请求只承载互认匹配记录标识，无任何范围或筛选字段。</summary>
  [Fact]
  public void Match_record_request_carries_only_the_record_identifier()
  {
    AssertRequestProperties(nameof(RecognitionMatchRecordQueryRequest),
      [("RecognitionMatchRecordId", "Guid")]);
  }

  /// <summary>
  /// V5：十三个统计读模型各自恰好一个声明文件，属性集合与类型逐项冻结；
  /// 全部读模型不得出现 <c>object</c> 属性类型与 <c>MaskedPatientInfo</c> 字段，
  /// 也不得出现分页禁用字段与请求分页字段。
  /// </summary>
  [Fact]
  public void Statistics_read_models_declare_strongly_typed_properties()
  {
    string[] bannedPropertyNames = ["HasNext", "Pagination", "SkipCount", "PageCount", "MaskedPatientInfo"];
    foreach ((string typeName, (string Name, string TypeName)[] expectedProperties) in ReadModels)
    {
      CompilationUnitSyntax root = ReadSingleReadModel(typeName);
      List<PropertyDeclarationSyntax> properties = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>()];

      Assert.Equal(
        expectedProperties.Select(property => property.Name).ToArray(),
        properties.Select(property => property.Identifier.ValueText).ToArray());

      foreach (PropertyDeclarationSyntax property in properties)
      {
        foreach (string banned in bannedPropertyNames)
          Assert.NotEqual(banned, property.Identifier.ValueText);
        Assert.NotEqual("object", DescribeType(property.Type));
        Assert.NotEqual("PageIndex", property.Identifier.ValueText);
        Assert.NotEqual("PageSize", property.Identifier.ValueText);
      }

      foreach ((string propertyName, string propertyType) in expectedProperties)
      {
        PropertyDeclarationSyntax property = Assert.Single(properties, candidate => candidate.Identifier.ValueText == propertyName);
        Assert.True(
          DescribeType(property.Type) == propertyType,
          $"{typeName}.{propertyName} 的属性类型应为 {propertyType}，实际为 {DescribeType(property.Type)}。");
      }
    }
  }

  /// <summary>
  /// V5：读模型实际承载的枚举同时返回取值与文本计算属性——项目类型、就诊类型用安全解析文本，
  /// 处理结果决策用互认结果枚举承载并提供文本；各文本属性都使用该枚举自己的 SourceGen 描述列表。
  /// </summary>
  [Fact]
  public void Statistics_read_models_expose_enum_value_and_text_pairs()
  {
    AssertEnumTextProperty("RecognitionUsageSummaryReadModel", "ItemTypeText", "MedicalItemTypeDescriptorList.List", safeParse: true);
    AssertEnumTextProperty("SourceRecognitionSummaryReadModel", "ItemTypeText", "MedicalItemTypeDescriptorList.List", safeParse: true);
    AssertEnumTextProperty("RecognitionStatisticsItemReadModel", "ItemTypeText", "MedicalItemTypeDescriptorList.List", safeParse: true);
    AssertEnumTextProperty("RecognitionUsageDetailReadModel", "VisitTypeText", "VisitTypeDescriptorList.List", safeParse: false);
    AssertEnumTextProperty("RecognitionMatchRecordReadModel", "VisitTypeText", "VisitTypeDescriptorList.List", safeParse: false);
    AssertEnumTextProperty("RecognitionProcessingResultDetailReadModel", "DecisionText", "RecognitionResultDescriptorList.List", safeParse: true);
    AssertEnumTextProperty("RecognitionMatchRecordItemReadModel", "DecisionText", "RecognitionResultDescriptorList.List", safeParse: true);
  }

  /// <summary>
  /// V5：接收侧汇总读模型携带汇总维度字段，两个汇总读模型都携带标准项目名称，
  /// 不采纳原因汇总为嵌套集合，匹配记录集合视图的匹配项为嵌套集合。
  /// </summary>
  [Fact]
  public void Statistics_summary_read_models_expose_dimension_project_name_and_nested_collections()
  {
    CompilationUnitSyntax usageSummary = ReadSingleReadModel("RecognitionUsageSummaryReadModel");
    PropertyDeclarationSyntax dimension = Assert.Single(
      usageSummary.DescendantNodes().OfType<PropertyDeclarationSyntax>(),
      property => property.Identifier.ValueText == "GroupDimension");
    Assert.Equal("RecognitionStatisticsGroupDimension", DescribeType(dimension.Type));

    CompilationUnitSyntax sourceSummary = ReadSingleReadModel("SourceRecognitionSummaryReadModel");
    Assert.Single(sourceSummary.DescendantNodes().OfType<PropertyDeclarationSyntax>(),
      property => property.Identifier.ValueText == "GroupDimension");
    Assert.Single(usageSummary.DescendantNodes().OfType<PropertyDeclarationSyntax>(),
      property => property.Identifier.ValueText == "StandardProjectName");
    Assert.Single(sourceSummary.DescendantNodes().OfType<PropertyDeclarationSyntax>(),
      property => property.Identifier.ValueText == "StandardProjectName");
  }

  /// <summary>
  /// V2、V3：三个统计枚举的取值、成员名与中文说明逐项冻结，枚举声明处启用 SourceGen 描述器，
  /// 每个成员带字面量中文 <c>[Description]</c>。
  /// </summary>
  [Fact]
  public void Statistics_enums_declare_exact_members_with_descriptor_opt_in_and_chinese_text()
  {
    foreach ((string enumName, (string Member, int Value, string Description)[] members) in StatisticsEnums)
    {
      CompilationUnitSyntax root = SourceSyntaxGuard.Read(
        "server", "Dy.MedicalRecognition.Domain.Share", "Enums", $"{enumName}.cs");

      EnumDeclarationSyntax declaration = Assert.Single(root.DescendantNodes().OfType<EnumDeclarationSyntax>(),
        candidate => candidate.Identifier.ValueText == enumName);
      Assert.NotNull(SourceSyntaxGuard.FindAttribute(declaration.AttributeLists, "EnumDescriptor"));
      Assert.Contains("using Dy.Core.SourceGen;", root.ToFullString(), StringComparison.Ordinal);

      Assert.Equal(
        members.Select(member => member.Member).ToArray(),
        declaration.Members.Select(member => member.Identifier.ValueText).ToArray());

      foreach (EnumMemberDeclarationSyntax member in declaration.Members)
      {
        (string Member, int Value, string Description) expected = Assert.Single(
          members, candidate => candidate.Member == member.Identifier.ValueText);
        Assert.NotNull(member.EqualsValue);
        Assert.Equal(
          expected.Value,
          Assert.IsType<LiteralExpressionSyntax>(member.EqualsValue!.Value).Token.Value is int literalValue
            ? literalValue
            : throw new InvalidOperationException($"枚举成员 {member.Identifier.ValueText} 的取值不是整数字面量。"));
        Assert.Equal(
          expected.Description,
          SourceSyntaxGuard.LiteralStringArgument(SourceSyntaxGuard.FindAttribute(member.AttributeLists, "Description")));
      }
    }
  }

  /// <summary>
  /// V6：四个统计查询请求以内嵌 <c>PageRequestDto Page</c> 承载分页，两个导出请求与匹配记录请求不声明分页对象；
  /// 全部请求的顶层不得出现页码与页容量字段（它们只属于内嵌分页对象）。
  /// </summary>
  [Fact]
  public void Statistics_query_requests_nest_paging_and_exports_declare_none()
  {
    string[] pagedRequests =
    [
      nameof(RecognitionUsageSummaryQueryRequest), nameof(BranchRecognitionUsageSummaryQueryRequest),
      nameof(RecognitionUsageDetailsQueryRequest), nameof(BranchRecognitionUsageDetailsQueryRequest),
      nameof(SourceRecognitionSummaryQueryRequest), nameof(BranchSourceRecognitionSummaryQueryRequest),
      nameof(SourceRecognitionDetailsQueryRequest), nameof(BranchSourceRecognitionDetailsQueryRequest)
    ];
    string[] unpagedRequests =
    [
      nameof(RecognitionStatisticsExportRequest), nameof(BranchRecognitionStatisticsExportRequest),
      nameof(RecognitionMatchRecordQueryRequest)
    ];

    foreach (string typeName in pagedRequests)
    {
      PropertyDeclarationSyntax page = Assert.Single(
        ReadSingleRequest(typeName).DescendantNodes().OfType<PropertyDeclarationSyntax>(),
        property => property.Identifier.ValueText == "Page");
      Assert.Equal("PageRequestDto", DescribeType(page.Type));
    }

    foreach (string typeName in unpagedRequests)
    {
      List<PropertyDeclarationSyntax> properties = [.. ReadSingleRequest(typeName).DescendantNodes().OfType<PropertyDeclarationSyntax>()];
      Assert.DoesNotContain(properties, property => property.Identifier.ValueText == "Page");
    }
  }

  /// <summary>
  /// V1、V6 的运行期面：请求校验拒绝未定义枚举取值、空白编码与空 Guid，合法请求通过；
  /// 内嵌分页对象的取值域声明（一基页码、页容量 1–200）经递归校验生效。
  /// </summary>
  /// <remarks>
  /// 直接调用公共请求校验器，不经过 HTTP、不访问数据库。枚举请求属性为非空值类型：
  /// 未提供即默认值 0，不在枚举定义值域内，由 <c>EnumDataType</c> 声明拒绝，因此必填语义在契约层成立。
  /// </remarks>
  [Fact]
  public void Statistics_request_validation_rejects_undefined_enums_blank_codes_and_empty_guid()
  {
    // 未提供汇总维度（默认值 0）按未定义枚举值拒绝：必填语义由取值域声明承载。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionStatisticsExportRequest { StartTime = new DateOnly(2026, 1, 1), EndTime = new DateOnly(2026, 1, 31) }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionUsageDetailsQueryRequest { StartTime = new DateOnly(2026, 1, 1), EndTime = new DateOnly(2026, 1, 31) }));

    // 明细类型取枚举之外的定义值（如 99）同样拒绝。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionUsageDetailsQueryRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        DetailType = (RecognitionUsageDetailType)99
      }));

    // 可选编码字段提交空白按拒绝处理，不降级为「不过滤」。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        OrganizationCode = " "
      }));

    // 匹配记录视图请求的空 Guid 拒绝，合法标识放行。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = Guid.Empty }));
    MedicalRecognitionRequestValidator.Validate(new RecognitionMatchRecordQueryRequest { RecognitionMatchRecordId = Guid.NewGuid() });

    // 分页窗口经内嵌对象递归校验：页码小于 1 与页容量越界拒绝，边界值放行。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new BranchRecognitionUsageSummaryQueryRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 0, PageSize = 20 }
      }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      new BranchRecognitionUsageSummaryQueryRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 201 }
      }));
    MedicalRecognitionRequestValidator.Validate(new BranchRecognitionUsageSummaryQueryRequest
    {
      StartTime = new DateOnly(2026, 1, 1),
      EndTime = new DateOnly(2026, 1, 31),
      GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
      Page = new PageRequestDto { PageIndex = 1, PageSize = 200 }
    });
  }

  /// <summary>
  /// V6 的序列化面：统计查询请求体把分页放在内嵌 <c>page</c> 对象里，页码与页容量不出现在顶层，
  /// 导出请求体不出现分页对象。
  /// </summary>
  /// <remarks>序列化按宿主的 Web 默认口径（camelCase 属性名），端点收到的就是该文本。</remarks>
  [Fact]
  public void Statistics_request_bodies_nest_paging_under_page_and_exports_omit_it()
  {
    System.Text.Json.JsonSerializerOptions options = new(System.Text.Json.JsonSerializerDefaults.Web);

    string queryBody = System.Text.Json.JsonSerializer.Serialize(
      new RecognitionUsageSummaryQueryRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital,
        Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
      },
      options);
    using System.Text.Json.JsonDocument queryDocument = System.Text.Json.JsonDocument.Parse(queryBody);
    System.Text.Json.JsonElement queryRoot = queryDocument.RootElement;
    Assert.False(queryRoot.TryGetProperty("pageIndex", out _));
    Assert.False(queryRoot.TryGetProperty("pageSize", out _));
    Assert.Equal(1, queryRoot.GetProperty("page").GetProperty("pageIndex").GetInt32());
    Assert.Equal(20, queryRoot.GetProperty("page").GetProperty("pageSize").GetInt32());

    string exportBody = System.Text.Json.JsonSerializer.Serialize(
      new RecognitionStatisticsExportRequest
      {
        StartTime = new DateOnly(2026, 1, 1),
        EndTime = new DateOnly(2026, 1, 31),
        ExportType = RecognitionStatisticsExportType.RecognitionUsageSummary,
        GroupDimension = RecognitionStatisticsGroupDimension.Hospital
      },
      options);
    using System.Text.Json.JsonDocument exportDocument = System.Text.Json.JsonDocument.Parse(exportBody);
    Assert.False(exportDocument.RootElement.TryGetProperty("page", out _));
  }

  /// <summary>
  /// V5 的运行期面：读模型的枚举文本计算属性按服务端声明解析——项目类型与决策取安全解析，
  /// 就诊类型取严格解析；未反馈项的决策文本为 <see langword="null"/>。
  /// </summary>
  [Fact]
  public void Statistics_read_model_enum_texts_parse_from_server_declarations()
  {
    RecognitionUsageSummaryReadModel summary = new() { ItemType = MedicalItemType.Laboratory };
    Assert.Equal("检验", summary.ItemTypeText);
    Assert.Null(new RecognitionUsageSummaryReadModel { ItemType = null }.ItemTypeText);
    Assert.Null(new RecognitionUsageSummaryReadModel { ItemType = (MedicalItemType)99 }.ItemTypeText);

    RecognitionUsageDetailReadModel detail = new() { VisitType = VisitType.Inpatient };
    Assert.Equal("住院", detail.VisitTypeText);

    RecognitionProcessingResultDetailReadModel processed = new() { Decision = RecognitionResult.Adopted };
    Assert.Equal("采纳", processed.DecisionText);
    Assert.Null(new RecognitionProcessingResultDetailReadModel { Decision = null }.DecisionText);
  }

  /// <summary>按类型名读取统计读模型的唯一声明文件。</summary>
  /// <param name="typeName">读模型类型名。</param>
  /// <returns>该读模型声明文件的语法树根节点。</returns>
  private static CompilationUnitSyntax ReadSingleReadModel(string typeName)
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
      SourceSyntaxGuard.ReadTypeDeclarations(ReadModelDirectory, typeName);
    Assert.True(
      declarations.Count == 1 && declarations[0].RelativePath == $"{ReadModelDirectory}/{typeName}.cs",
      $"读模型 {typeName} 应恰好声明在 {ReadModelDirectory}/{typeName}.cs，实际：{string.Join("、", declarations.Select(declaration => declaration.RelativePath))}。");
    return declarations[0].Root;
  }

  /// <summary>按类型名读取统计请求类型的唯一声明文件。</summary>
  /// <param name="typeName">请求类型名。</param>
  /// <returns>该请求类型声明文件的语法树根节点。</returns>
  private static CompilationUnitSyntax ReadSingleRequest(string typeName)
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations =
      SourceSyntaxGuard.ReadTypeDeclarations("server/Dy.MedicalRecognition.Application.Contracts/Queries", typeName);
    Assert.True(
      declarations.Count == 1,
      $"请求类型 {typeName} 应恰好一处声明，实际：{string.Join("、", declarations.Select(declaration => declaration.RelativePath))}。");
    return declarations[0].Root;
  }

  /// <summary>断言请求类型的属性名集合与声明顺序、属性类型逐项相符。</summary>
  /// <param name="typeName">请求类型名。</param>
  /// <param name="expectedProperties">期望的属性名与类型文本，按声明顺序。</param>
  private static void AssertRequestProperties(string typeName, (string Name, string TypeName)[] expectedProperties)
  {
    List<PropertyDeclarationSyntax> properties = [.. ReadSingleRequest(typeName).DescendantNodes().OfType<PropertyDeclarationSyntax>()];

    Assert.True(
      expectedProperties.Select(property => property.Name).SequenceEqual(properties.Select(property => property.Identifier.ValueText)),
      $"{typeName} 的属性集合或声明顺序不符。期望：{string.Join(", ", expectedProperties.Select(property => property.Name))}；" +
      $"实际：{string.Join(", ", properties.Select(property => property.Identifier.ValueText))}。");

    foreach ((string propertyName, string propertyType) in expectedProperties)
    {
      PropertyDeclarationSyntax property = Assert.Single(properties, candidate => candidate.Identifier.ValueText == propertyName);
      Assert.True(
        DescribeType(property.Type) == propertyType,
        $"{typeName}.{propertyName} 的属性类型应为 {propertyType}，实际为 {DescribeType(property.Type)}。");
    }
  }

  /// <summary>断言枚举文本计算属性存在、为表达式体，并使用指定枚举自己的描述列表解析。</summary>
  /// <param name="typeName">读模型类型名。</param>
  /// <param name="propertyName">文本计算属性名。</param>
  /// <param name="descriptorList">该枚举的 SourceGen 描述列表表达式。</param>
  /// <param name="safeParse">为真时要求安全解析（<c>GetOrNull</c>），为假时要求严格解析（<c>Get</c>）。</param>
  private static void AssertEnumTextProperty(string typeName, string propertyName, string descriptorList, bool safeParse)
  {
    PropertyDeclarationSyntax property = Assert.Single(
      ReadSingleReadModel(typeName).DescendantNodes().OfType<PropertyDeclarationSyntax>(),
      candidate => candidate.Identifier.ValueText == propertyName);

    string expression = (property.ExpressionBody ?? throw new InvalidOperationException(
        $"{typeName}.{propertyName} 应为表达式体计算属性。")).Expression.ToFullString();
    Assert.Contains(safeParse ? "EnumDescriptorText.GetOrNull" : "EnumDescriptorText.Get(", expression, StringComparison.Ordinal);
    Assert.Contains(descriptorList, expression, StringComparison.Ordinal);
  }

  /// <summary>重建属性或返回类型的类型文本：保留可空标记与泛型实参，限定名取简单名。</summary>
  /// <param name="type">类型语法节点。</param>
  /// <returns>归一后的类型文本。</returns>
  private static string DescribeType(TypeSyntax type) => type switch
  {
    NullableTypeSyntax nullable => DescribeType(nullable.ElementType) + "?",
    GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(", ", generic.TypeArgumentList.Arguments.Select(DescribeType))}>",
    ArrayTypeSyntax array => $"{DescribeType(array.ElementType)}[]",
    _ => SourceSyntaxGuard.SimpleTypeName(type)
  };

  /// <summary>把参数声明冻结为「类型 名称」文本：类型与名称任一漂移都会让断言失败。</summary>
  /// <param name="parameter">参数声明节点。</param>
  /// <returns>归一后的参数文本。</returns>
  private static string DescribeParameter(ParameterSyntax parameter) =>
    $"{DescribeType(parameter.Type!)} {parameter.Identifier.ValueText}";

  /// <summary>把签名文本中的换行与连续空白归一为单个空格，使冻结判据对排版不敏感。</summary>
  /// <param name="text">签名文本。</param>
  /// <returns>空白归一后的文本。</returns>
  private static string NormalizeType(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
