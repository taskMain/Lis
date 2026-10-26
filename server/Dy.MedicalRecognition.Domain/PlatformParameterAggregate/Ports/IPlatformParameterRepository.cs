using Dy.Core.Abstractions.Domain;

namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate.Ports;

/// <summary>
/// 平台参数的持久化端口：按编码读取、全量读取、新增与更新。
/// </summary>
/// <remarks>只做数据读写并返回行数或查询结果，不判断参数值是否合法；取值规则由领域层判定。</remarks>
public interface IPlatformParameterRepository : IRepository
{
  /// <summary>
  /// 生成一个新的参数主键。
  /// </summary>
  /// <returns>新的主键标识。</returns>
  Guid CreateGuid();

  /// <summary>
  /// 按参数编码读取单条参数，不筛选启用状态。
  /// </summary>
  /// <param name="parameterCode">参数编码。</param>
  /// <returns>命中的参数；不存在时返回 <see langword="null"/>。</returns>
  Task<PlatformParameter?> GetPlatformParameterByCodeAsync(string parameterCode);

  /// <summary>
  /// 读取全部平台参数，按参数编码升序返回。
  /// </summary>
  /// <returns>参数集合；没有记录时为空集合。</returns>
  Task<IReadOnlyList<PlatformParameter>> QueryAllPlatformParameterAsync();

  /// <summary>
  /// 新增一条平台参数记录。
  /// </summary>
  /// <param name="parameter">待写入的参数实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreatePlatformParameterAsync(PlatformParameter parameter);

  /// <summary>
  /// 按主键更新参数值、说明、启用状态与操作字段。
  /// </summary>
  /// <param name="parameter">待更新的参数实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdatePlatformParameterAsync(PlatformParameter parameter);
}