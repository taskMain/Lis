using System.ComponentModel.DataAnnotations;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准项目目录与互认项目配置的写入口契约。
/// </summary>
/// <remarks>主键、初始启用状态与操作信息由服务端写入。</remarks>
public partial interface IMedicalRecognitionReportAppService : IApplicationService
{
  /// <summary>
  /// 新建标准项目分类。
  /// </summary>
  /// <remarks>分类名称在全部项目类型范围内唯一；分类以启用状态创建，标识与操作信息由服务端写入。</remarks>
  /// <param name="request">新建分类请求。</param>
  /// <returns>是否创建成功。</returns>
  /// <exception cref="ValidationException">分类名称未填写，或项目类型不是已定义的取值。</exception>
  /// <exception cref="InvalidOperationException">登录上下文没有可解析的操作人标识，或分类名称已被其他分类占用、分类未能写入。</exception>
  Task<bool> CreateMedicalStandardCategoryAsync(CreateMedicalStandardCategoryRequest request);

  /// <summary>
  /// 修改标准项目分类的名称、项目类型与备注。
  /// </summary>
  /// <param name="request">修改分类请求。</param>
  /// <returns>是否修改成功。</returns>
  /// <exception cref="ValidationException">分类名称未填写，或项目类型不是已定义的取值。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类标识为空、分类不存在、名称被其他分类占用、分类已有下级分组却要求改变项目类型、修改未能写入。</exception>
  Task<bool> UpdateMedicalStandardCategoryAsync(UpdateMedicalStandardCategoryRequest request);

  /// <summary>
  /// 启用已被停用的标准项目分类。
  /// </summary>
  /// <remarks>启用后该分类重新参与有效目录；已启用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">启用分类请求。</param>
  /// <returns>是否启用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类标识为空、分类不存在、状态更新未能写入。</exception>
  Task<bool> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryRequest request);

  /// <summary>
  /// 停用标准项目分类。
  /// </summary>
  /// <remarks>分类及其下级数据保留、可重新启用；已停用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">停用分类请求。</param>
  /// <returns>是否停用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类标识为空、分类不存在、状态更新未能写入。</exception>
  Task<bool> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryRequest request);

  /// <summary>
  /// 在指定分类下新建标准项目分组。
  /// </summary>
  /// <remarks>要求上级分类已启用。</remarks>
  /// <param name="request">新建分组请求。</param>
  /// <returns>是否创建成功。</returns>
  /// <exception cref="ValidationException">分组名称未填写。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类标识为空、上级分类不存在或已停用、该分类下已有同名分组、分组未能写入。</exception>
  Task<bool> CreateMedicalStandardGroupAsync(CreateMedicalStandardGroupRequest request);

  /// <summary>
  /// 修改标准项目分组的名称与备注。
  /// </summary>
  /// <remarks>所属分类不可变更，启用状态保持不变。</remarks>
  /// <param name="request">修改分组请求。</param>
  /// <returns>是否修改成功。</returns>
  /// <exception cref="ValidationException">分组名称未填写。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组标识为空、分组不存在、同一分类内已有同名分组、修改未能写入。</exception>
  Task<bool> UpdateMedicalStandardGroupAsync(UpdateMedicalStandardGroupRequest request);

  /// <summary>
  /// 启用已被停用的标准项目分组。
  /// </summary>
  /// <remarks>已启用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">启用分组请求。</param>
  /// <returns>是否启用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组标识为空、分组不存在、状态更新未能写入。</exception>
  Task<bool> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupRequest request);

  /// <summary>
  /// 停用标准项目分组。
  /// </summary>
  /// <remarks>其下标准项目数据保留；已停用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">停用分组请求。</param>
  /// <returns>是否停用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或分组标识为空、分组不存在、状态更新未能写入。</exception>
  Task<bool> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupRequest request);

  /// <summary>
  /// 在指定分类与分组下新建标准项目。
  /// </summary>
  /// <remarks>要求分类与分组已启用且归属一致。</remarks>
  /// <param name="request">新建标准项目请求。</param>
  /// <returns>是否创建成功。</returns>
  /// <exception cref="ValidationException">标准项目编码或名称未填写。</exception>
  /// <exception cref="InvalidOperationException">操作人缺失，或分类或分组标识为空、分类或分组不存在、分类或分组已停用、分组不属于所选分类、编码已被其他标准项目占用、项目未能写入。</exception>
  Task<bool> CreateMedicalStandardItemAsync(CreateMedicalStandardItemRequest request);

  /// <summary>
  /// 用本次提交的文本整体覆盖标准项目的备注。
  /// </summary>
  /// <remarks>不区分空串与 <see langword="null"/>。</remarks>
  /// <param name="request">修改标准项目备注请求。</param>
  /// <returns>是否修改成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目标识为空、标准项目不存在、修改未能写入。</exception>
  Task<bool> ChangeMedicalStandardItemRemarkAsync(ChangeMedicalStandardItemRemarkRequest request);

  /// <summary>
  /// 启用已被停用的标准项目。
  /// </summary>
  /// <remarks>上级停用时启用后仍不属于有效目录；已启用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">启用标准项目请求。</param>
  /// <returns>是否启用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目标识为空、标准项目不存在、状态更新未能写入。</exception>
  Task<bool> EnableMedicalStandardItemAsync(EnableMedicalStandardItemRequest request);

  /// <summary>
  /// 停用标准项目。
  /// </summary>
  /// <remarks>停用后不再参与有效目录匹配；已停用时幂等成功且不产生状态变更事件。</remarks>
  /// <param name="request">停用标准项目请求。</param>
  /// <returns>是否停用成功。</returns>
  /// <exception cref="InvalidOperationException">操作人缺失，或标准项目标识为空、标准项目不存在、状态更新未能写入。</exception>
  Task<bool> DisableMedicalStandardItemAsync(DisableMedicalStandardItemRequest request);

  /// <summary>
  /// 新建互认项目配置，登记某组织下的可互认标准项目。
  /// </summary>
  /// <param name="request">新建互认项目配置请求。</param>
  /// <returns>创建成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>，入参不合法、可信组织或操作人不可解析、业务拒绝与写入失败都以异常结束。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">标准项目编码缺失、空串或纯空白，或可互认时间天数不是正整数。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码、用户标识不能解析为非空 Guid，或按请求提交的编码读不到标准项目时抛出；
  /// 标准项目、所属分类或所属分组不存在或已停用，或该组织已配置此标准项目（含已停用配置占用的配置）时同样抛出。
  /// 并发下两个请求可能同时通过查重，此时由唯一索引拒绝，数据库异常不翻译为业务拒绝。
  /// </exception>
  Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemRequest request);

  /// <summary>
  /// 修改互认项目配置的可互认时间天数。
  /// </summary>
  /// <param name="request">修改互认项目配置请求。</param>
  /// <returns>保存成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>，配置不存在、不属于当前可信组织与写入失败都以异常结束。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">配置标识为空 Guid，或可互认时间天数不是正整数。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，配置不存在或不属于当前可信组织（两者同一拒绝口径），
  /// 或更新影响的行数不为 1（含 0 行）时抛出。停用状态的配置同样允许修改，提交与当前值相同的天数照常保存。
  /// </exception>
  Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationRequest request);

  /// <summary>
  /// 启用互认项目配置，使该组织下的该标准项目重新参与互认匹配。
  /// </summary>
  /// <param name="request">启用互认项目配置请求。</param>
  /// <returns>启用成功或配置已是启用状态返回 <see langword="true"/>；本方法不返回 <see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">配置标识为空 Guid。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，配置不存在或不属于当前可信组织，
  /// 标准项目、所属分类或所属分组不存在或已停用，或条件更新影响多行时抛出；
  /// 其中"条件更新影响多行"是防御分支、物理不可达：条件更新语句按主键等值与组织编码等值定位，最多命中一行（见 <c>MutualRecognitionItem.xml</c> 的启用语句）；
  /// 条件更新影响 0 行时按同一可信组织复读一次配置：复读仍是停用按并发冲突拒绝、复读读不到配置按配置不存在拒绝，
  /// 复读已是启用状态按幂等成功返回、不抛出。
  /// </exception>
  Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemRequest request);

  /// <summary>
  /// 停用互认项目配置，使其不再参与互认匹配。
  /// </summary>
  /// <param name="request">停用互认项目配置请求。</param>
  /// <returns>停用成功或配置已是停用状态返回 <see langword="true"/>；本方法不返回 <see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">配置标识为空 Guid。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明取不到组织编码，用户标识不能解析为非空 Guid，配置不存在或不属于当前可信组织，
  /// 或条件更新影响多行时抛出；
  /// 其中"条件更新影响多行"是防御分支、物理不可达：条件更新语句按主键等值与组织编码等值定位，最多命中一行（见 <c>MutualRecognitionItem.xml</c> 的停用语句）；
  /// 条件更新影响 0 行时按同一可信组织复读一次配置：复读仍是启用按并发冲突拒绝、复读读不到配置按配置不存在拒绝，
  /// 复读已是停用状态按幂等成功返回、不抛出。停用不校验标准目录当前有效性。
  /// </exception>
  Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemRequest request);

  /// <summary>
  /// 保存组织医院院区互认项目金额（平台管理员入口）：业务键无记录时新建，有记录时覆盖。
  /// </summary>
  /// <remarks>组织、医院与院区按请求使用并校验存在、启用与父子归属；操作人与操作时间由服务端写入。</remarks>
  /// <param name="request">保存金额请求，携带组织、医院、院区、标准项目编码与本次金额。</param>
  /// <returns>保存成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>，入参不合法、可信身份不可解析、业务拒绝与写入失败都以异常结束。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">组织编码、医院编码、院区编码或标准项目编码缺失、空串、纯空白，或当前金额未提交时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 用户标识不能解析为非空 Guid，或请求的组织、医院、院区不存在、已停用、父子归属不匹配时抛出；
  /// 当前组织未建立该标准项目的互认配置、金额为负数或超过两位小数、并发首次保存命中唯一约束、
  /// 条件更新影响 0 行且复读无法解释为成功或影响行数大于 1 时同样抛出。
  /// 以上情况都没有写入金额，也没有登记保存事件。
  /// </exception>
  Task<bool> SaveOrganizationHospitalBranchRecognitionAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountRequest request);

  /// <summary>
  /// 保存本院区互认项目金额（医院管理员入口）：组织与医院取可信上下文，院区取请求。
  /// </summary>
  /// <remarks>请求院区必须属于可信医院；其余与平台管理员入口相同，两个入口共用同一个命令与同一个保存事件。</remarks>
  /// <param name="request">保存金额请求，只携带院区、标准项目编码与本次金额。</param>
  /// <returns>保存成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>，入参不合法、可信上下文不可用、业务拒绝与写入失败都以异常结束。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">院区编码或标准项目编码缺失、空串、纯空白，或当前金额未提交时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 登录令牌的组织声明或医院声明取不到时抛出，不使用默认值、不降级为空值；
  /// 用户标识不能解析为非空 Guid，或请求院区不存在、已停用、不属于可信医院时同样抛出；
  /// 当前组织未建立该标准项目的互认配置、金额为负数或超过两位小数、并发首次保存命中唯一约束、
  /// 条件更新影响 0 行且复读无法解释为成功或影响行数大于 1 时同样抛出。
  /// 以上情况都没有写入金额，也没有登记保存事件。
  /// </exception>
  Task<bool> SaveBranchRecognitionAmountAsync(SaveBranchRecognitionAmountRequest request);
}
