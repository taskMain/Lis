/**
 * 互认项目列视图（新增弹窗专用）：类型｜分类｜分组｜项目四栏同时摊开，纵向位置表达从属关系。
 *
 * 版面用 CSS Grid 的显式定位表达：每个单元格自己声明 `gridColumn` 与 `gridRow`，不依赖自动排布。
 * 表头占首行，正文的第一个分组行落在第二行；一个分组占一行网格，该分组的全部项目行纵向排在
 * 第 4 列的项目格里。类型格与分类格只在该层级的第一个分组行渲染，并用 `grid-row: span N`
 * 跨过自己的全部分组行，因此同一层级只有一处层格，页面上也没有折叠或展开控件。
 *
 * 边界：本组件只负责渲染与点击回调，不读取组织、不发起请求、不做业务判断，
 * 也不持有高亮状态——高亮由调用方给出，点击只把被点中的层级格回传。
 * 页面列表已改为 antd Table 承载（S2-D16 2026-10-13 修订），本组件只服务新增弹窗：
 * 弹窗把层级格用作整组勾选，经 `levelActionText` 与 `renderLevelExtra` 复用同一套布局。
 *
 * 无障碍：层级格是原生 `button` 直接作为网格项，网格项默认在两条轴上拉伸，整个格因此都是点击区域，
 * 格内空白处同样可点；项目格只承载项目行与行内操作，自身不承担点击语义。
 */
import type { ReactNode } from 'react'
import {
  RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS,
  type RecognitionProjectColumnRow,
  type RecognitionProjectColumnView,
} from './recognitionProjectsApi'
import {
  isCategoryHighlighted,
  isGroupHighlighted,
  isItemTypeHighlighted,
  type RecognitionProjectHighlight,
} from './recognitionProjectsHighlight'

// 高亮的目标类型与判定与页面列表（antd Table）共用 `recognitionProjectsHighlight`；
// 本文件继续原样转出该类型，新增弹窗的既有导入路径不变。
export type { RecognitionProjectHighlight } from './recognitionProjectsHighlight'

/**
 * 类型、分类、分组三列宽取共享常量（与页面表格列宽同一份事实，S2-D16）；项目列弹性占满剩余宽度，
 * 下界 560px 保证窄视口下项目名与编码仍能横向摊开，不足部分由外层滚动容器承载。
 */
const GRID_TEMPLATE_COLUMNS = `${RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.itemType}px `
  + `${RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.category}px `
  + `${RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.group}px minmax(560px, 1fr)`

export interface RecognitionProjectCascadeProps {
  /** 当前展示的列视图；空数组时只渲染空态，不渲染表头与网格。 */
  view: RecognitionProjectColumnView
  /** 高亮的层级格；`null` 表示没有高亮，弹窗复用时传 `null`。 */
  highlight: RecognitionProjectHighlight | null
  /** 点击层级格时回传被点中的格；页面据此设置高亮，弹窗据此整组勾选。 */
  onLevelClick: (target: RecognitionProjectHighlight) => void
  /** 项目行内容；`highlighted` 表示该行所属分组格已被高亮，由调用方决定如何使用。 */
  renderRow: (row: RecognitionProjectColumnRow, highlighted: boolean) => ReactNode
  /** 没有任何可展示项目时的空态文案。 */
  emptyText: string
  /**
   * 层级格可访问名称里的动作词。默认「高亮」，表达这一格当前用于定位；
   * 弹窗把层级格用作整组勾选时传「整组勾选」，使名称与这一格的实际用途一致。
   */
  levelActionText?: string
  /**
   * 层级格内的附加内容。不传时层级格只显示层级名称；
   * 第二个参数是该层级格的可访问名称，附加控件据此与层级格共用同一个名称。
   */
  renderLevelExtra?: (target: RecognitionProjectHighlight, levelName: string) => ReactNode
}

/** 正文第一个分组行所在的行号：首行留给表头。 */
const FIRST_BODY_ROW = 2

/** 网格列号：类型、分类、分组、项目，与 CSS 的列宽声明一一对应。 */
const TYPE_COLUMN = 1
const CATEGORY_COLUMN = 2
const GROUP_COLUMN = 3
const ITEM_COLUMN = 4

/** 层级格的样式类：基础类提供整格可点与定位所需的外观，高亮类只改变背景，便于按类名定位与断言。 */
function levelClassName(kind: 'item-type' | 'category' | 'group', highlighted: boolean): string {
  const base = `recognition-projects-level recognition-projects-level-${kind}`
  return highlighted ? `${base} recognition-projects-level-highlighted` : base
}

/** 层级格的跨行声明：只在该层级的第一个分组行渲染，向下跨过它的全部分组行。 */
function spanRows(firstRow: number, span: number): string {
  return `${firstRow} / span ${span}`
}

export function RecognitionProjectCascade({
  view,
  highlight,
  onLevelClick,
  renderRow,
  emptyText,
  levelActionText = '高亮',
  renderLevelExtra,
}: RecognitionProjectCascadeProps) {
  if (view.length === 0) {
    return <div className='recognition-projects-cascade-empty'>{emptyText}</div>
  }

  const cells: ReactNode[] = [
    <div key='head-type' className='recognition-projects-cascade-head' style={{ gridColumn: TYPE_COLUMN, gridRow: 1 }}>类型</div>,
    <div key='head-category' className='recognition-projects-cascade-head' style={{ gridColumn: CATEGORY_COLUMN, gridRow: 1 }}>分类</div>,
    <div key='head-group' className='recognition-projects-cascade-head' style={{ gridColumn: GROUP_COLUMN, gridRow: 1 }}>分组</div>,
    <div key='head-item' className='recognition-projects-cascade-head' style={{ gridColumn: ITEM_COLUMN, gridRow: 1 }}>项目</div>,
  ]

  // 行号按「一个分组占一行」推进：层格不单独占行，因此它的跨越行数就等于下级分组行数，层格与分组行不会错位。
  let nextGridRow = FIRST_BODY_ROW
  for (const [itemTypeIndex, itemTypeBlock] of view.entries()) {
    const itemTypeHighlighted = isItemTypeHighlighted(highlight, itemTypeBlock.itemType)
    const itemTypeRowSpan = itemTypeBlock.categories.reduce((total, category) => total + category.groups.length, 0)
    const itemTypeGridRow = nextGridRow
    const itemTypeKey = `type-${itemTypeIndex}`
    const typeCells: ReactNode[] = []

    for (const [categoryIndex, category] of itemTypeBlock.categories.entries()) {
      const categoryHighlighted = isCategoryHighlighted(highlight, itemTypeBlock.itemType, category.categoryName)
      const categoryGridRow = nextGridRow
      const categoryKey = `${itemTypeKey}-category-${categoryIndex}`
      const categoryCells: ReactNode[] = []

      if (category.groups.length > 0) {
        const categoryTarget: RecognitionProjectHighlight = { level: 'category', itemType: itemTypeBlock.itemType, categoryName: category.categoryName }
        const categoryLevelName = `${levelActionText}分类「${category.categoryName}」及其下级`
        categoryCells.push(<button
          key={`${categoryKey}-level`}
          type='button'
          className={levelClassName('category', categoryHighlighted)}
          style={{ gridColumn: CATEGORY_COLUMN, gridRow: spanRows(categoryGridRow, category.groups.length) }}
          aria-label={categoryLevelName}
          onClick={(event) => {
            // 点击不再冒泡到页面根：页面根上的处理器负责「点击其他区域清除高亮」，
            // 让它收到这一次点击会立刻清掉刚设置的高亮。
            event.stopPropagation()
            onLevelClick(categoryTarget)
          }}
        >
          {renderLevelExtra?.(categoryTarget, categoryLevelName)}
          {category.categoryName}
        </button>)
      }

      for (const [groupIndex, group] of category.groups.entries()) {
        const groupHighlighted = isGroupHighlighted(highlight, itemTypeBlock.itemType, category.categoryName, group.groupName)
        const groupGridRow = nextGridRow
        nextGridRow += 1
        const groupKey = `${categoryKey}-group-${groupIndex}`
        const groupTarget: RecognitionProjectHighlight = {
          level: 'group',
          itemType: itemTypeBlock.itemType,
          categoryName: category.categoryName,
          groupName: group.groupName,
        }
        const groupLevelName = `${levelActionText}分组「${group.groupName}」及其下级`

        categoryCells.push(<button
          key={`${groupKey}-level`}
          type='button'
          className={levelClassName('group', groupHighlighted)}
          style={{ gridColumn: GROUP_COLUMN, gridRow: groupGridRow }}
          aria-label={groupLevelName}
          onClick={(event) => {
            event.stopPropagation()
            onLevelClick(groupTarget)
          }}
        >
          {renderLevelExtra?.(groupTarget, groupLevelName)}
          {group.groupName}
        </button>)

        /*
         * 项目格不阻止冒泡：点项目行或项目格应当让页面收到点击并清除高亮。
         * 所属分组格被高亮时项目格一并高亮：被点中的是「其整条下级链路」，
         * 项目层是这条链路的最后一层，缺了它就无法在全平铺结构里看出项目行归属于哪一次点击。
         */
        categoryCells.push(<div
          key={`${groupKey}-items`}
          className={groupHighlighted
            ? 'recognition-projects-cascade-items recognition-projects-level-highlighted'
            : 'recognition-projects-cascade-items'}
          style={{ gridColumn: ITEM_COLUMN, gridRow: groupGridRow }}
        >
          {group.rows.map((row) => <div className='recognition-projects-row' data-row-key={row.rowIdentity} key={row.rowIdentity}>
            {renderRow(row, groupHighlighted)}
          </div>)}
        </div>)
      }

      typeCells.push(...categoryCells)
    }

    if (itemTypeRowSpan > 0) {
      const itemTypeTarget: RecognitionProjectHighlight = { level: 'itemType', itemType: itemTypeBlock.itemType }
      const itemTypeLevelName = `${levelActionText}类型「${itemTypeBlock.itemTypeText}」及其下级`
      cells.push(<button
        key={`${itemTypeKey}-level`}
        type='button'
        className={levelClassName('item-type', itemTypeHighlighted)}
        style={{ gridColumn: TYPE_COLUMN, gridRow: spanRows(itemTypeGridRow, itemTypeRowSpan) }}
        aria-label={itemTypeLevelName}
        onClick={(event) => {
          event.stopPropagation()
          onLevelClick(itemTypeTarget)
        }}
      >
        {renderLevelExtra?.(itemTypeTarget, itemTypeLevelName)}
        {itemTypeBlock.itemTypeText}
      </button>)
    }
    cells.push(...typeCells)
  }

  return <div className='recognition-projects-cascade' style={{ gridTemplateColumns: GRID_TEMPLATE_COLUMNS }}>{cells}</div>
}
