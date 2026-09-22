namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 统计读模型承载的来源方组织、医院与院区编码及回填名称。
/// </summary>
public sealed record SourceOrganizationReadModel
{
  /// <summary>来源组织编码。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>来源组织名称；按当页涉及编码批量回填。</summary>
  public string OrganizationName { get; init; } = string.Empty;
  /// <summary>来源医院编码。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>来源医院名称；按当页涉及编码批量回填。</summary>
  public string HospitalName { get; init; } = string.Empty;
  /// <summary>来源院区编码。</summary>
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>来源院区名称；按当页涉及编码批量回填。</summary>
  public string BranchName { get; init; } = string.Empty;
}
