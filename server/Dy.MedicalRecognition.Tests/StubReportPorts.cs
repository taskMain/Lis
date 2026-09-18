using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 报告 PDF 存储端口的手写替身：只满足写入口构造，不承载任何文件能力。
/// </summary>
/// <remarks>阶段 1 至阶段 3 的写入口测试不覆盖报告提交与下载，因此三个成员调用即失败，避免用例静默走过文件路径。</remarks>
internal sealed class StubReportPdfFileStore : IReportPdfFileStore
{
  /// <summary>未使用成员统一提示。</summary>
  private const string UnusedMember = "本测试未使用报告 PDF 存储端口。";

  /// <inheritdoc/>
  public Task<string> SaveAsync(Stream content) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<Stream> OpenReadAsync(string fileKey) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task DeleteAsync(string fileKey) => throw new NotSupportedException(UnusedMember);
}

/// <summary>
/// 报告只读查询端口的手写替身：只满足写入口构造，不承载任何查询能力。
/// </summary>
/// <remarks>阶段 1 至阶段 3 的用例不覆盖报告查询，因此全部成员调用即失败。</remarks>
internal sealed class StubReportQueryRepository : IMedicalRecognitionReportQueryRepository
{
  /// <summary>未使用成员统一提示。</summary>
  private const string UnusedMember = "本测试未使用报告只读查询端口。";

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);
}

/// <summary>
/// 报告只读查询端口的手写替身：只承载下载路径需要的版本文件信息，其余成员调用即失败。
/// </summary>
/// <remarks>
/// 下载路径只读取「版本定位与来源归属」一处投影，因此替身只提供该投影，
/// 使下载用例不必构造完整查询链路，同时避免其他查询被静默走过。
/// </remarks>
internal sealed class SyntheticReportPorts : IMedicalRecognitionReportQueryRepository
{
  /// <summary>未使用成员统一提示。</summary>
  private const string UnusedMember = "本测试只覆盖报告版本文件信息查询。";

  /// <summary>
  /// 版本文件信息；为 <see langword="null"/> 时按版本不存在返回。
  /// </summary>
  public MedicalReportVersionFileItem? VersionFile { get; init; }

  /// <inheritdoc/>
  public Task<MedicalReportVersionFileItem?> GetMedicalReportVersionFileAsync(Guid reportId, Guid reportVersionId) =>
    Task.FromResult(VersionFile is not null && VersionFile.ReportId == reportId && VersionFile.ReportVersionId == reportVersionId
      ? VersionFile
      : null);

  /// <inheritdoc/>
  public Task<MedicalReportScopeItem?> GetMedicalReportScopeAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalRecognitionReportDetailCommon?> GetMedicalReportVersionCommonAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<LaboratoryReportContentItem?> GetLaboratoryReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryResultItemView>> QueryLaboratoryResultItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratoryBacteriaResultItem>> QueryLaboratoryBacteriaResultsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<LaboratorySusceptibilityItem>> QueryLaboratorySusceptibilitiesAsync(Guid bacteriaResultId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<ExaminationReportContentItem?> GetExaminationReportContentAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationItemView>> QueryExaminationItemsAsync(Guid reportVersionId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<ExaminationSiteView>> QueryExaminationSitesAsync(Guid examinationItemId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardCategoryListItem>> QueryMedicalStandardCategoryListAsync(MedicalItemType? itemType) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardGroupListItem>> QueryMedicalStandardGroupListAsync(Guid? categoryId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalStandardItemListItem>> QueryMedicalStandardItemListAsync(Guid? categoryId, Guid? groupId, string? code, string? name, bool? isValid) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<EffectiveMedicalStandardCatalogItem>> QueryEffectiveMedicalStandardCatalogAsync(MedicalItemType? itemType, string? categoryName) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionProjectConfigurationListItem>> QueryRecognitionProjectConfigurationListAsync(string organizationCode, string? standardProjectCode, ConfigurationStatus? configurationStatus) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<RecognitionAmountListItem>> QueryRecognitionAmountListAsync(string organizationCode, string hospitalCode, string branchCode, string? standardProjectCode) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<long> CountMedicalReportListAsync(MedicalReportListFilter filter) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportListItem>> QueryMedicalReportListAsync(MedicalReportListFilter filter, int skipCount, int pageSize) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<IEnumerable<MedicalReportVersionItem>> QueryMedicalReportVersionListAsync(Guid reportId) => throw new NotSupportedException(UnusedMember);

  /// <inheritdoc/>
  public Task<MedicalReportVersionDetailItem?> GetMedicalReportVersionDetailAsync(Guid reportId, Guid reportVersionId) => throw new NotSupportedException(UnusedMember);
}
