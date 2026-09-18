namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 仓储返回的当页切片：当页数据与满足筛选条件的总数。
/// </summary>
/// <remarks>本类型是仓储到应用层的内部投影类型，不出现在对外契约。</remarks>
/// <typeparam name="T">当页数据项类型。</typeparam>
/// <param name="Items">当页数据。</param>
/// <param name="TotalCount">满足同一套筛选条件的总记录数。</param>
internal readonly record struct PageSlice<T>(IReadOnlyList<T> Items, long TotalCount);
