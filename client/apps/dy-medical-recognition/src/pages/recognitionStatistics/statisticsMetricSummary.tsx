/**
 * 统计页指标摘要：把选中汇总行的类型数量、互认率与金额以静态文本呈现。
 * 数量与选中行的对应关系由页面主体计算，本组件不包含选择状态或交互。
 */
export interface StatisticsMetricSummaryItem {
  label: string
  value: string
  compact?: boolean
}

export function StatisticsMetricSummary(props: { items: readonly StatisticsMetricSummaryItem[] }) {
  return <div className='statistics-board-metric-summary' role='group' aria-label='指标摘要'>
    {props.items.map((item) => <div className='statistics-board-metric-summary__item' key={item.label}>
      <span className='statistics-board-metric-summary__label'>{item.label}</span>
      <span className={`statistics-board-metric-summary__value${item.compact === true ? ' statistics-board-metric-summary__value--compact' : ''}`}>{item.value}</span>
    </div>)}
  </div>
}
