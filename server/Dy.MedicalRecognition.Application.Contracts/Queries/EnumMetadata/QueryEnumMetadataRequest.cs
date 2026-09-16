using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

/// <summary>
/// 枚举元数据查询请求。
/// </summary>
public sealed record QueryEnumMetadataRequest
{
    /// <summary>
    /// 白名单枚举名称；只接受已登记枚举的名称，按大小写精确匹配。
    /// </summary>
    [Required(ErrorMessage = "参数校验失败：枚举名称不能为空。")]
    public string EnumName { get; init; } = string.Empty;
}
