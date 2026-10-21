/**
 * 统计页左侧汇总列表卡片：维度切换、总行数与导出入口、行列表与分页。
 *
 * 只承接展示与用户输入回传；行数据、选中键、分页状态仍由页面主体持有，
 * 读取、导出与越界自愈不进入本组件。
 */
import { Button, Card, Empty, Pagination, Select, Space, Spin, Tag, Typography, type TablePaginationConfig } from 'antd'
import { createTablePagination } from '../../shared/tablePagination'
import type { UsageSummaryRow } from './recognitionUsageStatisticsApi'
import type { StatisticsGroupDimensionValue } from './recognitionStatisticsValues'
import type { SourceSummaryRow } from './sourceRecognitionStatisticsApi'
import {
  summaryRowAccessibleName,
  summaryRowKey,
  summaryRowPrimary,
  summaryRowSecondary,
  type StatisticsSide,
  type StatisticsSummaryRow,
} from './statisticsSummaryValues'

/** Table 分页配置 → Pagination 组件可接受字段；Table 专用的位置字段不能透传给普通分页组件。 */
function toListPagination(config: TablePaginationConfig) {
  return {
    size: config.size,
    current: config.current,
    pageSize: config.pageSize,
    total: config.total,
    disabled: config.disabled,
    showSizeChanger: config.showSizeChanger,
    pageSizeOptions: config.pageSizeOptions,
    showQuickJumper: config.showQuickJumper,
    showTotal: config.showTotal,
    onChange: config.onChange,
  }
}

/** 汇总列表卡片的输入边界：选中键与分页状态由页面主体持有，组件只回传变更。 */
interface StatisticsSummaryListProps {
  side: StatisticsSide
  rows: StatisticsSummaryRow[]
  dimension: StatisticsGroupDimensionValue
  dimensionOptions: { value: StatisticsGroupDimensionValue; label: string }[]
  totalCount: number
  selectedKey: string | null
  loading: boolean
  exporting: boolean
  emptyText: string
  pageIndex: number
  onDimensionChange: (value: StatisticsGroupDimensionValue) => void
  onPageChange: (pageIndex: number) => void
  onSelectRow: (rowKey: string) => void
  onExport: () => void
}

export function StatisticsSummaryList(props: StatisticsSummaryListProps) {
  /** 左列表固定每页 8 行、不提供页容量选择（设计 1.2）。 */
  const pagination = {
    ...toListPagination(createTablePagination({
      pageIndex: props.pageIndex,
      pageSize: 8,
      totalCount: props.totalCount,
      loading: props.loading,
      pageSizeOptions: [8],
      showSizeChanger: false,
      onChange: (nextPageIndex) => props.onPageChange(nextPageIndex),
    })),
    // 卡头已有「共 N 行」，分页不再重复总数。
    showTotal: undefined,
  }

  return <Card
    size='small'
    className='statistics-board-list-card'
    title={<Space size={8} align='center'>
      <Typography.Text type='secondary'>汇总维度</Typography.Text>
      <Select<StatisticsGroupDimensionValue>
        size='small'
        aria-label='汇总维度'
        style={{ width: 128 }}
        value={props.dimension}
        options={props.dimensionOptions}
        onChange={props.onDimensionChange}
      />
    </Space>}
    extra={<Space size={8} align='center'>
      <Typography.Text type='secondary'>共 {props.totalCount} 行</Typography.Text>
      <Button
        size='small'
        aria-label='导出汇总'
        loading={props.exporting}
        disabled={props.exporting}
        onClick={props.onExport}
      >导出汇总</Button>
    </Space>}
  >
    {props.side === 'usage' && props.dimension === 3 ? <Typography.Text type='secondary' className='statistics-board-note'>
      互认科室维度不含未反馈项：该维度的提醒次数与同期互认率仅统计已反馈的匹配项，跨维度的提醒合计不可直接对账。
    </Typography.Text> : null}
    {/* 汇总读取失败不渲染横幅（既定口径）：已有数据静默保留，错误由宿主统一提示，重试经查询按钮。 */}
    <div className='statistics-board-list'>
      {props.rows.length === 0
        ? (props.loading
            ? <div className='statistics-board-list__loading'><Spin size='small' /></div>
            : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={props.emptyText} />)
        : props.rows.map((row) => {
            const rowKey = summaryRowKey(props.side, row)
            return <button
              key={rowKey}
              type='button'
              className={`statistics-board-group-row${props.selectedKey === rowKey ? ' is-active' : ''}`}
              aria-label={summaryRowAccessibleName(props.side, row, props.dimension)}
              aria-pressed={props.selectedKey === rowKey}
              onClick={() => props.onSelectRow(rowKey)}
            >
              <span className='statistics-board-group-row__name'>
                {props.dimension === 4 ? <Tag color='blue' className='statistics-board-group-row__tag'>{row.itemTypeText}</Tag> : null}
                {summaryRowPrimary(props.side, row, props.dimension)}
              </span>
              <span className='statistics-board-group-row__secondary'>{summaryRowSecondary(props.side, row, props.dimension)}</span>
              <span className='statistics-board-group-row__metrics'>
                {props.side === 'usage' ? <>
                  <span>提醒 {(row as UsageSummaryRow).reminderCount}</span>
                  <span>采纳 {(row as UsageSummaryRow).adoptionCount}</span>
                  <span>不采纳 {(row as UsageSummaryRow).nonAdoptionCount}</span>
                  <span>引用 {(row as UsageSummaryRow).referenceCount}</span>
                </> : <span>被互认 {(row as SourceSummaryRow).recognitionCount}</span>}
              </span>
            </button>
          })}
    </div>
    <div className='statistics-board-list__pager'>
      <Pagination {...pagination} aria-label='汇总列表分页' />
    </div>
  </Card>
}
