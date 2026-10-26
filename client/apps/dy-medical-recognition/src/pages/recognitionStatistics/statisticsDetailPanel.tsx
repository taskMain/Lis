/**
 * 统计页明细表容器：只负责表格、分页、空态与可选的行使展开。
 *
 * 列定义与行展开内容由各侧列表区主体传入，容器不按侧别取值分支装配表格，
 * 使使用方与提供方的明细列可以各自改版。
 */
import { Alert, Empty, Table, type TableColumnsType, type TablePaginationConfig, type TableProps } from 'antd'
import type { UsageDetailRow } from './recognitionUsageStatisticsApi'
import type { SourceDetailRow } from './sourceRecognitionStatisticsApi'

/** 两个统计侧共用的明细行联合；实际行类型由调用方决定。 */
export type StatisticsDetailRow = UsageDetailRow | SourceDetailRow

export function StatisticsDetailPanel<T extends { recognitionMatchItemId: string }>(props: {
  rows: T[]
  columns: TableColumnsType<T>
  loading: boolean
  pagination: TablePaginationConfig
  emptyText: string
  expandable?: TableProps<T>['expandable']
  /** 当页行里组织名称缺失的编码；非空时在表格上方给非阻断提示。 */
  missingNameCodes?: string[]
}) {
  // 表格总宽按各列声明宽度求和；有行展开列时补上其列宽，窄屏下由容器横向滚动。
  const expandColumnWidth = typeof props.expandable?.columnWidth === 'number' ? props.expandable.columnWidth : 0
  const scrollX = expandColumnWidth + props.columns.reduce(
    (total, column) => total + (typeof column.width === 'number' ? column.width : 0),
    0,
  )
  // 名称缺失只提示不阻断：数据照常展示，编码列出来便于排查历史行的组织漂移。
  const missingCodes = props.missingNameCodes ?? []
  const shownCodes = missingCodes.slice(0, 3).join('、')
  const restCount = missingCodes.length - 3
  const missingNameHint = missingCodes.length === 0 ? null : <Alert
    type='warning'
    showIcon
    style={{ marginBottom: 8 }}
    message={`部分行组织名称无法解析，编码：${shownCodes}${restCount > 0 ? ` 等 ${restCount} 项` : ''}`}
  />
  return <div className='statistics-board-detail-table'>
    {missingNameHint}
    <Table<T>
      rowKey='recognitionMatchItemId'
      size='small'
      tableLayout='fixed'
      columns={props.columns}
      dataSource={props.rows}
      loading={props.loading}
      scroll={{ x: scrollX }}
      expandable={props.expandable}
      pagination={props.pagination}
      locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={props.emptyText} /> }}
    />
  </div>
}
