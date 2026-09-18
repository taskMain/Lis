using System.Reflection;
using System.Text.Json;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 4 报告公开类型与分页约定的形状校验（矩阵 V84 的读模型与分页约定面、V73 的枚举文本面）。
/// </summary>
/// <remarks>
/// 只读取公开属性的声明与可空性标注，不访问数据库、不构造应用服务实例、不写入任何数据。
/// 判定目标是"对外公布的字段集合与类型"：字段增删、改类型、改可空性都会让本用例失败，
/// 使页面与生成端不会在不知情的情况下收到形状变化。
/// </remarks>
public sealed class Stage4ContractTests
{
  /// <summary>分页请求对象只承载页码与页容量，且两者都是整数；两个列表请求以内嵌方式承载它。</summary>
  [Fact]
  public void Page_request_exposes_page_index_and_page_size_only()
  {
    Assert.Equal(["PageIndex", "PageSize"], DeclaredPropertyNames(typeof(PageRequestDto)));
    Assert.Equal(typeof(int), typeof(PageRequestDto).GetProperty("PageIndex")!.PropertyType);
    Assert.Equal(typeof(int), typeof(PageRequestDto).GetProperty("PageSize")!.PropertyType);

    Assert.Equal(typeof(PageRequestDto), typeof(MedicalReportListQueryRequest).GetProperty("Page")!.PropertyType);
    Assert.Equal(typeof(PageRequestDto), typeof(ReportListQueryRequest).GetProperty("Page")!.PropertyType);
    Assert.Equal(typeof(PageRequestDto), typeof(BranchReportListQueryRequest).GetProperty("Page")!.PropertyType);
    // 页码与页容量只属于内嵌的分页对象，列表请求的顶层没有这两个字段。
    Assert.Null(typeof(ReportListQueryRequest).GetProperty("PageIndex"));
    Assert.Null(typeof(ReportListQueryRequest).GetProperty("PageSize"));
    Assert.Null(typeof(BranchReportListQueryRequest).GetProperty("PageIndex"));
    Assert.Null(typeof(BranchReportListQueryRequest).GetProperty("PageSize"));
  }

  /// <summary>列表请求体把分页放在内嵌 page 对象里，页码与页容量不出现在顶层。</summary>
  /// <remarks>
  /// 判据来自 .agents/instructions/frontend-api-client.md 第 30 行与设计 V84 行：服务端分页请求为业务筛选加
  /// page: { pageIndex, pageSize }。请求体形状以序列化结果为准，端点收到的就是该文本；
  /// 序列化按宿主的 Web 默认口径（camelCase 属性名），否则字段名与真实请求体不一致。
  /// </remarks>
  [Fact]
  public void List_request_body_nests_paging_under_page()
  {
    JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
    string body = JsonSerializer.Serialize(new ReportListQueryRequest
    {
      OrganizationCode = "01",
      HospitalCode = "0101",
      BranchCode = "0101001",
      Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
    }, options);

    using JsonDocument document = JsonDocument.Parse(body);
    JsonElement root = document.RootElement;

    // 顶层不得出现页码与页容量：它们只属于内嵌的分页对象。
    Assert.False(root.TryGetProperty("pageIndex", out _));
    Assert.False(root.TryGetProperty("pageSize", out _));

    JsonElement page = root.GetProperty("page");
    Assert.Equal(1, page.GetProperty("pageIndex").GetInt32());
    Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
  }

  /// <summary>分页信息承载页码、页容量与长整型总数，不出现派生的分页字段。</summary>
  [Fact]
  public void Page_info_exposes_index_size_and_long_total_count()
  {
    Assert.Equal(["PageIndex", "PageSize", "TotalCount"], DeclaredPropertyNames(typeof(PageInfoDto)));
    Assert.Equal(typeof(int), typeof(PageInfoDto).GetProperty("PageIndex")!.PropertyType);
    Assert.Equal(typeof(int), typeof(PageInfoDto).GetProperty("PageSize")!.PropertyType);
    // 总数必须是长整型：报告总量可能超过 32 位整数范围。
    Assert.Equal(typeof(long), typeof(PageInfoDto).GetProperty("TotalCount")!.PropertyType);
  }

  /// <summary>分页响应由当页数据与分页信息两段组成。</summary>
  [Fact]
  public void Page_result_wraps_items_and_page_info()
  {
    Assert.Equal(["Items", "Page"], DeclaredPropertyNames(typeof(PageResultDto<MedicalReportListReadModel>)));
    Assert.Equal(typeof(IReadOnlyList<MedicalReportListReadModel>), typeof(PageResultDto<MedicalReportListReadModel>).GetProperty("Items")!.PropertyType);
    Assert.Equal(typeof(PageInfoDto), typeof(PageResultDto<MedicalReportListReadModel>).GetProperty("Page")!.PropertyType);
  }

  /// <summary>
  /// 分页约定中不得出现 <c>HasNext</c>、<c>Pagination</c>、<c>SkipCount</c>、<c>PageCount</c> 这四个派生或框架内部字段名。
  /// </summary>
  /// <remarks>
  /// 这四个名字分别暗示客户端推进分页、把框架分页对象直接对外、暴露窗口起点与页数，都与服务端分页的约定不符；
  /// 它们一旦出现在对外类型上，生成端与页面就会按另一套分页模型实现。
  /// </remarks>
  [Fact]
  public void Paging_contract_does_not_expose_derived_or_internal_paging_fields()
  {
    Type[] pagingTypes = [typeof(PageRequestDto), typeof(PageInfoDto), typeof(PageResultDto<MedicalReportListReadModel>)];
    string[] banned = ["HasNext", "Pagination", "SkipCount", "PageCount"];

    foreach (Type type in pagingTypes)
    {
      foreach (string propertyName in banned)
        Assert.Null(type.GetProperty(propertyName));
    }

    // 变异证据：这些禁用字段确实不在分页信息类型上，说明上面的逐项判空不是恒真。
    string[] pageInfoProperties = DeclaredPropertyNames(typeof(PageInfoDto));
    Assert.Contains("HasNext", banned);
    foreach (string propertyName in banned) Assert.DoesNotContain(propertyName, pageInfoProperties);
  }

  /// <summary>
  /// 平台管理员入口的列表请求承载三级范围与八类筛选条件，且都是可选条件。
  /// </summary>
  [Fact]
  public void Platform_list_request_exposes_three_level_scope_and_filters()
  {
    Type type = typeof(ReportListQueryRequest);
    Assert.Equal(
      ["BranchCode", "HospitalCode", "IdentityDocumentNo", "OrganizationCode", "Page", "PatientName", "ReportDateFrom", "ReportDateTo", "ReportNo", "ReportType"],
      DeclaredPropertyNames(type, includeInherited: true));

    Assert.Equal(typeof(string), type.GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("HospitalCode")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("BranchCode")!.PropertyType);
    Assert.Equal(typeof(DateOnly?), type.GetProperty("ReportDateFrom")!.PropertyType);
    Assert.Equal(typeof(DateOnly?), type.GetProperty("ReportDateTo")!.PropertyType);
    Assert.Equal(typeof(MedicalReportType?), type.GetProperty("ReportType")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ReportNo")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("IdentityDocumentNo")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("PatientName")!.PropertyType);
  }

  /// <summary>
  /// 医院管理员入口的列表请求不提交组织与医院，只提交院区与筛选条件。
  /// </summary>
  /// <remarks>组织与医院由服务端从可信上下文注入，请求侧不存在这两个字段，提交即越权覆盖。</remarks>
  [Fact]
  public void Branch_list_request_omits_organization_and_hospital()
  {
    Type type = typeof(BranchReportListQueryRequest);
    Assert.Null(type.GetProperty("OrganizationCode"));
    Assert.Null(type.GetProperty("HospitalCode"));
    Assert.Equal(
      ["BranchCode", "IdentityDocumentNo", "Page", "PatientName", "ReportDateFrom", "ReportDateTo", "ReportNo", "ReportType"],
      DeclaredPropertyNames(type, includeInherited: true));

    // 院区是请求侧必填：缺失或纯空白由公共请求校验拒绝。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new BranchReportListQueryRequest()));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new BranchReportListQueryRequest { BranchCode = "  " }));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 1, PageSize = 20 } })));
  }

  /// <summary>分页窗口的取值域在请求校验处生效：页码小于 1 或页容量超出 1 到 200 都被拒绝。</summary>
  /// <remarks>页码与页容量内嵌在分页对象里，校验必须递归进入该对象才能命中这两个声明。</remarks>
  [Fact]
  public void Page_window_range_is_declared_on_the_request()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(
        new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 0, PageSize = 20 } }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(
        new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 1, PageSize = 0 } }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(
        new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 1, PageSize = 201 } }));

    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 1, PageSize = 200 } })));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new BranchReportListQueryRequest { BranchCode = "BRH-A", Page = new PageRequestDto { PageIndex = 1, PageSize = 1 } })));
  }

  /// <summary>
  /// 列表读模型的服务端字段集合：三级编码与回填名称、两个患者字段、当前版本序号、两个枚举值与枚举文本。
  /// </summary>
  [Fact]
  public void List_read_model_exposes_names_patient_fields_version_sequence_and_enum_texts()
  {
    Type type = typeof(MedicalReportListReadModel);
    Assert.Equal(
      [
        "BranchCode", "BranchName", "CurrentVersionSequence", "HospitalCode", "HospitalName", "IdentityDocumentNo",
        "OrganizationCode", "OrganizationName", "PatientName", "ReportId", "ReportNo", "ReportTime", "ReportType",
        "ReportTypeText", "Status", "StatusText"
      ],
      DeclaredPropertyNames(type));

    Assert.Equal(typeof(Guid), type.GetProperty("ReportId")!.PropertyType);
    Assert.Equal(typeof(MedicalReportType), type.GetProperty("ReportType")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ReportTypeText")!.PropertyType);
    Assert.Equal(typeof(MedicalReportLifecycleStatus), type.GetProperty("Status")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("StatusText")!.PropertyType);
    Assert.Equal(typeof(int), type.GetProperty("CurrentVersionSequence")!.PropertyType);
    // 报告时间恒有值：取自报告主体上的检索列，因此不是可空类型。
    Assert.Equal(typeof(DateTime), type.GetProperty("ReportTime")!.PropertyType);
    Assert.Equal(NullabilityState.NotNull, ReadNullability(type, "ReportTime"));
  }

  /// <summary>
  /// 版本列表读模型同时给出「是否当前有效版本」与「是否已被后续版本替代」，责任人员为结构化字段。
  /// </summary>
  [Fact]
  public void Version_list_read_model_exposes_two_version_flags_and_structured_responsible_staff()
  {
    Type type = typeof(MedicalReportVersionListReadModel);
    Assert.Equal(
      [
        "DetailInspectors", "ExaminerName", "InspectorName", "IsCurrentVersion", "IsSuperseded", "PdfFileName",
        "PlatformReceivedTime", "ReportDoctorName", "ReportStatus", "ReportStatusText", "ReportVersionId",
        "ReviewDoctorName", "SourceModifiedTime", "SourceReportRemark", "VersionSequence"
      ],
      DeclaredPropertyNames(type));

    Assert.Equal(typeof(bool), type.GetProperty("IsCurrentVersion")!.PropertyType);
    Assert.Equal(typeof(bool), type.GetProperty("IsSuperseded")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ReportDoctorName")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ReviewDoctorName")!.PropertyType);
    // 检查侧人员字段在检验版本上没有业务值，因此是可空字符串。
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "ExaminerName"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "InspectorName"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "DetailInspectors"));
    // 取消字符串拼接后的历史字段不得残留。
    Assert.Null(type.GetProperty("ResponsibleStaff"));
  }

  /// <summary>
  /// 版本文件信息只承载文件名，不承载文件标识、物理路径或对外下载地址。
  /// </summary>
  [Fact]
  public void Version_file_read_model_exposes_download_name_only()
  {
    Type type = typeof(MedicalReportVersionFileReadModel);
    Assert.Equal(["FileName"], DeclaredPropertyNames(type));

    string[] forbidden = ["FileId", "PdfFileId", "FilePath", "PdfDownloadUrl", "DownloadUrl", "Url"];
    foreach (string propertyName in forbidden) Assert.Null(type.GetProperty(propertyName));
  }

  /// <summary>
  /// 版本详情读模型承载报告身份、版本信息、结构化人员、内容与文件信息。
  /// </summary>
  [Fact]
  public void Version_detail_read_model_exposes_identity_content_and_file()
  {
    Type type = typeof(MedicalReportVersionDetailQueryReadModel);
    Assert.Equal(
      [
        "Content", "DetailInspectors", "ExaminerName", "File", "InspectorName", "PlatformReceivedTime", "ReportDoctorName",
        "ReportNo", "ReportType", "ReportTypeText", "ReportVersionId", "ReviewDoctorName", "SourceModifiedTime",
        "SourceReportRemark", "VersionSequence"
      ],
      DeclaredPropertyNames(type));

    Assert.Equal(typeof(MedicalReportVersionContentReadModel), type.GetProperty("Content")!.PropertyType);
    Assert.Equal(typeof(MedicalReportVersionFileReadModel), type.GetProperty("File")!.PropertyType);
    Assert.Equal(typeof(MedicalReportVersionContentReadModel), typeof(MedicalReportVersionContentReadModel));
  }

  /// <summary>
  /// 版本内容读模型由公共段与检验、检查两段构成，两段按报告类型二选一。
  /// </summary>
  [Fact]
  public void Version_content_read_model_splits_common_laboratory_and_examination()
  {
    Type type = typeof(MedicalReportVersionContentReadModel);
    Assert.Equal(["Common", "ExaminationContent", "LaboratoryContent"], DeclaredPropertyNames(type));

    Assert.Equal(typeof(ReportVersionCommonReadModel), type.GetProperty("Common")!.PropertyType);
    Assert.Equal(typeof(LaboratoryReportContentViewReadModel), type.GetProperty("LaboratoryContent")!.PropertyType);
    Assert.Equal(typeof(ExaminationReportContentViewReadModel), type.GetProperty("ExaminationContent")!.PropertyType);
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "LaboratoryContent"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "ExaminationContent"));
  }

  /// <summary>
  /// 公共信息读模型承载患者身份、就诊、四组科室人员与三类业务时间，且提供就诊类型文本。
  /// </summary>
  [Fact]
  public void Common_read_model_exposes_patient_visit_staff_and_times()
  {
    Type type = typeof(ReportVersionCommonReadModel);
    string[] expected =
    [
      "AgeAtReport", "ApplicationDeptId", "ApplicationDeptName", "ApplicationDoctorId", "ApplicationDoctorName",
      "ApplicationTime", "BedNo", "ExecutionDeptId", "ExecutionDeptName", "IdentityDocumentNo",
      "IdentityDocumentTypeCode", "InpatientNo", "PatientBirthDate", "PatientGenderCode", "PatientName",
      "PatientPhoneNumber", "ReportDeptId", "ReportDeptName", "ReportDoctorId", "ReportDoctorName", "ReportTime",
      "ReviewDoctorId", "ReviewDoctorName", "ReviewTime", "RoomName", "SourceConfidentialFlag", "SourceReportName",
      "VisitSerialNo", "VisitType", "VisitTypeText", "WardName"
    ];
    Assert.Equal(expected, DeclaredPropertyNames(type));

    Assert.Equal(typeof(VisitType), type.GetProperty("VisitType")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("VisitTypeText")!.PropertyType);
    // 联系电话返回时已由服务端脱敏，来源未提供时为空。
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "PatientPhoneNumber"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "ReviewTime"));
  }

  /// <summary>
  /// 检验内容读模型的枚举字段同时提供取值与文本，量值类字段按文本承载。
  /// </summary>
  [Fact]
  public void Laboratory_content_read_model_exposes_typed_enums_and_texts()
  {
    Type resultType = typeof(LaboratoryResultItemReadModel);
    Assert.Equal(typeof(LaboratoryResultType), resultType.GetProperty("ResultType")!.PropertyType);
    Assert.Equal(typeof(string), resultType.GetProperty("ResultTypeText")!.PropertyType);
    Assert.Equal(typeof(LaboratoryAbnormalFlag?), resultType.GetProperty("AbnormalFlag")!.PropertyType);
    Assert.Equal(typeof(string), resultType.GetProperty("AbnormalFlagText")!.PropertyType);
    Assert.Equal(NullabilityState.Nullable, ReadNullability(resultType, "AbnormalFlagText"));

    // 药敏量值按来源文本整串保存与返回，不做数值解析。
    Type susceptibilityType = typeof(LaboratoryAntimicrobialSusceptibilityReadModel);
    foreach (string propertyName in new[] { "DiskContent", "MicValue", "InhibitionZoneDiameter", "ReferenceValue" })
      Assert.Equal(typeof(string), susceptibilityType.GetProperty(propertyName)!.PropertyType);

    Type contentType = typeof(LaboratoryReportContentViewReadModel);
    Assert.Equal(typeof(IReadOnlyList<LaboratoryResultItemReadModel>), contentType.GetProperty("Results")!.PropertyType);
    Assert.Equal(typeof(IReadOnlyList<LaboratoryBacteriaResultReadModel>), contentType.GetProperty("BacteriaResults")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<LaboratoryAntimicrobialSusceptibilityReadModel>),
      typeof(LaboratoryBacteriaResultReadModel).GetProperty("Susceptibilities")!.PropertyType);
  }

  /// <summary>
  /// 检查内容读模型的影像状态同时提供取值与文本，检查项目带部位集合。
  /// </summary>
  [Fact]
  public void Examination_content_read_model_exposes_image_status_and_site_collection()
  {
    Type type = typeof(ExaminationReportContentViewReadModel);
    Assert.Equal(typeof(SourceImageStatus), type.GetProperty("SourceImageStatus")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("SourceImageStatusText")!.PropertyType);
    Assert.Equal(typeof(IReadOnlyList<ExaminationItemReadModel>), type.GetProperty("Items")!.PropertyType);
    Assert.Equal(
      typeof(IReadOnlyList<ExaminationSiteReadModel>),
      typeof(ExaminationItemReadModel).GetProperty("Sites")!.PropertyType);
  }

  /// <summary>
  /// 报告版本 PDF 的查询与入参约定：请求用报告标识与报告版本标识两个入参定位，返回只承载文件名。
  /// </summary>
  [Fact]
  public void Pdf_query_uses_report_and_version_identifiers_and_returns_file_name_only()
  {
    Assert.Equal(["ReportId", "ReportVersionId"], DeclaredPropertyNames(typeof(MedicalReportVersionPdfQueryRequest)));
    Assert.Equal(["ReportId", "ReportVersionId"], DeclaredPropertyNames(typeof(MedicalReportVersionDetailQueryRequest)));
    Assert.Equal(["ReportId"], DeclaredPropertyNames(typeof(MedicalReportVersionListQueryRequest)));
    Assert.Equal(["FileName"], DeclaredPropertyNames(typeof(MedicalReportVersionPdfQueryReadModel)));

    // 空 Guid 一律拒绝：两个入参都是定位条件，缺失无法定位版本。
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new MedicalReportVersionPdfQueryRequest { ReportId = Guid.Empty, ReportVersionId = Guid.NewGuid() }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new MedicalReportVersionPdfQueryRequest { ReportId = Guid.NewGuid(), ReportVersionId = Guid.Empty }));
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new MedicalReportVersionPdfQueryRequest { ReportId = Guid.NewGuid(), ReportVersionId = Guid.NewGuid() })));
  }

  /// <summary>
  /// 窗口校验类型与当页切片类型属于应用层内部投影，不出现在对外公开类型所在程序集的公开类型清单里。
  /// </summary>
  /// <remarks>
  /// 这两个类型一旦公开，生成端会把窗口起点与切片形状写进客户端约定，服务端分页的数值域校验随之失去唯一归属。
  /// </remarks>
  [Fact]
  public void Page_window_and_slice_types_stay_internal()
  {
    Assembly contracts = typeof(MedicalReportListReadModel).Assembly;
    Assert.DoesNotContain(
      contracts.GetExportedTypes(),
      type => type.Name is "PageQueryWindow" or "PageSlice");

    Assembly application = typeof(Dy.MedicalRecognition.Application.Queries.MedicalRecognitionReportQueryAppService).Assembly;
    Type? window = application.GetType("Dy.MedicalRecognition.Application.Queries.PageQueryWindow");
    Assert.NotNull(window);
    Assert.False(window!.IsPublic);
    Assert.False(window.IsNestedPublic);

    Type? slice = application.GetType("Dy.MedicalRecognition.Application.Queries.PageSlice`1");
    Assert.NotNull(slice);
    Assert.False(slice!.IsPublic);
    Assert.False(slice.IsNestedPublic);
  }

  /// <summary>
  /// 提交入口的请求只承载文档原文、文件键与下载名；作废请求只承载报告单号、作废时间与原因。
  /// </summary>
  [Fact]
  public void Submission_and_void_requests_carry_no_file_bytes_or_trusted_scope()
  {
    Assert.Equal(["PdfFileId", "PdfFileName", "ReportJson"], DeclaredPropertyNames(typeof(CompleteReportSubmissionRequest)));
    Assert.Equal(["ReportNo", "VoidReason", "VoidedTime"], DeclaredPropertyNames(typeof(MedicalReportVoidRequest)));

    foreach (Type type in new[] { typeof(CompleteReportSubmissionRequest), typeof(MedicalReportVoidRequest) })
    {
      foreach (string propertyName in new[] { "OrganizationCode", "HospitalCode", "BranchCode", "OperId", "FileContent", "Content" })
        Assert.Null(type.GetProperty(propertyName));
    }
  }

  /// <summary>
  /// V82 的契约面：报告 PDF 存储接口只暴露保存、按键读取与按键删除三个操作，不返回文件字节数组。
  /// </summary>
  /// <remarks>
  /// 接口方法集合被逐项冻结：新增第四个操作、把读取结果改成字节数组或暴露物理路径都会让本用例失败。
  /// </remarks>
  [Fact]
  public void Pdf_file_store_contract_exposes_three_operations_only()
  {
    Assert.Equal(
      ["DeleteAsync", "OpenReadAsync", "SaveAsync"],
      [.. typeof(IReportPdfFileStore).GetMethods().Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal)]);

    MethodInfo save = typeof(IReportPdfFileStore).GetMethod("SaveAsync")!;
    Assert.Equal(typeof(Task<string>), save.ReturnType);
    Assert.Equal(typeof(Stream), Assert.Single(save.GetParameters()).ParameterType);

    MethodInfo openRead = typeof(IReportPdfFileStore).GetMethod("OpenReadAsync")!;
    Assert.Equal(typeof(Task<Stream>), openRead.ReturnType);

    MethodInfo delete = typeof(IReportPdfFileStore).GetMethod("DeleteAsync")!;
    Assert.Equal(typeof(Task), delete.ReturnType);

    // 不出现按字节数组读写或暴露物理路径的操作。
    Assert.DoesNotContain(
      typeof(IReportPdfFileStore).GetMethods(),
      method => method.ReturnType == typeof(byte[]) || method.GetParameters().Any(parameter => parameter.ParameterType == typeof(byte[])));
  }

  /// <summary>
  /// 取类型自身声明的公开实例属性名，按 Ordinal 升序，排除继承来的框架成员。
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

  /// <summary>读取属性的可空性标注，用于校验对外声明的可空字段。</summary>
  /// <param name="type">属性所属类型。</param>
  /// <param name="propertyName">属性名。</param>
  /// <returns>该属性读取方向的可空性标注。</returns>
  private static NullabilityState ReadNullability(Type type, string propertyName)
  {
    PropertyInfo property = type.GetProperty(propertyName)!;
    return new NullabilityInfoContext().Create(property).ReadState;
  }
}
