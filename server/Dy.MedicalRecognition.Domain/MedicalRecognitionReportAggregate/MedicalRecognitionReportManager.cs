using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 维护标准目录的分类、分组与标准项目及其启停与归属规则，同时维护标准项目的互认项目配置。
/// </summary>
/// <remarks>
/// 名称唯一性、下级归属、目录有效性与互认配置归属等业务拒绝由方法判断，成功变化由本类登记事件；
/// 互认配置按可信组织范围读取与更新，影响行数决定是否登记事件。
/// </remarks>
public partial class MedicalRecognitionReportManager : DomainService
{
  /// <summary>
  /// 读取与保存标准目录数据所需的仓储抽象。
  /// </summary>
  private readonly IMedicalRecognitionReportRepository repository;

  /// <summary>
  /// 初始化管理器。
  /// </summary>
  /// <param name="repository">标准目录的持久化入口。</param>
  public MedicalRecognitionReportManager(IMedicalRecognitionReportRepository repository) => this.repository = repository;

  /// <summary>
  /// 创建标准项目分类。
  /// </summary>
  /// <remarks>创建后默认启用。</remarks>
  /// <param name="command">新建分类命令。</param>
  /// <returns>创建成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">名称已被其他分类占用，或分类保存未写入任何行。</exception>
  public async Task<bool> CreateMedicalStandardCategoryAsync(CreateMedicalStandardCategoryCommand command)
  {
    if (await repository.MedicalStandardCategoryNameExistsAsync(command.Name)) throw new InvalidOperationException("业务拒绝：分类名称已存在。");
    MedicalStandardCategory category = command.MapToMedicalStandardCategory();
    category.Id = repository.CreateGuid(); category.IsValid = true;
    if (await repository.CreateMedicalStandardCategoryAsync(category) <= 0) throw new InvalidOperationException("业务拒绝：分类保存失败。");
    AddEvent(command.CreateMedicalStandardCategoryCreatedEvent(category.Id)); return true;
  }

  /// <summary>
  /// 保存分类的名称、备注与项目类型。
  /// </summary>
  /// <remarks>启停状态保持原值。</remarks>
  /// <param name="command">修改分类命令。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分类不存在、名称被其他分类占用，或该分类已有下级分组却要求改变项目类型。</exception>
  public async Task<bool> UpdateMedicalStandardCategoryAsync(UpdateMedicalStandardCategoryCommand command)
  {
    MedicalStandardCategory? existing = await repository.GetMedicalStandardCategoryByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分类不存在。");
    if (await repository.MedicalStandardCategoryNameExistsAsync(command.Name, command.Id)) throw new InvalidOperationException("业务拒绝：分类名称已存在。");
    if (existing.ItemType != command.ItemType && await repository.MedicalStandardCategoryHasGroupsAsync(command.Id))
      throw new InvalidOperationException("业务拒绝：分类存在下级分组时不可修改项目类型。");
    MedicalStandardCategory category = command.MapToMedicalStandardCategory(); category.IsValid = existing.IsValid;
    if (await repository.UpdateMedicalStandardCategoryAsync(category) <= 0) throw new InvalidOperationException("业务拒绝：分类不存在。");
    AddEvent(command.CreateMedicalStandardCategoryUpdatedEvent()); return true;
  }

  /// <summary>
  /// 启用分类。
  /// </summary>
  /// <remarks>只改变分类自身状态，不级联下级。</remarks>
  /// <param name="command">分类启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分类不存在或状态更新未影响记录。</exception>
  public async Task<bool> EnableMedicalStandardCategoryAsync(EnableMedicalStandardCategoryCommand command)
  {
    MedicalStandardCategory? existing = await repository.GetMedicalStandardCategoryByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分类不存在。");
    if (existing.IsValid) return true;
    if (await repository.EnableMedicalStandardCategoryAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：分类状态更新失败。");
    AddEvent(command.CreateMedicalStandardCategoryEnabledEvent()); return true;
  }

  /// <summary>
  /// 停用分类。
  /// </summary>
  /// <remarks>只改变分类自身状态，不级联下级。</remarks>
  /// <param name="command">分类启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分类不存在或状态更新未影响记录。</exception>
  public async Task<bool> DisableMedicalStandardCategoryAsync(DisableMedicalStandardCategoryCommand command)
  {
    MedicalStandardCategory? existing = await repository.GetMedicalStandardCategoryByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分类不存在。");
    if (!existing.IsValid) return true;
    if (await repository.DisableMedicalStandardCategoryAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：分类状态更新失败。");
    AddEvent(command.CreateMedicalStandardCategoryDisabledEvent()); return true;
  }

  /// <summary>
  /// 在被选分类下创建分组。
  /// </summary>
  /// <remarks>分类须存在且启用。</remarks>
  /// <param name="command">新建分组命令。</param>
  /// <returns>创建成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 分类不存在或已停用、名称在该分类内已存在。
  /// </exception>
  public async Task<bool> CreateMedicalStandardGroupAsync(CreateMedicalStandardGroupCommand command)
  {
    MedicalStandardCategory category = await repository.GetMedicalStandardCategoryByIdAsync(command.CategoryId) ?? throw new InvalidOperationException("业务拒绝：分类不存在。");
    if (!category.IsValid) throw new InvalidOperationException("业务拒绝：上级分类已停用。");
    if (await repository.MedicalStandardGroupNameExistsAsync(command.CategoryId, command.Name)) throw new InvalidOperationException("业务拒绝：分组名称已存在。");
    MedicalStandardGroup group = command.MapToMedicalStandardGroup(); group.Id = repository.CreateGuid(); group.IsValid = true;
    if (await repository.CreateMedicalStandardGroupAsync(group) <= 0) throw new InvalidOperationException("业务拒绝：分组保存失败。");
    AddEvent(command.CreateMedicalStandardGroupCreatedEvent(group.Id)); return true;
  }

  /// <summary>
  /// 保存分组的名称与备注。
  /// </summary>
  /// <remarks>分组创建后归属不可修改，启停状态保持原值。</remarks>
  /// <param name="command">修改分组命令。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分组不存在，或名称在其所属分类内已被其他分组占用。</exception>
  public async Task<bool> UpdateMedicalStandardGroupAsync(UpdateMedicalStandardGroupCommand command)
  {
    MedicalStandardGroup? existing = await repository.GetMedicalStandardGroupByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分组不存在。");
    if (await repository.MedicalStandardGroupNameExistsAsync(existing.CategoryId, command.Name, command.Id)) throw new InvalidOperationException("业务拒绝：分组名称已存在。");
    MedicalStandardGroup group = command.MapToMedicalStandardGroup(); group.CategoryId = existing.CategoryId; group.IsValid = existing.IsValid;
    if (await repository.UpdateMedicalStandardGroupAsync(group) <= 0) throw new InvalidOperationException("业务拒绝：分组不存在。");
    AddEvent(command.CreateMedicalStandardGroupUpdatedEvent(existing.CategoryId)); return true;
  }

  /// <summary>
  /// 启用分组。
  /// </summary>
  /// <remarks>父分类保持停用时仍可启用，但其下项目仍不属于当前有效目录。</remarks>
  /// <param name="command">分组启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分组不存在或状态更新未影响记录。</exception>
  public async Task<bool> EnableMedicalStandardGroupAsync(EnableMedicalStandardGroupCommand command)
  {
    MedicalStandardGroup? existing = await repository.GetMedicalStandardGroupByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分组不存在。");
    if (existing.IsValid) return true;
    if (await repository.EnableMedicalStandardGroupAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：分组状态更新失败。");
    AddEvent(command.CreateMedicalStandardGroupEnabledEvent()); return true;
  }

  /// <summary>
  /// 停用分组。
  /// </summary>
  /// <remarks>只改变分组自身状态，不级联其下项目。</remarks>
  /// <param name="command">分组启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">分组不存在或状态更新未影响记录。</exception>
  public async Task<bool> DisableMedicalStandardGroupAsync(DisableMedicalStandardGroupCommand command)
  {
    MedicalStandardGroup? existing = await repository.GetMedicalStandardGroupByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：分组不存在。");
    if (!existing.IsValid) return true;
    if (await repository.DisableMedicalStandardGroupAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：分组状态更新失败。");
    AddEvent(command.CreateMedicalStandardGroupDisabledEvent()); return true;
  }

  /// <summary>
  /// 创建标准项目。
  /// </summary>
  /// <remarks>所属分类与分组须存在、启用且归属一致。</remarks>
  /// <param name="command">新建标准项目命令。</param>
  /// <returns>创建成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 分类或分组不存在、分类或分组已停用、
  /// 分组不属于所选分类，或编码已被占用（含已停用项目占用的编码）。
  /// </exception>
  public async Task<bool> CreateMedicalStandardItemAsync(CreateMedicalStandardItemCommand command)
  {
    MedicalStandardCategory category = await repository.GetMedicalStandardCategoryByIdAsync(command.CategoryId) ?? throw new InvalidOperationException("业务拒绝：分类不存在。");
    MedicalStandardGroup group = await repository.GetMedicalStandardGroupByIdAsync(command.GroupId) ?? throw new InvalidOperationException("业务拒绝：分组不存在。");
    if (!category.IsValid || !group.IsValid) throw new InvalidOperationException("业务拒绝：分类和分组必须启用。");
    if (group.CategoryId != category.Id) throw new InvalidOperationException("业务拒绝：分组不属于所选分类。");
    if (await repository.MedicalStandardItemCodeExistsAsync(command.Code)) throw new InvalidOperationException("业务拒绝：标准项目编码已存在。");
    MedicalStandardItem item = command.MapToMedicalStandardItem(); item.Id = repository.CreateGuid(); item.IsValid = true;
    if (await repository.CreateMedicalStandardItemAsync(item) <= 0) throw new InvalidOperationException("业务拒绝：标准项目保存失败。");
    AddEvent(command.CreateMedicalStandardItemCreatedEvent(item.Id)); return true;
  }

  /// <summary>
  /// 修改标准项目备注。
  /// </summary>
  /// <remarks>名称、编码、分类、分组与启停状态均不随本方法改变。</remarks>
  /// <param name="command">修改备注命令。</param>
  /// <returns>修改成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">标准项目不存在或更新未影响记录。</exception>
  public async Task<bool> ChangeMedicalStandardItemRemarkAsync(ChangeMedicalStandardItemRemarkCommand command)
  {
    if (await repository.GetMedicalStandardItemByIdAsync(command.Id) is null) throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    MedicalStandardItem item = command.MapToMedicalStandardItem();
    if (await repository.ChangeMedicalStandardItemRemarkAsync(item) <= 0) throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    AddEvent(command.CreateMedicalStandardItemRemarkChangedEvent()); return true;
  }

  /// <summary>
  /// 启用标准项目。
  /// </summary>
  /// <remarks>父级分类或分组停用时仍可启用，但项目仍不属于当前有效目录。</remarks>
  /// <param name="command">标准项目启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">标准项目不存在或状态更新未影响记录。</exception>
  public async Task<bool> EnableMedicalStandardItemAsync(EnableMedicalStandardItemCommand command)
  {
    MedicalStandardItem? existing = await repository.GetMedicalStandardItemByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    if (existing.IsValid) return true;
    if (await repository.EnableMedicalStandardItemAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：标准项目状态更新失败。");
    AddEvent(command.CreateMedicalStandardItemEnabledEvent()); return true;
  }

  /// <summary>
  /// 停用标准项目。
  /// </summary>
  /// <remarks>只改变项目自身状态，不级联其父级。</remarks>
  /// <param name="command">标准项目启停命令。</param>
  /// <returns>返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">标准项目不存在或状态更新未影响记录。</exception>
  public async Task<bool> DisableMedicalStandardItemAsync(DisableMedicalStandardItemCommand command)
  {
    MedicalStandardItem? existing = await repository.GetMedicalStandardItemByIdAsync(command.Id);
    if (existing is null) throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    if (!existing.IsValid) return true;
    if (await repository.DisableMedicalStandardItemAsync(command) <= 0) throw new InvalidOperationException("业务拒绝：标准项目状态更新失败。");
    AddEvent(command.CreateMedicalStandardItemDisabledEvent()); return true;
  }

  /// <summary>
  /// 新增互认项目配置。
  /// </summary>
  /// <remarks>标准项目、所属分类与所属分组必须当前有效；配置以启用状态创建，同一组织与同一标准项目只能有一条配置，停用配置仍占用该配置。</remarks>
  /// <param name="command">新增配置命令，携带可信组织、应用层按编码解析出的标准项目标识与操作字段。</param>
  /// <returns>创建成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">
  /// 标准项目、所属分类或所属分组不存在或已停用，该组织已配置此标准项目，或配置影响的行数不为 1（含 0 行）。
  /// </exception>
  public async Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemCommand command)
  {
    await ValidateStandardCatalogAsync(command.StandardItemId);
    MutualRecognitionItem configuration = command.MapToMutualRecognitionItem();
    configuration.Id = repository.CreateGuid();
    // 聚合初值固定为启用：领域事件与"创建必为启用"的行为断言都以该值为准；
    // 落库由新增语句按项目既有约定写成常量（见 MutualRecognitionItem.xml），两者表达同一业务事实。
    configuration.IsValid = true;
    try
    {
      if (await repository.CreateMutualRecognitionItemAsync(configuration) != 1) throw new InvalidOperationException("业务拒绝：互认项目配置保存失败。");
    }
    catch (DuplicateMutualRecognitionItemException exception)
    {
      // 唯一约束冲突统一按重复配置拒绝，不向调用方暴露数据库错误细节。
      throw new InvalidOperationException("业务拒绝：该组织已配置此标准项目。", exception);
    }

    AddEvent(command.CreateMutualRecognitionItemCreatedEvent(configuration.Id));
    return true;
  }

  /// <summary>
  /// 修改互认项目配置的可互认时间天数。
  /// </summary>
  /// <remarks>
  /// 先按配置标识与可信组织读取配置，配置不存在或不属于该组织都按不存在拒绝；
  /// 只改变可互认时间天数与操作字段，组织、标准项目与启用状态保持已读取的配置记录原值，停用配置同样允许修改，提交原值也会照常保存并登记事件。
  /// </remarks>
  /// <param name="command">修改命令，携带配置标识、可信组织、本次天数与操作字段。</param>
  /// <returns>保存成功返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">配置不存在、不属于当前可信组织，或更新影响的行数不为 1（含 0 行）时抛出。</exception>
  public async Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationCommand command)
  {
    MutualRecognitionItem configuration = await repository.GetMutualRecognitionItemByIdAsync(command.Id, command.OrganizationCode)
      ?? throw new InvalidOperationException("业务拒绝：互认项目配置不存在。");
    MutualRecognitionItem updated = command.MapToMutualRecognitionItem();
    updated.OrganizationCode = configuration.OrganizationCode;
    updated.StandardProjectCode = configuration.StandardProjectCode;
    if (await repository.UpdateMutualRecognitionItemConfigurationAsync(updated) != 1) throw new InvalidOperationException("业务拒绝：互认项目配置更新失败。");
    AddEvent(command.CreateMutualRecognitionItemConfigurationUpdatedEvent(updated.StandardProjectCode));
    return true;
  }

  /// <summary>
  /// 启用互认项目配置。
  /// </summary>
  /// <remarks>
  /// 先按配置标识与可信组织读取配置并校验归属，已是启用状态时幂等成功；此时不更新操作字段、不登记事件，即使标准目录已停用也不把重复启用当作新启用。
  /// 真实启用前校验标准项目、所属分类与所属分组当前有效，条件更新恰好影响 1 行才登记启用事件。
  /// 条件更新影响多行是防御分支、物理不可达：更新语句按主键等值与可信组织编码等值定位，最多命中一行（见 <c>MutualRecognitionItem.xml</c> 的 EnableMutualRecognitionItem 语句）。
  /// </remarks>
  /// <param name="command">启用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>启用成功或已是启用状态返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">配置不存在或不属于当前可信组织、标准目录已停用、条件更新未影响记录，或防御分支上影响多行时抛出。</exception>
  public async Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand command)
  {
    MutualRecognitionItem configuration = await repository.GetMutualRecognitionItemByIdAsync(command.Id, command.OrganizationCode)
      ?? throw new InvalidOperationException("业务拒绝：互认项目配置不存在。");
    if (configuration.IsValid) return true;
    await ValidateStandardCatalogAsync(configuration.StandardItemId);
    int affectedRows = await repository.EnableMutualRecognitionItemAsync(command);
    if (affectedRows == 1)
    {
      AddEvent(command.CreateMutualRecognitionItemEnabledEvent(configuration.StandardProjectCode));
      return true;
    }

    if (affectedRows == 0) return await ResolveConcurrentStateChangeAsync(command.Id, command.OrganizationCode, targetIsValid: true);
    // 防御分支，物理不可达：更新语句按主键等值与可信组织编码等值定位，最多命中一行；保留该分支使影响行数异常时不被当成成功。
    throw new InvalidOperationException("业务拒绝：互认项目配置状态更新异常，未完成启用。");
  }

  /// <summary>
  /// 停用互认项目配置。
  /// </summary>
  /// <remarks>
  /// 先按配置标识与可信组织读取配置并校验归属，已是停用状态时幂等成功且不登记事件；
  /// 停用不校验标准目录当前有效性，条件更新恰好影响 1 行才登记停用事件。
  /// 条件更新影响多行是防御分支、物理不可达：更新语句按主键等值与可信组织编码等值定位，最多命中一行（见 <c>MutualRecognitionItem.xml</c> 的 DisableMutualRecognitionItem 语句）。
  /// </remarks>
  /// <param name="command">停用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>停用成功或已是停用状态返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">配置不存在或不属于当前可信组织、条件更新未影响记录，或防御分支上影响多行时抛出。</exception>
  public async Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand command)
  {
    MutualRecognitionItem configuration = await repository.GetMutualRecognitionItemByIdAsync(command.Id, command.OrganizationCode)
      ?? throw new InvalidOperationException("业务拒绝：互认项目配置不存在。");
    if (!configuration.IsValid) return true;
    int affectedRows = await repository.DisableMutualRecognitionItemAsync(command);
    if (affectedRows == 1)
    {
      AddEvent(command.CreateMutualRecognitionItemDisabledEvent(configuration.StandardProjectCode));
      return true;
    }

    if (affectedRows == 0) return await ResolveConcurrentStateChangeAsync(command.Id, command.OrganizationCode, targetIsValid: false);
    // 防御分支，物理不可达：更新语句按主键等值与可信组织编码等值定位，最多命中一行；保留该分支使影响行数异常时不被当成成功。
    throw new InvalidOperationException("业务拒绝：互认项目配置状态更新异常，未完成停用。");
  }

  /// <summary>
  /// 校验标准项目及其所属分类、所属分组当前均有效。
  /// </summary>
  /// <remarks>标准目录停用只影响当前有效性，不改写互认配置自身状态；新增配置时始终校验，启用时校验，停用时不需要校验。</remarks>
  /// <param name="standardItemId">标准项目标识。</param>
  /// <returns>三层目录均有效时完成的异步操作。</returns>
  /// <exception cref="InvalidOperationException">标准项目、所属分类或所属分组不存在或已停用时抛出。</exception>
  private async Task ValidateStandardCatalogAsync(Guid standardItemId)
  {
    MedicalStandardItem standardItem = await repository.GetMedicalStandardItemByIdAsync(standardItemId) ?? throw new InvalidOperationException("业务拒绝：标准项目不存在。");
    MedicalStandardCategory category = await repository.GetMedicalStandardCategoryByIdAsync(standardItem.CategoryId) ?? throw new InvalidOperationException("业务拒绝：分类不存在。");
    MedicalStandardGroup group = await repository.GetMedicalStandardGroupByIdAsync(standardItem.GroupId) ?? throw new InvalidOperationException("业务拒绝：分组不存在。");
    if (!standardItem.IsValid) throw new InvalidOperationException("业务拒绝：标准项目已停用。");
    if (!category.IsValid) throw new InvalidOperationException("业务拒绝：所属分类已停用。");
    if (!group.IsValid) throw new InvalidOperationException("业务拒绝：所属分组已停用。");
  }

  /// <summary>
  /// 条件更新影响 0 行时按同一可信组织复读一次配置，区分幂等成功与并发冲突。
  /// </summary>
  /// <remarks>
  /// 复读只进行一次、不循环重试：复读已是目标状态说明并发请求已完成同一动作，按幂等成功返回且不补登记事件；
  /// 复读仍是相反状态说明存在反向并发操作，返回提示刷新后重试的业务拒绝；
  /// 复读读不到配置说明配置已不存在或不再属于该组织，与归属校验使用同一拒绝口径，不泄漏其他组织的配置。
  /// </remarks>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">可信组织编码。</param>
  /// <param name="targetIsValid">本次操作的目标启用状态。</param>
  /// <returns>复读结果已是目标状态时返回 <see langword="true"/>。</returns>
  /// <exception cref="InvalidOperationException">配置不存在或不属于该组织，或复读仍是相反状态时抛出。</exception>
  private async Task<bool> ResolveConcurrentStateChangeAsync(Guid id, string organizationCode, bool targetIsValid)
  {
    MutualRecognitionItem? current = await repository.GetMutualRecognitionItemByIdAsync(id, organizationCode);
    if (current is null) throw new InvalidOperationException("业务拒绝：互认项目配置不存在。");
    if (current.IsValid == targetIsValid) return true;
    throw new InvalidOperationException("业务拒绝：互认项目配置状态已被其他操作变更，请刷新后重试。");
  }
}

