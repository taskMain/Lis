using Dy.Core.Abstractions.Http;

using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;
namespace Dy.MedicalRecognition.Application.Contracts.Queries;

/// <summary>提供标准医疗项目目录四项只读查询、互认项目配置列表查询、两个互认项目金额列表查询，以及报告管理端的报告列表、版本列表与版本详情查询。</summary>
public partial interface IMedicalRecognitionReportQueryAppService : IApplicationService
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

  /// <summary>按请求的组织、医院、院区与可选标准项目编码查询互认项目金额列表（平台管理员入口）。</summary>
  /// <remarks>
  /// 组织、医院、院区按请求使用，服务端校验其存在、启用与父子归属，不要求等于可信上下文组织；
  /// 行集由该组织已建立的互认配置驱动，未配置金额的行按未配置返回而不是零元。
  /// </remarks>
  /// <param name="request">组织、医院、院区必填，标准项目编码可选。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织、医院或院区缺失或为空白，或标准项目编码为空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">组织、医院或院区不存在、已停用或父子归属不匹配时抛出；此时不返回该范围的数据，也不降级为空集合。</exception>
  Task<IEnumerable<RecognitionAmountReadModel>> QueryRecognitionAmountListAsync(RecognitionAmountListQueryRequest request);

  /// <summary>按可信上下文组织与医院、请求院区及可选标准项目编码查询本院区互认项目金额列表（医院管理员入口）。</summary>
  /// <remarks>
  /// 组织与医院只取自可信上下文，缺失即拒绝；请求院区必须属于可信医院，不属于即拒绝，不返回其他医院的数据、也不降级为空集合。
  /// </remarks>
  /// <param name="request">院区必填，标准项目编码可选；请求不提交组织与医院。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">院区缺失或为空白，或标准项目编码为空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信组织或可信医院，或请求院区不存在、已停用、不属于可信医院时抛出。</exception>
  Task<IEnumerable<RecognitionAmountReadModel>> QueryBranchRecognitionAmountListAsync(BranchRecognitionAmountListQueryRequest request);
}
