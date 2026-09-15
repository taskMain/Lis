using Dy.Core.Abstractions.Domain;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;

/// <summary>
/// 维护标准目录的分类、分组与标准项目及其启停与归属规则。
/// </summary>
/// <remarks>名称唯一性与下级归属业务拒绝由方法判断，成功变化由本类登记事件。</remarks>
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

  public async Task<bool> CreateMutualRecognitionItemAsync(CreateMutualRecognitionItemCommand command)
  {
    var item = command.MapToMutualRecognitionItem(); item.Id = repository.CreateGuid(); await repository.CreateMutualRecognitionItemAsync(item);
    AddEvent(command.CreateMutualRecognitionItemCreatedEvent(item.Id)); return true;
  }
  public async Task<bool> UpdateMutualRecognitionItemConfigurationAsync(UpdateMutualRecognitionItemConfigurationCommand command)
  {
    var item = command.MapToMutualRecognitionItem(); var result = await repository.UpdateMutualRecognitionItemConfigurationAsync(item);
    AddEvent(command.CreateMutualRecognitionItemConfigurationUpdatedEvent()); return result > 0;
  }
  public async Task<bool> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand command)
  {
    var result = await repository.EnableMutualRecognitionItemAsync(command); AddEvent(command.CreateMutualRecognitionItemEnabledEvent()); return result > 0;
  }
  public async Task<bool> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand command)
  {
    var result = await repository.DisableMutualRecognitionItemAsync(command); AddEvent(command.CreateMutualRecognitionItemDisabledEvent()); return result > 0;
  }
}

