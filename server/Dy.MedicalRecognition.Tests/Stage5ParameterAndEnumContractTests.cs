using System.ComponentModel;
using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Application.Queries.EnumMetadata;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 5 的契约与静态面：处理结果与引用事实的项目编码列已删除（项目编码由互认匹配项标识反查匹配项取得）、
/// 三个系统参数的编码常量声明在各自读取入口、互认结果与不采纳原因两个枚举已补中文说明与描述器特性并登记。
/// </summary>
/// <remarks>
/// 全部判据按反射读取公开成员、特性与私有静态常量，不解析源码文本、不做正则匹配：
/// 源码里的注释与字符串字面量不会造成误判，成员的访问级别与声明位置由反射本身给出。
/// 参数读取行为（读取不传组织与医院与院区维度、三类取值异常的严格失败语义）属票 03 与票 05 的取证范围，
/// 本文件只声明常量面。不依赖数据库与宿主。
/// </remarks>
public sealed class Stage5ParameterAndEnumContractTests
{
  /// <summary>
  /// 本院报告匹配开关的参数编码。
  /// </summary>
  private const string OwnHospitalMatchSwitchCode = "own_hospital_match_switch";

  /// <summary>
  /// 本院报告排除时长的参数编码。
  /// </summary>
  private const string OwnHospitalExcludeHoursCode = "own_hospital_exclude_hours";

  /// <summary>
  /// 引用详情有效时长的参数编码。
  /// </summary>
  private const string CitationDetailValidHoursCode = "citation_detail_valid_hours";

  /// <summary>
  /// 四个已删除项目编码列的契约类型：两个领域实体与两个对外 DTO。
  /// </summary>
  private static readonly Type[] ProjectCodeColumnRemovedTypes =
  [
    typeof(RecognitionProcessingResult),
    typeof(RecognitionReference),
    typeof(RecognitionProcessingResultDto),
    typeof(RecognitionReferenceDto)
  ];

  /// <summary>
  /// 两个待登记的枚举类型及其在用例中的出现顺序。
  /// </summary>
  private static readonly Type[] Stage5ContractEnums = [typeof(RecognitionResult), typeof(RecognitionNonAdoptionReason)];

  /// <summary>
  /// 处理结果表与引用事实表都不保存项目编码：两者的项目编码均由互认匹配项标识反查匹配项取得。
  /// 该属性一旦回到领域实体或对外 DTO 上，写入方会重新携带一份可与匹配项不一致的冗余编码。
  /// </summary>
  [Fact]
  public void Processing_result_and_reference_contracts_no_longer_carry_the_project_code()
  {
    foreach (Type type in ProjectCodeColumnRemovedTypes)
    {
      string[] properties = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name)];

      // 先证明扫描确实读到了成员：类型上没有公开实例属性时，下面的负向断言会自动成立而失去判别力。
      Assert.NotEmpty(properties);
      Assert.DoesNotContain("StandardProjectCode", properties);
    }
  }

  /// <summary>
  /// 两个枚举的每个成员都必须声明中文说明，枚举类型必须声明描述器特性：
  /// 描述器缺特性时不生成，注册表与 OpenAPI 随之失去取值集合，中文说明也取不到。
  /// </summary>
  /// <param name="enumType">待检查的枚举类型。</param>
  [Theory]
  [MemberData(nameof(Stage5ContractEnumTypes))]
  public void Stage5_enums_declare_descriptor_attribute_and_chinese_member_descriptions(Type enumType)
  {
    Assert.True(
      enumType.GetCustomAttributes(inherit: false).Any(attribute => attribute.GetType().Name is "EnumDescriptor" or "EnumDescriptorAttribute"),
      $"{enumType.Name} 未声明 [EnumDescriptor]。");

    string[] members = [.. Enum.GetNames(enumType)];
    Assert.NotEmpty(members);

    foreach (string member in members)
    {
      DescriptionAttribute? description = enumType.GetField(member)!.GetCustomAttribute<DescriptionAttribute>();
      Assert.True(description is not null, $"{enumType.Name}.{member} 未声明 [Description]。");

      string text = description!.Description;
      Assert.False(string.IsNullOrWhiteSpace(text), $"{enumType.Name}.{member} 的说明为空白。");
      Assert.True(ContainsChinese(text), $"{enumType.Name}.{member} 的说明不是中文：{text}。");
    }
  }

  /// <summary>
  /// 两个枚举都必须出现在描述器注册表的键集合中：只声明特性而不登记时，
  /// 枚举元数据查询查不到该枚举，OpenAPI 也只声明 <c>integer</c>。
  /// </summary>
  [Theory]
  [MemberData(nameof(Stage5ContractEnumTypes))]
  public void Stage5_enums_are_registered_in_the_descriptor_registry(Type enumType)
  {
    Assert.Contains(enumType.Name, MedicalRecognitionEnumDescriptorRegistry.Descriptors.Keys);
    Assert.NotEmpty(MedicalRecognitionEnumDescriptorRegistry.Descriptors[enumType.Name]);
  }

  /// <summary>
  /// 本院报告匹配开关与本院报告排除时长两个参数由写侧应用服务在匹配查询入口读取，
  /// 编码以私有常量声明在该入口类内、取值为小写下划线形态。
  /// </summary>
  /// <param name="fieldName">常量字段名。</param>
  /// <param name="expectedValue">期望的常量取值。</param>
  [Theory]
  [InlineData("OwnHospitalMatchSwitch", OwnHospitalMatchSwitchCode)]
  [InlineData("OwnHospitalExcludeHours", OwnHospitalExcludeHoursCode)]
  public void Match_query_entry_declares_the_two_own_hospital_parameter_codes(string fieldName, string expectedValue) =>
    AssertPrivateConstantString(typeof(MedicalRecognitionReportAppService), fieldName, expectedValue);

  /// <summary>
  /// 引用详情有效时长参数由查询应用服务在引用详情查询入口读取，编码以私有常量声明在该入口类内。
  /// </summary>
  [Fact]
  public void Citation_detail_entry_declares_the_valid_hours_parameter_code() =>
    AssertPrivateConstantString(typeof(MedicalRecognitionReportQueryAppService), "CitationDetailValidHours", CitationDetailValidHoursCode);

  /// <summary>
  /// 两个枚举类型的用例数据。
  /// </summary>
  /// <returns>枚举类型作为用例参数的集合。</returns>
  public static TheoryData<Type> Stage5ContractEnumTypes() => new() { typeof(RecognitionResult), typeof(RecognitionNonAdoptionReason) };

  /// <summary>
  /// 核对指定类型内存在取值与方法名的常量字段。
  /// </summary>
  /// <param name="declaringType">常量应声明在其中的类型；必须是声明处本身，不含继承成员。</param>
  /// <param name="fieldName">常量字段名。</param>
  /// <param name="expectedValue">期望的常量取值。</param>
  private static void AssertPrivateConstantString(Type declaringType, string fieldName, string expectedValue)
  {
    FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

    Assert.True(field is not null, $"{declaringType.Name} 内不存在静态字段 {fieldName}。");
    Assert.True(field!.IsLiteral, $"{declaringType.Name}.{fieldName} 不是编译期常量。");
    Assert.Equal(typeof(string), field.FieldType);
    Assert.Equal(expectedValue, field.GetRawConstantValue());
  }

  /// <summary>
  /// 判断文本是否含有中日韩统一表意文字，用于识别中文说明。
  /// </summary>
  /// <param name="text">待判定的文本。</param>
  /// <returns>含有表意文字时为 <see langword="true"/>。</returns>
  private static bool ContainsChinese(string text) =>
    text.Any(character => character is >= '\u4e00' and <= '\u9fff');
}
