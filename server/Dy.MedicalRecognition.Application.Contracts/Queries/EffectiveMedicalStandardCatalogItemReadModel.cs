namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>当前有效标准目录的项目明细。</summary>
public sealed record EffectiveMedicalStandardCatalogItemReadModel
{
  /// <summary>标准项目编码。</summary>
  public string Code { get; init; } = string.Empty;
  /// <summary>标准项目名称。</summary>
  public string Name { get; init; } = string.Empty;
  /// <summary>备注；为空表示未填写。</summary>
  public string? Remark { get; init; }
  /// <summary>最后实际维护该项目的操作人。</summary>
  public Guid OperId { get; init; }
  /// <summary>最后实际维护该项目的操作时间。</summary>
  public DateTimeOffset OperTime { get; init; }
}
