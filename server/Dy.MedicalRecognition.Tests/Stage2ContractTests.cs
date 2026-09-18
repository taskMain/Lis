using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 2 互认项目配置公共契约的形状校验（矩阵 V21）：创建请求、查询请求和互认配置读模型的
/// 字段集合、可空性与枚举类型必须与阶段 2 后端设计一致，防止可信组织、内部目录 ID 或派生状态从契约泄漏。
/// </summary>
/// <remarks>只读取公开属性的声明，不访问数据库、不构造应用服务实例、不写入任何数据。</remarks>
public sealed class Stage2ContractTests
{
  /// <summary>创建请求只提交标准项目编码和可互认时间，组织编码与内部标准项目 ID 不得进入公共请求。</summary>
  [Fact]
  public void Create_request_exposes_only_project_code_and_duration()
  {
    // ChangedProperties 与 HasPropertyChanged 由 [IPropertyChangedAware] 生成器补充用于变更追踪，不是业务契约字段。
    string[] expected = ["ChangedProperties", "HasPropertyChanged", "RecognitionDurationDays", "StandardProjectCode"];

    Assert.Equal(expected, DeclaredPropertyNames(typeof(CreateMutualRecognitionItemRequest)));
    Assert.Null(typeof(CreateMutualRecognitionItemRequest).GetProperty("OrganizationCode"));
    Assert.Null(typeof(CreateMutualRecognitionItemRequest).GetProperty("StandardItemId"));
  }

  /// <summary>源生成器仍按剩余字段生成创建请求到领域命令的映射，可信组织不在其中、由应用层在映射后补入。</summary>
  [Fact]
  public void Create_request_still_maps_to_command_without_organization()
  {
    CreateMutualRecognitionItemRequest request = new() { StandardProjectCode = "PROBE-CODE", RecognitionDurationDays = 30 };

    Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands.CreateMutualRecognitionItemCommand command =
      Dy.MedicalRecognition.Application.CreateMutualRecognitionItemRequestAndCreateMutualRecognitionItemCommandMaps
        .MapToCreateMutualRecognitionItemCommand(request);

    Assert.Equal("PROBE-CODE", command.StandardProjectCode);
    Assert.Equal(30, command.RecognitionDurationDays);
    Assert.Null(command.OrganizationCode);
  }

  /// <summary>
  /// 互认配置读模型迁入查询契约目录，字段、可空性与枚举类型与设计一致，且不返回操作字段和派生状态；
  /// 2026-09-16 按负责人裁定与 <c>Dy.LisCenter</c> 同口径补入服务端交付的枚举中文文本字段。
  /// </summary>
  [Fact]
  public void Read_model_matches_the_published_field_shape()
  {
    Type type = typeof(RecognitionProjectConfigurationReadModel);
    Assert.Equal("Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition", type.Namespace);

    string[] expected =
    [
      "CategoryName", "ConfigurationId", "ConfigurationStatus", "ConfigurationStatusText", "GroupName", "ItemType",
      "ItemTypeText", "RecognitionDurationDays", "StandardItemName", "StandardProjectCode", "UnavailableReason"
    ];
    Assert.Equal(expected, DeclaredPropertyNames(type));

    Assert.Equal(typeof(Guid), type.GetProperty("ConfigurationId")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("StandardProjectCode")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("StandardItemName")!.PropertyType);
    Assert.Equal(typeof(MedicalItemType), type.GetProperty("ItemType")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("CategoryName")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("GroupName")!.PropertyType);
    Assert.Equal(typeof(int), type.GetProperty("RecognitionDurationDays")!.PropertyType);
    Assert.Equal(typeof(ConfigurationStatus), type.GetProperty("ConfigurationStatus")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ItemTypeText")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("ConfigurationStatusText")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("UnavailableReason")!.PropertyType);

    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "UnavailableReason"));
    // 项目类型取自无 CHECK 约束的列，中文文本允许为 null（页面按"未知类型"安全展示）；其余字段由服务端声明保证非空。
    // 注意 C# 形状与发布契约形状不同：两个 get-only 计算属性在 OpenAPI/生成端一律为可空（平台行为，与 LisCenter 同构），
    // 本用例校验的是 C# 契约，生成端可空性由 Stage2EnumMetadataQueryTests.Generated_client_exposes_enum_metadata_api_and_text_fields 锁定。
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "ItemTypeText"));
    foreach (string required in expected.Where(name => name is not "UnavailableReason" and not "ItemTypeText"))
    {
      Assert.Equal(NullabilityState.NotNull, ReadNullability(type, required));
    }

    string[] removed = ["IsStandardCatalogValid", "IsAvailableForNewMatch", "CreationTime", "Creator", "LastModifiedTime", "LastModifier"];
    Assert.DoesNotContain(type.GetProperties(), property => removed.Contains(property.Name));
  }

  /// <summary>启停请求的配置标识缺失或为空 Guid 时由请求校验拒绝，避免无标识的启停动作穿透到领域层。</summary>
  [Fact]
  public void Enable_and_disable_requests_reject_empty_configuration_id()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new EnableMutualRecognitionItemRequest { Id = Guid.Empty }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new DisableMutualRecognitionItemRequest { Id = Guid.Empty }));

    Assert.Null(Record.Exception(
      () => MedicalRecognitionRequestValidator.Validate(new EnableMutualRecognitionItemRequest { Id = Guid.NewGuid() })));
    Assert.Null(Record.Exception(
      () => MedicalRecognitionRequestValidator.Validate(new DisableMutualRecognitionItemRequest { Id = Guid.NewGuid() })));
  }

  /// <summary>创建请求的标准项目编码为纯空白时由请求校验拒绝，不再穿透为“标准项目不存在”的业务拒绝。</summary>
  [Fact]
  public void Create_request_rejects_blank_standard_project_code()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(
        new CreateMutualRecognitionItemRequest { StandardProjectCode = "   ", RecognitionDurationDays = 1 }));

    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new CreateMutualRecognitionItemRequest { StandardProjectCode = "P1", RecognitionDurationDays = 1 })));
  }

  /// <summary>查询请求保留必填组织编码和两个可选筛选条件，且不新增名称、类型、分类、分组筛选或分页字段。</summary>
  [Fact]
  public void List_query_request_fields_and_optionality_match_design()
  {
    Type type = typeof(RecognitionProjectConfigurationListQueryRequest);

    string[] expected = ["ConfigurationStatus", "OrganizationCode", "StandardProjectCode"];
    Assert.Equal(expected, DeclaredPropertyNames(type));

    Assert.Equal(typeof(string), type.GetProperty("OrganizationCode")!.PropertyType);
    Assert.Equal(typeof(string), type.GetProperty("StandardProjectCode")!.PropertyType);
    Assert.Equal(typeof(ConfigurationStatus?), type.GetProperty("ConfigurationStatus")!.PropertyType);

    Assert.Equal(NullabilityState.NotNull, ReadNullability(type, "OrganizationCode"));
    Assert.Equal(NullabilityState.Nullable, ReadNullability(type, "StandardProjectCode"));
  }

  /// <summary>查询请求的组织编码缺失、空串或纯空白都在进入查询前被拒绝，合法组织编码通过。</summary>
  [Fact]
  public void List_query_request_rejects_missing_or_blank_organization_code()
  {
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new RecognitionProjectConfigurationListQueryRequest()));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new RecognitionProjectConfigurationListQueryRequest { OrganizationCode = null! }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new RecognitionProjectConfigurationListQueryRequest { OrganizationCode = "   " }));
    Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
      () => MedicalRecognitionRequestValidator.Validate(new RecognitionProjectConfigurationListQueryRequest { OrganizationCode = "ORG-A", StandardProjectCode = "" }));

    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(
      new RecognitionProjectConfigurationListQueryRequest { OrganizationCode = "ORG-A", StandardProjectCode = null, ConfigurationStatus = ConfigurationStatus.Enabled })));
  }

  /// <summary>
  /// 取契约类型自身声明的公开实例属性名，按 Ordinal 排序以便断言稳定；排除继承来的框架成员，
  /// 防止基类属性混入公共契约字段集合。
  /// </summary>
  /// <param name="type">待检查的公共契约类型。</param>
  /// <returns>该类型自身声明的公开实例属性名，按 Ordinal 升序排列。</returns>
  private static string[] DeclaredPropertyNames(Type type)
  {
    return [.. DeclaredProperties(type)
      .Select(property => property.Name)
      .OrderBy(name => name, StringComparer.Ordinal)];
  }

  /// <summary>读取属性的可空性标注，用于校验契约声明的可空字段。</summary>
  /// <param name="type">属性所属的公共契约类型。</param>
  /// <param name="propertyName">属性名；必须是该类型自身声明的公开属性。</param>
  /// <returns>该属性读取方向的可空性标注。</returns>
  private static NullabilityState ReadNullability(Type type, string propertyName)
  {
    PropertyInfo property = type.GetProperty(propertyName, DeclaredPublicInstance)!;
    NullabilityInfo info = new NullabilityInfoContext().Create(property);
    return info.ReadState;
  }

  /// <summary>枚举类型自身声明的公开实例属性，避免把基类属性混入契约集合。</summary>
  /// <param name="type">待检查的类型。</param>
  /// <returns>该类型声明的公开实例属性。</returns>
  private static IEnumerable<PropertyInfo> DeclaredProperties(Type type) => type.GetProperties(DeclaredPublicInstance);

  /// <summary>反射绑定标记：只取类型自身声明的公开实例成员，排除继承来的框架成员。</summary>
  private static readonly BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
}
