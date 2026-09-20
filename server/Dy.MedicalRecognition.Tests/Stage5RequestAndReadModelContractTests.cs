using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Events;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 的请求与读模型契约面（矩阵 V81 四个请求的字段集合、V82 匹配响应读模型字段、
/// V83 引用详情读模型字段、V84 检查所见与结论的字段边界、V85 的读模型计算属性面）。
/// </summary>
/// <remarks>
/// 全部判据按反射读取公开属性、属性类型与访问器，并直接调用请求校验入口；
/// 不解析源码文本、不做正则匹配，不连接数据库、不启动宿主。
/// 请求与读模型的属性集合采用逐项等值断言：字段增删或改名都会让本文件失败，避免调用端在不知情的情况下收到形状变化。
/// </remarks>
public sealed class Stage5RequestAndReadModelContractTests
{
  /// <summary>
  /// V81 明列的禁止字段：组织编码、医院编码、院区编码与操作人。
  /// </summary>
  /// <remarks>组织、医院、院区由可信上下文确定，操作人由服务端从登录上下文解析；请求侧出现任一字段即为越权覆盖面。</remarks>
  private static readonly string[] TrustedScopeAndOperatorFields =
  [
    "OrganizationCode", "HospitalCode", "BranchCode", "OperId", "OperatorId"
  ];

  /// <summary>
  /// 匹配查询请求不得提交的院内时间字段。
  /// </summary>
  /// <remarks>查询截止点取平台收到本次查询的时间，医院不需要额外提供挂号、入院或统一就诊时间（SRS F04 业务规则 1）。</remarks>
  private static readonly string[] ForbiddenVisitTimeFields =
  [
    "RegistrationTime", "AdmissionTime", "UnifiedVisitTime", "VisitTime"
  ];

  /// <summary>
  /// 引用详情请求不得提交的平台侧定位字段。
  /// </summary>
  /// <remarks>引用详情按医院必然掌握的患者证件与本次来源就诊匹配，平台患者、匹配记录、匹配项与互认时间都不由调用方提交（SRS F06 前置条件 2）。</remarks>
  private static readonly string[] ForbiddenCitationLocatorFields =
  [
    "PlatformPatientId", "RecognitionMatchRecordId", "RecognitionMatchItemId", "RecognitionTime"
  ];

  /// <summary>
  /// 匹配响应不得返回的检查侧大篇幅内容字段。
  /// </summary>
  /// <remarks>检查匹配不直接返回检查所见与检查结论，医生通过 PDF 或影像查看后作出决定（SRS F04 业务规则 16）。</remarks>
  private static readonly string[] ForbiddenMatchResponseFields =
  [
    "ExaminationFindings", "ExaminationConclusion", "Findings", "Conclusion"
  ];

  /// <summary>
  /// 匹配响应不得返回的标本追溯字段：报告采集侧的院内标本号、标本采集时间、标本送检时间与标本类型编码。
  /// </summary>
  /// <remarks>
  /// 标识符取自报告采集侧实体与 DTO 的实际英文名称；标本类型编码与院内标本号只用于来源追溯，不进入匹配响应（SRS F04 业务规则 14）。
  /// 标本类型名称是唯一随匹配响应返回的标本字段。
  /// </remarks>
  private static readonly string[] ForbiddenSpecimenTraceFields =
  [
    "SourceSpecimenNo", "SpecimenCollectedTime", "SpecimenSubmittedTime", "SpecimenTypeCode"
  ];

  /// <summary>
  /// 匹配响应不得携带的固定互认超期时间字段。
  /// </summary>
  /// <remarks>有效期按查询时点与项目当前可互认时间动态判定，平台不保存也不返回固定截止时间（SRS F04 业务规则 19）。</remarks>
  private static readonly string[] ForbiddenFixedExpiryFields =
  [
    "RecognitionExpireTime", "RecognitionExpiredTime", "RecognitionDeadline", "RecognitionValidityTime",
    "ExpireTime", "ExpiredTime", "ExpirationTime", "ValidUntil"
  ];

  /// <summary>
  /// PDF 与影像入口不得携带的有效期与一次性状态字段。
  /// </summary>
  /// <remarks>下载入口不设独立期限、一次性读取或消费状态；报告版本失效只由每次下载时的归属与版本校验决定（SRS F06 业务规则 15、17）。</remarks>
  private static readonly string[] ForbiddenFileAccessStateFields =
  [
    "ValidUntil", "ValidMinutes", "ValidityMinutes", "ValidityDurationMinutes", "ValidityDurationHours",
    "ExpireTime", "ExpiredTime",
    "ExpirationTime", "IsConsumed", "ConsumedTime", "IsOneTime", "OneTimeOnly", "ReadStatus",
    "ConsumeStatus", "IsRead", "IsUsed", "DownloadCount"
  ];

  /// <summary>
  /// 四个请求类型的字段集合与 V81 逐项一致：各类只承载调用方能提交的业务字段。
  /// </summary>
  /// <remarks>
  /// 匹配查询携带患者证件、本次来源就诊与拟开项目集合；处理结果携带匹配记录、组级互认字段与完整项目决定数组；
  /// 引用详情只携带患者证件与本次来源就诊；引用结果只携带引用项目数组。
  /// </remarks>
  [Fact]
  public void Four_requests_expose_exactly_the_v81_field_set()
  {
    Assert.Equal(
      ["IdentityDocumentNo", "IdentityDocumentTypeCode", "ProposedItems", "VisitSerialNo", "VisitType"],
      DeclaredPropertyNames(typeof(RecognitionMatchQueryRequest)));
    Assert.Equal(
      [
        "ProcessingResults", "RecognitionDeptId", "RecognitionDeptName", "RecognitionDoctorId",
        "RecognitionDoctorName", "RecognitionMatchRecordId", "RecognitionTime"
      ],
      BusinessFieldNames(typeof(RecognitionProcessingResultSubmissionRequest)));
    Assert.Equal(
      ["IdentityDocumentNo", "IdentityDocumentTypeCode", "VisitSerialNo", "VisitType"],
      DeclaredPropertyNames(typeof(RecognitionCitationDetailRequest)));
    Assert.Equal(
      ["ReferenceItems"],
      DeclaredPropertyNames(typeof(RecognitionReferenceSubmissionRequest)));

    // 三个项目级输入类型各自承载项目数组元素的字段。
    Assert.Equal(
      ["ItemType", "StandardProjectCode"],
      DeclaredPropertyNames(typeof(RecognitionMatchProposedItemRequest)));
    Assert.Equal(
      ["NonAdoptionDescription", "NonAdoptionReason", "RecognitionMatchItemId", "Result"],
      DeclaredPropertyNames(typeof(RecognitionProcessingResultItemRequest)));
    Assert.Equal(
      [
        "RecognitionMatchItemId", "ReferenceDeptId", "ReferenceDeptName", "ReferenceDoctorId",
        "ReferenceDoctorName", "ReferencedTime"
      ],
      DeclaredPropertyNames(typeof(RecognitionReferenceItemRequest)));
  }

  /// <summary>
  /// 四个请求类型都不含组织编码、医院编码、院区编码与操作人。
  /// </summary>
  /// <remarks>这四个字段由服务端从可信上下文与登录上下文写入，请求侧一旦出现就会被调用方覆盖可信归属。</remarks>
  [Fact]
  public void Four_requests_carry_no_trusted_scope_or_operator_field()
  {
    Type[] requestTypes =
    [
      typeof(RecognitionMatchQueryRequest), typeof(RecognitionProcessingResultSubmissionRequest),
      typeof(RecognitionCitationDetailRequest), typeof(RecognitionReferenceSubmissionRequest)
    ];

    foreach (Type type in requestTypes)
    {
      foreach (string propertyName in TrustedScopeAndOperatorFields) Assert.Null(type.GetProperty(propertyName));

      // 扫描规模下界：请求类型上确实有业务字段，逐一判空不是扫描面为空的结果。
      Assert.NotEmpty(DeclaredPropertyNames(type));
    }

    // 变异证据：同一判据在真实存在的业务字段上必须命中。
    string[] matchQueryFields = DeclaredPropertyNames(typeof(RecognitionMatchQueryRequest));
    Assert.DoesNotContain("OrganizationCode", matchQueryFields);
    Assert.Contains("IdentityDocumentNo", matchQueryFields);
  }

  /// <summary>
  /// 匹配查询请求不含挂号时间、入院时间与统一就诊时间。
  /// </summary>
  [Fact]
  public void Match_query_request_omits_registration_admission_and_unified_visit_time()
  {
    Type type = typeof(RecognitionMatchQueryRequest);

    foreach (string propertyName in ForbiddenVisitTimeFields) Assert.Null(type.GetProperty(propertyName));

    string[] fields = DeclaredPropertyNames(type);
    Assert.DoesNotContain("RegistrationTime", fields);
    Assert.Contains("VisitSerialNo", fields);
  }

  /// <summary>
  /// 引用详情请求不含平台患者标识、互认匹配记录标识、互认匹配项标识与互认时间。
  /// </summary>
  [Fact]
  public void Citation_detail_request_omits_platform_patient_and_match_locators()
  {
    Type type = typeof(RecognitionCitationDetailRequest);

    foreach (string propertyName in ForbiddenCitationLocatorFields) Assert.Null(type.GetProperty(propertyName));

    string[] fields = DeclaredPropertyNames(type);
    Assert.DoesNotContain("RecognitionMatchItemId", fields);
    Assert.Contains("IdentityDocumentTypeCode", fields);
  }

  /// <summary>
  /// 处理结果请求的互认科室与互认医生标识以字符串承载，项目数组为强类型集合。
  /// </summary>
  /// <remarks>医院业务科室与人员标识统一使用字符串，不假定为平台主数据标识（医院业务科室和人员统一规则）。</remarks>
  [Fact]
  public void Processing_result_request_carries_string_department_and_doctor_identifiers()
  {
    Type type = typeof(RecognitionProcessingResultSubmissionRequest);

    Assert.Equal(typeof(string), type.GetProperty("RecognitionDeptId")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("RecognitionDoctorId")!.PropertyType);
    Assert.Equal(typeof(DateTime), type.GetProperty("RecognitionTime")!.PropertyType);
    Assert.Equal(typeof(Guid), type.GetProperty("RecognitionMatchRecordId")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionProcessingResultItemRequest>),
      type.GetProperty("ProcessingResults")!.PropertyType);
  }

  /// <summary>
  /// 引用结果请求的引用科室与引用医生标识以字符串承载，引用项目数组为强类型集合。
  /// </summary>
  [Fact]
  public void Reference_submission_request_carries_string_reference_staff_identifiers()
  {
    Type type = typeof(RecognitionReferenceSubmissionRequest);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionReferenceItemRequest>),
      type.GetProperty("ReferenceItems")!.PropertyType);

    Type itemType = typeof(RecognitionReferenceItemRequest);
    Assert.Equal(typeof(string), itemType.GetProperty("ReferenceDeptId")!.PropertyType);
    Assert.Equal(typeof(string), itemType.GetProperty("ReferenceDoctorId")!.PropertyType);
    Assert.Equal(typeof(DateTime), itemType.GetProperty("ReferencedTime")!.PropertyType);
    Assert.Equal(typeof(Guid), itemType.GetProperty("RecognitionMatchItemId")!.PropertyType);

    // 引用结果不重复提交匹配记录标识、本次来源就诊与互认时间。
    foreach (string propertyName in new[] { "RecognitionMatchRecordId", "VisitType", "VisitSerialNo", "RecognitionTime" })
      Assert.Null(type.GetProperty(propertyName));
    foreach (string propertyName in new[] { "RecognitionMatchRecordId", "RecognitionTime" })
      Assert.Null(itemType.GetProperty(propertyName));
  }

  /// <summary>
  /// 四个请求类型经请求校验入口判定：必填缺失与标识类型非法被判拒绝，合法请求通过。
  /// </summary>
  /// <remarks>校验特性只是声明，反序列化与参数绑定都不自动执行，必须由 <see cref="MedicalRecognitionRequestValidator"/> 显式触发。</remarks>
  [Fact]
  public void Four_requests_are_validated_for_required_fields_and_identifier_types()
  {
    // 空请求：逐项必填缺失全部被拒绝，且拒绝文案指向参数校验。
    Assert.Contains("参数校验失败", RequiredError(new RecognitionMatchQueryRequest()).Message);
    Assert.Contains("参数校验失败", RequiredError(new RecognitionProcessingResultSubmissionRequest()).Message);
    Assert.Contains("参数校验失败", RequiredError(new RecognitionCitationDetailRequest()).Message);
    Assert.Contains("参数校验失败", RequiredError(new RecognitionReferenceSubmissionRequest()).Message);

    // 标识类型非法：空 Guid 与空标识分别被拒绝；其余字段保持合法，使失败只归因于被检查的标识。
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      ValidProcessingResultRequest() with { RecognitionMatchRecordId = Guid.Empty }));
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      ValidProcessingResultRequest() with { ProcessingResults = [ValidProcessingResultItem() with { RecognitionMatchItemId = Guid.Empty }] }));
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      ValidReferenceRequest() with { ReferenceItems = [ValidReferenceItem() with { RecognitionMatchItemId = Guid.Empty }] }));
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      ValidReferenceRequest() with { ReferenceItems = [ValidReferenceItem() with { ReferenceDeptId = string.Empty }] }));
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(
      ValidMatchQueryRequest() with
      {
        ProposedItems = [new RecognitionMatchProposedItemRequest { ItemType = MedicalItemType.Laboratory, StandardProjectCode = string.Empty }]
      }));

    // 合法请求通过。
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(ValidMatchQueryRequest())));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(ValidProcessingResultRequest())));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(ValidCitationDetailRequest())));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(ValidReferenceRequest())));
  }

  /// <summary>
  /// 十个契约读模型不含无类型的公开实例属性。
  /// </summary>
  /// <remarks>
  /// 集合与枚举字段退化为 <see cref="object"/> 时，生成端与页面无法据此得到字段形状，
  /// 只能按任意结构处理，字段边界随之失去声明面。
  /// </remarks>
  [Fact]
  public void Ten_contract_read_models_expose_no_object_typed_property()
  {
    foreach (Type type in ContractReadModelTypes())
    {
      PropertyInfo[] properties = PublicInstanceProperties(type);

      // 扫描规模下界：属性集合为空时下面的负向断言恒真，先在这里失败。
      Assert.NotEmpty(properties);
      Assert.DoesNotContain(properties, property => property.PropertyType == typeof(object));
    }

    // 变异证据：同一判据在带无类型属性的类型上必须命中。
    Assert.Contains(
      typeof(Stage5UntypedProbe).GetProperties(),
      property => property.PropertyType == typeof(object));
  }

  /// <summary>
  /// 十个契约读模型的公开实例属性均为只读：没有可写 setter。
  /// </summary>
  /// <remarks>
  /// <c>init</c> 访问器只在对象构造阶段可赋值，在元数据里表现为带 <c>IsExternalInit</c> 修饰要求的公开 setter，
  /// 因此按该修饰要求区分只读初始化与可写赋值，不按 setter 是否存在判定。
  /// </remarks>
  [Fact]
  public void Ten_contract_read_models_expose_read_only_properties()
  {
    foreach (Type type in ContractReadModelTypes())
    {
      PropertyInfo[] properties = PublicInstanceProperties(type);
      Assert.NotEmpty(properties);

      foreach (PropertyInfo property in properties)
        Assert.False(IsWritable(property), $"{type.Name}.{property.Name} 存在可写 setter。");
    }

    // 变异证据：同一判据在带可写属性的类型上必须命中，证明上面的判空不是对所有属性都成立。
    Assert.True(IsWritable(typeof(Stage5WritableProbe).GetProperty(nameof(Stage5WritableProbe.Value))!));
    Assert.False(IsWritable(typeof(RecognitionMatchesResponseReadModel).GetProperty(nameof(RecognitionMatchesResponseReadModel.HasMatches))!));
  }

  /// <summary>
  /// 匹配响应不含检查所见与检查结论；引用详情含该两项（V84）。
  /// </summary>
  /// <remarks>
  /// 匹配响应只让医生决定是否采纳，检查所见与结论留待通过 PDF 或影像查看；
  /// 引用详情要把内容写入病历，因此按整份报告的公共内容携带该两项。
  /// 两侧都沿属性的元素类型与集合的元素类型向下遍历，嵌套层字段与本层字段按同一口径判定。
  /// </remarks>
  [Fact]
  public void Examination_findings_and_conclusion_belong_to_citation_detail_only()
  {
    Type citationDetailType = typeof(RecognitionCitationDetailReadModel);
    Type reportContextType = typeof(RecognitionReportContextReadModel);

    string[] matchResponsePropertyNames = ResponsePropertyNames(typeof(RecognitionMatchesResponseReadModel));
    foreach (string propertyName in ForbiddenMatchResponseFields)
      Assert.DoesNotContain(propertyName, matchResponsePropertyNames);

    string[] citationDetailPropertyNames = ResponsePropertyNames(citationDetailType);
    Assert.Contains("ExaminationFindings", citationDetailPropertyNames);
    Assert.Contains("ExaminationConclusion", citationDetailPropertyNames);
    Assert.Equal(typeof(string), reportContextType.GetProperty("ExaminationFindings")!.PropertyType);
    Assert.Equal(typeof(string), reportContextType.GetProperty("ExaminationConclusion")!.PropertyType);

    // 判据规模下界：嵌套层的字段确实进入集合，逐一判空不是只扫描根类型的结果。
    Assert.Contains("LaboratoryResults", matchResponsePropertyNames);
    Assert.Contains("PdfFileId", matchResponsePropertyNames);
    Assert.Contains("SourceApplicantDoctorId", citationDetailPropertyNames);

    // 引用详情的集合字段是强类型集合，不退回无类型对象。
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionCitationItemReadModel>),
      citationDetailType.GetProperty("MatchItems")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionReportContextReadModel>),
      citationDetailType.GetProperty("ReportContext")!.PropertyType);

    // 变异证据：负向判据在确实存在该字段的类型上必须命中，正向判据只在沿类型图下探到报告公共上下文时命中。
    Assert.Contains("ExaminationFindings", DeclaredPropertyNames(reportContextType));
    Assert.DoesNotContain(
      "ExaminationFindings", DeclaredPropertyNames(typeof(RecognitionMatchReportReadModel)));
    Assert.DoesNotContain(
      "ExaminationFindings", DeclaredPropertyNames(typeof(RecognitionMatchItemReadModel)));
  }

  /// <summary>
  /// 引用详情的 PDF 与影像入口不携带独立有效期，也不携带一次性或消费状态。
  /// </summary>
  [Fact]
  public void Pdf_and_image_access_carries_no_validity_or_consumption_state()
  {
    Type type = typeof(PdfAndImageAccessReadModel);

    foreach (string propertyName in ForbiddenFileAccessStateFields) Assert.Null(type.GetProperty(propertyName));

    // 变异证据：同一判据在真实字段上必须命中，证明逐一判空不是扫描面为空的结果。
    Assert.Contains("PdfDownloadUrl", DeclaredPropertyNames(type));
    Assert.DoesNotContain("ValidUntil", DeclaredPropertyNames(type));
  }

  /// <summary>
  /// 匹配报告含强类型的文件入口与匹配项集合，匹配项含检验明细、检查部位与来源影像状态。
  /// </summary>
  [Fact]
  public void Match_report_carries_typed_file_and_item_collections()
  {
    Type reportType = typeof(RecognitionMatchReportReadModel);
    Type fileType = typeof(PdfAndImageAccessReadModel);

    Assert.Equal(typeof(string), fileType.GetProperty("PdfFileId")!.PropertyType);
    Assert.Equal(typeof(string), fileType.GetProperty("PdfDownloadUrl")!.PropertyType);
    Assert.Equal(typeof(string), fileType.GetProperty("PdfOriginalFileName")!.PropertyType);
    Assert.Equal(typeof(PdfAndImageAccessReadModel), reportType.GetProperty("File")!.PropertyType);
    Assert.Equal(typeof(IReadOnlyList<RecognitionMatchItemReadModel>), reportType.GetProperty("MatchItems")!.PropertyType);
    Assert.Equal(typeof(MedicalReportType), reportType.GetProperty("ReportType")!.PropertyType);

    Type itemType = typeof(RecognitionMatchItemReadModel);
    Assert.Equal(typeof(string), itemType.GetProperty("SpecimenTypeName")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionMatchLaboratoryResultReadModel>),
      itemType.GetProperty("LaboratoryResults")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionExaminationSiteReadModel>),
      itemType.GetProperty("ExaminationSites")!.PropertyType);
    Assert.Equal(typeof(string), itemType.GetProperty("OverallAbnormalFlag")!.PropertyType);
    Assert.Equal(typeof(SourceImageStatus?), fileType.GetProperty("SourceImageStatus")!.PropertyType);
    Assert.Equal(typeof(string), fileType.GetProperty("ImageAccessUrl")!.PropertyType);
  }

  /// <summary>
  /// 项目级读模型的字段集合、字段类型与可空性与正式响应模型逐项一致（V82、V83）。
  /// </summary>
  /// <remarks>
  /// 字段清单取正式响应模型的对应读模型；断言采用集合逐项相等，任一字段缺失、改名或多出字段都会失败。
  /// 来源未提供即无业务值的字段为可空字符串，必填字段不可为空；可空引用类型不改变运行时属性类型，按可空性元数据判定。
  /// </remarks>
  [Fact]
  public void Item_level_read_models_expose_exactly_the_declared_fields_and_types()
  {
    Type matchLaboratoryType = typeof(RecognitionMatchLaboratoryResultReadModel);
    Type citationLaboratoryType = typeof(RecognitionCitationLaboratoryResultReadModel);

    // 匹配响应与引用详情的检验结果明细字段清单一致。
    string[] laboratoryResultFields =
    [
      "ResultItemName", "SourceAbnormalFlag", "SourceCriticalValueFlag", "SourceReferenceRange",
      "SourceResultContent", "Unit"
    ];
    Assert.Equal(laboratoryResultFields, DeclaredPropertyNames(matchLaboratoryType));
    Assert.Equal(laboratoryResultFields, DeclaredPropertyNames(citationLaboratoryType));

    foreach (Type laboratoryType in new[] { matchLaboratoryType, citationLaboratoryType })
    {
      Assert.Equal(typeof(string), laboratoryType.GetProperty("ResultItemName")!.PropertyType);
      Assert.Equal(typeof(string), laboratoryType.GetProperty("SourceResultContent")!.PropertyType);
      Assert.Equal(NullabilityState.NotNull, ReadNullability(laboratoryType, "ResultItemName"));
      Assert.Equal(NullabilityState.NotNull, ReadNullability(laboratoryType, "SourceResultContent"));

      foreach (string optionalName in new[]
                 { "Unit", "SourceReferenceRange", "SourceAbnormalFlag", "SourceCriticalValueFlag" })
      {
        Assert.Equal(typeof(string), laboratoryType.GetProperty(optionalName)!.PropertyType);
        Assert.Equal(NullabilityState.Nullable, ReadNullability(laboratoryType, optionalName));
      }
    }

    // 检查部位：部位名称为必填，来源部位编码允许无业务值。
    Type siteType = typeof(RecognitionExaminationSiteReadModel);
    Assert.Equal(["SiteName", "SourceSiteCode"], DeclaredPropertyNames(siteType));
    Assert.Equal(typeof(string), siteType.GetProperty("SiteName")!.PropertyType);
    Assert.Equal(NullabilityState.NotNull, ReadNullability(siteType, "SiteName"));
    Assert.Equal(typeof(string), siteType.GetProperty("SourceSiteCode")!.PropertyType);
    Assert.Equal(NullabilityState.Nullable, ReadNullability(siteType, "SourceSiteCode"));

    // 项目级引用内容：匹配项标识、报告标识与版本标识按值返回，编码与名称成对返回。
    Type citationItemType = typeof(RecognitionCitationItemReadModel);
    Assert.Equal(
      [
        "ExaminationSites", "LaboratoryResults", "RecognitionMatchItemId", "ReportId", "ReportVersionId",
        "StandardProjectCode", "StandardProjectName"
      ],
      DeclaredPropertyNames(citationItemType));
    Assert.Equal(typeof(Guid), citationItemType.GetProperty("RecognitionMatchItemId")!.PropertyType);
    Assert.Equal(typeof(Guid), citationItemType.GetProperty("ReportId")!.PropertyType);
    Assert.Equal(typeof(Guid), citationItemType.GetProperty("ReportVersionId")!.PropertyType);
    Assert.Equal(typeof(string), citationItemType.GetProperty("StandardProjectCode")!.PropertyType);
    Assert.Equal(typeof(string), citationItemType.GetProperty("StandardProjectName")!.PropertyType);
    Assert.Equal(NullabilityState.NotNull, ReadNullability(citationItemType, "StandardProjectCode"));
    Assert.Equal(NullabilityState.NotNull, ReadNullability(citationItemType, "StandardProjectName"));
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionCitationLaboratoryResultReadModel>),
      citationItemType.GetProperty("LaboratoryResults")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionExaminationSiteReadModel>),
      citationItemType.GetProperty("ExaminationSites")!.PropertyType);

    // 项目级匹配内容：字段集合逐项相等；项目类型与其文本不属于匹配响应的正式字段。
    Type matchItemType = typeof(RecognitionMatchItemReadModel);
    string[] matchItemFields = DeclaredPropertyNames(matchItemType);
    Assert.DoesNotContain("ItemType", matchItemFields);
    Assert.DoesNotContain("ItemTypeText", matchItemFields);
    Assert.Equal(
      [
        "ExaminationSites", "LaboratoryResults", "OverallAbnormalFlag", "RecognitionMatchItemId",
        "SpecimenTypeName", "StandardProjectCode", "StandardProjectName"
      ],
      matchItemFields);
    Assert.Equal(typeof(string), matchItemType.GetProperty("SpecimenTypeName")!.PropertyType);
    Assert.Equal(typeof(string), matchItemType.GetProperty("OverallAbnormalFlag")!.PropertyType);

    // 报告公共上下文中的来源申请与来源审核医生允许无业务值。
    Type contextType = typeof(RecognitionReportContextReadModel);
    foreach (string optionalName in new[]
               { "SourceApplicantDoctorId", "SourceApplicantDoctorName", "SourceReviewerDoctorId", "SourceReviewerDoctorName" })
    {
      Assert.NotNull(contextType.GetProperty(optionalName));
      Assert.Equal(typeof(string), contextType.GetProperty(optionalName)!.PropertyType);
      Assert.Equal(NullabilityState.Nullable, ReadNullability(contextType, optionalName));
    }
  }

  /// <summary>
  /// 匹配响应不含标本类型编码与院内标本号，也不含固定的互认超期时间字段。
  /// </summary>
  [Fact]
  public void Match_response_omits_specimen_trace_codes_and_fixed_expiry()
  {
    Type[] matchResponseTypes =
    [
      typeof(RecognitionMatchesResponseReadModel), typeof(RecognitionMatchReportReadModel),
      typeof(RecognitionMatchItemReadModel), typeof(RecognitionMatchLaboratoryResultReadModel),
      typeof(RecognitionExaminationSiteReadModel), typeof(PdfAndImageAccessReadModel)
    ];

    foreach (Type type in matchResponseTypes)
    {
      foreach (string propertyName in ForbiddenSpecimenTraceFields) Assert.Null(type.GetProperty(propertyName));
      foreach (string propertyName in ForbiddenFixedExpiryFields) Assert.Null(type.GetProperty(propertyName));
    }

    // 负向清单与报告采集侧 DTO 上标本相关字段的实际英文标识符逐项一致：清单使用泛称或漏项时在此失败。
    string[] reportSideSpecimenFields =
    [
      .. DeclaredPropertyNames(typeof(LaboratoryReportContentDto))
        .Where(name => name.Contains("Specimen", StringComparison.Ordinal))
        .Where(name => name != "SpecimenTypeName")
    ];
    Assert.Equal(ForbiddenSpecimenTraceFields, reportSideSpecimenFields);

    // 变异证据：同一判据在真实存在的字段上必须命中，证明逐一判空不是扫描面为空的结果。
    Assert.Contains("SpecimenTypeName", DeclaredPropertyNames(typeof(RecognitionMatchItemReadModel)));
    Assert.DoesNotContain(
      "SourceSpecimenNo", DeclaredPropertyNames(typeof(RecognitionMatchItemReadModel)));
  }

  /// <summary>
  /// 匹配响应与引用详情的关键字段类型：集合与枚举字段都是强类型，空匹配的定位字段可空。
  /// </summary>
  [Fact]
  public void Match_and_citation_read_models_use_strongly_typed_collections_and_enums()
  {
    Type responseType = typeof(RecognitionMatchesResponseReadModel);
    Assert.Equal(typeof(bool), responseType.GetProperty("HasMatches")!.PropertyType);
    Assert.Equal(typeof(IReadOnlyList<RecognitionMatchReportReadModel>), responseType.GetProperty("Reports")!.PropertyType);

    // 空匹配时记录标识与生成时间为空，因此两者是可空值类型。
    Assert.Equal(typeof(Guid?), responseType.GetProperty("RecognitionMatchRecordId")!.PropertyType);
    Assert.Equal(typeof(DateTime?), responseType.GetProperty("MatchCreatedTime")!.PropertyType);

    Type itemType = typeof(RecognitionMatchItemReadModel);
    Assert.Equal(typeof(Guid), itemType.GetProperty("RecognitionMatchItemId")!.PropertyType);
    Assert.Equal(typeof(string), itemType.GetProperty("StandardProjectCode")!.PropertyType);

    Type citationItemType = typeof(RecognitionCitationItemReadModel);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionCitationLaboratoryResultReadModel>),
      citationItemType.GetProperty("LaboratoryResults")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionExaminationSiteReadModel>),
      citationItemType.GetProperty("ExaminationSites")!.PropertyType);

    // 引用详情的报告公共上下文承载 SRS F06 业务规则 5 的必要上下文。
    Type contextType = typeof(RecognitionReportContextReadModel);
    string[] requiredContextFields =
    [
      "SourceOrganizationCode", "SourceOrganizationName", "SourceHospitalCode", "SourceHospitalName",
      "SourceBranchCode", "SourceBranchName", "ReportName", "ClinicalTime", "ReportTime",
      "SourceApplicantDoctorId", "SourceApplicantDoctorName", "SourceReviewerDoctorId", "SourceReviewerDoctorName"
    ];
    foreach (string propertyName in requiredContextFields) Assert.NotNull(contextType.GetProperty(propertyName));

    Assert.Equal(typeof(DateTime), contextType.GetProperty("ClinicalTime")!.PropertyType);
    Assert.Equal(typeof(DateTime), contextType.GetProperty("ReportTime")!.PropertyType);
  }

  /// <summary>
  /// 枚举文本在读模型上以计算属性暴露，且按枚举声明解析出非空中文（V85 的读模型面）。
  /// </summary>
  /// <remarks>
  /// 文本由服务端解析后随契约返回，前端不重写一份文案；计算属性不可赋值。
  /// 枚举属性与文本属性都按反射设置与读取：属性缺失时按断言失败报告，而不是编译错误。
  /// </remarks>
  [Fact]
  public void Read_models_expose_enum_text_as_computed_property()
  {
    Assert.Equal("检验报告", EnumTextOf(new RecognitionMatchReportReadModel(), "ReportType", "ReportTypeText", MedicalReportType.Laboratory));
    Assert.Equal("检查报告", EnumTextOf(new RecognitionReportContextReadModel(), "ReportType", "ReportTypeText", MedicalReportType.Examination));
    Assert.Equal("有影像", EnumTextOf(new PdfAndImageAccessReadModel(), "SourceImageStatus", "SourceImageStatusText", SourceImageStatus.Available));

    // 计算属性不可赋值：没有可写 setter。
    foreach ((Type type, string textProperty) in new[]
    {
      (typeof(RecognitionMatchReportReadModel), "ReportTypeText"),
      (typeof(RecognitionReportContextReadModel), "ReportTypeText"),
      (typeof(PdfAndImageAccessReadModel), "SourceImageStatusText")
    })
    {
      Assert.False(IsWritable(type.GetProperty(textProperty)!));
    }
  }

  /// <summary>
  /// 三个领域事件按设计声明：聚合标识取既有聚合常量，事件类型标识取类型名称，关键字段为强类型。
  /// </summary>
  /// <remarks>本用例只核对事件的声明面；事件登记与登记时机的断言由承接各写入口的票完成。</remarks>
  [Fact]
  public void Three_domain_events_declare_aggregate_identity_and_key_fields()
  {
    RecognitionMatchesReturnedEvent matchesReturned = new();
    RecognitionProcessingResultsSavedEvent processingResultsSaved = new();
    RecognitionReferencesRecordedEvent referencesRecorded = new();

    DomainEvent[] events = [matchesReturned, processingResultsSaved, referencesRecorded];
    foreach (DomainEvent domainEvent in events)
    {
      Assert.Equal(MedicalRecognitionReportConst.AggregateId, domainEvent.AggregateId);
      Assert.Equal(domainEvent.GetType().Name, domainEvent.EventType);
    }

    Assert.Equal(
      [
        "AggregateId", "EventType", "MatchCreatedTime", "MatchItemIds", "MatchedItemCount",
        "ReceiverBranchCode", "ReceiverHospitalCode", "ReceiverOrganizationCode", "RecognitionMatchRecordId"
      ],
      DeclaredPropertyNames(typeof(RecognitionMatchesReturnedEvent)));
    Assert.Equal(
      [
        "AggregateId", "EstimatedSavingAmount", "EventType", "NonAdoptionReasons", "RecognitionDeptId",
        "RecognitionDeptName", "RecognitionDoctorId", "RecognitionDoctorName", "RecognitionMatchItemIds",
        "RecognitionMatchRecordId", "RecognitionTime", "Results"
      ],
      DeclaredPropertyNames(typeof(RecognitionProcessingResultsSavedEvent)));
    Assert.Equal(
      ["AggregateId", "EventType", "ReferenceFacts", "ReferencedMatchItemIds"],
      DeclaredPropertyNames(typeof(RecognitionReferencesRecordedEvent)));

    // 命中项目数为整数、匹配项标识集合为强类型集合。
    Assert.Equal(typeof(int), typeof(RecognitionMatchesReturnedEvent).GetProperty("MatchedItemCount")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<Guid>),
      typeof(RecognitionMatchesReturnedEvent).GetProperty("MatchItemIds")!.PropertyType);

    // 处理结果事件逐项携带决定与不采纳原因；引用结果事件逐项携带实际引用时间与引用科室与医生。
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionResult>),
      typeof(RecognitionProcessingResultsSavedEvent).GetProperty("Results")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionNonAdoptionReason?>),
      typeof(RecognitionProcessingResultsSavedEvent).GetProperty("NonAdoptionReasons")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<RecognitionReferenceFact>),
      typeof(RecognitionReferencesRecordedEvent).GetProperty("ReferenceFacts")!.PropertyType);

    // 事件字段为可读写属性，供管理器在登记前逐项填写；事件类型名称与设计约定一致。
    Assert.Contains("RecognitionMatchRecordId", DeclaredPropertyNames(typeof(RecognitionMatchesReturnedEvent)));
    Assert.DoesNotContain("OrganizationCode", DeclaredPropertyNames(typeof(RecognitionMatchesReturnedEvent)));
  }

  /// <summary>
  /// 合法的匹配查询请求。
  /// </summary>
  /// <returns>字段齐全的匹配查询请求。</returns>
  private static RecognitionMatchQueryRequest ValidMatchQueryRequest() => new()
  {
    IdentityDocumentTypeCode = "01",
    IdentityDocumentNo = "N-0001",
    VisitType = VisitType.Outpatient,
    VisitSerialNo = "V-0001",
    ProposedItems = [new RecognitionMatchProposedItemRequest { ItemType = MedicalItemType.Laboratory, StandardProjectCode = "SP-1" }]
  };

  /// <summary>
  /// 合法的一条互认匹配项决定。
  /// </summary>
  /// <returns>字段齐全的采纳决定。</returns>
  private static RecognitionProcessingResultItemRequest ValidProcessingResultItem() => new()
  {
    RecognitionMatchItemId = Guid.NewGuid(),
    Result = RecognitionResult.Adopted
  };

  /// <summary>
  /// 合法的处理结果提交请求。
  /// </summary>
  /// <returns>字段齐全的处理结果请求。</returns>
  private static RecognitionProcessingResultSubmissionRequest ValidProcessingResultRequest() => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
    RecognitionDeptId = "D-1",
    RecognitionDeptName = "检验科",
    RecognitionDoctorId = "U-1",
    RecognitionDoctorName = "张三",
    ProcessingResults = [ValidProcessingResultItem()]
  };

  /// <summary>
  /// 合法的引用详情请求。
  /// </summary>
  /// <returns>字段齐全的引用详情请求。</returns>
  private static RecognitionCitationDetailRequest ValidCitationDetailRequest() => new()
  {
    IdentityDocumentTypeCode = "01",
    IdentityDocumentNo = "N-0001",
    VisitType = VisitType.Outpatient,
    VisitSerialNo = "V-0001"
  };

  /// <summary>
  /// 合法的一条实际引用项目。
  /// </summary>
  /// <returns>字段齐全的引用项目。</returns>
  private static RecognitionReferenceItemRequest ValidReferenceItem() => new()
  {
    RecognitionMatchItemId = Guid.NewGuid(),
    ReferencedTime = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
    ReferenceDeptId = "D-2",
    ReferenceDeptName = "心内科",
    ReferenceDoctorId = "U-2",
    ReferenceDoctorName = "李四"
  };

  /// <summary>
  /// 合法的引用结果提交请求。
  /// </summary>
  /// <returns>字段齐全的引用结果请求。</returns>
  private static RecognitionReferenceSubmissionRequest ValidReferenceRequest() =>
    new() { ReferenceItems = [ValidReferenceItem()] };

  /// <summary>
  /// 触发必填校验失败并返回异常。
  /// </summary>
  /// <param name="request">待校验的请求。</param>
  /// <returns>校验异常，其消息汇总了违规属性与原因。</returns>
  private static ValidationException RequiredError(object request) =>
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));

  /// <summary>
  /// 十个待重建的契约读模型。
  /// </summary>
  /// <returns>本票范围内的读模型类型清单。</returns>
  private static Type[] ContractReadModelTypes() =>
  [
    typeof(RecognitionMatchesResponseReadModel),
    typeof(RecognitionMatchReportReadModel),
    typeof(RecognitionMatchItemReadModel),
    typeof(RecognitionMatchLaboratoryResultReadModel),
    typeof(RecognitionExaminationSiteReadModel),
    typeof(RecognitionCitationDetailReadModel),
    typeof(RecognitionCitationItemReadModel),
    typeof(RecognitionCitationLaboratoryResultReadModel),
    typeof(RecognitionReportContextReadModel),
    typeof(PdfAndImageAccessReadModel)
  ];

  /// <summary>
  /// 取类型自身声明的公开实例属性。
  /// </summary>
  /// <param name="type">待检查的类型。</param>
  /// <returns>该类型自身声明的公开实例属性。</returns>
  private static PropertyInfo[] PublicInstanceProperties(Type type) =>
    type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

  /// <summary>
  /// 判断属性是否可写。
  /// </summary>
  /// <param name="property">待判断的属性。</param>
  /// <returns>存在公开 setter 且该 setter 不要求 <c>IsExternalInit</c> 修饰时返回 <see langword="true"/>。</returns>
  /// <remarks>
  /// C# 的 <c>init</c> 访问器编译为公开 setter，其返回值带有 <c>System.Runtime.CompilerServices.IsExternalInit</c> 修饰要求；
  /// 该修饰要求正是只读初始化与可写赋值在元数据里的唯一区别。
  /// </remarks>
  private static bool IsWritable(PropertyInfo property)
  {
    MethodInfo? setter = property.SetMethod;
    if (setter is null || !setter.IsPublic) return false;

    return !setter.ReturnParameter.GetRequiredCustomModifiers()
      .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
  }

  /// <summary>
  /// 取类型自身声明的公开实例属性名，按 Ordinal 升序。
  /// </summary>
  /// <param name="type">待检查的类型。</param>
  /// <param name="includeInherited">为真时把继承来的公开属性一并计入。</param>
  /// <returns>该类型的公开实例属性名。</returns>
  private static string[] DeclaredPropertyNames(Type type, bool includeInherited = false) =>
  [
    .. type.GetProperties(
        includeInherited
          ? BindingFlags.Public | BindingFlags.Instance
          : BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Select(property => property.Name)
      .OrderBy(name => name, StringComparer.Ordinal)
  ];

  /// <summary>
  /// 取某响应读模型沿属性类型与集合元素类型向下可达的全部公开实例属性名。
  /// </summary>
  /// <param name="rootType">响应读模型的根类型。</param>
  /// <returns>根类型及其可达契约类型自身声明的公开实例属性名，按 Ordinal 升序去重。</returns>
  /// <remarks>
  /// 两侧响应采用同一遍历口径，嵌套层的字段与本层字段一并参与判定；
  /// 已访问类型参与去重，类型互相引用时不会反复遍历。
  /// </remarks>
  private static string[] ResponsePropertyNames(Type rootType)
  {
    HashSet<Type> visitedTypes = [];
    Queue<Type> pendingTypes = new();
    pendingTypes.Enqueue(rootType);
    SortedSet<string> propertyNames = new(StringComparer.Ordinal);

    while (pendingTypes.Count > 0)
    {
      Type type = pendingTypes.Dequeue();
      if (!visitedTypes.Add(type)) continue;

      foreach (PropertyInfo property in PublicInstanceProperties(type))
      {
        propertyNames.Add(property.Name);

        Type? elementType = ContractElementType(property.PropertyType);
        if (elementType is not null) pendingTypes.Enqueue(elementType);
      }
    }

    return [.. propertyNames];
  }

  /// <summary>
  /// 取响应读模型属性的元素类型。
  /// </summary>
  /// <param name="propertyType">属性的声明类型。</param>
  /// <returns>属于本项目契约类型的元素类型；标量、框架类型与系统类型返回 <see langword="null"/>。</returns>
  /// <remarks>集合与可空值类型按其单一泛型实参向下取元素类型，只跟进本项目的契约类型。</remarks>
  private static Type? ContractElementType(Type propertyType)
  {
    Type candidate = propertyType;

    if (candidate.IsGenericType && candidate.GetGenericArguments().Length == 1)
      candidate = candidate.GetGenericArguments()[0];

    return candidate.Namespace?.StartsWith("Dy.MedicalRecognition.", StringComparison.Ordinal) == true
      ? candidate
      : null;
  }

  /// <summary>
  /// 读取属性的可空性标注。
  /// </summary>
  /// <param name="type">属性所属的读模型类型。</param>
  /// <param name="propertyName">属性名；必须是该类型自身声明的公开实例属性。</param>
  /// <returns>该属性读取方向的可空性标注。</returns>
  /// <remarks>可空引用类型不改变运行时属性类型，只能按编译期可空性元数据判定。</remarks>
  private static NullabilityState ReadNullability(Type type, string propertyName)
  {
    PropertyInfo property = type.GetProperty(
      propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
    NullabilityInfo info = new NullabilityInfoContext().Create(property);
    return info.ReadState;
  }

  /// <summary>
  /// 取请求类型的业务字段名，排除 <c>IPropertyChangedAware</c> 源生成器补入的变更跟踪成员。
  /// </summary>
  /// <param name="type">待检查的请求类型。</param>
  /// <returns>该请求类型的业务字段名，按 Ordinal 升序。</returns>
  /// <remarks>变更跟踪成员由特性生成、不是调用方能提交的业务字段，与阶段 2 至阶段 4 的同一过滤口径一致。</remarks>
  private static string[] BusinessFieldNames(Type type) =>
  [
    .. DeclaredPropertyNames(type).Where(name => name is not ("ChangedProperties" or "HasPropertyChanged"))
  ];

  /// <summary>
  /// 在承载枚举的读模型上按枚举声明解析中文，并核对文本属性不可赋值。
  /// </summary>
  /// <param name="model">读模型实例，其枚举属性按反射设置。</param>
  /// <param name="enumPropertyName">枚举属性名；属性不存在时以断言失败报告。</param>
  /// <param name="textPropertyName">枚举文本计算属性名；属性不存在时以断言失败报告。</param>
  /// <param name="enumValue">要设置并解析的枚举取值。</param>
  /// <returns>解析出的中文文本。</returns>
  private static string EnumTextOf(object model, string enumPropertyName, string textPropertyName, object enumValue)
  {
    Type type = model.GetType();

    PropertyInfo? enumProperty = type.GetProperty(enumPropertyName);
    Assert.NotNull(enumProperty);
    enumProperty!.SetValue(model, enumValue);

    PropertyInfo? textProperty = type.GetProperty(textPropertyName);
    Assert.NotNull(textProperty);
    Assert.False(IsWritable(textProperty!));
    Assert.Equal(typeof(string), textProperty!.PropertyType);

    string text = (string)textProperty.GetValue(model)!;
    Assert.False(string.IsNullOrWhiteSpace(text));
    return text;
  }
}

/// <summary>
/// 无类型属性判据的变异探针：声明一个 <see cref="object"/> 类型的公开实例属性。
/// </summary>
internal sealed class Stage5UntypedProbe
{
  /// <summary>无类型属性，仅用于证明无类型判据能够命中。</summary>
  public object Payload { get; set; } = new();
}

/// <summary>
/// 只读属性判据的变异探针：声明一个带公开 setter 的公开实例属性。
/// </summary>
internal sealed class Stage5WritableProbe
{
  /// <summary>可写属性，仅用于证明只读判据能够命中。</summary>
  public string Value { get; set; } = string.Empty;
}
