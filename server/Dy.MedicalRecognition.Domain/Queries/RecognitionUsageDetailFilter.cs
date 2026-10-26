using Dy.MedicalRecognition.Domain.Share.Enums;
namespace Dy.MedicalRecognition.Domain.Queries;
/// <summary>使用方互认使用明细的查询条件。</summary>
public sealed record RecognitionUsageDetailFilter
{
  public DateTime PeriodStart { get; init; }
  public DateTime PeriodEnd { get; init; }
  public string? OrganizationCode { get; init; }
  public string? HospitalCode { get; init; }
  public string? BranchCode { get; init; }
  public string? SourceOrganizationCode { get; init; }
  public string? SourceHospitalCode { get; init; }
  public string? SourceBranchCode { get; init; }
  public string? Keyword { get; init; }
  public RecognitionNonAdoptionReason? NonAdoptionReason { get; init; }
  public MedicalItemType? ItemType { get; init; }
  public string? CategoryName { get; init; }
  public string? GroupName { get; init; }
  public string? RecognitionDeptId { get; init; }
  public string? StandardProjectCode { get; init; }
}
