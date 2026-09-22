namespace Dy.MedicalRecognition.Application.Contracts.ReadModels;

/// <summary>
/// 统计读模型承载的接收方组织、医院与院区编码及回填名称。
/// </summary>
public sealed record ReceiverOrganizationReadModel
{
  /// <summary>接收组织编码。</summary>
  public string OrganizationCode { get; init; } = string.Empty;
  /// <summary>接收组织名称；按当页涉及编码批量回填。</summary>
  public string OrganizationName { get; init; } = string.Empty;
  /// <summary>接收医院编码。</summary>
  public string HospitalCode { get; init; } = string.Empty;
  /// <summary>接收医院名称；按当页涉及编码批量回填。</summary>
  public string HospitalName { get; init; } = string.Empty;
  /// <summary>接收院区编码。</summary>
  public string BranchCode { get; init; } = string.Empty;
  /// <summary>接收院区名称；按当页涉及编码批量回填。</summary>
  public string BranchName { get; init; } = string.Empty;
}
