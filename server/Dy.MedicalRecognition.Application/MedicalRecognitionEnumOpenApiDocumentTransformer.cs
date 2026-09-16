using System.Text.Json.Nodes;
using Dy.MedicalRecognition.Application.Queries.EnumMetadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Dy.MedicalRecognition.Application;

/// <summary>
/// 使用 SourceGen 静态描述器补充公共枚举的 OpenAPI 数值、名称和中文说明。
/// </summary>
/// <remarks>
/// 框架为枚举只生成 <c>{"type":"integer"}</c>，缺少取值集合；生成端据此无法把可空枚举映射为数值，
/// 会把请求体写成空对象 <c>{}</c>。本转换器按登记的描述器补齐 <c>enum</c>、<c>x-enumNames</c> 与
/// <c>x-enumDescriptions</c>，使契约与实际线形状一致。
/// </remarks>
internal sealed class MedicalRecognitionEnumOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
  /// <inheritdoc/>
  public Task TransformAsync(
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken)
  {
    if (document.Components?.Schemas is null) return Task.CompletedTask;

    foreach ((string enumName, IReadOnlyList<IEnumDescriptor> descriptors) in
             MedicalRecognitionEnumDescriptorRegistry.Descriptors)
    {
      if (!document.Components.Schemas.TryGetValue(enumName, out IOpenApiSchema? schema) ||
          schema is not OpenApiSchema concreteSchema)
        continue;

      concreteSchema.Enum = descriptors
        .Select(descriptor => (JsonNode)JsonValue.Create(descriptor.Value)!)
        .ToList();
      concreteSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
      concreteSchema.Extensions["x-enumNames"] = new JsonNodeExtension(
        new JsonArray(descriptors.Select(descriptor => JsonValue.Create(descriptor.Name)).ToArray()));
      concreteSchema.Extensions["x-enumDescriptions"] = new JsonNodeExtension(
        new JsonArray(descriptors.Select(descriptor => JsonValue.Create(descriptor.Description)).ToArray()));
    }

    return Task.CompletedTask;
  }
}
