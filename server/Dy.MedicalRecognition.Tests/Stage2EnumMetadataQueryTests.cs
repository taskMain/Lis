using Dy.Core.Extensions.Models;
using Dy.MedicalRecognition.Application;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Application.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 枚举中文来源的契约治理：中文只在服务端枚举声明处定义，对外经枚举元数据查询与只读模型的文本字段交付，
/// 前端不再自持文案映射（口径与同组织 <c>Dy.LisCenter</c> 一致）。
/// </summary>
/// <remarks>
/// 本文件守卫三条路径：元数据查询按白名单返回稳定取值与中文；只读模型的中文文本由服务端声明解析；
/// 元数据服务只消费静态登记注册表，不按运行时反射枚举。全部不依赖数据库与宿主。
/// 源码类判据按 Roslyn 语法树判定（不做文本或正则匹配），共享工具见 <see cref="SourceSyntaxGuard"/>。
/// </remarks>
public sealed class Stage2EnumMetadataQueryTests
{
  /// <summary>
  /// 元数据查询必须按枚举声明顺序返回取值、成员名称与中文说明；
  /// 契约文本与前端下拉都取自这里，任一取值或文案漂移都会让页面显示与后端声明不符。
  /// </summary>
  /// <remarks>
  /// 期望值是硬编码的精确值（不是与登记注册表再比较一次——服务正是从注册表投影而来，那样比较是同源重言）。
  /// </remarks>
  /// <param name="enumName">白名单枚举名称。</param>
  /// <param name="expectedValues">期望的稳定数值，按声明顺序。</param>
  /// <param name="expectedNames">期望的成员名称，按声明顺序。</param>
  /// <param name="expectedDescriptions">期望的中文说明，按声明顺序。</param>
  [Theory]
  [InlineData(nameof(ConfigurationStatus), new[] { 1, 2 }, new[] { "Enabled", "Disabled" }, new[] { "启用", "停用" })]
  [InlineData(nameof(MedicalItemType), new[] { 0, 1 }, new[] { "Laboratory", "Examination" }, new[] { "检验", "检查" })]
  [InlineData(nameof(MedicalStandardUsageStatus), new[] { 0, 1 }, new[] { "Unused", "InUse" }, new[] { "未使用", "已使用" })]
  public async Task Enum_metadata_query_returns_registry_metadata_in_declaration_order(
    string enumName,
    int[] expectedValues,
    string[] expectedNames,
    string[] expectedDescriptions)
  {
    EnumMetadataAppService service = new();

    IReadOnlyList<EnumMetadataItemDto> items = await service.GetEnumMetadataAsync(new QueryEnumMetadataRequest { EnumName = enumName });

    Assert.Equal(expectedValues, items.Select(item => item.Value).ToArray());
    Assert.Equal(expectedNames, items.Select(item => item.Name).ToArray());
    Assert.Equal(expectedDescriptions, items.Select(item => item.Description).ToArray());
  }

  /// <summary>
  /// 未登记枚举、大小写不符与空白名称都必须被拒绝：白名单按大小写精确匹配，
  /// 未知名称不得降级为空集合，否则前端会拿到空下拉。
  /// </summary>
  [Fact]
  public async Task Enum_metadata_query_rejects_unknown_case_variant_and_blank_names()
  {
    EnumMetadataAppService service = new();

    ValidationException unknown = await Assert.ThrowsAsync<ValidationException>(() =>
      service.GetEnumMetadataAsync(new QueryEnumMetadataRequest { EnumName = "NotRegistered" }));
    ValidationException caseVariant = await Assert.ThrowsAsync<ValidationException>(() =>
      service.GetEnumMetadataAsync(new QueryEnumMetadataRequest { EnumName = "configurationstatus" }));
    await Assert.ThrowsAsync<ValidationException>(() =>
      service.GetEnumMetadataAsync(new QueryEnumMetadataRequest { EnumName = " " }));
    await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetEnumMetadataAsync(null!));

    Assert.Equal("不支持枚举：NotRegistered。", unknown.Message);
    Assert.Equal("不支持枚举：configurationstatus。", caseVariant.Message);
  }

  /// <summary>
  /// 静态缓存的查询结果必须以只读集合暴露：调用方修改返回值不得污染后续请求拿到的同一份缓存。
  /// </summary>
  [Fact]
  public async Task Enum_metadata_query_does_not_share_mutable_results()
  {
    EnumMetadataAppService service = new();

    IReadOnlyList<EnumMetadataItemDto> items = await service.GetEnumMetadataAsync(new QueryEnumMetadataRequest { EnumName = nameof(ConfigurationStatus) });

    Assert.IsNotType<EnumMetadataItemDto[]>(items);
    IList<EnumMetadataItemDto> mutableView = Assert.IsAssignableFrom<IList<EnumMetadataItemDto>>(items);
    Assert.Throws<NotSupportedException>(() => mutableView[0] = new EnumMetadataItemDto
    {
      Value = 999,
      Name = "Polluted",
      Description = "污染值"
    });
  }

  /// <summary>
  /// 元数据服务必须直接消费静态登记注册表：自行引用描述器列表或按运行时反射枚举都会绕过白名单，
  /// 使未登记枚举也能被查询到，并让中文来源出现第二套解析。
  /// </summary>
  [Fact]
  public void Enum_metadata_service_uses_static_generated_registry()
  {
    string serviceSource = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Application", "Queries", "EnumMetadata", "EnumMetadataAppService.cs"));

    Assert.True(UsesStaticRegistryOnly(serviceSource), "元数据服务未按静态登记注册表取值。");
  }

  /// <summary>
  /// 注册表消费判据的源码变体：必须真的引用静态登记注册表，且不得出现任何绕过白名单的写法。
  /// </summary>
  /// <remarks>
  /// 这些样例同时是判别力证据：违规只写在注释里、或只写在字符串字面量里都不算违规（旧文本判据会假红），
  /// 而注释里的"注册表引用"不算引用（旧文本判据剥离注释后同样判为未引用）。
  /// </remarks>
  public static TheoryData<string, bool> EnumMetadataRegistryConsumptionSources => new()
  {
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
      }
      """,
      true
    },
    {
      """
      internal sealed class Service
      {
        // 不使用 System.Reflection 反射枚举，也不直接引用 ConfigurationStatusDescriptorList.List。
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
      }
      """,
      true
    },
    {
      """
      internal sealed class Service
      {
        private const string Message = "枚举取值不得通过 GetFields( 或 Enum.GetValues 获取";
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
      }
      """,
      true
    },
    {
      """
      internal sealed class Service
      {
        private readonly IReadOnlyDictionary<string, Type> _cache = null!;
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
      }
      """,
      true
    },
    {
      """
      using System.Reflection;
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
      }
      """,
      false
    },
    {
      """
      using R = System.Reflection;
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Members() => R.GetFields();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => new Dictionary<string, Type>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => new Dictionary<string, Type?>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => new Dictionary<System.String, System.Type>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => new System.Collections.Generic.Dictionary<string, Type>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => ConfigurationStatusDescriptorList.List;
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => Enum.GetValues<ConfigurationStatus>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => Enum.GetNames<ConfigurationStatus>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => typeof(ConfigurationStatus).GetCustomAttribute<DescriptionAttribute>();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        private static readonly object Metadata = MedicalRecognitionEnumDescriptorRegistry.Descriptors;
        private static object Bypass() => typeof(ConfigurationStatus).GetMembers();
      }
      """,
      false
    },
    {
      """
      internal sealed class Service
      {
        // MedicalRecognitionEnumDescriptorRegistry.Descriptors
        private static object Bypass() => typeof(ConfigurationStatus).GetFields();
      }
      """,
      false
    }
  };

  /// <summary>
  /// 注册表消费断言的变体自校验：改成反射枚举、直接引用描述器列表、自建类型字典，
  /// 或根本不引用注册表时都必须被判为违规，否则上面的用例只是"读到源码里出现过的字符串"。
  /// </summary>
  /// <param name="source">被检查的服务源码变体。</param>
  /// <param name="expected">期望的判定结果。</param>
  [Theory]
  [MemberData(nameof(EnumMetadataRegistryConsumptionSources))]
  public void Enum_metadata_registry_consumption_check_rejects_reflection_or_direct_descriptor_use(string source, bool expected)
  {
    Assert.Equal(expected, UsesStaticRegistryOnly(source));
  }

  /// <summary>
  /// 判断服务源码是否只消费静态登记注册表：必须真的引用注册表，
  /// 且不得出现描述器列表、反射取枚举成员或自建类型字典等绕过白名单的写法。
  /// </summary>
  /// <remarks>
  /// 按语法节点判定而不是文本匹配：注释与字符串字面量不是节点，因此"解释不使用反射的说明文字"与
  /// "文案里恰好含有违规写法"都不会假红；类型判定取语法节点的简单名（解包可空与限定名），
  /// 因此 <c>Dictionary&lt;System.String, System.Type&gt;</c> 与 <c>new Dictionary&lt;string, Type?&gt;()</c>
  /// 同样判为违规，而 <c>IReadOnlyDictionary&lt;string, Type&gt;</c> 这类合法声明按简单名区分、不被误判。
  /// 反射判定按成员名而不是命名空间文本：<c>GetFields()</c>、<c>Enum.GetValues&lt;T&gt;()</c>、
  /// <c>GetCustomAttribute&lt;T&gt;()</c> 与别名 <c>using R = System.Reflection;</c> 都会被判为违规。
  /// 已知边界：预处理器关闭的分支（<c>#if false</c>）不产生节点，其中的写法对节点扫描不可见；
  /// 注册表引用按类型名接收方判定，因此 <c>using static</c> 后裸写 <c>Descriptors</c> 的等价改写会因"未引用注册表"而失败；
  /// 反射判定只按成员名，对其它类型上同名的 <c>GetValues</c>/<c>GetNames</c> 调用同样判为违规
  /// （本判据只扫元数据服务的取值路径，当前不存在同名调用）。
  /// </remarks>
  /// <param name="source">服务源码或源码变体。</param>
  /// <returns>只消费注册表时为 <see langword="true"/>。</returns>
  private static bool UsesStaticRegistryOnly(string source)
  {
    CompilationUnitSyntax root = SourceSyntaxGuard.Parse(source);

    bool referencesRegistry = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(access =>
      access.Name.Identifier.ValueText == "Descriptors" &&
      access.Expression.ToString().EndsWith("MedicalRecognitionEnumDescriptorRegistry", StringComparison.Ordinal));

    return referencesRegistry && !HasReflectionOrDescriptorBypass(root);
  }

  /// <summary>
  /// 判断源码中是否存在绕过静态登记注册表的写法。
  /// </summary>
  /// <param name="root">编译单元语法树根节点。</param>
  /// <returns>出现反射枚举、描述器列表直取或自建类型字典时为 <see langword="true"/>。</returns>
  private static bool HasReflectionOrDescriptorBypass(CompilationUnitSyntax root)
  {
    if (root.DescendantNodes().OfType<UsingDirectiveSyntax>()
      .Any(directive => directive.Name?.ToString().StartsWith("System.Reflection", StringComparison.Ordinal) == true))
    {
      return true;
    }

    if (root.DescendantNodes().OfType<NameSyntax>()
      .Any(name => name.ToString().StartsWith("System.Reflection.", StringComparison.Ordinal)))
    {
      return true;
    }

    if (root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
      .Any(access => access.Name.Identifier.ValueText == "List" &&
                     access.Expression.ToString().EndsWith("DescriptorList", StringComparison.Ordinal)))
    {
      return true;
    }

    if (root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any(IsStringToTypeDictionary))
    {
      return true;
    }

    return root.DescendantNodes().OfType<InvocationExpressionSyntax>()
      .Any(invocation => SourceSyntaxGuard.MemberName(invocation.Expression) is "GetFields" or "GetMembers"
        or "GetCustomAttribute" or "GetCustomAttributes" or "GetValues" or "GetNames");
  }

  /// <summary>
  /// 判断对象创建表达式是否在构造 <c>string</c> 到 <c>Type</c> 的字典（自建类型字典的等价写法）。
  /// </summary>
  /// <param name="creation">对象创建表达式。</param>
  /// <returns>类型简单名为 <c>Dictionary</c> 且类型参数为字符串与类型时为 <see langword="true"/>。</returns>
  private static bool IsStringToTypeDictionary(ObjectCreationExpressionSyntax creation)
  {
    if (SourceSyntaxGuard.SimpleTypeName(creation.Type) != "Dictionary") return false;

    // 泛型实参按类型节点取，限定名写法（如 System.Collections.Generic.Dictionary&lt;string, Type&gt;）同样计入。
    IReadOnlyList<TypeSyntax> arguments = SourceSyntaxGuard.TypeArgumentList(creation.Type);

    return arguments.Count == 2 &&
      SourceSyntaxGuard.SimpleTypeName(arguments[0]) is "string" or "String" &&
      SourceSyntaxGuard.SimpleTypeName(arguments[1]) is "Type";
  }

  /// <summary>
  /// 只读模型必须以服务端声明交付中文文本：页面直接取这两个字段展示，
  /// 若服务端不再返回或返回错误文案，前端本地常量会成为事实上的第二来源。
  /// </summary>
  /// <param name="itemType">项目类型取值。</param>
  /// <param name="configurationStatus">配置状态取值。</param>
  /// <param name="expectedItemTypeText">期望的项目类型中文。</param>
  /// <param name="expectedConfigurationStatusText">期望的配置状态中文。</param>
  [Theory]
  [InlineData(MedicalItemType.Laboratory, ConfigurationStatus.Enabled, "检验", "启用")]
  [InlineData(MedicalItemType.Examination, ConfigurationStatus.Disabled, "检查", "停用")]
  public void Read_model_exposes_enum_texts_from_server_declarations(
    MedicalItemType itemType,
    ConfigurationStatus configurationStatus,
    string expectedItemTypeText,
    string expectedConfigurationStatusText)
  {
    RecognitionProjectConfigurationReadModel model = new()
    {
      ItemType = itemType,
      ConfigurationStatus = configurationStatus
    };

    Assert.Equal(expectedItemTypeText, model.ItemTypeText);
    Assert.Equal(expectedConfigurationStatusText, model.ConfigurationStatusText);
  }

  /// <summary>
  /// 项目类型取自无存储约束的列，未登记取值必须安全降级为 <see langword="null"/>：
  /// 若按严格解析抛出，一条越界数据会让整张互认配置列表查询失败。
  /// </summary>
  [Fact]
  public void Read_model_returns_null_item_type_text_for_unregistered_value()
  {
    RecognitionProjectConfigurationReadModel model = new() { ItemType = (MedicalItemType)99 };

    Assert.Null(model.ItemTypeText);
  }

  /// <summary>
  /// 配置状态由布尔派生、取值集合封闭，未登记取值必须抛出而不是静默降级，
  /// 否则调用链损坏会被显示成"未知状态"而长期不被发现。
  /// </summary>
  [Fact]
  public void Read_model_throws_for_unregistered_configuration_status()
  {
    // 取值 99 不可由正常业务路径产生：配置状态由互认配置表的布尔启用列派生，只有启用与停用两种取值。
    // 这里只用于约束验证（防御用途）：确认损坏的调用链不会被静默降级成"未知状态"。
    RecognitionProjectConfigurationReadModel model = new() { ConfigurationStatus = (ConfigurationStatus)99 };

    Assert.Throws<ExtensionException>(() => model.ConfigurationStatusText);
  }

  /// <summary>
  /// 冻结的 Kiota 输入文档必须同时暴露枚举元数据端点与只读模型的中文文本字段：
  /// 缺端点则前端下拉只能永远退回本地常量，缺字段则前端只能自持文案映射。
  /// </summary>
  [Fact]
  public void Frozen_openapi_exposes_enum_metadata_endpoint_and_read_model_texts()
  {
    JsonNode document = JsonNode.Parse(File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "client", "packages", "api-client-medical-recognition", "openapi",
      "medical-recognition.openapi.json")))!;

    JsonNode operation = document["paths"]!["/Api/EnumMetadata/GetEnumMetadata"]!["post"]!;
    Assert.Equal("#/components/schemas/QueryEnumMetadataRequest",
      operation["requestBody"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
    Assert.Equal(["enumName"], document["components"]!["schemas"]!["QueryEnumMetadataRequest"]!["required"]!
      .AsArray().Select(node => node!.GetValue<string>()).ToArray());
    Assert.Equal(["value", "name", "description"], document["components"]!["schemas"]!["EnumMetadataItemDto"]!["properties"]!
      .AsObject().Select(property => property.Key).ToArray());

    JsonNode readModelProperties = document["components"]!["schemas"]!["RecognitionProjectConfigurationReadModel"]!["properties"]!;
    Assert.NotNull(readModelProperties["itemTypeText"]);
    Assert.NotNull(readModelProperties["configurationStatusText"]);
  }

  /// <summary>
  /// 生成端必须暴露枚举元数据 API 与两个中文文本字段：Kiota 输入文档变了但生成物未重建时，
  /// 前端会在类型检查阶段就取不到这些成员。
  /// </summary>
  [Fact]
  public void Generated_client_exposes_enum_metadata_api_and_text_fields()
  {
    string clientSourceRoot = Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "client", "packages", "api-client-medical-recognition", "src");

    string requestBuilder = File.ReadAllText(Path.Combine(clientSourceRoot, "api", "enumMetadata", "getEnumMetadata", "index.ts"));
    // 只锁"动词 + 参数前缀 + 返回类型"，不锁整条签名的空白与参数顺序：Kiota 升级带来的无害格式变化不应假红。
    Assert.Contains("post(body: QueryEnumMetadataRequest", requestBuilder, StringComparison.Ordinal);
    Assert.Contains("Promise<EnumMetadataItemDto[] | undefined>;", requestBuilder, StringComparison.Ordinal);
    Assert.DoesNotContain("get(body: QueryEnumMetadataRequest", requestBuilder, StringComparison.Ordinal);
    Assert.Contains("{+baseurl}/Api/EnumMetadata/GetEnumMetadata", requestBuilder, StringComparison.Ordinal);

    string models = File.ReadAllText(Path.Combine(clientSourceRoot, "models", "index.ts"));
    Assert.Contains("itemTypeText?: string | null;", models, StringComparison.Ordinal);
    Assert.Contains("configurationStatusText?: string | null;", models, StringComparison.Ordinal);
  }

  /// <summary>
  /// OpenAPI 通道的枚举契约必须与枚举成员声明逐字一致：取值、成员名称与 <c>[Description]</c> 中文三者
  /// 都取自**枚举声明**（第三种来源），不是与元数据查询的返回值比较——
  /// 转换器与元数据服务都从同一份描述器投影，拿两者互比只能发现形态分歧，发现不了共同取错值。
  /// </summary>
  /// <remarks>
  /// 按语法节点读取成员声明：显式整数取值、<c>Description</c> 与 <c>DescriptionAttribute</c> 两种写法、
  /// 以及同一方括号内的其它特性都不影响判定；缺显式取值或缺字面量中文会按成员名单独报出。
  /// 成员顺序即声明顺序，与描述器的取值顺序一致。
  /// </remarks>
  /// <param name="enumName">对外枚举名称。</param>
  [Theory]
  [InlineData(nameof(ConfigurationStatus))]
  [InlineData(nameof(MedicalItemType))]
  [InlineData(nameof(MedicalStandardUsageStatus))]
  public async Task OpenApi_enum_contract_matches_the_enum_source_declarations(string enumName)
  {
    OpenApiSchema schema = new();
    OpenApiDocument document = new()
    {
      Components = new OpenApiComponents
      {
        Schemas = new Dictionary<string, IOpenApiSchema> { [enumName] = schema }
      }
    };
    await new MedicalRecognitionEnumOpenApiDocumentTransformer().TransformAsync(document, null!, CancellationToken.None);

    EnumDeclarationSyntax declaration = SourceSyntaxGuard
      .Read("server", "Dy.MedicalRecognition.Domain.Share", "Enums", $"{enumName}.cs")
      .DescendantNodes().OfType<EnumDeclarationSyntax>()
      .Single(item => item.Identifier.ValueText == enumName);

    (string Name, int? Value, string? Description)[] declared = [.. declaration.Members.Select(member => (
      member.Identifier.ValueText,
      member.EqualsValue?.Value is LiteralExpressionSyntax { Token.Value: int value } ? value : (int?)null,
      SourceSyntaxGuard.LiteralStringArgument(SourceSyntaxGuard.FindAttribute(member.AttributeLists, "Description"))))];

    Assert.NotEmpty(declared);
    Assert.Empty(declared.Where(member => member.Value is null).Select(member => $"缺少显式整数取值：{member.Name}"));
    Assert.Empty(declared.Where(member => member.Description is null).Select(member => $"缺少字面量 [Description] 中文：{member.Name}"));

    Assert.Equal(declared.Select(member => member.Value!.Value), schema.Enum!.Select(value => value!.GetValue<int>()));
    Assert.Equal(declared.Select(member => member.Name), ExtensionValues(schema, "x-enumNames"));
    Assert.Equal(declared.Select(member => member.Description!), ExtensionValues(schema, "x-enumDescriptions"));
  }

  /// <summary>
  /// 契约程序集公开类型上的枚举必须已登记，或属于下方显式列出的后续阶段枚举：
  /// 新增契约枚举却漏登记、或把已登记枚举混进排除清单，都会让用例失败。
  /// </summary>
  /// <remarks>
  /// 扫描整个契约程序集（而不是手工列具体类型），新增请求或只读模型会自动纳入；
  /// 属性类型解包可空、数组与泛型集合元素；继承来的公开属性一并计入（契约字段当前均为自有属性）。
  /// 排除清单承载尚未进入登记范围的**后续阶段读模型上的枚举**，当前为空：阶段 1 至阶段 3 读模型上的枚举
  /// （<c>MedicalItemType</c>、<c>MedicalStandardUsageStatus</c>、<c>ConfigurationStatus</c>）、
  /// 阶段 4 报告契约上的枚举（报告类型、报告生命周期状态、就诊类型、检验结果类型、检验异常标志、来源影像状态）
  /// 与阶段 5 处理结果与引用事实契约上的枚举（互认结果、不采纳原因）均已登记，不再有需要排除的枚举；
  /// 排除项自身也必须确实出现在契约上且不得与白名单重叠，否则清单过期或侵占已登记枚举都会让用例失败。
  /// 已知边界：只扫属性，不扫方法签名（返回类型与参数）上的枚举；扫描范围含嵌套公开类型，
  /// 但当前契约程序集没有嵌套公开类型，因此该分支没有字段证据，只有扫描面本身。
  /// </remarks>
  [Fact]
  public void Stage2_contract_enums_match_the_registry_whitelist()
  {
    string[] laterStageContractEnums = [];

    string[] contractEnums = typeof(RecognitionProjectConfigurationReadModel).Assembly
      .GetTypes()
      .Where(type => type.IsPublic || type.IsNestedPublic)
      .SelectMany(type => type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
      .SelectMany(property => EnumTypesIn(property.PropertyType))
      .Select(type => type.Name)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal)
      .ToArray();

    string[] registered = MedicalRecognitionEnumDescriptorRegistry.Descriptors.Keys.Order(StringComparer.Ordinal).ToArray();

    // 契约上的枚举要么已登记，要么属于显式列出的后续阶段枚举（漏登记会在这里失败）。
    Assert.Empty(contractEnums.Except(registered, StringComparer.Ordinal).Except(laterStageContractEnums, StringComparer.Ordinal));
    // 白名单不得包含契约之外的枚举（避免把后续阶段枚举冒充已覆盖范围）。
    Assert.Empty(registered.Except(contractEnums, StringComparer.Ordinal));
    // 排除清单不得过期：每一项都必须确实出现在契约上。
    Assert.Empty(laterStageContractEnums.Except(contractEnums, StringComparer.Ordinal));
    // 排除清单不得侵占已登记枚举：把已登记枚举写进排除清单同样会让用例失败。
    Assert.Empty(laterStageContractEnums.Intersect(registered, StringComparer.Ordinal));
  }

  /// <summary>
  /// 取出属性类型中承载的枚举类型：解包可空、数组与泛型集合元素，其余返回空。
  /// </summary>
  /// <param name="type">属性声明类型。</param>
  /// <returns>该类型承载的枚举类型；不含枚举时为空。</returns>
  private static IEnumerable<Type> EnumTypesIn(Type type)
  {
    Type current = Nullable.GetUnderlyingType(type) ?? type;
    if (current.IsEnum)
    {
      yield return current;
      yield break;
    }

    if (current.IsArray && current.GetElementType() is Type element)
    {
      foreach (Type nested in EnumTypesIn(element)) yield return nested;
      yield break;
    }

    if (current.IsGenericType)
    {
      foreach (Type argument in current.GetGenericArguments())
      {
        foreach (Type nested in EnumTypesIn(argument)) yield return nested;
      }
    }
  }

  /// <summary>
  /// 读取 OpenAPI JsonNode 扩展中的字符串数组。
  /// </summary>
  /// <param name="schema">枚举架构。</param>
  /// <param name="extensionName">扩展名称。</param>
  /// <returns>扩展中的字符串值。</returns>
  private static string[] ExtensionValues(OpenApiSchema schema, string extensionName)
  {
    JsonNodeExtension extension = Assert.IsType<JsonNodeExtension>(schema.Extensions![extensionName]);
    return [.. Assert.IsType<JsonArray>(extension.Node).Select(node => node!.GetValue<string>())];
  }
}
