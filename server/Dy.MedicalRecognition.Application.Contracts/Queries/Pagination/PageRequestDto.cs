using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;

/// <summary>
/// 服务端分页的请求分页对象：业务筛选之外以内嵌 <c>page</c> 承载页码与页容量。
/// </summary>
/// <remarks>
/// 页码为一基，页容量取值域为 1 到 200；窗口校验在应用层、第一次仓储访问之前完成。
/// 不出现 <c>HasNext</c>、<c>Pagination</c>、<c>SkipCount</c>、<c>PageCount</c> 等派生字段。
/// </remarks>
public sealed record PageRequestDto
{
  /// <summary>页码，一基；最小值为 1。</summary>
  [Range(1, int.MaxValue, ErrorMessage = "参数校验失败：页码不能小于 1。")]
  public int PageIndex { get; init; }
  /// <summary>页容量；取值域 1 到 200。</summary>
  [Range(1, 200, ErrorMessage = "参数校验失败：页容量必须在 1 到 200 之间。")]
  public int PageSize { get; init; }
}
