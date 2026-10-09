using System.Text.Json.Nodes;
using Dy.MedicalRecognition.Tests.Architecture;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 阶段 3 重开轮累计生成的输入面与生成端判据：冻结 OpenAPI 中两个金额查询请求的可选关键字，
/// 以及生成出的请求模型与序列化函数是否携带同名属性。
/// </summary>
/// <remarks>
/// 对应 [票 10「两金额页与互认项目页统一重开轮」](../../docs/plans/005-阶段3-互认项目金额维护/阶段3-Tickets/10-两金额页与互认项目页统一重开轮.md)
/// 第 13 行「按累计规则重新生成 API Client」，适用范围为本票 API Client 棒；判据只读取冻结输入与生成物文件，
/// 不连接数据库、不发起网络请求。全部为静态证据。
/// </remarks>
public sealed class Stage3ApiContractTests
{
  /// <summary>
  /// 两个金额查询的可选关键字必须同时出现在冻结输入与生成端：输入侧是可空属性且不在必填集合，
  /// 生成侧的请求模型与序列化函数携带同名属性；任一环节缺失时，页面按新设计提交的关键字会被静默丢弃。
  /// </summary>
  /// <remarks>
  /// 该用例是累计生成的失败基线：重生成之前输入文档与生成端都没有关键字，用例判定为失败。
  /// 属性与序列化按声明片段整体截取判定，片段缺失、被复制成两处或顺序漂移都判定为失败，不依赖属性在文件中的行号。
  /// </remarks>
  [Fact]
  public void Frozen_openapi_and_generated_model_declare_optional_keyword_for_amount_queries()
  {
    string repositoryRoot = SourceSyntaxGuard.FindRepositoryRoot();
    JsonNode document = JsonNode.Parse(File.ReadAllText(Path.Combine(
      repositoryRoot, "client", "packages", "api-client-medical-recognition", "openapi",
      "medical-recognition.openapi.json")))!;

    foreach (string schemaName in new[] { "RecognitionAmountListQueryRequest", "BranchRecognitionAmountListQueryRequest" })
    {
      JsonNode? schema = document["components"]!["schemas"]?[schemaName];
      Assert.True(schema is not null, $"缺少 {schemaName} 声明。");
      JsonNode? properties = schema!["properties"];
      Assert.True(properties is not null, $"{schemaName} 缺少属性集合。");
      JsonNode? keyword = properties!["keyword"];
      Assert.True(keyword is not null, $"{schemaName} 的属性集合缺少 keyword。");
      JsonNode? keywordType = keyword!["type"];
      Assert.True(keywordType is not null, $"{schemaName} 的 keyword 缺少类型声明。");
      Assert.Equal(
        ["null", "string"],
        keywordType!.AsArray().Select(node => node!.GetValue<string>()).ToArray());

      string[] required = schema["required"] is JsonNode requiredNode
        ? requiredNode.AsArray().Select(node => node!.GetValue<string>()).ToArray()
        : [];
      Assert.DoesNotContain("keyword", required);
    }

    string models = File.ReadAllText(Path.Combine(
      repositoryRoot, "client", "packages", "api-client-medical-recognition", "src", "models", "index.ts"));

    foreach ((string modelName, string serializerName, string parameterName) in new[]
    {
      ("RecognitionAmountListQueryRequest", "serializeRecognitionAmountListQueryRequest", "recognitionAmountListQueryRequest"),
      ("BranchRecognitionAmountListQueryRequest", "serializeBranchRecognitionAmountListQueryRequest", "branchRecognitionAmountListQueryRequest")
    })
    {
      Assert.Contains(
        "keyword?: string | null;",
        DeclarationBlockAfter(models, $"export interface {modelName} "),
        StringComparison.Ordinal);
      Assert.Contains(
        $"writer.writeStringValue(\"keyword\", {parameterName}.keyword);",
        DeclarationBlockAfter(models, $"export function {serializerName}("),
        StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// 截取源码中某个声明片段（从声明标记到下一个顶格右括号），供生成物断言只判定该声明的内部内容。
  /// </summary>
  /// <remarks>标记缺失或在全文出现多处都判定为失败，避免取到别处的同名声明而让块内断言失去判别力。</remarks>
  /// <param name="source">生成物源码全文。</param>
  /// <param name="marker">声明起始标记，例如 <c>export interface Xxx </c> 或 <c>export function serializeXxx(</c>。</param>
  /// <returns>声明标记起始处到片段结束的源码文本。</returns>
  private static string DeclarationBlockAfter(string source, string marker)
  {
    int start = source.IndexOf(marker, StringComparison.Ordinal);
    Assert.True(start >= 0, $"未找到声明片段：{marker}");
    int occurrences = 0;
    for (int index = source.IndexOf(marker, StringComparison.Ordinal); index >= 0; index = source.IndexOf(marker, index + marker.Length, StringComparison.Ordinal))
    {
      occurrences++;
    }

    Assert.True(occurrences == 1, $"声明片段应恰好一处，实际 {occurrences} 处：{marker}");
    int end = source.IndexOf("\n}", start, StringComparison.Ordinal);
    Assert.True(end > start, $"声明片段未在预期位置结束：{marker}");
    return source[start..end];
  }

  /// <summary>
  /// 声明片段截取的自校验：标记缺失、同名声明出现两处、关键字只出现在目标声明之外三种形态，
  /// 必须分别表现为“截取失败”“截取失败”“块内取不到关键字”，否则块内断言可能对错误范围的内容给出通过结论。
  /// </summary>
  /// <param name="marker">要截取的声明标记。</param>
  /// <param name="source">合成源码。</param>
  /// <param name="blockShouldContainKeyword">期望截取出的片段内是否含 <c>keyword</c>。</param>
  [Theory]
  [InlineData("export interface Missing ", "export interface A {\n  a?: string | null;\n}\n", false)]
  [InlineData("export interface A ", "export interface A {\n  a?: string | null;\n}\nexport interface A {\n  keyword?: string | null;\n}\n", false)]
  [InlineData("export interface A ", "export interface A {\n  a?: string | null;\n}\nexport interface B {\n  keyword?: string | null;\n}\n", false)]
  public void Declaration_block_extraction_rejects_missing_duplicate_and_out_of_block_content(string marker, string source, bool blockShouldContainKeyword)
  {
    // 0 处或 2 处标记都必须由截取本身判定为失败，块内断言因此不会对错误范围的内容给出通过结论。
    if (source.Split(marker).Length - 1 != 1)
    {
      Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => DeclarationBlockAfter(source, marker));
      return;
    }

    string block = DeclarationBlockAfter(source, marker);
    Assert.Equal(blockShouldContainKeyword, block.Contains("keyword", StringComparison.Ordinal));
  }
}
