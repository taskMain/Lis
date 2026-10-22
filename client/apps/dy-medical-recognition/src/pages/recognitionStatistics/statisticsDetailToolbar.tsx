/**
 * 使用方两页明细工具行：选中上下文文案、不采纳原因筛选与明细类型页签。
 *
 * 页签只承担明细类型切换；类型数量统一由上方静态指标摘要展示，避免同一数字出现两次。
 * 键盘左右/Home/End 切换沿用 WAI-ARIA tablist 口径，页签与 tabpanel 的 id 由共享取值模块生成。
 * 提供方两页没有明细类型切换与原因筛选，其列表区主体直接渲染一行上下文（S6-D24）。
 */
import { Select, Typography } from 'antd'
import type { KeyboardEvent } from 'react'
import type { UsageDetailTypeValue } from './recognitionStatisticsValues'
import { USAGE_DETAIL_TAB_ITEMS, usageDetailPanelId, usageDetailTabId } from './statisticsDetailTabs'

interface StatisticsDetailToolbarProps {
  /** 页面主体的挂载实例前缀；页签 id、aria-controls 与焦点定位共用。 */
  idPrefix: string
  contextText: string
  showReasonFilter: boolean
  reasonCode: string | null
  reasonOptions: { value: string; label: string }[]
  detailType: UsageDetailTypeValue
  onReasonChange: (value: string | null) => void
  onDetailTypeChange: (value: UsageDetailTypeValue) => void
}

export function StatisticsDetailToolbar(props: StatisticsDetailToolbarProps) {
  const handleUsageTabKeyDown = (event: KeyboardEvent<HTMLButtonElement>, currentValue: UsageDetailTypeValue) => {
    const currentIndex = USAGE_DETAIL_TAB_ITEMS.findIndex((item) => item.value === currentValue)
    if (currentIndex < 0) return
    let nextIndex = currentIndex
    if (event.key === 'ArrowRight') nextIndex = (currentIndex + 1) % USAGE_DETAIL_TAB_ITEMS.length
    else if (event.key === 'ArrowLeft') nextIndex = (currentIndex - 1 + USAGE_DETAIL_TAB_ITEMS.length) % USAGE_DETAIL_TAB_ITEMS.length
    else if (event.key === 'Home') nextIndex = 0
    else if (event.key === 'End') nextIndex = USAGE_DETAIL_TAB_ITEMS.length - 1
    else return
    event.preventDefault()
    const next = USAGE_DETAIL_TAB_ITEMS[nextIndex]
    props.onDetailTypeChange(next.value)
    window.setTimeout(() => document.getElementById(usageDetailTabId(props.idPrefix, String(next.value)))?.focus(), 0)
  }

  return <div className='statistics-board-detail-toolbar'>
    <Typography.Text type='secondary' className='statistics-board-detail-context'>{props.contextText}</Typography.Text>
    <div className='statistics-board-detail-actions'>
      {props.showReasonFilter ? <div className='statistics-board-reason-filter'>
        <span className='statistics-board-reason-filter__label'>不采纳原因</span>
        <Select<string>
          aria-label='不采纳原因'
          className='statistics-board-reason-filter__select'
          value={props.reasonCode ?? ''}
          options={props.reasonOptions}
          onChange={(value) => props.onReasonChange(value === '' ? null : value)}
        />
      </div> : null}
      {/* 明细类型切换位于表格工具行；使用方四类。 */}
      <div className='statistics-board-type-control'>
        <span className='statistics-board-type-control__label'>明细类型</span>
        <div className='statistics-board-type-tabs' role='tablist' aria-label='明细类型'>
          {USAGE_DETAIL_TAB_ITEMS.map((item) => {
            const selected = item.value === props.detailType
            return <button
              key={item.value}
              id={usageDetailTabId(props.idPrefix, String(item.value))}
              type='button'
              role='tab'
              className={`statistics-board-type-tab${selected ? ' is-active' : ''}`}
              aria-selected={selected}
              aria-controls={usageDetailPanelId(props.idPrefix, String(item.value))}
              tabIndex={selected ? 0 : -1}
              onClick={() => props.onDetailTypeChange(item.value)}
              onKeyDown={(event) => handleUsageTabKeyDown(event, item.value)}
            >{item.label}</button>
          })}
        </div>
      </div>
    </div>
  </div>
}
