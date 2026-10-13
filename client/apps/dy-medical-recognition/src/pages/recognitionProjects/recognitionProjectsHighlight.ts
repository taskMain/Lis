/**
 * 层级格高亮的目标与判定：页面列表（antd Table）与新增弹窗（CSS Grid 列视图）共用同一套规则。
 *
 * 三条判定对应三种被点中的层级格，彼此互斥：一次点击只点亮被点中的格与它的下级，
 * 上层的格不因下级被点中而点亮。因此：
 * - 点类型格 → 该类型格、其下全部分类格、其下全部分组格与项目格；
 * - 点分类格 → 该分类格、其下全部分组格与项目格，类型格保持未高亮；
 * - 点分组格 → 只有该分组格与它的项目格，分类格与类型格都保持未高亮。
 */
import type { MedicalItemTypeValue } from './recognitionProjectsApi'

/** 被点中的层级格：只标识位置，不携带任何写操作。 */
export type RecognitionProjectHighlight =
  | { level: 'itemType'; itemType: MedicalItemTypeValue | null }
  | { level: 'category'; itemType: MedicalItemTypeValue | null; categoryName: string }
  | { level: 'group'; itemType: MedicalItemTypeValue | null; categoryName: string; groupName: string }

/**
 * 类型格高亮判定：只在该类型格本身被点中时成立。
 * 项目类型缺失的类型格同样参与判定，点它只高亮它自己与它的下级。
 */
export function isItemTypeHighlighted(highlight: RecognitionProjectHighlight | null, itemType: MedicalItemTypeValue | null): boolean {
  return highlight !== null && highlight.level === 'itemType' && highlight.itemType === itemType
}

/**
 * 分类格高亮判定：所属类型格被点中时，该类型下的全部分类格一并高亮；
 * 分类格自己或它的下级分组格被点中时，只有它自己（或更下层）点亮。
 */
export function isCategoryHighlighted(
  highlight: RecognitionProjectHighlight | null,
  itemType: MedicalItemTypeValue | null,
  categoryName: string,
): boolean {
  if (highlight === null) return false
  if (highlight.level === 'itemType') return highlight.itemType === itemType
  if (highlight.level === 'category') return highlight.itemType === itemType && highlight.categoryName === categoryName
  return false
}

/**
 * 分组格高亮判定：所属分类格或所属类型格被点中时，该分组一并高亮；
 * 分组格自己被点中时按类型、分类、分组三者同时相等判定，因此点分组格只点亮该分组。
 */
export function isGroupHighlighted(
  highlight: RecognitionProjectHighlight | null,
  itemType: MedicalItemTypeValue | null,
  categoryName: string,
  groupName: string,
): boolean {
  if (highlight === null) return false
  if (highlight.level === 'itemType') return highlight.itemType === itemType
  if (highlight.level === 'category') return highlight.itemType === itemType && highlight.categoryName === categoryName
  return highlight.itemType === itemType && highlight.categoryName === categoryName && highlight.groupName === groupName
}
