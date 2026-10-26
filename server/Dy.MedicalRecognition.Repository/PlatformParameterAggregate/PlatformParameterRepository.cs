using Dy.Core.Abstractions.Data;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate;
using Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;

namespace Dy.MedicalRecognition.Repository.PlatformParameterAggregate;

/// <summary>
/// 平台参数的持久化实现：按参数编码与主键读写 mrec_platform_parameter。
/// </summary>
/// <remarks>
/// 只发送映射文件中已有的语句，不判断参数值是否合法、目标是否存在；
/// 参数编码唯一性由数据库唯一索引兜底，保存前查重由应用层与领域层完成。
/// </remarks>
public sealed class PlatformParameterRepository : IPlatformParameterRepository, IHasDataMapper
{
  /// <summary>平台参数语句集的作用域名。</summary>
  private const string ParameterScope = "PlatformParameter";

  /// <summary>
  /// 框架写入的映射器实例，供本仓储的全部读写方法使用。
  /// </summary>
  private IDataMapper dataMapper = default!;

  /// <summary>
  /// 框架写入的映射器实例，供本仓储的全部读写方法使用。
  /// </summary>
  public IDataMapper DataMapper { get => dataMapper; set => dataMapper = value; }

  /// <inheritdoc/>
  public Guid CreateGuid() => dataMapper.CreateGuid();

  /// <inheritdoc/>
  public async Task<PlatformParameter?> GetPlatformParameterByCodeAsync(string parameterCode) =>
    await dataMapper.QuerySingleAsync<PlatformParameter>(new { ParameterCode = parameterCode }, scope: ParameterScope);

  /// <inheritdoc/>
  public async Task<IReadOnlyList<PlatformParameter>> QueryAllPlatformParameterAsync() =>
    [.. await dataMapper.QueryAsync<PlatformParameter>(new { }, scope: ParameterScope)];

  /// <inheritdoc/>
  public async Task<int> CreatePlatformParameterAsync(PlatformParameter parameter) => await dataMapper.InsertAsync(parameter, scope: ParameterScope);

  /// <inheritdoc/>
  public async Task<int> UpdatePlatformParameterAsync(PlatformParameter parameter) => await dataMapper.UpdateAsync(parameter, scope: ParameterScope);
}