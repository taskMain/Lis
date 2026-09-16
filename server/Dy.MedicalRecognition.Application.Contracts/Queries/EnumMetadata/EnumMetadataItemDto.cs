namespace Dy.MedicalRecognition.Application.Contracts.Queries.EnumMetadata;

/// <summary>
/// 枚举元数据条目：白名单枚举的单个取值及其展示资料。
/// </summary>
public sealed record EnumMetadataItemDto
{
    /// <summary>
    /// 稳定数值；与枚举成员的声明值一致，不随名称变化。
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    /// 成员名称；用于排查与日志，不作为展示文案。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 中文说明；来自枚举成员的 <c>[Description]</c>，是前端展示文案的唯一来源。
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
