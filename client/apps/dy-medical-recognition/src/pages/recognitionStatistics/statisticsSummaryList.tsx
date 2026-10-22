/**
 * 统计页左侧汇总列表卡片的共用骨架：维度切换、总行数与导出入口、行列表、分页与空态。
 *
 * 使用方与提供方两页共用本卡片；行的主名称、次行与指标块由调用方以渲染片段传入，
 * 卡片只承接展示与用户输入回传，选中键、分页状态与读取仍由页面主体持有。
 */
import { Button, Card, Empty, Pagination, Select, Space, Spin, Typography } from 'antd'
import type { ReactNode } from 'react'
import { createTablePagination, toPaginationProps } from '../../shared/tablePagination'
import type { StatisticsGroupDimensionValue } from './recognitionStatisticsValues'

/** 汇总列表卡片的输入边界：行内容由调用方给出，状态与回调由页面主体持有。 */
export interface StatisticsSummaryListProps<Row> {
  rows: Row[]
  dimension: StatisticsGroupDimensionValue
  dimensionOptions: { value: StatisticsGroupDimensionValue; label: string }[]
  totalCount: number
  selectedKey: string | null
  loading: boolean
  exporting: boolean
  emptyText: string
  pageIndex: number
  /** 行的稳定键：由维度相关字段合成。 */
  rowKey: (row: Row) => string
  /** 行按钮的可访问名称：名称、次行与指标一起进入。 */
  accessibleName: (row: Row) => string
  /** 主名称行内容；调用方可自带标签。 */
  renderName: (row: Row) => ReactNode
  /** 归属次行内容。 */
  renderSecondary: (row: Row) => ReactNode
  /** 行尾指标块：容器元素与类名由调用方给出（使用方四项指标、提供方行尾计数块）。 */
  renderMetrics: (row: Row) => ReactNode
  /** 维度说明段；返回 `null` 表示该维度不渲染。 */
  renderNote?: () => ReactNode
  onDimensionChange: (value: StatisticsGroupDimensionValue) => void
  onPageChange: (pageIndex: number) => void
  onSelectRow: (rowKey: string) => void
  onExport: () => void
}

export function StatisticsSummaryList<Row>(props: StatisticsSummaryListProps<Row>) {
  /** 左列表固定每页 8 行、不提供页容量选择（设计 1.2）。 */
  const pagination = {
    ...toPaginationProps(createTablePagination({
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
    {props.renderNote?.()}
    {/* 汇总读取失败不渲染横幅（既定口径）：已有数据静默保留，错误由宿主统一提示，重试经查询按钮。 */}
    <div className='statistics-board-list'>
      {props.rows.length === 0
        ? (props.loading
            ? <div className='statistics-board-list__loading'><Spin size='small' /></div>
            : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={props.emptyText} />)
        : props.rows.map((row) => {
            const rowKey = props.rowKey(row)
            return <button
              key={rowKey}
              type='button'
              className={`statistics-board-group-row${props.selectedKey === rowKey ? ' is-active' : ''}`}
              aria-label={props.accessibleName(row)}
              aria-pressed={props.selectedKey === rowKey}
              onClick={() => props.onSelectRow(rowKey)}
            >
              <span className='statistics-board-group-row__text'>
                <span className='statistics-board-group-row__name'>{props.renderName(row)}</span>
                <span className='statistics-board-group-row__secondary'>{props.renderSecondary(row)}</span>
              </span>
              {props.renderMetrics(row)}
            </button>
          })}
    </div>
    <div className='statistics-board-list__pager'>
      <Pagination {...pagination} aria-label='汇总列表分页' />
    </div>
  </Card>
}
