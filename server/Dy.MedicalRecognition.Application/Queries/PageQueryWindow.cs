namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 分页窗口：把一基页码与页容量换算为仓储需要的偏移与页容量，并在读取仓储之前完成校验。
/// </summary>
/// <remarks>
/// 本类型是应用层内部投影类型，不出现在对外契约；页码小于 1、页容量小于 1 或超过上限时按业务拒绝抛出。
/// </remarks>
internal readonly record struct PageQueryWindow
{
  /// <summary>页容量的上限；与对外契约声明的取值域一致。</summary>
  internal const int MaxPageSize = 200;

  /// <summary>
  /// 按请求的页码与页容量建立窗口并校验取值域。
  /// </summary>
  /// <param name="pageIndex">一基页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <exception cref="InvalidOperationException">页码小于 1、页容量小于 1 或超过上限时抛出。</exception>
  private PageQueryWindow(int pageIndex, int pageSize)
  {
    PageIndex = pageIndex;
    PageSize = pageSize;
    SkipCount = (pageIndex - 1) * pageSize;
  }

  /// <summary>本次查询的页码。</summary>
  internal int PageIndex { get; }
  /// <summary>本次查询的页容量。</summary>
  internal int PageSize { get; }
  /// <summary>本次查询的偏移量，等于（页码减一）乘以页容量。</summary>
  internal int SkipCount { get; }

  /// <summary>
  /// 建立分页窗口并校验取值域。
  /// </summary>
  /// <param name="pageIndex">一基页码。</param>
  /// <param name="pageSize">页容量。</param>
  /// <returns>已校验的分页窗口。</returns>
  /// <exception cref="InvalidOperationException">页码小于 1、页容量小于 1 或超过上限时抛出，此时不发生仓储访问。</exception>
  internal static PageQueryWindow Create(int pageIndex, int pageSize)
  {
    if (pageIndex < 1) throw new InvalidOperationException("业务拒绝：页码不能小于 1。");
    if (pageSize < 1 || pageSize > MaxPageSize) throw new InvalidOperationException($"业务拒绝：页容量必须在 1 到 {MaxPageSize} 之间。");

    return new PageQueryWindow(pageIndex, pageSize);
  }
}
