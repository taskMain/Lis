namespace Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;

/// <summary>
/// 服务端分页的响应分页信息：页码、页容量与总数。
/// </summary>
/// <remarks>总数为长整型；不出现 <c>HasNext</c>、<c>Pagination</c>、<c>SkipCount</c>、<c>PageCount</c>。</remarks>
public sealed record PageInfoDto
{
  /// <summary>本次返回的页码，与请求一致。</summary>
  public int PageIndex { get; init; }
  /// <summary>本次返回的页容量，与请求一致。</summary>
  public int PageSize { get; init; }
  /// <summary>满足筛选条件的总记录数，长整型。</summary>
  public long TotalCount { get; init; }
}
