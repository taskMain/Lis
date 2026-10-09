using Dy.Core.Abstractions.Http;

using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionAmount;
using Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;
using Dy.MedicalRecognition.Application.Contracts.Queries.TrustedScope;
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

  /// <summary>按组织、可选关键字与可选配置状态查询标准项目互认配置列表。</summary>
  /// <remarks>请求组织必须等于可信当前组织，否则拒绝且不返回该组织的数据。</remarks>
  /// <param name="request">组织编码必填；关键字与配置状态可选，关键字按字面包含同时匹配标准项目编码、名称、所属分类名称与所属分组名称且不区分大小写。</param>
  /// <returns>互认配置只读模型集合；无匹配配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织编码缺失或为空白、关键字为空白，或配置状态不是已定义枚举值时抛出。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织，或请求组织与可信当前组织不一致时抛出。</exception>
  Task<IEnumerable<RecognitionProjectConfigurationReadModel>> QueryRecognitionProjectConfigurationListAsync(RecognitionProjectConfigurationListQueryRequest request);

  /// <summary>查询当前可信组织尚未建立互认配置、且三级均启用的标准项目目录（供新增互认项目选择）。</summary>
  /// <remarks>
  /// 组织只取自可信上下文，不接受调用方指定组织；返回形状与当前有效标准目录一致（项目类型 → 分类 → 分组 → 标准项目），不分页。
  /// 已建立互认配置的项目（含已停用的配置）与三级任一级停用的项目都不返回。
  /// </remarks>
  /// <param name="request">关键字可选；按字面包含同时匹配标准项目编码、名称、所属分类名称与所属分组名称，不区分大小写。</param>
  /// <returns>可选标准项目目录只读模型；没有可新增项目时各层级为空。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">关键字已传入但为空白文本时抛出；空白文本不代表“不过滤”，而是会放大匹配结果的无效条件。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织时抛出。</exception>
  Task<EffectiveMedicalStandardCatalogReadModel> QuerySelectableStandardItemListAsync(SelectableStandardItemListQueryRequest request);

  /// <summary>按可信上下文查询当前可信范围的展示资料；不接收入参，也不返回医院与院区。</summary>
  /// <remarks>
  /// 组织只取自可信上下文，不接受调用方指定组织，也不新建授权组织集合；
  /// 组织名称由服务端按该编码从外部组织服务解析，解析不到或服务失败时按拒绝结束，不返回空值、也不降级为只回编码。
  /// 该查询只读取外部组织数据，不访问本平台数据库。
  /// </remarks>
  /// <returns>可信范围的当前组织编码与名称。</returns>
  /// <exception cref="InvalidOperationException">无法解析可信当前组织，或组织不存在、已停用、名称读取失败时抛出。</exception>
  Task<TrustedOrganizationScopeReadModel> QueryTrustedOrganizationScopeAsync();

  /// <summary>按请求的组织、医院、院区与可选标准项目编码、关键字查询互认项目金额列表（平台管理员入口）。</summary>
  /// <remarks>
  /// 组织、医院、院区按请求使用，服务端校验其存在、启用与父子归属，不要求等于可信上下文组织；
  /// 行集由该组织已建立的互认配置驱动，未配置金额的行按未配置返回而不是零元。
  /// </remarks>
  /// <param name="request">组织、医院、院区必填，标准项目编码与关键字可选；关键字按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，不区分大小写，`%`、`_` 按普通字符处理，缺失或去首尾空白后为空按未提供处理。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">组织、医院或院区缺失或为空白，或标准项目编码为空白时抛出；关键字缺失或去首尾空白后为空不抛出，按未提供处理。</exception>
  /// <exception cref="InvalidOperationException">组织、医院或院区不存在、已停用或父子归属不匹配时抛出；此时不返回该范围的数据，也不降级为空集合。</exception>
  Task<IEnumerable<RecognitionAmountReadModel>> QueryRecognitionAmountListAsync(RecognitionAmountListQueryRequest request);

  /// <summary>按可信上下文组织与医院、请求院区及可选标准项目编码、关键字查询本院区互认项目金额列表（医院管理员入口）。</summary>
  /// <remarks>
  /// 组织与医院只取自可信上下文，缺失即拒绝；请求院区必须属于可信医院，不属于即拒绝，不返回其他医院的数据、也不降级为空集合。
  /// </remarks>
  /// <param name="request">院区必填，标准项目编码与关键字可选；请求不提交组织与医院。关键字按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，不区分大小写，`%`、`_` 按普通字符处理，缺失或去首尾空白后为空按未提供处理。</param>
  /// <returns>金额只读模型集合；该组织未建立互认配置时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定，null 请求没有可校验的属性）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">院区缺失或为空白，或标准项目编码为空白时抛出；关键字缺失或去首尾空白后为空不抛出，按未提供处理。</exception>
  /// <exception cref="InvalidOperationException">无法解析可信组织或可信医院，或请求院区不存在、已停用、不属于可信医院时抛出。</exception>
  Task<IEnumerable<RecognitionAmountReadModel>> QueryBranchRecognitionAmountListAsync(BranchRecognitionAmountListQueryRequest request);
}
