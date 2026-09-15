using Dy.Core.Abstractions.Http;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>提供标准医疗项目目录的四项只读查询。</summary>
public interface IMedicalRecognitionReportQueryAppService : IApplicationService
{
  /// <summary>按项目类型查询分类列表。</summary>
  Task<IEnumerable<MedicalStandardCategoryListReadModel>> QueryMedicalStandardCategoryListAsync(MedicalStandardCategoryListQueryRequest request);

  /// <summary>按分类查询分组列表。</summary>
  Task<IEnumerable<MedicalStandardGroupListReadModel>> QueryMedicalStandardGroupListAsync(MedicalStandardGroupListQueryRequest request);

  /// <summary>按分类、分组、编码、名称和启用状态查询标准项目。</summary>
  Task<IEnumerable<MedicalStandardItemListReadModel>> QueryMedicalStandardItemListAsync(MedicalStandardItemListQueryRequest request);

  /// <summary>查询仅三级状态均启用的层级目录。</summary>
  Task<EffectiveMedicalStandardCatalogReadModel> QueryEffectiveMedicalStandardCatalogAsync(EffectiveMedicalStandardCatalogQueryRequest request);
}
