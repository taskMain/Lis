using Dy.Core.Abstractions.Models;
using Dy.MedicalRecognition.Application;
using Dy.MedicalRecognition.Application.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Dy.MedicalRecognition.Tests.Architecture;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using Xunit;

using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 对外枚举的契约治理：枚举必须在源码声明处启用 SourceGen 描述器，描述器必须被 OpenAPI 注册表登记，
/// OpenAPI 输入文档与生成端必须按数值交互。
/// </summary>
/// <remarks>
/// 对应阶段 2 立项修复的跨阶段缺陷：框架只为枚举生成 <c>{"type":"integer"}</c>，缺少取值集合时 Kiota
/// 会把可空枚举退化为空对象类型，请求体在线上被写成 <c>{}</c>。本文件是该修复的回归守卫，
/// 覆盖阶段 1 与阶段 2 共用的三个枚举（<see cref="MedicalItemType"/>、<see cref="ConfigurationStatus"/>、<see cref="MedicalStandardUsageStatus"/>）。
/// 全部为静态证据（源码、程序集、冻结的 OpenAPI 输入、生成物），不依赖数据库与宿主。
/// 源码类判据按 Roslyn 语法树判定（不做文本或正则匹配），共享工具见 <see cref="SourceSyntaxGuard"/>。
/// </remarks>
public sealed class Stage2EnumContractTests
{
  /// <summary>登记在 OpenAPI 注册表里的对外枚举名称，按登记顺序。</summary>
  private static readonly string[] ExpectedEnumNames =
  [
    nameof(ConfigurationStatus), nameof(MedicalItemType), nameof(MedicalStandardUsageStatus),
    nameof(MedicalReportType), nameof(MedicalReportLifecycleStatus), nameof(VisitType),
    nameof(LaboratoryResultType), nameof(LaboratoryAbnormalFlag), nameof(SourceImageStatus)
  ];

  /// <summary>
  /// 三个对外枚举都必须在源码声明处启用描述器生成：缺少特性时描述器不会生成，
  /// 注册表与 OpenAPI 随之失去取值集合。
  /// </summary>
  /// <param name="fileName">枚举源码文件名。</param>
  /// <param name="enumName">枚举名称。</param>
  /// <param name="descriptions">该枚举每个成员必须具备的中文说明声明。</param>
  [Theory]
  [InlineData("ConfigurationStatus.cs", nameof(ConfigurationStatus), "[Description(\"启用\")]", "[Description(\"停用\")]")]
  [InlineData("MedicalItemType.cs", nameof(MedicalItemType), "[Description(\"检验\")]", "[Description(\"检查\")]")]
  [InlineData("MedicalStandardUsageStatus.cs", nameof(MedicalStandardUsageStatus), "[Description(\"未使用\")]", "[Description(\"已使用\")]")]
  public void Public_enums_opt_into_enum_descriptor_generation(string fileName, string enumName, string enabledDescription, string disabledDescription)
  {
    string source = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "server", "Dy.MedicalRecognition.Domain.Share", "Enums", fileName));

    Assert.Contains("using Dy.Core.SourceGen;", source, StringComparison.Ordinal);
    Assert.Matches($@"\[EnumDescriptor\]\s*public enum {enumName}", source);
    Assert.Contains(enabledDescription, source, StringComparison.Ordinal);
    Assert.Contains(disabledDescription, source, StringComparison.Ordinal);
  }

  /// <summary>
  /// 枚举所在程序集的编译产物必须包含 SourceGen 生成的描述器列表：特性与生成器缺一不可，
  /// 只声明特性而没有生成物时本用例失败。
  /// </summary>
  /// <param name="enumName">枚举名称。</param>
  [Theory]
  [InlineData(nameof(ConfigurationStatus))]
  [InlineData(nameof(MedicalItemType))]
  [InlineData(nameof(MedicalStandardUsageStatus))]
  public void Enum_assembly_contains_generated_descriptor_list(string enumName)
  {
    byte[] assemblyBytes = File.ReadAllBytes(typeof(ConfigurationStatus).Assembly.Location);

    Assert.True(
      assemblyBytes.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes($"{enumName}DescriptorList")) >= 0,
      $"{enumName}DescriptorList 未由 Dy.Core.SourceGen 生成。");
  }

  /// <summary>
  /// 注册表必须恰好登记这三个对外枚举，且取值、名称与中文说明与业务定义一致。
  /// </summary>
  /// <remarks>
  /// 本用例只守"不得多登记、不得漏掉这三个、顺序与取值稳定"。
  /// "阶段2 契约上新增枚举却漏登记"由
  /// <c>Stage2EnumMetadataQueryTests.Stage2_contract_enums_match_the_registry_whitelist</c> 按契约类型扫描枚举属性守卫。
  /// </remarks>
  [Fact]
  public void Enum_descriptor_registry_covers_every_contract_enum()
  {
    Assert.Equal(ExpectedEnumNames, MedicalRecognitionEnumDescriptorRegistry.Descriptors.Keys.ToArray());

    IReadOnlyList<IEnumDescriptor> configurationStatus =
      MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(ConfigurationStatus)];
    Assert.Equal([1, 2], configurationStatus.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Enabled", "Disabled"], configurationStatus.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["启用", "停用"], configurationStatus.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> medicalItemType =
      MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(MedicalItemType)];
    Assert.Equal([0, 1], medicalItemType.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Laboratory", "Examination"], medicalItemType.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["检验", "检查"], medicalItemType.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> medicalStandardUsageStatus =
      MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(MedicalStandardUsageStatus)];
    Assert.Equal([0, 1], medicalStandardUsageStatus.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Unused", "InUse"], medicalStandardUsageStatus.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["未使用", "已使用"], medicalStandardUsageStatus.Select(descriptor => descriptor.Description).ToArray());

    // 阶段 4 把报告契约上的六个枚举带入对外契约并登记：取值、成员名与中文说明逐项冻结，
    // 缺少这些断言时，新登记的枚举取值被改动、说明被改写或成员顺序被调整都不会让任何用例失败。
    IReadOnlyList<IEnumDescriptor> medicalReportType = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(MedicalReportType)];
    Assert.Equal([1, 2], medicalReportType.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Laboratory", "Examination"], medicalReportType.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["检验报告", "检查报告"], medicalReportType.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> reportLifecycleStatus = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(MedicalReportLifecycleStatus)];
    Assert.Equal([1, 2], reportLifecycleStatus.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Effective", "Voided"], reportLifecycleStatus.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["有效", "已作废"], reportLifecycleStatus.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> visitType = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(VisitType)];
    Assert.Equal([1, 2, 3, 4, 5], visitType.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Outpatient", "Emergency", "Inpatient", "PhysicalExam", "Other"], visitType.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["门诊", "急诊", "住院", "体检", "其他"], visitType.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> laboratoryResultType = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(LaboratoryResultType)];
    Assert.Equal([1, 2, 3], laboratoryResultType.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Numeric", "Qualitative", "Textual"], laboratoryResultType.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["数值型", "定性型", "文本型"], laboratoryResultType.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> laboratoryAbnormalFlag = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(LaboratoryAbnormalFlag)];
    Assert.Equal([1, 2, 3, 4], laboratoryAbnormalFlag.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Normal", "High", "Low", "OtherAbnormal"], laboratoryAbnormalFlag.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["正常", "偏高", "偏低", "其他异常"], laboratoryAbnormalFlag.Select(descriptor => descriptor.Description).ToArray());

    IReadOnlyList<IEnumDescriptor> sourceImageStatus = MedicalRecognitionEnumDescriptorRegistry.Descriptors[nameof(SourceImageStatus)];
    Assert.Equal([1, 2, 3], sourceImageStatus.Select(descriptor => descriptor.Value).ToArray());
    Assert.Equal(["Available", "None", "Unknown"], sourceImageStatus.Select(descriptor => descriptor.Name).ToArray());
    Assert.Equal(["有影像", "无影像", "未知"], sourceImageStatus.Select(descriptor => descriptor.Description).ToArray());
  }

  /// <summary>
  /// OpenAPI 文档转换器必须为已存在的枚举架构补齐取值、名称与中文说明，且不改动无关架构、
  /// 不新增架构；缺少转换器时框架只输出 <c>integer</c>，生成端会退化为空对象。
  /// </summary>
  [Fact]
  public async Task OpenApi_transformer_fills_enum_values_names_and_descriptions()
  {
    OpenApiSchema configurationStatusSchema = new() { Enum = [JsonValue.Create(999)] };
    OpenApiSchema medicalItemTypeSchema = new() { Enum = [JsonValue.Create(999)] };
    OpenApiSchema medicalStandardUsageStatusSchema = new() { Enum = [JsonValue.Create(999)] };
    OpenApiSchema unrelatedSchema = new();
    OpenApiDocument document = new()
    {
      Components = new OpenApiComponents
      {
        Schemas = new Dictionary<string, IOpenApiSchema>
        {
          [nameof(ConfigurationStatus)] = configurationStatusSchema,
          [nameof(MedicalItemType)] = medicalItemTypeSchema,
          [nameof(MedicalStandardUsageStatus)] = medicalStandardUsageStatusSchema,
          ["UnrelatedSchema"] = unrelatedSchema
        }
      }
    };

    MedicalRecognitionEnumOpenApiDocumentTransformer transformer = new();
    await transformer.TransformAsync(document, null!, CancellationToken.None);

    Assert.Equal([1, 2], configurationStatusSchema.Enum!.Select(value => value!.GetValue<int>()).ToArray());
    Assert.Equal(["Enabled", "Disabled"], ExtensionValues(configurationStatusSchema, "x-enumNames"));
    Assert.Equal(["启用", "停用"], ExtensionValues(configurationStatusSchema, "x-enumDescriptions"));
    Assert.Equal([0, 1], medicalItemTypeSchema.Enum!.Select(value => value!.GetValue<int>()).ToArray());
    Assert.Equal(["Laboratory", "Examination"], ExtensionValues(medicalItemTypeSchema, "x-enumNames"));
    Assert.Equal(["检验", "检查"], ExtensionValues(medicalItemTypeSchema, "x-enumDescriptions"));
    Assert.Equal([0, 1], medicalStandardUsageStatusSchema.Enum!.Select(value => value!.GetValue<int>()).ToArray());
    Assert.Equal(["Unused", "InUse"], ExtensionValues(medicalStandardUsageStatusSchema, "x-enumNames"));
    Assert.Equal(["未使用", "已使用"], ExtensionValues(medicalStandardUsageStatusSchema, "x-enumDescriptions"));
    Assert.Null(unrelatedSchema.Enum);
    Assert.Null(unrelatedSchema.Extensions);
    Assert.Equal(4, document.Components.Schemas.Count);
  }

  /// <summary>
  /// 冻结的 Kiota 输入文档必须同时满足两点：枚举架构带取值集合，且可空枚举属性归一为 <c>$ref</c>；
  /// 任一环节回退都会让生成端重新把可空枚举写成空对象。
  /// </summary>
  [Fact]
  public void Frozen_openapi_declares_enum_values_and_flattened_nullable_references()
  {
    JsonNode document = JsonNode.Parse(File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "client", "packages", "api-client-medical-recognition", "openapi",
      "medical-recognition.openapi.json")))!;

    JsonNode configurationStatus = document["components"]!["schemas"]![nameof(ConfigurationStatus)]!;
    Assert.Equal([1, 2], configurationStatus["enum"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray());
    Assert.Equal(["Enabled", "Disabled"], configurationStatus["x-enumNames"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());
    Assert.Equal(["启用", "停用"], configurationStatus["x-enumDescriptions"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());

    JsonNode medicalItemType = document["components"]!["schemas"]![nameof(MedicalItemType)]!;
    Assert.Equal([0, 1], medicalItemType["enum"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray());
    Assert.Equal(["检验", "检查"], medicalItemType["x-enumDescriptions"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());

    JsonNode medicalStandardUsageStatus = document["components"]!["schemas"]![nameof(MedicalStandardUsageStatus)]!;
    Assert.Equal([0, 1], medicalStandardUsageStatus["enum"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray());
    Assert.Equal(["Unused", "InUse"], medicalStandardUsageStatus["x-enumNames"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());
    Assert.Equal(["未使用", "已使用"], medicalStandardUsageStatus["x-enumDescriptions"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray());

    JsonNode requestProperty = document["components"]!["schemas"]![
      "RecognitionProjectConfigurationListQueryRequest"]!["properties"]!["configurationStatus"]!;
    Assert.Null(requestProperty["oneOf"]);
    Assert.Equal("#/components/schemas/ConfigurationStatus", requestProperty["$ref"]!.GetValue<string>());
  }

  /// <summary>
  /// 生成端必须把三个枚举属性按数值读写：属性类型为 <c>number | null</c>、序列化走 <c>writeNumberValue</c>，
  /// 且不再出现空对象回退类型；这三条正是缺陷期线上写出 <c>{}</c> 的直接原因。
  /// </summary>
  [Fact]
  public void Generated_client_reads_and_writes_enum_properties_as_numbers()
  {
    string models = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "client", "packages", "api-client-medical-recognition", "src", "models", "index.ts"));

    Assert.Contains("configurationStatus?: number | null;", models, StringComparison.Ordinal);
    Assert.Contains("itemType?: number | null;", models, StringComparison.Ordinal);
    Assert.Contains("usageStatus?: number | null;", models, StringComparison.Ordinal);
    Assert.Contains("writer.writeNumberValue(\"configurationStatus\"", models, StringComparison.Ordinal);
    Assert.Contains("writer.writeNumberValue(\"itemType\"", models, StringComparison.Ordinal);
    Assert.Contains("writer.writeNumberValue(\"usageStatus\"", models, StringComparison.Ordinal);
    Assert.DoesNotContain("_configurationStatusMember1", models, StringComparison.Ordinal);
    Assert.DoesNotContain("_itemTypeMember1", models, StringComparison.Ordinal);
    // 缺陷期实际存在的两个按上下文命名的回退类型：即使将来出现同名前缀的新类型，也必须由具体名称守卫回归。
    Assert.DoesNotContain("EffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1", models, StringComparison.Ordinal);
    Assert.DoesNotContain("MedicalStandardCategoryListQueryRequest_itemTypeMember1", models, StringComparison.Ordinal);
  }

  /// <summary>
  /// 应用模块必须把枚举文档转换器注册进 OpenAPI 管线：只声明转换器而不注册时，
  /// 真实 <c>/openapi/v1.json</c> 会失去取值与中文说明，而直接调用转换器的用例仍会全绿。
  /// </summary>
  /// <remarks>
  /// 判据读取该类型的全部声明文件而不是某一个固定文件：声明数量先被冻结为恰好一处，
  /// 类型被拆分到别的分片（partial）或声明被删除时用例直接失败，不会因为只读一个文件而漏判。
  /// </remarks>
  [Fact]
  public void Application_module_registers_the_enum_open_api_transformer()
  {
    IReadOnlyList<(string RelativePath, CompilationUnitSyntax Root)> declarations = SourceSyntaxGuard.ReadTypeDeclarations(
      "server/Dy.MedicalRecognition.Application", nameof(MedicalRecognitionApplicationModule));

    Assert.Equal(
      ["server/Dy.MedicalRecognition.Application/MedicalRecognitionApplicationModule.cs"],
      declarations.Select(declaration => declaration.RelativePath).ToArray());
    Assert.True(
      RegistersEnumOpenApiTransformer([.. declarations.Select(declaration => declaration.Root)]),
      "应用模块未把枚举文档转换器注册进 OpenAPI 管线。");
  }

  /// <summary>
  /// 注册判据的源码变体：注册语句必须真的落在 <c>OnPostConfigureServices</c> 的方法体里。
  /// </summary>
  /// <remarks>
  /// 这些样例同时是判别力证据，覆盖三种写法差异：注释与字符串字面量（旧文本判据会假绿）、
  /// 写在别的方法里（旧文本判据会假绿）、以及字符串里的 <c>//</c> 截断后续文本（旧文本判据会假红）。
  /// </remarks>
  public static TheoryData<string, bool> EnumOpenApiRegistrationSources => new()
  {
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      true
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new OtherTransformer()));
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          context.Services.AddSingleton<MedicalRecognitionEnumOpenApiDocumentTransformer>();
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          // context.Services.ConfigureAll<OpenApiOptions>(options => options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          /* context.Services.ConfigureAll<OpenApiOptions>(options => options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer())); */
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
        }

        private void RegisterOther(object context)
        {
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          string text = "context.Services.ConfigureAll<OpenApiOptions>(options => options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));";
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          string address = "http://localhost:15014/";
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      true
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
      #if false
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
      #endif
        }
      }
      """,
      false
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          OptionsServiceCollectionExtensions.ConfigureAll<OpenApiOptions>(context.Services, options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      true
    },
    {
      """
      internal sealed class Module
      {
        public void OnPostConfigureServices(object context)
        {
          context.Services.ConfigureAll<OtherOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """,
      false
    }
  };

  /// <summary>
  /// 注册断言的变体自校验：删掉注册语句、替换转换器、把注册语句注释掉、或把它写到别的方法里时，
  /// 都必须被判定为未注册，否则上面的用例只是"读到源码里出现过的字符串"。
  /// </summary>
  /// <param name="source">被检查的模块源码变体。</param>
  /// <param name="expected">期望的判定结果。</param>
  [Theory]
  [MemberData(nameof(EnumOpenApiRegistrationSources))]
  public void Enum_open_api_registration_check_rejects_missing_or_replaced_registration(string source, bool expected)
  {
    Assert.Equal(expected, RegistersEnumOpenApiTransformer(SourceSyntaxGuard.Parse(source)));
  }

  /// <summary>
  /// 注册判据必须把类型的每一个分片一起判定，并且只在"带方法体的同名声明恰好一处"时才继续判定：
  /// 只看第一个分片会把"注册写在另一分片"判成未注册；同名声明出现 0 处或多处时只取第一处会让判据静默失真。
  /// </summary>
  [Fact]
  public void Enum_open_api_registration_check_covers_partial_parts_and_declaration_counts()
  {
    const string declarationPartWithoutMethod = """
      internal sealed partial class Module
      {
      }
      """;
    const string registrationPart = """
      internal sealed partial class Module
      {
        public void OnPostConfigureServices(object context)
        {
          context.Services.ConfigureAll<OpenApiOptions>(options =>
            options.AddDocumentTransformer(new MedicalRecognitionEnumOpenApiDocumentTransformer()));
        }
      }
      """;
    const string duplicatedDeclarationPart = """
      internal sealed partial class Module
      {
        public void OnPostConfigureServices(object context)
        {
        }
      }
      """;

    // 注册语句写在第二个分片：两个分片一起判定时必须命中，只传第一个分片时必须判为未注册。
    Assert.True(RegistersEnumOpenApiTransformer(
      SourceSyntaxGuard.Parse(declarationPartWithoutMethod), SourceSyntaxGuard.Parse(registrationPart)));
    Assert.False(RegistersEnumOpenApiTransformer(SourceSyntaxGuard.Parse(declarationPartWithoutMethod)));

    // 带方法体的同名声明出现 2 处：不能只取第一处就当作命中。
    Assert.False(RegistersEnumOpenApiTransformer(
      SourceSyntaxGuard.Parse(registrationPart), SourceSyntaxGuard.Parse(duplicatedDeclarationPart)));
  }

  /// <summary>
  /// 判断模块源码是否在 <c>OnPostConfigureServices</c> 方法体内把枚举文档转换器注册进 OpenAPI 管线。
  /// </summary>
  /// <remarks>
  /// 按语法节点判定：必须存在 <c>OnPostConfigureServices</c> 的方法体，且其可执行范围内有一处
  /// <c>ConfigureAll&lt;OpenApiOptions&gt;(...)</c>，其参数里创建并注册了本转换器。
  /// 注释与字符串字面量不是节点，因此"注释掉注册语句""把注册语句写进文案"都不会假绿；
  /// 判据锚定在方法体内（不是整个文件），因此"写在别的、从不调用的方法里"也不会假绿；
  /// 成员名与类型名取自语法节点，限定名写法（静态调用 <c>OptionsServiceCollectionExtensions.ConfigureAll</c>）
  /// 与换行、具名局部变量等形式差异都不影响判定。
  /// 传入的是一组源码根节点（该类型的全部声明文件），带方法体的同名声明必须恰好 1 处：
  /// 0 处或多处都返回 <see langword="false"/>，避免只取第一处或只看一个文件而放过其余分片。
  /// 已知边界：目标类型 <c>new()</c> 无法在语法层判出类型名；该形参类型为接口，目标类型 new 不能编译。
  /// </remarks>
  /// <param name="roots">该类型在仓库内的全部声明文件语法树。</param>
  /// <returns>注册存在时为 <see langword="true"/>。</returns>
  private static bool RegistersEnumOpenApiTransformer(params CompilationUnitSyntax[] roots)
  {
    MethodDeclarationSyntax[] declarations = [.. roots
      .SelectMany(root => root.DescendantNodes().OfType<MethodDeclarationSyntax>())
      .Where(candidate => candidate.Identifier.ValueText == "OnPostConfigureServices" && candidate.Body is not null)];
    if (declarations.Length != 1) return false;

    MethodDeclarationSyntax method = declarations[0];
    return method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
      SourceSyntaxGuard.MemberName(invocation.Expression) == "AddDocumentTransformer" &&
      invocation.ArgumentList.Arguments.Any(argument =>
        SourceSyntaxGuard.CreatesType(argument.Expression, nameof(MedicalRecognitionEnumOpenApiDocumentTransformer))) &&
      invocation.Ancestors().OfType<InvocationExpressionSyntax>().Any(ancestor =>
        SourceSyntaxGuard.MemberName(ancestor.Expression) == "ConfigureAll" &&
        SourceSyntaxGuard.TypeArguments(ancestor.Expression).Contains("OpenApiOptions", StringComparer.Ordinal)));
  }

  /// <summary>
  /// 冻结文档的路径清单与全局归一结果必须与本次生成范围一致：整体被替换成过期版本、
  /// 归一在别的属性上回退、或本阶段新增端点在契约中丢失时，只断言枚举相关键的用例发现不了。
  /// </summary>
  /// <remarks>
  /// 路径按**完整集合相等**断言（不是计数 + 几个 Contains），失败信息自带差异；
  /// 下面的清单是**阶段 4 交付后**真实后端输出 + 归一的快照，**新增或删除端点时必须同步本清单与生成入口**。
  /// </remarks>
  [Fact]
  public void Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes()
  {
    string raw = File.ReadAllText(Path.Combine(
      SourceSyntaxGuard.FindRepositoryRoot(), "client", "packages", "api-client-medical-recognition", "openapi",
      "medical-recognition.openapi.json"));
    JsonNode document = JsonNode.Parse(raw)!;

    string[] expectedPaths =
    [
      "/Api/EnumMetadata/GetEnumMetadata",
      "/Api/MedicalRecognitionReport/ChangeMedicalStandardItemRemark",
      "/Api/MedicalRecognitionReport/CreateMedicalStandardCategory",
      "/Api/MedicalRecognitionReport/CreateMedicalStandardGroup",
      "/Api/MedicalRecognitionReport/CreateMedicalStandardItem",
      "/Api/MedicalRecognitionReport/CreateMutualRecognitionItem",
      "/Api/MedicalRecognitionReport/DisableMedicalStandardCategory",
      "/Api/MedicalRecognitionReport/DisableMedicalStandardGroup",
      "/Api/MedicalRecognitionReport/DisableMedicalStandardItem",
      "/Api/MedicalRecognitionReport/DisableMutualRecognitionItem",
      "/Api/MedicalRecognitionReport/EnableMedicalStandardCategory",
      "/Api/MedicalRecognitionReport/EnableMedicalStandardGroup",
      "/Api/MedicalRecognitionReport/EnableMedicalStandardItem",
      "/Api/MedicalRecognitionReport/EnableMutualRecognitionItem",
      "/Api/MedicalRecognitionReport/OpenReportVersionPdf",
      "/Api/MedicalRecognitionReport/SaveBranchRecognitionAmount",
      "/Api/MedicalRecognitionReport/SaveOrganizationHospitalBranchRecognitionAmount",
      "/Api/MedicalRecognitionReport/SubmitCompleteExaminationReport",
      "/Api/MedicalRecognitionReport/SubmitCompleteLaboratoryReport",
      "/Api/MedicalRecognitionReport/UpdateMedicalStandardCategory",
      "/Api/MedicalRecognitionReport/UpdateMedicalStandardGroup",
      "/Api/MedicalRecognitionReport/UpdateMutualRecognitionItemConfiguration",
      "/Api/MedicalRecognitionReport/VoidExaminationReport",
      "/Api/MedicalRecognitionReport/VoidLaboratoryReport",
      "/Api/MedicalRecognitionReportQuery/QueryBranchMedicalReportList",
      "/Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList",
      "/Api/MedicalRecognitionReportQuery/QueryEffectiveMedicalStandardCatalog",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalReportList",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionDetail",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionList",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalStandardCategoryList",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalStandardGroupList",
      "/Api/MedicalRecognitionReportQuery/QueryMedicalStandardItemList",
      "/Api/MedicalRecognitionReportQuery/QueryRecognitionAmountList",
      "/Api/MedicalRecognitionReportQuery/QueryRecognitionProjectConfigurationList",
      "/api/v1/report-pdf/examination-report",
      "/api/v1/report-pdf/laboratory-report",
      "/api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf",
      "/auth/login"
    ];
    string[] paths = document["paths"]!.AsObject().Select(property => property.Key).Order(StringComparer.Ordinal).ToArray();
    Assert.Equal(expectedPaths, paths);

    // 归一必须全量生效：任何一处可空引用或整数联合残留都说明脚本没跑或规则回退。
    Assert.DoesNotContain("\"oneOf\"", raw, StringComparison.Ordinal);
    Assert.Empty(FindIntegerUnions(document));
  }

  /// <summary>
  /// 递归查找 <c>["integer","string"]</c> 形状的残留数组：归一脚本必须把所有 int32 联合改写为 <c>"integer"</c>。
  /// </summary>
  /// <param name="node">待检查的 JSON 节点。</param>
  /// <param name="location">当前节点的路径，仅用于失败时的定位信息。</param>
  /// <returns>残留节点的位置列表；无残留时为空。</returns>
  private static List<string> FindIntegerUnions(JsonNode? node, string location = "$")
  {
    List<string> found = [];
    switch (node)
    {
      case JsonArray array:
        bool hasInteger = array.Any(item => IsStringValue(item, "integer"));
        bool hasString = array.Any(item => IsStringValue(item, "string"));
        if (array.Count == 2 && hasInteger && hasString) found.Add(location);
        for (int index = 0; index < array.Count; index++) found.AddRange(FindIntegerUnions(array[index], $"{location}[{index}]"));
        break;
      case JsonObject jsonObject:
        foreach ((string key, JsonNode? value) in jsonObject) found.AddRange(FindIntegerUnions(value, $"{location}.{key}"));
        break;
    }

    return found;
  }

  /// <summary>
  /// 判断 JSON 节点是否为指定字符串值的 <see cref="JsonValue"/>。
  /// </summary>
  /// <param name="node">待判断节点。</param>
  /// <param name="expected">期望的字符串值。</param>
  /// <returns>节点是字符串且与期望值相等时为 <see langword="true"/>。</returns>
  private static bool IsStringValue(JsonNode? node, string expected) =>
    node is JsonValue value && value.TryGetValue(out string? text) && text == expected;

  /// <summary>
  /// 读取 OpenAPI JsonNode 扩展中的字符串数组。
  /// </summary>
  /// <param name="schema">枚举架构。</param>
  /// <param name="extensionName">扩展名称。</param>
  /// <returns>扩展中的字符串值。</returns>
  private static string[] ExtensionValues(OpenApiSchema schema, string extensionName)
  {
    JsonNodeExtension extension = Assert.IsType<JsonNodeExtension>(schema.Extensions![extensionName]);
    JsonArray values = Assert.IsType<JsonArray>(extension.Node);
    return [.. values.Select(value => value!.GetValue<string>())];
  }
}
