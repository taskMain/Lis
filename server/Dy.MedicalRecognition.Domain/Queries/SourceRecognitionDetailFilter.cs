using Dy.MedicalRecognition.Domain.Share.Enums;
namespace Dy.MedicalRecognition.Domain.Queries;
/// <summary>提供方医院被互认明细的查询条件。</summary>
public sealed record SourceRecognitionDetailFilter
{
  public DateTime PeriodStart { get; init; }
  public DateTime PeriodEnd { get; init; }
  public string? SourceOrganizationCode { get; init; }
  public string? SourceHospitalCode { get; init; }
  public string? SourceBranchCode { get; init; }
  public string? ReceiverOrganizationCode { get; init; }
  public string? ReceiverHospitalCode { get; init; }
  public string? ReceiverBranchCode { get; init; }
  public MedicalItemType? ItemType { get; init; }
  public string? CategoryName { get; init; }
  public string? GroupName { get; init; }
  public string? Keyword { get; init; }
  public string? StandardProjectCode { get; init; }
}
