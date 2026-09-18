namespace Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;

/// <summary>
/// 服务端分页的公共响应：当页数据与分页信息。
/// </summary>
/// <typeparam name="T">当页数据项类型。</typeparam>
public sealed record PageResultDto<T>
{
  /// <summary>当页数据；无匹配时为空数组。</summary>
  public IReadOnlyList<T> Items { get; init; } = [];
  /// <summary>分页信息。</summary>
  public PageInfoDto Page { get; init; } = new();
}
