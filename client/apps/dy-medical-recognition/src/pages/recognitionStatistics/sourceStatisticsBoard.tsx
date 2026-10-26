/**
 * 提供方两页（「提供方医院被互认统计」平台页与「本院被互认统计」本院页）的列表区主体。
 *
 * 采用承接方核查版式（S6-D24）：左栏为两行式分组清单与行尾被互认计数块，右卡卡头即归属链
 * （组织、医院、院区三行）并在右侧内联被互认次数与导出明细，卡头下只有一行上下文；
 * 明细首列为使用方（三行式、固定左侧），其后为标准项目、项目类型、分类、分组、互认时间、
 * 患者、科室与医生，提供方院区为条件列（医院维度且平台页未锁院区时出现）。
 *
 * 数据源、读取状态与回调由统计页主体注入，本组件只承接列表区的展示与用户输入回传；
 * 汇总清单卡片与使用方两页共用同一骨架，行的主名称、次行与指标块以渲染片段传入。
 * 提供方只有被互认次数一个指标：明细不提供行展开，也不提供匹配记录入口（S6-D10）。
 */
import { Button, Card, Empty, Space, Tag, Typography, type TableColumnsType, type TablePaginationConfig } from 'antd'
import { useMemo } from 'react'
import type { SourceDetailRow, SourceSummaryRow } from './sourceRecognitionStatisticsApi'
import type { StatisticsGroupDimensionValue } from './recognitionStatisticsValues'
import { formatStatisticsDateTime, missingScopeNameCodes } from './statisticsDisplays'
import { DeptDoctorCell, ItemCell, OptionalTextCell, PatientCell, PartyCell, TextCell } from './statisticsDetailCells'
import {
  partyName,
  sourceDetailTableEmptyText,
  statisticsDetailCardEmptyText,
  statisticsSummaryEmptyText,
} from './statisticsDetailValues'
import { StatisticsDetailPanel } from './statisticsDetailPanel'
import { StatisticsSummaryList } from './statisticsSummaryList'
import {
  joinSummaryParts,
  sourceSummaryPrimary,
  sourceSummaryRowKey,
  sourceSummarySecondary,
} from './statisticsSummaryValues'
import { projectNameText } from './statisticsDetailValues'

/** 左栏分组清单的读取状态与当前选中键。 */
export interface SourceStatisticsSummaryState {
  rows: SourceSummaryRow[]
  totalCount: number
  pageIndex: number
  selectedKey: string | null
  loading: boolean
  failure: boolean
}

/** 右卡明细的读取状态、分页与上下文行。 */
export interface SourceStatisticsDetailState {
  rows: SourceDetailRow[]
  loading: boolean
  failure: boolean
  pagination: TablePaginationConfig
  /** 统计时间区间文本（起止日期）。 */
  timeRangeText: string
  /** 当前明细总数：上下文行的「共 N 条」取此值。 */
  detailTotal: number
  /** 医院维度且平台页未锁院区时为真，明细保留「提供方院区」列。 */
  showBranchColumn: boolean
}

/** 列表区的输入边界：行数据、读取状态、范围取值与回调，组件只做展示与输入回传。 */
export interface SourceStatisticsBoardProps {
  dimension: StatisticsGroupDimensionValue
  dimensionOptions: { value: StatisticsGroupDimensionValue; label: string }[]
  summary: SourceStatisticsSummaryState
  detail: SourceStatisticsDetailState
  /**
   * 上下文行承接范围的展示值：标识与名称都由统计页主体按已生效筛选派生。
   * 标识是组织实例 Id；名称缺失时展示层显示「未知」，不回退显示标识。
   */
  scopeValues: {
    organizationCode?: string
    organizationName?: string | null
    hospitalCode?: string
    hospitalName?: string | null
    branchCode?: string
    branchName?: string | null
  }
  /** 承接范围是否包含院区：平台页仅院区维度为真，本院页恒为真。 */
  includeScopeBranch: boolean
  /** 选中分组行；未选中时右卡显示空态。 */
  selectedRow: SourceSummaryRow | null
  /** 本院页登录院区缺失时的空态文案；平台页不传。 */
  missingScopeText?: string
  exporting: boolean
  onDimensionChange: (value: StatisticsGroupDimensionValue) => void
  onSummaryPageChange: (pageIndex: number) => void
  onSelectRow: (rowKey: string) => void
  onExportSummary: () => void
  onExportDetails: () => void
}

/**
 * 提供方明细列（S6-D24、S6-D25）：首列为使用方三行式并固定在左侧，其后依次为标准项目
 * （名称主行、编码次行）、项目类型、分类、分组、互认时间、患者、科室与医生；
 * 提供方院区为条件列，医院维度且平台页未锁院区时追加在末位。
 */
function sourceDetailColumns(showBranchColumn: boolean): TableColumnsType<SourceDetailRow> {
  const columns: TableColumnsType<SourceDetailRow> = [
    { title: '使用方', width: 200, fixed: 'left', render: (_, row) => <PartyCell {...row.receiver} /> },
    { title: '标准项目', width: 210, render: (_, row) => <ItemCell item={row} /> },
    // 项目类型文本的缺失与越界兜底由适配层统一给出，列表区不再补默认值。
    { title: '项目类型', width: 80, render: (_, row) => <Tag color='blue'>{row.itemTypeText}</Tag> },
    { title: '分类', width: 100, render: (_, row) => <OptionalTextCell value={row.categoryName} /> },
    { title: '分组', width: 100, render: (_, row) => <OptionalTextCell value={row.groupName} /> },
    { title: '互认时间', width: 145, render: (_, row) => <TextCell value={formatStatisticsDateTime(row.recognitionTime)} /> },
    { title: '患者', width: 150, render: (_, row) => <PatientCell patientName={row.patientName} identityDocumentNo={row.identityDocumentNo} /> },
    { title: '科室 / 医生', width: 140, render: (_, row) => <DeptDoctorCell deptName={row.recognitionDeptName} doctorName={row.recognitionDoctorName} /> },
  ]
  if (showBranchColumn) {
    columns.push({ title: '提供方院区', width: 96, render: (_, row) => <TextCell value={row.source.branchName} code={row.source.branchCode} /> })
  }
  return columns
}

/**
 * 卡头标题按汇总维度取字段：医院维度显示组织与医院两行；院区维度显示组织、医院、院区三行；
 * 标准项目维度不显示三级归属链，改为标准项目名称与「编码 · 分类 / 分组」两行。
 * 不适用的层级直接省略，不显示「未知」占位。
 */
function sourceDetailTitle(row: SourceSummaryRow, dimension: StatisticsGroupDimensionValue) {
  if (dimension === 4) {
    return <div className='statistics-board-source-chain'>
      <span className='statistics-board-source-chain__project'>{projectNameText(row.standardProjectName)}</span>
      <span className='statistics-board-source-chain__secondary'>{sourceSummarySecondary(row, 4)}</span>
    </div>
  }
  return <div className='statistics-board-source-chain'>
    <span className='statistics-board-source-chain__organization'>{partyName(row.sourceOrganizationName)}</span>
    <span className='statistics-board-source-chain__hospital'>{partyName(row.sourceHospitalName)}</span>
    {dimension === 2
      ? <span className='statistics-board-source-chain__branch'>{partyName(row.sourceBranchName)}</span>
      : null}
  </div>
}

/** 承接范围文本：三级名称按包含的层级拼接，名称缺失显示「未知」，不回退显示组织实例 Id。 */
function sourceScopeText(scope: {
  organizationName?: string | null
  hospitalName?: string | null
  branchName?: string | null
}, includeBranch: boolean): string {
  return joinSummaryParts([
    scope.organizationName ?? '未知',
    scope.hospitalName ?? '未知',
    includeBranch ? scope.branchName ?? '未知' : null,
  ])
}

/**
 * 提供方列表区主体。
 *
 * 左栏使用统计页共用的汇总清单卡片，右栏为选中分组的明细卡；卡头与上下文行按汇总维度取值，
 * 数据源、读取状态与回调全部由统计页主体注入，组件本身不发起请求、不持有选中与分页状态。
 */
export function SourceStatisticsBoard(props: SourceStatisticsBoardProps) {
  const columns = useMemo(
    () => sourceDetailColumns(props.detail.showBranchColumn),
    [props.detail.showBranchColumn],
  )
  const summaryEmptyText = statisticsSummaryEmptyText(props.summary.failure, props.missingScopeText)
  const detailCardEmptyText = statisticsDetailCardEmptyText(props.summary.failure)
  const detailTableEmptyText = sourceDetailTableEmptyText(
    props.detail.failure,
    props.detail.rows.length,
    props.missingScopeText,
  )
  const selectedRow = props.selectedRow
  const detailContextText = selectedRow === null
    ? ''
    : `统计时间 ${props.detail.timeRangeText} · 承接范围 ${sourceScopeText(props.scopeValues, props.includeScopeBranch)} · 共 ${props.detail.detailTotal} 条`

  return <div className='statistics-board-master statistics-board-master--source'>
    <section aria-label='汇总列表'>
      <StatisticsSummaryList<SourceSummaryRow>
        rows={props.summary.rows}
        dimension={props.dimension}
        dimensionOptions={props.dimensionOptions}
        totalCount={props.summary.totalCount}
        selectedKey={props.summary.selectedKey}
        loading={props.summary.loading}
        exporting={props.exporting}
        emptyText={summaryEmptyText}
        pageIndex={props.summary.pageIndex}
        rowKey={sourceSummaryRowKey}
        accessibleName={(row) => [
          sourceSummaryPrimary(row, props.dimension),
          sourceSummarySecondary(row, props.dimension),
          `被互认 ${row.recognitionCount}`,
        ].join('，')}
        renderName={(row) => sourceSummaryPrimary(row, props.dimension)}
        renderSecondary={(row) => sourceSummarySecondary(row, props.dimension)}
        renderMetrics={(row) => <span className='statistics-board-group-row__count'>
          <span className='statistics-board-group-row__count-value'>{row.recognitionCount}</span>
          <span className='statistics-board-group-row__count-label'>被互认</span>
        </span>}
        onDimensionChange={props.onDimensionChange}
        onPageChange={props.onSummaryPageChange}
        onSelectRow={props.onSelectRow}
        onExport={props.onExportSummary}
      />
    </section>

    <section aria-label='明细'>
      <div className='statistics-board-detail-anchor' aria-label='明细'>
        <Card
          size='small'
          className='statistics-board-detail-card'
          title={selectedRow === null ? '明细' : sourceDetailTitle(selectedRow, props.dimension)}
          extra={<Space size={12} align='center'>
            {selectedRow === null ? null : <span className='statistics-board-source-count'>被互认 {selectedRow.recognitionCount} 次</span>}
            <Button
              size='small'
              aria-label='导出明细'
              loading={props.exporting}
              disabled={props.exporting || selectedRow === null}
              onClick={props.onExportDetails}
            >导出明细</Button>
          </Space>}
        >
          {selectedRow === null ? <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={detailCardEmptyText}
          /> : <>
            {/* 卡头下只有一行上下文：统计时间、承接范围与明细总数；不设指标摘要条与独立工具行。 */}
            <Typography.Text type='secondary' className='statistics-board-detail-context'>{detailContextText}</Typography.Text>

            <StatisticsDetailPanel<SourceDetailRow>
              missingNameCodes={missingScopeNameCodes(props.detail.rows.flatMap((row) => [
                { code: row.source.organizationCode, name: row.source.organizationName },
                { code: row.source.hospitalCode, name: row.source.hospitalName },
                { code: row.source.branchCode, name: row.source.branchName },
                { code: row.receiver.organizationCode, name: row.receiver.organizationName },
                { code: row.receiver.hospitalCode, name: row.receiver.hospitalName },
                { code: row.receiver.branchCode, name: row.receiver.branchName },
              ]))}
              rows={props.detail.rows}
              columns={columns}
              loading={props.detail.loading}
              pagination={props.detail.pagination}
              emptyText={detailTableEmptyText}
            />
          </>}
        </Card>
      </div>
    </section>
  </div>
}
