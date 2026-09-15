/**
 * 标准项目目录维护页面的原型数据与派生逻辑。
 *
 * 用途：仅供本阶段页面形态比对，共用同一份内存数据与同一套业务规则。
 * 本文件不是生产代码，生产实现不沿用其结构与样式。
 */

export type VariantKey = 'A' | 'A2' | 'B' | 'C'
export type ItemType = '检验' | '检查'

/** 使用情况派生值，与后端 `UsageStatus` 对应：0=未被使用，1=已被下级使用。 */
export const UsageStatus = { Unused: 0, InUse: 1 } as const
export type UsageStatusValue = (typeof UsageStatus)[keyof typeof UsageStatus]

export interface Category {
  id: string
  itemType: ItemType
  name: string
  remark: string | null
  isValid: boolean
}

export interface Group {
  id: string
  categoryId: string
  name: string
  remark: string | null
  isValid: boolean
}

export interface StandardItem {
  id: string
  categoryId: string
  groupId: string
  code: string
  name: string
  remark: string | null
  isValid: boolean
}

export interface PrototypeData {
  categories: Category[]
  groups: Group[]
  items: StandardItem[]
}

export const variants: Array<{ key: VariantKey; name: string; structure: string }> = [
  { key: 'A', name: '目录树 + 项目列表', structure: '左侧固定宽度目录树（类型→分类→分组）+ 右侧项目列表；分类、分组、项目备注均用弹窗' },
  { key: 'A2', name: 'A 优化版', structure: '目录树 + 项目列表' },
  { key: 'B', name: '目录铺开 + 行内改备注', structure: '目录横向铺开为分类卡片与分组标签，项目表格占满全宽；分类与分组用弹窗，项目备注改行内编辑' },
  { key: 'C', name: '窄树 + 就地编辑面板', structure: '左侧窄树 + 右侧就地编辑面板 + 项目表格；分类与分组在右侧面板直接改，项目仍用弹窗' },
]

export const initialData: PrototypeData = {
  categories: [
    { id: 'c1', itemType: '检验', name: '检验专业', remark: '平台检验类项目根分类', isValid: true },
    { id: 'c2', itemType: '检验', name: '病理专业', remark: null, isValid: true },
    { id: 'c3', itemType: '检查', name: '放射影像专业', remark: null, isValid: true },
    { id: 'c4', itemType: '检查', name: '超声影像专业', remark: '含心脏与腹部彩超', isValid: false },
  ],
  groups: [
    { id: 'g1', categoryId: 'c1', name: '传染病检验', remark: null, isValid: true },
    { id: 'g2', categoryId: 'c1', name: '生化常规检验', remark: null, isValid: true },
    { id: 'g3', categoryId: 'c1', name: '血液常规检验', remark: '已并入生化流程', isValid: false },
    { id: 'g4', categoryId: 'c2', name: '组织病理', remark: null, isValid: true },
    { id: 'g5', categoryId: 'c3', name: 'CT', remark: null, isValid: true },
    { id: 'g6', categoryId: 'c3', name: 'MR', remark: null, isValid: true },
    { id: 'g7', categoryId: 'c4', name: '腹部彩超', remark: null, isValid: true },
  ],
  items: [
    { id: 'i1', categoryId: 'c1', groupId: 'g1', code: '002504030040000-01001', name: '乙型肝炎表面抗原（HBsAg）', remark: '门诊与住院共用', isValid: true },
    { id: 'i2', categoryId: 'c1', groupId: 'g1', code: '002504030050000-01001', name: '乙型肝炎表面抗体（anti-HBs）', remark: null, isValid: true },
    { id: 'i3', categoryId: 'c1', groupId: 'g2', code: '002503040010000-01001', name: '钾（K）', remark: null, isValid: true },
    { id: 'i4', categoryId: 'c1', groupId: 'g2', code: '002503040020000-01001', name: '钠（Na）', remark: null, isValid: true },
    { id: 'i5', categoryId: 'c1', groupId: 'g3', code: '002501010010000-01001', name: '白细胞计数（WBC）', remark: null, isValid: true },
    { id: 'i6', categoryId: 'c2', groupId: 'g4', code: '002704010010000-01001', name: '组织病理学检查', remark: null, isValid: true },
    { id: 'i7', categoryId: 'c3', groupId: 'g5', code: '002103000010000-02002', name: '胸部CT平扫', remark: '64排CT', isValid: true },
    { id: 'i8', categoryId: 'c3', groupId: 'g6', code: '002102000010000-01002', name: '头颅MRI平扫', remark: null, isValid: false },
    { id: 'i9', categoryId: 'c4', groupId: 'g7', code: '002202010010000-01001', name: '肝', remark: null, isValid: true },
  ],
}

/** 生成原型内新对象的标识，仅用于内存态。 */
let sequence = 100
export function nextId(prefix: string): string {
  sequence += 1
  return `${prefix}${sequence}`
}

/** 分类的「使用情况」：按是否存在分组判定，含已停用分组。 */
export function categoryUsage(data: PrototypeData, categoryId: string): UsageStatusValue {
  return data.groups.some((group) => group.categoryId === categoryId) ? UsageStatus.InUse : UsageStatus.Unused
}

/** 分组的「使用情况」：按是否存在标准项目判定，含已停用项目。 */
export function groupUsage(data: PrototypeData, groupId: string): UsageStatusValue {
  return data.items.some((item) => item.groupId === groupId) ? UsageStatus.InUse : UsageStatus.Unused
}

export function usageLabel(usage: UsageStatusValue): string {
  return usage === UsageStatus.InUse ? '已被下级使用' : '未被使用'
}

/**
 * 分类的 `ItemType` 冻结判断：存在任何分组（含已停用）时不可修改类型。
 * 名称与备注不受此限制。
 */
export function isCategoryTypeFrozen(data: PrototypeData, categoryId: string): boolean {
  return data.groups.some((group) => group.categoryId === categoryId)
}

/** 标准项目「当前有效」：自身、所属分组、所属分类均启用。 */
export function isItemEffective(data: PrototypeData, item: StandardItem): boolean {
  if (!item.isValid) return false
  const group = data.groups.find((entry) => entry.id === item.groupId)
  const category = data.categories.find((entry) => entry.id === item.categoryId)
  return Boolean(group?.isValid && category?.isValid)
}

/** 「当前有效」不可用时的原因说明。 */
export function ineffectiveReasons(data: PrototypeData, item: StandardItem): string[] {
  const reasons: string[] = []
  const group = data.groups.find((entry) => entry.id === item.groupId)
  const category = data.categories.find((entry) => entry.id === item.categoryId)
  if (!item.isValid) reasons.push('标准项目自身已停用')
  if (!group) reasons.push('所属分组缺失')
  else if (!group.isValid) reasons.push('所属分组已停用')
  if (!category) reasons.push('所属分类缺失')
  else if (!category.isValid) reasons.push('所属分类已停用')
  return reasons
}

/** 本地字面包含匹配：去首尾空白、统一小写，`%`、`_` 为普通字符。 */
export function matchesText(source: string | null | undefined, keyword: string): boolean {
  const needle = keyword.trim().toLowerCase()
  if (!needle) return true
  return (source ?? '').toLowerCase().includes(needle)
}

export interface ItemFilter {
  code: string
  name: string
  isValid: 'all' | 'enabled' | 'disabled'
}

export const emptyItemFilter: ItemFilter = { code: '', name: '', isValid: 'all' }

export interface TreeScope {
  itemType: ItemType | null
  categoryId: string | null
  groupId: string | null
}

export const defaultScope: TreeScope = { itemType: '检验', categoryId: null, groupId: null }

/** 按目录树选中范围与筛选条计算右侧列表可见结果，全部为本地计算。 */
export function visibleItems(data: PrototypeData, scope: TreeScope, filter: ItemFilter): StandardItem[] {
  return data.items.filter((item) => {
    if (scope.groupId) {
      if (item.groupId !== scope.groupId) return false
    } else if (scope.categoryId) {
      if (item.categoryId !== scope.categoryId) return false
    } else if (scope.itemType) {
      const category = data.categories.find((entry) => entry.id === item.categoryId)
      if (category?.itemType !== scope.itemType) return false
    }
    if (!matchesText(item.code, filter.code)) return false
    if (!matchesText(item.name, filter.name)) return false
    if (filter.isValid === 'enabled' && !item.isValid) return false
    if (filter.isValid === 'disabled' && item.isValid) return false
    return true
  })
}

export function itemTypeText(itemType: ItemType): string {
  return itemType
}

export function categoryName(data: PrototypeData, categoryId: string): string {
  return data.categories.find((entry) => entry.id === categoryId)?.name ?? '—'
}

export function groupName(data: PrototypeData, groupId: string): string {
  return data.groups.find((entry) => entry.id === groupId)?.name ?? '—'
}

export function categoryItemType(data: PrototypeData, categoryId: string): ItemType | null {
  return data.categories.find((entry) => entry.id === categoryId)?.itemType ?? null
}
