using Dy.Core.Abstractions.Http;

namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>提供标准医疗项目目录四项只读查询，以及互认项目配置列表查询。</summary>
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

  /// <summary>按组织、可选标准项目编码与可选配置状态查询标准项目互认配置列表。</summary>
  /// <remarks>请求组织必须等于可信当前组织，否则拒绝且不返回该组织的数据。</remarks>
  /// <param name="request">组织编码必填，标准项目编码与配置状态可选。</param>
  /// <returns>互认配置只读模型集合；无匹配配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织编码缺失或为空白、标准项目编码为空白，或配置状态不是已定义枚举值时抛出。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织，或请求组织与可信当前组织不一致时抛出。</exception>
  Task<IEnumerable<RecognitionProjectConfigurationReadModel>> QueryRecognitionProjectConfigurationListAsync(RecognitionProjectConfigurationListQueryRequest request);
}
