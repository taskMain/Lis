using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Queries.EnumMetadata;

/// <summary>
/// 枚举元数据查询应用服务：按白名单枚举名称交付取值与中文说明。
/// </summary>
/// <remarks>
/// 中文来源是服务端枚举成员上的 <c>[Description]</c>，本服务只把 SourceGen 描述器投影为对外只读模型，
/// 不在应用层重写文案，也不按运行时反射枚举。口径与同组织 <c>Dy.LisCenter</c> 一致。
/// </remarks>
public sealed class EnumMetadataAppService : ApplicationService, IEnumMetadataAppService
{
  /// <summary>
  /// 按枚举名称索引的对外元数据；启动时由登记注册表一次性投影，避免每次请求重建。
  /// </summary>
  private static readonly IReadOnlyDictionary<string, IReadOnlyList<EnumMetadataItemDto>> _enumMetadata =
    new ReadOnlyDictionary<string, IReadOnlyList<EnumMetadataItemDto>>(
      MedicalRecognitionEnumDescriptorRegistry.Descriptors.ToDictionary(
        pair => pair.Key,
        pair => ToMetadata(pair.Value),
        StringComparer.Ordinal));

  /// <inheritdoc/>
  public Task<IReadOnlyList<EnumMetadataItemDto>> GetEnumMetadataAsync(QueryEnumMetadataRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    if (!_enumMetadata.TryGetValue(request.EnumName, out IReadOnlyList<EnumMetadataItemDto>? result))
      throw new ValidationException($"不支持枚举：{request.EnumName}。");

    return Task.FromResult(result);
  }

  /// <summary>
  /// 将静态枚举描述器投影为对外只读元数据，避免跨请求共享的结果被调用方修改。
  /// </summary>
  /// <param name="descriptors">SourceGen 生成的枚举描述器。</param>
  /// <returns>保持枚举声明顺序的只读元数据。</returns>
  private static IReadOnlyList<EnumMetadataItemDto> ToMetadata(IEnumerable<IEnumDescriptor> descriptors) =>
    Array.AsReadOnly(descriptors
      .Select(descriptor => new EnumMetadataItemDto
      {
        Value = descriptor.Value,
        Name = descriptor.Name,
        Description = descriptor.Description
      })
      .ToArray());
}
