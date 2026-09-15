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
  /// <returns>是否创建成功。</returns>
  /// <exception cref="InvalidOperationException">登录上下文没有可解析的操作人标识，或配置未能写入。</exception>
  Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemRequest request);

  /// <summary>
  /// 修改互认项目配置的可互认时间天数。
  /// </summary>
  /// <param name="request">修改互认项目配置请求。</param>
  /// <returns>是否修改成功，配置不存在返回 <see langword="false"/>。</returns>
  /// <exception cref="InvalidOperationException">修改未能写入。</exception>
  Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationRequest request);

  /// <summary>
  /// 启用互认项目配置，使该组织下的该标准项目重新参与互认匹配。
  /// </summary>
  /// <param name="request">启用互认项目配置请求。</param>
  /// <returns>是否启用成功，配置不存在返回 <see langword="false"/>。</returns>
  /// <exception cref="InvalidOperationException">启用未能写入。</exception>
  Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemRequest request);

  /// <summary>
  /// 停用互认项目配置，使其不再参与互认匹配。
  /// </summary>
  /// <param name="request">停用互认项目配置请求。</param>
  /// <returns>是否停用成功，配置不存在返回 <see langword="false"/>。</returns>
  /// <exception cref="InvalidOperationException">停用未能写入。</exception>
  Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemRequest request);
}
