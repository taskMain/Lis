/**
 * 统计页明细类型页签的共享取值与可访问性标识：页签按钮与对应 tabpanel 必须使用同一组 id。
 */
import { USAGE_DETAIL_TYPE, type UsageDetailTypeValue } from './recognitionStatisticsValues'

/** 使用方明细类型页签（S6-D2 明细类型枚举取值）；提供方无页签切换。 */
export const USAGE_DETAIL_TAB_ITEMS = [
  { value: USAGE_DETAIL_TYPE.reminder, label: '提醒' },
  { value: USAGE_DETAIL_TYPE.adoption, label: '采纳' },
  { value: USAGE_DETAIL_TYPE.nonAdoption, label: '不采纳' },
  { value: USAGE_DETAIL_TYPE.reference, label: '引用' },
] as const

/**
 * 每个 StatisticsBoard 挂载实例的全局唯一前缀：独立 React root（多个 micro-app）之间也不能重复。
 * 优先 `crypto.randomUUID`；非安全上下文等不可用时回退时间戳 + 自增序号 + 随机串。
 */
let detailTabsFallbackSequence = 0

export function createDetailTabsIdPrefix(): string {
  try {
    const uuid = globalThis.crypto?.randomUUID?.()
    if (typeof uuid === 'string' && uuid.length > 0) return `statistics-detail-${uuid}`
  } catch {
    // 随机源不可用时走确定性回退，不阻断页面渲染。
  }
  detailTabsFallbackSequence += 1
  return `statistics-detail-${Date.now().toString(36)}-${detailTabsFallbackSequence.toString(36)}-${Math.random().toString(36).slice(2, 10)}`
}

/** 页签按钮 id；`idPrefix` 由页面主体在挂载时生成，保证多实例宿主内唯一，并与 tabpanel 的 `aria-labelledby` 共用。 */
export function usageDetailTabId(idPrefix: string, key: string): string {
  return `${idPrefix}-usage-tab-${key}`
}

/** tabpanel id；与页签的 `aria-controls` 共用同一实例前缀。 */
export function usageDetailPanelId(idPrefix: string, key: string): string {
  return `${idPrefix}-usage-panel-${key}`
}

/** 明细类型中文名；提供方侧不经过本函数。 */
export function usageDetailTypeLabel(value: UsageDetailTypeValue): string {
  return USAGE_DETAIL_TAB_ITEMS.find((item) => item.value === value)?.label ?? ''
}
