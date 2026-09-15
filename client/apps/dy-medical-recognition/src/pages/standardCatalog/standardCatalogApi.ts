import type {
  MedicalRecognitionClient,
  MedicalStandardCategoryListReadModel,
  MedicalStandardGroupListReadModel,
  MedicalStandardItemListReadModel,
} from '@dy/api-client-medical-recognition'

export const ITEM_TYPES = [0, 1] as const
export type ItemTypeValue = (typeof ITEM_TYPES)[number]
export type UsageStatusValue = 0 | 1

export interface StandardCategory {
  id: string | null
  itemType: ItemTypeValue | null
  name: string | null
  remark: string | null
  isValid: boolean | null
  usageStatus: UsageStatusValue | null
}

export interface StandardGroup {
  id: string | null
  categoryId: string | null
  name: string | null
  remark: string | null
  isValid: boolean | null
  usageStatus: UsageStatusValue | null
}

export interface StandardItem {
  id: string | null
  categoryId: string | null
  groupId: string | null
  itemType: ItemTypeValue | null
  code: string | null
  name: string | null
  remark: string | null
  isValid: boolean | null
}

export interface CatalogData {
  categories: StandardCategory[]
  groups: StandardGroup[]
  items: StandardItem[]
}

export interface TreeScope {
  itemType: ItemTypeValue | null
  categoryId: string | null
  groupId: string | null
}

export interface ItemFilter {
  code: string
  name: string
  status: 'all' | 'enabled' | 'disabled'
}

export const DEFAULT_SCOPE: TreeScope = { itemType: 0, categoryId: null, groupId: null }
export const EMPTY_ITEM_FILTER: ItemFilter = { code: '', name: '', status: 'all' }

export interface VisibleCatalogCategory {
  category: StandardCategory
  groups: StandardGroup[]
}

/**
 * 按当前状态过滤与名称搜索裁剪左树。
 *
 * 关闭「显示停用」时停用分组一律隐藏；停用分类只在名称搜索命中其启用分组时
 * 作为必要祖先保留，其余情况与停用分组一同隐藏。返回的 `groups` 即该分类下
 * 应展示的分组，已应用同一套状态与名称条件。
 */
export function visibleCatalogCategories(data: CatalogData, showDisabled: boolean, keyword: string): VisibleCatalogCategory[] {
  const searchActive = keyword.trim().length > 0
  return data.categories.flatMap((category) => {
    const categoryMatched = matchesText(category.name, keyword)
    const groups = data.groups.filter((group) => group.categoryId === category.id && (showDisabled || group.isValid === true) && (categoryMatched || matchesText(group.name, keyword)))
    const categoryVisible = ((showDisabled || category.isValid === true) && categoryMatched) || (searchActive && groups.some((group) => group.isValid === true))
    return categoryVisible ? [{ category, groups }] : []
  })
}

export function resolveTreeScope(data: CatalogData, scope: TreeScope, showDisabled: boolean): TreeScope {
  const category = scope.categoryId === null ? null : data.categories.find((value) => value.id === scope.categoryId) ?? null
  const group = scope.groupId === null ? null : data.groups.find((value) => value.id === scope.groupId) ?? null
  const categoryFromGroup = group === null ? category : data.categories.find((value) => value.id === group.categoryId) ?? null
  const selectedCategory = categoryFromGroup ?? category
  const itemType = selectedCategory?.itemType ?? scope.itemType
  const typeScope = { itemType, categoryId: null, groupId: null }
  if (selectedCategory === null) return typeScope
  const categoryAllowed = showDisabled || selectedCategory.isValid === true
  if (scope.groupId === null) return categoryAllowed ? { itemType, categoryId: selectedCategory.id, groupId: null } : typeScope
  if (group !== null && group.categoryId === selectedCategory.id && (showDisabled || group.isValid === true)) {
    return { itemType, categoryId: selectedCategory.id, groupId: group.id }
  }
  return categoryAllowed ? { itemType, categoryId: selectedCategory.id, groupId: null } : typeScope
}

function mapItemType(value: number | null | undefined): ItemTypeValue | null {
  return value === 0 || value === 1 ? value : null
}

function mapUsageStatus(value: number | null | undefined): UsageStatusValue | null {
  return value === 0 || value === 1 ? value : null
}

function mapCategory(value: MedicalStandardCategoryListReadModel): StandardCategory {
  return {
    id: value.categoryId ?? null,
    itemType: mapItemType(value.itemType),
    name: value.name ?? null,
    remark: value.remark ?? null,
    isValid: value.isValid ?? null,
    usageStatus: mapUsageStatus(value.usageStatus),
  }
}

function mapGroup(value: MedicalStandardGroupListReadModel): StandardGroup {
  return {
    id: value.groupId ?? null,
    categoryId: value.categoryId ?? null,
    name: value.name ?? null,
    remark: value.remark ?? null,
    isValid: value.isValid ?? null,
    usageStatus: mapUsageStatus(value.usageStatus),
  }
}

function mapItem(value: MedicalStandardItemListReadModel): StandardItem {
  return {
    id: value.itemId ?? null,
    categoryId: value.categoryId ?? null,
    groupId: value.groupId ?? null,
    itemType: mapItemType(value.itemType),
    code: value.code ?? null,
    name: value.name ?? null,
    remark: value.remark ?? null,
    isValid: value.isValid ?? null,
  }
}

/** 将 Kiota 的可空查询响应转换为页面使用的稳定目录模型。 */
export async function queryCategories(client: MedicalRecognitionClient): Promise<StandardCategory[]> {
  const values = await client.api.medicalRecognitionReportQuery.queryMedicalStandardCategoryList.post({})
  return (values ?? []).map(mapCategory)
}

export async function queryGroups(client: MedicalRecognitionClient): Promise<StandardGroup[]> {
  const values = await client.api.medicalRecognitionReportQuery.queryMedicalStandardGroupList.post({})
  return (values ?? []).map(mapGroup)
}

export async function queryItems(client: MedicalRecognitionClient): Promise<StandardItem[]> {
  const values = await client.api.medicalRecognitionReportQuery.queryMedicalStandardItemList.post({})
  return (values ?? []).map(mapItem)
}

export interface CategoryInput {
  itemType: ItemTypeValue
  name: string
  remark: string | null
}

export interface GroupInput {
  categoryId: string
  name: string
  remark: string | null
}

export interface ItemInput {
  categoryId: string
  groupId: string
  code: string
  name: string
  remark: string | null
}

export interface CategoryUpdateInput extends CategoryInput {
  id: string
}

export interface GroupUpdateInput {
  id: string
  name: string
  remark: string | null
}

export interface ItemRemarkInput {
  id: string
  remark: string | null
}

/** 所有写操作集中在适配层，页面不依赖 Kiota 请求模型。 */
export async function createCategory(client: MedicalRecognitionClient, input: CategoryInput): Promise<void> {
  await client.api.medicalRecognitionReport.createMedicalStandardCategory.post(input)
}

export async function updateCategory(client: MedicalRecognitionClient, input: CategoryUpdateInput): Promise<void> {
  await client.api.medicalRecognitionReport.updateMedicalStandardCategory.post(input)
}

export async function createGroup(client: MedicalRecognitionClient, input: GroupInput): Promise<void> {
  await client.api.medicalRecognitionReport.createMedicalStandardGroup.post(input)
}

export async function updateGroup(client: MedicalRecognitionClient, input: GroupUpdateInput): Promise<void> {
  await client.api.medicalRecognitionReport.updateMedicalStandardGroup.post(input)
}

export async function createItem(client: MedicalRecognitionClient, input: ItemInput): Promise<void> {
  await client.api.medicalRecognitionReport.createMedicalStandardItem.post(input)
}

export async function updateItemRemark(client: MedicalRecognitionClient, input: ItemRemarkInput): Promise<void> {
  await client.api.medicalRecognitionReport.changeMedicalStandardItemRemark.post(input)
}

export async function setCategoryEnabled(client: MedicalRecognitionClient, id: string, enabled: boolean): Promise<void> {
  if (enabled) await client.api.medicalRecognitionReport.enableMedicalStandardCategory.post({ id })
  else await client.api.medicalRecognitionReport.disableMedicalStandardCategory.post({ id })
}

export async function setGroupEnabled(client: MedicalRecognitionClient, id: string, enabled: boolean): Promise<void> {
  if (enabled) await client.api.medicalRecognitionReport.enableMedicalStandardGroup.post({ id })
  else await client.api.medicalRecognitionReport.disableMedicalStandardGroup.post({ id })
}

export async function setItemEnabled(client: MedicalRecognitionClient, id: string, enabled: boolean): Promise<void> {
  if (enabled) await client.api.medicalRecognitionReport.enableMedicalStandardItem.post({ id })
  else await client.api.medicalRecognitionReport.disableMedicalStandardItem.post({ id })
}

export function matchesText(value: string | null, keyword: string): boolean {
  const normalizedKeyword = keyword.trim().toLocaleLowerCase()
  return normalizedKeyword.length === 0 || (value ?? '').toLocaleLowerCase().includes(normalizedKeyword)
}

export function isItemEffective(data: CatalogData, item: StandardItem): boolean | null {
  if (item.isValid === null || item.categoryId === null || item.groupId === null) return null
  const category = data.categories.find((value) => value.id === item.categoryId)
  const group = data.groups.find((value) => value.id === item.groupId)
  if (!category || !group || category.isValid === null || group.isValid === null) return null
  return item.isValid && category.isValid && group.isValid
}

export function ineffectiveReasons(data: CatalogData, item: StandardItem): string[] {
  const reasons: string[] = []
  const category = data.categories.find((value) => value.id === item.categoryId)
  const group = data.groups.find((value) => value.id === item.groupId)
  if (item.isValid === false) reasons.push('标准项目自身已停用')
  else if (item.isValid === null) reasons.push('标准项目状态未知')
  if (!group) reasons.push('所属分组缺失')
  else if (group.isValid === false) reasons.push('所属分组已停用')
  else if (group.isValid === null) reasons.push('所属分组状态未知')
  if (!category) reasons.push('所属分类缺失')
  else if (category.isValid === false) reasons.push('所属分类已停用')
  else if (category.isValid === null) reasons.push('所属分类状态未知')
  return reasons
}

export function filterItems(data: CatalogData, scope: TreeScope, filter: ItemFilter): StandardItem[] {
  return data.items.filter((item) => {
    const category = data.categories.find((value) => value.id === item.categoryId)
    if (scope.groupId !== null && item.groupId !== scope.groupId) return false
    if (scope.categoryId !== null && item.categoryId !== scope.categoryId) return false
    if (scope.categoryId === null && scope.groupId === null && scope.itemType !== null && category?.itemType !== scope.itemType) return false
    if (!matchesText(item.code, filter.code) || !matchesText(item.name, filter.name)) return false
    if (filter.status === 'enabled' && item.isValid !== true) return false
    if (filter.status === 'disabled' && item.isValid !== false) return false
    return true
  })
}

/** 保留用户输入原值；只有没有输入时才提交显式 null。 */
export function toRemark(value: string | undefined): string | null {
  return value === undefined || value.length === 0 ? null : value
}
