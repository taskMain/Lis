using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 标准目录与互认项目配置的读写端口。
/// </summary>
/// <remarks>同时负责生成实体主键；只做数据读写并返回行数或查询结果，不判断业务状态。</remarks>
public partial interface IMedicalRecognitionReportRepository : IRepository
{
  /// <summary>
  /// 生成一个新的实体主键。
  /// </summary>
  /// <remarks>供领域层在写入前为分类、分组与标准项目赋值；生成值冲突由主键唯一性兜底。</remarks>
  /// <returns>新的主键标识。</returns>
  Guid CreateGuid();
  /// <summary>
  /// 插入一条标准项目分类记录。
  /// </summary>
  /// <remarks>启用状态固定写为启用。</remarks>
  /// <param name="medicalStandardCategory">待插入的分类实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory);
  /// <summary>
  /// 按分类标识更新项目类型、名称与备注。
  /// </summary>
  /// <remarks>不改变启用状态。</remarks>
  /// <param name="medicalStandardCategory">待更新的分类实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateMedicalStandardCategoryAsync(MedicalStandardCategory medicalStandardCategory);
  /// <summary>
  /// 按标识启用已被停用的分类。
  /// </summary>
  /// <remarks>仅停用态才实际更新，已处于启用态或不存在时不影响任何行。</remarks>
  /// <param name="enableMedicalStandardCategoryCommand">启用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand enableMedicalStandardCategoryCommand);
  /// <summary>
  /// 按标识停用启用中的分类。
  /// </summary>
  /// <remarks>仅启用态才实际更新，已处于停用态或不存在时不影响任何行。</remarks>
  /// <param name="disableMedicalStandardCategoryCommand">停用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand disableMedicalStandardCategoryCommand);
  /// <summary>
  /// 插入一条标准项目分组记录。
  /// </summary>
  /// <remarks>分组归属到实体携带的分类下，启用状态固定写为启用。</remarks>
  /// <param name="medicalStandardGroup">待插入的分组实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup);
  /// <summary>
  /// 按分组标识更新名称与备注。
  /// </summary>
  /// <remarks>不改变所属分类与启用状态。</remarks>
  /// <param name="medicalStandardGroup">待更新的分组实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateMedicalStandardGroupAsync(MedicalStandardGroup medicalStandardGroup);
  /// <summary>
  /// 按标识启用已被停用的分组。
  /// </summary>
  /// <remarks>仅停用态才实际更新，已处于启用态或不存在时不影响任何行。</remarks>
  /// <param name="enableMedicalStandardGroupCommand">启用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand enableMedicalStandardGroupCommand);
  /// <summary>
  /// 按标识停用启用中的分组。
  /// </summary>
  /// <remarks>仅启用态才实际更新，已处于停用态或不存在时不影响任何行。</remarks>
  /// <param name="disableMedicalStandardGroupCommand">停用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand disableMedicalStandardGroupCommand);
  /// <summary>
  /// 插入一条标准项目记录。
  /// </summary>
  /// <remarks>启用状态固定写为启用。</remarks>
  /// <param name="medicalStandardItem">待插入的标准项目实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMedicalStandardItemAsync(MedicalStandardItem medicalStandardItem);
  /// <summary>
  /// 按标准项目标识覆盖备注。
  /// </summary>
  /// <remarks>覆盖后刷新操作人与操作时间。</remarks>
  /// <param name="medicalStandardItem">待更新的标准项目实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> ChangeMedicalStandardItemRemarkAsync(MedicalStandardItem medicalStandardItem);
  /// <summary>
  /// 按标识启用已被停用的标准项目。
  /// </summary>
  /// <remarks>仅停用态才实际更新，已处于启用态或不存在时不影响任何行。</remarks>
  /// <param name="enableMedicalStandardItemCommand">启用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand enableMedicalStandardItemCommand);
  /// <summary>
  /// 按标识停用启用中的标准项目。
  /// </summary>
  /// <remarks>仅启用态才实际更新，已处于停用态或不存在时不影响任何行。</remarks>
  /// <param name="disableMedicalStandardItemCommand">停用命令。</param>
  /// <returns>受影响行数。</returns>
  Task<int> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand disableMedicalStandardItemCommand);
  /// <summary>
  /// 按主键读取单个标准项目分类。
  /// </summary>
  /// <remarks>启用中与已停用的分类都会被返回；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="id">ID</param>
  /// <returns>分类实体。</returns>
  Task<MedicalStandardCategory?> GetMedicalStandardCategoryByIdAsync(Guid id);
  /// <summary>
  /// 统计同名分类是否已存在。
  /// </summary>
  /// <remarks>用于新建与改名前的名称查重；不传排除标识表示不排除。</remarks>
  /// <param name="name">待校验的分类名称。</param>
  /// <param name="excludedId">查重时排除的标识。</param>
  /// <returns>是否存在同名分类。</returns>
  Task<bool> MedicalStandardCategoryNameExistsAsync(string name, Guid? excludedId = null);
  /// <summary>
  /// 判断指定分类下是否已有下级分组。
  /// </summary>
  /// <remarks>含已停用分组。</remarks>
  /// <param name="categoryId">分类标识。</param>
  /// <returns>是否存在下级分组。</returns>
  Task<bool> MedicalStandardCategoryHasGroupsAsync(Guid categoryId);
  /// <summary>
  /// 按主键读取单个标准项目分组。
  /// </summary>
  /// <remarks>启用中与已停用的分组都会被返回；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="id">ID</param>
  /// <returns>分组实体。</returns>
  Task<MedicalStandardGroup?> GetMedicalStandardGroupByIdAsync(Guid id);
  /// <summary>
  /// 统计指定分类内同名分组是否已存在。
  /// </summary>
  /// <remarks>用于新建与改名前的分组名称查重；不传排除标识表示不排除。</remarks>
  /// <param name="categoryId">查重的分类范围。</param>
  /// <param name="name">待校验的分组名称。</param>
  /// <param name="excludedId">查重时排除的标识。</param>
  /// <returns>分类内是否存在同名分组。</returns>
  Task<bool> MedicalStandardGroupNameExistsAsync(Guid categoryId, string name, Guid? excludedId = null);
  /// <summary>
  /// 按主键读取单个标准项目。
  /// </summary>
  /// <remarks>启用中与已停用的项目都会被返回；不存在时返回 <see langword="null"/>。</remarks>
  /// <param name="id">ID</param>
  /// <returns>标准项目实体。</returns>
  Task<MedicalStandardItem?> GetMedicalStandardItemByIdAsync(Guid id);
  /// <summary>
  /// 按标准项目编码精确读取单个标准项目。
  /// </summary>
  /// <remarks>
  /// 供应用层把调用方提交的标准项目编码解析为服务端目录标识，调用方不能直接提交内部标识；
  /// 启用中与已停用的项目都会被返回，当前是否有效由领域层判断；不存在时返回 <see langword="null"/>。
  /// </remarks>
  /// <param name="code">标准项目编码。</param>
  /// <returns>标准项目实体。</returns>
  Task<MedicalStandardItem?> GetMedicalStandardItemByCodeAsync(string code);
  /// <summary>
  /// 统计同编码的标准项目是否已存在。
  /// </summary>
  /// <remarks>用于新建标准项目前的编码查重。</remarks>
  /// <param name="code">待校验的标准项目编码。</param>
  /// <returns>是否存在同编码标准项目。</returns>
  Task<bool> MedicalStandardItemCodeExistsAsync(string code);
  /// <summary>
  /// 插入一条互认项目配置记录。
  /// </summary>
  /// <remarks>同一可信组织与同一标准项目只能存在一条配置，重复写入由组织与标准项目唯一约束拒绝。</remarks>
  /// <param name="mutualRecognitionItem">待插入的互认项目配置实体。</param>
  /// <returns>受影响行数。</returns>
  /// <exception cref="DuplicateMutualRecognitionItemException">写入违反组织与标准项目唯一约束时抛出。</exception>
  Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem mutualRecognitionItem);
  /// <summary>
  /// 在指定组织范围内按配置标识读取互认项目配置。
  /// </summary>
  /// <remarks>供修改、启用与停用前校验配置归属；其他组织的同标识配置不会被返回，不存在或越组织统一返回 <see langword="null"/>。</remarks>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">可信组织编码。</param>
  /// <returns>互认项目配置实体。</returns>
  Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode);
  /// <summary>
  /// 按配置标识与可信组织更新可互认时间天数；不改变组织编码、标准项目与启用状态。
  /// </summary>
  /// <param name="mutualRecognitionItem">携带配置标识、可信组织、本次天数与操作字段的实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem mutualRecognitionItem);
  /// <summary>
  /// 将指定互认项目配置由停用改为启用；仅当前为停用态且属于可信组织时才实际更新。
  /// </summary>
  /// <param name="enableMutualRecognitionItemCommand">启用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand enableMutualRecognitionItemCommand);
  /// <summary>
  /// 将指定互认项目配置由启用改为停用；仅当前为启用态且属于可信组织时才实际更新。
  /// </summary>
  /// <param name="disableMutualRecognitionItemCommand">停用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand disableMutualRecognitionItemCommand);
}
