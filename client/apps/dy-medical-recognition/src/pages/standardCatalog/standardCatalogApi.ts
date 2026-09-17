import type {
  MedicalRecognitionClient,
  MedicalStandardCategoryListReadModel,
  MedicalStandardGroupListReadModel,
  MedicalStandardItemListReadModel,
} from '@dy/api-client-medical-recognition'
import {
  MEDICAL_ITEM_TYPES,
  MEDICAL_ITEM_TYPE_TEXTS,
  UNKNOWN_MEDICAL_ITEM_TYPE_TEXT,
  isMedicalItemTypeValue,
  type MedicalItemTypeValue,
} from '../../shared/medicalItemType'

/**
 * 项目类型取值集合、取值域判定与兜底文案：定义已上移到跨页面共享模块 `src/shared/medicalItemType.ts`
 * （阶段 2、阶段 3 适配层使用同一份事实），本模块只按阶段 1 的既有名字转发，调用方与用例无需改变导入位置。
 */
export const ITEM_TYPES = MEDICAL_ITEM_TYPES
export type ItemTypeValue = MedicalItemTypeValue
export const ITEM_TYPE_TEXTS = MEDICAL_ITEM_TYPE_TEXTS
export { UNKNOWN_MEDICAL_ITEM_TYPE_TEXT, isMedicalItemTypeValue }
/** 使用情况取值集合：只服务本页面，故不放进共享模块，但仍在此集中声明一次并由 `isUsageStatusValue` 判定。 */
export const USAGE_STATUS_VALUES = Object.freeze([0, 1] as const)
export type UsageStatusValue = (typeof USAGE_STATUS_VALUES)[number]

/** 使用情况取值域判定：字符串、`NaN`、越界数字与缺失一律为假。 */
export function isUsageStatusValue(value: unknown): value is UsageStatusValue {
  return typeof value === 'number' && (USAGE_STATUS_VALUES as readonly number[]).includes(value)
}

/**
 * 使用情况兜底文案；服务端只读模型已交付 `usageStatusText`，本表只在该契约字段缺失时兜底展示。
 * 中文与后端 `MedicalStandardUsageStatus` 的 `[Description]` 一致，避免同一状态在不同来源下出现两种中文。
 */
export const USAGE_STATUS_TEXTS: Readonly<Record<UsageStatusValue, string>> = Object.freeze({ 0: '未使用', 1: '已使用' })

/**
 * 枚举元数据选项的最小形状；与 `useEnumMetadata` 的 `EnumMetadataOption` 结构一致。
 * 本模块只声明结构而不从 hooks 反向导入，保持「页面 → 适配层 → 生成契约」的依赖方向。
 */
export interface EnumMetadataOptionLike {
  value: number
  label: string
  name: string
}

/**
 * 项目类型表单与筛选的兜底选项：枚举元数据接口不可用或返回空集合时仍可维护目录。
 * 中文取自共享文案出口（`src/shared/medicalItemType.ts`），页面不再自己持有这份业务事实。
 */
export const ITEM_TYPE_METADATA_FALLBACK: readonly (EnumMetadataOptionLike & { value: ItemTypeValue })[] = [
  { value: MEDICAL_ITEM_TYPES[0], name: 'Laboratory', label: ITEM_TYPE_TEXTS[MEDICAL_ITEM_TYPES[0]] },
  { value: MEDICAL_ITEM_TYPES[1], name: 'Examination', label: ITEM_TYPE_TEXTS[MEDICAL_ITEM_TYPES[1]] },
]

/**
 * 把枚举元数据选项收窄为项目类型选项：只保留已确认取值。
 * 后端 `MedicalItemType` 取值集合封闭，未登记取值不可达；此处保留防御是为了让表单与筛选
 * 不出现语义无法判定的项（契约若新增取值，先改共享模块的取值域即可，判定随之一致）。
 */
export function toItemTypeOptions(
  options: readonly Pick<EnumMetadataOptionLike, 'value' | 'label'>[],
): { value: ItemTypeValue; label: string }[] {
  return options.flatMap((option) => (isMedicalItemTypeValue(option.value) ? [{ value: option.value, label: option.label }] : []))
}

export interface StandardCategory {
  id: string | null
  itemType: ItemTypeValue | null
  /** 项目类型官方文案，取自服务端枚举 Description；缺失时为 null，由页面回退到枚举元数据。 */
  itemTypeText: string | null
  name: string | null
  remark: string | null
  isValid: boolean | null
  usageStatus: UsageStatusValue | null
  /** 使用情况官方文案，取自服务端枚举 Description；缺失时为 null。 */
  usageStatusText: string | null
}

export interface StandardGroup {
  id: string | null
  categoryId: string | null
  name: string | null
  remark: string | null
  isValid: boolean | null
  usageStatus: UsageStatusValue | null
  /** 使用情况官方文案，取自服务端枚举 Description；缺失时为 null。 */
  usageStatusText: string | null
}

export interface StandardItem {
  id: string | null
  categoryId: string | null
  groupId: string | null
  itemType: ItemTypeValue | null
  /** 项目类型官方文案，取自服务端枚举 Description；缺失时为 null，由页面回退到枚举元数据。 */
  itemTypeText: string | null
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
  return isMedicalItemTypeValue(value) ? value : null
}

function mapUsageStatus(value: number | null | undefined): UsageStatusValue | null {
  return isUsageStatusValue(value) ? value : null
}

/** 服务端文案空串与纯空白都视作缺失，交由调用方回退。 */
function serverText(value: string | null | undefined): string | null {
  const text = value?.trim()
  return text ? text : null
}

function mapCategory(value: MedicalStandardCategoryListReadModel): StandardCategory {
  return {
    id: value.categoryId ?? null,
    itemType: mapItemType(value.itemType),
    itemTypeText: serverText(value.itemTypeText),
    name: value.name ?? null,
    remark: value.remark ?? null,
    isValid: value.isValid ?? null,
    usageStatus: mapUsageStatus(value.usageStatus),
    usageStatusText: serverText(value.usageStatusText),
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
    usageStatusText: serverText(value.usageStatusText),
  }
}

function mapItem(value: MedicalStandardItemListReadModel): StandardItem {
  return {
    id: value.itemId ?? null,
    categoryId: value.categoryId ?? null,
    groupId: value.groupId ?? null,
    itemType: mapItemType(value.itemType),
    itemTypeText: serverText(value.itemTypeText),
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
