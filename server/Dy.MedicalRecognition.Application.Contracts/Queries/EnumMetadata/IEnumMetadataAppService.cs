namespace Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

/// <summary>
/// 枚举元数据查询应用服务：向外交付白名单枚举的取值与中文说明。
/// </summary>
public interface IEnumMetadataAppService : IApplicationService
{
    /// <summary>
    /// 查询白名单枚举的稳定数值、成员名称和中文说明，顺序与枚举声明顺序一致。
    /// </summary>
    /// <param name="request">只带枚举名称的查询请求。</param>
    /// <returns>该枚举的只读元数据集合。</returns>
    /// <exception cref="ArgumentNullException">查询条件为 null 时抛出，此时无法判定要查询的枚举。</exception>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">枚举名称缺失、为空白或不是已登记枚举名称时抛出；此时不降级为空集合。</exception>
    Task<IReadOnlyList<EnumMetadataItemDto>> GetEnumMetadataAsync(QueryEnumMetadataRequest request);
}
