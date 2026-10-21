/**
 * 统计页右侧明细表容器：只负责表格、分页、空态和使用方行展开。
 * 明细列定义和值组件由统计页主体传入，避免明细表与页面编排状态耦合。
 */
import { Empty, Table, type TableColumnsType, type TablePaginationConfig, type TableProps } from 'antd'
import type { UsageDetailRow } from './recognitionUsageStatisticsApi'
import type { SourceDetailRow } from './sourceRecognitionStatisticsApi'
import { UsageDetailExpanded } from './statisticsDetailCells'

/** 两个统计侧共用的明细行联合；实际行类型由 `side` 决定。 */
export type StatisticsDetailRow = UsageDetailRow | SourceDetailRow

/** 通用明细表：使用方行展开列宽固定为 36px，提供方不传 expandable。 */
function StatisticsDetailTable<T extends { recognitionMatchItemId: string }>(props: {
  rows: T[]
  columns: TableColumnsType<T>
  loading: boolean
  pagination: TablePaginationConfig
  emptyText: string
  scrollX: number
  expandable?: TableProps<T>['expandable']
}) {
  return <Table<T>
    rowKey='recognitionMatchItemId'
    size='small'
    tableLayout='fixed'
    columns={props.columns}
    dataSource={props.rows}
    loading={props.loading}
    scroll={{ x: props.scrollX }}
    expandable={props.expandable}
    pagination={props.pagination}
    locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={props.emptyText} /> }}
  />
}

/** 明细面板：按侧别装配列与行展开，表格宽度由实际列宽计算。 */
export function StatisticsDetailPanel(props: {
  side: 'usage' | 'source'
  rows: StatisticsDetailRow[]
  loading: boolean
  pagination: TablePaginationConfig
  usageColumns: TableColumnsType<UsageDetailRow>
  sourceColumns: TableColumnsType<SourceDetailRow>
  emptyText: string
}) {
  const usageScrollX = 36 + props.usageColumns.reduce(
    (total, column) => total + (typeof column.width === 'number' ? column.width : 0),
    0,
  )
  const sourceScrollX = props.sourceColumns.reduce(
    (total, column) => total + (typeof column.width === 'number' ? column.width : 0),
    0,
  )
  return <div className='statistics-board-detail-table'>
    {props.side === 'usage'
      ? <StatisticsDetailTable<UsageDetailRow>
          rows={props.rows as UsageDetailRow[]}
          columns={props.usageColumns}
          loading={props.loading}
          pagination={props.pagination}
          emptyText={props.emptyText}
          scrollX={usageScrollX}
          expandable={{
            columnWidth: 36,
            expandedRowRender: (row) => <UsageDetailExpanded row={row} />,
          }}
        />
      : <StatisticsDetailTable<SourceDetailRow>
          rows={props.rows as SourceDetailRow[]}
          columns={props.sourceColumns}
          loading={props.loading}
          pagination={props.pagination}
          emptyText={props.emptyText}
          scrollX={sourceScrollX}
        />}
  </div>
}
